# WinHome Session Progress

## Goal
- Review and manage open PRs for WinHome, assign GSSOC issues, enforce one-issue-per-contributor, and send reminders to stale assignees.

## Constraints & Preferences
- `git pull` after every merge to keep local repo in sync.
- All plugin issues follow the same pattern: Python, `config_provider`, JSON-over-stdio, GSSOC+type:feature labels.
- Prefer squash merges with descriptive subject lines; add `gssoc:approved` label on merge (not for Dependabot).
- Issues with assignees but no PR after one week get reassigned.
- Contributors who mass‑request issues get warned; one issue per contributor at a time — enforced.
- Plugin files must end with POSIX trailing newline.
- `sys.exit(1)` on JSON parse error is banned — must return JSON error response.
- All new plugins must include `requestId` in response dicts (JSON-RPC contract).
- Contributors with open PRs or warnings do not get new assignments.
- `check_installed` must return bare `bool`, not `{"installed": bool}`.
- `settings` must come from `args.get("settings", {})`, not `args` directly.
- Corruption backups must use UUID suffix (not plain `.bak` or bare timestamp).
- Test files must use `sys.path.append` + `sys.path.remove` or `importlib`, not `sys.path.insert(0)`.
- Dry-run responses must report `changed: True` when changes would be made, not `False`.
- `gssoc:approved` label is required on every GSSOC PR (+50 base points).
- Label schema: difficulty required (`level:beginner`/`intermediate`/`advanced`/`critical`), quality optional (`quality:clean`/`exceptional`), type optional stackable (`type:bug`/`feature`/`docs`/`testing`/`security`/`performance`/`design`/`refactor`/`devops`/`accessibility`).
- Empty stdin (`if not input_data: return`) with no stdout response is a protocol violation — host hangs.
- C# backup services should also use UUID suffixes, not bare timestamps.
- `FileSystemAccessRule` rules must include `InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit` to propagate ACL to child objects.
- All Python plugin PRs share the same `check_installed` shape bug pattern — `{"data": {"installed": bool}}` instead of bare `bool`.
- Quality labels (`quality:clean`/`quality:exceptional`) belong on PRs after review, not on issues at assignment.

