import configparser
import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(
    os.path.join(
        os.path.dirname(__file__),
        "..",
        "src",
        "plugin.py"
    )
)

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
    """Read an audacity.cfg, tolerating bare key=value before any section."""
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
    """Returns True when %APPDATA%\\audacity\\ directory is present."""
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

        assert res["success"] is True
        assert res["changed"] is False
        assert res["data"] is True

    print("✓ check_installed_dir_exists")


def test_check_installed_dir_missing():
    """Returns False when %APPDATA%\\audacity\\ is absent and exe not on PATH."""
    with tempfile.TemporaryDirectory() as tmp:
        # tmp has no 'audacity' sub-dir; PATH set to empty location
        res = run_plugin(
            {
                "requestId": "ci-2",
                "command": "check_installed",
                "args": {},
                "context": {},
            },
            env={"APPDATA": tmp, "PATH": tmp},
        )

        assert res["success"] is True
        assert res["data"] is False

    print("✓ check_installed_dir_missing")


def test_check_installed_no_appdata():
    """Returns success=False when APPDATA is missing entirely."""
    env = os.environ.copy()
    env.pop("APPDATA", None)

    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps({
            "requestId": "ci-3",
            "command": "check_installed",
            "args": {},
            "context": {},
        }),
        capture_output=True,
        text=True,
        env=env,
    )

    res = json.loads(result.stdout.strip())

    assert res["success"] is False
    assert "error" in res

    print("✓ check_installed_no_appdata")


# ---------------------------------------------------------------------------
# apply — basic writes
# ---------------------------------------------------------------------------

def test_apply_creates_config_dir():
    """apply creates %APPDATA%\\audacity\\ and audacity.cfg when absent."""
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
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"] is True
        assert res["changed"] is True

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        assert os.path.exists(cfg_path)

    print("✓ apply_creates_config_dir")


def test_apply_writes_correct_values():
    """Parsed audacity.cfg contains the values that were applied."""
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
                "context": {"dryRun": False},
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
    """True maps to '1' and False maps to '0' in the written file."""
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
                "context": {"dryRun": False},
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
    """Dry-run with no existing config must not create any file."""
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "dr-1",
                "command": "apply",
                "args": {"settings": {"AudioIO/SampleRate": 44100}},
                "context": {"dryRun": True},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"] is True
        assert res["changed"] is False

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        assert not os.path.exists(cfg_path)

    print("✓ apply_dry_run_no_file")


def test_apply_dry_run_existing_file_unchanged():
    """Dry-run must not modify an already-existing config file."""
    with tempfile.TemporaryDirectory() as tmp:
        # First real write
        run_plugin(
            {
                "requestId": "dr-2a",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "light"}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        mtime_before = os.path.getmtime(cfg_path)

        # Dry-run with a new value
        res = run_plugin(
            {
                "requestId": "dr-2b",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "dark"}},
                "context": {"dryRun": True},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"] is True
        assert res["changed"] is False
        assert os.path.getmtime(cfg_path) == mtime_before

    print("✓ apply_dry_run_existing_file_unchanged")


# ---------------------------------------------------------------------------
# apply — merge / idempotency
# ---------------------------------------------------------------------------

def test_apply_merges_with_existing_config():
    """Applying new keys preserves keys already in the file."""
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "m-1a",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "light", "GUI/Language": "en"}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        run_plugin(
            {
                "requestId": "m-1b",
                "command": "apply",
                "args": {"settings": {"AudioIO/SampleRate": 44100}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        parser = read_cfg(cfg_path)

        # Original keys must still be there
        assert parser.get("GUI", "Theme") == "light"
        assert parser.get("GUI", "Language") == "en"
        # New key was added
        assert parser.get("AudioIO", "SampleRate") == "44100"

    print("✓ apply_merges_with_existing_config")


def test_apply_idempotent():
    """Applying the same settings twice reports changed=False on the second run."""
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "i-1",
            "command": "apply",
            "args": {"settings": {"GUI/Theme": "dark", "AudioIO/SampleRate": 48000}},
            "context": {"dryRun": False},
        }

        first = run_plugin(payload, env={"APPDATA": tmp})
        second = run_plugin(payload, env={"APPDATA": tmp})

        assert first["success"] is True
        assert first["changed"] is True

        assert second["success"] is True
        assert second["changed"] is False

    print("✓ apply_idempotent")


def test_apply_partial_update():
    """Changing one key out of many reports changed=True only for that run."""
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
                "context": {"dryRun": False},
            },
            env=env,
        )

        res = run_plugin(
            {
                "requestId": "pu-2",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "dark"}},
                "context": {"dryRun": False},
            },
            env=env,
        )

        assert res["success"] is True
        assert res["changed"] is True

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")
        parser = read_cfg(cfg_path)

        # Other key untouched
        assert parser.get("AudioIO", "SampleRate") == "44100"
        # Updated key reflects new value
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
    """Correctly patches a realistic pre-existing audacity.cfg."""
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
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"] is True
        assert res["changed"] is True

        parser = read_cfg(cfg_path)

        # Changed values
        assert parser.get("AudioIO", "SampleRate") == "48000"
        assert parser.get("Quality", "SampleFormat") == "32-float"
        assert parser.get("GUI", "Theme") == "dark"
        assert parser.get("GUI", "ShowSplashScreen") == "0"
        assert parser.get("FileFormats", "FFmpegFound") == "1"

        # Untouched values preserved
        assert parser.get("AudioIO", "BufferLength") == "100"
        assert parser.get("AudioIO", "RecordingDevice") == "default"
        assert parser.get("GUI", "Language") == "en"

    print("✓ apply_with_sample_cfg")


# ---------------------------------------------------------------------------
# apply — atomic write verification
# ---------------------------------------------------------------------------

def test_apply_atomic_no_temp_files_left():
    """No .tmp files should remain after a successful write."""
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "aw-1",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "dark"}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        cfg_dir = os.path.join(tmp, "audacity")
        leftover = [f for f in os.listdir(cfg_dir) if f.endswith(".tmp")]

        assert leftover == [], f"Temp files left behind: {leftover}"

    print("✓ apply_atomic_no_temp_files_left")


# ---------------------------------------------------------------------------
# apply — POSIX trailing newline
# ---------------------------------------------------------------------------

def test_apply_posix_trailing_newline():
    """Written config file must end with a newline character."""
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "nl-1",
                "command": "apply",
                "args": {"settings": {"GUI/Theme": "dark"}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        cfg_path = os.path.join(tmp, "audacity", "audacity.cfg")

        with open(cfg_path, "rb") as f:
            content = f.read()

        assert content.endswith(b"\n"), "File does not end with a newline"

    print("✓ apply_posix_trailing_newline")


# ---------------------------------------------------------------------------
# Error paths
# ---------------------------------------------------------------------------

def test_unknown_command():
    res = run_plugin({
        "requestId": "e-1",
        "command": "explode",
        "args": {},
        "context": {},
    })

    assert res["success"] is False
    assert "error" in res

    print("✓ unknown_command")


def test_apply_no_settings_no_change():
    """Applying an empty settings dict must report changed=False."""
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "e-2",
                "command": "apply",
                "args": {"settings": {}},
                "context": {"dryRun": False},
            },
            env={"APPDATA": tmp},
        )

        assert res["success"] is True
        assert res["changed"] is False

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

    test_unknown_command()
    test_apply_no_settings_no_change()

    print("\nAll tests passed.")
