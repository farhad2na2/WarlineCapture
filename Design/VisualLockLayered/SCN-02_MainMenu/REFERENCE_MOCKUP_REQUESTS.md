# SCN-02 Main Menu Reference Mockup Requests

Date: 2026-05-22
Status: Reference-only candidate requests. Do not create layers yet.

## Purpose

Request several polished main menu reference mockups before starting the V15 layered extraction workflow. These mockups are for visual direction selection only. After one reference is approved, request the implementation layers using `../WORKFLOW_V15_3D_GREENSCREEN.md`.

## Active Design Requirements

The mockups must align with:

- Full 3D single-map mobile RTS direction.
- Player as a field commander preparing attacks and operations against hostile cells hidden within civilian city districts.
- One large 3D Middle Eastern-inspired town / forward operating area as the gameplay world.
- Campaign, Operations, and Skirmish as the three primary modes.
- Command-base menu visual language: dark military panels, weathered metal, olive selected states, gold action accents, muted blue command-resource accents, restrained off-white text.
- Existing gameplay/UI targets unchanged in structure: command-base main menu, mode cards, commander panel, top resource bar, left navigation, bottom-right deploy CTA.

## Gameplay Scene References

Use the current gameplay scene references as the visual source for all battlefield/base/town imagery:

- `Assets/Game/Scenes/Demo.unity`
- `Assets/Game/Scenes/Demo2.unity`

Support references, if needed:

- `Assets/Game/Scenes/Generated/GC29_AuthoredDemoHybrid_2048.unity`
- `Assets/Game/Scenes/Generated/GC27_Demo2ArtRoadHybrid_2048.unity`
- `Assets/Game/Scenes/Generated/GC30_Demo2RoadGreenTerrain_2048.unity`

The main-menu background and mode-card art should echo these scenes: modular roads, dirt roads, concrete base pads, base walls, barriers, damaged road segments, armored vehicles, crashed aircraft debris, water/edge terrain where appropriate, command/base compounds, market/town blocks, airfield/hangar staging, and road-connected playable areas.

Do not invent a gameplay look that conflicts with Demo or Demo2. The mockup can polish composition and lighting to AAA mobile menu quality, but the recognizable gameplay world should come from these scenes.

## Required Visible Routes And Buttons

Every candidate must show these routes clearly:

- Campaign
- Operations
- Skirmish
- Store / Command Exchange
- Commander
- Settings
- Inbox / Messages
- Deploy Operation

Every candidate should also reserve space for:

- Commander profile panel
- Readiness meter
- Squad Management locked/disabled row
- Intel Report locked/disabled row
- Comms/status panel

## Required Text

Use live-text-safe typography and keep text readable. The reference can show these labels, but the final Canvas will rebuild text as TMP:

- `WARLINE CAPTURE`
- `Credits`
- `Supplies`
- `Command`
- `Campaign`
- `Operations`
- `Skirmish`
- `Store`
- `Commander`
- `Settings`
- `COMMANDER`
- `FIELD COMMANDER`
- `READINESS`
- `SQUAD MANAGEMENT`
- `INTEL REPORT`
- `COMMS ONLINE`
- `DEPLOY OPERATION`

## Candidate Requests

| Candidate | Prompt File | Intent |
|---|---|---|
| A | `reference_requests/SCN-02_MainMenu_Reference_A_CommandTent.md` | Closest to the user-provided command tent/base reference. |
| B | `reference_requests/SCN-02_MainMenu_Reference_B_OperationTable.md` | Stronger tactical table / district map first read. |
| C | `reference_requests/SCN-02_MainMenu_Reference_C_ForwardBaseHangar.md` | More 3D base depth, vehicles, helicopters, and live operation staging. |

## Output Request

For each candidate, request only:

```text
Design/VisualLockLayered/SCN-02_MainMenu/reference/candidates/SCN-02_MainMenu_Reference_[A-C]_[Name]_1672x941.png
```

Do not request:

- separated layers,
- chroma-key layer sheets,
- `layer_manifest.json`,
- Unity sprites,
- Canvas prefab changes.

## Acceptance Check Before Layer Request

Approve only one candidate after checking:

- All required routes are visible and readable.
- The player-facing modes are Campaign, Operations, and Skirmish.
- The main CTA is Deploy Operation and is not treated as a fourth mode.
- The menu reads as a 3D command base preparing a city operation, not a 2D/isometric game board.
- The gameplay art and mode-card imagery read as polished versions of Demo / Demo2, not unrelated generic military concept art.
- The background supports future mode-card extraction and 20:9 layout adaptation.
- Text is not too small or overcrowded for mobile landscape.
- The visual language matches AAA mobile strategy quality without becoming too busy.
