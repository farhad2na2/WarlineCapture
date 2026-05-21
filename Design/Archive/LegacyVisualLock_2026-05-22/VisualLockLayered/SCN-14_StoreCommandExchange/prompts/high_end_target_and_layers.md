# SCN-14 Store / Command Exchange High-End Layer Prompt

Use case: ui-mockup
Asset type: AAA mobile RTS store screen plus separated implementation layer atlas.

Primary request:
Create a high-end WarlineCapture Store / Command Exchange landscape UI target at 1672 x 941, matching the existing dark graphite military HUD style with thin cyan chrome, amber/gold CTA accents, Oxanium-like typography, premium polished panels, and deterministic store offer cards.

Reference:
Use `reference/SCN-14_Store_CommandExchange_Target.png` only for quality, layout density, and chrome style. Do not preserve its outdated text.

Canonical content:
- Header title: COMMAND EXCHANGE
- Header resources: Credits 24.8K, Materials 12.6K, Command Authority 1,250
- Left tabs: Featured, Starter Packs, Resources, Armory, Cosmetics, Operation
- Featured offer: Recon Starter Pack, 72h, $4.99
- Featured offer contents: 2,500 Credits, 300 Materials, 120 Command Authority, 3 Intel
- Starter packs: Recon Starter Pack, Base Builder Pack, Operation Founder Pack
- Shop items: Command Authority, Credit Cache, Material Cache, Ranger Parts, Support Drone Kit, Intel Dossier, Aid Convoy, Night Ops

Do not include:
- Tokens
- Intel Keys
- loot boxes, random odds, gacha language, hidden rarity reveals
- direct SagaStars, OperationTrust, OperationSecurity, OperationIntel, or OperationInfrastructure grants

Layer atlas request:
Also create a separate clean layer atlas on a flat chroma-key background for implementation. The atlas must separate:
- shell frame and fill
- header frame and fill
- left navigation button normal/selected/disabled backgrounds
- offer card frames and fills
- starter pack card frames and fills
- shop item card frames and fills
- resource counter frames and fills
- CTA/price button normal and pressed backgrounds
- back icon, close icon, chevrons
- canonical resource icons: Credits, Materials, Command Authority, Intel, Rush Tickets
- product art tiles: Recon case, Base Builder case, Operation Founder case, Command Authority icon, Credit Cache, Material Cache, Ranger Parts, Support Drone Kit, Intel Dossier, Aid Convoy, Night Ops

Layer rules:
- No reusable layer may include text.
- No frame may include item art, icon, or label.
- No button background may include its icon or label.
- Icons must be transparent standalone sprites.
- Card frames must be usable as 9-sliced sprites.
- Content art must be separated from frames.
