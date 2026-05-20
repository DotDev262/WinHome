import subprocess
import json
import os
import tempfile
import sys

# Resolve the main plugin path relative to the test file location
PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "main.py"))

def run_plugin(payload: dict, env: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=env
    )
    return json.loads(result.stdout.strip())

def setup_powertoys_dir(base: str):
    pt_dir = os.path.join(base, "Microsoft", "PowerToys")
    os.makedirs(pt_dir, exist_ok=True)
    os.makedirs(os.path.join(pt_dir, "FancyZones"), exist_ok=True)
    os.makedirs(os.path.join(pt_dir, "Awake"), exist_ok=True)
    os.makedirs(os.path.join(pt_dir, "PowerRename"), exist_ok=True)
    return pt_dir

def test_apply_general_settings():
    with tempfile.TemporaryDirectory() as tmp:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = tmp
        pt_dir = setup_powertoys_dir(tmp)

        # Write initial settings.json
        gen_path = os.path.join(pt_dir, "settings.json")
        with open(gen_path, "w") as f:
            json.dump({"theme": "dark", "startup": True}, f)

        res = run_plugin({
            "requestId": "1",
            "command": "apply",
            "args": {
                "general": {
                    "settings": {
                        "theme": "light"
                    }
                }
            },
            "context": {"dryRun": False}
        }, env)

        assert res["success"], res
        assert res["changed"]

        with open(gen_path) as f:
            data = json.load(f)
        assert data["theme"] == "light"
        assert data["startup"] == True
        print("OK apply_general_settings")

def test_apply_module_settings():
    with tempfile.TemporaryDirectory() as tmp:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = tmp
        pt_dir = setup_powertoys_dir(tmp)

        fz_path = os.path.join(pt_dir, "FancyZones", "settings.json")
        with open(fz_path, "w") as f:
            json.dump({"enabled": False, "properties": {"shiftDrag": False}}, f)

        res = run_plugin({
            "requestId": "2",
            "command": "apply",
            "args": {
                "fancyzones": {
                    "enabled": True,
                    "settings": {
                        "shiftDrag": True
                    }
                }
            },
            "context": {"dryRun": False}
        }, env)

        assert res["success"], res
        assert res["changed"]

        with open(fz_path) as f:
            data = json.load(f)
        assert data["enabled"] == True
        assert data["properties"]["shiftDrag"] == True
        print("OK apply_module_settings")

def test_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = tmp
        pt_dir = setup_powertoys_dir(tmp)

        fz_path = os.path.join(pt_dir, "FancyZones", "settings.json")
        with open(fz_path, "w") as f:
            json.dump({"enabled": False, "properties": {"shiftDrag": False}}, f)

        payload = {
            "requestId": "3",
            "command": "apply",
            "args": {
                "fancyzones": {
                    "enabled": True,
                    "settings": {
                        "shiftDrag": True
                    }
                }
            },
            "context": {"dryRun": False}
        }

        # First run: should change
        res1 = run_plugin(payload, env)
        assert res1["success"]
        assert res1["changed"]

        # Second run: should not change
        res2 = run_plugin(payload, env)
        assert res2["success"]
        assert not res2["changed"]
        print("OK idempotent")

def test_check_installed():
    with tempfile.TemporaryDirectory() as tmp:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = tmp
        pt_dir = setup_powertoys_dir(tmp)

        res = run_plugin({
            "requestId": "4",
            "command": "check_installed",
            "args": {"module": "fancyzones"},
            "context": {}
        }, env)
        assert res["success"]
        assert not res["data"] # Doesn't exist yet

        # Write setting.json for fancyzones
        fz_path = os.path.join(pt_dir, "FancyZones", "settings.json")
        with open(fz_path, "w") as f:
            json.dump({}, f)

        res2 = run_plugin({
            "requestId": "5",
            "command": "check_installed",
            "args": {"module": "fancyzones"},
            "context": {}
        }, env)
        assert res2["success"]
        assert res2["data"] # Now exists
        print("OK check_installed")

if __name__ == "__main__":
    test_apply_general_settings()
    test_apply_module_settings()
    test_idempotent()
    test_check_installed()
    print("\nAll PowerToys tests passed.")
