# Dense City Phase 0A Building Attachment Ownership Inventory

## Scope

- Tracker item: inventory every visual attached to each current damageable building; assign each stable identity to exactly one building and one intact/destroyed state without proximity or name inference.
- Probe: `Assets/Game/Scripts/Editor/OperationMapBuildingAttachmentOwnershipInventoryProbe.cs`
- Focused tests: `OperationMapBuildingAttachmentOwnershipInventoryProbeTests` — 6/6 passed
- Full report: `Design/AgentReports/2026-07-21_dense_city_phase0a_building_attachment_ownership_inventory.json`
- Summary: `Design/AgentReports/2026-07-21_dense_city_phase0a_building_attachment_ownership_inventory_summary.json`

## Result

`AttachmentOwnershipInventoryComplete`

| Metric | Count |
|---|---:|
| Building placements | 432 |
| Exact joins / unresolved / reused | 432 / 0 / 0 |
| Intact renderer attachments | 1,324 |
| Destroyed prefab renderer attachments | 272 |
| Orphans under Buildings root | 0 |
| Shared across buildings | 0 |
| Dual-state conflicts | 0 |
| Placements with destroyed visual prefab | 266 |

## Ownership rules used

- **Intact:** renderer descends from an exact-joined building source (`ExactBuildingSourceAncestor`).
- **Destroyed:** renderer is under the placement’s configured `destroyedVisualPrefab` (`ConfiguredDestroyedPrefabReference`). Shared prefab assets are allowed; claim uniqueness is `(ownerSourceGlobalObjectId, prefabGuid, localId)`.
- **No** name, proximity, mesh likeness, or role labeling (roof/shop/lamp/etc.).

## Implication

Attachment ownership for current buildings is identity-complete and conflict-free. Role/semantic labels remain out of scope until generator/authoring contracts introduce them. Building ECS conversion still depends on the managed `RuntimeBuildingEntity` dependency inventory and remains a GPT mid-point mutation gate.

## Non-mutation guarantee

No scene, SubScene, Addressables, presentation-mode, or asset mutation in this slice.
