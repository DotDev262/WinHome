import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { describe, test, expect, vi, beforeEach, afterEach } from 'vitest';
import * as child from 'child_process';

vi.mock('child_process');

describe('VSCode Plugin', () => {
  let tempDir: string;
  let settingsPath: string;

  beforeEach(async () => {
    vi.clearAllMocks();
    vi.resetModules(); // Force re-import so APPDATA is re-read

    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'vscode-test-'));
    const vscodeUserPath = path.join(tempDir, 'Code', 'User');
    fs.mkdirSync(vscodeUserPath, { recursive: true });
    settingsPath = path.join(vscodeUserPath, 'settings.json');
    process.env.APPDATA = tempDir;
  });

  afterEach(() => {
    vi.restoreAllMocks();
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  async function getModule() {
    return await import('../src/index.js');
  }

  test('install: dry run returns changed=false without calling execSync', async () => {
    const { install } = await getModule();
    const spy = vi.spyOn(child, 'execSync').mockReturnValue('' as any);

    const res = install(
      { packageId: 'ms-python.python' },
      { dryRun: true },
      '1'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(false);
    const installCalls = spy.mock.calls.filter((c) =>
      String(c[0]).includes('--install-extension')
    );
    expect(installCalls).toHaveLength(0);
  });

  test('install: calls --install-extension when not already installed', async () => {
    const { install } = await getModule();
    const spy = vi
      .spyOn(child, 'execSync')
      .mockReturnValueOnce('' as any)
      .mockReturnValueOnce('' as any);

    const res = install(
      { packageId: 'ms-python.python' },
      { dryRun: false },
      '2'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(true);
    const installCall = spy.mock.calls.find((c) =>
      String(c[0]).includes('--install-extension')
    );
    expect(installCall).toBeDefined();
    expect(String(installCall![0])).toContain('ms-python.python');
  });

  test('install: skips install if extension already present', async () => {
    const { install } = await getModule();
    vi.spyOn(child, 'execSync').mockReturnValue('ms-python.python\n' as any);

    const res = install(
      { packageId: 'ms-python.python' },
      { dryRun: false },
      '3'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(false);
  });

  test('uninstall: returns changed=false if extension not installed', async () => {
    const { uninstall } = await getModule();
    vi.spyOn(child, 'execSync').mockReturnValue('' as any);

    const res = uninstall(
      { packageId: 'fake.extension' },
      { dryRun: false },
      '4'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(false);
  });

  test('uninstall: calls --uninstall-extension when extension is present', async () => {
    const { uninstall } = await getModule();
    const spy = vi
      .spyOn(child, 'execSync')
      .mockReturnValueOnce('ms-python.python\n' as any)
      .mockReturnValueOnce('' as any);

    const res = uninstall(
      { packageId: 'ms-python.python' },
      { dryRun: false },
      '5'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(true);
    const uninstallCall = spy.mock.calls.find((c) =>
      String(c[0]).includes('--uninstall-extension')
    );
    expect(uninstallCall).toBeDefined();
  });

  test('checkInstalled: returns false for missing extension', async () => {
    const { checkInstalled } = await getModule();
    vi.spyOn(child, 'execSync').mockReturnValue('' as any);

    const res = checkInstalled({ packageId: 'fake.extension' }, '6');

    expect(res.success).toBe(true);
    expect(res.data).toBe(false);
  });

  test('checkInstalled: returns true when extension is listed', async () => {
    const { checkInstalled } = await getModule();
    vi.spyOn(child, 'execSync').mockReturnValue('ms-python.python\n' as any);

    const res = checkInstalled({ packageId: 'ms-python.python' }, '7');

    expect(res.success).toBe(true);
    expect(res.data).toBe(true);
  });

  test('applyConfig: writes new settings to the real settings.json', async () => {
    const { applyConfig } = await getModule();
    fs.writeFileSync(
      settingsPath,
      JSON.stringify({ 'editor.fontSize': 14 }),
      'utf8'
    );

    const res = applyConfig(
      { settings: { 'editor.fontSize': 16, 'editor.wordWrap': 'on' } },
      { dryRun: false },
      '8'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(true);
    const written = JSON.parse(fs.readFileSync(settingsPath, 'utf8'));
    expect(written['editor.fontSize']).toBe(16);
    expect(written['editor.wordWrap']).toBe('on');
  });

  test('applyConfig: returns changed=false when settings already up to date', async () => {
    const { applyConfig } = await getModule();
    fs.writeFileSync(
      settingsPath,
      JSON.stringify({ 'editor.fontSize': 16, 'editor.wordWrap': 'on' }),
      'utf8'
    );

    const res = applyConfig(
      { settings: { 'editor.fontSize': 16, 'editor.wordWrap': 'on' } },
      { dryRun: false },
      '9'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(false);
  });

  test('applyConfig: dry run does not modify settings.json', async () => {
    const { applyConfig } = await getModule();
    const original = JSON.stringify({ 'editor.fontSize': 14 });
    fs.writeFileSync(settingsPath, original, 'utf8');

    applyConfig(
      { settings: { 'editor.fontSize': 99 } },
      { dryRun: true },
      '10'
    );

    const after = fs.readFileSync(settingsPath, 'utf8');
    expect(after).toBe(original);
  });

  test('applyConfig: installs listed extensions via applyConfig->install', async () => {
    const { applyConfig } = await getModule();
    const spy = vi
      .spyOn(child, 'execSync')
      .mockReturnValueOnce('' as any)
      .mockReturnValueOnce('' as any);

    const res = applyConfig(
      { extensions: ['ms-python.python'] },
      { dryRun: false },
      '11'
    );

    expect(res.success).toBe(true);
    expect(res.changed).toBe(true);
    const installCall = spy.mock.calls.find((c) =>
      String(c[0]).includes('--install-extension')
    );
    expect(installCall).toBeDefined();
  });
});
