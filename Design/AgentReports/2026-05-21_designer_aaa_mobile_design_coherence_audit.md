# Designer Audit: AAA Mobile Design Coherence

Date: 2026-05-21
Lane: Designer
Status: Fixed

## Scope

Audited the active game-design stack for two questions:

1. Is WarlineCapture aligned with a credible AAA mobile game design target?
2. Do the active design documents relate cleanly to each other from high-level direction down to child implementation docs?

Primary docs reviewed:

- `README.md`
- `Design/README.md`
- `Design/GAME_DESIGN_REFERENCE.md`
- `Design/AAA_Mobile_Game_Design_Document_v0_1.md`
- `Design/3D_SingleMap_Gameplay_Direction.md`
- `Design/Gameplay_North_Star_And_Content_Grammar.md`
- `Design/Level_And_Mission_Content_Plan.md`
- `Design/LargeScale_Grid_Movement_Design.md`
- `Design/UIUX_Gameplay_Element_Alignment.md`
- `Design/UIUX_MainMenu_Visual_Contract.md`
- `Design/Architecture/performance_regression_contract.md`
- active Chapter, FTUE, M01, UI, economy, monetization, and project-state docs.

## Verdict

WarlineCapture is conceptually aligned with an AAA mobile RTS direction at the source-of-truth layer. The strongest alignment is:

- one full 3D operation map per mission or operation
- Campaign / Operations / Skirmish product structure
- proactive field-commander fantasy
- hostile cells embedded in civilian towns
- civilian safety and infrastructure consequence as differentiators
- command-base main menu art direction
- config-backed roster source under `Assets/Game/Configs/Prefabs`
- mobile readability and performance explicitly called out as acceptance gates
- economy and monetization guardrails that avoid pay-to-win objective completion

However, the documentation stack is not fully coherent yet. Several active child docs still contain old 2D/isometric, `IsoMapId`, Saga, Persistent Operation, and Quick Custom assumptions. This creates implementation risk because agents following child specs can still build against the previous direction even though the root docs are now correct.

## Findings

