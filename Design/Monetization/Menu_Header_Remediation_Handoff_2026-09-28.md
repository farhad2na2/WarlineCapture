# Main menu, headers and content access: agent handoff

Date: 2026-09-28. Status: implementation plan for another agent; Campaign-led home and updated account-header mockup v01 approved as visual direction by the user's instruction to update this handoff so the other agent implements the main menu “like this.” No UI implemented by this document. Baseline inspected: `3d728d005`, with active uncommitted mission-resource and Support presentation work. Recheck the live checkout before editing.

## Objective

Implement the Campaign-led main-menu redesign and updated account header together, following the approved mockup below. Make mode selection, content store and related status screens accurately express the [monetization plan](Monetization_Plan_2026-09-28.md). Reuse the game's existing Campaign intro/comic art, typography, portraits, colorful controls and mobile spacing. Remove misleading currency/energy/purchase cues and distinguish access, earned progression, readiness and downloads. This assignment includes the full home composition, not just removing a header counter.

Read the [mission product contract](Mission_Product_Contract_2026-09-28.md), [205-entry policy register](Mission_Product_Policies_2026-09-28.csv), [mission-remediation handoff](Implemented_Missions_Remediation_Handoff_2026-09-28.md), [main-menu visual contract](../UIUX_MainMenu_Visual_Contract.md), [first-player flow](../First_Player_Experience_And_Story_Onboarding_Design.md), [economy authority](../Economy_Reward_Design.md) and root [AGENTS.md](../../AGENTS.md).

This assignment covers presentation, navigation, binding and their validation. The mission agent owns tactical cost migration, mission rewards, entitlement evaluation and Operations run scope. Do not build a second wallet, purchase verifier or content-access service inside the UI. No new live products, hardcoded final prices, paid ads, release deployment or asset purchases are authorized by this handoff. Keep commerce disabled until the complete advertised content and real billing/restore gates pass.

## Approved home and header direction — implement together

Primary visual authority for this redesign: [home-campaign-header-v01.png](../AgentReports/MenuHeaderMonetization/Mockups/home-campaign-header-v01.png). Read the [generation prompt, source references and review record](../AgentReports/MenuHeaderMonetization/Mockups/home-campaign-header-v01.md). The user requested this reference-based ImageGen mockup and then asked to update the handoff so the other agent also updates the main menu “like this.” This records approval of the shown home/header composition; do not ask again for the same direction. It does not approve unseen store, mode-selection or match-HUD redesigns or establish runtime acceptance.

| Region | Implementation requirement |
|---|---|
| Header | Slim dark full-width strip with existing WARLINE CAPTURE logo on the left, one earned Credits display and Settings on the right. Remove Command and currency purchase-plus controls. Retain profile access through the Commander card; do not duplicate an oversized portrait in the header. The pictured `2,400` is an illustrative balance, never an initialization value. Credits can remain informational when no cosmetic destination is ready; do not invent a tappable dead end. |
| Campaign main area | Replace the dominant full-size male commander background with spoiler-safe artwork from the current Campaign mission's intro/comic. The scene fills the main left/center area, with readable gradient-backed Campaign label, chapter/mission, mission title, one short purpose line and a large green Continue Campaign action. Preserve visible art and faces; do not show comic playback controls or baked dialogue. Steel Push is an example state, not a fixed home mission. |
| Other modes | Two large side-by-side cards below the Campaign area: green Operations with “Multi-mission strategic operations”; orange-red Skirmish with “Standalone tactical battles.” Preserve distinctive mode icons/art and large touch targets. Cards open their own existing mode flows directly; they are independent game modes, not Campaign sub-missions. Respect first-session reveal rules. |
| Right column | Keep a compact cyan ARIA portrait panel above the dedicated Commander card. Move the existing selected commander portrait into that card, replacing the ambiguous Change treatment with Commander and View Commander. Bind the actual selected identity and preserve access to commander selection/profile. ARIA remains included; do not add a premium lock or invent a new home action. |
| Secondary navigation | Store and Armory become smaller stacked buttons at the bottom of the right column. Keep their existing functional routes while applying the monetization requirements below. Remove the dominant full-width shopping footer. |

