# M3 and M4 — mobile game design and Editor audit

Date: 2026-09-13. Revision: `f9a7d597cba1d2f38b04b71cc4e1d03eca96525b`, branch `codex/m03-radar-warning`.

Scope: M3 **Radar Warning** and M4 **Airlift**, as corrected by the user. This is an audit and proposed correction list, not an implementation or release sign-off. No runtime code, assets, localization, or balance values were changed. All captures remain outside the repository; no evidence images were added to Design.

## Assessment

**27 findings: 10 high priority (P1), 17 medium priority (P2).** Both missions can complete and award their rewards, but they are not ready for a mobile experience sign-off. The most serious problems are M3 teaching a command that disables its defense, guidance disappearing before the main encounter, camera framing that conceals the actors, and M4 navigation assistance that points at the wrong subject.

Evidence labels distinguish **Live** observations in the current Editor runs, **Code** findings established by current implementation, and **Design** judgments about clarity, pacing, and satisfaction. A code finding is not represented as a manually reproduced playthrough. P1 means likely mission failure, inability to understand/progress, or materially misleading controls. P2 means a significant readability, feedback, consistency, or pacing problem.

| ID | Priority | Mission | Finding | Evidence |
|---|---|---|---|---|
| A01 | P1 | M3 | Stop lesson leaves the defense unable to auto-engage; following it lost the mission | Live + Code |
| A02 | P1 | M3 | Moved soldiers disappear behind the selection panel | Live |
| A03 | P1 | M3 | Tutorial can finish before the main convoy and leave ARIA blank | Live + Code |
| A04 | P1 | M4 | Opening returns to an excessively distant playable camera | Live + Code |
| A05 | P1 | M4 | Show Me points at the APC/helicopter instead of the required destination | Live + Code |
| A06 | P1 | M4 | Required landing/departure zones lack readable world guidance in the inspected views | Live + Code |
| A07 | P1 | M3 | Build popup displays placeholder resource balances | Live + Code |
| A08 | P1 | Both | Single-tap Destroy can sacrifice mission-critical actors without confirmation | Code |
| A09 | P1 | M3 | Movement lesson checks a narrower condition than its instructions explain | Code |
| A10 | P1 | M4 | Losing every escort is an insufficiently disclosed mandatory failure condition | Code |
| A11 | P2 | M3 | Build prices omit the credits charged by the transaction | Live + Code |
| A12 | P2 | M3 | Optional defense opens with a redundant Barracks selected | Live + Design |
| A13 | P2 | M3 | Farsi still exposes English building names and a soldier description | Live |
| A14 | P2 | M3 | Critical contact silently skips optional lessons | Live + Code |
| A15 | P2 | M3 | Casualty feedback can remain green, including a selected dead soldier | Live |
| A16 | P2 | M3 | Victory finale promises that the opening tutorial will start | Live + Code |
| A17 | P2 | M3 | A maximum-star route gives little reason to use the featured radar/build tools | Live + Design |
| A18 | P2 | M4 | Minimap clutter obscures the actual rescue roster | Live + Code |
| A19 | P2 | M4 | Enabled Jet shortcut selects nothing | Live |
| A20 | P2 | M4 | Status panel covers substantial battlefield space while its captions shrink/overflow | Live + Code |
| A21 | P2 | M4 | Selecting one specialist satisfies “select the four” | Code |
| A22 | P2 | M4 | Unload lesson drops contextual action help at a complex interaction | Live + Code + Design |
| A23 | P2 | M4 | ARIA tutorial narration is explicitly unavailable | Code |
| A24 | P2 | M4 | Instructions contain implementation explanations instead of concise action guidance | Code + Design |
| A25 | P2 | M4 | Rescue can finish before the patrol activates; landing hold becomes empty waiting | Live + Design |
| A26 | P2 | M4 | Helicopter returns home during the victory finale, leaving the exit shot empty | Live + Code |
| A27 | P2 | M4 | Results emphasize optional kills and do not explain individual star awards | Live + Design |

## Detailed findings

### A01 — Stop lesson undermines the defense

**Reproduction:** enter M3 with Full Guidance, select the eight rifles, move them to the road, execute Hold, then use the optional Stop lesson. Continue the later explanations without independently issuing another Hold or Attack.

