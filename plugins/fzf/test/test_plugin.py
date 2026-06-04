import json
import os
import subprocess
import sys
from pathlib import Path

PLUGIN = Path(__file__).resolve().parents[1] / "src" / "plugin.py"


def run_plugin(payload=None, env=None, raw_input=None):
    input_data = raw_input if raw_input is not None else json.dumps(payload)
    result = subprocess.run(
        [sys.executable, str(PLUGIN)],
        input=input_data,
        capture_output=True,
        text=True,
        env=env,
    )
    output = result.stdout.strip()
    assert output
    return json.loads(output), result


def plugin_env(tmp_path, path=None):
    env = os.environ.copy()
    env["USERPROFILE"] = str(tmp_path)
    env["HOME"] = str(tmp_path)
    if path is not None:
        env["PATH"] = str(path)
    return env


def fzfrc_path(tmp_path):
    return tmp_path / ("_fzfrc" if os.name == "nt" else ".fzfrc")


def assert_schema(response, success=True, changed=False):
    assert response["success"] is success
    assert response["changed"] is changed
    assert "requestId" in response
    assert "data" in response
    if success:
        assert "error" not in response
    else:
        assert "error" in response


def test_apply_fzf_settings(tmp_path):
    env = plugin_env(tmp_path)
    payload = {
        "requestId": "apply-1",
        "command": "apply",
        "args": {
            "settings": {
                "FZF_DEFAULT_OPTS": "--height 40% --border",
                "FZF_CTRL_T_COMMAND": "fd --type f",
            }
        },
        "context": {"dryRun": False},
    }

    response, result = run_plugin(payload, env=env)

    assert_schema(response, success=True, changed=True)
    config_file = fzfrc_path(tmp_path)
    content = config_file.read_text(encoding="utf-8")
    assert 'export FZF_DEFAULT_OPTS="--height 40% --border"' in content
    assert 'export FZF_CTRL_T_COMMAND="fd --type f"' in content
    assert "Updated fzf config" in result.stderr


def test_idempotent(tmp_path):
    env = plugin_env(tmp_path)
    payload = {
        "requestId": "idem-1",
        "command": "apply",
        "args": {"settings": {"FZF_DEFAULT_OPTS": "--height 40% --border"}},
    }

    first, _ = run_plugin(payload, env=env)
    second, _ = run_plugin(payload, env=env)

    assert_schema(first, success=True, changed=True)
    assert_schema(second, success=True, changed=False)


def test_dry_run(tmp_path):
    env = plugin_env(tmp_path)
    config_file = fzfrc_path(tmp_path)
    payload = {
        "requestId": "dry-1",
        "command": "apply",
        "args": {"settings": {"FZF_DEFAULT_OPTS": "--height 40%"}},
        "context": {"dryRun": True},
    }

    response, result = run_plugin(payload, env=env)

    assert_schema(response, success=True, changed=True)
    assert not config_file.exists()
    assert "Would update" in result.stderr


def test_check_installed(tmp_path):
    bin_dir = tmp_path / "bin"
    bin_dir.mkdir()
    executable = bin_dir / ("fzf.exe" if os.name == "nt" else "fzf")
    executable.write_text("", encoding="utf-8")
    executable.chmod(0o755)

    payload = {"requestId": "check-1", "command": "check_installed"}
    installed, _ = run_plugin(payload, env=plugin_env(tmp_path, bin_dir))
    missing, _ = run_plugin(payload, env=plugin_env(tmp_path, tmp_path / "missing"))

    assert_schema(installed, success=True, changed=False)
    assert installed["data"] is True
    assert_schema(missing, success=True, changed=False)
    assert missing["data"] is False


def test_empty_stdin(tmp_path):
    response, _ = run_plugin(env=plugin_env(tmp_path), raw_input="")

    assert_schema(response, success=False, changed=False)
    assert response["requestId"] == "unknown"
    assert "Empty stdin" in response["error"]


def test_malformed_json(tmp_path):
    response, result = run_plugin(env=plugin_env(tmp_path), raw_input="{ not json")

    assert result.returncode == 0
    assert_schema(response, success=False, changed=False)
    assert response["requestId"] == "unknown"
    assert "Failed to parse request" in response["error"]
    assert "Failed to parse request" in result.stderr


def test_invalid_settings_shape(tmp_path):
    payload = {
        "requestId": "bad-settings-1",
        "command": "apply",
        "args": {"settings": "not-an-object"},
    }

    response, _ = run_plugin(payload, env=plugin_env(tmp_path))

    assert_schema(response, success=False, changed=False)
    assert response["error"] == "args.settings must be an object"


