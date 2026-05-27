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
        "plugin.py",
    )
)


def run_plugin(payload: dict, env: dict | None = None) -> dict:
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

    if result.returncode != 0:
        print(f"Error output: {result.stderr}")

    return json.loads(result.stdout.strip())


def run_plugin_raw(input_data: str) -> tuple[int, dict]:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=input_data,
        capture_output=True,
        text=True,
    )

    return result.returncode, json.loads(result.stdout.strip())


def get_settings_path(appdata: str) -> str:
    return os.path.join(appdata, "topgrade.toml")


def test_check_installed():
    res = run_plugin(
        {
            "requestId": "1",
            "command": "check_installed",
            "args": {},
            "context": {},
        }
    )

    assert res["requestId"] == "1"
    assert res["success"] is True
    assert res["changed"] is False
    assert isinstance(res["data"]["installed"], bool)
    print("OK: test_check_installed")


def test_apply_creates_file():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "2",
            "command": "apply",
            "args": {
                "settings": {
                    "disable": ["pip", "npm"],
                    "set_title": True,
                    "display_time": True,
                    "git_repos": {"~/Projects/dotfiles": "main"},
                }
            },
            "context": {"dryRun": False},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True

        config_path = get_settings_path(tmp)
        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            lines = f.read().splitlines()

        assert 'disable = ["pip", "npm"]' in lines
        assert "set_title = true" in lines
        assert "display_time = true" in lines
        assert "[git_repos]" in lines
        assert '"~/Projects/dotfiles" = "main"' in lines
        print("OK: test_apply_creates_file")


def test_apply_merges_existing():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = get_settings_path(tmp)
        with open(config_path, "w", encoding="utf-8") as f:
            f.write(
                'existing_key = "existing_val"\n[git_repos]\n"~/Projects/old" = "dev"\n'
            )

        payload = {
            "requestId": "3",
            "command": "apply",
            "args": {
                "settings": {
                    "disable": ["pip"],
                    "git_repos": {"~/Projects/dotfiles": "main"},
                }
            },
            "context": {"dryRun": False},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert 'existing_key = "existing_val"' in content
        assert 'disable = ["pip"]' in content
        assert "[git_repos]" in content
        assert '"~/Projects/old" = "dev"' in content
        assert '"~/Projects/dotfiles" = "main"' in content
        print("OK: test_apply_merges_existing")


def test_dry_run_does_not_create():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "4",
            "command": "apply",
            "args": {"settings": {"disable": ["pip"]}},
            "context": {"dryRun": True},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True
        assert not os.path.exists(get_settings_path(tmp))
        print("OK: test_dry_run_does_not_create")


def test_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "5",
            "command": "apply",
            "args": {
                "settings": {
                    "disable": ["pip", "npm"],
                    "git_repos": {"~/Projects/dotfiles": "main"},
                }
            },
            "context": {"dryRun": False},
        }
        res1 = run_plugin(payload, {"APPDATA": tmp})
        assert res1["success"] is True
        assert res1["changed"] is True

        res2 = run_plugin(payload, {"APPDATA": tmp})
        assert res2["success"] is True
        assert res2["changed"] is False
        print("OK: test_idempotent")


def test_corrupt_toml_backup():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = get_settings_path(tmp)
        with open(config_path, "w", encoding="utf-8") as f:
            f.write("this is not valid toml = [")

        payload = {
            "requestId": "6",
            "command": "apply",
            "args": {"settings": {"disable": ["pip"]}},
            "context": {"dryRun": False},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True

        backups = [f for f in os.listdir(tmp) if f.endswith(".bak")]
        assert len(backups) == 1
        with open(os.path.join(tmp, backups[0]), "r") as f:
            assert f.read() == "this is not valid toml = ["

        with open(config_path, "r") as f:
            assert 'disable = ["pip"]' in f.read()
        print("OK: test_corrupt_toml_backup")


def test_missing_appdata():
    payload = {
        "requestId": "7",
        "command": "apply",
        "args": {"settings": {"disable": ["pip"]}},
        "context": {"dryRun": False},
    }
    res = run_plugin(payload, {"APPDATA": ""})
    assert res["success"] is False
    assert "APPDATA environment variable not found" in res["error"]
    print("OK: test_missing_appdata")


if __name__ == "__main__":
    test_check_installed()
    test_apply_creates_file()
    test_apply_merges_existing()
    test_dry_run_does_not_create()
    test_idempotent()
    test_corrupt_toml_backup()
    test_missing_appdata()
    print("\nAll Topgrade tests passed.")
