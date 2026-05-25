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


def read_config(tmp: str) -> str:
    with open(os.path.join(tmp, ".ssh", "config"), "r", encoding="utf-8") as f:
        return f.read()


def apply_payload(request_id: str, dry_run: bool = False) -> dict:
    return {
        "requestId": request_id,
        "command": "apply",
        "args": {
            "hosts": {
                "github.com": {
                    "HostName": "github.com",
                    "User": "git",
                    "IdentityFile": "~/.ssh/id_ed25519",
                    "ForwardAgent": "yes",
                },
                "dev-server": {
                    "HostName": "192.168.1.100",
                    "User": "ubuntu",
                    "Port": 22,
                    "IdentityFile": "~/.ssh/id_rsa",
                    "ServerAliveInterval": 60,
                },
            },
            "global": {
                "AddKeysToAgent": "yes",
                "StrictHostKeyChecking": "accept-new",
            },
        },
        "context": {"dryRun": dry_run},
    }


def test_check_installed():
    with tempfile.TemporaryDirectory() as tmp:
        os.makedirs(os.path.join(tmp, ".ssh"))
        res = run_plugin({
            "requestId": "1",
            "command": "check_installed",
            "args": {},
            "context": {},
        }, {"USERPROFILE": tmp})

    assert res["requestId"] == "1"
    assert res["success"] is True
    assert res["changed"] is False
    assert isinstance(res["data"]["installed"], bool)
    assert res["data"]["configDirExists"] is True
    print("OK: check_installed")


def test_apply_config_dry_run_does_not_create_file():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(apply_payload("2", dry_run=True), {"USERPROFILE": tmp})

        assert res["success"] is True
        assert res["changed"] is False
        assert not os.path.exists(os.path.join(tmp, ".ssh", "config"))
        print("OK: apply_config_dry_run")


def test_apply_config_creates_directory_and_file():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(apply_payload("3"), {"USERPROFILE": tmp})

        assert res["success"] is True
        assert res["changed"] is True

        content = read_config(tmp)
        assert "AddKeysToAgent yes" in content
        assert "StrictHostKeyChecking accept-new" in content
        assert "Host github.com" in content
        assert "  HostName github.com" in content
        assert "  User git" in content
        assert "  Port 22" in content
        assert "  ServerAliveInterval 60" in content
        print("OK: apply_config")


def test_apply_config_merges_existing_hosts_and_globals():
    with tempfile.TemporaryDirectory() as tmp:
        ssh_dir = os.path.join(tmp, ".ssh")
        os.makedirs(ssh_dir)
        with open(os.path.join(ssh_dir, "config"), "w", encoding="utf-8") as f:
            f.write(
                "Compression yes\n"
                "\n"
                "Host github.com\n"
                "  HostName ssh.github.com\n"
                "  User old-user\n"
                "\n"
                "Host existing\n"
                "  User admin\n"
            )

        res = run_plugin(apply_payload("4"), {"USERPROFILE": tmp})

        assert res["success"] is True
        assert res["changed"] is True

        content = read_config(tmp)
        assert "Compression yes" in content
        assert "Host github.com" in content
        assert "  HostName github.com" in content
        assert "  User git" in content
        assert "Host existing" in content
        assert "  User admin" in content
        assert "Host dev-server" in content
        print("OK: apply_config_merges_existing")


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        payload = apply_payload("5")

        res1 = run_plugin(payload, {"USERPROFILE": tmp})
        assert res1["success"] is True
        assert res1["changed"] is True

        res2 = run_plugin(payload, {"USERPROFILE": tmp})
        assert res2["success"] is True
        assert res2["changed"] is False
        print("OK: idempotent_apply")


def test_unknown_command():
    res = run_plugin({
        "requestId": "6",
        "command": "explode",
        "args": {},
        "context": {},
    })

    assert res["requestId"] == "6"
    assert res["success"] is False
    assert "error" in res
    print("OK: unknown_command")


if __name__ == "__main__":
    test_check_installed()
    test_apply_config_dry_run_does_not_create_file()
    test_apply_config_creates_directory_and_file()
    test_apply_config_merges_existing_hosts_and_globals()
    test_idempotent_apply()
    test_unknown_command()
    print("\nAll tests passed.")