**Observed:** the extra run issued Stop at approximately 16 seconds. Subsequent combat snapshots show `hold=False`, `AutoEngage=0`, and no acquired target. The mission ended in **Defeat at 131.146 seconds**. The lesson explains cancellation and that the battle continues, but does not explicitly explain that the squad stops acquiring/firing at enemies or give a recovery action. The Stop prompt also persisted until the critical-contact transition in this run.

**Why it matters:** a novice following ARIA can dismantle a working defense and then receive no instruction to repair it. This is stronger evidence than a hypothetical balance concern. It does not mean every use of Stop inevitably loses: the player can recover by issuing a suitable combat order.

**Direction:** teach Stop safely, show its combat effect, and explicitly guide the next defensive command. Validate the exact tutorial path without extra commands supplied by the QA driver.

**Evidence:** `m3-stop.log`, `m3-stop/039-Result-step0-t131.txt`. The existing probe has a hidden recovery: [M03RadarWarningEditorLaunchProbe.Guidance.cs:85](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/M03RadarWarningEditorLaunchProbe.Guidance.cs:85) issues Hold when `guidanceRestoreHold` is set; line 145 sets it after step 7. The audit removed only that driver recovery in the isolated project. Relevant copy: [M03RadarWarningTutorialCopyCatalog.cs:13](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Configs/Localization/M03RadarWarningTutorialCopyCatalog.cs:13); command path: [RtsSelectionImmediateSelectedUnitCommandSystem.cs:161](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionImmediateSelectedUnitCommandSystem.cs:161).

### A02 — Camera shows empty road while the selected squad is behind the HUD

**Reproduction:** complete M3's movement lesson with the selection panel open and leave the tutorial camera where it returns.

**Observed:** all eight soldiers at the road position projected to screen x **62–209** in the 1920×1080 capture. The opaque left selection panel occupies approximately x **14–456**. The entire squad is therefore hidden behind the panel while the central battlefield shows road/scenery. This is an occlusion problem even though the soldiers are technically inside the camera viewport.

**Direction:** frame the selected/action subjects inside the unobscured battlefield rectangle, with group extent and a margin. Reframe after a tutorial movement completes or provide a clear follow action. Test with both side panels open.

**Evidence:** `m3/025-Engage-step7-t43.png` and `.txt` (the rendered prompt is still Hold during that transition), `m3/032-Engage-step0-t83.png`.

### A03 — ARIA can go blank before the main encounter

**Reproduction:** continue the confirmed-threat and main-convoy explanations as soon as their buttons permit it.

**Observed:** the main-convoy explanation was acknowledged at approximately **53.6 seconds** in the first run. The main element activates at **140 seconds**. Guidance then became inactive; captures show a large ARIA panel without a current objective/body while the mission continues. A separate reactive warning can temporarily appear, but there is no dependable current-task fallback.

**Cause:** [MissionDefenseInteractionSystem.cs:83](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/MissionDefenseInteractionSystem.cs:83) accepts explanatory acknowledgements for steps 10 and 11 without their encounter conditions. [CampaignMissionGuidanceProjectionSystem.Defense.cs:110](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.Defense.cs:110) searches only the first eleven steps, then clears the projection when they are acknowledged.

**Direction:** distinguish acknowledging advice from completing a gameplay milestone. Keep a compact, persistent current objective and next event visible until the actual result; do not leave ARIA empty.

**Evidence:** `m3.log`, `m3/032-Engage-step0-t83.txt`; main activation is authored in [M03RadarWarningConfigBuilder.cs:92](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/M03RadarWarningConfigBuilder.cs:92).

### A04 — M4's playable camera is much too distant

**Reproduction:** enter M4 and let the introduction finish without manually using a focus control.

**Observed:** the returned camera has height **285.20**, pitch **67°**, FOV **55°**. The rescue actors become tiny within a map overview. By comparison, M3 now returns at approximately height 55.5. M4's later Show Me views can get much closer, but the initial playable view does not help the player identify the APC or four specialists.

**Cause:** the overview calculation frames the entire corridor and multiple subjects. It is used as a control view rather than a temporary establishing shot.

**Direction:** retain the wide establishing shot, then return to a close command view of the first actionable group. Apply bounds for readable actor scale and test the visible area after HUD occlusion.