Use the image's relative hierarchy, not rigid coordinates: approximately three quarters main area and one quarter side column, adjusted for safe areas, Persian and larger text. Reuse actual project fonts, portraits, icons and clean scene art. The generated image is a composition reference, not a single flattened shipping UI or a replacement portrait pack. Localize all labels and render controls/text natively. Responsive adjustments within this direction do not need another visual-direction approval.

**ARIA identity is locked.** The user explicitly corrected the generated mockup: “obviously aria should keep her face not drift like in this image gen”. Reuse the exact existing ARIA portrait asset from the native game; preserve her face, hair, expression and established cyan holographic treatment. Do not regenerate, redraw, beautify or substitute the mockup's altered face. The mockup approves ARIA panel placement and sizing only, not its generated portrait. Fit the original asset without facial distortion or a crop that changes her recognizable appearance. Record the source asset path and verify it against the existing native reference during visual acceptance.

### Campaign destination and artwork must agree

Resolve one authoritative home Campaign target from saved progression and current content access/readiness. Use it for the title, purpose, artwork and navigation intent. Continue Campaign opens the Campaign screen focused on that mission for review/briefing; it must not deploy directly, skip story, or start ARIA. Revalidate when activated. Preserve cold open → identity → M01 → first debrief → headquarters, where continuation leads to the appropriate next mission.

Do not simply choose the highest unlocked bit or the last sprite available. The current `MainMenuCampaignCardView` scans unlocked missions backwards and can choose another mission's art when the target has none. Replace that presentation selection with the shared target. Use a neutral Campaign fallback if target art is missing, never unrelated mission art. On completion, offer Choose Mission or Replay Campaign instead of an impossible next mission. For ownership/progression/readiness restrictions, show a truthful navigation action and reason without implying a purchase fixes unavailable content. No automatic purchase prompt, fabricated checkpoint Resume, or unseen story spoiler.

Update the older main-menu visual contract to this approved hierarchy as part of implementation, while preserving onboarding, accessibility and routing requirements. The old commander-dominated composition and full-width Store/Armory footer no longer govern this redesign.

## Product facts every screen must share

| Offer | Content | Access rule |
|---|---|---|
| Free Campaign | CH01-M01–M05, complete chapter story and replay | ARIA included; ordinary earned progression applies. |
| Free Skirmish | Certified S001 sample, unlimited replay | Same scenario rules and ARIA as the owner's copy. |
| Free Operations | Certified O001–O003 standalone introduction | Requires the actual finishable intro scope, not an incomplete six-district run. |
| Campaign Edition | All 25 Campaign missions **and S001–S120** | One permanent content package; do not sell Skirmish a second time to its owners. |
| Operations: Sahrin Theater | Complete O001–O060 and six-district conclusion | Independent purchase; no Campaign Edition prerequisite or district tolls. |

Future additional collections may be separate products; the original 120 Skirmish scenarios stay included. ARIA, mission retries and normal tactical time controls are not premium upgrades. Prices in the planning document are hypotheses; production labels must use real localized storefront metadata. Counts describe planned membership until every advertised entry is release-certified. A product promise or `Playable` asset flag is not a release gate pass.

## Source map and current problems

These are starting points, not a promise that each file owns all behavior. Trace actual prefab bindings and current callers before changing them.

