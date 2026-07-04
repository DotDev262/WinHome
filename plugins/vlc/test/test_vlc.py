"""
Tests for the VLC configuration provider plugin.

Run with:  python -m pytest plugins/vlc/test/test_vlc.py -v
       or:  python plugins/vlc/test/test_vlc.py
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
import textwrap

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))

SAMPLE_VLCRC = textwrap.dedent("""\
    # VLC media player preferences
    [core]
    volume=256
    network-caching=1000
    file-caching=300
    video-on-top=0
    snapshot-path=C:\\Users\\User\\Pictures
    snapshot-format=png
    [qt]
    qt-max-volume=125
    [sout]
    enable-lua-sd=
""")


def run_plugin(payload: dict, env: dict | None = None) -> dict:
    merged_env = {**os.environ, **(env or {})}
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=merged_env,
    )
    return json.loads(result.stdout.strip())


def _write_vlcrc(tmp_dir: str, content: str) -> str:
    vlc_dir = os.path.join(tmp_dir, "vlc")
    os.makedirs(vlc_dir, exist_ok=True)
    path = os.path.join(vlc_dir, "vlcrc")
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    return path


def test_empty_stdin_returns_json_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="",
        capture_output=True,
        text=True,
    )
    res = json.loads(result.stdout.strip())
    assert res["requestId"] == "unknown"
    assert "error" in res
    print("âœ“ empty_stdin_returns_json_error")


def test_bad_json_returns_json_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="not-json",
        capture_output=True,
        text=True,
    )
    res = json.loads(result.stdout.strip())
    assert res["requestId"] == "unknown"
    assert "error" in res
    print("âœ“ bad_json_returns_json_error")


def test_check_installed_present():
    with tempfile.TemporaryDirectory() as tmp:
        os.makedirs(os.path.join(tmp, "vlc"))
        res = run_plugin(
            {"requestId": "1", "command": "check_installed", "args": {}, "context": {}},
            env={"APPDATA": tmp},
        )
        assert res["installed"] is True
        assert res["requestId"] == "1"
        print("âœ“ check_installed_present")


def test_check_installed_absent():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {"requestId": "2", "command": "check_installed", "args": {}, "context": {}},
            env={"APPDATA": tmp, "PATH": ""},
        )
        assert res["installed"] is False
        print("âœ“ check_installed_absent")


def test_check_installed_no_success_field():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {"requestId": "ci3", "command": "check_installed", "args": {}, "context": {}},
            env={"APPDATA": tmp},
        )
        assert "success" not in res
        assert "data" not in res
        print("âœ“ check_installed_no_success_field")


def test_apply_dry_run_does_not_write():
    with tempfile.TemporaryDirectory() as tmp:
        cfg = _write_vlcrc(tmp, SAMPLE_VLCRC)
        original = open(cfg).read()
        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {"settings": {"volume": 50}, "dryRun": True},
            },
            env={"APPDATA": tmp},
        )
        assert not res.get("error")
        assert open(cfg).read() == original
        print("âœ“ apply_dry_run_does_not_write")


def test_apply_dry_run_changed_true_when_settings_exist():
    with tempfile.TemporaryDirectory() as tmp:
        _write_vlcrc(tmp, SAMPLE_VLCRC)
        res = run_plugin(
            {
                "requestId": "dr2",
                "command": "apply",
                "args": {"settings": {"volume": 50}, "dryRun": True},
            },
            env={"APPDATA": tmp},
        )
        assert res["changed"] is True
        print("âœ“ apply_dry_run_changed_true_when_settings_exist")


def test_apply_dry_run_changed_false_when_no_settings():
    with tempfile.TemporaryDirectory() as tmp:
        _write_vlcrc(tmp, SAMPLE_VLCRC)
        res = run_plugin(
            {
                "requestId": "dr3",
                "command": "apply",
                "args": {"settings": {}, "dryRun": True},
            },
            env={"APPDATA": tmp},
        )
        assert res["changed"] is False
        print("âœ“ apply_dry_run_changed_false_when_no_settings")


def test_apply_updates_existing_key():
    with tempfile.TemporaryDirectory() as tmp:
        cfg = _write_vlcrc(tmp, SAMPLE_VLCRC)
        res = run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"settings": {"volume": 100}},
            },
            env={"APPDATA": tmp},
        )
        assert res["changed"] is True
        content = open(cfg).read()
        assert "volume=100" in content
        assert "volume=256" not in content
        print("âœ“ apply_updates_existing_key")


def test_apply_adds_new_key():
    with tempfile.TemporaryDirectory() as tmp:
        cfg = _write_vlcrc(tmp, SAMPLE_VLCRC)
        run_plugin(
            {
                "requestId": "5",
                "command": "apply",
                "args": {"settings": {"aspect-ratio": "16:9"}},
            },
            env={"APPDATA": tmp},
        )
        assert "aspect-ratio=16:9" in open(cfg).read()
        print("âœ“ apply_adds_new_key")


def test_apply_bool_settings():
    with tempfile.TemporaryDirectory() as tmp:
        cfg = _write_vlcrc(tmp, SAMPLE_VLCRC)
        run_plugin(
            {
                "requestId": "6",
                "command": "apply",
                "args": {"settings": {"video-on-top": True, "playlist-cork": False}},
            },
            env={"APPDATA": tmp},
        )
        content = open(cfg).read()
        assert "video-on-top=1" in content
        assert "playlist-cork=0" in content
        print("âœ“ apply_bool_settings")


def test_apply_creates_config_when_absent():
    with tempfile.TemporaryDirectory() as tmp:
        cfg_path = os.path.join(tmp, "vlc", "vlcrc")
        assert not os.path.exists(cfg_path)
        res = run_plugin(
            {
                "requestId": "7",
                "command": "apply",
                "args": {"settings": {"volume": 200}},
            },
            env={"APPDATA": tmp},
        )
        assert not res.get("error")
        assert os.path.exists(cfg_path)
        assert "volume=200" in open(cfg_path).read()
        print("âœ“ apply_creates_config_when_absent")


def test_apply_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        _write_vlcrc(tmp, SAMPLE_VLCRC)
        cfg = os.path.join(tmp, "vlc", "vlcrc")
        payload = {
            "requestId": "8",
            "command": "apply",
            "args": {"settings": {"volume": 300, "network-caching": 1500}},
        }
        run_plugin(payload, env={"APPDATA": tmp})
        first = open(cfg).read()
        run_plugin(payload, env={"APPDATA": tmp})
        second = open(cfg).read()
        assert first == second
        print("âœ“ apply_idempotent")


def test_apply_preserves_unknown_sections_and_keys():
    with tempfile.TemporaryDirectory() as tmp:
        cfg = _write_vlcrc(tmp, SAMPLE_VLCRC)
        run_plugin(
            {
                "requestId": "9",
                "command": "apply",
                "args": {"settings": {"volume": 128}},
            },
            env={"APPDATA": tmp},
        )
        content = open(cfg).read()
        assert "[sout]" in content
        assert "enable-lua-sd=" in content
        assert "[qt]" in content
        print("âœ“ apply_preserves_unknown_sections_and_keys")


def test_apply_multivalue_key_collapsed():
    with tempfile.TemporaryDirectory() as tmp:
        multi = "[core]\nvolume=256\nvolume=512\n"
        cfg = _write_vlcrc(tmp, multi)
        run_plugin(
            {
                "requestId": "10",
                "command": "apply",
                "args": {"settings": {"volume": 100}},
            },
            env={"APPDATA": tmp},
        )
        content = open(cfg).read()
        assert content.count("volume=") == 1
        assert "volume=100" in content
        print("âœ“ apply_multivalue_key_collapsed")


def test_posix_trailing_newline():
    with tempfile.TemporaryDirectory() as tmp:
        run_plugin(
            {
                "requestId": "11",
                "command": "apply",
                "args": {"settings": {"volume": 256}},
            },
            env={"APPDATA": tmp},
        )
        raw = open(os.path.join(tmp, "vlc", "vlcrc"), "rb").read()
        assert raw.endswith(b"\n")
        print("âœ“ posix_trailing_newline")


def test_apply_no_success_field():
    with tempfile.TemporaryDirectory() as tmp:
        _write_vlcrc(tmp, SAMPLE_VLCRC)
        res = run_plugin(
            {
                "requestId": "ns1",
                "command": "apply",
                "args": {"settings": {"volume": 128}},
            },
            env={"APPDATA": tmp},
        )
        assert "success" not in res
        print("âœ“ apply_no_success_field")


def test_apply_settings_not_dict():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "nd1",
                "command": "apply",
                "args": {"settings": "invalid"},
            },
            env={"APPDATA": tmp},
        )
        assert "error" in res
        assert res["requestId"] == "nd1"
        print("âœ“ apply_settings_not_dict")


def test_unknown_command():
    res = run_plugin({"requestId": "12", "command": "explode", "args": {}, "context": {}})
    assert "error" in res
    print("âœ“ unknown_command")


def test_request_id_echoed():
    res = run_plugin({"requestId": "my-custom-id", "command": "check_installed", "args": {}, "context": {}})
    assert res["requestId"] == "my-custom-id"
    print("âœ“ request_id_echoed")


def test_request_id_null_defaults_to_unknown():
    res = run_plugin({"requestId": None, "command": "check_installed", "args": {}, "context": {}})
    assert res["requestId"] == "unknown"
    print("âœ“ request_id_null_defaults_to_unknown")


if __name__ == "__main__":
    test_empty_stdin_returns_json_error()
    test_bad_json_returns_json_error()
    test_check_installed_present()
    test_check_installed_absent()
    test_check_installed_no_success_field()
    test_apply_dry_run_does_not_write()
    test_apply_dry_run_changed_true_when_settings_exist()
    test_apply_dry_run_changed_false_when_no_settings()
    test_apply_updates_existing_key()
    test_apply_adds_new_key()
    test_apply_bool_settings()
    test_apply_creates_config_when_absent()
    test_apply_idempotent()
    test_apply_preserves_unknown_sections_and_keys()
    test_apply_multivalue_key_collapsed()
    test_posix_trailing_newline()
    test_apply_no_success_field()
    test_apply_settings_not_dict()
    test_unknown_command()
    test_request_id_echoed()
    test_request_id_null_defaults_to_unknown()

    print("\nAll tests passed.")
