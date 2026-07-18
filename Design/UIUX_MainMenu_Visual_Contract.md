# WarlineCapture Main Menu Visual Contract

Date: 2026-05-21

2026-07-10 first-player amendment: this remains the visual contract for the command-base menu, but a fresh profile must not open here. `First_Player_Experience_And_Story_Onboarding_Design.md` routes first launch through the cold open, ARIA boot, identity, M01, and first debrief before revealing this menu as the headquarters the player has restored.

## Active Target

Primary visual target:

```text
Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png
Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json
```

The user-provided command-base reference remains the active visual direction. The previous `SCN-02B_MainMenuAlt` layered package and older teal/cyan main-menu targets are archived under `Design/Archive/LegacyVisualLock_2026-05-22/` and are historical comparison material only.

The next main-menu visual-lock task is to create a fresh implementation-ready layered pack at `Design/VisualLockLayered/SCN-02_MainMenu/` following `Design/VisualLockLayered/README.md`.

## Reference Resolution

- Target aspect: 16:9 landscape.
- Runtime Unity reference resolution: 1920x1080.
- Current generated target image may be scaled to 1920x1080 for screenshot comparison.

## Locked Layout Regions

- Full-screen command-base frame with dark military-metal border and worn olive/gold accents.
- Top logo/resource bar: roughly top 12-15% of the screen.
- Left navigation rail: roughly left 14-16% of the screen below the header.
- Center mode cards: Campaign, Operations, Skirmish, arranged as large command cards over the command tent/base background.
- Right commander panel: portrait, rank, readiness pips, locked feature rows.
- Bottom-right primary CTA: `DEPLOY OPERATION`.
- Bottom-left comms/status panel.

## Progressive Disclosure State

On the first post-M01 reveal, `Continue Campaign` and M02 are dominant. Commander Profile, Settings/accessibility, and Story Archive are available but secondary. Operations, Skirmish, Store, advanced currencies, and dense progression surfaces are hidden or clearly locked until their authored reveal points. This contract must support both the simplified first-session state and the later full command-base state.

## Locked Text

- `WARLINE CAPTURE`
- `Credits`
- `Command`

The previous third `Supplies` chip is retired. The header must not display Materials, Fuel, Oil, Supply, or any match-scoped balance. Existing three-chip target art may reuse the freed width for spacing/profile status or leave the third slot absent; it must not invent another currency.
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

## Visual Rules

- Use Oxanium family for implemented Unity text unless a later typography pass replaces it project-wide.
- Use dark black/green military HUD panels, weathered metal frames, olive selected states, gold CTA/action accents, muted blue command resource accents, and restrained off-white text.
- Do not return to the old dominant teal/cyan main-menu palette for the active target.
- Mode cards should show 3D command/base/town art, not 2.5D isometric tactical-map art.
- `Deploy Operation` is a primary shortcut to the selected/next available operation path; it is not a separate fourth mode.
- Keep all buttons mobile readable and at least 80 px tall at 1920x1080.

## Phase 1 Visual-Lock Implementation

Use the user-provided command-base direction as the comparison target, then create the new `SCN-02_MainMenu` layered pack and rebuild as real Canvas pieces.

This is intentional for the first approval pass:

- It gives a stable pixel target immediately.
- It lets gameplay routing continue working.
- It avoids prematurely decomposing the concept art into dozens of sprites before visual approval.

After approval, decompose into reusable UI kit pieces:

- header bar frame
- left navigation button states
- resource counter slots
- command card frames and artwork
- commander panel frame
- readiness pips
- deploy button background
- comms/status panel
- replaceable icons from the layered package

## Main Interactive Hit Zones

- Settings: top-right gear and left-rail Settings.
- Campaign: selected left rail item and Campaign card.
- Operations: left rail item and Operations card.
- Skirmish: left rail item and Skirmish card.
- Store: left rail item.
- Commander: left rail item and right commander panel.
- Inbox/messages: top-right envelope.
- Deploy Operation: bottom-right CTA, routing to the selected/next available operation path.
- Comms/status: bottom-left status surface.

## Acceptance

- Unity screenshot at 1920x1080 visually matches the active command-base target.
- Transparent hit zones remain clickable.
- Existing functional tests continue passing.
- No text or duplicate UI should visibly overlay the background in visual-lock mode.
- Player-facing mode labels are Campaign, Operations, and Skirmish.
