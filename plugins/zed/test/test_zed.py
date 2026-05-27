import json
import os
import subprocess
import sys
import tempfile
from contextlib import contextmanager
from unittest import mock


PLUGIN = os.path.abspath(
    os.path.join(
        os.path.dirname(__file__),
        "..",
        "src",
        "plugin.py",
    )
)


def run_plugin(payload: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
    )

    assert result.returncode == 0, result.stderr
    return json.loads(result.stdout.strip())


def read_settings(appdata: str) -> dict:
    settings_path = os.path.join(appdata, "Zed", "settings.json")
    with open(settings_path, "r", encoding="utf-8") as settings_file:
        return json.load(settings_file)


@contextmanager
def temp_appdata():
    with tempfile.TemporaryDirectory() as tmp:
        with mock.patch.dict(os.environ, {"APPDATA": tmp}):
            yield tmp


def test_check_installed():
    res = run_plugin({
        "requestId": "1",
        "command": "check_installed",
        "args": {},
        "context": {},
    })

    assert res["success"]
    assert not res["changed"]
    assert isinstance(res["data"], bool)


def test_apply_creates_missing_directory_and_file():
    with temp_appdata() as tmp:
        res = run_plugin({
            "requestId": "2",
            "command": "apply",
            "args": {
                "theme": {
                    "mode": "system",
                    "light": "One Light",
                    "dark": "One Dark",
                },
                "buffer_font_family": "JetBrains Mono",
                "buffer_font_size": 14,
                "tab_size": 2,
                "format_on_save": "on",
                "vim_mode": False,
                "relative_line_numbers": True,
                "soft_wrap": "editor_width",
                "cursor_shape": "bar",
                "terminal": {
                    "font_family": "JetBrains Mono",
                    "font_size": 14,
                    "shell": "powershell.json",
                },
                "languages": {
                    "Python": {
                        "tab_size": 4,
                    },
                    "JavaScript": {
                        "tab_size": 2,
                    },
                },
                "features": {
                    "copilot": True,
                },
                "auto_install_extensions": {
                    "html": True,
                    "dockerfile": True,
                },
            },
            "context": {
                "dryRun": False,
            },
        })

        assert res["success"]
        assert res["changed"]

        settings = read_settings(tmp)
        assert settings["theme"]["dark"] == "One Dark"
        assert settings["buffer_font_family"] == "JetBrains Mono"
        assert settings["languages"]["Python"]["tab_size"] == 4
        assert settings["features"]["copilot"] is True


def test_apply_deep_merges_existing_commented_settings():
    with temp_appdata() as tmp:
        zed_dir = os.path.join(tmp, "Zed")
        os.makedirs(zed_dir)
        settings_path = os.path.join(zed_dir, "settings.json")

        with open(settings_path, "w", encoding="utf-8") as settings_file:
            settings_file.write(
                """
{
  // Existing comments should not prevent parsing.
  "theme": {
    "mode": "dark",
    "dark": "Ayu Dark"
  },
  "languages": {
    "Python": {
      "formatter": "ruff",
      "tab_size": 2
    }
  },
  "project_panel": {
    "dock": "left"
  }
}
"""
            )

        res = run_plugin({
            "requestId": "3",
            "command": "apply",
            "args": {
                "theme": {
                    "mode": "system",
                    "light": "One Light",
                },
                "languages": {
                    "Python": {
                        "tab_size": 4,
                    },
                },
            },
            "context": {
                "dryRun": False,
            },
        })

        assert res["success"]
        assert res["changed"]

        settings = read_settings(tmp)
        assert settings["theme"]["mode"] == "system"
        assert settings["theme"]["light"] == "One Light"
        assert settings["theme"]["dark"] == "Ayu Dark"
        assert settings["languages"]["Python"]["formatter"] == "ruff"
        assert settings["languages"]["Python"]["tab_size"] == 4
        assert settings["project_panel"]["dock"] == "left"


def test_apply_backs_up_corrupt_settings_before_replacing():
    with temp_appdata() as tmp:
        zed_dir = os.path.join(tmp, "Zed")
        os.makedirs(zed_dir)
        settings_path = os.path.join(zed_dir, "settings.json")

        with open(settings_path, "w", encoding="utf-8") as settings_file:
            settings_file.write("{ invalid json")

        res = run_plugin({
            "requestId": "4",
            "command": "apply",
            "args": {
                "tab_size": 2,
            },
            "context": {
                "dryRun": False,
            },
        })

        backups = [
            name
            for name in os.listdir(zed_dir)
            if name.startswith("settings.json.corrupt-") and name.endswith(".bak")
        ]

        assert res["success"]
        assert res["changed"]
        assert len(backups) == 1
        assert read_settings(tmp)["tab_size"] == 2


def test_dry_run_reports_change_without_writing():
    with temp_appdata() as tmp:
        zed_dir = os.path.join(tmp, "Zed")
        os.makedirs(zed_dir)
        settings_path = os.path.join(zed_dir, "settings.json")

        with open(settings_path, "w", encoding="utf-8") as settings_file:
            json.dump({"tab_size": 4}, settings_file)

        res = run_plugin({
            "requestId": "5",
            "command": "apply",
            "args": {
                "tab_size": 2,
            },
            "context": {
                "dryRun": True,
            },
        })

        assert res["success"]
        assert res["changed"]
        assert read_settings(tmp)["tab_size"] == 4


def test_idempotent_apply():
    with temp_appdata() as tmp:
        payload = {
            "requestId": "6",
            "command": "apply",
            "args": {
                "settings": {
                    "vim_mode": False,
                },
            },
            "context": {
                "dryRun": False,
            },
        }

        first = run_plugin(payload)
        second = run_plugin(payload)

        assert first["success"]
        assert first["changed"]
        assert second["success"]
        assert not second["changed"]


def test_apply_normalizes_string_scalars_from_host():
    with temp_appdata() as tmp:
        res = run_plugin({
            "requestId": "7",
            "command": "apply",
            "args": {
                "tab_size": "2",
                "vim_mode": "false",
                "buffer_font_size": "14",
                "features": {
                    "copilot": "true",
                },
            },
            "context": {
                "dryRun": False,
            },
        })

        assert res["success"]
        assert res["changed"]

        settings = read_settings(tmp)
        assert settings["tab_size"] == 2
        assert settings["vim_mode"] is False
        assert settings["buffer_font_size"] == 14
        assert settings["features"]["copilot"] is True


def test_top_level_dry_run_and_config_protocol():
    with tempfile.TemporaryDirectory() as tmp:
        settings_path = os.path.join(tmp, "settings.json")

        res = run_plugin({
            "requestId": "8",
            "command": "apply",
            "dryRun": True,
            "config": {
                "configPath": settings_path,
                "tab_size": 8,
            },
        })

        assert res["success"]
        assert res["changed"]
        assert not os.path.exists(settings_path)


def test_unknown_command():
    res = run_plugin({
        "requestId": "9",
        "command": "explode",
        "args": {},
        "context": {},
    })

    assert not res["success"]
    assert "error" in res


if __name__ == "__main__":
    test_check_installed()
    test_apply_creates_missing_directory_and_file()
    test_apply_deep_merges_existing_commented_settings()
    test_apply_backs_up_corrupt_settings_before_replacing()
    test_dry_run_reports_change_without_writing()
    test_idempotent_apply()
    test_apply_normalizes_string_scalars_from_host()
    test_top_level_dry_run_and_config_protocol()
    test_unknown_command()

    print("\nAll tests passed.")
