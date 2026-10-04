# Enemy Barrack C — implementation

2026-10-04. Owner explicitly requested option C for enemy Barracks after approving player option B.

## Behavior and authoring

Authoring checkout: `/Volumes/T7/Projects/WarlineCapture-MenuUiUx`. Main production assets and its running Editor session remain untouched; only review evidence is copied there.

The shared Barrack remains one gameplay definition with its original asset identity and settings. Its Model wrapper now binds B and C as mutually exclusive visual children. Only enemy/hostile ownership selects C; player, neutral and unowned presentation uses B. The native C source is `Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_House_01.prefab`, positioned within the same service compound at its original scale. Both resolve to the original 28 × 15 footprint. No second producer, extra colliders, ownership entity, cost, health or production change is added.

`BuildingFactionVisualVariants` is an opt-in MonoBehaviour holding art references and activation only, with no polling/Update. Runtime consumes its `IBuildingFactionVisualVariants` contract through the existing ownership/faction presentation path. It caches the selector with building renderers, toggles art on ownership events, and selects per-instance enemy ruins. The shared definition's player destruction asset is never mutated. Destroyed buildings do not activate new live art during owner changes. Other buildings without this binding keep their existing presentation.

New project-owned enemy art:
- `Assets/Game/Prefabs/Buildings/Visuals/BarrackMasonry/Barrack_Masonry_Enemy_C.prefab`
- `Assets/Game/Prefabs/Buildings/Visuals/BarrackMasonry/Barrack_Masonry_Enemy_C_Ruins.prefab`

The player native builder preserves an already installed enemy binding and C child when rebuilding B.

## Validation

Two GUI wrapper runs exited 0; full logs and dispatch receipts are here.

1. `EnemyBarrackPrefabBuilder.BuildAndValidate`: original identity/config unchanged; C source/ruins fit; native runtime initialization selects enemy C, ownership switches to B, neutral/unowned use B, hostile faction 3 uses C, enemy-specific destruction and cleanup work, destroyed ownership changes do not resurrect art, a fresh player using the same definition remains B, and pooled enemy instances reused by the player return to B. Exactly 18 active renderers in each intact variant, never both sets active together. Soldier recipe and spawn exits remain preserved.
2. `EnemyBarrackPrefabBuilder.ValidatePlayerRebuild`: reruns the player native installer, then repeats ownership/lifecycle validation; C survives player rebuilding.

Pass markers include:
- [EnemyBarrackNative] result=Passed player=B enemy=C hostileFaction3=C neutral=B capture=B poolReuse=B sharedDefinition=Unchanged destruction=PerOwner cleanup=Passed noResurrection=Passed footprint=28x15 fullMission=NotClaimed deviceAcceptance=Pending
- [EnemyBarrackInstall] result=Passed enemy=C player=B identity=Preserved settings=Preserved activeScene=Preserved
- [EnemyBarrackRebuild] result=Passed playerRebuild=EnemyVariantPreserved
- [ExistingEditorValidation] result=Passed for each entry point

Native enemy, player and enemy destruction captures were visually reviewed. C# compilation passes after introducing the contract across the established runtime/rendering assembly boundary; the initial compile error is retained in CompilationInitialFailure.txt. Focused C# diff whitespace checks pass; Unity's native prefab serialization includes an empty `m_Name: ` trailing space on the newly serialized MonoBehaviour. It is recorded rather than hand-editing the prefab YAML.

## Acceptance limits

These are focused runtime-helper and native art checks in isolated preview scenes, not a normal-input complete mission or device run. Full mission/player/device acceptance remains pending. Both mesh variants are resident dependencies although only one renders; device memory/material cost requires measurement. Independently copied/generated authored-map visual owners are not broadly rebuilt by this prefab installer, as documented in the prior B implementation report; those map revisions still need their owner-specific rebuild/readiness lane. No commit, push, merge or main asset synchronization was requested this turn.
