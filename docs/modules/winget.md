# Winget

Installs packages using the `winget` command-line tool.

**YAML Key:** `winget`

**Properties:**
-   `id`: The package identifier (e.g., `Microsoft.PowerToys`).
-   `source`: (Optional) The source to install from (e.g., `msstore`).

**Example:**
```yaml
winget:
  - id: Microsoft.PowerToys
  - id: JanDeDobbeleer.OhMyPosh
    source: winget
```
