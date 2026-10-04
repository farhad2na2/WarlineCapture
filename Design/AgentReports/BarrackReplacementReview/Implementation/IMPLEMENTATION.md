# Approved masonry Barrack — implementation

2026-10-04. Owner selected B, reviewed the native compound and explicitly approved replacement.

## Installed assets

Authoring checkout: `/Volumes/T7/Projects/WarlineCapture-MenuUiUx`.

- Existing `Assets/Game/Prefabs/Buildings/Building_Barrack.prefab`: same GUID 683195b1e21c94df7add42ab26cd2a33 and root fileID, with the approved compound beneath its preserved Model root.
- `Assets/Game/Prefabs/Buildings/Visuals/BarrackMasonry/Barrack_Masonry_Intact.prefab`: approved B plus service office, generator, medical stores, supplies, foundation and muster markings, linked to vendor source prefabs. Project materials copied from the approved candidate; no runtime dependency on the preview folder.
- `Assets/Game/Prefabs/Buildings/Visuals/BarrackMasonry/Barrack_Masonry_Ruins.prefab`: dedicated charred masonry remnants and rubble, matching the footprint. Shared generic city destruction asset stays unchanged.
- Barrack gameplay config: only destroyedVisualPrefab changes. Existing cost 40,000 credits / 90 materials, 1,200 health, 30-second production and recipe of four soldiers stay unchanged.
- `Assets/Game/Scripts/Editor/BarrackMasonryPrefabBuilder.cs`: bounded native installer plus focused visual lifecycle validation. It requires Edit mode and preserves active scene/dirty state.

The source Model's old mesh/components and wooden children were removed through PrefabUtility; its transform is identity for the new grounded compound. The gameplay root, source GUID/local file identity and configured footprint remain stable. Visual attachments have no extra authoring or physics colliders. Shader/material and mesh references pass checks. Native bounds remain inside 28 × 15; runtime visual-derived occupancy still resolves exactly 28 × 15.

## Evidence

`invoke_unity_macos.sh --reuse --timeout 300` exited 0. Full log and dispatch receipt are saved here.

- [BarrackMasonryReplacement] result=Passed assetIdentity=Preserved footprint=28x15 gameplay=Preserved genericCityRuins=Preserved runtimeCreateDestroyCleanup=Passed productionExit=OutsideFootprint activeScene=Preserved
- [BarrackMasonryRuntime] result=Passed create=Native destruction=Native cleanup=Native production=4Soldiers exits=Outside28x15 footprint=28x15 deviceAcceptance=Pending fullMission=NotClaimed
- [ExistingEditorValidation] result=Passed executeMethod=Game.Editor.BarrackMasonryPrefabBuilder.BuildAndValidate

The focused check uses actual runtime definition metadata and visual creation/destruction/cleanup helpers in an isolated preview scene. It verifies the existing recipe and computed spawn exits outside occupancy; it does not execute a timed player-input production cycle or a complete mission. Native installed/destroyed captures were visually reviewed. Source controls/configs and unrelated generic city ruins are preserved.

## Remaining gates and checkout state

The main checkout was running a Match validation when inspected; its Editor session and production assets were left untouched. This replacement is authored in T7. Only review evidence is copied to the main report directory. No commit/push/merge was requested this turn.

Intact renderer count changes from 6 to 18. Device frame-time/material cost acceptance and full normal-input mission interaction/production/destruction/return remain pending. This edit replaces the shared Barrack prefab. Independent copied or generated authored-map presentation content is not broadly rebuilt by this narrowly scoped installer; those consuming map revisions require their owning source/build and readiness evidence before publication. Existing prefab/config references keep resolving through the preserved Barrack identity.
