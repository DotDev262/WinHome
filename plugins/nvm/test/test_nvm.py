import os

# We need to import the plugin.py, but it's a script without an __init__.py package.
# A common way to test standalone scripts is via importlib.util or by updating sys.path.
import sys
import tempfile
from unittest.mock import patch

import pytest

sys.path.append(os.path.join(os.path.dirname(__file__), "..", "src"))
import plugin


@pytest.fixture
def mock_env():
    with tempfile.TemporaryDirectory() as temp_dir:
        with patch.dict(os.environ, {"APPDATA": temp_dir}):
            yield temp_dir


def test_check_installed_false(mock_env):
    with patch("shutil.which", return_value=None):
        resp = plugin.check_installed({}, "req-1")
        assert resp["success"] is True
        assert resp["data"] is False


def test_check_installed_true_via_file(mock_env):
    os.makedirs(os.path.join(mock_env, "nvm"))
    with open(os.path.join(mock_env, "nvm", "settings.txt"), "w") as f:
        f.write("root=C:\\nvm\n")

    with patch("shutil.which", return_value=None):
        resp = plugin.check_installed({}, "req-2")
        assert resp["success"] is True
        assert resp["data"] is True


def test_check_installed_true_via_path(mock_env):
    with patch("shutil.which", return_value="C:\\nvm\\nvm.exe"):
        resp = plugin.check_installed({}, "req-3")
        assert resp["success"] is True
        assert resp["data"] is True


def test_apply_new_file(mock_env):
    args = {"settings": {"root": "C:\\nvm", "path": "C:\\Program Files\\nodejs", "arch": "64"}}
    resp = plugin.apply_config(args, {}, "req-4")
    assert resp["success"] is True
    assert resp["changed"] is True

    settings_path = os.path.join(mock_env, "nvm", "settings.txt")
    assert os.path.exists(settings_path)
    with open(settings_path, "r") as f:
        content = f.read()
    assert "root=C:\\nvm\n" in content
    assert "path=C:\\Program Files\\nodejs\n" in content
    assert "arch=64\n" in content


def test_apply_merge_existing_file(mock_env):
    os.makedirs(os.path.join(mock_env, "nvm"))
    settings_path = os.path.join(mock_env, "nvm", "settings.txt")
    with open(settings_path, "w") as f:
        f.write("# NVM settings\nroot=C:\\old_nvm\narch=32\n")

    args = {"settings": {"root": "C:\\new_nvm", "proxy": "none"}}
    resp = plugin.apply_config(args, {}, "req-5")
    assert resp["success"] is True
    assert resp["changed"] is True

    with open(settings_path, "r") as f:
        lines = f.readlines()

    assert lines[0] == "# NVM settings\n"
    assert lines[1] == "root=C:\\new_nvm\n"
    assert lines[2] == "arch=32\n"
    assert lines[3] == "proxy=none\n"


def test_apply_idempotent(mock_env):
    os.makedirs(os.path.join(mock_env, "nvm"))
    settings_path = os.path.join(mock_env, "nvm", "settings.txt")
    with open(settings_path, "w") as f:
        f.write("root=C:\\nvm\npath=C:\\nodejs\n")

    args = {"settings": {"root": "C:\\nvm", "path": "C:\\nodejs"}}
    resp = plugin.apply_config(args, {}, "req-6")
    assert resp["success"] is True
    assert resp["changed"] is False


def test_apply_dry_run(mock_env):
    os.makedirs(os.path.join(mock_env, "nvm"))
    settings_path = os.path.join(mock_env, "nvm", "settings.txt")
    with open(settings_path, "w") as f:
        f.write("root=C:\\old_nvm\n")

    args = {"settings": {"root": "C:\\new_nvm"}}
    resp = plugin.apply_config(args, {"dryRun": True}, "req-7")
    assert resp["success"] is True
    assert resp["changed"] is True

    # File should not be modified
    with open(settings_path, "r") as f:
        assert f.read() == "root=C:\\old_nvm\n"


if __name__ == "__main__":
    # Simple manual runner for CI discovery
    import os
    import tempfile
    from unittest.mock import patch

    with tempfile.TemporaryDirectory() as tmp:
        with patch.dict(os.environ, {"APPDATA": tmp}):
            print("Running test_check_installed_false...")
            test_check_installed_false(tmp)
            print("Running test_check_installed_true_via_file...")
            test_check_installed_true_via_file(tmp)
            print("Running test_check_installed_true_via_path...")
            test_check_installed_true_via_path(tmp)
            print("Running test_apply_new_file...")
            test_apply_new_file(tmp)
            print("Running test_apply_merge_existing_file...")
            test_apply_merge_existing_file(tmp)
            print("Running test_apply_idempotent...")
            test_apply_idempotent(tmp)
            print("Running test_apply_dry_run...")
            test_apply_dry_run(tmp)

    print("\nAll tests passed.")
