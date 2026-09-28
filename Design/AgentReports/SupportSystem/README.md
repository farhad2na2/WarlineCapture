# Support system — approved implementation handoff

Approved by the owner on 2026-09-28: Smoke Screen, Precision Strike, Paratroopers, Supply Drop, the full-screen Build-style popup and project-asset-based visual direction. **Ready to implement; runtime and player readiness are not established.**

Start the implementing agent with [AGENT_START.md](AGENT_START.md). Follow [TASKS.md](TASKS.md), T00–T12, using the concrete contracts, source map, transaction rules and test vectors in [IMPLEMENTATION.md](IMPLEMENTATION.md). [support_defaults.json](support_defaults.json) supplies initial values and all 25 mission policies for canonical configuration synchronization.

The [product plan](support_design_and_build_plan.md) explains the player experience and campaign progression. The [asset manifest](support_asset_manifest.md) requires existing game sprites/models. The original [campaign audit](../2026-09-28_pm_support-ability-campaign-audit.md) retains historical findings.

Approved selection reference: [full-screen v02](Mockups/05-support-fullscreen-v02-project-assets.png). Earlier drawers are superseded; v01 aircraft artwork is rejected. Battlefield [Smoke targeting](Mockups/02-smoke-preview-v02.png) and [ARIA consent](Mockups/03-aria-consent-v01.png) remain interaction references with the corrections recorded in [provenance](mockup_provenance.md).

Build and prove the Smoke slice first, then Strike, Paratroopers and Supply; activate campaign exposure only after each ability works. Mockup approval does not replace native visual review, automated checks, complete normal-input missions or real-player/device acceptance. See [validation notes](validation_notes.md).


## Cross-mode roadmap extension — 2026-09-28

The owner additionally requested Support planning for future Campaign missions, all 120 Skirmish battles and all 60 Operations. Follow [Roadmap/Support/PLAN.md](../../Roadmap/Support/PLAN.md) and its complete per-entry policy CSVs. Campaign remains the first implementation slice; X01–X05 then add separately validated mode adapters and coverage. This is authorization for the roadmap scope, not runtime acceptance or permission to expose old catalog availableModes without validation.
