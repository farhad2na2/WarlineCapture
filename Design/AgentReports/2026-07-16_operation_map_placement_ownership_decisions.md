# Operation Map Placement Ownership Decisions

Date: 2026-07-16
Status: Accepted shared-foundation decisions
Evidence: `2026-07-15_opmap-006_phase0_placement_ownership.json`
Tracker: `../Architecture/operation_map_scene_split_and_generator_tracker.md`

## Decision Rule

- The current building/vehicle placement config assets and their authoring roots are map-owned compatibility content because every entry is expressed in the current map's world space and source hierarchy.
- Runtime spawning, canonical-source hiding, entity ownership, and teardown are shell/runtime policy operating on the selected map's placement data.
- Scenario-owned starting-force data must eventually reference typed map anchors; it must not mutate the preserved current-map placement baseline during extraction.
- A hierarchy path is evidence, not stable identity, when multiple entries or scene candidates share it.

This decision is independent of map loading and generation technology.

## Config Decisions

| Config | Accepted classification | Required migration disposition |
|---|---|---|
| `Match_MapBuildingPlacement_Config.asset` | `MapOwned` | Preserve asset and `.meta` GUID; register it with the current operation-map identity; move/copy its authoring root in the staged map; prove all `451` complete placement identities before removing Match compatibility fields. |
| `Match_MapVehiclePlacement_Config.asset` | `MapOwned` | Preserve asset and `.meta` GUID; register it with the current operation-map identity; move/copy its authoring root in the staged map; prove all `29` complete placement identities before removing Match compatibility fields. |

The existing asset paths may remain unchanged during compatibility registration. Physical relocation is optional and must not be mixed with the scene split unless GUID, reference, and rollback parity are proven.

## Duplicate Source-Path Decision

The `49` building groups covering `152` entries and `5` vehicle groups covering `10` entries are classified `TemporaryCompatibility`, not `Unresolved` ownership.

Before atomic cutover, the migration must create a deterministic mapping for every placement entry using the complete accepted identity:

```text
config asset GUID/local id
placement kind and faction
prefab GUID/local id/type
source-path group plus occurrence/candidate identity
position, rotation/yaw, and scale
old scene GUID/object local id -> staged scene GUID/object local id
```

Rules:

1. Never hide or delete a source renderer/object using source path alone.
2. Singleton groups may map directly only when complete identity and transform agree.
3. Duplicate groups must map each config entry to exactly one staged candidate.
4. Zero, multiple, or reused candidates fail the cutover.
5. Preserve the old authoring hierarchy until spawned-entity parity and source-hiding parity pass.
6. Record the mapping hash and all rejected/ambiguous counts in migration evidence.

## Acceptance Consequences

The historical probe remains `NeedsDecision`; this record resolves both config decisions and the grouped path ambiguity. All `480` placement entries now have accepted ownership and migration disposition.

This closes inventory only. It does not authorize moving roots, rewriting configs, hiding old sources, or changing scenario starting forces.