**Evidence:** `m4/013-Engage-step2-t11.png` and `.txt`; [CampaignMissionSpawnSystem.MissionOverview.cs:8](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionSpawnSystem.MissionOverview.cs:8) and `:31`.

### A05 — M4 Show Me does not show where to go

**Reproduction:** press the actual Show Me button on lesson 6 (escort APC to Laila) and lesson 11 (fly to the exit).

**Observed:** lesson 6 focused the APC near the western pickup, approximately **246 world units from the landing destination**. Lesson 11 focused the helicopter at its current landing location. The latter instruction explicitly promises that Show Me locates the exit.

**Cause:** [UiShellEcsGateway.ExtractionCamera.cs:14](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ExtractionCamera.cs:14) prioritizes the carrier for lesson 6 and aircraft for lesson 11, overriding the destination represented in the guidance projection.

**Direction:** focus the required destination, or show a short subject-to-destination transition with a persistent destination marker. Preserve the player's return view.

**Evidence:** `m4-interaction-journey/interaction.txt`, entries `lesson6-show-me` and `lesson11-show-me`. These are real button interactions, not merely calculated target predictions.

### A06 — “Marked” landing/exit areas are not readable on the battlefield

**Observed:** inspected landing, hold, and departure views do not show a readable world-space perimeter or exit beacon explaining where the vehicle must be. The HUD has Landing/Departure camera buttons, but a camera jump alone does not communicate the accepted footprint or clearance condition.

**Impact:** the player must infer a position from scenery and wait to discover whether the hidden radius accepts it. This compounds A05. The scripted rescue driver succeeds because it knows destination coordinates.

**Direction:** show distinct landing and exit markers, a readable accepted area, and state changes such as approach, contested, holding, and cleared. Use the same geometry as the mission predicate.

**Evidence:** `m4/019-Engage-step7-t46.png`, `m4/022-Engage-step10-t71.png`; radius checks in [CampaignMissionRuntimeSystem.Extraction.cs:101](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem.Extraction.cs:101) and `:113`. This finding concerns the inspected player views; it is not a claim that no marker metadata exists anywhere in the project.

### A07 — Build balances are authoring placeholders

**Reproduction:** open Build during M3's optional defense lesson.

**Observed:** Build shows **12,450 materials, 3,280 oil, 6,750 fuel**, while the actual match HUD shows **100 materials, 0 oil, 10K fuel**. The numbers match the prefab builder's literal example values.

**Impact:** the purchase decision is based on false information. Players cannot trust affordability or understand a later rejection.

**Direction:** bind the drawer and HUD to the same authoritative live resource model. Test deliberately unusual balances so placeholder values cannot pass unnoticed.

**Evidence:** `m3/020-Engage-step4-t14.png`; [BuildDrawerV3PrefabBuilder.cs:311](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/BuildDrawerV3PrefabBuilder.cs:311).

### A08 — One accidental tap can destroy a required unit

**Code finding:** the selected-unit Destroy button invokes its callback directly, which queues `DestroyFocusedUnit`. Availability is ownership-based. No confirmation or hold interaction exists in this inspected callback chain, including for mission-critical transports.

**Impact:** a destructive control sits among ordinary selection actions on a touch interface. Destroying an M4 carrier before transfer, the helicopter, or a specialist can immediately invalidate the mission.

**Direction:** protect irreversible mission-critical actions with a hold/confirmation or remove the action where it serves no useful mission choice. Keep the touch target large; shrinking it is not a solution.

**Evidence:** [MatchHudSelectionPanelView.cs:444](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs:444), [SelectionGameplayStartupSystemHelper.cs:263](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:263), [SelectionUiCommandUiSystemHelper.cs:107](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionUiCommandUiSystemHelper.cs:107), [SelectionUiReadModelUiSystemHelper.cs:74](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionUiReadModelUiSystemHelper.cs:74), and [CampaignMissionExtractionRuleUtility.cs:8](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionExtractionRuleUtility.cs:8). This audit did not deliberately tap Destroy during the live rescue.

### A09 — M3's movement acceptance is more specific than the lesson

**Code finding:** the text permits a reachable location beside the convoy road and previously offers forward defense or defense near the post. Completion also requires a threshold of armed units, normally four, stationary inside their attack-range-minus-ten distance from a fixed fork anchor, plus an accepted Move command.

