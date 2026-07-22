import json
import os
import shutil
import sys
import urllib.request

# Official Obsidian releases url for community plugins
OBSIDIAN_RELEASES_URL = "https://raw.githubusercontent.com/obsidianmd/obsidian-releases/master/community-plugins.json"

# Mapping of setting keys to their respective file names in .obsidian/ directory
SETTING_FILE_MAP = {
    "accentColor": "appearance.json",
    "theme": "appearance.json",
    "cssTheme": "appearance.json",
    "baseFontSize": "appearance.json",
    "interfaceFontFamily": "appearance.json",
    "textFontFamily": "appearance.json",
    "monospaceFontFamily": "appearance.json",
    "foldIndent": "app.json",
    "showLineNumber": "app.json",
    "spellcheck": "app.json",
    "spellcheckLanguages": "app.json",
    "vimMode": "app.json",
    "readableLineLength": "app.json",
    "strictLineBreaks": "app.json",
    "defaultViewMode": "app.json",
    "livePreview": "app.json",
}

# Helpers


def log(msg):
    sys.stderr.write(f"[obsidian-plugin] {msg}\n")
    sys.stderr.flush()


class MockResponse:
    def __init__(self, data: bytes):
        self.data = data

    def read(self):
        return self.data

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        pass


def make_request(url: str):
    """Make an HTTP request to the given URL with a user agent"""
    if os.environ.get("WINHOME_TEST_MOCK_URLOPEN") == "1":
        if "community-plugins.json" in url:
            data = json.dumps([{"id": "obsidian-git", "repo": "mgmeyers/obsidian-git"}]).encode("utf-8")
            return MockResponse(data)
        elif "releases/latest" in url:
            data = json.dumps({"tag_name": "v1.2.3"}).encode("utf-8")
            return MockResponse(data)
        elif "manifest.json" in url:
            data = json.dumps({"id": "obsidian-git", "version": "1.2.3"}).encode("utf-8")
            return MockResponse(data)
        else:
            return MockResponse(b"mock content")

    req = urllib.request.Request(url, headers={"User-Agent": "WinHome-Environment-Manager/1.0"})
    return urllib.request.urlopen(req)


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        log(f"Warning: could not parse {file_path}: {e}")
        return {}


def write_json(file_path: str, data) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    with open(file_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)


def merge_settings(target: dict, source: dict) -> bool:
    """Merge settings from source into target and return True if any changes were made"""
    changed = False
    for key, value in source.items():
        if key not in target or target[key] != value:
            target[key] = value
            changed = True
    return changed


def group_settings_by_file(settings: dict) -> dict:
    """Seperates settings into app.json and appearance.json"""
    grouped = {}
    for key, value in settings.items():
        file_name = SETTING_FILE_MAP.get(key, "app.json")
        if file_name not in grouped:
            grouped[file_name] = {}
        grouped[file_name][key] = value
    return grouped


# Vault-wise settings


def apply_vault_settings(vault_path: str, settings: dict, dry_run: bool) -> dict:
    """Apply settings to a vault"""
    obsidian_dir = os.path.join(vault_path, ".obsidian")
    grouped = group_settings_by_file(settings)
    overall_changed = False

    for file_name, desired_values in grouped.items():
        file_path = os.path.join(obsidian_dir, file_name)
        current = read_json(file_path)
        changed = merge_settings(current, desired_values)
        if not changed:
            continue
        if dry_run:
            log(f"Would update {file_path} with: {json.dumps(desired_values)}")
            continue
        try:
            write_json(file_path, current)
            log(f"Updated {file_path}")
            overall_changed = True
        except Exception as e:
            return {
                "success": False,
                "changed": overall_changed,
                "error": str(e),
            }

    return {"success": True, "changed": overall_changed}


# Community Plugins


def get_enabled_plugins(vault_path: str) -> list:
    """Get list of enabled community plugins"""
    file_path = os.path.join(vault_path, ".obsidian", "community-plugins.json")
    if not os.path.exists(file_path):
        return []
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        return data if isinstance(data, list) else []
    except Exception as e:
        log(f"Warning: could not read {file_path}: {e}")
        return []


def save_enabled_plugins(vault_path: str, plugins: list) -> None:
    """Overwrites community-plugins.json with the given list of plugins"""
    file_path = os.path.join(vault_path, ".obsidian", "community-plugins.json")
    write_json(file_path, plugins)


def fetch_plugin_repo(plugin_id: str):
    """Fetches the github repo of a plugin"""
    try:
        with make_request(OBSIDIAN_RELEASES_URL) as r:
            plugins = json.loads(r.read().decode("utf-8"))
        plugin = next((p for p in plugins if p.get("id") == plugin_id), None)
        if not plugin:
            log(f"Plugin {plugin_id} not found in community registry")
            return None
        return plugin.get("repo")
    except Exception as e:
        log(f"Warning: could not fetch community plugin registry: {e}")
        return None


def fetch_latest_version(repo: str):
    """Fetches the latest version of a plugin from GitHub"""
    try:
        url = f"https://api.github.com/repos/{repo}/releases/latest"
        with make_request(url) as res:
            release_data = json.loads(res.read().decode("utf-8"))
        return release_data.get("tag_name")
    except Exception as e:
        log(f"Failed to fetch latest release tag for {repo}: {e}")
        return None


