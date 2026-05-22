# SCN-13 Skirmish Setup Target Lock V02 Prompt

Use case: ui-mockup
Asset type: WarlineCapture SCN-13 Skirmish Setup target-lock mockup, landscape mobile game UI reference only.

Primary request: Create a high-end AAA mobile RTS UI target-lock mockup for the WarlineCapture Skirmish Setup screen. This is a full-screen reference mockup only, not separated implementation layers.

Style reference: Match the current WarlineCapture V22 Main Menu command-base style: dark worn metal military panels, olive/black/gold bevel trims, realistic command tent / forward base lighting, gold primary CTA, Oxanium-like military typography, compact premium mobile landscape layout, realistic 3D operation-map imagery, no cyan sci-fi theme. Use the archived Custom Game Setup image only as rough content/layout reference, not visual style.

Screen context: Full 3D single-map mobile RTS. Skirmish is player-facing language. Internal QuickCustom naming must not appear. The commander is preparing a configurable combat operation in a Middle Eastern-inspired town/base where hostile cells may hide among civilians. No real-world flags, real groups, or real conflicts.

Canvas: 20:9-safe landscape. Keep all text within safe margins, readable, and not overlapping.

Required layout:

- Top header bar similar to V22 Main Menu: Warline Capture logo panel on far left, resource counters for Credits `187,540`, Supplies `92,860`, Command `2,715`, inbox and settings buttons on the top right.
- Title area: Back arrow and title text `SKIRMISH` with subtitle `Configure Operation`.
- Left preset rail: selected `Tutorial Intercept`; locked future presets `Convoy Pressure`, `Airlift Extraction`, `Breach Assault`, `Hidden Cell Raid`.
- Center operation preview: large 3D map preview with roadblocks, forward base staging, patrol routes, civilians, hostile intel markers, deployment zone markers.
- Preview badges: `Map: Desert Outpost`, `Seed 104729`, `Intel Reveal ON`, `Civilian Risk MED`.
- Right rules panel: Enemy Type `Balanced`, Enemy Count stepper `1`, Difficulty `Normal`, Starting Credits `Normal`, Income `1.0x`, Build Speed `Normal`, Production Speed `Normal`, Aggression `Balanced`, Expansion `Normal`, Win Condition `Destroy All Enemies`, Fog Of War locked with reason `Requires Fog Runtime`.
- Bottom action bar: info text, `RESET`, `RANDOMIZE SEED`, large gold `LAUNCH MISSION` CTA with chevrons.

Avoid:

- `Quick Custom`
- `Custom Game Setup`
- `Saga`
- teal/cyan legacy styling
- real flags
- real armed groups
- real political symbols
- placeholder text
- watermarks

Postprocess:

- Saved original V02 candidate at `reference/SCN-13_SkirmishSetup_TargetLock_V02.png`.
- Resized canonical approval target to `2400 x 1080` at `reference/SCN-13_SkirmishSetup_Landscape_Target.png`.
