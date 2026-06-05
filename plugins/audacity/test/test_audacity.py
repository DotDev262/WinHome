import configparser
import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def run_plugin(payload: dict, env: dict = None) -> dict:
    merged_env = os.environ.copy()
    if env:
        merged_env.update(env)

    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=merged_env,
    )

    return json.loads(result.stdout.strip())


def read_cfg(path: str) -> configparser.RawConfigParser:
    parser = configparser.RawConfigParser()
    parser.optionxform = str

    with open(path, "r", encoding="utf-8") as f:
        raw = f.read()

    lines = raw.splitlines(keepends=True)
    if lines and not lines[0].lstrip().startswith("["):
        raw = "[__root__]\n" + raw

    parser.read_string(raw)
    return parser


# ---------------------------------------------------------------------------
# check_installed
# ---------------------------------------------------------------------------


def test_check_installed_dir_exists():
    with tempfile.TemporaryDirectory() as tmp:
        audacity_dir = os.path.join(tmp, "audacity")
        os.makedirs(audacity_dir)

        res = run_plugin(
            {
                "requestId": "ci-1",
                "command": "check_installed",
                "args": {},
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        # FIX 4: result is bare bool wrapped as {"installed": true}
        assert res["installed"] is True
        assert "requestId" in res

    print("✓ check_installed_dir_exists")


def test_check_installed_dir_missing():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "ci-2",
                "command": "check_installed",
                "args": {},
                "context": {},
            },
            env={"APPDATA": tmp, "PATH": tmp},
        )

        assert res["installed"] is False

    print("✓ check_installed_dir_missing")


def test_check_installed_no_appdata():
    env = os.environ.copy()
    env.pop("APPDATA", None)

    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(
            {
                "requestId": "ci-3",
                "command": "check_installed",
                "args": {},
                "context": {},
            }
        ),
        capture_output=True,
        text=True,
        env=env,
    )

    res = json.loads(result.stdout.strip())
    # Without APPDATA, installed is False (no error, just False)
    assert res["installed"] is False

    print("✓ check_installed_no_appdata")


# ---------------------------------------------------------------------------
# apply — basic writes
# ---------------------------------------------------------------------------


def test_apply_creates_config_dir():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "a-1",
                "command": "apply",
                "args": {
                    "settings": {
                        "AudioIO/SampleRate": 44100,
                        "GUI/Theme": "dark",
                    }
                },
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        assert res["changed"] is True
        assert "error" not in res

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        assert os.path.exists(cfg_path)

    print("✓ apply_creates_config_dir")


def test_apply_writes_correct_values():
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "a-2",
                "command": "apply",
                "args": {
                    "settings": {
                        "AudioIO/BufferLength": 100,
                        "AudioIO/LatencyDuration": 100,
                        "AudioIO/RecordingDevice": "Microphone (USB)",
                        "AudioIO/PlaybackDevice": "Speakers (Realtek)",
                        "AudioIO/SampleRate": 48000,
                        "Quality/SampleRate": 48000,
                        "Quality/SampleFormat": "32-float",
                        "Quality/RealTimeResample": True,
                        "GUI/Language": "en",
                        "GUI/Theme": "dark",
                        "GUI/ShowSplashScreen": False,
                        "FileFormats/FFmpegFound": True,
                        "TrackBehaviors/TypeToCreateAClip": True,
                    }
                },
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        parser = read_cfg(cfg_path)

        assert parser.get("AudioIO", "BufferLength") == "100"
        assert parser.get("AudioIO", "RecordingDevice") == "Microphone (USB)"
        assert parser.get("AudioIO", "SampleRate") == "48000"
        assert parser.get("Quality", "SampleFormat") == "32-float"
        assert parser.get("Quality", "RealTimeResample") == "1"
        assert parser.get("GUI", "ShowSplashScreen") == "0"
        assert parser.get("FileFormats", "FFmpegFound") == "1"

    print("✓ apply_writes_correct_values")