**Impact:** a reachable, apparently sensible position can leave the lesson waiting without explaining the missing count or spatial condition. The successful audit route used the driver's known accepted position, so it does not validate all positions the copy appears to permit.

**Direction:** align the completion condition with the offered tactical choices, or visibly identify the required area and number of soldiers. Explain unmet conditions instead of repeating a generic move instruction.

**Evidence:** [M03RadarWarningTutorialCopyCatalog.cs:9](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Configs/Localization/M03RadarWarningTutorialCopyCatalog.cs:9) and `:11`; [CampaignMissionGuidanceProjectionSystem.Defense.cs:50](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.Defense.cs:50), `:75`, and `:100`.

### A10 — M4's mandatory escort-survival rule is under-explained

**Code finding:** total loss of the armed escort causes defeat even if the specialists and transports survive. The principal briefing emphasizes all four specialists, vehicle survival, and the deadline; preserving the escorts is easily read as protection advice or a bonus-star goal rather than “at least one must survive.”

**Impact:** a player can make an apparently valid sacrificial covering action and lose for an undisclosed mandatory condition.

**Direction:** state the minimum escort-survival condition with the other mandatory objectives, or change the failure rule if sacrifice is intended to be a valid rescue strategy. Separate mandatory survival from the zero-loss bonus.

**Evidence:** [CampaignMissionExtractionRuleUtility.cs:8](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionExtractionRuleUtility.cs:8), [CampaignMissionRuntimeSystem.Extraction.cs:92](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem.Extraction.cs:92), [M04AirliftCopyCatalog.cs:11](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Configs/Localization/M04AirliftCopyCatalog.cs:11). This is a code/copy comparison, not a new live total-escort-loss run.

### A11 — M3 purchase price and budget are incomplete

**Observed/code:** a Guard Tower's transaction costs **22,000 credits and 50 materials**; its card/detail presentation exposes material/fuel prices but no credit price. The match header correctly uses oil/fuel, so the relevant mission spending budget is not communicated there either. This is separate from the false balances in A07.

**Direction:** expose the complete transaction price and relevant available budget inside Build. Preserve the user's required oil/fuel header. Affordability and rejection text must use the same quote as the transaction.

**Evidence:** [BuildDrawerCatalogPresentationSystemHelper.cs:344](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresentationSystemHelper.cs:344) and `:369`; [M03RadarWarningEditorLaunchProbe.Construction.cs:49](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/M03RadarWarningEditorLaunchProbe.Construction.cs:49); `m3/020-Engage-step4-t14.png`.

### A12 — Optional defense begins on the wrong purchase

**Observed:** ARIA asks the player to choose Tower or Barrier, but Build opens with Barracks selected even though M3 already supplies a completed Barracks. Its 90-material cost would consume most of the initial 100 materials.

**Impact:** the default and prominent green action lead toward a redundant purchase instead of the taught defensive choice.

**Direction:** open the appropriate category with a relevant defense selected, and make the existing production building discoverable. Do not automatically buy/place anything; each tutorial action should still correspond to one visible UI action.

**Evidence:** `m3/020-Engage-step4-t14.png`; [M03RadarWarningTutorialCopyCatalog.cs:10](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Configs/Localization/M03RadarWarningTutorialCopyCatalog.cs:10).

### A13 — Farsi still has English in the M3 branch

**Observed:** Build cards display **“Guard Tower”** and **“Road Barrier”** in a Farsi session. After casualties, the individual rifle selection panel displays an English description beginning **“Regular soldier armed with a rifle…”**, clipped in the available header space.

**Direction:** route these additional building and individual-unit paths through the localization catalog and run coverage against all M3/M4 selectable classes, not just the previously audited start-to-M2 flow. Verify rendered overflow as well as missing-key coverage.

**Evidence:** `m3/020-Engage-step4-t14.png`, `m3-stop/036-Engage-step0-t110.png`. The previous localization commit is not evidence that these branch-specific bindings are covered.

### A14 — Contact pressure silently skips teaching

**Observed/code:** around the first critical contact, guidance jumped from the optional portion to steps 10 and 11. The projection automatically marks optional steps, and the early defense-choice step, acknowledged when a critical warning is active.

