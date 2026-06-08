Lane:
Art/Atlas

Task:
Produce the Art/Atlas scale/readability package after the user rejected temporary Gate 4 M01 art.

Files changed:
- `Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`

Contracts touched:
- M01 infantry and building/decor visible scale guidance.
- M01 selected-state art treatment guidance.
- M01 infantry atlas state readiness.
- No source/runtime contract files changed by Art/Atlas.

User-visible behavior:
No runtime behavior changed by Art/Atlas in this pass. This report defines the art-side scale/readability package that Gameplay and QA/HCI should validate against.

Validation run:
- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read `Design/AgentTasks/art-atlas_pm_message.md`.
- Read `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`.
- Read `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`.
- Read `Design/AgentTasks/M01_CRITICAL_PATH.md`.
- Read `Design/M01_FirstContact_Production_Contract.md`.
- Reviewed `Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_scale_contract.asset`.
- Reviewed `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/Manifests/Unit_Chr_Soldier_Male_02_FullSetup_Manifest.json`.
- Checked generated 2DISO marker/VFX art paths under `Assets/Game/Art/Generated/2DISO`.

Validation result:
Needs fixes for final art, but the metric scale direction is accepted for the next implementation/QA pass.

Handoff assessment:
- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`: accepted as the current rejection and routing source.
- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`: accepted as runtime implementation evidence for the metric-scale direction, ECS-only atlas quad path, small per-soldier selection markers, slower infantry motion, and pose changes while moving. It does not close Art/Atlas final asset gaps.

Scale/readability package:
- Reject the previous reviewed values as too small for public M01 review:
  - soldier scale around `0.1505` is not acceptable
  - building/decor scale around `0.14` is not acceptable for door/road-context readability
- Use the user-provided metric anchors:
  - soldier visual anchor: about `1.8m`
  - building door visual anchor: about `2.3m`
  - road/context: units and buildings must read against the road width and civilian-block context, not as tiny board-game counters
- Recommended implementation scale roles/values:
  - InfantrySquad / individual soldier atlas quads: `0.20` public M01 close-camera scale target. This is the minimum accepted direction for the next QA pass and should replace the rejected `0.1505` result.
  - CommandBuilding / visible M01 building-decor role: `0.80` close-camera readability target. This is the correct direction for door/road-context readability and should replace the rejected `0.14` tiny decor scale.
  - Small tent/camp props may remain smaller only when they are not used as door/building readability anchors and do not define the player’s sense of human scale.
- The current `chapter01_tactical_scale_contract.asset` matches this direction:
  - role `0` defaultScale `0.2` for M01 metric infantry
  - role `3` defaultScale `0.8` for M01 building/decor door/road-context readability

Selected-state art treatment:
- Reject the huge group marker/screen-covering marker.
- Reject the unclear blue marker as a primary selected-state treatment.
- Required treatment: small grounded selection marks under each soldier, or an equivalent subtle per-soldier bracket/contact-light treatment.
- Color/shape direction: restrained warm/amber or low-saturation tactical ground glow/bracket that frames each soldier footprint without covering the squad or terrain.
- The selected state should reinforce four distinct soldiers under one squad identity: one marker per soldier, not one oversized plate for the whole squad.
- The treatment must remain separate from the unit sprite and compatible with the ECS atlas quad path.

Infantry atlas/run-frame readiness:
- Current candidate sheet:
  - `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`
- The sheet covers four facings and these key-pose rows: idle, walk, run, aim, fire, reload, hit, death.
- It has a run key pose, so Gameplay can prove a temporary pose/state change while moving.
- It does not have enough multi-frame run/walk frames for final movement animation. The manifest explicitly classifies it as key-pose setup art and says walk/run/reload/hit/death need in-between frames before final runtime animation.
- Art/Atlas blocks final art signoff on replacement or expanded multi-frame run/walk loops if the next milestone requires final animation quality.

Destroyed/death treatment:
- Death/destroyed must remain part of the atlas visual state set.
- Do not restore a separate child `Destroyed` runtime dependency.
- The current sheet includes a death key-pose row, but final destroyed/death feedback still needs approved atlas-state art and/or approved VFX overlay art for `vfx.unit.destroyed.small`.

VFX and enemy-variant readiness:
- Generated 2DISO marker references exist for selection, move, attack, and capture point markers under `Assets/Game/Art/Generated/2DISO/GoldenAssets`.
- Final `vfx.impact.light` and `vfx.unit.destroyed.small` art are not present as generated 2DISO VFX assets in the reviewed folder. Current projectile/impact readability remains a runtime/test assertion, not final Art/Atlas signoff.
- No approved enemy red-accent/tinted patrol variant is present in the reviewed infantry package. Enemy readability still needs either an approved red-accent atlas variant or an explicit temporary tint/material waiver.

Known gaps:
- FinalAtlasArtReady should remain `0`.
- Current infantry source is AI-generated key-pose temporary art, not final multi-frame animation.
- Final/milestone run and walk loops are missing.
- Enemy patrol red-accent/final variant is missing.
- Final impact/destroyed VFX assets are missing.
- Fresh QA/HCI must verify the `0.20` infantry and `0.80` building/decor scale direction in public 16:9 and 20:9 captures.

Cross-lane impacts:
- Designer should codify the same concise metric contract: `1.8m` soldier anchor, `2.3m` door anchor, `0.20` M01 infantry scale direction, `0.80` building/decor readability direction, subtle per-soldier selection treatment, and final run-frame requirement.
- Gameplay owns runtime consumption of these scale roles, ECS-only presentation naming/proof, per-soldier marker implementation, movement speed, and temporary movement pose changes.
- QA/HCI should rerun after Designer and Gameplay handoffs are present, checking 16:9 and 20:9 public captures for infantry scale, building/decor scale, per-soldier selection, ECS-only atlas presentation, movement pacing, and run-pose readability.
- PM/user owns whether the key-pose sheet remains acceptable for another temporary milestone after the scale/selection/motion fixes.

Next recommended task:
Designer should deliver `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md` if it is not already present. Then QA/HCI should rerun after the Art/Atlas, Designer, and Gameplay rejected-temporary-art handoffs are all available.

Waiting on lane:
Designer, then QA/HCI

Waiting on exact file/report/asset/command:
- `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`
- QA/HCI rerun report after all three rejected-temporary-art handoffs are present

Owner of next action:
Designer owns the metric contract report. QA/HCI owns validation after required handoffs are present.

Can my lane still continue fallback work? no.
