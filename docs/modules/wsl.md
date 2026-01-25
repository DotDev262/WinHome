# WSL (Windows Subsystem for Linux)

Manages WSL distributions.

**YAML Key:** `wsl`

**Properties:**
-   `update`: `true` to run `wsl --update`.
-   `defaultVersion`: Set the default WSL version (e.g., `2`).
-   `defaultDistro`: Set the default WSL distribution by name.
-   `distros`: A list of distributions to install and configure.
    -   `name`: The distro name (e.g., `Ubuntu-22.04`).
    -   `setupScript`: Path to a shell script to run inside the distro after installation.

**Example:**
```yaml
wsl:
  update: true
  defaultVersion: 2
  defaultDistro: "Ubuntu-22.04"
  distros:
    - name: "Ubuntu-22.04"
      setupScript: "scripts/ubuntu_setup.sh"
```
