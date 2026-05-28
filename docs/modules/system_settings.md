# System Settings

Provides a simplified way to apply common system settings (backed by registry tweaks).

**YAML Key:** `system_settings`

**Available Settings:**
-   `dark_mode`: `true` or `false`.
-   `clipboard_history`: `true` or `false`. Enable or disable Windows clipboard history.
-   `taskbar_alignment`: `left` or `center`.
-   `taskbar_widgets`: `hide` or `show`.
-   `show_file_extensions`: `true` or `false`.
-   `show_hidden_files`: `true` or `false`.
-   `seconds_in_clock`: `true` or `false`.
-   `explorer_launch_to`: `this_pc` or `quick_access`.
-   `desktop_icons_this_pc`: `true` to show or `false` to hide.
-   `desktop_icons_user_folder`: `true` to show or `false` to hide.
-   `desktop_icons_network`: `true` to show or `false` to hide.
-   `desktop_icons_recycle_bin`: `true` to show or `false` to hide.
-   `desktop_icons_control_panel`: `true` to show or `false` to hide.
-   `bing_search_enabled`: `true` or `false`.
-   `taskbar_search`: `hidden`, `icon`, `icon_label`, or `search_box`.
-   `brightness`: `0-100`. Sets the screen brightness.
-   `volume`: `0-100`. Sets the system volume.
-   `notification`: A dictionary with `title` and `message` to send a notification.

**Example:**
```yaml
system_settings:
  dark_mode: true
  clipboard_history: true
  taskbar_alignment: center
  taskbar_search: icon
  show_file_extensions: true
  brightness: 80
  volume: 50
  notification:
    title: WinHome
    message: System settings applied!
```