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
- Before accepting/assigning any plugin feature request or issue, verify `plugins/<name>/` doesn't already exist on `main` — if it does, close as duplicate
- Contributors with open PRs cannot receive new assignments
- Partial/split submissions for a single issue are not accepted — full feature must be delivered in one PR
- Repo homepage set to GitHub Pages: https://DotDev262.github.io/WinHome/

## Merged This Session
- **#385** (Deno, @silentguyracer) — Closes #330.
- **#428** (Multimedia docs, @Monica-CodingWorld) — Closes #400.
- **#415** (Module docs, @bhagya-2006) — Closes #236.
- **#430** (ScheduledTask identity bug, @ionfwsrijan) — Closes #425.
- **#423** (Package managers batch, @Stewartsson) — Closes #402.
- **#388** (Dual State Management, @ionfwsrijan) — Closes #376.
- **#384** (Docker multi-stage, @Stewartsson) — Closes #311.
- **#377** (Syncthing, @Bhagyashri77777) — Closes #181.
- **#371** (CliBuilder tests, @VIDYANKSHINI) — Closes #223.
- **#379** (Wallpaper Engine, @Stewartsson) — Closes #301.
- **#381** (Flow Launcher, @basantnema31) — Closes #141.
- **#422** (Spotify, @Devexhhh) — Closes #130.
- **#386** (Joplin, @Vidheendu) — Closes #184.
- **#433** (Greenshot, @Stewartsson) — Closes #293.
- **#439** (Bump JsonSchema.Net, @app/dependabot) — Dependency upgrade.
- **#432** (Sublime Text, @RaghuveerSingh05) — Closes #178.
- **#437** (Config Backup & Restore, @sat-06) — Closes #434.
- **#431** (Developer Tools Docs, @VIDYANKSHINI) — Closes #399.
- **#441** (dependsOn, @akshara200829-lgtm) — Closes #435.
- **#338** (NuGet, @lokeshkumar69) — Closes #328.
- **#442** (Postman, @Vidheendu) — Closes #291.
- **#417** (Fix Process PATH, @Bhagyashri77777) — Closes #392.
- **#457** (actions/checkout v6→v7, @app/dependabot) — Dependency bump.
- **#443** (Log file persistence, @Stewartsson) — Closes #147.
- **#460** (Spotify docs, @CH-GAGANRAJ) — Closes #453.
- **#462** (Joplin docs, @rj9884) — Closes #449.
- **#372** (VLC, @A-adilajaleel) — Closes #297.
- **#463** (Greenshot docs, @rj9884) — Closes #448.
- **#464** (Postman docs, @Stewartsson) — Closes #451.
- **#465** (Rainmeter docs, @Vishxlll20) — Closes #452.
- **#468** (Sublime Text docs, @hasitapattapu) — Closes #454.
- **#461** (Drift detection + source path metadata, @sat-06) — Full drift detection delivered. Closes #444, #425.
- **#458** (README entries, @Bhagyashri77777) — Closes #455.
- **#456** (Scoop docs rewrite, @Aashita101) — Closes #230.

## Closed This Session
- **#436** (README TOC, @Yogender-verma) — TOC redundant with DocFx site.
- **#427** (Logging - JSON, @Shashank1725) — Per author request. Closes #147 (unassigned).
- **#380** (Binary registry, @aayushprsingh) — Issue already fixed by #373. Non-assignee PR.
- **#383** (Plugin Health Checker, @Vidheendu) — Vague scope, core infra touches.
- **#382** (README TOC, @Yogender-verma) — Duplicate of #436 rationale.
- **#440** (Audacity duplicate, @ashu-here) — Plugin already on main via PR #354.
- **#474** (Landing page styling, @khushitripathi06) — Out of scope (CSS/design preference, DocFx styling).

## Unassigned
- **#236** (Docs, @priyanshi-coder-2) — 12 days, no PR.
- **#311** (Docker, @mahi-bansal) — 10 days, no PR.
- **#230** (Docs, @Stewartsson) — Per request (wanted #293).
- **#390** (Non-elevated shell, @Gnanesh67) — 9 days, no PR.