def test_parse_existing_config(tmp_path):
    env = plugin_env(tmp_path)
    config_file = fzfrc_path(tmp_path)
    config_file.write_text(
        '# comment\n\nexport FZF_CTRL_T_COMMAND="fd --type f"\nexport OTHER_TOOL="keep me"\n',
        encoding="utf-8",
    )
    payload = {
        "requestId": "parse-1",
        "command": "apply",
        "args": {"settings": {"height": "40%", "border": True}},
    }

    response, _ = run_plugin(payload, env=env)

    assert_schema(response, success=True, changed=True)
    content = config_file.read_text(encoding="utf-8")
    assert 'export FZF_CTRL_T_COMMAND="fd --type f"' in content
    assert 'export OTHER_TOOL="keep me"' in content
    assert 'export FZF_DEFAULT_OPTS="--border true --height 40%"' in content


def test_corrupted_config_backup(tmp_path):
    env = plugin_env(tmp_path)
    config_file = fzfrc_path(tmp_path)
    config_file.write_text('export FZF_DEFAULT_OPTS="unterminated\n', encoding="utf-8")
    payload = {
        "requestId": "corrupt-1",
        "command": "apply",
        "args": {"settings": {"FZF_DEFAULT_OPTS": "--height 60%"}},
    }

    response, result = run_plugin(payload, env=env)

    assert_schema(response, success=True, changed=True)
    backups = list(tmp_path.glob(f"{config_file.name}.bak.*"))
    assert len(backups) == 1
    assert backups[0].read_text(encoding="utf-8") == 'export FZF_DEFAULT_OPTS="unterminated\n'
    assert 'export FZF_DEFAULT_OPTS="--height 60%"' in config_file.read_text(encoding="utf-8")
    assert "Backed up corrupted fzf config" in result.stderr
    assert response["data"]["corrupted"] is True
    assert "backupPath" in response["data"]


def test_utf8_decode_failure_backup(tmp_path):
    env = plugin_env(tmp_path)
    config_file = fzfrc_path(tmp_path)
    config_file.write_bytes(b"\xff\xfe\xfa")
    payload = {
        "requestId": "utf8-1",
        "command": "apply",
        "args": {"settings": {"FZF_DEFAULT_OPTS": "--height 70%"}},
    }

    response, _ = run_plugin(payload, env=env)

    assert_schema(response, success=True, changed=True)
    backups = list(tmp_path.glob(f"{config_file.name}.bak.*"))
    assert len(backups) == 1
    assert backups[0].read_bytes() == b"\xff\xfe\xfa"
    assert 'export FZF_DEFAULT_OPTS="--height 70%"' in config_file.read_text(encoding="utf-8")


def test_shell_escaping_round_trip(tmp_path):
    env = plugin_env(tmp_path)
    value = '--preview "bat $FILE `pwd` C:\\tmp"'
    payload = {
        "requestId": "escape-1",
        "command": "apply",
        "args": {"settings": {"FZF_DEFAULT_OPTS": value}},
    }

    first, _ = run_plugin(payload, env=env)
    second, _ = run_plugin(payload, env=env)

    assert_schema(first, success=True, changed=True)
    assert_schema(second, success=True, changed=False)
    content = fzfrc_path(tmp_path).read_text(encoding="utf-8")
    assert '\\"bat \\$FILE \\`pwd\\` C:\\\\tmp\\"' in content


def test_filesystem_write_error_returns_json(tmp_path):
    bad_home = tmp_path / "profile-as-file"
    bad_home.write_text("not a directory", encoding="utf-8")
    payload = {
        "requestId": "write-error-1",
        "command": "apply",
        "args": {"settings": {"FZF_DEFAULT_OPTS": "--height 40%"}},
    }

    response, result = run_plugin(payload, env=plugin_env(bad_home))

    assert result.returncode == 0
    assert_schema(response, success=False, changed=False)
    assert response["data"] == {}
    assert response["error"]
    assert "Failed to apply config" in result.stderr


def test_dry_run_corrupted_config_does_not_backup_or_write(tmp_path):
    env = plugin_env(tmp_path)
    config_file = fzfrc_path(tmp_path)
    original = 'export FZF_DEFAULT_OPTS="unterminated\n'
    config_file.write_text(original, encoding="utf-8")
    payload = {
        "requestId": "dry-corrupt-1",
        "command": "apply",
        "args": {"settings": {"FZF_DEFAULT_OPTS": "--height 80%"}},
        "context": {"dryRun": True},
    }

    response, result = run_plugin(payload, env=env)

    assert_schema(response, success=True, changed=True)
    assert response["data"]["corrupted"] is True
    assert config_file.read_text(encoding="utf-8") == original
    assert list(tmp_path.glob(f"{config_file.name}.bak.*")) == []
    assert "Would back up corrupted config" in result.stderr


def test_unknown_command(tmp_path):
    payload = {"requestId": "unknown-1", "command": "unknown_cmd"}

    response, _ = run_plugin(payload, env=plugin_env(tmp_path))

    assert_schema(response, success=False, changed=False)
    assert response["error"] == "Unknown command: unknown_cmd"