**Impact:** a player reading at mobile speed may lose lessons without deciding to skip them or learning where to recover them. The full twelve-step sequence competes with the first element's 45-second activation.

**Direction:** make combat preemption explicit, keep deferred lessons discoverable, and avoid presenting later explanatory steps as completed gameplay. Validate slower readers and Farsi text length.

**Evidence:** `m3.log`, `m3-stop.log`; [CampaignMissionGuidanceProjectionSystem.Defense.cs:109](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.Defense.cs:109); [M03RadarWarningConfigBuilder.cs:92](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/M03RadarWarningConfigBuilder.cs:92).

### A15 — Casualties do not produce trustworthy selection feedback

**Observed:** in the winning run the group changed from eight to seven soldiers while showing a full **875/875** green bar. That fraction is mathematically valid for survivors, but the loss is easy to miss. More seriously, the Stop-loss run retained a selected individual with visibly **0/125 health and a full green bar**, an apparently ready order state, and selection commands still present.

**Direction:** clear or explicitly mark dead selections, derive bar/text from one consistent health state, and give brief squad-loss feedback that survives a change in the denominator. Confirm command availability after death.

**Evidence:** `m3/032-Engage-step0-t83.png`, `m3-stop/036-Engage-step0-t110.png`. Relevant presentation entry points: [SelectionHudFeedbackUiSystemHelper.cs:710](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs:710), [MatchHudSelectionPanelView.cs:261](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs:261) and `:459`. The exact cause of the zero-health/full-bar mismatch still needs implementation-level tracing.

### A16 — M3 finale reuses opening instructions

**Observed:** the victory camera displays **“MISSION OVERVIEW”** and says ARIA's tutorial starts when the camera returns, with a Step 1/1 presentation. The shot is a completed-mission finale, not an introduction.

**Impact:** the game contradicts the player's achievement precisely when it should confirm success. The shot mainly shows the post/scenery rather than the successful squad.

**Direction:** distinguish opening and finale presentation state/copy; show a victory-specific acknowledgement and frame a meaningful surviving subject.

**Evidence:** `m3-journey/finale-post.png`; [MissionDefenseHudView.cs:48](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/MissionDefenseHudView.cs:48) uses the generic tour read model to activate the opening hint. The English capture resulted from the probe's deliberate locale switch; it is not evidence of an automatic language-switch bug.

### A17 — Radar and building decisions lack a demonstrated payoff

**Observed:** the winning guided rifle-defense route finished at approximately **3:15**, with all seven hostiles stopped, three stars, no spending, and both radar charges retained. One rifle was lost.

**Design assessment:** allowing a skilled low-spend route is good. However, this route gives little feedback about why the mission's featured radar and defensive-building tools are useful. The tutorial introduces multiple systems while the successful play pattern can collapse to move, Hold, and wait.

**Direction:** create a clear information/positioning tradeoff and communicate the benefit of a timely scan or defensive investment. Preserve valid alternative strategies rather than making every optional tool a mandatory chore. Confirm balance with multiple genuine player strategies; one automated route cannot establish overall difficulty.

**Evidence:** `m3.log`, `m3-journey/result-fa-20x9.png`.

### A18 — M4's minimap overstates the useful rescue roster

**Observed:** many green markers crowd the map despite the active movable roster containing exactly **18 actors**: eight escort rifles, four specialists, APC, helicopter, and four hostiles. The clutter should not be interpreted as extra playable jets or an unintended active army.

**Code:** the stricter mission roster/map-building filtering is keyed to the M3 defense/intel context, not M4 extraction.

**Direction:** distinguish rescue objectives, controllable actors, map buildings, and unrelated markers. Prioritize the four specialists, vehicles, threats, and current destination, with readable grouping at mobile scale.

**Evidence:** `m4/013-Engage-step2-t11.png`, `m4-interaction-journey/active-roster.txt`; [MatchHudMinimapMarkerSystem.cs:33](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs:33) and `:148`.

### A19 — Jet shortcut is enabled but does nothing useful

**Reproduction:** tap the enabled Jet squad card in M4.

**Observed:** no jet exists in the mission's movable roster and the click selects no unit. The card visually advertises an available category. The Vehicles/Transport split also makes the APC's category less obvious than its tutorial name suggests.

