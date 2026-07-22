import importlib.util
import json
import os
import sys
from io import StringIO
from unittest.mock import patch

PLUGIN_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))
spec = importlib.util.spec_from_file_location("yarn_plugin", PLUGIN_PATH)
plugin = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(plugin)


def run_plugin(input_dict):
    input_str = json.dumps(input_dict)

    old_stdin = sys.stdin
    old_stdout = sys.stdout
    sys.stdin = StringIO(input_str)
    sys.stdout = StringIO()

    try:
        plugin.main()
        output_str = sys.stdout.getvalue()
        return json.loads(output_str)
    finally:
        sys.stdin = old_stdin
        sys.stdout = old_stdout


def test_check_installed_via_config(tmp_path):
    # Create berry config file
    berry = tmp_path / ".yarnrc.yml"
    berry.write_text("nodeLinker: node-modules\n", encoding="utf-8")

    # Avoid environment-dependent behavior in shutil.which during tests.
    with patch.object(plugin.shutil, "which", return_value="/usr/bin/yarn"), \
         patch.object(plugin, "_get_user_home", return_value=str(tmp_path)):

        response = run_plugin({"requestId": "r1", "command": "check_installed"})
        assert response["installed"] is True


def test_apply_dry_run_berry(tmp_path):
    with patch.object(plugin, "_get_user_home", return_value=str(tmp_path)):
        request = {
            "requestId": "r2",
            "command": "apply",
            "args": {
                "dryRun": True,
                "settings": {
                    "nodeLinker": "node-modules",
                    "enableTelemetry": False,
                    "compressionLevel": 0,
                    "supportedArchitectures": {"os": "linux"},
                },
            },
        }

        response = run_plugin(request)
        assert response["changed"] is True
        assert not (tmp_path / ".yarnrc.yml").exists()


def test_apply_writes_berry_file_with_newline(tmp_path):
    with patch.object(plugin, "_get_user_home", return_value=str(tmp_path)):
        request = {
            "requestId": "r3",
            "command": "apply",
            "args": {
                "dryRun": False,
                "settings": {
                    "nodeLinker": "node-modules",
                    "enableTelemetry": False,
                    "compressionLevel": 7,
                    "supportedArchitectures": {"cpu": "x64"},
                },
            },
        }

        response = run_plugin(request)
        assert response["changed"] is True

        p = tmp_path / ".yarnrc.yml"
        assert p.exists()
        content = p.read_text(encoding="utf-8")
        assert content.endswith("\n")
        assert "nodeLinker:" in content
        assert "enableTelemetry:" in content


def test_apply_classic_prefers_classic_if_present(tmp_path):
    with patch.object(plugin, "_get_user_home", return_value=str(tmp_path)):
        # Create classic file
        (tmp_path / ".yarnrc").write_text("npmRegistryServer https://example.com\n", encoding="utf-8")

        request = {
            "requestId": "r4",
            "command": "apply",
            "args": {
                "dryRun": False,
                "settings": {
                    "npmRegistryServer": "https://registry.yarnpkg.com",
                },
            },
        }

        response = run_plugin(request)
        assert response["changed"] is True

        content = (tmp_path / ".yarnrc").read_text(encoding="utf-8")
        assert "npmRegistryServer https://registry.yarnpkg.com" in content
