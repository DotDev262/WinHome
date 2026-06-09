# WinHome Session Progress

## Goal
- Review, approve, and manage open PRs for WinHome; enforce one-issue-per-contributor; ensure PRs are merged with `gssoc:approved` label.

## Constraints & Preferences
- `git pull` after every merge
- Prefer squash merges with descriptive subject lines; add `gssoc:approved` label on merge only (not on issues)
- Plugin files must end with POSIX trailing newline
- No `sys.exit(1)` on JSON parse error — return JSON error response
- All new plugins must include `requestId` in response dicts (JSON-RPC contract)
- `check_installed` must return bare `bool`; `main()` wraps into `{"requestId": ..., "installed": result}`
- `settings` from `args.get("settings", {})`, not `args` directly
- Test files use `sys.path.append` + `sys.path.remove` or `importlib`, not `sys.path.insert(0)`
- Dry-run returns `changed: True` when changes would be made
- Empty stdin must return JSON error response — silent return hangs host
- Atomic writes via `tempfile.mkstemp()` + `os.replace()`
- `requestId` uses `request.get("requestId") or "unknown"` — not `.get("requestId", "")`
- `dryRun` from `args`, not `context`
- `"data"` wrapper and `"success"` field banned from all responses
- PRs from non-assignees closed — verify assigned issues before review
- PRs must only touch files in scope of assigned issue
- Contributors with open PRs cannot receive new assignments
- Repo homepage set to GitHub Pages: https://DotDev262.github.io/WinHome/

## Merged This Session
- **#377** (Syncthing, @Bhagyashri77777) — approved, squash merged with `gssoc:approved`. Closes #181.
- **#371** (CliBuilder tests, @VIDYANKSHINI) — scope creep reverted, approved, squash merged with `gssoc:approved`. Closes #223.
- **#379** (Wallpaper Engine, @Stewartsson) — 8 protocol issues fixed, 7/7 tests pass, 0 behind main. Approved and squash merged with `gssoc:approved`. Closes #301.

## Closed This Session
- **#380** (Binary registry, @aayushprsingh) — closed. Issue #364 already fixed by #373 (@ionfwsrijan). Non-assignee PR for resolved issue.
- **#383** (Plugin Health Checker, @Vidheendu) — closed. Vague scope, no GSSOC labels, touches core infrastructure. Alternatives offered (#184, #202, #291).
- **#382** (README TOC, @Yogender-verma) — closed. Repo already has DocFx documentation site on GitHub Pages; README TOC is redundant.

## Unassigned
- **#236** (Docs, @priyanshi-coder-2) — unassigned (12 days, no PR after 1-week policy). @bhagya-2006 asked to clarify merged PR claim.
- **#311** (Docker, @mahi-bansal) — unassigned (10 days, no PR after 1-week policy). Conflict resolved by first-requester priority — awarded to @Stewartsson.

## New Assignments
- **#327** (yarn) → @krishsharma-code
- **#330** (Deno) → @silentguyracer (has merged PR #368)
- **#141** (Flow Launcher) → @basantnema31 (user override of mass-requesting warning)
- **#184** (Joplin) → @Vidheendu
- **#311** (Docker multi-stage) → @Stewartsson (first requester)
- **#130** (Spotify) → @Devexhhh (first requester, #375 merged)
- **#202** (Rainmeter) → @VIDYANKSHINI (chose this from available options)
- **#96** (Config Backup) → @sat-06 (detailed planned approach)

## Reviewed This Session
- **#387** (Config Backup, @sat-06) — well-scoped, 0 behind main, clean approach. 3 issues: tab indentation in DotfileService.cs, no BackupService unit tests, `DateTime.Now` vs `UtcNow`. CHANGES_REQUESTED.
- **#384** (Docker multi-stage, @Stewartsson) — round 3 review: `.dockerignore` fixed ✅, but `FROM ://microsoft.com` regression persists (empty commit), still 1 behind main. CHANGES_REQUESTED.
- **#354** (Audacity, @Achiever199) — protocol-compliant, 18 tests, 0 behind main. Only issue: stray `bat/plugin.yaml` change. Asked to revert.
- **#339** (Ditto, @vedika76) — 5 protocol issues including critical `check_installed` bare bool. CHANGES_REQUESTED. 48h window.
- **#338** (NuGet, @lokeshkumar69) — 6 protocol issues including `"success"` field everywhere, `dryRun` from context. CHANGES_REQUESTED. 48h window.

## Open PRs (by review status)

### Aprroved — 0 behind main, ready to merge
- *(none currently)*

