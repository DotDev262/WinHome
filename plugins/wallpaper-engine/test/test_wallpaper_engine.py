import subprocess
import json


def run_plugin(payload):
    process = subprocess.run(
        ["python", "src/plugin.py"],
        input=json.dumps(payload),
        text=True,
        capture_output=True
    )

    if not process.stdout.strip():
        raise Exception(f"Plugin failed: {process.stderr}")

    return json.loads(process.stdout)


# ---------------- TESTS ----------------

def test_check_installed():
    result = run_plugin({
        "requestId": "1",
        "method": "check_installed",
        "params": {}
    })

    assert result["success"] in [True, False]
    assert "data" in result
    assert "installed" in result["data"]


def test_apply_dry_run():
    result = run_plugin({
        "requestId": "2",
        "method": "apply",
        "params": {
            "settings": {"volume": 0.5},
            "context": {"dryRun": True}
        }
    })

    assert "success" in result
    assert "data" in result
    assert "dryRun" in result["data"]
    assert result["data"]["dryRun"] is True


def test_apply_structure():
    result = run_plugin({
        "requestId": "3",
        "method": "apply",
        "params": {
            "settings": {"volume": 0.8},
            "context": {"dryRun": False}
        }
    })

    assert "success" in result
    assert "changed" in result
    assert "data" in result


def test_invalid_settings():
    result = run_plugin({
        "requestId": "4",
        "method": "apply",
        "params": {
            "settings": "invalid_string",
            "context": {"dryRun": True}
        }
    })

    assert result["success"] is False
    assert "error" in result