def test_apply_bool_casting():
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "a-3",
                "command": "apply",
                "args": {
                    "settings": {
                        "GUI/ShowSplashScreen": True,
                        "FileFormats/FFmpegFound": False,
                    }
                },
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        parser = read_cfg(cfg_path)

        assert parser.get("GUI", "ShowSplashScreen") == "1"
        assert parser.get("FileFormats", "FFmpegFound") == "0"

    print("✓ apply_bool_casting")


# ---------------------------------------------------------------------------
# apply — dry-run
# ---------------------------------------------------------------------------


def test_apply_dry_run_no_file():
    """Dry-run with changes pending must report changed=True but not write file."""
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "dr-1",
                "command": "apply",
                # FIX 6: dryRun comes from args
                "args": {
                    "dryRun": True,
                    "settings": {"AudioIO/SampleRate": 44100},
                },
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        assert res["changed"] is True
        assert "error" not in res

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        assert not os.path.exists(cfg_path)

    print("✓ apply_dry_run_no_file")


def test_apply_dry_run_existing_file_unchanged():
    """Dry-run must report changed=True but not modify the file on disk."""
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "dr-2a",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "light"}},
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        mtime_before = os.path.getmtime(cfg_path)

        res = run_plugin(
            {
                "requestId": "dr-2b",
                "command": "apply",
                # FIX 6: dryRun in args
                "args": {
                    "dryRun": True,
                    "settings": {"GUI/Theme": "dark"},
                },
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        assert res["changed"] is True
        assert "error" not in res
        assert os.path.getmtime(cfg_path) == mtime_before

    print("✓ apply_dry_run_existing_file_unchanged")


# ---------------------------------------------------------------------------
# apply — merge / idempotency
# ---------------------------------------------------------------------------


def test_apply_merges_with_existing_config():
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "m-1a",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "light", "GUI/Language": "en"}},
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        run_plugin(
            {
                "requestId": "m-1b",
                "command": "apply",
                "args": {"settings": {"AudioIO/SampleRate": 44100}},
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        parser = read_cfg(cfg_path)

        assert parser.get("GUI", "Theme") == "light"
        assert parser.get("GUI", "Language") == "en"
        assert parser.get("AudioIO", "SampleRate") == "44100"

    print("✓ apply_merges_with_existing_config")


def test_apply_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "i-1",
            "command": "apply",
            "args": {"settings": {"GUI/Theme": "dark", "AudioIO/SampleRate": 48000}},
            "context": {},
        }

        first = run_plugin(payload, env={"APPDATA": tmp})
        second = run_plugin(payload, env={"APPDATA": tmp})

        assert first["changed"] is True
        assert "error" not in first

        assert second["changed"] is False
        assert "error" not in second

    print("✓ apply_idempotent")


def test_apply_partial_update():
    with tempfile.TemporaryDirectory() as tmp:
        env = {"APPDATA": tmp}

        run_plugin(
            {
                "requestId": "pu-1",
                "command": "apply",
                "args": {
                    "settings": {
                        "AudioIO/SampleRate": 44100,
                        "GUI/Theme": "light",
                    }
                },
                "context": {},
            },
            env=env,
        )

        res = run_plugin(
            {
                "requestId": "pu-2",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "dark"}},
                "context": {},
            },
            env=env,
        )

        assert res["changed"] is True

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        parser = read_cfg(cfg_path)

        assert parser.get("AudioIO", "SampleRate") == "44100"
        assert parser.get("GUI", "Theme") == "dark"

    print("✓ apply_partial_update")


# ---------------------------------------------------------------------------
# apply — existing real-world-style audacity.cfg
# ---------------------------------------------------------------------------

SAMPLE_CFG = """\
[AudioIO]
BufferLength=100
LatencyDuration=100
RecordingDevice=default
PlaybackDevice=default
SampleRate=44100

[Quality]
SampleRate=44100
SampleFormat=16-bit

[GUI]
Language=en
Theme=light
ShowSplashScreen=1

[FileFormats]
FFmpegFound=0
"""


