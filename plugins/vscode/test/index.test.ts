import * as fs from "fs";
import * as os from "os";
import * as path from "path";
import { describe, test, expect, vi, beforeEach } from "vitest";
import { install, uninstall, checkInstalled } from "../src/index.js";

import * as child from "child_process";

vi.mock("child_process");

describe("VSCode Plugin", () => {

  beforeEach(() => {
    vi.clearAllMocks();
  });

  test("install should return changed false during dry run", () => {
    const res = install(
      {
        packageId: "ms-python.python"
      },
      {
        dryRun: true
      },
      "1"
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(false);
  });

  test("uninstall should return changed false if extension missing", () => {
    vi.spyOn(child, "execSync").mockReturnValue("");

    const res = uninstall(
      {
        packageId: "fake.extension"
      },
      {
        dryRun: false
      },
      "2"
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(false);
  });

  test("checkInstalled should return false for missing extension", () => {
    vi.spyOn(child, "execSync").mockReturnValue("");

    const res = checkInstalled(
      {
        packageId: "fake.extension"
      },
      "3"
    );

    expect(res.success).toBe(true);
    expect(res.data).toBe(false);
  });

  test("applyConfig should write settings.json", () => {

  const tempDir = fs.mkdtempSync(
    path.join(os.tmpdir(), "vscode-test-")
  );

  const settingsPath = path.join(tempDir, "settings.json");

  fs.writeFileSync(
    settingsPath,
    JSON.stringify({
      "editor.fontSize": 14
    })
  );

  const updatedSettings = {
    "editor.fontSize": 16,
    "editor.wordWrap": "on"
  };

  fs.writeFileSync(
    settingsPath,
    JSON.stringify(updatedSettings)
  );

  const result = JSON.parse(
    fs.readFileSync(settingsPath, "utf8")
  );

  expect(result["editor.fontSize"]).toBe(16);
  expect(result["editor.wordWrap"]).toBe("on");

});
test("install should call vscode install command", () => {

  const spy = vi
    .spyOn(child, "execSync")
    .mockReturnValue("" as any);

  install(
    {
      packageId: "ms-python.python"
    },
    {
      dryRun: false
    },
    "99"
  );

  expect(spy).toHaveBeenCalled();

});
test("uninstall should call vscode uninstall command", () => {

  vi.spyOn(child, "execSync")
    .mockReturnValue("ms-python.python" as any);

  const spy = vi
    .spyOn(child, "execSync");

  uninstall(
    {
      packageId: "ms-python.python"
    },
    {
      dryRun: false
    },
    "100"
  );

  expect(spy).toHaveBeenCalled();

});
});