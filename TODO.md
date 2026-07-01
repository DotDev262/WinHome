# TODO

- [ ] Inspect failing import path in `plugins/yarn/test/test_yarn.py` and align with `plugins/yarn/src/plugin.py`.
- [ ] Remove any `src.plugin` imports and patch tests to patch/match the correct module path.
- [ ] Run yarn plugin test suite and verify no `AttributeError: module 'src' has no attribute 'plugin'` remains.

