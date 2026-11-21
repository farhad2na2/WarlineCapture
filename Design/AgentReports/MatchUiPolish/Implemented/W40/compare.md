# W40 native vs mockup: Board 2 (compact objective card), Board 4 (ownership colours and text fit)

These are native captures of the shipped `SkirmishMatchView` objective card over the shipped `SCN08_MatchHudContent` prefab. They were made in Play Mode by `MatchHudObjectiveCardCapture` (Tests/Editor) on the Windows UI lab checkout, with fixture match and public-objective models served by `W40ObjectiveCardGateway`. The fixture models are the same before and after.

- Before: `before/`, unmodified UI code at 8a4f3bac9 plus the fixture only. Log `w40-capture-before-v2.log:5957`: `result=Passed phase=before captures=32 ruleFailures=235 enforced=False`.
- After: `after/`, the final W40 code (8813d0aac). Log `w40-capture-after-v4.log:3162`: `result=Passed phase=after captures=32 ruleFailures=0 enforced=True`. The earlier pass `w40-capture-after-v3.log:1708` had the same geometry.
- Each folder has 32 PNGs: 8 states × EN/FA × 1920×1080/2400×1080, and a `geometry.md` with measured screen pixels and the rule check.
- Each PNG is the top 600 px band of the 1080 frame, full width: resource bar, objective card, the ARIA rail top and (Base Assault states) the in-world enemy-base chip. The rest of the frame is flat background in this fixture.

This is a static fixture. States are **named after** the audited missions and reproduce their objective shape and copy (S002 objective, S026 first order, the shared FC/BT/CE objective rows). They are not live mission frames, and they do not show mission readiness. The ARIA journeys are recorded in the W40 results note.

The `live/` folder holds opening frames (1920×1080) from the counted W40 ARIA run 2 on the final code. These frames are native live evidence of the card in real matches:
- [S002 EN](live/s002-en-opening.png), Base Assault;
- [S031 EN](live/s031-en-opening.png), Frontline Control with three neutral zones;
- [S037 EN](live/s037-en-opening.png), Breakthrough;
- [S020 EN](live/s020-en-opening.png), Convoy Escort;
- [S043 FA](live/s043-fa-opening.png), Convoy Escort with the service pad, right-to-left.

| State | Mission / objective | What it stages |
|---|---|---|
| `s002-ba` | S002 Base Assault | Base chips, enemy base not in sight, in-world enemy base chip (reported position) |
| `s026-ba-first-order` | S026 Base Assault (audit) | The opening first order that used to be a second stacked bar |
| `s031-fc` | S031 Frontline Control | Zone A ours, zone B enemy + contested, zone C neutral + capture 40 % |
| `s011-fc-neutral` | S011 Frontline Control (audit) | All three zones NEUTRAL (the "NEUTRA / L" wrap) |
| `s037-bt` | S037 Breakthrough | Corridors 0 / 20 s, exit closed |
| `s041-bt-exit-open` | S041 Breakthrough (audit) | Corridor names; corridors 20 / 20 s and 12 / 20 s, exit open |
| `s020-ce` | S020 Convoy Escort | Destination, delivered 0 / 2 |
| `s046-ce-service` | S046 Convoy Escort (audit) | Destination plus the service pad "WITHIN 12 m · MATERIALS 900" |

## Side by side

Mockups: Board 2 [centre-info-panels.png](../../centre-info-panels.png), Board 4 [hud-colour-consistency.png](../../hud-colour-consistency.png).

| State | Mockup | Before (EN 1920) | After (EN 1920) | After (FA 1920 / EN 2400 / FA 2400) |
|---|---|---|---|---|
| S002 BA | Board 2 | [before](before/s002-ba-en-1920.png) | [after](after/s002-ba-en-1920.png) | [fa](after/s002-ba-fa-IR-1920.png) · [en wide](after/s002-ba-en-2400.png) · [fa wide](after/s002-ba-fa-IR-2400.png) |
| S026 BA first order | Board 2 | [before](before/s026-ba-first-order-en-1920.png) | [after](after/s026-ba-first-order-en-1920.png) | [fa](after/s026-ba-first-order-fa-IR-1920.png) · [en wide](after/s026-ba-first-order-en-2400.png) · [fa wide](after/s026-ba-first-order-fa-IR-2400.png) |
| S031 FC | Board 4 | [before](before/s031-fc-en-1920.png) | [after](after/s031-fc-en-1920.png) | [fa](after/s031-fc-fa-IR-1920.png) · [en wide](after/s031-fc-en-2400.png) · [fa wide](after/s031-fc-fa-IR-2400.png) |
| S011 FC all neutral | Board 4 | [before](before/s011-fc-neutral-en-1920.png) | [after](after/s011-fc-neutral-en-1920.png) | [fa](after/s011-fc-neutral-fa-IR-1920.png) · [en wide](after/s011-fc-neutral-en-2400.png) · [fa wide](after/s011-fc-neutral-fa-IR-2400.png) |
| S037 BT | Board 2 | [before](before/s037-bt-en-1920.png) | [after](after/s037-bt-en-1920.png) | [fa](after/s037-bt-fa-IR-1920.png) · [en wide](after/s037-bt-en-2400.png) · [fa wide](after/s037-bt-fa-IR-2400.png) |
| S041 BT corridor names | Board 2 | [before](before/s041-bt-exit-open-en-1920.png) | [after](after/s041-bt-exit-open-en-1920.png) | [fa](after/s041-bt-exit-open-fa-IR-1920.png) · [en wide](after/s041-bt-exit-open-en-2400.png) · [fa wide](after/s041-bt-exit-open-fa-IR-2400.png) |
| S020 CE | Board 2 | [before](before/s020-ce-en-1920.png) | [after](after/s020-ce-en-1920.png) | [fa](after/s020-ce-fa-IR-1920.png) · [en wide](after/s020-ce-en-2400.png) · [fa wide](after/s020-ce-fa-IR-2400.png) |
| S046 CE Materials | Board 2 | [before](before/s046-ce-service-en-1920.png) | [after](after/s046-ce-service-en-1920.png) | [fa](after/s046-ce-service-fa-IR-1920.png) · [en wide](after/s046-ce-service-en-2400.png) · [fa wide](after/s046-ce-service-fa-IR-2400.png) |

