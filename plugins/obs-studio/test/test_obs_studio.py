import configparser
import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py")
)


def run_plugin(payload: dict, env=None) -> dict:
    merged_env = dict(os.environ)
    if env:
        upper_map = {k.upper(): k for k in merged_env}
        for k, v in env.items():
            existing_key = upper_map.get(k.upper(), k)
            merged_env[existing_key] = v
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=merged_env,
    )
    return json.loads(result.stdout.strip())


def write_global_ini(obs_dir, profile_name):
    global_ini = os.path.join(obs_dir, "global.ini")
    config = configparser.RawConfigParser()
    config.add_section("Basic")
    config.set("Basic", "ProfileDir", profile_name)
    os.makedirs(obs_dir, exist_ok=True)
    with open(global_ini, "w") as f:
        config.write(f)


def test_check_installed_false():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "1",
                "command": "check_installed",
                "args": {},
                "context": {},
            },
            env={"OBS_INSTALL_DIR": tmp},
        )
        assert res["success"]
        assert res["data"] is False
        print("PASS: check_installed_false")


def test_check_installed_true():
    with tempfile.TemporaryDirectory() as tmp:
        exe_dir = os.path.join(tmp, "bin", "64bit")
        os.makedirs(exe_dir)
        open(os.path.join(exe_dir, "obs64.exe"), "w").close()

        res = run_plugin(
            {
                "requestId": "2",
                "command": "check_installed",
                "args": {},
                "context": {},
            },
            env={"OBS_INSTALL_DIR": tmp},
        )
        assert res["success"]
        assert res["data"] is True
        print("PASS: check_installed_true")


def test_apply_general_creates_global_ini():
    with tempfile.TemporaryDirectory() as tmp:
        obs_dir = os.path.join(tmp, "obs-studio")

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {"general": {"theme": "Yami", "language": "en-US"}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]

        global_ini = os.path.join(obs_dir, "global.ini")
        assert os.path.exists(global_ini)

        config = configparser.RawConfigParser()
        config.read(global_ini)
        assert config.get("General", "theme") == "Yami"
        assert config.get("General", "language") == "en-US"
        print("PASS: apply_general_creates_global_ini")


def test_apply_general_merges_preserves_existing():
    with tempfile.TemporaryDirectory() as tmp:
        obs_dir = os.path.join(tmp, "obs-studio")
        os.makedirs(obs_dir)

        global_ini = os.path.join(obs_dir, "global.ini")
        config = configparser.RawConfigParser()
        config.add_section("General")
        config.set("General", "Theme", "Default")
        config.set("General", "ExistingKey", "KeepMe")
        with open(global_ini, "w") as f:
            config.write(f)

        run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"general": {"theme": "Yami"}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        config2 = configparser.RawConfigParser()
        config2.read(global_ini)
        assert config2.get("General", "theme") == "Yami"
        assert config2.get("General", "existingkey") == "KeepMe"
        print("PASS: apply_general_merges_preserves_existing")


def test_apply_profile_video_audio():
    with tempfile.TemporaryDirectory() as tmp:
        obs_dir = os.path.join(tmp, "obs-studio")
        write_global_ini(obs_dir, "Streaming")

        res = run_plugin(
            {
                "requestId": "5",
                "command": "apply",
                "args": {
                    "video": {"base_resolution": "1920x1080", "fps_common": 60},
                    "audio": {"sample_rate": 48000, "channels": "Stereo"},
                },
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]

        basic_ini = os.path.join(
            obs_dir, "basic", "profiles", "Streaming", "basic.ini"
        )
        assert os.path.exists(basic_ini)

        config = configparser.RawConfigParser()
        config.read(basic_ini)
        assert config.get("Video", "base") == "1920x1080"
        assert config.get("Video", "fpscommon") == "60"
        assert config.get("Audio", "samplerate") == "48000"
        print("PASS: apply_profile_video_audio")


def test_apply_dry_run_no_files_written():
    with tempfile.TemporaryDirectory() as tmp:
        obs_dir = os.path.join(tmp, "obs-studio")

        res = run_plugin(
            {
                "requestId": "6",
                "command": "apply",
                "args": {"general": {"theme": "Yami"}},
                "context": {"dryRun": True},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]
        assert not os.path.exists(os.path.join(obs_dir, "global.ini"))
        print("PASS: apply_dry_run_no_files_written")


def test_apply_no_changes_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "7",
            "command": "apply",
            "args": {"general": {"theme": "Yami"}},
            "context": {"dryRun": False},
        }
        env = {"APPDATA": tmp}

        run_plugin(payload, env=env)
        res = run_plugin(payload, env=env)

        assert res["success"]
        assert not res["changed"]
        print("PASS: apply_no_changes_idempotent")


def test_apply_creates_profile_directories():
    with tempfile.TemporaryDirectory() as tmp:
        obs_dir = os.path.join(tmp, "obs-studio")

        res = run_plugin(
            {
                "requestId": "8",
                "command": "apply",
                "args": {
                    "profiles": [
                        {"name": "Streaming", "settings": {}},
                        {"name": "Recording", "settings": {}},
                    ]
                },
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]
        assert os.path.exists(
            os.path.join(obs_dir, "basic", "profiles", "Streaming", "basic.ini")
        )
        assert os.path.exists(
            os.path.join(obs_dir, "basic", "profiles", "Recording", "basic.ini")
        )
        print("PASS: apply_creates_profile_directories")


def test_apply_profile_explicit_name():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "9",
                "command": "apply",
                "args": {
                    "profile": "MyProfile",
                    "video": {"base_resolution": "2560x1440"},
                },
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"]
        obs_dir = os.path.join(tmp, "obs-studio")
        basic_ini = os.path.join(
            obs_dir, "basic", "profiles", "MyProfile", "basic.ini"
        )
        assert os.path.exists(basic_ini)

        config = configparser.RawConfigParser()
        config.read(basic_ini)
        assert config.get("Video", "base") == "2560x1440"
        print("PASS: apply_profile_explicit_name")


def test_unknown_command():
    res = run_plugin(
        {"requestId": "10", "command": "explode", "args": {}, "context": {}}
    )
    assert not res["success"]
    assert "error" in res
    print("PASS: unknown_command")


if __name__ == "__main__":
    test_check_installed_false()
    test_check_installed_true()
    test_apply_general_creates_global_ini()
    test_apply_general_merges_preserves_existing()
    test_apply_profile_video_audio()
    test_apply_dry_run_no_files_written()
    test_apply_no_changes_idempotent()
    test_apply_creates_profile_directories()
    test_apply_profile_explicit_name()
    test_unknown_command()
    print("\nAll tests passed.")