| Boundary | Existing source | Inspect/change |
|---|---|---|
| Menu initialization | [MenuBootstrapCompositionSystemHelper](../../Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs) | Seeds `12,450` Credits and `78/100` Command; replace runtime placeholder initialization with authoritative state. |
| Shell defaults | [UiShellEcsGateway.ReadModels.DefaultState](../../Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs), [UiShellStateSystem](../../Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs) | Duplicate placeholder resource values can reappear after navigation/recreation. Use the same profile-backed projection and explicit loading/error state everywhere. |
| Header prefab authoring | [MainMenuPersistentResourcesPrefabBuilder](../../Assets/Game/Scripts/Editor/MainMenuPersistentResourcesPrefabBuilder.cs), [MainMenuV3PrefabBuilder](../../Assets/Game/Scripts/Editor/MainMenuV3PrefabBuilder.cs) | Audit authored sample values, resource slots, plus-button routes and responsive layout. Update the builder and generated assets together through checked Editor tooling. |
| Home Campaign artwork/target | [MainMenuCampaignCardView](../../Assets/Game/Scripts/UI/Screens/MainMenuCampaignCardView.cs), [MainMenuV3PrefabBuilder](../../Assets/Game/Scripts/Editor/MainMenuV3PrefabBuilder.cs) | Replace the commander-dominated background and small Campaign card with the approved main area; select art and navigation from the same authoritative Campaign target. Inspect `MainMenuCommanderVariantView` bindings before relocating the selected commander to its card. Remove obsolete hotspots and update builder validation paths when hierarchy changes. |
| Mode navigation | [MainMenuNavigationView](../../Assets/Game/Scripts/UI/Screens/MainMenuNavigationView.cs), [CampaignOperationsScreenView](../../Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.cs) and its partials | Preserve actual routes, chapter progression, Story Archive and next-mission behavior; bind access/readiness without inventing progress. |
| Store | [StoreCommandExchangeV3View](../../Assets/Game/Scripts/UI/Screens/StoreCommandExchangeV3View.cs) | Static consumable offers, hardcoded dollar prices and scarcity text; purchase currently disabled. Replace the preview catalog with truthful content/ownership states, not a fake working checkout. |
| Operations dashboard | [OperationsMissionPresentationSystem](../../Assets/Game/Scripts/Composition/OperationsMissionPresentationSystem.cs) | Already reads real profile information in some paths. Reuse it; do not assume every visible balance is fake or create another owner. |
| Match resources | [MatchHudResourceHeaderPresentation](../../Assets/Game/Scripts/UI/Screens/MatchHudResourceHeaderPresentation.cs) and [UiMissionHudRestrictionsModel](../../Assets/Game/Scripts/UI/Contracts/UiMissionHudRestrictionsModel.cs) | Active mission-agent work. Integrate the agreed resource model; do not independently alter Money/Materials costs, Fuel accounting or slot semantics. |

The older main-menu contract still names Command as locked text. Amend that obsolete content requirement as part of this task while preserving the visual identity. Existing old targets are visual references, not instructions to retain retired wallets or fake offers.

## Ownership and dependencies with the mission agent

At handoff creation, the dirty files include `MatchHudResourceHeaderPresentation.cs`, `UiMissionHudRestrictionsModel.cs`, mission initialization/validation, construction costs, scenario builders and Support UI. Treat these as active shared work; do not overwrite, regenerate over or reset them.

| Owner | Responsibility | Consumer contract |
|---|---|---|
| Mission remediation IM-01–03 | Tactical resource ownership/costs, earned reward settlement and legacy-save migration. | Menu reads actual account totals; match presentation reads authoritative local resources and scenario visibility. No duplicated balance arithmetic. |
| Mission remediation IM-04 | Product membership and content-access evaluation outside simulation. | UI consumes separate ownership, progression, readiness and installed-content facts; actions revalidate through the same gateway. |
| Mission remediation IM-06 | Intro/full Operations scope, completion, persistence and first-clear deduplication. | Dashboard receives scope, available missions, AP/day and intro-complete state. UI never computes city victory or migrates run state itself. |
| Menu/header agent | View models at its assigned presentation boundary, prefabs, localization, layouts, navigation and purchase-state presentation. | Forward intents; render authoritative values/reasons; preserve existing input and accessibility contracts. |
| Commerce integration | Platform metadata, verified entitlements, purchase/restore lifecycle. | Injectable interface for UI testing; no shipping local `owned=true` shortcut or fabricated successful transaction. |

Record the agreed fields and owners before editing shared files. A conceptual UI state may include profile readiness/Credits, product ownership, content readiness, progression reason, download status and localized offer metadata; use existing project contracts where possible rather than adding a parallel architecture. Preparation, mockups and isolated presentation fixtures can proceed while another dependency is unfinished. Shared mutations wait for integration with their owner. Do not message another chat without user authorization.

