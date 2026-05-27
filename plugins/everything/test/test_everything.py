import json
import subprocess
import sys


PLUGIN = "everything/src/plugin.py"


def run_plugin(payload: dict):
    p = subprocess.Popen(
        [sys.executable, PLUGIN],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True
    )

    out, err = p.communicate(json.dumps(payload))
    return out.strip()


def test_check_installed():
    out = run_plugin({"command": "check_installed", "args": {}})
    data = json.loads(out)
    assert "installed" in data


def test_apply_dry_run():
    payload = {
        "command": "apply",
        "args": {
            "dry_run": True,
            "args": {
                "general": {
                    "run_as_admin": True,
                    "show_in_taskbar": True
                },
                "search": {
                    "match_path": True,
                    "max_visible_results": 100
                }
            }
        }
    }

    out = run_plugin(payload)

    assert "success" in json.loads(out)