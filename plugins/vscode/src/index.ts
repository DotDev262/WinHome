import * as fs from "fs";
import * as path from "path";
import { execSync } from "child_process";

interface Request {
  requestId: string;
  command: string;
  args: any;
  context: {
    dryRun: boolean;
  };
}

interface Response {
  requestId: string;
  success: boolean;
  changed: boolean;
  error?: string | null;
  data?: any;
}

const APPDATA = process.env.APPDATA || "";
const VSCODE_USER_PATH = path.join(APPDATA, "Code", "User");
const SETTINGS_JSON_PATH = path.join(VSCODE_USER_PATH, "settings.json");
const STORAGE_JSON_PATH = path.join(VSCODE_USER_PATH, "globalStorage", "storage.json");

function log(msg: string) {
  process.stderr.write(`[vscode-plugin] ${msg}\n`);
}

function ensureProfile(profileName: string, dryRun: boolean): string | null {
  if (!profileName || profileName.toLowerCase() === "default") return null;

  if (!fs.existsSync(STORAGE_JSON_PATH)) {
    log(`Warning: storage.json not found at ${STORAGE_JSON_PATH}. Cannot manage named profiles.`);
    return null;
  }

  try {
    const storage = JSON.parse(fs.readFileSync(STORAGE_JSON_PATH, "utf8"));
    const profiles = storage.userDataProfiles || [];
    let profile = profiles.find((p: any) => p.name === profileName);

    if (!profile) {
      if (dryRun) {
        log(`Would create VSCode profile: ${profileName}`);
        return "dry-run-location";
      }

      log(`Creating VSCode profile: ${profileName}...`);
      const location = Math.random().toString(16).slice(2, 10);
      profile = {
        name: profileName,
        location: location,
        icon: "gear"
      };
      profiles.push(profile);
      storage.userDataProfiles = profiles;

      const profileDir = path.join(VSCODE_USER_PATH, "profiles", location);
      if (!fs.existsSync(profileDir)) {
        fs.mkdirSync(profileDir, { recursive: true });
      }

      fs.writeFileSync(STORAGE_JSON_PATH, JSON.stringify(storage, null, 4), "utf8");
      return location;
    }
    return profile.location;
  } catch (e) {
    log(`Error ensuring profile ${profileName}: ${e}`);
    return null;
  }
}

function getInstalledExtensions(profileName?: string): string[] {
  try {
    const profileArg = profileName ? `--profile "${profileName}"` : "";
    const output = execSync(`code ${profileArg} --list-extensions`, { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] });
    return output.split(/\r?\n/).map(line => line.trim().toLowerCase()).filter(line => line.length > 0);
  } catch (e) {
    log(`Warning: 'code' command failed for profile ${profileName || "default"}.`);
    return [];
  }
}

function checkInstalled(args: any, requestId: string): Response {
  const packageId = args.packageId.toLowerCase();
  const installed = getInstalledExtensions(args.profile);
  return {
    requestId: requestId,
    success: true,
    changed: false,
    data: installed.includes(packageId)
  };
}

function install(args: any, context: any, requestId: string): Response {
  const packageId = args.packageId;
  const profileName = args.profile;
  const installed = getInstalledExtensions(profileName);

  if (installed.includes(packageId.toLowerCase())) {
    return { requestId: requestId, success: true, changed: false };
  }

  if (context.dryRun) {
    log(`Would install VSCode extension: ${packageId}${profileName ? ` in profile ${profileName}` : ""}`);
    return { requestId: requestId, success: true, changed: false };
  }

  log(`Installing VSCode extension: ${packageId}${profileName ? ` in profile ${profileName}` : ""}...`);
  try {
    const profileArg = profileName ? `--profile "${profileName}"` : "";
    execSync(`code ${profileArg} --install-extension ${packageId}`, { stdio: ["ignore", 2, 2] });
    return { requestId: requestId, success: true, changed: true };
  } catch (e: any) {
    return { requestId: requestId, success: false, changed: false, error: e.message };
  }
}

