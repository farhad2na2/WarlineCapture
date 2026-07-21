# Phase 0A Shared Art Ownership Proof

Result: `SharedArtOwnershipProven`

| Metric | Count |
|---|---:|
| Sources | 11892 |
| Unique mesh assets | 670 |
| Mesh placement references | 11892 |
| Repeated mesh assets | 621 |
| Unique material assets | 39 |
| Material references | 11989 |
| Repeated material assets | 32 |
| Unique prefab assets | 671 |
| Prefab placement references | 11892 |
| Repeated prefab assets | 621 |
| Missing assets | 0 |

## Notes
- Each mesh/material/prefab GUID resolves to one AssetDatabase package path.
- Placement rows contribute only transform/render-reference identity; art bytes are owned once per GUID.
- Does not mutate scenes, SubScenes, Addressables, or OperationMapPresentationKind.
