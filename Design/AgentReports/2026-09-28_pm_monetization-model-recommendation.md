# Monetization Model Recommendation

Date: 2026-09-28

Status: Recommendation only. `Design/Monetization/Monetization_Strategy.md` and `Design/Monetization/Monetization_Store_Catalog.md` remain the current authority until they are revised.

Related report: `2026-09-28_pm_support-ability-campaign-audit.md` (Support ability monetization).

## Summary

Recommended model: free-to-start with a one-time full-game unlock. Chapter 1 (CH01-M01 to CH01-M05) is free; a single purchase of about $7.99-$9.99 unlocks the full Campaign, Operations, and Skirmish. Add a cosmetics store and later paid campaign expansions. Do not make a free-to-play economy (currency bundles, season pass, rotating offers) the primary revenue model at launch.

## Inputs Reviewed

- `Design/AAA_Mobile_Game_Design_Document_v0_2.md`: product vision, audience, session shape, design pillars, and monetization guardrails.
- `Design/Monetization/Monetization_Strategy.md`: current free-to-play plan with Credits/Command, starter packs, season pass, and pricing tiers.
- `Design/Monetization/Monetization_Store_Catalog.md`: current store products.
- `Design/Economy_Reward_Design.md`: wallet resources and store grant rules.

## Why The Current Free-To-Play Plan Does Not Fit

- The guardrails remove the usual free-to-play revenue drivers. Free-to-play strategy titles earn mainly through PvP competition, progress timers, gacha, and sold power. WarlineCapture correctly forbids pay-to-win, energy gates, hidden-odds containers, and paid campaign stars.
- `Fair Completion` is a design pillar: the full story must be available through play. Without spending pressure, store conversion is expected to be low.
- A $9.99 season pass needs a large, long-retained daily audience and a constant content cadence. A single-player authored RTS is unlikely to have either at launch.
- The product has premium-game characteristics: a 25-mission authored campaign, a central mystery, motion-comic cutscenes, 6-10 minute sessions, and a serious tone. The target audience (approachable command fantasy, low APM) is used to paying once.
- Several catalog items drift toward pay-to-progress: Credit bundles, blueprint-part cases, Rush Tickets, and purchased Operation supplies only sell when the economy is tuned to create demand, which conflicts with the "readable progression" principle.
- Interruptive ads clash with a story about terrorism and civilians. Some ad networks may apply brand-safety limits to this subject matter; verify before relying on ad revenue.

## Recommended Structure

| Layer | Contents | Price guide |
|---|---|---|
| Free trial | First launch, CH01-M01 to CH01-M05, Skirmish demo map | Free |
| Full Command unlock | Chapters 2-5, Operations, full Skirmish | $7.99-$9.99, one-time |
| Cosmetics | Commander frames, squad cards, banners, HUD accents, unit skins, aircraft liveries | $1.99-$4.99 |
| Expansions | New campaigns or Operations regions | $4.99-$7.99 each |
| Optional supporter pack | Cosmetics, soundtrack, art book | $14.99-$19.99 |

### Placement

- Keep the existing rule that the first session never shows monetization: cold open, M01, and first debrief stay uninterrupted.
- Present the full unlock after the Chapter 1 finale, following its story cliffhanger.
- Provide restore purchases from the first release.

## Reuse Of Existing Work

- Keep the SCN-14 Store / Command Exchange screen and the `RewardService` / `RewardConfig` grant pipeline.
- Repurpose `Command` as the cosmetics currency, earned mainly through play; it may also be sold in small bundles for cosmetics only.
- Keep `Credits` as an earned progression currency; stop selling Credit bundles.
- Remove or sharply reduce paid Rush Tickets, blueprint-part and armory cases, and purchased Operation supplies. In a premium product, timers that Rush Tickets skip should not need to exist.
- Keep all existing guardrails: no paid match resources, no paid stars, no hidden-odds containers, no paid story access, no paid "correct" ending.

## Support Abilities

Do not sell match Support power (paid charges, paid in-match strikes, paid early unlocks). Sell only Support cosmetics: aircraft liveries, readable strike VFX variants, pilot callsigns and radio voice lines, and Support menu card art. Full reasoning is in `2026-09-28_pm_support-ability-campaign-audit.md`.

## When Free-To-Play Live-Ops Becomes Reasonable

Treat a season pass and rotating offers as a later phase, earned by data rather than assumed. Consider them only if Operations demonstrates strong long-term retention (for example, healthy day-30 retention) and the team can sustain a regular content cadence. Any season track must stay fixed-content, cosmetic-forward, and free of gameplay power without an earn path.

## Additional Options

- Distribution deals: Apple Arcade or Netflix Games suit story-driven games without microtransactions and could fund development up front.
- PC: the design already lists PC as a secondary platform. A premium Steam release uses the same model.

## Metrics To Validate The Model

- Chapter 1 completion rate (free trial funnel health).
- Conversion from Chapter 1 completion to full unlock.
- Day-1, day-7, and day-30 retention, split by unlocked and trial players.
- Operations and Skirmish session frequency after campaign completion (evidence for or against later live-ops).
- Cosmetic attach rate among unlocked players.

## Document Updates Needed If Adopted

1. Rewrite `Design/Monetization/Monetization_Strategy.md` around the free-to-start plus one-time unlock model.
2. Revise `Design/Monetization/Monetization_Store_Catalog.md`: add the full unlock and expansion products; remove Credit bundles, Rush Ticket packs, armory part cases, and Operation supply sales; keep and extend cosmetics.
3. Update store grant and wallet rules in `Design/Economy_Reward_Design.md` to match.
4. Add the trial boundary and unlock prompt to the first-player-experience and progressive menu disclosure design.