**Direction:** bind availability to the live, controllable mission roster and make empty categories visibly unavailable. Use terminology/icons that let the player find the APC quickly.

**Evidence:** `m4-interaction-journey/jet-shortcut-selection.txt`, `active-roster.txt`; `m4/013-Engage-step2-t11.txt` records the enabled card. [UiShellEcsGateway.ReadModels.CommandHeader.cs:137](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.CommandHeader.cs:137) forwards the default card definitions.

### A20 — M4's mission panel spends space inefficiently

**Observed:** the extra central panel is about **887×209 pixels** at 1920×1080, approximately 46% of screen width and 19% of height, in addition to the resource header and ARIA. Yet all four metric captions and the route footer shrink to font size **12**, with TMP reporting overflow. The principal action buttons are large; the finding is not that every button is tiny.

**Impact:** important gameplay is covered while the information requiring that space is still difficult to read. Several equally prominent cyan buttons compete with ARIA's current action and the command tray.

**Direction:** integrate a compact objective/timer summary into the established HUD hierarchy. Prioritize the current rescue stage, retain large touch targets, and open secondary detail on demand. Check Farsi at supported landscape ratios and large-text settings.

**Evidence:** `m4/013-Engage-step2-t11.png` and `.txt`; [MissionSupportUiStyle.cs:83](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MissionSupportUiStyle.cs:83).

### A21 — Selection lesson advances with only one specialist

**Code finding:** the lesson requests all four specialists, but its completion uses `selectedTeam`, an OR across the team. Selecting one is sufficient to advance to the boarding lesson.

**Impact:** the next boarding command can affect an incomplete group. The player has seemingly followed a completed instruction but then waits for a four-passenger condition they cannot satisfy without correcting selection.

**Direction:** require the stated selection, or teach incremental boarding explicitly. Show selected/required count and provide a reliable mobile group-selection affordance.

**Evidence:** [CampaignMissionGuidanceProjectionSystem.Extraction.cs:38](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.Extraction.cs:38) and `:54`; `M04AirliftCopyCatalog.cs` lesson 4.

### A22 — Unload is a gap in contextual assistance

**Observed/code:** lesson 7 tells the player to use the APC passenger panel. At this step the green assistant action is correctly labelled **Field Guide**, and the action handler opens the guide rather than exposing the relevant passenger controls. The player must discover another UI surface at a multi-stage transport handoff.

**Direction:** let ARIA perform one visible navigation action, such as opening/highlighting the passenger panel, then teach the individual unload action. Preserve the user's one-click-per-Do-It requirement; do not silently execute a chain or transfer passengers.

**Evidence:** the rendered lesson-7 captures in `m4-interactions/`; [CampaignMissionGuidanceProjectionSystem.Extraction.cs:69](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.Extraction.cs:69); [MatchHudAssistantUiSystemHelper.Extraction.cs:8](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.Extraction.cs:8). This is an assistance-design gap, not a false claim that a button labelled Do It failed to unload. See the discarded probe observation under limitations.

### A23 — M4 tutorial has no ARIA narration path

**Code finding:** the tutorial narration gateway explicitly returns false in extraction context. The M4 tutorial therefore cannot play equivalent step narration through that path.

**Impact:** compared with narrated M3, M4 asks the player to read dense instructions while locating tiny actors and managing real-time transport. This weakens continuity and increases visual attention demand.

**Direction:** provide properly localized M4 step narration with replay/interruption behavior and matching subtitles. Do not substitute unrelated M3 recordings.

**Evidence:** [UiShellEcsGateway.TutorialNarration.cs:28](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.TutorialNarration.cs:28). This conclusion is based on the disabled tutorial path, not a claim that all M4 sound effects or every narrative sequence are silent.

### A24 — Instruction copy includes implementation commentary

**Examples:** the escort lesson explains that oil is نفت and fuel is بنزین, and warns not to confuse the fuel header with a passenger counter. The helicopter lesson describes its state as real rather than a cinematic shortcut. These are explanations of earlier implementation concerns, not the player's next decision.

**Impact:** long paragraphs consume ARIA space and reading time during a timed rescue. The actual verb, target, and success check are harder to find.