## Measured change (1920×1080 unless stated)

| Rule (approved spec) | Before | After |
|---|---|---|
| One centre card; no second stacked bar (S026) | S026: an 820×62 `FirstOrderPrompt` bar 10 px under the strip | One card in every state; the first order is the card's next-action line |
| 12 px below the resource bar | 17.0 px (fixed top 110) | 12.0 px, measured from the live `ResourceStrip` |
| Charcoal fill, 2–3 px warm keyline `#E7562C` | Cyan `#00BAD9`, 2 px | `#E7562C`, 3 px |
| 16 px padding | 8 px | 16.0 px top, left, right and bottom |
| Purpose and next action first, then timer and status | Status controls only; clock in the middle | Row 1: amber objective glyph, purpose, next action, timer. Row 2: status controls and score |
| English Base Assault card size (target about 560×106) | 825.6×115 | 560.0×184 (see difference 1) |
| Card width by objective (EN 1920) | 825.6 for every family | BA 560, FC 825.6 (max), FC all neutral 665, BT 749, CE 560, CE + service pad 734 |
| Card at 2400×1080 | 1032 wide for every family | BA 560; widths follow content, max 1032 |
| Interactive targets ≥ 64 px | 99 px | 72 px |
| At most one gold camera action | 0 | 0 (see difference 2) |
| S041 corridor names inside their frames | "CORRIDOR A/B" cross the frame (FAIL) | Inside, one line (PASS) |
| S046 Materials inside its frame | "WITHIN 12 m · MATERIALS 900" wraps to 3 lines and crosses the frame (FAIL) | One line inside the chip, auto-sized (PASS) |
| S011 NEUTRAL on one line | "NEUTRA / L" (2 lines, FAIL) | "NEUTRAL" on its own line with a hollow ring (PASS) |
| Ownership colours | Friendly `#4CD966` green fill, enemy `#FF5926` red fill, zones cyan whatever the owner | Friendly `#49BD62` + filled shield, enemy `#F45C3B` + diamond, neutral `#BBC7CD` + hollow ring, on dark surfaces |
| In-world enemy base chip | Slate / amber keyline by knowledge, no ownership mark | `#F45C3B` keyline (strength by knowledge) + diamond before the title |
| Rule failures (all 32 captures) | 235 (not enforced) | 0 (enforced) |

## Intentional differences from the mockups

1. **Card height.** The card is 184 px tall, not about 106. Two rows are needed: purpose, next action and timer first, then the status controls. Every control must be at least 64 px tall, and the padding is 16 px, so the 106 px target cannot hold both rows. The status row and purpose row are 72 px (not 64) so Persian fits: the Persian font's line box is about twice its point size, and at 64 px Persian titles had to shrink to 14 px and the dot of "خ" reached above the keyline. The mockup card is 154 px tall. The old strip plus the S026 bar was 187 px.
2. **No separate gold SHOW ME.** The mockup's gold camera action duplicates the ENEMY BASE focus. Every camera action in the card is an existing focus control that ARIA's input layer taps (`PlayerFocusButton`, `EnemyFocusButton`, the third goal control and the service pad). A second gold button for the same focus would add a new target and button identity, so none was added. The camera stays one tap away on the ownership-coloured ENEMY BASE chip. "At most one" is met with zero.
3. **Base chips stay in the card.** YOUR BASE is kept as a 72 px friendly chip, not plain status text, because ARIA and the base-alert flow focus the own base through it.
4. **Next action copy.** The next-action line uses existing localized copy:
   - the mission's first order while it is shown (S026, first 45 s);
   - otherwise the objective sentence. Base Assault uses the session's objective line. FC, BT and CE use their family's existing sentence (`skirmish.s007/s013/s019.objective`).
   - The first live W40 ARIA run showed why the families need their own sentence: S031's session objective line is the Base Assault sentence.

   The mockup's "Scout the reported base" is not used, because it would be new copy. Enemy base knowledge stays on the ENEMY BASE chip ("NOT IN SIGHT ›", "SEEN …", or health).
5. **Purpose titles** reuse the Battle Library family names (BASE ASSAULT, FRONTLINE CONTROL, BREAKTHROUGH, CONVOY ESCORT and their FA pairs). There is no new string in W40.
6. **Ownership words.** The existing words are kept: OURS / OPPONENT / NEUTRAL (FA خودی / دشمن / بی‌طرف). The mockup's FRIENDLY / ENEMY are not used. Contested state and capture progress move from the ownership line to an amber tag on the title line (CONTESTED, CAPTURE 40 %, NEUTRALIZE n %), plus a 4 px capture bar along the chip bottom in the capturing side's colour.
7. **Persian next action** stays on one line and auto-sizes. English wraps to two lines. Persian at two lines would need about 4× its point size in height.
8. **Score cell.** Tickets, or completed and alive, sit in an unframed cell between the goal chips, where the clock used to be. The timer moved to the end of the purpose row.
9. **Non-ownership goals** (corridors, exit, destination, service pad) keep the native blue camera-navigation chip. Silver is reserved for neutral ownership.
10. **Fixture content.** Resource values, ARIA copy and the flat background are fixture content. Live data comes from the session.