## Merged
- **#221** (lazydocker, @AdityaM-IITH) — approved and squash merged. Both review issues fixed (empty stdin, data key). Closes #265.
- **#304** (State fix, @hitdepani) — approved and squash merged. Apply/Revert return bool, SaveState filtered to confirmedApplied. Closes #302.
- **#220** (Scoop plugin, @VIDYANKSHINI) — approved and squash merged. Closes #168.
- **#238** (Docs batch 5, @SarthakKharche) — approved and squash merged. Trailing newlines fixed after 3 flags.
- **#251** (Zed plugin, @YashKrTripathi) — approved and squash merged. All 5 issues + 2 edge cases (empty stdin, block comments) fixed.
- **#253** (State rollback fix, @hitdepani) — approved and squash merged. Closes #249.
- **#267** (curl plugin, @VIDYANKSHINI) — 4 review rounds. Closes #263.
- **#237** (OBS Studio, @Subramaniyajothi6) — All 4 issues fixed.
- **#257** (Bootstrapper tests, @VIDYANKSHINI) — Closes #225.
- **#269** (Docs batch 6, @VIDYANKSHINI) — 2 review rounds.
- **#271** (fzf plugin, @CH-GAGANRAJ) — Zero issues. Closes #265.
- **#272** (mise plugin, @VIDYANKSHINI) — 3 issues fixed. Closes #261.
- **#275** (chezmoi plugin, @VIDYANKSHINI) — Zero issues. Closes #260.
- **#239** (Plugin directory, @ramyacm23) — previously merged.
- **#255** (README expansion, @ishita526) — approved and squash merged. Closes #234.
- **#256** (Package manager docs, @Tharsiga-21) — approved and squash merged. Closes #230.
- **#243** (Dependabot Bump) — previously merged.
- **#177** (LPE fix, @Bhavex) — previously merged. Closes #169.
- **#273** (CliBuilder tests, @mukund58) — approved and squash merged. Clean 58-assertion test suite.
- **#274** (RegistryGuard tests, @sat-06) — rebased from 120 files to 3. Approved and squash merged. Closes #226.
- **#275** (chezmoi, @VIDYANKSHINI) — zero issues. Approved and squash merged. Closes #260.
- **#277** (alacritty, @VIDYANKSHINI) — zero issues. Approved and squash merged. Closes #258.
- **#278** (state clear command, @Anvi-Siddamsetti) — approved and squash merged. Closes #250.
- **#279** (.prettierrc, @VIDYANKSHINI) — approved and squash merged. Closes #247.
- **#281** (CI lint extension, @VIDYANKSHINI) — approved and squash merged. 84 files, +5538/-4240. Ruff + prettier across all 37 plugins. Closes #280.
- **#268** (Docs batch 2, @manishachoudhary11) — approved and squash merged. H1 headers + standard template fixed after 2 review rounds. Closes #229.
- **#241** (YASB, @Tannuu18) — approved and squash merged. Edge-case checklist (10/10) clean. Closes #241.
- **#303** (CHANGELOG.md, @Exodus2004) — approved and squash merged. Closes #248. Accurate entries, clean format.
- **#312** (Model tests, @Achiever199) — approved and squash merged. Clean +1517/0 replacement for #306. Closes #222.
- **#306** (Model tests, @Achiever199) — CLOSED as superseded by #312 (noise diff).
- **#307** (ripgrep, @sat-06) — approved and squash merged. All 13/13 checks pass. Closes #264.
- **#313** (BetterDiscord, @Srishti-Gupta74) — approved and squash merged. All 7 issues fixed. Closes #300.
- **#284** (Joplin, @Akanksha-2712) — CHANGES_REQUESTED. 4 issues still unfixed: missing "data" in responses, dry-run "changed": False, no atomic writes, no corruption backup.
- **#285** (Docs batch 4, @juhi13912-maker) — CHANGES_REQUESTED. Non-standard headings remain (`## Usage examples` lowercase, `## Configuration file location`). Re-review comment sent.
- **#282** (Everything, @Nissy-niveditha21) — CHANGES_REQUESTED. 5/7 fixed. 2 remaining: empty stdin missing `data`/`changed` keys, missing trailing newline.
- **#254** (Sublime Text, @gitsofyash) — new commit fixed UUID backup, atomic writes, empty stdin. 3 remaining: missing `data` key in "not changed", empty stdin, and error response paths. Re-review comment sent.
- **#252** (Backup, @krushnanirmalkar) — new commit fixed UUID + corrupted state backup. Remaining: delete-after-failed-backup edge case (copy fails but delete succeeds = data loss). Re-review comment sent.
- **#242** (PkgMgrAdapter tests, @ramyacm23) — CHANGES_REQUESTED. Noise diff. Stale warning sent.
- **#195** (Flow Launcher, @lover3123) — new commit fixed UUID backup. 3 remaining: empty stdin silent exit, static `.tmp` path, missing isinstance settings guard. Re-review comment sent.
- **#240** (Rainmeter, @kundurukarthik15-gif) — CHANGES_REQUESTED. 5 issues. No new commits since warning.
- **#314** (CI workflow, @sachin-mahato25) — CHANGES_REQUESTED. 1 issue: missing trailing newline in test-plugins.yml. 2 caveats noted.
- **#315** (bat plugin, @ishita526) — CHANGES_REQUESTED → approved → merged. Engine.cs/EngineTest.cs reverted. Closes #259.
- **#316** (IrfanView, @VIDYANKSHINI) — CHANGES_REQUESTED. 8 issues: sys.exit(1), check_installed returns {installed: bool}, dry_run from args not context, no UUID backup, no isinstance guard, missing response fields, unrelated ModelTests.cs changes.
- **#317** (Wallpaper Engine, @DishaKinge27) — CHANGES_REQUESTED. Major rewrite needed: no main() function, no JSON-RPC protocol, dry_run from args, no data/changed fields, check_installed always True, no UUID backup, missing trailing newlines, wrong capabilities.
- **#318** (7-Zip, @Exodus2004) — CHANGES_REQUESTED. Empty stdin silent exit, sys.exit(1), no UUID backup, unrelated EngineTest.cs/ModelTests.cs changes.
- **#319** (Docs batch 1, @A-adilajaleel) — CHANGES_REQUESTED. Non-standard doc template (uses Description/Supported Settings/Example Usage instead of standard sections).
- **#321** (DefaultFileSystem tests, @vedika76) — approved and squash merged. Adds 6 missing IFileSystem methods + tests. Closes #227.
- **#322** (PkgMgrAdapter clean, @ramyacm23) — CHANGES_REQUESTED. Needs rebase onto latest main (conflicts with #321).
- **#323** (Spicetify, @sat-06) — CHANGES_REQUESTED. UUID backup added → approved → merged. Closes #299.
- **#283** (Topgrade, @Akanksha-2712) — CLOSED by author (picked Joplin per one-PR policy).