**Direction:** use one short action, one target, and one visible completion check per step. Put optional system explanations in the guide. Keep practical restrictions such as landed boarding and four required passengers.

**Evidence:** `M04AirliftCopyCatalog.cs`, lessons 6 and 8. This is a writing/design judgment, not a claim that the Persian words themselves are incorrect.

### A25 — Rescue pacing can bypass the threat entirely

**Observed:** ordinary transport-command rescue runs finished at **83.321** and **92.429 seconds**, both with **0/4 hostiles killed**, no escort/civilian losses, and all four specialists delivered. The patrol activates at **120 seconds**. The landing stage required its full 20-second hold with no contest in these routes.

**Design assessment:** avoiding combat should remain valid in a rescue. However, the explicit approaching-patrol setup and twenty-second hold provide little tension on these routes; the hold is largely waiting. These are knowledgeable automated routes, not estimates of first-time human completion speed.

**Direction:** tune pressure against rescue progress and intended difficulty, with fair advance warning. Give the hold a readable purpose and meaningful defensive choice; do not punish efficient play with an untelegraphed spawn.

**Evidence:** `m4.log`, `m4-interactions.log`; [M04AirliftConfigBuilder.cs:85](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/M04AirliftConfigBuilder.cs:85), `:97`, and `:103`.

### A26 — Extraction finale loses its hero vehicle

**Observed:** at the start of the finale the departing aircraft is partly behind the right HUD. By the settled exit-camera frame it is below the viewport and returning toward its home landing position. Subsequent state shows it landed back at home while the successful debrief proceeds. The camera focuses the static exit rather than the aircraft.

**Impact:** the rescue's visual payoff contradicts departure: the helicopter turns back and the camera watches empty scenery. This was movement during the finale, not an observed teleport.

**Direction:** give the finale explicit ownership of the aircraft's departure behavior and camera framing, then hand off cleanly after the visual exit. Do not allow idle return-home behavior to interrupt the success shot.

**Evidence:** `m4/026-SecureCorridor-step0-t83.txt`, `m4/027-Result-step0-t83.txt`; [CampaignMissionPatrolOrderSystem.DefenseCamera.cs:81](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Runtime/Missions/CampaignMissionPatrolOrderSystem.DefenseCamera.cs:81) targets the departure anchor for three seconds; [UnitAirMovementSystem.cs:939](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitAirMovementSystem.cs:939) implements the return-home movement.

### A27 — M4 results do not make the quality of the rescue clear

**Observed:** the result page fits and awards the correct reward, but prominently reports **0/4 enemy kills** on a three-star rescue even though kills are optional. It does not clearly explain the individual earned star conditions, such as time and escort preservation, alongside their result.

**Impact:** a clean non-combat rescue can appear incomplete. The player sees the reward but learns little about which decisions earned it or what a replay should improve.

**Direction:** lead with four people rescued, transfer/vehicle success, elapsed time, and each earned/missed star criterion. Treat optional kills as secondary statistics. Use this to reinforce the mission's rescue identity.

**Evidence:** `m4-journey/result-fa-16x9.png`. This is a result-hierarchy assessment, not a reward persistence failure.

## What passed in the current audit

- M3 entered its current twelve-step tutorial; the historical empty-on-entry/Step-1-of-1 screenshot was not reproduced at mission start.
- M3's initial playable camera is now much closer. Its remaining serious camera issue is HUD occlusion after movement, not the old initial overview distance.
- M3's match header showed materials, oil, and fuel with Farsi labels. The remaining balance/price problems are in Build and transaction presentation.
- A real M3 defense reached victory, three debrief panels, bilingual result views, reward persistence, and Campaign return with M4 availability preserved.
- M4 completed real APC boarding, driving, unloading, helicopter boarding, the 20-second clearance condition, airborne departure, debrief, bilingual results, saved unlocks, and Campaign return.
- The inspected M3 and M4 result layouts were readable in the captured Farsi landscape variants. Their functional success does not resolve the finale/feedback findings above.
- Close M4 Show Me views work as camera movements; the problem is choosing the right destination and returning to a useful play view.

## Method, evidence, and limits

