# M1/M2 tactical resources correction

Authority: `Design/Economy_Reward_Design.md` (Match Resources and Required Runtime And Data Migration) and the SCN-08 Match HUD target: Materials, Oil, Fuel; Credits belong to account progression/rewards.

## Changes

- M2 no longer substitutes Credits for Fuel or hides Oil. The header shows Materials, Oil and Fuel; the mission starts with 120 Materials. Oil and Fuel show usable physical storage rather than a renamed Credit balance.
- M2 construction/recruitment uses the already-authored Materials costs: Barracks 90; four-person rifle squad 20. A complete required build/recruit sequence leaves 10 Materials. No Credit balance is required or spent. Spending and rollback share the same faction policy; a cancelled order cannot mint Credits.
- M2 Build balances, item cards, detail and placement no longer expose the legacy Credit costs. Placement does not display the mock Fuel cost.
- During touch QA, Build could intercept ARIA clicks because a local batching canvas reported sorting order zero while inheriting a higher popup layer. Guided ARIA now follows the effective popup sorting owner, preserves its override during HUD rebinding, and restores the previous order on close. Drawer width follows the ARIA rail as the popup opening animation settles, keeping its close button and detail column clear.
- M2 uses the expanded ARIA instruction layout so the complete Materials lesson wraps and remains readable.
- Lesson 5 is now “Plan your Materials”: explains the 120 → 30 budget after Barracks, reserving 20 for the rifle squad. English/Farsi text uses stable localization configuration keys, including future-language coverage validation.
- The user approved two replacement ElevenLabs ARIA recordings. Both were generated using the existing paid account and established ARIA voice. The two existing asset paths and audio event IDs remain stable; the voice manifest now matches the new captions.
- M1 already disables construction, production and economy actions and has no Credit override. Regression checks explicitly protect its no-Credits behavior and preserve its Materials/Oil/Fuel presentation and combat tutorial.

## Scope and compatibility

This is the M1/M2 correction requested by the user. M2 marks its faction as using Materials-only construction when the attempt budget is initialized. Legacy serialized Money fields, scenario StartingCredits, and prices are retained for compatibility, but M2 initializes Money to zero and does not use it for building or recruiting. Other mission/mode economy migration remains separate; this change does not claim the complete project-wide migration described by the economy design. Account Credits, mission rewards and saved progression are unchanged.

## Validation

Completed through `Tools/CI/invoke_unity_macos.sh` on an isolated Editor QA copy; no Android validation.

- Final log: `/private/tmp/warline-m02-materials-complete.log`.
- 26 focused checks: 9 M2 resource/attempt/retry/header checks, 8 M1 HUD restriction checks, 8 legacy resource transaction regressions, and the new Materials-only purchase/refund/rejection check.
- 28 M2 lesson layouts: seven instructions × English/Farsi × normal/enlarged text. No truncation or overflow. Stable localization keys match the authoritative caption copy.
- All 14 existing M2 tutorial voice records match their caption text and audio hashes, including the two approved replacement WAVs (9.20 seconds English, 11.28 seconds Farsi; 44.1 kHz mono).
- Nested-canvas regression verifies guided ARIA stays above the inherited popup layer, keeps its ordering through HUD rebinding, and restores the previous order on close.
- Final Play Mode run used the actual UI: place/confirm Barracks, then four separate ARIA actions for Build → Soldiers → rifle → Produce. No extra automatic clicks. The squad was delivered; Materials went 120 → 30 → 10, with Money remaining zero. The same run asserts the Build close button does not overlap ARIA.
- 6 Barracks configuration/production checks passed in `/private/tmp/warline-m02-materials-presentation.log`.
- Architecture audit: 137/139 passed on the broad run in that log; its two source-size guard failures were corrected without changing the limits. Both guards pass in the final preflight. The final transaction helper is below its existing 85-line/3813-byte ceiling, and the runtime resource owner remains below its 274-line/11062-byte ceiling.
- Earlier runs exposed the obsolete Credits test expectation, inherited popup sorting bug, and opening-animation sizing issue. They were corrected and the complete touch flow rerun successfully. Two early validation harness failures required the wrapper's own timeout cleanup; no user Editor or Hub was terminated.

Screenshots remain under `/private/tmp/warline-m02-placement/`, outside Design. The final lesson and recruitment screenshots were visually inspected for readable Farsi, Materials-only prices, canonical Oil/Fuel labels and a visible close button. `git diff --check` passes for code, JSON and Markdown.
