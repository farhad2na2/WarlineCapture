# Generic source-map clearance overlay

`preflight.py` admits a `SourceDerivedCampaignMapPackage` and writes a compact
`SourceDerivedCampaignMapDropClearanceProjection`. The projection carries the
capture path and hash, map definition pin and operation map ID, selected grid
frame, original collector authority, and collector query rectangle. The C# host
overlay reopens the hash-pinned capture and obtains the complete bounds rows
from `fixedMapReadDomain.dropClearanceBounds`; it does not copy those large rows
into the compact projection. Admission requires the complete `Reserve` query
domain for every on-grid preferred cell. It follows the original float32
operation order and `safe = max(0.01f, cellSize)`: low edge is
`origin + (preferred - 98) * safe`, then high edge is `xMin + 197 * safe`.
This preserves behavior for grid cells below `0.01f` and avoids replacing the
original evaluation sequence with an algebraically equivalent expression.

`build_controls.py` creates a disposable copy of the current
`GeneratedS002DropClearance`, inserts support for that projection kind, and
compiles it using the checked-in S026 source-host response. The existing
`Reserve` implementation is preserved byte-for-byte. A future staged host
admission helper can pass the preflight projection path and SHA-256 to the
existing clearance constructor, then pass that clearance object through the
existing placement pipeline. This recipe does not edit the canonical manifest
or runtime source.

Run the controls with a fresh output directory inside this recipe:

```sh
python3 Tools/CI/SkirmishSourceSimulation/Recipes/source_map_clearance/build_controls.py \
  --output Tools/CI/SkirmishSourceSimulation/Recipes/source_map_clearance/out-rN
```

The controls only construct the clearance object and validate its source
envelope. They do not call `Reserve`, create a world, activate presentation,
run a mission, or claim occupancy, native parity, fit, or readiness.
