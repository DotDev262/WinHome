import json
import os
import subprocess
import sys
from pathlib import Path

PLUGIN = Path(__file__).resolve().parents[1] / "src" / "plugin.py"


def run_plugin(payload: dict, env: dict | None = None) -> dict:
    result = subprocess.run(
        [sys.executable, str(PLUGIN)],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=env,
        check=False,
    )

    assert result.stdout.strip(), result.stderr
    return json.loads(result.stdout.strip())


def make_env(tmp_path: Path, extra_path: Path | None = None) -> dict:
    env = os.environ.copy()
    env["APPDATA"] = str(tmp_path / "AppData" / "Roaming")
    env["USERPROFILE"] = str(tmp_path)
    if extra_path is not None:
        env["PATH"] = str(extra_path)
    else:
        env["PATH"] = ""
    return env


def test_check_installed_present_by_settings_file(tmp_path):
    settings_path = tmp_path / "AppData" / "Roaming" / "nvm" / "settings.txt"
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    settings_path.write_text("root=C:\\nvm\n", encoding="utf-8")

    response = run_plugin(
        {
            "requestId": "req-1",
            "command": "check_installed",
            "args": {},
            "context": {},
        },
        env=make_env(tmp_path),
    )

    assert response["requestId"] == "req-1"
    assert response["success"] is True
    assert response["changed"] is False
    assert response["data"] is True


def test_check_installed_present_by_executable(tmp_path):
    bin_dir = tmp_path / "bin"
    bin_dir.mkdir()
    exe = bin_dir / ("nvm.exe" if os.name == "nt" else "nvm")
    exe.write_text("", encoding="utf-8")
    exe.chmod(0o755)

    response = run_plugin(
        {
            "requestId": "req-2",
            "command": "check_installed",
            "args": {},
            "context": {},
        },
        env=make_env(tmp_path, bin_dir),
    )

    assert response["success"] is True
    assert response["data"] is True


def test_check_installed_absent(tmp_path):
    response = run_plugin(
        {
            "requestId": "req-3",
            "command": "check_installed",
            "args": {},
            "context": {},
        },
        env=make_env(tmp_path),
    )

    assert response["success"] is True
    assert response["data"] is False


def test_apply_merges_and_preserves_comments(tmp_path):
    env = make_env(tmp_path)
    settings_path = tmp_path / "AppData" / "Roaming" / "nvm" / "settings.txt"
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    settings_path.write_text(
        "# nvm settings\nroot = C:\\nvm\npath=C:\\node\n; keep this comment\n",
        encoding="utf-8",
    )

    response = run_plugin(
        {
            "requestId": "req-4",
            "command": "apply",
            "args": {"settings": {"root": "D:\\tools\\nvm", "arch": "64"}},
            "context": {"dryRun": False},
        },
        env=env,
    )

    assert response["success"] is True
    assert response["changed"] is True
    assert response["data"]["path"] == str(settings_path)

    content = settings_path.read_text(encoding="utf-8")
    assert "# nvm settings\n" in content
    assert "root = D:\\tools\\nvm\n" in content
    assert "path=C:\\node\n" in content
    assert "; keep this comment\n" in content
    assert content.endswith("\n")


def test_apply_preserves_inline_comment_suffix(tmp_path):
    env = make_env(tmp_path)
    settings_path = tmp_path / "AppData" / "Roaming" / "nvm" / "settings.txt"
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    settings_path.write_text("root=C:\\nvm  # install directory\n", encoding="utf-8")

    response = run_plugin(
        {
            "requestId": "req-4b",
            "command": "apply",
            "args": {"settings": {"root": "D:\\tools\\nvm"}},
            "context": {"dryRun": False},
        },
        env=env,
    )

    assert response["success"] is True
    assert response["changed"] is True
    assert settings_path.read_text(encoding="utf-8") == ("root=D:\\tools\\nvm  # install directory\n")


def test_apply_dry_run_does_not_write(tmp_path):
    env = make_env(tmp_path)
    settings_path = tmp_path / "AppData" / "Roaming" / "nvm" / "settings.txt"
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    original = "root=C:\\nvm\npath=C:\\node\n"
    settings_path.write_text(original, encoding="utf-8")

    response = run_plugin(
        {
            "requestId": "req-5",
            "command": "apply",
            "args": {"settings": {"path": "D:\\node"}},
            "context": {"dryRun": True},
        },
        env=env,
    )

    assert response["success"] is True
    assert response["changed"] is True
    assert settings_path.read_text(encoding="utf-8") == original


def test_apply_creates_missing_file(tmp_path):
    env = make_env(tmp_path)

    response = run_plugin(
        {
            "requestId": "req-6",
            "command": "apply",
            "args": {
                "settings": {
                    "root": "C:\\nvm",
                    "path": "C:\\Program Files\\nodejs",
                    "npm_mirror": "https://registry.npmjs.org/",
                }
            },
            "context": {"dryRun": False},
        },
        env=env,
    )

    settings_path = tmp_path / "AppData" / "Roaming" / "nvm" / "settings.txt"

    assert response["success"] is True
    assert response["changed"] is True
    assert settings_path.exists()
    assert settings_path.read_text(encoding="utf-8") == (
        "root=C:\\nvm\npath=C:\\Program Files\\nodejs\nnpm_mirror=https://registry.npmjs.org/\n"
    )


def test_apply_idempotent_when_no_diff(tmp_path):
    env = make_env(tmp_path)
    settings_path = tmp_path / "AppData" / "Roaming" / "nvm" / "settings.txt"
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    settings_path.write_text("root=C:\\nvm\npath=C:\\node\n", encoding="utf-8")

    payload = {
        "requestId": "req-7",
        "command": "apply",
        "args": {"settings": {"root": "C:\\nvm", "path": "C:\\node"}},
        "context": {"dryRun": False},
    }

    first = run_plugin(payload, env=env)
    second = run_plugin(payload, env=env)

    assert first["success"] is True
    assert first["changed"] is False
    assert second["success"] is True
    assert second["changed"] is False
    assert settings_path.read_text(encoding="utf-8") == "root=C:\\nvm\npath=C:\\node\n"


def test_apply_no_trailing_newline_in_existing_file(tmp_path):
    env = make_env(tmp_path)
    settings_path = tmp_path / "AppData" / "Roaming" / "nvm" / "settings.txt"
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    settings_path.write_text("root=C:\\nvm", encoding="utf-8")

    response = run_plugin(
        {
            "requestId": "req-8",
            "command": "apply",
            "args": {"settings": {"arch": "64"}},
            "context": {"dryRun": False},
        },
        env=env,
    )

    content = settings_path.read_text(encoding="utf-8")
    assert response["success"] is True
    assert response["changed"] is True
    assert "root=C:\\nvm\n" in content
    assert "arch=64\n" in content
