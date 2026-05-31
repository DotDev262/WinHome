import configparser
import datetime
import json
import os
import shutil
import sys
import tempfile
import uuid


def log(msg):
    sys.stderr.write(f"[obs-studio] {msg}\n")
    sys.stderr.flush()


def get_obs_appdata():
    appdata = os.getenv("APPDATA")
    if not appdata:
        raise Exception("APPDATA environment variable not found")
    return os.path.join(appdata, "obs-studio")


def read_ini(path):
    config = configparser.RawConfigParser()
    if os.path.exists(path):
        try:
            config.read(path, encoding="utf-8")
        except configparser.Error as e:
            timestamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d%H%M%S")
            suffix = uuid.uuid4().hex[:8]
            backup_path = f"{path}.corrupted.{timestamp}.{suffix}"
            log(f"Config corrupted. Backing up to {backup_path} and starting fresh. Error: {e}")
            try:
                shutil.move(path, backup_path)
            except Exception as backup_e:
                log(f"Failed to backup corrupted config: {backup_e}")
    return config


def write_ini(path, config):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    fd, temp_path = tempfile.mkstemp(prefix="obs-studio-", dir=os.path.dirname(path))
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            config.write(f)
        os.replace(temp_path, path)
    except Exception:
        os.unlink(temp_path)
        raise


def merge_ini_section(config, section, values):
    changed = False
    if not config.has_section(section):
        config.add_section(section)
    for key, value in values.items():
        str_value = str(value)
        if not config.has_option(section, key) or config.get(section, key) != str_value:
            config.set(section, key, str_value)
            changed = True
    return changed


def get_obs_install_dir():
    override = os.getenv("OBS_INSTALL_DIR")
    if override:
        return override
    program_files = os.getenv("PROGRAMFILES", "C:\\Program Files")
    return os.path.join(program_files, "obs-studio")


def check_installed(args, request_id):
    obs_exe = os.path.join(get_obs_install_dir(), "bin", "64bit", "obs64.exe")
    installed = os.path.exists(obs_exe)
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


GENERAL_KEY_MAP = {
    "theme": "Theme",
    "language": "Language",
    "prevents_display_sleep": "PreventDisplaySleep",
}


def apply_global_ini(obs_dir, general, dry_run):
    global_ini_path = os.path.join(obs_dir, "global.ini")
    config = read_ini(global_ini_path)
    changed = False

    if general:
        mapped = {GENERAL_KEY_MAP[k]: v for k, v in general.items() if k in GENERAL_KEY_MAP}
        if mapped:
            if dry_run:
                log(f"Would update {global_ini_path} [General] with: {mapped}")
                changed = merge_ini_section(config, "General", mapped)
            else:
                changed = merge_ini_section(config, "General", mapped)
                if changed:
                    write_ini(global_ini_path, config)
                    log(f"Updated {global_ini_path}")

    return changed


def get_active_profile(obs_dir):
    global_ini_path = os.path.join(obs_dir, "global.ini")
    config = read_ini(global_ini_path)
    if config.has_option("Basic", "ProfileDir"):
        return config.get("Basic", "ProfileDir")
    return None


VIDEO_KEY_MAP = {
    "base_resolution": "Base",
    "output_resolution": "Output",
    "fps_type": "FPSType",
    "fps_common": "FPSCommon",
}

AUDIO_KEY_MAP = {
    "sample_rate": "SampleRate",
    "channels": "ChannelSetup",
    "desktop_device": "DesktopAudioDevice1",
    "mic_device": "AuxAudioDevice1",
}


