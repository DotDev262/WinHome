import subprocess
import json
import os
import tempfile
import sys

# Compute dynamic path to the oh-my-posh plugin script
PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))

def run_plugin(payload: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True
    )
    if result.returncode != 0:
        print(f"CRASH ERROR:\n{result.stderr}", file=sys.stderr)
        sys.exit(result.returncode)
    return json.loads(result.stdout.strip())

def read_file(path: str) -> str:
    with open(path, "r", encoding="utf-8") as f:
        return f.read()

def write_file(path: str, content: str) -> None:
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

# Tests 

def test_apply_fresh_install():
    with tempfile.TemporaryDirectory() as tmp:
        profile = os.path.join(tmp, "Microsoft.PowerShell_profile.ps1")
        
        res = run_plugin({
            "requestId": "1",
            "command": "apply",
            "args": {"theme": "tokyonight_storm", "profile": profile},
            "context": {"dryRun": False}
        })
        
        assert res["success"], res
        assert res["changed"]
        
        content = read_file(profile)
        assert "# OH-MY-POSH-PLUGIN BEGIN" in content
        assert 'oh-my-posh init pwsh --config "tokyonight_storm" | Invoke-Expression' in content
        assert "# OH-MY-POSH-PLUGIN END" in content
        print("✓ test_apply_fresh_install")

def test_apply_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        profile = os.path.join(tmp, "Microsoft.PowerShell_profile.ps1")
        payload = {
            "requestId": "2",
            "command": "apply",
            "args": {"theme": "paradox", "profile": profile},
            "context": {"dryRun": False}
        }
        
        run_plugin(payload)
        
        res = run_plugin(payload)
        assert res["success"]
        assert not res["changed"]  
        print("✓ test_apply_idempotent")

def test_apply_update_theme():
    with tempfile.TemporaryDirectory() as tmp:
        profile = os.path.join(tmp, "Microsoft.PowerShell_profile.ps1")
        
        run_plugin({
            "requestId": "3a",
            "command": "apply",
            "args": {"theme": "chips", "profile": profile},
            "context": {"dryRun": False}
        })
        
        res = run_plugin({
            "requestId": "3b",
            "command": "apply",
            "args": {"theme": "jandedobbeleer", "profile": profile},
            "context": {"dryRun": False}
        })
        
        assert res["success"]
        assert res["changed"]
        
        content = read_file(profile)
        assert "chips" not in content
        assert 'oh-my-posh init pwsh --config "jandedobbeleer"' in content
        print("✓ test_apply_update_theme")

def test_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        profile = os.path.join(tmp, "Microsoft.PowerShell_profile.ps1")
        
        res = run_plugin({
            "requestId": "4",
            "command": "apply",
            "args": {"theme": "bubbles", "profile": profile},
            "context": {"dryRun": True}
        })
        
        assert res["success"]
        assert res["changed"]
        assert not os.path.exists(profile)
        print("✓ test_dry_run")

def test_dry_run_theme_swap():
    with tempfile.TemporaryDirectory() as tmp:
        profile = os.path.join(tmp, "Microsoft.PowerShell_profile.ps1")

        run_plugin({
            "requestId": "8a",
            "command": "apply",
            "args": {"theme": "bubbles", "profile": profile},
            "context": {"dryRun": False}
        })

        res = run_plugin({
            "requestId": "8b",
            "command": "apply",
            "args": {"theme": "paradox", "profile": profile},
            "context": {"dryRun": True}
        })

        assert res["success"]
        assert res["changed"]  
        content = read_file(profile)
        assert "bubbles" in content  
        assert "paradox" not in content
        print("✓ test_dry_run_theme_swap")


def test_check_installed():
    with tempfile.TemporaryDirectory() as tmp:
        profile = os.path.join(tmp, "Microsoft.PowerShell_profile.ps1")
        
        res = run_plugin({
            "requestId": "5a",
            "command": "check_installed",
            "args": {"profile": profile},
            "context": {}
        })
        assert res["success"]
        assert res["data"] is False
        
        run_plugin({
            "requestId": "5b",
            "command": "apply",
            "args": {"theme": "agnoster", "profile": profile},
            "context": {"dryRun": False}
        })
        
        res_global = run_plugin({
            "requestId": "5c",
            "command": "check_installed",
            "args": {"profile": profile},
            "context": {}
        })
        assert res_global["data"] is True
        
        res_specific = run_plugin({
            "requestId": "5d",
            "command": "check_installed",
            "args": {"theme": "agnoster", "profile": profile},
            "context": {}
        })
        assert res_specific["data"] is True
        
        res_mismatch = run_plugin({
            "requestId": "5e",
            "command": "check_installed",
            "args": {"theme": "cloudish", "profile": profile},
            "context": {}
        })
        assert res_mismatch["data"] is False
        print("✓ test_check_installed")

def test_invalid_payloads():
    res_err = run_plugin({
        "requestId": "6a",
        "command": "apply",
        "args": {},
        "context": {}
    })
    assert not res_err["success"]
    assert "error" in res_err
    
    res_cmd = run_plugin({
        "requestId": "6b",
        "command": "dismantle_system",
        "args": {},
        "context": {}
    })
    assert not res_cmd["success"]
    print("✓ test_invalid_payloads")

def test_existing_content_preserved():
    with tempfile.TemporaryDirectory() as tmp:
        profile = os.path.join(tmp, "Microsoft.PowerShell_profile.ps1")
        write_file(profile, "Set-Alias vim nvim\n")

        run_plugin({
            "requestId": "7a",
            "command": "apply",
            "args": {"theme": "paradox", "profile": profile},
            "context": {"dryRun": False}
        })

        run_plugin({
            "requestId": "7b",
            "command": "apply",
            "args": {"theme": "chips", "profile": profile},
            "context": {"dryRun": False}
        })

        content = read_file(profile)
        assert "Set-Alias vim nvim" in content
        assert "paradox" not in content
        assert "chips" in content
        print("✓ test_existing_content_preserved")

# Main Runner Execution 

if __name__ == "__main__":
    print("Launching Oh My Posh Plugin Testing Pass...\n")
    
    test_apply_fresh_install()
    test_apply_idempotent()
    test_apply_update_theme()
    test_dry_run()
    test_dry_run_theme_swap()
    test_check_installed()
    test_invalid_payloads()
    test_existing_content_preserved()
    
    print("\nAll integration checks finished successfully.")