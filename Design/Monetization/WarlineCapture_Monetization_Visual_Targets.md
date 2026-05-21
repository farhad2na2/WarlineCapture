# WarlineCapture Monetization Visual Targets

Date: 2026-05-04

## Source Visual Language

The monetization visuals follow the accepted WarlineCapture visual-lock direction:

- Dark graphite/black military HUD panels.
- Olive selected states and weathered metal bevels.
- Amber/gold accents for rewards, CTAs, and premium value.
- Muted blue command-resource accents.
- Soft shadows, readable Oxanium-like typography, mobile landscape composition.
- Separate Unity-ready UI parts: category tabs, offer cards, item icons, resource counters, buttons, badges, and reward strips.

Relevant references:

- `Design/VisualLockLayered/README.md`
- `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`
- `Design/VisualLockLayered/SCN-14_CommandExchange/reference/SCN-14_CommandExchange_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_Landscape_Target.png`
- `Design/VisualLockLayered/POP-04_RewardUnlock/reference/POP-04_RewardUnlock_Landscape_Target.png`
- `Design/VisualLockLayered/POP-09_AbilityUpgradeDetail/reference/POP-09_AbilityUpgradeDetail_Landscape_Target.png`

Previous monetization-adjacent VisualLock references were archived under `Design/Archive/LegacyVisualLock_2026-05-22/` and are comparison material only.

## Generated Image Index

| Image | Purpose |
|---|---|
| `../VisualLockLayered/SCN-14_CommandExchange/reference/SCN-14_CommandExchange_Landscape_Target.png` | Store screen composition target. |
| `../VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_Landscape_Target.png` | Armory inspection target for store Armory links. |
| `../VisualLockLayered/POP-09_AbilityUpgradeDetail/reference/POP-09_AbilityUpgradeDetail_Landscape_Target.png` | Ability/upgrade product detail popup target. |
| `Images/StarterPack_Recon_CommandCard.png` | Starter pack visual for Recon Starter Pack. |
| `Images/StarterPack_BaseBuilder_CommandCard.png` | Starter pack visual for Base Builder Pack. |
| `Images/StarterPack_OperationFounder_CommandCard.png` | Starter pack visual for Operation Founder Pack. |
| `Images/ShopItem_CommandAuthority_Icon.png` | Command Authority resource item icon. |
| `Images/ShopItem_IntelDossier_Icon.png` | Operation intel item icon. |
| `Images/ShopItem_AidConvoy_Icon.png` | Operation supply item icon. |
| `Images/ShopItem_NightOpsCards_Icon.png` | Cosmetic squad card item icon. |

## SCN-14 Store / Command Exchange Target

Use this as the first store screen visual target:

`Design/VisualLockLayered/SCN-14_CommandExchange/reference/SCN-14_CommandExchange_Landscape_Target.png`

### Layout

- Top header: back button, title `COMMAND EXCHANGE`, resource counters, restore-purchases icon button.
- Left category rail: Featured selected, Starter Packs, Resources, Armory, Cosmetics, Operation.
- Large featured offer panel: Recon Starter Pack with included rewards, timer, and primary purchase CTA.
- Starter pack row: three large cards.
- Shop item grid: eight compact items.
- Bottom line: `Transparent contents. Platform product ids are assigned by release catalog.`

### Unity Decomposition

Required UI pieces:

- `StoreCategoryButtonView`
- `FeaturedOfferView`
- `StarterPackCardView`
- `ShopItemCardView`
- `StoreResourceCounterView`
- `PurchaseButtonView`
- `OfferTimerView`
- `RestorePurchasesButtonView`

## Visual Prompt - Store Screen

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for WarlineCapture Store / Command Exchange, matching the accepted premium military RTS HUD style.

Scene/backdrop: Dark command-base exchange interface inside a mobile military RTS shell. Black/olive military panels, weathered metal frames, gold reward accents, muted blue command-resource accents, subtle command table or forward-base texture, no casino look.

UI layout:
- Full-screen futuristic military HUD frame.
- Top header bar: back arrow, title "COMMAND EXCHANGE", resource counters for Credits, Supplies, Command, small restore icon button.
- Left vertical category rail with "FEATURED" selected in olive/gold, then "STARTER PACKS", "RESOURCES", "ARMORY", "COSMETICS", "OPERATIONS".
- Large featured offer card with "RECON STARTER PACK", tactical supply case art, listed rewards, timer "72H", and primary gold purchase button "$4.99".
- Starter pack row with three cards: Recon Starter Pack, Base Builder Pack, Operation Founder Pack.
- Item grid with compact cards: Command Authority, Intel Dossier, Aid Convoy, Night Ops Cards.
- Bottom legal/status line for restore purchases and transparent contents.

Style requirements:
- Match WarlineCapture command-base visual targets: dark black/olive panels, worn metal edges, olive selected states, gold CTA/reward accents, muted blue command-resource accents, soft shadows, crisp readable Oxanium-like typography.
- Cards, icons, tabs, counters, timers, and purchase buttons must look like separate Unity UI parts.
- No loot-box casino visuals, no generic neon sci-fi, no bright white borders, no hard block shadows, no watermark.
```

## Visual Prompt - Starter Pack Cards

```text
Use case: game UI card art
Asset type: WarlineCapture starter pack card, 1024x640.

Create a premium mobile military RTS store card in the WarlineCapture command-base HUD style. Dark black/olive beveled frame, worn metal trim, amber/gold reward accents, soft shadow, readable title area, 3D tactical supply case or field crate render, small reward icon strip, and clear value badge. The card must feel like a separable Unity UI component, not a full screen. No watermark, no casino visuals, no unreadable text.
```

## Visual Prompt - Shop Item Icons

```text
Use case: game UI item icon
Asset type: square WarlineCapture store item icon, 512x512.

Create a square premium military RTS shop item icon matching WarlineCapture command-base visual lock: dark beveled black/olive frame, muted blue edge light where needed, amber reward accent, centered 3D object render/silhouette, soft shadow, clean readable symbol language. No text baked into the icon except tiny nonessential markings, no watermark, no casino styling.
```
