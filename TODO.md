- [ ] Implement maintainer-requested changes in plugins/yarn/src/plugin.py
  - [ ] Set dry_run using args only: dry_run = bool(args.get("dryRun", False))
  - [ ] Update apply_config signature to def apply_config(args: dict, request_id: str) -> dict:
  - [ ] Remove all usage of context variable in apply_config
  - [ ] Update main() to call apply_config with new signature
- [ ] Update plugins/yarn/test/test_yarn.py
  - [ ] Import main via: from plugin import main
  - [ ] Update all @patch("src.plugin._get_user_home") to @patch("plugin._get_user_home") (verify none remain)
- [ ] Run plugin/yarn tests (if available) to ensure changes work