| Priority | File / Lines | Finding | Impact | Required Fix |
|---|---|---|---|---|
| P0 | `Design/Gameplay_Features_High_Level_Spec.md:94-101` | The high-level gameplay feature spec has a `3D Single-Map Gameplay Alignment` heading, but the body still says configs should bind to 2D isometric map ids, `IsoMapId`, `TacticalMapDefinition`, ISO-01 readability, a 2D isometric track, and macro-tile terrain. | This directly contradicts the active 3D direction and can mislead gameplay implementation. | Rewrite this section around `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, 3D metadata, and 3D operation-map validation. |
| P0 | `Design/Gameplay_Features_Detailed_Spec.md:34-45` | The detailed gameplay implementation spec still has `2D Isometric Implementation Rules` and `Macro-Tile Terrain Implementation Rules`, including `IsoMapId`, `TerrainSetId`, `TacticalMapDefinition`, and 2DISO asset paths. | This is the clearest child-spec conflict with the new direction. | Replace with 3D operation-map implementation rules and move old assumptions to archive context if still needed. |
| P0 | `Design/M01_FirstContact_Production_Contract.md:37-40, 165, 227-230` | The M01 contract partially uses `OperationMapId`, but still requires `LevelId`, `MapPreviewArtId`, `MinimapArtId`, says FTUE must resolve to the strategic/tactical map, and validation still checks `IsoMapId` / `TacticalMapDefinition`. | M01 is the active playable-slice gate, so this inconsistency can block QA/HCI and runtime work. | Make M01 use one complete operation-map contract: `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, 3D anchors, runtime markers, and operation-map metadata. |
| P0 | `Design/FTUE_And_Command_Assistant_Design.md:85-89, 329, 438-459` | FTUE still routes the first launch through `Saga Campaign`, `Saga Map`, and strategic/tactical map intent using `IsoMapId` / `TacticalMapDefinition`. | The early game teaching flow is not aligned with Campaign naming or the single-map model. | Rename player-facing FTUE flow to Campaign, and update ARIA map intents to operation-map anchors/camera states. |
| P1 | `Design/UIUX_Implementation_High_Level_Spec.md:33-46, 206, 220-228, 420, 436-442` | UI high-level spec still frames modes as Saga Map Campaign, Persistent City Operation, and Quick Custom Game, and still asks gameplay-facing screens to use 2D isometric references. | UI implementation may regress to old mode names and old content art. | Update mode names, implementation order, and content-art rules to Campaign / Operations / Skirmish and 3D command-base / operation-map content. |
| P1 | `Design/UIUX_Screen_Popup_Implementation_Spec.md:23-30, 69-82, 180-190, 507-512` | The spec has a legacy warning, but it remains in the active reading order while its body still contains old mode labels, old resource labels, and old art language. | The warning helps, but the active reading order still invites agents to consume stale section details. | Either move this spec to archive and replace it with a clean active UI screen inventory, or rewrite the body sections for the new direction. |
| P1 | `Design/SagaChapters/README.md:35` | Chapter update rules still say every tactical mission must resolve strategic and tactical map ids including `IsoMapId`. | Chapter authors can create new mission specs against the old map contract. | Replace with `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, and operation-map metadata anchors. |
| P1 | `Design/README.md:21` | Design index still summarizes the gameplay layer as planned around `Saga, Operation, AI profiles`. | Minor but visible contradiction in the source index. | Change this summary to Campaign, Operations, Skirmish, AI profiles, and encounter templates. |
| P1 | `Design/Architecture/performance_regression_contract.md:36-44` | Performance contract requires structured metrics and platform-aware budgets, but the design stack does not yet define concrete AAA mobile targets for target devices, frame rate, unit counts, draw/marker budgets, or thermal/session constraints. | The AAA mobile promise is stated, but production-scale validation lacks numeric target bars. | Add an `AAA Mobile Technical Targets` section or doc with device tiers, FPS targets, max visible units/markers, memory budgets, capture matrix, and soak duration. |
| P2 | `Design/Art_Asset_Requirements_Register.md:107` | Art register still mentions `SagaStars` as a reward icon/tile treatment. This may be an internal economy term, but player-facing docs now prefer Campaign. | Low risk if internal, but polish risk for UI/resource naming. | Clarify internal vs player-facing naming for Campaign stars. |
| P2 | `Design/UIUX_Mockup_To_Canvas_Conversion_Plan.md:67` | The canonical target inventory still uses the old folder path `SCN-13_QuickCustomGameSetup` for Skirmish. | Acceptable as a path compatibility note, but needs a clear player-facing label guardrail. | Keep path if runtime/assets need it, but label all visible UI as Skirmish and note the path is legacy-compatible. |

## AAA Mobile Evaluation

| Area | Current Rating | Notes |
|---|---|---|
| Core fantasy | Strong | The field-commander framing, hostile cells in civilian towns, and district consequences create a clear differentiator. |
| Mode structure | Strong at source layer, inconsistent in child docs | Campaign / Operations / Skirmish is clean. Old Saga / Persistent / Quick Custom labels remain too active in implementation docs. |
| Gameplay loop | Strong | Briefing -> intel/loadout -> 3D operation -> result/reward/consequence is a credible mobile RTS loop. |
| Mission grammar | Strong | Archetypes, threat families, star rules, reward pacing, and balance bands are coherent. |
| 3D single-map direction | Strong | The design clearly rejects separate strategy/tactical maps and defines camera/UI states on one world. |
| Mobile readability | Good but incomplete | Readability rules exist. Needs concrete quantitative targets for unit counts, marker counts, camera distances, text sizes in HUD, and device tiers. |
| Performance readiness | Good process, incomplete budgets | Performance regression process is sound, but production AAA targets are not quantified yet. |
| UI/UX direction | Strong at Main Menu, mixed in older specs | Main Menu command-base target is strong. UI child specs still contain old mode/art assumptions. |
| Economy/monetization | Good | Strong deterministic reward and no objective/star purchase guardrails. Resource display naming still needs careful Campaign/Operations/Skirmish polish. |
| Documentation hierarchy | Improved but not complete | README tree and archive separation are good. Several active child docs still contradict parents. |

## Relationship Map Check

The intended source chain is now valid:

```text
README
-> Design/README
-> GAME_DESIGN_REFERENCE
-> AAA GDD / 3D Single-Map Direction / Gameplay North Star
-> Level & Mission Plan / UI Element Alignment / Combat & Economy
-> Chapter docs / M01 contract / UI implementation / FTUE / balance
```

The problem is not the tree. The problem is that some leaves still contain previous-branch content. The highest-risk leaves are gameplay feature specs, M01, FTUE, UI high-level implementation, and the legacy UI screen/popup spec.

## Recommended Fix Order

1. Rewrite `Gameplay_Features_High_Level_Spec.md` and `Gameplay_Features_Detailed_Spec.md` around 3D operation maps.
2. Fix `M01_FirstContact_Production_Contract.md` so M01 has one operation-map contract and no `IsoMapId` validation.
3. Update `FTUE_And_Command_Assistant_Design.md` to Campaign naming and operation-map anchors.
4. Update `UIUX_Implementation_High_Level_Spec.md` and decide whether `UIUX_Screen_Popup_Implementation_Spec.md` should be rewritten or archived.
5. Patch `SagaChapters/README.md` and the few remaining source-index wording issues.
6. Add concrete AAA mobile technical targets for performance/readability validation.

## Bottom Line

The game design direction is AAA-mobile credible, and the 2026-05-21 fix pass has aligned the highest-risk implementation-facing docs with the full 3D single-map direction.

## Fix Pass Completed

Completed after the audit:

- Rewrote gameplay feature specs around `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, operation-map metadata, Campaign, Operations, and Skirmish.
- Updated M01, FTUE, assistant, balance, and gameplay/UI handoff contracts to resolve 3D operation-map fields instead of `IsoMapId`, preview-art, tactical-map, or split strategic/tactical assumptions.
- Updated Chapter 1 mission content to use `opmap.*` ids, planning cameras, and minimap projections.
- Added `AAA_Mobile_Technical_Targets.md` and linked it from the README tree and Design index.
- Archived the old screen/popup spec, UI codex package, and immediate UI phase plans under `Design/Archive/LegacyUI_2026-05-21/`.
- Updated art, audio, economy, monetization, VFX, and UI implementation docs so player-facing terms are Campaign, Operations, Skirmish, Campaign stars, and 3D operation map.

Residual acceptable legacy terms are now limited to explicit runtime/storage compatibility notes, legacy folder paths, or archive references.
