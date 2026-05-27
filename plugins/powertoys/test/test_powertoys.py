import json
import os
import subprocess
import sys
import tempfile

# Resolve the main plugin path relative to the test file location
PLUGIN = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src", "main.py")
)


def run_plugin(payload: dict, env: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=env,
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

        res = run_plugin(
            {
                "requestId": "1",
                "command": "apply",
                "args": {"general": {"settings": {"theme": "light"}}},
                "context": {"dryRun": False},
            },
            env,
        )

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

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {
                    "fancyzones": {
                        "enabled": True,
                        "settings": {"shiftDrag": True},
                    }
                },
                "context": {"dryRun": False},
            },
            env,
        )

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
                "fancyzones": {"enabled": True, "settings": {"shiftDrag": True}}
            },
            "context": {"dryRun": False},
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

        res = run_plugin(
            {
                "requestId": "4",
                "command": "check_installed",
                "args": {"module": "fancyzones"},
                "context": {},
            },
            env,
        )
        assert res["success"]
        assert not res["data"]  # Doesn't exist yet

        # Write settings.json for fancyzones
        fz_path = os.path.join(pt_dir, "FancyZones", "settings.json")
        with open(fz_path, "w") as f:
            json.dump({}, f)

        res2 = run_plugin(
            {
                "requestId": "5",
                "command": "check_installed",
                "args": {"module": "fancyzones"},
                "context": {},
            },
            env,
        )
        assert res2["success"]
        assert res2["data"]  # Now exists
        print("OK check_installed")


def test_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = tmp
        pt_dir = setup_powertoys_dir(tmp)

        fz_path = os.path.join(pt_dir, "FancyZones", "settings.json")
        with open(fz_path, "w") as f:
            json.dump({"enabled": False}, f)

        res = run_plugin(
            {
                "requestId": "6",
                "command": "apply",
                "args": {"fancyzones": {"enabled": True}},
                "context": {"dryRun": True},
            },
            env,
        )

        assert res["success"], res
        assert not res["changed"]  # dry run: no actual write

        # File should be unchanged
        with open(fz_path) as f:
            data = json.load(f)
        assert data["enabled"] == False
        print("OK dry_run")


def test_unknown_module():
    with tempfile.TemporaryDirectory() as tmp:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = tmp
        setup_powertoys_dir(tmp)

        res = run_plugin(
            {
                "requestId": "7",
                "command": "apply",
                "args": {"modules": {"nonexistent_module": {"enabled": True}}},
                "context": {"dryRun": False},
            },
            env,
        )

        assert not res["success"]  # should fail for unknown module
        assert res["error"] is not None
        print("OK unknown_module")


def test_corrupt_json():
    with tempfile.TemporaryDirectory() as tmp:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = tmp
        pt_dir = setup_powertoys_dir(tmp)

        # Write corrupt JSON
        fz_path = os.path.join(pt_dir, "FancyZones", "settings.json")
        with open(fz_path, "w") as f:
            f.write("{ this is not valid json }")

        res = run_plugin(
            {
                "requestId": "8",
                "command": "apply",
                "args": {"fancyzones": {"enabled": True}},
                "context": {"dryRun": False},
            },
            env,
        )

        assert not res["success"]  # should fail, not silently overwrite
        assert res["error"] is not None
        print("OK corrupt_json")


def test_missing_localappdata():
    env = os.environ.copy()
    env["LOCALAPPDATA"] = ""  # unset

    res = run_plugin(
        {
            "requestId": "9",
            "command": "apply",
            "args": {"fancyzones": {"enabled": True}},
            "context": {"dryRun": False},
        },
        env,
    )

    assert not res["success"]
    assert res["error"] == "LOCALAPPDATA is not set."
    print("OK missing_localappdata")


if __name__ == "__main__":
    test_apply_general_settings()
    test_apply_module_settings()
    test_idempotent()
    test_check_installed()
    test_dry_run()
    test_unknown_module()
    test_corrupt_json()
    test_missing_localappdata()
    print("\nAll PowerToys tests passed.")
