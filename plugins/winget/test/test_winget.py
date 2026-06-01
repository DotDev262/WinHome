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

PACKAGE_FAMILY = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe"


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


def settings_path(local_app_data: str) -> str:
    return os.path.join(
        local_app_data,
        "Packages",
        PACKAGE_FAMILY,
        "LocalState",
        "settings.json",
    )


def read_settings(local_app_data: str) -> dict:
    with open(settings_path(local_app_data), "r", encoding="utf-8") as f:
        return json.load(f)


def apply_payload(request_id: str, dry_run: bool = False) -> dict:
    return {
        "requestId": request_id,
        "command": "apply",
        "args": {
            "settings": {
                "visual": {
                    "progressBar": "rainbow",
                    "anonymizeDisplayedPaths": True,
                },
                "installBehavior": {
                    "preferences": {
                        "scope": "machine",
                        "architectures": ["x64", "arm64"],
                    },
                    "disableInstallNotes": True,
                },
                "telemetry": {
                    "disable": True,
                },
            },
        },
        "context": {"dryRun": dry_run},
    }


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
    print("OK: check_installed")


def test_apply_config_dry_run_does_not_create_file():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(apply_payload("2", dry_run=True), {"LOCALAPPDATA": tmp})

        assert res["success"] is True
        assert res["changed"] is True
        assert not os.path.exists(settings_path(tmp))
        print("OK: apply_config_dry_run")


def test_apply_config_creates_settings_file():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(apply_payload("3"), {"LOCALAPPDATA": tmp})

        assert res["success"] is True
        assert res["changed"] is True

        settings = read_settings(tmp)
        assert settings["visual"]["progressBar"] == "rainbow"
        assert settings["visual"]["anonymizeDisplayedPaths"] is True
        assert settings["installBehavior"]["preferences"]["scope"] == "machine"
        assert settings["installBehavior"]["preferences"]["architectures"] == [
            "x64",
            "arm64",
        ]
        assert settings["installBehavior"]["disableInstallNotes"] is True
        assert settings["telemetry"]["disable"] is True
        print("OK: apply_config_creates_settings_file")


def test_apply_config_deep_merges_existing_settings():
    with tempfile.TemporaryDirectory() as tmp:
        path = settings_path(tmp)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as f:
            json.dump(
                {
                    "visual": {
                        "progressBar": "accent",
                        "enableSixels": True,
                    },
                    "logging": {
                        "level": "warning",
                        "file": {
                            "ageLimitInDays": 7,
                        },
                    },
                    "installBehavior": {
                        "preferences": {
                            "locale": ["en-US"],
                        },
                    },
                },
                f,
            )

        res = run_plugin(apply_payload("4"), {"LOCALAPPDATA": tmp})

        assert res["success"] is True
        assert res["changed"] is True

        settings = read_settings(tmp)
        assert settings["visual"]["progressBar"] == "rainbow"
        assert settings["visual"]["enableSixels"] is True
        assert settings["logging"]["level"] == "warning"
        assert settings["logging"]["file"]["ageLimitInDays"] == 7
        assert settings["installBehavior"]["preferences"]["locale"] == ["en-US"]
        assert settings["installBehavior"]["preferences"]["scope"] == "machine"
        print("OK: apply_config_deep_merges_existing_settings")


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        payload = apply_payload("5")

        res1 = run_plugin(payload, {"LOCALAPPDATA": tmp})
        assert res1["success"] is True
        assert res1["changed"] is True

        res2 = run_plugin(payload, {"LOCALAPPDATA": tmp})
        assert res2["success"] is True
        assert res2["changed"] is False
        print("OK: idempotent_apply")


def test_rejects_non_object_settings():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "6",
                "command": "apply",
                "args": {"settings": ["not", "an", "object"]},
                "context": {"dryRun": False},
            },
            {"LOCALAPPDATA": tmp},
        )

        assert res["requestId"] == "6"
        assert res["success"] is False
        assert "settings must be an object" in res["error"]
        print("OK: rejects_non_object_settings")


def test_unknown_command():
    res = run_plugin(
        {
            "requestId": "7",
            "command": "explode",
            "args": {},
            "context": {},
        }
    )

    assert res["requestId"] == "7"
    assert res["success"] is False
    assert "error" in res
    print("OK: unknown_command")


def test_invalid_json_returns_error_response():
    returncode, res = run_plugin_raw("{not-json")

    assert returncode == 0
    assert res["requestId"] == "unknown"
    assert res["success"] is False
    assert res["changed"] is False
    assert "Failed to parse request" in res["error"]
    print("OK: invalid_json_returns_error_response")


if __name__ == "__main__":
    test_check_installed()
    test_apply_config_dry_run_does_not_create_file()
    test_apply_config_creates_settings_file()
    test_apply_config_deep_merges_existing_settings()
    test_idempotent_apply()
    test_rejects_non_object_settings()
    test_unknown_command()
    test_invalid_json_returns_error_response()
    print("\nAll tests passed.")
