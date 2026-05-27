import importlib.util
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


def load_plugin():
    spec = importlib.util.spec_from_file_location("opencode_plugin", PLUGIN)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def run_plugin(payload: dict, env=None) -> dict:
    process_env = os.environ.copy()
    if env:
        process_env.update(env)

    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=process_env,
        check=False,
    )

    assert result.returncode == 0, result.stderr
    return json.loads(result.stdout.strip())


def apply_payload(args: dict, dry_run: bool = False) -> dict:
    return {
        "requestId": "apply-1",
        "command": "apply",
        "args": args,
        "context": {"dryRun": dry_run},
    }


def test_check_installed_returns_bare_installed_boolean():
    response = run_plugin({
        "requestId": "check-1",
        "command": "check_installed",
        "args": {},
        "context": {},
    })

    assert response["requestId"] == "check-1"
    assert response["success"] is True
    assert response["changed"] is False
    assert isinstance(response["data"], bool)


def test_apply_creates_missing_global_config_directory():
    with tempfile.TemporaryDirectory() as tmp:
        response = run_plugin(
            apply_payload({
                "model": "anthropic/claude-sonnet-4-5",
                "small_model": "anthropic/claude-haiku-4-5",
            }),
            env={"USERPROFILE": tmp},
        )

        config_path = os.path.join(tmp, ".config", "opencode", "opencode.json")

        assert response["success"] is True
        assert response["changed"] is True
        assert os.path.exists(config_path)
        assert not os.path.exists(f"{config_path}.tmp")

        with open(config_path, "r", encoding="utf-8") as config_file:
            saved = json.load(config_file)

        assert saved["model"] == "anthropic/claude-sonnet-4-5"
        assert saved["small_model"] == "anthropic/claude-haiku-4-5"


def test_apply_reads_jsonc_and_preserves_existing_keys():
    with tempfile.TemporaryDirectory() as tmp:
        config_dir = os.path.join(tmp, ".config", "opencode")
        os.makedirs(config_dir)
        config_path = os.path.join(config_dir, "opencode.json")

        with open(config_path, "w", encoding="utf-8") as config_file:
            config_file.write(
                '{\n'
                '  // keep this unrelated setting\n'
                '  "theme": "dark",\n'
                '  "url": "https://example.com//not-a-comment",\n'
                '  "permission": {\n'
                '    "write": "deny",\n'
                '    "read": "allow"\n'
                '  },\n'
                '  "agent": {\n'
                '    "existing": { "mode": "subagent" }\n'
                '  }\n'
                '}\n'
            )

        response = run_plugin(
            apply_payload({
                "permission": {
                    "write": "allow",
                    "edit": "allow",
                },
                "agent": {
                    "code-reviewer": {
                        "description": "Reviews code for best practices",
                        "model": "anthropic/claude-sonnet-4-5",
                        "permission": {
                            "write": "deny",
                            "read": "allow",
                        },
                        "mode": "subagent",
                    },
                },
            }),
            env={"USERPROFILE": tmp},
        )

        with open(config_path, "r", encoding="utf-8") as config_file:
            saved = json.load(config_file)

        assert response["success"] is True
        assert response["changed"] is True
        assert saved["theme"] == "dark"
        assert saved["url"] == "https://example.com//not-a-comment"
        assert saved["permission"]["read"] == "allow"
        assert saved["permission"]["write"] == "allow"
        assert saved["permission"]["edit"] == "allow"
        assert saved["agent"]["existing"]["mode"] == "subagent"
        assert saved["agent"]["code-reviewer"]["mode"] == "subagent"


def test_apply_dry_run_reports_change_without_writing():
    with tempfile.TemporaryDirectory() as tmp:
        response = run_plugin(
            apply_payload({"default_agent": "build"}, dry_run=True),
            env={"USERPROFILE": tmp},
        )

        config_path = os.path.join(tmp, ".config", "opencode", "opencode.json")

        assert response["success"] is True
        assert response["changed"] is True
        assert not os.path.exists(config_path)


def test_idempotent_apply_reports_unchanged_second_time():
    with tempfile.TemporaryDirectory() as tmp:
        env = {"USERPROFILE": tmp}
        payload = apply_payload({
            "command": {
                "test": {
                    "template": "Run the full test suite with coverage...",
                    "description": "Run tests with coverage",
                },
            },
        })

        first = run_plugin(payload, env=env)
        second = run_plugin(payload, env=env)

        assert first["changed"] is True
        assert second["success"] is True
        assert second["changed"] is False


def test_apply_supports_project_level_config():
    with tempfile.TemporaryDirectory() as tmp:
        response = run_plugin(
            apply_payload({
                "projectRoot": tmp,
                "mcp": {
                    "filesystem": {
                        "type": "local",
                        "command": [
                            "npx",
                            "-y",
                            "@modelcontextprotocol/server-filesystem",
                            "/path",
                        ],
                        "enabled": True,
                    },
                },
            }),
            env={"USERPROFILE": os.path.join(tmp, "home")},
        )

        config_path = os.path.join(tmp, "opencode.json")

        assert response["success"] is True
        assert response["changed"] is True
        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as config_file:
            saved = json.load(config_file)

        assert saved["mcp"]["filesystem"]["enabled"] is True
        assert "projectRoot" not in saved


def test_apply_supports_legacy_config_and_top_level_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        response = run_plugin(
            {
                "requestId": "legacy-1",
                "command": "apply",
                "config": {"model": "anthropic/claude-sonnet-4-5"},
                "dryRun": True,
            },
            env={"USERPROFILE": tmp},
        )

        config_path = os.path.join(tmp, ".config", "opencode", "opencode.json")

        assert response["success"] is True
        assert response["changed"] is True
        assert not os.path.exists(config_path)


def test_strip_jsonc_comments_keeps_comment_like_text_in_strings():
    plugin = load_plugin()
    cleaned = plugin.strip_jsonc_comments(
        '{"url": "https://example.com//ok", // remove me\n "value": 1}'
    )

    assert json.loads(cleaned) == {
        "url": "https://example.com//ok",
        "value": 1,
    }


def test_strip_jsonc_comments_handles_block_comments():
    plugin = load_plugin()
    cleaned = plugin.strip_jsonc_comments(
        '{"url": "https://example.com//ok", /* remove\n me */ "value": 1}'
    )

    assert json.loads(cleaned) == {
        "url": "https://example.com//ok",
        "value": 1,
    }


def test_unknown_command():
    response = run_plugin({
        "requestId": "unknown-1",
        "command": "explode",
        "args": {},
        "context": {},
    })

    assert response["requestId"] == "unknown-1"
    assert response["success"] is False
    assert "Unknown command" in response["error"]


if __name__ == "__main__":
    test_check_installed_returns_bare_installed_boolean()
    test_apply_creates_missing_global_config_directory()
    test_apply_reads_jsonc_and_preserves_existing_keys()
    test_apply_dry_run_reports_change_without_writing()
    test_idempotent_apply_reports_unchanged_second_time()
    test_apply_supports_project_level_config()
    test_apply_supports_legacy_config_and_top_level_dry_run()
    test_strip_jsonc_comments_keeps_comment_like_text_in_strings()
    test_strip_jsonc_comments_handles_block_comments()
    test_unknown_command()

    print("\nAll tests passed.")