## Screen-by-screen requirements

| Surface | Required behavior |
|---|---|
| Account/menu header | Follow approved v01: logo, real earned Credits and Settings, with identity/profile access in the Commander card. Remove the active Command counter, `78/100` energy implication and currency purchase plus-buttons. XP/stars are progression, not a new currency strip. No tactical Materials/Oil/Fuel here. |
| Unready account data | While loading or unavailable, show a deliberate neutral/loading state rather than fake balances or a fabricated zero. A valid zero balance must display correctly. Keep the approved Credits display informational if no usable cosmetic destination exists; do not add a dead-end currency CTA. |
| Main menu | Implement the approved Campaign artwork/Continue area, larger separate Operations and Skirmish cards, right-side ARIA/Commander and secondary Store/Armory controls. Continue Campaign opens Campaign focused on the shared target. Preserve cold open, identity, M01 and first debrief; no new store interruption. Respect authored mode reveal points and do not describe unfinished content as purchasable. |
| Campaign screen | Chapter 1 free; later chapters belong to Campaign Edition. Show progress separately from ownership. An owner who has not cleared the previous mission sees the actual progression requirement, not another Buy button. Story/debrief/archive behavior follows content access and earned records. |
| M05 transition | Complete story, fragment and rewards first. Then an eligible finished product may offer View Campaign Edition, replay/free modes and return. No repeated sales modal on every replay or later menu return. If product/commerce readiness is missing, preserve continuation/navigation appropriate to the development build without a live purchase claim. |
| Skirmish library | S001 sample is identifiable. The core collection is all 120 S-IDs; ownership covers every certified included configuration. Distinguish unfinished content and unsupported device sizes from product locks. Do not add per-scenario prices or account-roster grind. |
| Operations entry/dashboard | Identify the free introduction versus full theater and show the correct scope's purpose/progress. Retain strategic AP, day, district facts and End Day explanation locally on the dashboard. Remove paid AP/supply/recovery/Black Market routes. Intro completion/replay comes from the scope model. |
| Store / content page | Retain the established route where practical; simplify to released Game Content and later implemented Cosmetics. Retire Starter Packs, Resources, paid Armory/OperationSupply, season offers, fake discounts and countdowns. Show exact contents, separate theater, owned state and restore access. Do not advertise the unaccepted 25/120/60 collections as finished. |
| Purchase details | Actual localized price, one-time ownership, included content, exclusions, download needs if supported, restore and clear transaction status. No claim of all future content or cross-platform ownership without implementation. |
| Profile / Armory / rewards | Show real earned milestones, collection ownership and cosmetic use. Preserve existing earned records. Remove paths selling missing parts, readiness or permanent tactical power. Do not invent a cosmetic catalog or reward replacement to fill space; consume the implemented economy. |
| Settings / restore | Expose the supported restore/refresh action with checking/success/nothing-to-restore/failure states. Distinguish content ownership from save recovery. Progress reset must not erase verified purchases. No nonfunctional Restore button pretending a backend exists. |
| Match header | After mission-resource integration, display only resources relevant to that mission: Materials/Oil/Fuel plus separately identified tactical constraints when appropriate. Keep Steel Push's usable Fuel visible and civilian reserve meaning accurate. Do not show account Credits or replace Fuel with Credits. Preserve mission purpose/objectives, normal command controls and visible ARIA Play/Stop. |
| Defeat / Partial / result | Explain actual outcome and offer normal retry/help/return. No ad revive, paid rescue, currency multiplier or timed offer. Header updates after exactly-once reward settlement and never grants rewards itself. |

Removing a view must also remove its focus/hit target and obsolete navigation route. Do not leave invisible plus-buttons, stretched intercepting panels or selectable dead slots. Keep useful spacing; do not fill the removed Command slot with a speculative resource.

## Required presentation states