## New Assignments
- **#407** (Command Injection, @ionfwsrijan) — type:security, level:intermediate
- **#425** (ScheduledTask identity bug, @ionfwsrijan) — type:bug, level:beginner
- **#178** (Sublime Text, @RaghuveerSingh05) — type:feature, level:beginner
- **#293** (Greenshot, @Stewartsson) — type:feature, level:beginner
- **#434** (Backup & Restore, @sat-06) — type:feature, level:intermediate
- **#435** (dependsOn support, @akshara200829-lgtm) — type:feature, level:intermediate
- **#147** (Logging persistent, @Stewartsson) — type:feature, level:beginner
- **#291** (Postman, @Vidheendu) — type:feature, level:beginner
- **#230** (Docs scoop, @Aashita101) — type:docs, level:beginner
- **#390** (Non-elevated shell, @Randomlyclueless) — type:bug, level:intermediate
- **#444** (Drift Detection, @sat-06) — type:feature, level:intermediate
- **#445** (Docs deno, @billu-beep) — type:docs, level:beginner
- **#453** (Docs spotify, @CH-GAGANRAJ) — type:docs, level:beginner
- **#454** (Docs sublime-text, @hasitapattapu) — type:docs, level:beginner
- **#455** (Docs README entries, @Bhagyashri77777) — type:docs, level:beginner
- **#452** (Docs rainmeter, @Vishxlll20) — type:docs, level:beginner
- **#449** (Docs joplin, @rj9884) — type:docs, level:beginner
- **#448** (Docs greenshot, @rj9884) — type:docs, level:beginner
- **#451** (Docs postman, @Stewartsson) — type:docs, level:beginner

## Reviews Done This Session

### Approved and merged
- **#372** (VLC, @A-adilajaleel) — BOM removed from plugin.yaml, 21 tests pass, protocol-compliant. Merged.
- **#443** (Log persistence, @Stewartsson) — Build fixed, clean persistence without emoji, 527 tests pass. Merged.
- **#460** (Spotify docs, @CH-GAGANRAJ) — fzf revert confirmed, clean scoped PR. Merged.
- **#462** (Joplin docs, @rj9884) — Standard template, POSIX newlines, scoped. Merged.
- **#463** (Greenshot docs, @rj9884) — Standard template, POSIX newlines, scoped. Merged.
- **#464** (Postman docs, @Stewartsson) — Standard template, POSIX newlines, CLEAN merge state. Merged.
- **#465** (Rainmeter docs, @Vishxlll20) — POSIX newline fixed, standard template. Merged.
- **#468** (Sublime Text docs, @hasitapattapu) — POSIX newline fixed, standard template. Merged.
- **#461** (Drift detection + source path metadata, @sat-06) — Full drift detection delivered. Build passes (0 errors), 3 drift tests pass. Merged. Closes #444, #425.
- **#458** (README entries, @Bhagyashri77777) — Clean scoped PR, 5 entries added. Merged. Closes #455.
- **#456** (Scoop docs rewrite, @Aashita101) — Standard template, POSIX newlines. Merged. Closes #230.
- **#475** (Ditto docs, @sleepyme06) — Standard template, POSIX newlines, scoped. Merged. Closes #446.
- **#472** (VLC test lint fix, @Vishxlll20) — Removed blank line causing ruff import-sort error. Merged.
- **#471** (NuGet docs, @Vishxlll20) — Comprehensive NuGet docs + README entry. Merged. Closes #450.
- **#476** (Flow Launcher docs, @vipul674) — Standard template, POSIX newlines. Merged. Closes #447.
- **#369** (Topgrade plugin, @AdityaM-IITH) — check_installed returns bare bool, dryRun from args, atomic writes, protocol-compliant. Build passes. Merged. Closes #186.

