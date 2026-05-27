import json
import os
import subprocess
import sys
import tempfile
from typing import Optional, Tuple


PLUGIN = os.path.abspath(
    os.path.join(
        os.path.dirname(__file__),
        "..",
        "src",
        "plugin.py",
    )
)


def run_plugin(payload: dict, env: Optional[dict] = None) -> Tuple[dict, str]:
    merged_env = os.environ.copy()
    if env:
        merged_env.update(env)

    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=merged_env,
        check=True,
    )

    return json.loads(result.stdout.strip()), result.stderr


def preferences_path(appdata: str) -> str:
    return os.path.join(
        appdata,
        "Sublime Text",
        "Packages",
        "User",
        "Preferences.sublime-settings",
    )


def test_check_installed_reports_boolean():
    res, _stderr = run_plugin(
        {
            "requestId": "1",
            "command": "check_installed",
            "args": {},
            "context": {},
        }
    )

    assert res["success"]
    assert res["changed"] is False
    assert isinstance(res["data"]["installed"], bool)


def test_apply_merges_existing_preferences():
    with tempfile.TemporaryDirectory() as tmp:
        pref_path = preferences_path(tmp)
        os.makedirs(os.path.dirname(pref_path), exist_ok=True)

        with open(pref_path, "w", encoding="utf-8") as f:
            json.dump(
                {
                    "theme": "Default.sublime-theme",
                    "font_size": 10,
                    "nested": {"keep": True, "replace": "old"},
                    "unchanged": "preserved",
                },
                f,
            )

        res, _stderr = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {
                    "theme": "Adaptive.sublime-theme",
                    "font_size": 12,
                    "nested": {"replace": "new", "added": True},
                    "ignored_packages": ["Vintage"],
                },
                "context": {"dryRun": False},
            },
            {"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]

        with open(pref_path, "r", encoding="utf-8") as f:
            config = json.load(f)

        assert config["theme"] == "Adaptive.sublime-theme"
        assert config["font_size"] == 12
        assert config["unchanged"] == "preserved"
        assert config["nested"] == {
            "keep": True,
            "replace": "new",
            "added": True,
        }
        assert config["ignored_packages"] == ["Vintage"]


def test_apply_supports_issue_input_format():
    with tempfile.TemporaryDirectory() as tmp:
        res, _stderr = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "color_scheme": (
                        "Packages/Theme - Monokai Pro/"
                        "Monokai Pro.sublime-color-scheme"
                    ),
                    "theme": "Adaptive.sublime-theme",
                    "font_size": 12,
                    "font_face": "JetBrains Mono",
                    "tab_size": 4,
                    "translate_tabs_to_spaces": True,
                    "word_wrap": True,
                    "highlight_line": True,
                    "save_on_focus_lost": True,
                    "ignored_packages": ["Vintage"],
                    "rulers": [80, 120],
                },
                "context": {},
            },
            {"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]

        with open(preferences_path(tmp), "r", encoding="utf-8") as f:
            config = json.load(f)

        assert config["font_face"] == "JetBrains Mono"
        assert config["translate_tabs_to_spaces"] is True
        assert config["rulers"] == [80, 120]


def test_dry_run_logs_and_does_not_write():
    with tempfile.TemporaryDirectory() as tmp:
        res, stderr = run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"theme": "Adaptive.sublime-theme"},
                "context": {"dryRun": True},
            },
            {"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]
        assert res["data"]["dryRun"] is True
        assert "Dry run: would update" in stderr
        assert not os.path.exists(preferences_path(tmp))


def test_apply_creates_missing_directories():
    with tempfile.TemporaryDirectory() as tmp:
        pref_path = preferences_path(tmp)

        res, _stderr = run_plugin(
            {
                "requestId": "5",
                "command": "apply",
                "args": {"word_wrap": True},
                "context": {},
            },
            {"APPDATA": tmp},
        )

        assert res["success"]
        assert res["changed"]
        assert os.path.exists(pref_path)


def test_idempotent_apply_reports_unchanged():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "6",
            "command": "apply",
            "args": {"theme": "Adaptive.sublime-theme"},
            "context": {},
        }

        first, _stderr = run_plugin(payload, {"APPDATA": tmp})
        second, _stderr = run_plugin(payload, {"APPDATA": tmp})

        assert first["success"]
        assert first["changed"]
        assert second["success"]
        assert second["changed"] is False


def test_unknown_command_returns_error():
    res, _stderr = run_plugin(
        {
            "requestId": "7",
            "command": "explode",
            "args": {},
            "context": {},
        }
    )

    assert res["success"] is False
    assert res["changed"] is False
    assert "Unknown command" in res["error"]


if __name__ == "__main__":
    test_check_installed_reports_boolean()
    test_apply_merges_existing_preferences()
    test_apply_supports_issue_input_format()
    test_dry_run_logs_and_does_not_write()
    test_apply_creates_missing_directories()
    test_idempotent_apply_reports_unchanged()
    test_unknown_command_returns_error()

    print("\nAll tests passed.")
