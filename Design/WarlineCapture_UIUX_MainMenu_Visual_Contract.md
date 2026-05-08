# WarlineCapture Main Menu Visual Contract

## Active Target

`Design/VisualLock/MainMenu/MainMenu_Landscape_Visual_Target.png`

The old `SCN-02_main_menu_mode_select.jpg` remains the style seed, but it is not the active comparison target because it is an 850x869 presentation crop rather than a landscape game resolution.

## Reference Resolution

- Target aspect: 16:9 landscape.
- Runtime Unity reference resolution: 1920x1080.
- Current generated target image may be scaled to 1920x1080 for screenshot comparison.

## Locked Layout Regions

- Full-screen HUD frame with dark military-metal border.
- Top profile/resource bar: roughly top 15% of the screen.
- Left navigation rail: roughly left 12% of the screen below the header.
- Mode card stack: center/right primary content, three horizontal cards stacked vertically.
- Bottom utility strip: roughly bottom 10% of the screen.

## Locked Text

- `Commander_7X`
- `LV. 32`
- `24.8K`
- `12.6K`
- `1,250`
- `PROFILE`
- `INBOX`
- `STORE`
- `EVENTS`
- `RANKING`
- `SAGA CAMPAIGN`
- `Experience the story of WarlineCapture`
- `PERSISTENT OPERATION`
- `Live events. Territory control. Global war.`
- `QUICK CUSTOM GAME`
- `Skirmish. Rules your way.`
- `[Global] benvv4: Hold the line!`

## Visual Rules

- Use Oxanium family for implemented Unity text.
- Use dark teal/black military HUD panels.
- Use cyan trim for Saga/primary UI.
- Use amber trim for Persistent Operation and progression highlights.
- Use green trim for Quick Custom.
- Keep all buttons mobile readable and at least 80 px tall at 1920x1080.

## Phase 1 Visual-Lock Implementation

Use the generated landscape target as the full-screen screen background and place transparent Unity buttons over the interactive regions.

This is intentional for the first approval pass:

- It gives a stable pixel target immediately.
- It lets gameplay routing continue working.
- It avoids prematurely decomposing the concept art into dozens of sprites before visual approval.

After approval, decompose into reusable UI kit pieces:

- HUD frame sprite
- navigation button sprite
- resource counter sprite
- wide mode-card frame sprites
- mode-card artwork crops
- bottom utility strip sprite

## Main Interactive Hit Zones

- Settings: top-right gear button.
- Saga Campaign: first wide mode card.
- Persistent Operation: second wide mode card.
- Quick Custom Game: third wide mode card.
- Profile, Inbox, Store, Events, Ranking: left rail buttons.
- Chat and social: bottom-left utility buttons.

## Acceptance

- Unity screenshot at 1920x1080 visually matches the active target.
- Transparent hit zones remain clickable.
- Existing functional tests continue passing.
- No text or duplicate UI should visibly overlay the background in visual-lock mode.
