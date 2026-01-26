# System Settings

Provides a simplified way to apply common system settings (backed by registry tweaks).

**YAML Key:** `system_settings`

**Available Settings:**
-   `dark_mode`: `true` or `false`.
-   `taskbar_alignment`: `left` or `center`.
-   `taskbar_widgets`: `hide` or `show`.
-   `show_file_extensions`: `true` or `false`.
-   `show_hidden_files`: `true` or `false`.
-   `seconds_in_clock`: `true` or `false`.
-   `explorer_launch_to`: `this_pc` or `quick_access`.
-   `bing_search_enabled`: `true` or `false`.
-   `brightness`: `0-100`. Sets the screen brightness.
-   `volume`: `0-100`. Sets the system volume.
-   `notification`: A dictionary with `title` and `message` to send a notification.

**Example:**
```yaml
system_settings:
  dark_mode: true
  taskbar_alignment: center
  show_file_extensions: true
  brightness: 80
  volume: 50
  notification:
    title: WinHome
    message: System settings applied!
```
