# S002 first expanded ground slice

Catalog **S002** — Desert Base · Base Assault · Ground Maneuver · Established Base.
Handoff ordinal 4. First visit: Regular / Standard. Seed sample: `104731`.

## What landed

### SK-00
- New `Game.Skirmish.Contracts` assembly (`noEngineReferences`, auto-referenced).
- Typed IDs/enums for catalog/definition/setup, size, difficulty, army, start,
  objective, roles, outcomes, checkpoint header, and reason codes.
- Legacy prototype map helper (0/1/3 + reserved stress 2).

### SK-01
- Twelve config types under `Assets/Game/Scripts/Configs/Skirmish/`.
- `SkirmishResolvedSetup`, `SkirmishLaunchPayload`, session/ownership/objective
  components, and composition launch helper.
- Shared authored assets under `Assets/Game/Configs/SkirmishExpansion/Shared/`.
- `SkirmishSetupCompiler.TryCompile` compares army/start/logic/size vectors to
  `INITIAL_SETUP_MATRIX.csv` with field-specific reasons.
- `SkirmishDefinitionBuilder` + `SkirmishSetupCompilerValidation`.
- Session init / spawn / cleanup systems. Spawn fails cleanly when the shared
  prefab registry is not ready and removes attempt-owned ledger entities.
- Catalog entry gained optional `DefinitionId` / `ContentVersion` /
  `ReadinessManifestId` fields. Original `SCENARIO_CATALOG.csv` column order is
  unchanged. Publication lives in `SkirmishPublicationManifest.asset`.

### S002
- Definition / setup / layout at
  `Assets/Game/Configs/SkirmishExpansion/Scenarios/S002`.
- BA reducer + outcome freeze for expanded sessions only.
- Publication status: **InProgress** (definition/compiler). Not Playable, not
  certified, not ARIA/War accepted.

## Remaining ticket gaps

| Ticket | Gap |
|---|---|
| SK-02 | Real role overlays, Ground Staging prefab, production-to-death certification |
| SK-03 | Capacity reservation/lifecycle, research, fabrication/refinery profiles |
| SK-04 | Army groups, fog/intel, movement/transport |
| SK-05 | Enemy strategy + ARIA visible-control skills |
| SK-06 | Full BA fixtures (replacement barracks, pause, hidden health, world damage) |
| SK-10 | Checkpoint/result/replay DTOs and atomic restore |
| SK-11 | Measured DB layout, legal pads, route times |
| SK-12 | Quick Custom briefing/HUD, EN/FA catalog wiring, publication validator |
| SK-13 | Manual + ARIA win matrix, device/recovery evidence |

## How to validate in Editor

1. Keep Unity Hub open and signed in.
2. Compile/check the new assemblies. `Game.Skirmish.Contracts` is auto-referenced;
   if a consumer assembly fails to see it, add an explicit reference (existing
   asmdefs were not edited in this PR).
3. Run:

```sh
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/skirmish-s002-definitions.log -- \
  -quit -executeMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation
```

Required marker: `[SkirmishExpandedDefinitionTests] result=Passed`.

Optional compiler rebuild:

```
Tools/Warline/Skirmish/Rebuild Expanded Definitions
```

4. Legacy smoke: Quick Custom still offers S001 (index 0) and S025 (index 1).
   Editor stress remains index 2. Do not expect S002 on the old playable catalog
   override.
5. Expanded seed path (compiler only until SK-02 spawn is certified):
   Regular Standard `104731` (also `130365`, `155923`).

## Localization follow-up

Shared `V3UiLocalizationCatalog` was not edited. Required keys:

| Key | EN | FA |
|---|---|---|
| `skirmish.s002.title` | Desert Base · Base Assault · Established | پایگاه صحرا · حمله به پایگاه · پایگاه برقرار |
| `skirmish.s002.brief` | Use the starting tank and APC to contest the highway. Keep one infantry squad at home. Press now or scout the south sweep before siege. | با تانک و نفربر شروع‌شده بزرگراه را بگیر. یک دسته پیاده در خانه بماند. یا سریع فشار بده یا جنوب را شناسایی کن. |
| `skirmish.s002.objective` | Destroy the original enemy Barracks while your original Barracks survives. | سربازخانه اصلی دشمن را نابود کن، در حالی که سربازخانه اصلی خودت سالم است. |
| `skirmish.s002.warning.offensive_air` | Ground Maneuver cannot queue attack helicopters or jets. | پروفایل زمینی حمله هوایی تهاجمی را صف نمی‌کند. |
| `skirmish.s002.warning.replacement_base` | A replacement Barracks is not the designated victory base. | سربازخانه جایگزین پایگاه پیروزی تعیین‌شده نیست. |
| `skirmish.s002.result.victory` | Enemy main base destroyed. | پایگاه اصلی دشمن نابود شد. |
| `skirmish.s002.result.defeat` | Your main base was destroyed. | پایگاه اصلی تو نابود شد. |
| `skirmish.s002.result.draw_bases` | Both original bases were destroyed. | هر دو پایگاه اصلی نابود شدند. |
| `skirmish.s002.result.draw_deadline` | Time expired with both original bases standing. | زمان تمام شد و هر دو پایگاه اصلی سالم ماندند. |
| `skirmish.s002.result.surrender` | Surrender accepted. | تسلیم پذیرفته شد. |

Owner: SK-12 / localization catalog builder. Do not mark S002 Playable until
those keys exist in both EN and FA tables.