def download_plugin(vault_path: str, plugin_id: str, repo: str, version: str) -> None:
    """Downloads a plugin to the vault"""
    plugin_dir = os.path.join(vault_path, ".obsidian", "plugins", plugin_id)
    os.makedirs(plugin_dir, exist_ok=True)

    files = ["main.js", "manifest.json", "styles.css"]
    versions_to_try = [version]
    if not version.startswith("v"):
        versions_to_try.append(f"v{version}")

    for file in files:
        success = False
        last_error = None

        for v in versions_to_try:
            url = f"https://github.com/{repo}/releases/download/{v}/{file}"
            try:
                with make_request(url) as res:
                    content = res.read()
                target_path = os.path.join(plugin_dir, file)
                with open(target_path, "wb") as out:
                    out.write(content)
                log(f"Downloaded {file} for {plugin_id} ({v})")
                success = True
                break
            except Exception as e:
                last_error = e
                continue

        if not success and file != "styles.css":
            raise Exception(f"Failed to download required asset '{file}' for {plugin_id}: {last_error}")


# Apply Changes


def check_installed(args: dict, request_id: str) -> dict:
    """Checks if a plugin is installed and enabled"""
    vault_path = args["vaultPath"]
    plugin_id = args["pluginId"]
    enabled = get_enabled_plugins(vault_path)
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": plugin_id in enabled,
    }


def install_plugin(args: dict, context: dict, request_id: str) -> dict:
    """Installs a plugin to the vault"""
    vault_path = args["vaultPath"]
    plugin_id = args["pluginId"]
    enabled = get_enabled_plugins(vault_path)
    plugin_dir = os.path.join(vault_path, ".obsidian", "plugins", plugin_id)
    already_installed = plugin_id in enabled and os.path.exists(plugin_dir)

    if already_installed:
        log(f"Plugin {plugin_id} already installed and enabled.")
        return {"requestId": request_id, "success": True, "changed": False}

    if context.get("dryRun"):
        log(f"Would install {plugin_id}")
        return {"requestId": request_id, "success": True, "changed": False}

    try:
        repo = fetch_plugin_repo(plugin_id)
        if not repo:
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": f"Plugin not found in registry: {plugin_id}",
            }

        version = fetch_latest_version(repo)
        if not version:
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": f"Could not determine version for {plugin_id}",
            }

        download_plugin(vault_path, plugin_id, repo, version)

        if plugin_id not in enabled:
            enabled.append(plugin_id)
            save_enabled_plugins(vault_path, enabled)

        log(f"Installed and enabled plugin: {plugin_id} version {version}")
        return {"requestId": request_id, "success": True, "changed": True}
    except Exception as e:
        log(f"Error installing plugin {plugin_id}: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }


def uninstall_plugin(args: dict, context: dict, request_id: str) -> dict:
    """Uninstalls a plugin from the vault"""
    vault_path = args["vaultPath"]
    plugin_id = args["pluginId"]
    enabled = get_enabled_plugins(vault_path)
    plugin_dir = os.path.join(vault_path, ".obsidian", "plugins", plugin_id)
    is_installed = plugin_id in enabled or os.path.exists(plugin_dir)

    if not is_installed:
        log(f"Plugin {plugin_id} not installed.")
        return {"requestId": request_id, "success": True, "changed": False}

    if context.get("dryRun"):
        log(f"Would uninstall {plugin_id}")
        return {"requestId": request_id, "success": True, "changed": False}

    try:
        if os.path.exists(plugin_dir):
            shutil.rmtree(plugin_dir)
            log(f"Removed plugin directory: {plugin_id}")

        updated = [p for p in enabled if p != plugin_id]
        save_enabled_plugins(vault_path, updated)

        log(f"Uninstalled plugin: {plugin_id}")
        return {"requestId": request_id, "success": True, "changed": True}
    except Exception as e:
        log(f"Error uninstalling plugin {plugin_id}: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    overall_success = True
    overall_changed = False

    for vault in args.get("vaults", []):
        vault_path = vault.get("path")
        if not vault_path:
            log("Skipping vault with no path")
            continue
        if not os.path.exists(vault_path):
            log(f"Warning: vault path does not exist: {vault_path}")
            continue

        if vault.get("settings"):
            res = apply_vault_settings(vault_path, vault["settings"], context.get("dryRun", False))
            if not res["success"]:
                overall_success = False
            if res["changed"]:
                overall_changed = True
            if res.get("error"):
                log(f"Vault settings error ({vault_path}): {res['error']}")

        for plugin_id in vault.get("plugins", []):
            res = install_plugin(
                {"vaultPath": vault_path, "pluginId": plugin_id},
                context,
                request_id,
            )
            if not res["success"]:
                overall_success = False
            if res["changed"]:
                overall_changed = True

    return {
        "requestId": request_id,
        "success": overall_success,
        "changed": overall_changed,
    }


# Main


def main():
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.exit(1)

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    response = {"requestId": request_id, "success": False, "changed": False}

    try:
        if command == "check_installed":
            response = check_installed(args, request_id)
        elif command == "install":
            response = install_plugin(args, context, request_id)
        elif command == "uninstall":
            response = uninstall_plugin(args, context, request_id)
        elif command == "apply":
            response = apply_config(args, context, request_id)
        else:
            response["error"] = f"Unknown command: {command}"
    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
