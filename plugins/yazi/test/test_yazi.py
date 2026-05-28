import json
import os
import subprocess
import sys
import tempfile

try:
    import tomllib
except ImportError:
    tomllib = None

PLUGIN = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py")
)


def run_plugin(payload: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
    )

    return json.loads(result.stdout.strip())


def read_toml(path: str) -> dict:
    if tomllib is None:
        raise AssertionError("tomllib is required for tests")

    with open(path, "rb") as f:
        return tomllib.load(f)


def test_check_installed():
    res = run_plugin(
        {
            "requestId": "1",
            "command": "check_installed",
            "args": {},
            "context": {},
        }
    )

    assert res["success"]
    assert "data" in res
    assert "installed" in res["data"]

    print("✓ check_installed")


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {"manager": {"show_hidden": True}},
                "context": {"dryRun": True},
            }
        )

        assert res["success"]
        assert res["changed"]
        assert res["data"]["wouldChange"] is True

        config_path = os.path.join(tmp, "yazi", "config", "yazi.toml")

        assert not os.path.exists(config_path)

        print("✓ apply_config_dry_run")


def test_apply_config_routing_and_write():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "manager": {"show_hidden": True},
                    "keymap": {
                        "prepend_keymap": [
                            {"on": ["g", "h"], "run": "cd ~", "desc": "Go home"}
                        ]
                    },
                    "theme": {"type": "catppuccin-mocha"},
                },
                "context": {"dryRun": False},
            }
        )

        assert res["success"]
        assert res["changed"]

        config_root = os.path.join(tmp, "yazi", "config")
        yazi_path = os.path.join(config_root, "yazi.toml")
        keymap_path = os.path.join(config_root, "keymap.toml")
        theme_path = os.path.join(config_root, "theme.toml")

        assert os.path.exists(yazi_path)
        assert os.path.exists(keymap_path)
        assert os.path.exists(theme_path)

        yazi_data = read_toml(yazi_path)
        keymap_data = read_toml(keymap_path)
        theme_data = read_toml(theme_path)

        assert yazi_data["manager"]["show_hidden"] is True
        assert keymap_data["keymap"]["prepend_keymap"][0]["run"] == "cd ~"
        assert theme_data["theme"]["type"] == "catppuccin-mocha"

        print("✓ apply_config_routing_and_write")


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        payload = {
            "requestId": "4",
            "command": "apply",
            "args": {"manager": {"show_hidden": True}},
            "context": {"dryRun": False},
        }

        run_plugin(payload)

        res = run_plugin(payload)

        assert res["success"]
        assert not res["changed"]
        assert res.get("data") is None

        print("✓ idempotent_apply")


def test_unknown_command():
    res = run_plugin(
        {"requestId": "5", "command": "explode", "args": {}, "context": {}}
    )

    assert not res["success"]
    assert "error" in res

    print("✓ unknown_command")


if __name__ == "__main__":
    test_check_installed()
    test_apply_config_dry_run()
    test_apply_config_routing_and_write()
    test_idempotent_apply()
    test_unknown_command()

    print("\nAll tests passed.")
