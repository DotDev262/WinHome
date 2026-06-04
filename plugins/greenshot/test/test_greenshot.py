"""
Tests for the Greenshot configuration provider plugin.
Run with: python -m pytest plugins/greenshot/test/test_greenshot.py -v
"""

from __future__ import annotations

import configparser
import os
import sys
import textwrap
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).parent.parent / "src"))
import plugin as gs


@pytest.fixture()
def tmp_config(tmp_path, monkeypatch):
    greenshot_dir = tmp_path / "Greenshot"
    greenshot_dir.mkdir()
    monkeypatch.setenv("APPDATA", str(tmp_path))
    return greenshot_dir / "Greenshot.ini"


@pytest.fixture()
def sample_ini(tmp_config):
    content = textwrap.dedent("""\
        [Capture]
        CaptureMode=Region
        CaptureMousepointer=True
        CaptureDelay=0

        [Destination]
        FileSavePath=C:\\Users\\User\\Pictures
        FileSaveAs=False
        CopyToClipboard=False

        [Output]
        FileFormat=png
        JpgQuality=80
        OutputFilePath=C:\\Users\\User\\Pictures
        OutputFileFilenamePattern=${title}_${date}_${time}

        [General]
        Language=en-US
    """)
    tmp_config.write_text(content, encoding="utf-8")
    return tmp_config


class TestCheckInstalled:
    def test_returns_true_when_appdata_dir_exists(self, tmp_config):
        result = gs.check_installed({"requestId": "r1"})
        assert result["installed"] is True

    def test_returns_false_when_missing(self, tmp_path, monkeypatch):
        monkeypatch.setenv("APPDATA", str(tmp_path))
        monkeypatch.setenv("PATH", "")
        result = gs.check_installed({})
        assert result["installed"] is False


class TestApply:
    def test_creates_config_when_missing(self, tmp_config):
        result = gs.apply({"settings": {"Output": {"FileFormat": "jpg"}}})
        assert tmp_config.exists()
        assert result["changeCount"] == 1

    def test_updates_existing_value(self, sample_ini):
        gs.apply({"settings": {"Capture": {"CaptureMode": "Fullscreen"}}})
        parser = configparser.RawConfigParser()
        parser.optionxform = str
        parser.read(str(sample_ini), encoding="utf-8")
        assert parser.get("Capture", "CaptureMode") == "Fullscreen"

    def test_preserves_untouched_keys(self, sample_ini):
        gs.apply({"settings": {"Output": {"FileFormat": "bmp"}}})
        parser = configparser.RawConfigParser()
        parser.optionxform = str
        parser.read(str(sample_ini), encoding="utf-8")
        assert parser.get("Output", "JpgQuality") == "80"


class TestDryRun:
    def test_does_not_write_file(self, tmp_config):
        gs.apply({"dryRun": True, "settings": {"Output": {"FileFormat": "gif"}}})
        assert not tmp_config.exists()

    def test_does_not_mutate_existing_file(self, sample_ini):
        original = sample_ini.read_text(encoding="utf-8")
        gs.apply({"dryRun": True, "settings": {"Capture": {"CaptureMode": "Window"}}})
        assert sample_ini.read_text(encoding="utf-8") == original


class TestIdempotency:
    def test_no_changes_on_repeat_apply(self, sample_ini):
        settings = {"Capture": {"CaptureMode": "Region"}}
        gs.apply({"settings": settings})
        result = gs.apply({"settings": settings})
        assert result["changeCount"] == 0
