# Executed owned Python source bytes

Use the direct source CLI/preparation script entry points. They bootstrap the
finite owned loader from the exact bytes read and hashed, so valid old pyc files
cannot supply older helper code. The loader explicitly admits Syntax,
ProductionWorker, CausalWorker, Manifest and Build in that order; placement then
admits its numeric generator and builder. Standard-library imports are unchanged.

Each module executes the same captured bytes whose canonical path/SHA enter the
normal resolution/build/run/preparation receipts and full verification barriers.
Actual package origins are recorded even when `--repo` and `--package-root` differ.
The loader itself is included in the captured-byte pin map. No pyc files are
deleted and no general Python import hook is installed.

An owned module can be reused only in the same registered source epoch. Source,
binding or path drift fails closed; failed execution cleans its module binding and
poisons the epoch so it cannot later qualify. After editing an owned module, start
a fresh ordinary CLI process. There is no historical source-hash whitelist.

Run the finite protocol suite through the same qualified bootstrap:

```sh
python3 Tools/CI/run_skirmish_source_tests.py --repo /absolute/source-repository
```

Tests use TEMP protocol/source fixtures, not synthetic gameplay outcomes. Native
source gameplay, normal input/device acceptance and standalone runtime behavior
remain separate from Python helper loading fidelity.
