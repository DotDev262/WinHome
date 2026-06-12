import sys
from unittest.mock import MagicMock

import pytest

# Mock winreg so that Windows-only modules don't crash the import on Linux.
if sys.platform != "win32":
    sys.modules["winreg"] = MagicMock()


# Remove any dynamically inserted plugin source/test directories from sys.path
# to prevent cross-plugin path pollution.
def clean_sys_path():
    cleaned = []
    for p in sys.path:
        norm = p.replace("\\", "/").lower()
        if "plugins/" in norm and ("src" in norm or "test" in norm or "tests" in norm):
            continue
        cleaned.append(p)
    sys.path = cleaned


# Clear cached "plugin" module and clean sys.path before importing each test file node.
def pytest_collectstart(collector):
    clean_sys_path()
    if "plugin" in sys.modules:
        del sys.modules["plugin"]


# Also clear before file collection checks.
def pytest_collect_file(file_path, parent):
    clean_sys_path()
    if "plugin" in sys.modules:
        del sys.modules["plugin"]


# Restore sys.modules["plugin"] to the correct plugin module imported by
# the test file before executing each test, and keep sys.path clean.
def pytest_runtest_setup(item):
    clean_sys_path()
    test_module = item.module
    if hasattr(test_module, "plugin"):
        sys.modules["plugin"] = test_module.plugin


@pytest.fixture(autouse=True)
def mock_win32_platform(monkeypatch):
    monkeypatch.setattr(sys, "platform", "win32")
