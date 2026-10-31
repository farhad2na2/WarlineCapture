# S073 legacy startup readback inputs

`retained/` contains the exact source-only JSON inputs consumed by
`Tests/generate_s073_legacy_config_readback.py`. Their SHA-256 values and
historical producer lineage are recorded in `retained/provenance.json`. The
source identity and building definition records came from retained Unity
6000.5.8f1 readbacks; they are preserved byte-for-byte, not reconstructed from
names or candidate data.

## Publication dependency closure

The reproducible generator packet consists of:

- `Tests/generate_s073_legacy_config_readback.py`.
- `Recipes/selected_operation_map/helpers/read_source_configs.rb`, pinned in
  `retained/provenance.json`; it uses Ruby's standard `yaml`/Psych and `json`
  libraries. Validated here with Python 3.9.6 and Ruby 2.6.10.
- All six files under `retained/`, including the original
  `InitialForces.baseline.asset` used by the parity control.
- Current project sources pinned by the retained audit and identity records:
  the 12 `configuration_roots` in `retained/source-asset-reference-audit.json`
  and the 22 `Assets/Game/Prefabs/` files in
  `retained/source-object-names.json`. The generator requires all of these to
  match their retained hashes, except current `InitialForces.asset`, whose
  source change is the explicit readback subject. It also checks the pinned
  `Construction.asset` definition source.

The generator itself uses Python's standard library only. It does not require
Unity, a source build receipt, the owner harness, or captured runtime geometry.
The separate original-owner harness validation has those additional
dependencies and is not part of the minimal readback packet.

Regenerate current config, identity, audit, and rebound definitions from the
current Unity YAML using:

```sh
python3 Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_legacy_config_readback.py \
  --repo "$PWD" \
  --output-dir /private/tmp/s073-source-config-readback-new
```

The output directory must be new. The generator re-parses the current
`InitialForces.asset` and `UnitRegistry.asset`, checks retained source roots and
prefab pins, and updates only the config provenance pins in the retained
building definitions. The readback does not establish Unity startup, placement
source fit, unit spawning, visual readiness, mission outcome, or readiness.

Run the dependency controls with:

```sh
python3 Tools/CI/SkirmishSourceSimulation/Tests/run_s073_legacy_config_readback_controls.py
```

The controls run `--verify-retained` against a temporary source overlay that
uses the byte-pinned original `InitialForces.asset`, then alter a temporary copy
of the retained identity JSON and require its provenance hash check to reject
it. Neither control edits the project source.
