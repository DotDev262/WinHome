import subprocess
import json
import os
import tempfile
import shutil
import sys

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))

def run_plugin(payload: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True
    )
    return json.loads(result.stdout.strip())

def make_vault(base: str) -> str:
    vault = os.path.join(base, "TestVault")
    os.makedirs(os.path.join(vault, ".obsidian", "plugins"), exist_ok=True)
    return vault


# Tests 

def test_apply_settings():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        res = run_plugin({
            "requestId": "1",
            "command": "apply",
            "args": {
                "vaults": [{
                    "path": vault,
                    "settings": {"spellcheck": True, "accentColor": "#002aff"}
                }]
            },
            "context": {"dryRun": False}
        })
        assert res["success"], res
        assert res["changed"]

        app = json.loads(open(os.path.join(vault, ".obsidian", "app.json")).read())
        appearance = json.loads(open(os.path.join(vault, ".obsidian", "appearance.json")).read())
        assert app["spellcheck"] == True
        assert appearance["accentColor"] == "#002aff"
        print("✓ apply_settings")

def test_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        payload = {
            "requestId": "2",
            "command": "apply",
            "args": {"vaults": [{"path": vault, "settings": {"spellcheck": True}}]},
            "context": {"dryRun": False}
        }
        run_plugin(payload)
        res = run_plugin(payload)
        assert res["success"]
        assert not res["changed"]
        print("✓ idempotent")

def test_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        res = run_plugin({
            "requestId": "3",
            "command": "apply",
            "args": {"vaults": [{"path": vault, "settings": {"spellcheck": True}}]},
            "context": {"dryRun": True}
        })
        assert res["success"]
        assert not res["changed"]
        assert not os.path.exists(os.path.join(vault, ".obsidian", "app.json"))
        print("✓ dry_run")

def test_check_installed_false():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        res = run_plugin({
            "requestId": "4",
            "command": "check_installed",
            "args": {"vaultPath": vault, "pluginId": "obsidian-git"},
            "context": {}
        })
        assert res["success"]
        assert not res["data"]
        print("✓ check_installed_false")

def test_nonexistent_vault():
    res = run_plugin({
        "requestId": "5",
        "command": "apply",
        "args": {"vaults": [{"path": "C:\\DoesNotExist\\Vault", "settings": {"spellcheck": True}}]},
        "context": {"dryRun": False}
    })
    assert res["success"]
    assert not res["changed"]
    print("✓ nonexistent_vault")

def test_unknown_command():
    res = run_plugin({
        "requestId": "6",
        "command": "explode",
        "args": {},
        "context": {}
    })
    assert not res["success"]
    assert "error" in res
    print("✓ unknown_command")

def test_install_plugin():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        res = run_plugin({
            "requestId": "7",
            "command": "install",
            "args": {"vaultPath": vault, "pluginId": "obsidian-git"},
            "context": {"dryRun": False}
        })
        assert res["success"], res
        assert res["changed"]

        plugin_dir = os.path.join(vault, ".obsidian", "plugins", "obsidian-git")
        assert os.path.exists(os.path.join(plugin_dir, "main.js"))
        assert os.path.exists(os.path.join(plugin_dir, "manifest.json"))

        enabled = json.loads(open(os.path.join(vault, ".obsidian", "community-plugins.json")).read())
        assert "obsidian-git" in enabled
        print("✓ install_plugin")

def test_install_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        run_plugin({
            "requestId": "8",
            "command": "install",
            "args": {"vaultPath": vault, "pluginId": "obsidian-git"},
            "context": {"dryRun": False}
        })
        res = run_plugin({
            "requestId": "9",
            "command": "install",
            "args": {"vaultPath": vault, "pluginId": "obsidian-git"},
            "context": {"dryRun": False}
        })
        assert res["success"]
        assert not res["changed"]
        print("✓ install_idempotent")

def test_uninstall_plugin():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        run_plugin({
            "requestId": "10",
            "command": "install",
            "args": {"vaultPath": vault, "pluginId": "obsidian-git"},
            "context": {"dryRun": False}
        })
        res = run_plugin({
            "requestId": "11",
            "command": "uninstall",
            "args": {"vaultPath": vault, "pluginId": "obsidian-git"},
            "context": {"dryRun": False}
        })
        assert res["success"]
        assert res["changed"]

        plugin_dir = os.path.join(vault, ".obsidian", "plugins", "obsidian-git")
        assert not os.path.exists(plugin_dir)

        enabled = json.loads(open(os.path.join(vault, ".obsidian", "community-plugins.json")).read())
        assert "obsidian-git" not in enabled
        print("✓ uninstall_plugin")

def test_install_fake_plugin():
    with tempfile.TemporaryDirectory() as tmp:
        vault = make_vault(tmp)
        res = run_plugin({
            "requestId": "12",
            "command": "install",
            "args": {"vaultPath": vault, "pluginId": "this-plugin-does-not-exist-xyz"},
            "context": {"dryRun": False}
        })
        assert not res["success"]
        assert "error" in res
        print("✓ install_fake_plugin")

if __name__ == "__main__":
    test_apply_settings()
    test_idempotent()
    test_dry_run()
    test_check_installed_false()
    test_nonexistent_vault()
    test_unknown_command()
    test_install_plugin()
    test_install_idempotent()
    test_uninstall_plugin()
    test_install_fake_plugin()
    print("\nAll tests passed.")