Use one state model across all mode cards, details and Continue/deploy paths. At minimum cover: free and ready; owned and ready; owned but prior mission incomplete; product not owned but released; content not released/certified; supported content not installed; store metadata unavailable; purchase pending; cancelled/failed; verified purchase; restore checking/complete/failed; known owner offline; and confirmed revocation applied at a safe boundary.

When more than one restriction applies, do not imply purchase will fix an unavailable build/device. Separate owned status from deployability and explain the actionable next requirement. Repeated taps and stale views must not create duplicate requests. Pending/cancelled/failed transactions never display Owned. Connectivity failure alone does not revoke known ownership. Never open commerce during combat or let a late callback mutate a mission outcome.

## Work packages and visual gate

| Package | Work | Exit requirement |
|---|---|---|
| MH-00 | Inspect current native menu/mode/header states, paths, data sources, dirty-file owners and evidence. | Surface inventory, exact baseline, dependency contract and representative EN/FA captures. |
| MH-01 | Consume approved home/header v01 and its review record. Produce additional actual-reference mockups only for materially new directions on content store, mode access screens or match headers. | Home/header visual-direction review is satisfied; do not block it on repeat approval. Unseen substantial redesigns still require review. Mockups are not runtime acceptance. |
| MH-02 | Bind truthful account state and remove active Command/plus-button routes; keep legacy storage intact. | Shared profile updates, valid zero/large balances, loading/error states, no placeholder flash or duplicated observers. |
| MH-03 | Implement the full approved main-menu composition with MH-02; relocate commander, enlarge both other-mode cards, reduce shopping prominence, bind the Campaign scene/copy/action to one target. Update Campaign/Skirmish selection and Operations entry to consume shared access/scope models. | Native home matches approved hierarchy; artwork and destination agree; correct first-session reveal, free/owned/progression/readiness states and normal navigation. |
| MH-04 | Replace store preview categories and add purchase/restore presentation behind real integration/readiness gates. | No fake prices/scarcity/success; comprehensive state fixtures; no active purchase for unfinished products. |
| MH-05 | Integrate resource-header presentation with the mission agent; update builders/prefabs/localization together. | No duplicate resource logic or loss of Fuel/objectives/ARIA; actual native affected-mission review. |
| MH-06 | Validate ordinary navigation, settlement updates, accessibility/localization and lifecycle; update visual/economy authorities. | Evidence categories and pending gates reported separately; no unsupported release/readiness claims. |

Root AGENTS.md requires: “For a new mission screen or substantial mission UI redesign, create ImageGen mockups using actual Campaign UI references and obtain the user's visual-direction review before implementation.” The [monetization plan](Monetization_Plan_2026-09-28.md) also requires review for material changes to existing commerce/menu screens. The linked home/header v01 and subsequent user instruction satisfy this direction gate for the home/header redesign. Approval does not extend to unseen mockups. Routine data-binding fixes and choices within the approved direction do not require repeated approval.

Reference candidates: current native captures first, then [Campaign selection target](../VisualLockLayered/SCN-05_CampaignOperations/reference/SCN-05_CampaignOperationsV3_MissionSelect_Final_Target.png), [main-menu target](../VisualLockLayered/SCN-02_MainMenuV3/reference/SCN-02_MainMenuV3_Final_Target.png), [store target](../VisualLockLayered/SCN-14_StoreCommandExchange/reference/SCN-14_StoreV3_Final_Target.png), and [Steel Push native HUD](../AgentReports/CH04M02SteelPush/VisualReview/en-hud-aria-5.png). Inspect references before using them; legacy target text/catalogs do not override the current product contract.

## Validation and delivery

Use root AGENTS.md and the Unity CLI skill when controlling an Editor. Unity execution/captures must follow the checked repository wrappers with explicit logs/timeouts/markers, Hub open and signed in, no direct executable or macOS batchmode. Prefer the verified existing Editor/reuse path when appropriate; do not stop another agent's Editor, reset IPC or overwrite generated work. Modify Unity scenes/prefabs through checked Editor tooling, not hand-edited YAML.

Acceptance cases:

1. Fresh profile follows cold open → identity → M01 → debrief → headquarters. Continue leads to the correct next mission; Settings/profile/archive remain reachable.
2. Real reward settlement updates the same Credits value on all relevant menu/profile screens exactly once; navigation/reopen/reload does not restore `12,450`, `78/100` or prefab sample values. Profile loading/error does not flash a fake balance.
3. Free, Campaign-only, theater-only and combined-owner fixtures produce correct content states. Campaign owners are never asked to buy the included Skirmish library again. Unfinished and unsupported-device entries never masquerade as purchasable fixes.
4. Purchase/restore pending, cancellation, failure, offline and unavailable-metadata states behave truthfully under an injectable fixture; real store-sandbox acceptance is separate and only possible after billing exists. No ownership test shortcut ships enabled.
5. Complete normal-input M05 result/return and an affected Materials/Fuel mission journey, including visible ARIA Start/Stop, with the integrated candidate. Run the Operations introduction result/return path when its scope dependency is ready; do not fabricate its completion for a readiness claim.
6. EN/FA at 16:9 and wide mobile ratios, safe areas, long labels, zero/large numbers, enlarged text and actual touch hit targets. Preserve established 80-pixel reference controls where applicable. Verify focus, back navigation, modal raycasts and responsive wrapping.
7. Menu → mode → briefing → match → result → menu cycles preserve selection, profile, language and input; stale resource/listener state cannot leak across modes. No disabled or hidden route remains tappable.
8. Separate mockup approval, native visual review, automated tests, agent-operated normal-input play, real-user acceptance, packaged build and physical-device results. Retain failures and current source/config hashes.
9. Capture the implemented home beside approved v01 at 16:9 and wide aspect ratios: dominant Campaign artwork/action, readable mission purpose, larger independent Operations/Skirmish cards, selected commander confined to its card, visible ARIA, compact secondary navigation and Credits-only header. Verify native labels and hit targets rather than treating the generated image as implemented UI.
10. Exercise Campaign targeting after a clear, return/reload, missing art, no next playable mission, all accessible missions completed and access/readiness changes. Title/art/selected mission must agree; Continue opens selection without deploying. Verify Operations, Skirmish, Commander, Settings, Store and Armory routes plus focus/back navigation after the hierarchy change.
11. Verify ARIA uses the unchanged existing native portrait asset, not the generated mockup likeness. Compare her identity against the original native home capture at both target aspect ratios; check scaling/cropping and record the bound asset path.

Deliver `Design/AgentReports/MenuHeaderMonetization/` containing the source/surface inventory, dependency ownership, mockups and review decision, before/after native captures, state matrix, input/test logs and final handoff. Keep product copy, visual contract, prefab builder and implementation aligned. Report code completion separately from pending visual, billing, content, human or device gates. Commit only this agent's assigned work unless explicitly authorized otherwise; preserve the concurrent mission changes.

## Copy-ready instruction

> Implement `Design/Monetization/Menu_Header_Remediation_Handoff_2026-09-28.md`, including the full main-menu redesign and header together. Follow the user-approved `Design/AgentReports/MenuHeaderMonetization/Mockups/home-campaign-header-v01.png`: large current-mission Campaign artwork and Continue Campaign, larger separate Operations/Skirmish cards, dedicated right-side commander and ARIA, smaller Store/Armory, and Credits-only header plus Settings. ARIA must retain her exact existing native portrait asset and face; the mockup's generated likeness is explicitly rejected and only its placement/sizing is approved. Do not ask again for this approved direction. Continue opens Campaign focused on the same mission used by the art/copy; do not hardcode Steel Push or the sample balance. Start by rechecking active mission-remediation work and resource/access view-model ownership. Consume authoritative state and coordinate match-header integration; do not duplicate economy or entitlement logic. Preserve onboarding, the existing visual identity, all 120 core Skirmish scenarios, included ARIA and the separate full Operations theater. Additional unseen substantial screen redesigns require their own visual review. Do not activate live purchases or fabricate availability, prices or ownership. Validate native EN/FA ordinary-input journeys and record mockup approval separately from native, gameplay, billing, human and device acceptance.