## Blocked
- @basantnema31: warned for mass-requesting — difficulty upgrade requests on #150/#162 denied. No assignments.
- @ishita526: warned for mass-requesting #259/#264/#266 — must pick one.
- @priyanshi-coder-2: warned for spamming — no assignments until #236 resolved.
- @A-adilajaleel: has #228 assigned, warned for mass-requesting.
- @Pratikshya32, @Exodus2004: warned for mass-requesting #247/#248 — must pick one each.
  - @Pratikshya32: **final warning** — filed 3 more off-topic issues (#308/#309/#310 closed) with no templates/labels after prior warning. Further violations reported to GSSoC.
- @juhi13912-maker: assigned #231, has PR #285.
- @Akanksha-2712: has 1 open PR (#284) — must address remaining 4 issues. Unassigned from #186 (topgrade).
- @gaurav123-4: warned for mass-requesting #287/#291/#301 — assigned #291.
- @sachin-mahato25: PR #314 open — cannot take new work until merged.
- @A-adilajaleel: has #228 assigned, PR #319 open, warned for mass-requesting.
- @Exodus2004: PR #318 open — cannot take new work until merged.
- @Pratikshya32: warned for spamming — no assignments. Final warning issued for off-topic issues #308/#309/#310.
- @gaurav123-4: assigned #291, no PR yet — mass-requesting #287/#310 denied.
- @enoshdev: unassigned from #130/#131 after 1 week with no PR/no response. Two-issue violation.

## Eligible
- @VIDYANKSHINI: 9 merged PRs (#220, #257, #267, #269, #272, #275, #277, #279, #281). Most productive. Needs new issue provided.
- @SarthakKharche: PR #238 merged.
- @YashKrTripathi: PR #251 merged.
- @vedika76: PR #321 merged — eligible for new assignments.
- @Sujith-RMD: 5 merged PRs, strong track record — assigned #292.
- @DishaKinge27: assigned #301.
- @Stewartsson: assigned #262.

## Key Decisions
- All doc files in PRs must end with POSIX trailing newline.
- Static `.tmp` paths for atomic writes should use `tempfile.mkstemp()` instead.
- `chore` type issues use `type:devops` label.
- Full-repo `.gitattributes` line-ending normalization is out of scope for individual PRs.

## New Issues
- **#258** (alacritty terminal, level:beginner) — closed by #277.
- **#259** (bat pager, level:beginner) — unassigned
- **#286** (7-Zip, level:beginner, type:feature) → @Exodus2004 — assigned.
- **#287** (Windows Explorer, level:beginner, type:feature) — unassigned.
- **#288** (Windows Sandbox, level:beginner, type:feature) — unassigned.
- **#289** (IrfanView, level:beginner, type:feature) → @VIDYANKSHINI — assigned.
- **#290** (Go, level:beginner, type:feature) — unassigned.
- **#291** (Postman, level:beginner, type:feature) → @gaurav123-4 — assigned.
- **#292** (nvm-windows, level:beginner, type:feature) → @Sujith-RMD — assigned.
- **#293** (Greenshot, level:beginner, type:feature) — unassigned.
- **#294** (Ditto, level:beginner, type:feature) → @vedika76 — assigned.
- **#295** (Audacity, level:beginner, type:feature) → @Achiever199 — assigned.
- **#296** (SDKMAN, level:beginner, type:feature) → @Srishti-Gupta74 — assigned.
- **#297** (VLC, level:beginner, type:feature) — unassigned.
- **#298** (Dependabot Bump) — previously merged.
- **#299** (Spicetify, level:beginner, type:feature) → @sat-06 — assigned.
- **#300** (BetterDiscord, level:beginner, type:feature) — closed by #313.
- **#301** (Wallpaper Engine, level:beginner, type:feature) → @DishaKinge27 — assigned.
- **#311** (Docker multi-stage, level:intermediate, type:feature) → @mahi-bansal — assigned.
- **#320** (State cleanup bug, level:critical, type:bug) → @hitdepani — assigned.
- **#260** (chezmoi dotfiles, level:beginner) — closed by #275.
- **#261** (mise version manager, level:beginner) — closed by #272.
- **#262** (rustup toolchain, level:beginner) → @Stewartsson — assigned
- **#263** (curl HTTP client, level:beginner) — closed by #267.
- **#264** (ripgrep, level:beginner) — unassigned
- **#265** (fzf, level:beginner) → closed by #271/#221.
- **#266** (Docs batch 6, level:beginner, type:docs) — closed by #269.
- **#270** (CI workflow, level:beginner, type:devops) → @sachin-mahato25 — assigned.

## Assignments
- **#225** (Tests bootstrappers) → @VIDYANKSHINI — closed.
- **#229** (Docs batch 2) → @manishachoudhary11 — assigned (warned to submit PR).
- **#228** (Docs batch 1) → @A-adilajaleel — assigned.
- **#222** (Tests models) → @Achiever199 — closed via #312.
- **#265** (fzf) → @CH-GAGANRAJ — assigned.
- **#227** (Tests DefaultFileSystem) → @vedika76 — assigned.
- **#231** (Docs batch 4) → @juhi13912-maker — assigned.
- **#264** (ripgrep) → @sat-06 — assigned.
- **#248** (CHANGELOG.md) → @Exodus2004 — assigned.
- **#302** (State file bug) → @hitdepani — closed via #304.
- **#300** (BetterDiscord plugin) → @Srishti-Gupta74 — closed via #313.
- **#291** (Postman plugin) → @gaurav123-4 — assigned.
- **#286** (7-Zip plugin) → @Exodus2004 — assigned.
- **#289** (IrfanView plugin) → @VIDYANKSHINI — assigned.
- **#299** (Spicetify plugin) → @sat-06 — assigned.
- **#262** (rustup plugin) → @Stewartsson — assigned.
- **#301** (Wallpaper Engine plugin) → @DishaKinge27 — assigned.
- **#292** (nvm-windows plugin) → @Sujith-RMD — assigned.
- **#295** (Audacity plugin) → @Achiever199 — assigned.
- **#296** (SDKMAN plugin) → @Srishti-Gupta74 — assigned.
- **#311** (Docker multi-stage) → @mahi-bansal — assigned.
- **#320** (State cleanup bug) → @hitdepani — assigned.

## Next Steps
- Monitor open PRs for author fixes after latest flags.
- @Akanksha-2712: 1 open PR (#284) — must address remaining 4 issues. Unassigned from #186 (topgrade).
- @juhi13912-maker: PR #285 reviewed — needs scope clarification and formatting fixes.
- @krushnanirmalkar: waiting on delete-after-failed-backup fix on #252.
- @Achiever199: assigned #295 (Audacity) — mass-request warning resolved.
- @Srishti-Gupta74: assigned #296 (SDKMAN) — new assignment.
- @mahi-bansal: assigned #311 (Docker multi-stage) — new contributor.
- Send reminder to @anishachoudhary5 on #181 (Syncthing) — 5 days, no PR yet, approaching 1-week deadline.
- **#83** (Docs XML comments), **#186** (Topgrade plugin), **#202** (Rainmeter plugin) — reopened after unassigning stale assignees.
