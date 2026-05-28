import json
import os
import tempfile

from plugins.ripgrep.src.plugin import (
    parse_ripgreprc,
    build_ripgreprc_content,
    merge_settings,
)


def test_parse_ripgreprc():
    lines = [
        "--smart-case\n",
        "--hidden\n",
        "--max-columns=150\n",
    ]

    parsed = parse_ripgreprc(lines)

    assert parsed["smart-case"] is True
    assert parsed["hidden"] is True
    assert parsed["max-columns"] == "150"


def test_build_ripgreprc_content():
    config = {
        "smart-case": True,
        "hidden": True,
        "max-columns": 150,
    }

    content = build_ripgreprc_content(config)

    assert "--smart-case" in content
    assert "--hidden" in content
    assert "--max-columns=150" in content


def test_merge_settings():
    current = {
        "smart-case": True,
    }

    new = {
        "hidden": True,
    }

    changed = merge_settings(current, new)

    assert changed is True
    assert current["hidden"] is True


def test_merge_settings_remove_false():
    current = {
        "hidden": True,
    }

    new = {
        "hidden": False,
    }

    changed = merge_settings(current, new)

    assert changed is True
    assert "hidden" not in current