### CHANGES_REQUESTED — awaiting author fixes
- **#374** (Cross-platform config, @ANSHIKATYAGI30) — 0 behind main. 8 of 10 issues fixed, 1 remaining: `write_yaml` not atomic (still `open(file_path, "w")`). Tests don't assert `changed` field. Changelog under `data` instead of root.
- **#354** (Audacity, @Achiever199) — 0 behind main, protocol-compliant, 18 tests pass. Only issue: stray `plugins/bat/plugin.yaml` change (prettier side effect). Asked to revert.
- **#338** (NuGet, @lokeshkumar69) — 12 behind main, 6 issues: `"success"` in every response, `dryRun` from `context`, unknown command response polluted, `check_installed` unused params. 48h window.
- **#339** (Ditto, @vedika76) — 12 behind main, 5 issues: CRITICAL — `check_installed` returns bare bool without `requestId` wrap, wrong `requestId` pattern, no `isinstance(settings, dict)` guard, unused params. 48h window.
- **#384** (Docker multi-stage, @Stewartsson) — CHANGES_REQUESTED. Author rewrote for .NET 10 but 2 critical issues remain: (1) `net10.0-windows` + `win-x64` binary cannot run on `dotnet/runtime:10.0` Linux image — needs Windows containers or artifact-stage approach; (2) `dotnet restore` fails because `tests/` not copied but .sln references it. Also: sparse `.dockerignore`, 1 behind main, no `--no-restore`. Options offered (Windows containers vs. artifact stage). Round 2 (Jun 8): author implemented Option B but introduced malformed `FROM ://microsoft.com` line (regression), `.dockerignore` still not updated, still 1 behind main. Re-reviewed with 3 remaining issues. Round 3 (Jun 8): `.dockerignore` fixed ✅, but `FROM ://microsoft.com` unchanged (empty commit, no file changes), still 1 behind main. Re-reviewed.
- **#385** (Deno, @silentguyracer) — CHANGES_REQUESTED. 7 protocol violations (`"success"`, `"data"`, `dryRun` from context, wrong `requestId`, `check_installed` wrap issue, `"changed"` leak, unused param). Scope creep: includes `plugins/windows-explorer/` files. Uses fork's `main` branch.

### Awaiting review or rebase
- **#372** (VLC, @A-adilajaleel) — asked to rebase.
- **#369** (Topgrade, @AdityaM-IITH) — asked to rebase.
- **#254** (Sublime Text, @gitsofyash) — extension granted, asked to rebase.
- **#381** (Flow Launcher, @basantnema31) — 15 protocol issues (sys.exit, empty stdin hang, data/success, etc.).

### Stale / scope creep
- **#359** (--log-file, @Sujith-RMD) — scope creep (9 unrelated plugin files), formatting issues, stale.
- **#362** (Greenshot, @sachin-mahato25) — 6 issues, merge conflict, stale.
- **#360** (Scoop docs, @Tharsiga-21) — 1 of 6 plugins documented, stale.

## Blocked / Warned
- @Pratikshya32: final warning — off-topic issues #308/#309/#310 closed. Reported to GSSoC.
- @leno23: reported to GSSoC — 10 unassigned PRs closed.
- @basantnema31: warned for mass-requesting, user override granted for #141. PR #381 has 15 issues.
- @VIDYANKSHINI: 10+ merged PRs. Assigned #202 (Rainmeter) after losing #311 to Stewartsson (first-requester priority).

## Key Contributors
- @VIDYANKSHINI: 10+ merged PRs. Most productive. Assigned #202 (Rainmeter).
- @Stewartsson: #361 (Rustup), #379 (Wallpaper Engine) merged. Assigned #311 (Docker multi-stage). PR #384 needs full rewrite for C#.
- @Devexhhh: #375 (Discord) merged. Assigned #130 (Spotify).
- @Bhagyashri77777: #357 (Miniconda), #377 (Syncthing) merged. No open assignments.
- @silentguyracer: #368 (Win Explorer) merged. Assigned #330 (Deno). PR #385 has 7 issues + scope creep.
- @ionfwsrijan: #366, #367, #373 merged. Assigned #376 (Dual State Management).
- @sat-06: assigned #96 (Config Backup). No open PRs.
- @Vidheendu: assigned #184 (Joplin). New contributor.

## Available Issues
- **#202** (Rainmeter, level:beginner) — unassigned
- **#291** (Postman, level:beginner) — unassigned
- **#96** (Config Backup, level:beginner) — unassigned
- **#236** (Docs, level:beginner) — unassigned