function uninstall(args: any, context: any, requestId: string): Response {
  const packageId = args.packageId;
  const profileName = args.profile;
  const installed = getInstalledExtensions(profileName);

  if (!installed.includes(packageId.toLowerCase())) {
    return { requestId: requestId, success: true, changed: false };
  }

  if (context.dryRun) {
    log(`Would uninstall VSCode extension: ${packageId}${profileName ? ` in profile ${profileName}` : ""}`);
    return { requestId: requestId, success: true, changed: false };
  }

  log(`Uninstalling VSCode extension: ${packageId}${profileName ? ` in profile ${profileName}` : ""}...`);
  try {
    const profileArg = profileName ? `--profile "${profileName}"` : "";
    execSync(`code ${profileArg} --uninstall-extension ${packageId}`, { stdio: ["ignore", 2, 2] });
    return { requestId: requestId, success: true, changed: true };
  } catch (e: any) {
    return { requestId: requestId, success: false, changed: false, error: e.message };
  }
}

function applyToProfile(profileName: string | null, config: any, context: any, requestId: string): { success: boolean, changed: boolean, error?: string } {
  let overallSuccess = true;
  let overallChanged = false;

  const location = profileName ? ensureProfile(profileName, context.dryRun) : null;
  const settingsPath = location 
    ? path.join(VSCODE_USER_PATH, "profiles", location, "settings.json")
    : SETTINGS_JSON_PATH;

  // 1. Install Extensions
  if (config.extensions && Array.isArray(config.extensions)) {
    for (const ext of config.extensions) {
      const res = install({ packageId: ext, profile: profileName }, context, requestId);
      if (!res.success) overallSuccess = false;
      if (res.changed) overallChanged = true;
    }
  }

  // 2. Apply Settings
  const desiredSettings = config.settings || {};
  
  const settingsDir = path.dirname(settingsPath);
  if (!fs.existsSync(settingsDir)) {
    if (!context.dryRun) {
      fs.mkdirSync(settingsDir, { recursive: true });
    }
  }

  let currentSettings: any = {};
  if (fs.existsSync(settingsPath)) {
    try {
      const content = fs.readFileSync(settingsPath, "utf8");
      currentSettings = JSON.parse(content);
    } catch (e) {
      log(`Error parsing ${settingsPath}: ${e}. Starting with empty settings.`);
    }
  }

  let changed = false;
  for (const [key, value] of Object.entries(desiredSettings)) {
    if (JSON.stringify(currentSettings[key]) !== JSON.stringify(value)) {
      currentSettings[key] = value;
      changed = true;
    }
  }

  if (!changed) {
    return { success: overallSuccess, changed: overallChanged };
  }

  if (context.dryRun) {
    log(`Would update VSCode settings.json for profile ${profileName || "default"}`);
    return { success: overallSuccess, changed: overallChanged };
  }

  try {
    fs.writeFileSync(settingsPath, JSON.stringify(currentSettings, null, 4), "utf8");
    return { success: overallSuccess, changed: true };
  } catch (e: any) {
    return { success: false, changed: overallChanged, error: e.message };
  }
}

function applyConfig(args: any, context: any, requestId: string): Response {
  let overallSuccess = true;
  let overallChanged = false;

  // Apply to default profile if settings or extensions are at top level
  if (args.settings || args.extensions) {
    const res = applyToProfile(null, args, context, requestId);
    if (!res.success) overallSuccess = false;
    if (res.changed) overallChanged = true;
  }

  // Apply to named profiles
  if (args.profiles && typeof args.profiles === "object") {
    for (const [name, config] of Object.entries(args.profiles)) {
      const res = applyToProfile(name, config, context, requestId);
      if (!res.success) overallSuccess = false;
      if (res.changed) overallChanged = true;
    }
  }

  return {
    requestId: requestId,
    success: overallSuccess,
    changed: overallChanged
  };
}

async function main() {
  let inputData = "";
  process.stdin.on("data", (chunk) => {
    inputData += chunk;
  });

  process.stdin.on("end", () => {
    if (!inputData) return;
    
    let request: Request;
    try {
      request = JSON.parse(inputData);
    } catch (e) {
      log(`Failed to parse request: ${e}`);
      process.exit(1);
    }

    let response: Response;
    switch (request.command) {
      case "check_installed":
        response = checkInstalled(request.args, request.requestId);
        break;
      case "install":
        response = install(request.args, request.context, request.requestId);
        break;
      case "uninstall":
        response = uninstall(request.args, request.context, request.requestId);
        break;
      case "apply":
        response = applyConfig(request.args, request.context, request.requestId);
        break;
      default:
        response = {
          requestId: request.requestId,
          success: false,
          changed: false,
          error: `Unknown command: ${request.command}`
        };
    }

    process.stdout.write(JSON.stringify(response) + "\n");
  });
}

main();