def apply_profile_ini(obs_dir, profile_name, args, dry_run):
    profile_dir = os.path.join(obs_dir, "basic", "profiles", profile_name)
    basic_ini_path = os.path.join(profile_dir, "basic.ini")
    config = read_ini(basic_ini_path)
    changed = False

    video = args.get("video")
    if video:
        mapped = {VIDEO_KEY_MAP[k]: v for k, v in video.items() if k in VIDEO_KEY_MAP}
        if dry_run:
            log(f"Would update {basic_ini_path} [Video] with: {mapped}")
            changed = merge_ini_section(config, "Video", mapped) or changed
        else:
            changed = merge_ini_section(config, "Video", mapped) or changed

    audio = args.get("audio")
    if audio:
        mapped = {AUDIO_KEY_MAP[k]: v for k, v in audio.items() if k in AUDIO_KEY_MAP}
        if dry_run:
            log(f"Would update {basic_ini_path} [Audio] with: {mapped}")
            changed = merge_ini_section(config, "Audio", mapped) or changed
        else:
            changed = merge_ini_section(config, "Audio", mapped) or changed

    output = args.get("output")
    if output:
        output_section = {"Mode": output["mode"]} if "mode" in output else {}
        recording = output.get("recording", {})
        streaming = output.get("streaming", {})
        simple_output = {}
        if recording:
            simple_output.update(
                {
                    "FilePath": recording.get("path", ""),
                    "RecQuality": recording.get("quality", ""),
                    "RecFormat": recording.get("format", ""),
                    "RecEncoder": recording.get("encoder", ""),
                }
            )
        if streaming:
            simple_output.update(
                {
                    "VBitrate": str(streaming.get("bitrate", "")),
                    "StreamEncoder": streaming.get("encoder", ""),
                }
            )
        if dry_run:
            log(f"Would update {basic_ini_path} [Output] with: {output_section}")
            log(f"Would update {basic_ini_path} [SimpleOutput] with: {simple_output}")
            if output_section:
                changed = merge_ini_section(config, "Output", output_section) or changed
            if simple_output:
                changed = merge_ini_section(config, "SimpleOutput", simple_output) or changed
        else:
            if output_section:
                changed = merge_ini_section(config, "Output", output_section) or changed
            if simple_output:
                changed = merge_ini_section(config, "SimpleOutput", simple_output) or changed

    hotkeys = args.get("hotkeys")
    if hotkeys:
        if dry_run:
            log(f"Would update {basic_ini_path} [Hotkeys] with: {hotkeys}")
            changed = merge_ini_section(config, "Hotkeys", hotkeys) or changed
        else:
            changed = merge_ini_section(config, "Hotkeys", hotkeys) or changed

    if changed and not dry_run:
        write_ini(basic_ini_path, config)
        log(f"Updated {basic_ini_path}")

    return changed


def ensure_profiles(obs_dir, profiles, dry_run):
    changed = False
    for profile in profiles:
        name = profile.get("name")
        if not name:
            log("Skipping profile entry with no name")
            continue

        profile_dir = os.path.join(obs_dir, "basic", "profiles", name)
        basic_ini_path = os.path.join(profile_dir, "basic.ini")

        if dry_run:
            log(f"Would create profile directory: {profile_dir}")
            continue

        os.makedirs(profile_dir, exist_ok=True)

        if not os.path.exists(basic_ini_path):
            config = configparser.RawConfigParser()
            write_ini(basic_ini_path, config)
            log(f"Created profile: {name}")
            changed = True
        else:
            log(f"Profile already exists: {name}")

        settings = profile.get("settings", {})
        if settings:
            config = read_ini(basic_ini_path)
            section_changed = merge_ini_section(config, "General", settings)
            if section_changed:
                write_ini(basic_ini_path, config)
                changed = True

    return changed


def apply_config(args, context, request_id):
    dry_run = context.get("dryRun", False)
    changed = False

    try:
        obs_dir = get_obs_appdata()

        general = args.get("general")
        if general:
            changed = apply_global_ini(obs_dir, general, dry_run) or changed

        profile_name = args.get("profile") or get_active_profile(obs_dir)
        has_profile_settings = any(args.get(k) for k in ("video", "audio", "output", "hotkeys"))

        if has_profile_settings:
            if not profile_name:
                log("No profile specified and no active profile found — skipping video/audio/output/hotkeys")
            else:
                changed = apply_profile_ini(obs_dir, profile_name, args, dry_run) or changed

        profiles = args.get("profiles", [])
        if profiles:
            changed = ensure_profiles(obs_dir, profiles, dry_run) or changed

    except Exception as e:
        log(f"Error applying config: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
            "data": None,
        }

    return {
        "requestId": request_id,
        "success": True,
        "changed": changed,
        "data": None,
    }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "error": "No input",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "error": f"Failed to parse request: {e}",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    response = {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "data": None,
    }

    try:
        if command == "check_installed":
            response = check_installed(args, request_id)
        elif command == "apply":
            response = apply_config(args, context, request_id)
        else:
            response["error"] = f"Unknown command: {command}"
    except Exception as e:
        response["error"] = f"Internal error: {str(e)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
