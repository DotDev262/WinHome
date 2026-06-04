import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))


def run_plugin(payload: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
    )

    return json.loads(result.stdout.strip())


def test_check_installed_response_format():
    res = run_plugin({"requestId": "1", "command": "check_installed", "args": {}, "context": {}})

    assert res["requestId"] == "1"
    assert res["success"]
    assert res["changed"] is False
    assert "data" in res
    assert isinstance(res["data"], bool)


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {"settings": {"storeDir": "C:/pnpm/store", "autoInstallPeers": True}},
                "context": {"dryRun": True},
            }
        )

        npmrc_path = os.path.join(tmp, ".npmrc")

        assert res["requestId"] == "2"
        assert res["success"]
        assert res["changed"]
        assert "data" in res
        assert not os.path.exists(npmrc_path)


def test_apply_config_writes_supported_settings():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "settings": {
                        "storeDir": "C:/pnpm/store",
                        "globalDir": "C:/pnpm/global",
                        "globalBinDir": "C:/pnpm/bin",
                        "nodeVersion": "22.0.0",
                        "packageManager": "pnpm@9.0.0",
                        "autoInstallPeers": True,
                        "strictPeerDependencies": False,
                        "shamefullyHoist": True,
                    }
                },
                "context": {"dryRun": False},
            }
        )

        npmrc_path = os.path.join(tmp, ".npmrc")

        assert res["requestId"] == "3"
        assert res["success"]
        assert res["changed"]
        assert os.path.exists(npmrc_path)

        with open(npmrc_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "store-dir=C:/pnpm/store" in content
        assert "global-dir=C:/pnpm/global" in content
        assert "global-bin-dir=C:/pnpm/bin" in content
        assert "node-version=22.0.0" in content
        assert "package-manager=pnpm@9.0.0" in content
        assert "auto-install-peers=true" in content
        assert "strict-peer-dependencies=false" in content
        assert "shamefully-hoist=true" in content


def test_preserves_unknown_existing_keys():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp
        npmrc_path = os.path.join(tmp, ".npmrc")

        with open(npmrc_path, "w", encoding="utf-8") as f:
            f.write("unknown-setting=value\n")

        res = run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"settings": {"storeDir": "C:/pnpm/store"}},
                "context": {"dryRun": False},
            }
        )

        assert res["success"]

        with open(npmrc_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "unknown-setting=value" in content
        assert "store-dir=C:/pnpm/store" in content


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        payload = {
            "requestId": "5",
            "command": "apply",
            "args": {"settings": {"storeDir": "C:/pnpm/store"}},
            "context": {"dryRun": False},
        }

        first = run_plugin(payload)
        second = run_plugin(payload)

        assert first["success"]
        assert first["changed"]
        assert second["success"]
        assert second["changed"] is False


def test_invalid_settings_returns_json_error():
    res = run_plugin(
        {
            "requestId": "6",
            "command": "apply",
            "args": {"settings": None},
            "context": {},
        }
    )

    assert res["requestId"] == "6"
    assert not res["success"]
    assert res["changed"] is False
    assert "error" in res
    assert "data" in res


def test_empty_stdin_returns_json_error():
    result = subprocess.run([sys.executable, PLUGIN], input="", capture_output=True, text=True)

    res = json.loads(result.stdout.strip())

    assert res["requestId"] == "unknown"
    assert not res["success"]
    assert res["changed"] is False
    assert "error" in res
    assert "data" in res


if __name__ == "__main__":
    test_check_installed_response_format()
    test_apply_config_dry_run()
    test_apply_config_writes_supported_settings()
    test_preserves_unknown_existing_keys()
    test_idempotent_apply()
    test_invalid_settings_returns_json_error()
    test_empty_stdin_returns_json_error()

    print("\nAll tests passed.")
