Status: needs fixes
Topic:
QA/HCI focused Gate 4 rerun after M01 opening-control and atlas-quad handoff

Lane:
QA/HCI

Task:
Rerun focused Gate 4 validation from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` after PM accepted the Gameplay architecture handoff for M01 opening control and ECS atlas presentation.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`

Workspace preparation:
- `/Users/farhad/Projects/WarlineCapture-CodexUnity3` was stale and did not contain the new Gameplay handoff report, tests, atlas quad runtime, or opening-control protection system.
- Refreshed only the focused Gameplay handoff files listed in `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` into `WarlineCapture-CodexUnity3`.
- Verified the refreshed workspace contains `MissionRuntimeOpeningControlProtection`, `MissionRuntimeAtlasQuadRuntime`, and the new focused tests:
  - `PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup`
  - `GameScene_M01OpeningControlWindowPreventsLethalEnemyFireUntilFirstMove`
  - `GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds`

Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-qa-gate4-focused-results.xml -logFile /private/tmp/warlinecapture-qa-gate4-focused.log`
- Reviewed generated public launch captures:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`

Validation accepted:
- Focused PlayMode rerun passed in the refreshed QA workspace: `Chapter01M01PlayModeValidationTests` 8/8, exit code 0.
- Public Campaign route automated golden path reaches result popup.
- Opening-control protection test passes: hostile fire does not create lethal first-control failure before the move teaching step.
- M01 select, move-to-cover, attack, patrol neutralization/result-route readiness, and build-rejection feedback coverage pass.
- ECS atlas architecture assertions pass: `MissionRuntimeAtlasQuadRuntime` is required, `MissionRuntimeSpriteRendererRuntime` is rejected, separate M01 destroyed visual components are rejected, and tactical projectile trace sizing is asserted.
- Automated scope assertions still report one player command squad and one hostile patrol with no player vehicle entities, build entry, transport, base, or extra player unit type.

Needs fixes:
- Public capture HCI does not yet meet Gate 4 visual-readability bar. At gameplay camera scale, the player rifle squad is tiny; in the 20:9 captures it is visible but still hard to parse as four distinct soldiers, and in the 16:9 Campaign/Quick Custom captures the first-control composition does not clearly present the player's squad as the immediate focus.
- Selected-state clarity cannot be accepted from the public captures. Automated tests assert a selected marker, but the captured player-facing image does not clearly show a readable world selection state at the squad.
- M01 infantry-only scope is visually contradicted by the HUD. The public M01 bottom bar still shows APC, Tank, air support, and Build affordances/cards. Even if tests prove no vehicle/build entities are spawned or usable, this is a first-mission HCI defect because the player is being shown vehicle/build options in an infantry-only tutorial.
- Current unit art remains temporary. The Gameplay handoff and tests explicitly keep `FinalAtlasArtReady = 0`; QA/HCI cannot sign off final visual quality while the unit states still depend on temporary manifest source art.
- Touch/camera ergonomics were not manually validated beyond automated route coverage. The current capture composition suggests the camera framing does not reliably prioritize the command squad at first control.
- Invalid command recovery and assistant ownership/Stop behavior were not revalidated in this pass after the gameplay presentation change.

Log classification:
- Focused tests pass, but logs still include repeated `Animator is not playing an AnimatorController` warnings from `MenuView` panel show/hide paths.
- Log still includes known editor/tooling noise: XcodeApplications plist warnings, preview scene leak warning, persistent allocation leak warning, and usbmuxd shutdown noise.
- `FrameRateDiag:PreGame` showed low initial fps in batchmode (`fps=3.2`, avg frame `316.5ms`) during menu/bootstrap. This did not fail the test, but QA/HCI should not use this batchmode run as performance acceptance.

Owner lane:
- UI owns the visible M01 HUD affordance mismatch: remove, lock, or clearly suppress APC/Tank/air support/Build affordances for the M01 infantry-only teaching slice.
- Gameplay/Art owns unit visual readability at gameplay camera scale: four soldiers, selected state, grounding/contact, and temporary/final atlas art readiness.
- QA/HCI owns the next validation after fixes land.
- Support/FTUE has no new action unless assistant/Stop or invalid-command recovery fails in the next pass.

Can another lane continue:
- UI can continue immediately on M01 HUD scope/readability fixes.
- Gameplay/Art can continue immediately on unit scale/readability/selection marker/art readiness.
- QA/HCI should not produce final Gate 4 acceptance until the visible HCI issues above are fixed or explicitly waived by PM/user.

Next recommended task:
- Route UI to make the M01 public HUD match infantry-only scope and first-control teaching.
- Route Gameplay/Art to improve public camera-scale readability of the four-soldier squad and selected marker, or prepare an explicit temporary-art approval package.
- Rerun QA/HCI Gate 4 after those fixes.
