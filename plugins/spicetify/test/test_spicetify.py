import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))


def run_plugin(payload: dict) -> dict:
    result = subprocess.run([sys.executable, PLUGIN], input=json.dumps(payload), capture_output=True, text=True)

    return json.loads(result.stdout.strip())


def test_check_installed_absent():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin({"requestId": "1", "command": "check_installed", "args": {}, "context": {}})

        assert res["requestId"] == "1"
        assert res["success"]
        assert "data" in res


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {"settings": {"Setting": {"theme": "Catppuccin", "color_scheme": "mocha", "inject_css": True}}},
                "context": {"dryRun": True},
            }
        )

        config_path = os.path.join(tmp, ".spicetify", "config.ini")

        assert res["requestId"] == "2"
        assert res["success"]
        assert res["changed"]
        assert not os.path.exists(config_path)


def test_apply_config_creates_file():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "settings": {
                        "Setting": {"theme": "Catppuccin", "color_scheme": "mocha", "inject_css": True},
                        "AdditionalOptions": {"sidebar_config": "1"},
                    }
                },
                "context": {"dryRun": False},
            }
        )

        config_path = os.path.join(tmp, ".spicetify", "config.ini")

        assert res["requestId"] == "3"
        assert res["success"]
        assert res["changed"]
        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "[Setting]" in content
        assert "theme = Catppuccin" in content
        assert "color_scheme = mocha" in content
        assert "inject_css = 1" in content
        assert "[AdditionalOptions]" in content


def test_preserves_unknown_sections():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp
        config_dir = os.path.join(tmp, ".spicetify")
        os.makedirs(config_dir, exist_ok=True)
        config_path = os.path.join(config_dir, "config.ini")

        with open(config_path, "w", encoding="utf-8") as f:
            f.write("[CustomSection]\nunknown = keep\n")

        res = run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"settings": {"Setting": {"theme": "Dribbblish"}}},
                "context": {"dryRun": False},
            }
        )

        assert res["success"]

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "[CustomSection]" in content
        assert "unknown = keep" in content
        assert "[Setting]" in content
        assert "theme = Dribbblish" in content


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        payload = {
            "requestId": "5",
            "command": "apply",
            "args": {"settings": {"Setting": {"theme": "Catppuccin"}}},
            "context": {"dryRun": False},
        }

        first = run_plugin(payload)
        second = run_plugin(payload)

        assert first["success"]
        assert first["changed"]
        assert second["success"]
        assert not second["changed"]


def test_invalid_settings_returns_error():
    res = run_plugin({"requestId": "6", "command": "apply", "args": {"settings": None}, "context": {}})

    assert res["requestId"] == "6"
    assert not res["success"]
    assert "error" in res
    assert "data" in res