### CHANGES_REQUESTED
- **#374** (Cross-platform config, @ANSHIKATYAGI30) — Atomic write fix applied. 4 test assertions check `response["changed"]` on non-apply responses (check_installed, error). Asked to remove stale assertions.
- **#429** (Yarn, @krishsharma-code) — dryRun from context, test import paths, success field. Author says "working on it."
- **#470** (GitHub Desktop, @Stewartsson) — File paths at wrong directory level, plugin.yaml format incorrect. 6 behind main.
- **#459** (Deno docs, @billu-beep) — README scope fixed (now +1 line only). Content is correct but 15 behind main — needs rebase before merge.
- **#477** (Admin elevation check, @Randomlyclueless) — Duplicate `using` statement in AppRunner.cs (compilation error). 0 behind main.
## Open PRs

| # | Author | Title | State | Status |
|---|--------|-------|-------|--------|
| 477 | @Randomlyclueless | Admin elevation check (#390) | UNSTABLE | CHANGES_REQUESTED (duplicate using statement — compilation error) — 2 behind main |
| 470 | @Stewartsson | GitHub Desktop plugin | UNSTABLE | CHANGES_REQUESTED (file paths, plugin.yaml format) — 2 behind main |
| 459 | @billu-beep | Deno docs | UNKNOWN | CHANGES_REQUESTED resolved (content correct) — 17 behind main, needs rebase |
| 429 | @krishsharma-code | Yarn plugin | UNSTABLE | CHANGES_REQUESTED (protocol) — 28 behind main |
| 374 | @ANSHIKATYAGI30 | Cross-platform config | UNSTABLE | CHANGES_REQUESTED (test assertions) — 38 behind main |

## Available Issues
- **#467** (Plugin Docker Compose, level:beginner) — assigned to @amanyadav2107

## New Assignments This Session
- **#469** (Configuration Recipes docs, @suryansh24-coder) — type:docs, level:beginner
- **#450** (Docs nuget, @Vishxlll20) — type:docs, level:beginner
- **#447** (Docs flow-launcher, @vipul674) — type:docs, level:beginner
- **#467** (Plugin Docker Compose, @amanyadav2107) — type:feature, level:beginner

## Key Contributors
- @VIDYANKSHINI: 10+ merged PRs — most productive.
- @Stewartsson: #361, #379, #384, #433, #423, #443, #464 merged. Has open PR #470 (GitHub Desktop, 2 behind main, CHANGES_REQUESTED).
- @Randomlyclueless: Open PR #477 (admin elevation fix, CHANGES_REQUESTED). Assigned #390.
- @Bhagyashri77777: #357, #377, #417, #458 merged. No open assignments.
- @sat-06: #387, #437, #461 merged. No open assignments.
- @ionfwsrijan: #366, #367, #373, #388, #430 merged. No open assignments.
- @Vidheendu: #386, #442 merged. No open assignments.
- @rj9884: #462, #463 merged. New contributor, completed #449 and #448. No open assignments.
- @RaghuveerSingh05: #432 merged. No open assignments.
- @akshara200829-lgtm: #441 merged. No open assignments.
- @lokeshkumar69: #338 merged. No open assignments.
- @A-adilajaleel: #372 (VLC) merged. No open assignments.
- @AdityaM-IITH: #369 (Topgrade) merged. No open assignments.
- @Vishxlll20: #465, #472, #471 merged. All assignments completed. No open assignments.
- @CH-GAGANRAJ: #460 merged. New contributor, completed #453.
- @Aashita101: #456 merged. New contributor, completed #230.
- @hasitapattapu: #468 merged. New contributor, completed #454.
- @sleepyme06: #475 merged. New contributor, completed #446. No open assignments.
- @vipul674: #476 merged. New contributor, completed #447. No open assignments.

## Blocked / Warned
- @Pratikshya32: final warning — off-topic issues. Reported to GSSoC.
- @leno23: reported to GSSoC — 10 unassigned PRs closed.
- @basantnema31: warned for mass-requesting.