def test_apply_with_sample_cfg():
    with tempfile.TemporaryDirectory() as tmp:
        cfg_dir = os.path.join(tmp, "audacity")
        os.makedirs(cfg_dir)
        cfg_path = os.path.join(cfg_dir, "audacity.cfg")

        with open(cfg_path, "w", encoding="utf-8") as f:
            f.write(SAMPLE_CFG)

        res = run_plugin(
            {
                "requestId": "s-1",
                "command": "apply",
                "args": {
                    "settings": {
                        "AudioIO/SampleRate": 48000,
                        "Quality/SampleFormat": "32-float",
                        "GUI/Theme": "dark",
                        "GUI/ShowSplashScreen": False,
                        "FileFormats/FFmpegFound": True,
                    }
                },
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        assert res["changed"] is True
        assert "error" not in res

        parser = read_cfg(cfg_path)

        assert parser.get("AudioIO", "SampleRate") == "48000"
        assert parser.get("Quality", "SampleFormat") == "32-float"
        assert parser.get("GUI", "Theme") == "dark"
        assert parser.get("GUI", "ShowSplashScreen") == "0"
        assert parser.get("FileFormats", "FFmpegFound") == "1"
        assert parser.get("AudioIO", "BufferLength") == "100"
        assert parser.get("AudioIO", "RecordingDevice") == "default"
        assert parser.get("GUI", "Language") == "en"

    print("✓ apply_with_sample_cfg")


# ---------------------------------------------------------------------------
# Atomic write / POSIX newline
# ---------------------------------------------------------------------------


def test_apply_atomic_no_temp_files_left():
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "aw-1",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "dark"}},
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        cfg_dir = os.path.join(tmp, "audacity")
        leftover = [f for f in os.listdir(cfg_dir) if f.endswith(".tmp")]

        assert leftover == [], f"Temp files left behind: {leftover}"

    print("✓ apply_atomic_no_temp_files_left")


def test_apply_posix_trailing_newline():
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "nl-1",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "dark"}},
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")

        with open(cfg_path, "rb") as f:
            content = f.read()

        assert content.endswith(b"\n"), "File does not end with a newline"

    print("✓ apply_posix_trailing_newline")


# ---------------------------------------------------------------------------
# FIX 1: empty stdin returns JSON error
# ---------------------------------------------------------------------------


def test_empty_stdin_returns_json_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="",
        capture_output=True,
        text=True,
        env=os.environ.copy(),
    )

    assert result.stdout.strip(), "Expected JSON output on empty stdin"
    res = json.loads(result.stdout.strip())
    assert "error" in res

    print("✓ empty_stdin_returns_json_error")


# ---------------------------------------------------------------------------
# FIX 2: invalid JSON returns JSON error (not sys.exit)
# ---------------------------------------------------------------------------


def test_invalid_json_returns_json_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="not valid json {{{",
        capture_output=True,
        text=True,
        env=os.environ.copy(),
    )

    assert result.returncode == 0, "Process must not exit with error code"
    res = json.loads(result.stdout.strip())
    assert "error" in res

    print("✓ invalid_json_returns_json_error")


# ---------------------------------------------------------------------------
# Error paths
# ---------------------------------------------------------------------------


def test_unknown_command():
    res = run_plugin(
        {
            "requestId": "e-1",
            "command": "explode",
            "args": {},
            "context": {},
        }
    )

    assert "error" in res

    print("✓ unknown_command")


def test_apply_no_settings_no_change():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "e-2",
                "command": "apply",
                "args": {"settings": {}},
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        assert res["changed"] is False
        assert "error" not in res

    print("✓ apply_no_settings_no_change")


# ---------------------------------------------------------------------------
# Runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    test_check_installed_dir_exists()
    test_check_installed_dir_missing()
    test_check_installed_no_appdata()

    test_apply_creates_config_dir()
    test_apply_writes_correct_values()
    test_apply_bool_casting()

    test_apply_dry_run_no_file()
    test_apply_dry_run_existing_file_unchanged()

    test_apply_merges_with_existing_config()
    test_apply_idempotent()
    test_apply_partial_update()

    test_apply_with_sample_cfg()

    test_apply_atomic_no_temp_files_left()
    test_apply_posix_trailing_newline()

    test_empty_stdin_returns_json_error()
    test_invalid_json_returns_json_error()

    test_unknown_command()
    test_apply_no_settings_no_change()

    print("\nAll tests passed.")
