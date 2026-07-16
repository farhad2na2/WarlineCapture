# Current Operation Map Placement Registration

Date: 2026-07-16
Status: Accepted loader-neutral compatibility registration

## Operation Map

- Operation-map id: `opmap.skirmish.desert_base_01`
- Current source scene: `Assets/Game/Scenes/Match.unity`
- Current source scene GUID: `cc4f48a57793d4597b4ffac2906c515e`
- Authoritative complete-entry evidence:
  `2026-07-15_opmap-006_phase0_placement_ownership.json`
- Accepted ownership decision:
  `2026-07-16_operation_map_placement_ownership_decisions.md`

## Registered Placement Products

| Kind | Map-owned config | GUID | Count | Current authoring root | Identity aggregate SHA-256 |
|---|---|---|---:|---|---|
| Building | `Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset` | `e859aa1a53b0942609e537713fd55fb7` | 451 | `Map[10]/Buildings[18]` | `87a26e3d33214e942e0075e461d66a91a45e0735bfe51455bb140c695149f65b` |
| Vehicle | `Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset` | `03d5c67074cde47488712cef0e5f494a` | 29 | `Map[10]/Vehicles[20]` | `9d2ec4c8c563e7692efe51d3fd879bcf2d9ff2df015cf41730f11a6b75c4a065` |

Both configs currently use `spawnOnMatchStart=true` and
`hideAuthoringVisualsAfterSpawn=true`.

## Source-Path Contract

The authoritative JSON records every complete placement entry and source path;
this registration does not duplicate those 480 rows. Duplicate hierarchy paths
remain `TemporaryCompatibility` and are not unique identity. Migration and
parity use the complete accepted identity tuple and the aggregate hashes above.

## Migration Rules

- Both config assets and all 480 placement identities are `MapOwned` by
  `opmap.skirmish.desert_base_01`.
- Preserve each asset `.meta` GUID and current runtime behavior.
- Existing asset paths may remain unchanged during compatibility registration.
- Do not remove current `MatchSceneView` bindings or authoring roots before the
  staged scene passes exact placement and source-hiding parity.
- This record selects no scene loader, Addressables layout, generator, or remote
  delivery policy.
