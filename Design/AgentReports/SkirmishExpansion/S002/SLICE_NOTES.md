# S002 first expanded ground slice

Catalog **S002** — Desert Base · Base Assault · Ground Maneuver · Established Base.
Handoff ordinal 4. First visit: Regular / Standard. Seed sample: `104731`.

## What landed

### SK-00
- New `Game.Skirmish.Contracts` assembly (`noEngineReferences`, auto-referenced).
  Consumer assemblies also list it explicitly (see `ASMDEF_REFERENCES.md`).
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
- Session init / spawn / cleanup systems. S002 Standard Regular starting
  forces/structures bind `UnitSourcePrefabKey` from the role catalog, so
  `SpawnVisualPending=0` without a live ECS prefab registry. Instantiating
  GameObjects from that registry remains later SK-02 certification.
- Catalog entry gained optional `DefinitionId` / `ContentVersion` /
  `ReadinessManifestId` fields. Original `SCENARIO_CATALOG.csv` column order is
  unchanged. Publication lives in `SkirmishPublicationManifest.asset`.

### SK-02 (S002 army G ground slice)
- Typed `SkirmishRoleOverlay` + `SkirmishRoleOverlayCatalog` for infantry and
  S002 ground vehicles (rifle/gunner/rocketeer/car/APC/tank plus G-legal
  support). Overlays bake into the compiled snapshot with MATCH_SETUP Materials
  costs. `CapabilityCertified=false`.
- `SkirmishRosterProjectionSystem` applies overlays once (health/damage/range/
  producer/target domains). Blanket prototype rifle tuning is not used on
  expanded sessions.
- Infantry production accepts **4 members per squad**. Vehicles require Ground
  Staging. Army G still rejects offensive air.
- Ground Staging is a typed producer + compiled starting structure (visual key
  `Building_GroundStaging`). Not a new art prefab.
- Spawn expands force quantity, binds `Faction` + `UnitSourcePrefabKey` for
  every S002 starting member. `SpawnVisualPending=0` for Standard Regular.
- `SkirmishProductionService.TryProduce` / `TryReleaseDeath` is the
  production-to-death path for the S002 ground set (ledger + overlay + cap).

### SK-03 (S002 capacity / stocks)
- `SkirmishEconomyStockComponent` seeds Materials/Oil/Fuel from the compiled
  snapshot (Standard E: 900/240/700).
- `SkirmishCapacityLedger` + reservation buffer: Reserved → Live → Released.
  Instant S002 produce visits Reserved then Live in the same call. Starting
  grants count as Live. Death releases once.
- Shared eligibility now also checks Materials/Fuel/caps when
  `EnforceStocks=true`. No research tree in this slice.

### SK-06 (BA depth)
- `SkirmishBaseAssaultFacts` + `SkirmishObjectiveFactProjectionSystem`.
  Terminal evaluation uses **original designated** `base.player` / `base.enemy`
  identities only. Replacement Barracks do not count as extra lives.
- Field-army wipe is non-terminal. Pause freezes the deadline clock.
  Surrender is accepted only while Playing. Same-tick both-designated-dead is
  Draw / BothBasesDestroyed.
- `SkirmishOutcomeSystem` remains the only terminal writer.

### Launch path
- `SkirmishExpandedLaunchResolver.TryCompileAndQueue` compiles catalog S002
  (size/difficulty/seed) and queues the immutable snapshot. No new
  `MatchSceneView` S-ID switch. Map hint remains Desert Base (index 0) for
  scene load only.

### S002 publication
- Status: **InProgress**. Closer to a playable ground slice (compiler +
  overlays + designated BA facts + launch resolver) but **not Playable**, not
  ARIA/War certified, not Accepted.

## Remaining ticket gaps

| Ticket | Gap |
|---|---|
| SK-02 | Registry GameObject instantiate, Ground Staging prefab/pads, air roles, combat certification |
| SK-03 | Research tree, fabrication/refinery profiles, physical haul, cancel/refund 75% mid-produce. Recruitment FuelCost stays 0 (MATCH_SETUP treats fuel as operation, not a second buy currency) |
| SK-04 | Army groups, fog/intel, movement/transport |
| SK-05 | Enemy strategy + ARIA visible-control skills |
| SK-06 | Hidden-health HUD, world-damage fixtures, pause/result replay through live match |
| SK-10 | Checkpoint/result/replay DTOs and atomic restore |
| SK-11 | Measured DB layout, legal pads, route times |
| SK-12 | Quick Custom briefing/HUD, EN/FA catalog wiring, publication validator |
| SK-13 | Manual + ARIA win matrix, device/recovery evidence |

## How to validate in Editor

Use Programmer 1’s **shadow project only**: `D:\Projects\WarlineCapture-Skirmish`
(git worktree, own `Library`). Check out `cursor/skirmish-shadow` or this
short-lived `cursor/skirmish-s002-…` branch there. Do **not** open or lock the
shared `D:\Projects\WarlineCapture` checkout.

1. Keep Unity Hub open and signed in.
2. Compile/check the new assemblies. `Game.Skirmish.Contracts` stays its own
   assembly. Consumers that `using Game.Skirmish.Contracts` reference it
   explicitly: `Game.Components`, `Game.Configs`, `Game.Runtime`,
   `Game.Composition`, `Game.Editor`, and `Game.Tests.Editor`.
3. From the shadow worktree, run the Windows wrapper (preferred on Programmer 1):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-definitions.log" `
  -RequiredPassMarker "[SkirmishExpandedDefinitionTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedObjectiveTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-objectives.log" `
  -RequiredPassMarker "[SkirmishExpandedObjectiveTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedEconomyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-economy.log" `
  -RequiredPassMarker "[SkirmishExpandedEconomyTests] result=Passed"
```

Required markers:

- `[SkirmishExpandedDefinitionTests] result=Passed`
- `[SkirmishExpandedObjectiveTests] result=Passed`
- `[SkirmishExpandedEconomyTests] result=Passed`

Optional compiler rebuild:

```
Tools/Warline/Skirmish/Rebuild Expanded Definitions
```

4. Legacy smoke: Quick Custom still offers S001 (index 0), S025 (index 1), and
   Farhad’s S073 Industrial Basin (index 3). Editor stress remains index 2.
   Do not expect S002 on the old playable catalog override.
5. Expanded seed path: Regular Standard `104731` (also `130365`, `155923`).

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
