# M1/M2 Playability and Button QA — 2026-09-02

## Outcome

Automated M1/M2 playability and UI interaction gate: **Passed**.

- 28 integrated suites passed.
- 10 player-facing prefabs were scanned.
- 140 `Button` components have a live `targetGraphic`, an enabled raycast target, and an in-button hierarchy target.
- M1 victory Continue and loss Retry callbacks invoke exactly one bound action.
- M2 campaign launch, placement, production, guidance, Do It, lifecycle, resources, barracks production, delayed wave, victory, settlement, and result UI passed.

## Defects found and fixed

1. Pause popup actions had procedural gradient targets with raycasts disabled. The builder now enables pointer targets for every Pause button and validates them after generation.
2. The First Launch prefab and Menu scene had been generated before the responsive comic layout controller was added. The V3 prefab was rebuilt and reinstalled into Menu.
3. Match HUD command roots contained invisible dead zones. All eight command-rail actions and contextual squad actions now keep a full-rect invisible hit surface in addition to their procedural gradient target.
4. Board was still tested against an obsolete rail layout. The V3 rail remains the approved eight-command layout; Board is validated as a contextual squad action without creating a duplicate rail command.
5. Contextual camera/Board interaction states still assumed legacy sprite-swap art. They now retain procedural V3 gradients and use immediate V3 color states for hover, press, selection, and disabled feedback.
6. Settings focus overlays used a nine-slice PPU multiplier of 1. The shared V3 factory now authors multiplier 2, and the Settings builder validates every sliced image against that sharp-border rule.
7. M2 guidance could retain the disabled legacy right-rail Build reference after the V3 footer was installed. ARIA now resolves and rebinds the visible V3 footer Build command, while the footer input binding remains the owner of opening the drawer. A dedicated regression invokes the serialized `CommandRail/BuildCommand` and requires exactly one drawer-open request.
8. M2's mission-scope presentation was removing Support and unavailable squad-category buttons from the hierarchy. Mission restrictions now preserve every authored button position, keep unavailable controls non-interactable and fully opaque, and apply the shared `Warline/UI/Disabled Grayscale` material used by M1. The active V3 footer command rail now receives the same restriction state, so Support stays visible-gray while M2's required Build action stays visible and enabled.

## Final validation evidence

### Extended mission and UI gate

Log: `/private/tmp/warline-qa-m01-m02-playable-final.log`

Required marker:

```text
[M01M02PlayableButtonQaValidation] result=Passed suites=28 buttonPrefabs=10 directInteractions=3
```

Important included markers:

```text
[M01FirstContactDeterministicGameplayValidation] result=Passed cases=10 repeats=2
[M01FirstContactRuntimeOwnershipValidation] result=Passed tests=13
[M01FirstContactSettlementValidation] result=Passed tests=15
[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=15 v3FooterBuildDrawer=Passed
[SettingsPopupValidation] result=Passed tests=10
[M01FirstContactHudResultValidation] result=Passed tests=12 captures=3 pointerTargets=Passed
[M02EstablishBaseLaunchValidation] result=Passed tests=18
[M02EstablishBaseObjectiveValidation] result=Passed tests=26
[M02EstablishBaseGuidanceValidation] result=Passed tests=42
[M02EstablishBaseDoItValidation] result=Passed tests=10 routes=5 v3FooterBuild=Passed lateBinding=Passed
[M02EstablishBaseBuildCatalogValidation] result=Passed tests=10
[M02EstablishBaseLifecycleValidation] result=Passed tests=15
[M02EstablishBaseResourceValidation] result=Passed tests=9
[M02EstablishBaseBarracksProductionValidation] result=Passed tests=6
[M02EstablishBaseWaveValidation] result=Passed tests=9
[M02EstablishBaseResultSettlementValidation] result=Passed tests=15
[M02EstablishBaseHudResultValidation] result=Passed tests=7
```

### Exact button inventory

Log: `/private/tmp/warline-qa-m01-m02-button-inventory-final.log`

```text
[M01M02ButtonInventoryValidation] result=Passed prefabs=10 buttons=140 pointerTargets=Passed
```

### Focused regenerated-prefab validation

```text
[PauseOptionsV3PrefabTests] result=Passed tests=4 gradients=procedural borders=3 restart=queued help=interactive
[MatchHudV3PrefabBuilder] validation=Passed commands=8 squads=5 gradients=78 pointerTargets=44 passengers=10-slots rope-drop=bound aria=minimap-attached art=aspect-preserved
[SettingsPopupPrefabBuilder] validation=Passed layout=vertical-tabs activePages=1 gradients=29 images=96 buttons=30 uniqueSprites=8
[FirstLaunchNarrativeV3PrefabBuilder] result=Passed screens=5 layout=1672x941 gradients=procedural borders=3 atlases=shared
```

## QA boundary

This is the automated deterministic/editor runtime gate. A command-line Unity Test Framework Play Mode attempt was not counted because the mandatory macOS GUI-licensing wrapper exited without producing its requested NUnit XML. No direct Unity executable or batchmode fallback was used. A physical-device/manual touch playthrough remains a separate release-candidate QA step.
