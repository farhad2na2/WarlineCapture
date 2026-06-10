# Match HUD Selection Summary Panel Plan

## Goal

Make `Canvas (Environment) / SCN08_MatchHudContent / LeftContent / SelectedSquadPanel` useful for every valid selection state. The panel must never appear with an empty portrait or misleading text when multiple entities are selected.

The panel should remain a raw view: serialized references and visual application only. Selection decisions, summaries, command availability, and fallback sprite choice belong in systems.

## Current Status

- [x] Plan created.
- [x] Existing asset scan done.
- [x] Existing candidate assets found:
  - `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01/scn08_portrait_rifle_squad.png`
  - `Assets/Game/Art/UI/Icons/scn09_icon_squad_group.png`
  - Existing squad tray card portrait sprites on `MatchHudSquadTrayView`.
- [x] Add explicit fallback sprites to the selected panel view or a UI config asset.
- [x] Build selected-entity composition summary system.
- [x] Update selected panel model for all multi-selection combinations.
- [x] Add focused editor tests for selected-panel summary behavior.
- [x] Run Unity compile and focused validation.

## Recommended UX Rule

Keep `SelectedSquadPanel` visible whenever there is a valid selected unit, selected building, or multi-selection. Do not hide it for multi-selection. A visible summary panel is better feedback than no panel, and it gives the player a stable home for squad-level commands.

The panel must use this portrait priority:

1. Single focused unit: unit `portraitActionSprite`, fallback `portraitCardSprite`.
2. Single selected building: building `portraitActionSprite`, fallback `portraitCardSprite`.
3. Squad tray selection: exact portrait sprite already assigned to the active squad tray card.
4. Manual same-category multi-selection: category fallback sprite.
5. Manual mixed multi-selection: generic mixed-force fallback sprite.
6. Missing/invalid sprite: generic squad fallback sprite. Never show a blank portrait.

## Selection Scenarios

| Scenario | Portrait | Title | Subtitle | Health | Order |
| --- | --- | --- | --- | --- | --- |
| No selection | Panel hidden | - | - | - | - |
| One soldier | Unit action/card portrait | Unit display name | Unit description/class | Unit health | Unit current order |
| One vehicle | Unit action/card portrait | Vehicle display name | Vehicle role | Vehicle health | Vehicle current order |
| One aircraft | Unit action/card portrait | Aircraft display name | Aircraft role | Aircraft health | Aircraft current order |
| One transport | Unit action/card portrait | Transport display name | Seats/passenger status if available | Transport health | Transport current order |
| One building | Building action/card portrait | Building display name | Building role/status | Building health if available | Building production/order status |
| Multiple soldiers from squad tray | Active squad tray card portrait | `{N} SOLDIERS` | Infantry squad | Aggregate health | Common order or `Mixed orders` |
| Multiple vehicles from squad tray | Active squad tray card portrait | `{N} VEHICLES` | Vehicle squad | Aggregate health | Common order or `Mixed orders` |
| Multiple aircraft from squad tray | Active squad tray card portrait | `{N} AIRCRAFT` | Air wing | Aggregate health | Common order or `Mixed orders` |
| Multiple transports from squad tray | Active squad tray card portrait | `{N} TRANSPORTS` | Transport group | Aggregate health | Common order or `Mixed orders` |
| Manual multiple soldiers | Soldier/squad fallback portrait | `{N} SOLDIERS` | Infantry squad | Aggregate health | Common order or `Mixed orders` |
| Manual multiple vehicles | Vehicle fallback portrait | `{N} VEHICLES` | Vehicle squad | Aggregate health | Common order or `Mixed orders` |
| Manual mixed soldiers and vehicles | Mixed-force fallback portrait | `MIXED SQUAD` | `{S} infantry / {V} vehicles` | Aggregate health | Common order or `Mixed orders` |
| Manual mixed ground and air | Mixed-force fallback portrait | `MIXED FORCE` | `{G} ground / {A} air` | Aggregate health | Common order or `Mixed orders` |
| Manual units plus selected building | Mixed-force fallback portrait | `MIXED SELECTION` | `{U} units / {B} structure` | Aggregate health where available | `Mixed orders` |
| Multiple buildings only, if later supported | Building group fallback portrait | `{N} STRUCTURES` | Building group | Aggregate structure health | Common production/status or `Mixed status` |
| Enemy/non-owned selection, if exposed | Neutral/invalid fallback portrait | Selection label | Ownership/status | Health if visible | Commands disabled |

## Implementation Steps

### Step 1 - Add Summary Model Inputs

- [x] Extend the selected panel model only if needed with fields for command availability and optional category/state metadata.
- [x] Keep `MatchHudSelectionPanelView` simple: apply title, subtitle, portrait, health, order, badge, and action state.
- [x] Do not add controller/presenter/bridge classes.

### Step 2 - Add Selection Composition Query

- [x] Add a small system-style query helper, for example `SelectionSummaryQuerySystem`.
- [x] It should read selected ECS entities and count:
  - soldiers
  - combat vehicles
  - transports
  - aircraft
  - buildings, if building selection is represented together with unit selection
  - owned/non-owned where relevant
- [x] It should also compute:
  - total health current/max
  - common order state, or mixed order state
  - preferred category label
  - preferred fallback sprite key

### Step 3 - Portrait Fallbacks

