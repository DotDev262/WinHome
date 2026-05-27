"""Unit tests for the Rainmeter plugin.

Tests cover installation detection and apply merging behavior (dry-run and actual write).
"""
from __future__ import annotations

import json
import os
import sys
from pathlib import Path
import shutil

import pytest

# Ensure the plugin module can be imported from src directory
SRC_DIR = Path(__file__).resolve().parents[1] / "src"
sys.path.insert(0, str(SRC_DIR))

import plugin  # type: ignore


def test_check_installed_via_path(monkeypatch, tmp_path):
    # Simulate Rainmeter.exe on PATH
    monkeypatch.setattr(shutil, "which", lambda name: str(tmp_path / "Rainmeter.exe") if name.startswith("Rainmeter") else None)
    (tmp_path / "Rainmeter.exe").write_text("exe")

    p = plugin.find_rainmeter_executable()
    assert p is not None
    assert p.name.lower().startswith("rainmeter")


def test_check_installed_via_program_files(monkeypatch, tmp_path):
    # Simulate missing PATH but existing Program Files install
    monkeypatch.setattr(shutil, "which", lambda name: None)
    monkeypatch.setenv("ProgramFiles", str(tmp_path))
    rf = tmp_path / "Rainmeter"
    rf.mkdir()
    (rf / "Rainmeter.exe").write_text("exe")

    p = plugin.find_rainmeter_executable()
    assert p is not None
    assert p.exists()


def test_apply_merge_dry_run_and_write(monkeypatch, tmp_path):
    # Use a fake APPDATA so we don't touch the real user's files
    appdata = tmp_path / "Roaming"
    rm_dir = appdata / "Rainmeter"
    rm_dir.mkdir(parents=True)
    ini = rm_dir / "Rainmeter.ini"

    # Create existing config with one section and option
    from configparser import ConfigParser

    cp = ConfigParser(interpolation=None)
    cp.optionxform = str
    cp.add_section("Rainmeter")
    cp.set("Rainmeter", "SkinPath", "C:\\OldPath")
    with ini.open("w", encoding="utf-8") as f:
        cp.write(f)

    monkeypatch.setenv("APPDATA", str(appdata))

    # Prepare new settings to merge
    new_settings = {
        "Rainmeter": {"SkinPath": r"%USERPROFILE%\\Documents\\Rainmeter\\Skins", "ConfigEditor": "notepad.exe"},
        "UserInterface": {"Language": "en"},
    }

    # Dry-run: should not write, but should report changes
    resp = plugin.handle_apply("req-1", {"dry_run": True, "settings": new_settings})
    assert resp.get("requestId") == "req-1"
    assert "data" in resp
    data = resp["data"]
    assert data["dry_run"] is True
    assert isinstance(data["changes"], list)
    # The SkinPath value is changing, and ConfigEditor and UserInterface.Language will be added
    keys = {(c["section"], c["key"]) for c in data["changes"]}
    assert ("Rainmeter", "SkinPath") in keys
    assert ("Rainmeter", "ConfigEditor") in keys
    assert ("UserInterface", "Language") in keys

    # Actual write: file should be updated
    resp2 = plugin.handle_apply("req-2", {"dry_run": False, "settings": new_settings})
    assert resp2.get("requestId") == "req-2"
    assert resp2.get("data", {}).get("dry_run") is False
    assert ini.exists()

    # Confirm merged values exist in written file
    cp2 = ConfigParser(interpolation=None)
    cp2.optionxform = str
    cp2.read(ini, encoding="utf-8")
    # Normalize backslashes because configparser may escape backslashes when writing
    actual_skinpath = cp2.get("Rainmeter", "SkinPath").replace('\\\\', '\\')
    assert actual_skinpath == r"%USERPROFILE%\Documents\Rainmeter\Skins"
    assert cp2.get("Rainmeter", "ConfigEditor") == "notepad.exe"
    assert cp2.get("UserInterface", "Language") == "en"