The current working tree was clean at audit start. Assets, Packages, and ProjectSettings were synced into the existing isolated project `/private/tmp/warline-campaign-return-qa`. The user's main Editor was left running. All four validation launches used the checked-in macOS wrapper with GUI licensing, an explicit timeout and log, while Unity Hub stayed open. No Unity process was killed, no batchmode or alternate licensing route was used, and the normal user save was not used as the audit save.

The audit added temporary observation/interaction scripts only to the isolated QA project. They recorded rendered UI text, button bounds/availability, camera transforms, mission state, and actor screen coordinates. Transport/combat runs used ordinary commands, not position, health, mission-fact, or outcome rewrites. Temporary scripts were archived outside Git and removed; the two temporarily instrumented existing probe files were restored byte-for-byte from the main project after the runs.

| Run | Outcome | What it establishes |
|---|---|---|
| M3 guided defense, readable pacing | Wrapper exit 0; victory and Campaign return | Functional completion, current presentation, no-spend route, tutorial/camera problems |
| M4 rescue, readable pacing | Wrapper exit 0; victory at 83.321 seconds and Campaign return | Real transport chain, timing, current presentation and rewards |
| M4 visible interaction checks | Wrapper exit 0; victory at 92.429 seconds | Actual Show Me targets, empty Jet shortcut, exact movable roster |
| M3 Stop without driver's extra Hold | Wrapper exit 1; gameplay defeat at 131.146 seconds | Following Stop without unprompted recovery disables the working defense; this is an audit failure, not a licensing failure |

Evidence root: `/private/tmp/warline-mobile-audit-20260913/`. Names under individual findings are relative to that directory. Logs: `m3.log`, `m4.log`, `m4-interactions.log`, `m3-stop.log`. Instrumentation: `instrumentation/`. Captures and observations: `m3/`, `m3-journey/`, `m4/`, `m4-journey/`, `m4-interactions/`, `m4-interaction-journey/`, `m3-stop/`, `m3-stop-journey/`. These local temporary files are not durable repository evidence; the measurements and conclusions needed for this list are recorded above.

Source filenames above are under `Assets/Game/Scripts/`; resolve their full paths with the unique basename. Line numbers refer to the audited revision.

Limits and excluded claims:

- Editor-only, as requested. This is not Android hardware, touch accuracy, thermal, or audio listening certification.
- This combines automated command journeys, actual UI button checks, visual inspection, and source review. It is not a complete unaided novice playthrough, exhaustive bilingual/manual branch traversal, or player study.
- Screen capture/inspection overhead invalidates these runs as performance benchmarks. No new FPS/P95 claim is made, and no full architectural suite was rerun for this audit-only task.
- One observation can record a new tutorial state a frame before the rendered UI catches up. Findings use the rendered state when identifying visible copy or button labels.
- The `lesson7-do-it guide=False passengers=0` entry in `m4-interaction-journey/interaction.txt` is **discarded as evidence of a broken unload button**: it ran before the corresponding rendered lesson and after the existing driver had queued unload. A22 instead uses the visible Field Guide label and the verified action mapping.
- No fresh exhaustive animation, LOD, grounding, road-placement, flashing-alert, retry, loss-recovery, reduced-motion, or all-class guide regression was completed. Historical screenshots or prior pass reports are not counted as current failures or current passes for those areas.

## QA changes needed before a future readiness claim

1. Run M3's displayed tutorial sequence with no hidden Hold restoration, secret coordinates, or unrelated recovery commands. Assert that its instructions leave a viable defense and explain any recovery.
2. Check actor bounds against the **unobscured** battlefield, not merely camera viewport bounds. Include selection panel, ARIA, command tray, and mission panel; verify moving action and both finales.
3. Test Show Me against the destination promised by the current copy. Verify a player-visible world marker and accepted region, not just a camera request.
4. Assert presentation truth: actual balances/prices, full selection count, live health, roster-based shortcuts, and the correct opening/finale/current-objective text.
5. Add slow-reader and alternative-strategy journeys, including manual selection of a subset, voluntary Stop, near-post defense, missed optional lessons, contested landing, and efficient non-combat rescue.
6. Verify localized text at supported landscape ratios and large-text settings, while preserving generous touch targets. Then assess pacing with real players rather than treating an automated maximum-star result as evidence of fun.

Recommended correction order: A01–A10 first; then navigation/selection feedback and HUD readability; then narration, pacing, and victory presentation. No corrections are included in this audit document.