- [x] Add serialized fallback sprites on an existing UI config/view path, not hardcoded asset paths in runtime code.
- [x] Minimum fallback set:
  - generic squad
  - soldiers
  - vehicles
  - aircraft
  - transports
  - buildings
  - mixed force
- [x] First pass can reuse existing assets:
  - soldiers/generic squad: `scn08_portrait_rifle_squad.png`
  - mixed force: `scn09_icon_squad_group.png` if it looks acceptable in the selected portrait frame.
- [ ] If visual review rejects the reused assets, use the imagegen workflow below.

### Step 4 - Multi-Selection Panel Copy

- [x] Replace the current generic `{N} UNITS` title logic with composition-aware copy.
- [x] Examples:
  - `4 SOLDIERS`
  - `2 VEHICLES`
  - `MIXED SQUAD`
  - `MIXED FORCE`
  - `3 INFANTRY / 1 APC`
  - `2 GROUND / 1 AIR`
- [x] Keep title concise and subtitle descriptive.

### Step 5 - Health and Order Rules

- [x] Aggregate health across selected entities with `UnitHealth` or building health where available.
- [x] If at least one selected entity has health:
  - health text: `Health: {current}/{max}`
  - fill: `current / max`
- [x] If no selected entity exposes health:
  - health text: `Health: -`
  - fill: `0`
- [x] Order text:
  - If all selected entities share the same order, show that order.
  - If orders differ, show `Mixed orders`.
  - If no actionable order exists, show `Idle` or `Structure selected`.

### Step 6 - Command Availability

- [x] Return:
  - enabled for movable owned units.
  - for buildings, later maps to “call assigned units home” if that feature is implemented.
- [x] Destroy:
  - enabled for owned selected units/buildings.
- [x] Board:
  - enabled/clickable only when selection includes a selected transport or focused transport with capacity.
  - non-transport selections should produce a clear feedback message if the button is still visible.
- [x] Do not let panel clicks pass through to world selection.

### Step 7 - Tests

- [x] Add or update editor tests to verify:
  - multi-selection never passes null portrait when fallback sprites are configured.
  - manual multi-soldier selection uses soldier fallback.
  - mixed soldier/vehicle selection uses mixed fallback.
  - aggregate health is computed correctly.
- [x] Remaining focused tests:
  - tray-selected squad uses the active tray portrait.
  - mixed orders display `Mixed orders`.
- [x] Not applicable to this implementation because no new text components were added:
  - no legacy `Text` component is added.
  - TMP text remains `Oxanium-Medium SDF`.

### Step 8 - Validation

- [x] Unity C# compiler response validation: no errors, no new warnings.
- [ ] Play-mode/manual smoke:
  - select one soldier
  - select one building
  - select multiple soldiers by rectangle
  - select mixed soldiers/vehicles if available
  - select squad tray soldier card
  - select vehicle/transport tray cards
- [ ] Confirm portrait never appears blank while panel is visible.
- [ ] Confirm panel copy fits within the current selected panel layout at 16:9 and 20:9.

Validation note: after Unity licensing was restarted manually, focused main-project EditMode validation passed for `SelectionSummaryQuerySystemTests`: 5/5 tests, XML `/private/tmp/selection-summary-query-tests.xml`, log `/private/tmp/selection-summary-query-tests.log`. The older `BattleHudRuntimeFeedbackSystemConnectionTests` fixture still fails in shared `SetUp` because `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab` is missing; the new selection-summary tests were moved into an isolated fixture that does not depend on that legacy overlay prefab.

## Imagegen Fallback Workflow

Only run imagegen if the existing assets are not acceptable in the selected panel portrait frame.

Generate a small consistent set of 512x512 portrait-style icons on a flat chroma-key background, then remove the green background cleanly.

Required generated sprites if needed:

- `selection_summary_soldiers_512.png`
- `selection_summary_vehicles_512.png`
- `selection_summary_aircraft_512.png`
- `selection_summary_transport_512.png`
- `selection_summary_buildings_512.png`
- `selection_summary_mixed_force_512.png`

Prompt template:

```text
Create a high-quality 512x512 military RTS UI portrait icon on a perfectly flat solid #00ff00 chroma-key background for background removal.
Subject: <soldier squad / armored vehicles / aircraft wing / transport convoy / base buildings / mixed combined force>.
Style: premium low-poly military game UI, clean command-HUD portrait, dark graphite and olive palette, subtle gold highlights, readable silhouette at small size.
Composition: centered subject, generous padding, no text, no numbers, no UI frame, no watermark.
Background: exactly uniform #00ff00, no shadow, no gradient, no texture, no floor plane.
Do not use #00ff00 anywhere in the subject.
```

After generation:

- [ ] Remove chroma key with the imagegen skill helper.
- [ ] Validate no green fringe remains.
- [ ] Import under `Assets/Game/Art/UI/Icons` or `Assets/Game/Art/UI/Portraits/Secondary` as Sprites.
- [ ] Assign the sprites to the selected panel fallback config/view fields.

## Architecture Notes

- View classes remain raw reference holders and visual appliers.
- Systems compute selection summary state and command availability.
- No new class names ending with `Controller`, `Presenter`, `Bridge`, or `Button`.
- Runtime code must not discover child UI by hierarchy strings.
- Prefab references must be serialized.
- New text must be TextMeshPro and use `Oxanium-Medium SDF`.
