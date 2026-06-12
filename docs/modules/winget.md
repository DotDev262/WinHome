# Winget

Installs packages using the `winget` command-line tool. Winget is Windows' official built-in package
manager that allows you to install, update, and manage applications from the command line.

**YAML Key:** `winget`

**Properties:**

- `id`: The package identifier (e.g., `Microsoft.PowerToys`).
- `source`: (Optional) The source to install from (e.g., `msstore`).

---

## Basic Usage

Winget lets you install applications by specifying their package ID. You can find package IDs by
searching on [winget.run](https://winget.run) or by running:

```powershell
winget search <app-name>
```

To install a single package using WinHome, add it under the `winget` key in your `config.yaml`:

```yaml
winget:
  - id: Microsoft.PowerToys
```

To install multiple packages at once:

```yaml
winget:
  - id: Microsoft.PowerToys
  - id: Microsoft.VSCode
  - id: Git.Git
```

---

## Advanced Configuration

### Installing from a Specific Source

Winget supports multiple sources like `winget` (default) and `msstore` (Microsoft Store). You can
specify the source explicitly:

```yaml
winget:
  - id: Microsoft.PowerToys
    source: winget
  - id: 9NKSQGP7F2NH
    source: msstore
```

### Real-World config.yaml Examples

**Example 1 — Developer Setup**

```yaml
winget:
  - id: Git.Git
    source: winget
  - id: Microsoft.VisualStudioCode
    source: winget
  - id: OpenJS.NodeJS
    source: winget
  - id: JanDeDobbeleer.OhMyPosh
    source: winget
```

**Example 2 — Productivity Setup**

```yaml
winget:
  - id: Microsoft.PowerToys
    source: winget
  - id: Notion.Notion
    source: winget
  - id: SlackTechnologies.Slack
    source: winget
```

**Example 3 — System Utilities Setup**

```yaml
winget:
  - id: 7zip.7zip
    source: winget
  - id: Microsoft.WindowsTerminal
    source: winget
  - id: voidtools.Everything
    source: winget
```

---

## Troubleshooting

**Issue: `winget` is not recognized as a command**

- Winget comes pre-installed on Windows 11. For Windows 10, install the
  [App Installer](https://apps.microsoft.com/store/detail/app-installer/9NBLGGH4NNS1) from the
  Microsoft Store.

**Issue: Package not found**

- Double-check the package ID at [winget.run](https://winget.run) or run `winget search <app-name>`
  in your terminal.
- Make sure you are specifying the correct `source`. Some packages are only available on `msstore`.

**Issue: Installation fails silently**

- Run winget manually to see the full error:

```powershell
  winget install --id Microsoft.PowerToys --source winget
```

- Make sure you are running the terminal as **Administrator**.

**Issue: Package already installed but WinHome tries to reinstall**

- This is expected behavior. Winget will skip installation if the package is already up to date.
