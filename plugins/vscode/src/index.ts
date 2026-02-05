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

function log(msg: string) {
  process.stderr.write(`[vscode-plugin] ${msg}\n`);
}

function getInstalledExtensions(): string[] {
  try {
    const output = execSync("code --list-extensions", { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] });
    return output.split(/\r?\n/).map(line => line.trim().toLowerCase()).filter(line => line.length > 0);
  } catch (e) {
    log(`Warning: 'code' command not found or failed. Extensions management might not work.`);
    return [];
  }
}

function checkInstalled(args: any, requestId: string): Response {
  const packageId = args.packageId.toLowerCase();
  const installed = getInstalledExtensions();
  return {
    requestId: requestId,
    success: true,
    changed: false,
    data: installed.includes(packageId)
  };
}

function install(args: any, context: any, requestId: string): Response {
  const packageId = args.packageId;
  const installed = getInstalledExtensions();

  if (installed.includes(packageId.toLowerCase())) {
    return { requestId: requestId, success: true, changed: false };
  }

  if (context.dryRun) {
    log(`Would install VSCode extension: ${packageId}`);
    return { requestId: requestId, success: true, changed: false };
  }

  log(`Installing VSCode extension: ${packageId}...`);
  try {
    execSync(`code --install-extension ${packageId}`, { stdio: "inherit" });
    return { requestId: requestId, success: true, changed: true };
  } catch (e: any) {
    return { requestId: requestId, success: false, changed: false, error: e.message };
  }
}

function uninstall(args: any, context: any, requestId: string): Response {
  const packageId = args.packageId;
  const installed = getInstalledExtensions();

  if (!installed.includes(packageId.toLowerCase())) {
    return { requestId: requestId, success: true, changed: false };
  }

  if (context.dryRun) {
    log(`Would uninstall VSCode extension: ${packageId}`);
    return { requestId: requestId, success: true, changed: false };
  }

  log(`Uninstalling VSCode extension: ${packageId}...`);
  try {
    execSync(`code --uninstall-extension ${packageId}`, { stdio: "inherit" });
    return { requestId: requestId, success: true, changed: true };
  } catch (e: any) {
    return { requestId: requestId, success: false, changed: false, error: e.message };
  }
}

function applyConfig(args: any, context: any, requestId: string): Response {
  // args is the config object for vscode
  const desiredSettings = args.settings || {};
  
  if (!fs.existsSync(VSCODE_USER_PATH)) {
    if (!context.dryRun) {
      fs.mkdirSync(VSCODE_USER_PATH, { recursive: true });
    }
  }

  let currentSettings: any = {};
  if (fs.existsSync(SETTINGS_JSON_PATH)) {
    try {
      const content = fs.readFileSync(SETTINGS_JSON_PATH, "utf8");
      currentSettings = JSON.parse(content);
    } catch (e) {
      log(`Error parsing settings.json: ${e}. Starting with empty settings.`);
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
    return { requestId: requestId, success: true, changed: false };
  }

  if (context.dryRun) {
    log("Would update VSCode settings.json");
    return { requestId: requestId, success: true, changed: false };
  }

  try {
    fs.writeFileSync(SETTINGS_JSON_PATH, JSON.stringify(currentSettings, null, 4), "utf8");
    return { requestId: requestId, success: true, changed: true };
  } catch (e: any) {
    return { requestId: requestId, success: false, changed: false, error: e.message };
  }
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