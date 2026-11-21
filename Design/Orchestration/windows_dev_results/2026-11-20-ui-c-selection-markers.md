# Board 6 · Selection and target markers

Implementation commits `61e4aa19b` and `3569242f8` on `missions/ui-c-selection-markers`. [Native comparison and art paths](../../AgentReports/MatchUiPolish/Implemented/ui-c-selection-markers/compare.md). The owner approved the visual direction on November 20, 2026.

Evidence root is `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/ui-c-selection-markers/`.

## Compile and EditMode

| Check | Result and evidence |
| --- | --- |
| C# compile | 0 `error CS`. Tundra build succeeded at `editmode-final.log:728` and rebuilt the final capture probe at `native-2400.log:708`. |
| EditMode marker fixture | Passed 19, failed 0 at `editmode-final.log:1538`, including an open-centre pixel check. The intermediate run passed 18 at `editmode-retry.log:1581`. First run failed one old sizing expectation at `editmode.log:1863`, with 17 passed at `editmode.log:2000`. The updated test verifies the rotated building's rendered bounds. |

The existing ARIA Burst diagnostic `BC1016` logged at `editmode-final.log:1257`. The C# error count is zero.

## Native captures

| Capture | Result and evidence |
| --- | --- |
| EN 1920×1080, first | The touch actuator declined start at `native-1920.log:2478`. Harness failed at `:2502`. |
| EN 1920×1080, retry | Infantry selected at `native-1920-retry.log:3049`. The Move marker was missing after a touch and the probe timed out at `:3936`. Harness failed at `:3960`. |
| EN 1920×1080, corrected glyph assets | Infantry and tank selected at `native-1920-final.log:2150` and `:2345`. Launcher selection timed out at `:3074`. Harness failed at `:3098`. |
| EN 2400×1080 | Infantry and tank selected at `native-2400.log:3053` and `:3245`. Move accepted at `:4792`, with a frame at `:4907`. Launcher selection stalled again at `:4000`, with result Failed at `:5062`. The first launcher failure is `native-1920-final.log:3074`. |
| EN 1920×1080, Move follow-up | Move accepted at `native-1920-move.log:3831`, frame at `:3945`. Launcher selection stalled at `:2963` and the overall capture result failed at `:4019`. |

The 1920×1080 infantry mark looked filled in both frames. The first visual evidence is `native-1920-retry.log:3049`, and the repeated evidence is `native-1920-final.log:2150`. Both frames and their run logs are retained. The green Move endpoint is partially visible in the 1920×1080 frame and obscured in the 2400×1080 frame. The capture gate remains open for the launcher, both zooms and Attack.

## Counted-style ARIA regression

The first queue was interrupted during its unjudged warm-up, after the menu journey passed at `aria-warmup-interrupted/unity.log:1208`. PID 2654 was the verified lab Editor and was 3 minutes 50 seconds old. Its exact PID received TERM after the capture revealed art issues. The runner parsed zero jobs at `aria-warmup-interrupted/wrapper.log:7`. The Completed marker was missing at `:8`. The stop record is `aria-warmup-interrupted/stop.txt:1`.

The final queue recorded all six jobs and four wins, with an unchanged source fingerprint at `aria/unity.log:180392`. The launcher reported zero mission table updates at `aria-wrapper.log:13`. There were zero `AriaBlocked` lines.

| Job | Outcome and deadline fraction | Menu journey | Return to MAIN MENU | Gate 3 |
| --- | --- | --- | --- | --- |
| Warm-up S031 / 130366 / en | Abort during startup at `aria/unity.log:1439`, summary `:180302` | Passed `:1270` | — | Unjudged |
| S002 / 130365 / en | Victory `aria/unity.log:151804`, 21.92% `aria-wrapper.log:8` | Passed `aria/unity.log:2299` | Passed `:152062` | **FAIL** `aria/gate3.log:1` |
| S020 / 130365 / en | Victory, 52.35% `aria/unity.log:159146` | Passed `:152817` | Passed `:159400` | PASS `aria/gate3.log:2` |
| S031 / 130365 / en | Victory, 46.14% `aria/unity.log:171249` | Passed `:160119` | Passed `:171575` | PASS `aria/gate3.log:3` |
| S037 / 130365 / en | Victory, 54.19% `aria/unity.log:178966` | Passed `:172372` | Passed `:179220` | PASS `aria/gate3.log:4` |
| S043 / 130365 / fa-IR | **Abort** `aria/unity.log:180377`, startup timeout `:180092` | Passed `:179924` | No marker | No row |

S002's first Gate 3 failure evidence is `aria/unity.log:151790`. Base Assault logged four unused roles, `role.apc_fast`, `role.breacher`, `role.marksman` and `role.radar`. S043's SubScene import took 460.23 seconds at `aria/unity.log:180058`. The first startup failure is `:180092`.

The queue's generated evidence is archived under `aria/generated/` in the log folder. `SkirmishS002AriaRunHarness.cs:997` rewrote the tracked S002 ARIA-controls screenshot. The generated screenshot was archived and the tracked copy was restored.

Real-player and device acceptance are pending.

## Board 6 follow-up

The follow-up is on `missions/ui-c-fix-selection-markers`, based on `91f0dbfc1`. Implementation commit `dc523ad` makes the capture fixture use the visible Continue and launcher guidance targets, records screen and UI hit information, and sends zoom through HUD touches. The capture gate still requires all eight frames at the requested dimensions. The selection shader uses quad UVs and limits ink and keyline coverage around the ring and four outward ticks. This revision has no native capture evidence yet.

Follow-up evidence root is `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/ui-c-fix-selection-markers/`.

| Check | Result and evidence |
| --- | --- |
| C# compile and EN 1920×1080 launch | Failed before the harness started. The first compiler error is `native-1920.log:2120`. An escaped string in the new tap diagnostic caused the failure. The quoting is corrected in `dc523ad` and awaits a fresh compile. The process sample shows the startup script refresh entering a native modal at `startup-dialog.sample:54` and `:58`. The verified lab Editor exited after TERM and the wrapper returned 143 at `stop.txt:1`. |
| Native capture gate and EditMode | Pending after the startup-dialog stop. The existing comparison frames and the original gate failures above remain the latest completed native evidence. The revised infantry ring and launcher tap are unverified. |
| Counted-style ARIA regression | Pending. The follow-up did not launch the queue, including the S002 and S043 retries. The prior S002 Gate 3 failure at `aria/unity.log:151790` and S043 startup timeout at `aria/unity.log:180092` are under the original evidence root. |

The prior 2400×1080 log shows that the launcher lesson had a world target at `native-2400.log:3337` and the fixture timed out at `:4000`. There was no world gesture between the lesson’s Continue touch and the timeout. The follow-up needs a new native run to establish whether an ordinary tap can select the launcher at each aspect ratio.

## Board 6 follow-up 2

Evidence root is `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/ui-c-fix2-selection-markers/`. The work ran on `missions/ui-c-fix2-selection-markers` from `3f7621d61`.

| Check | Result and evidence |
| --- | --- |
| Initial C# compile | Tundra built successfully with zero `error CS` at `editmode.log:724`, before the first capture. |
| Initial marker EditMode | Passed 19 of 19 at `editmode.log:1471`. The focused render test saw an open centre. |
| First EN 1920×1080 capture | The launcher received an unobstructed touch at `native-1920.log:2329` and `:2346`, and the selected launcher frame was recorded at `:2394`. The infantry frame at `:2157` still showed solid cyan patches. Zoom-in stalled at `:3091`, and the gate failed with four frames at `:8937`. |
| Infantry mesh authoring | Tundra built the revised source with zero `error CS` at `apply-infantry-ring.log:726`. The prefab authoring method passed at `:1402`. |
| EN 1920×1080 retry | Seven frames. Infantry used the revised mesh at `native-1920-retry.log:2152` and showed an open ring in the frame at `:2178`. The vehicle frame is at `:2288`. The launcher tap began and ended at `:2425` and `:2441`, and selection was recorded at `:2489`. |
| Zoom frames | Near and far views were captured at `native-1920-retry.log:2522` and `:2753`, at camera heights 15.05 and 74.35. The fixture used camera requests at `:2506`, `:2536` and `:2629` because the HUD offered no reachable zoom button. |
| Attack preview and accepted | Frames at `native-1920-retry.log:2999` and `:3060`, with the target touch at `:3027` and `:3043`. Launcher command acceptance is logged at `:3139`. The preview frame shows the cyan guide prominently. The accepted frame still shows a large missile impact-area circle from `SplitFrontLauncherHudView` around the compact target glyph. |
| Move and retry gate | Move was accepted on the first attempt, with a frame at `native-1920.log:8864`. Refinery geometry obscures the endpoint. After the retry's Attack, the pointer trace showed a mismatched press and release at `native-1920-retry.log:3201` and `:3272`. The retry stalled while reselecting the tank at `:5242` and again while publishing the result at `:7327`. The verified lab Editor was stopped after the stall, and the wrapper exited 143. See `native-1920-retry-stop.txt:1` and `:2`. |
| Final compile and EditMode | Stopped before compilation. The startup process sample shows `HandleDanglingSceneBackups` at `editmode-final-startup.sample:50` and a native modal at `:54`. The scene backup remained available, and the wrapper exited 143 at `editmode-final-stop.txt:2`. |

The last fixture revision captures Move while the tank is already selected, spaces out touches, and evaluates the unchanged eight-frame gate before checking input restrictions. The startup dialog prevented compilation and execution of that revision.

The EN 2400×1080 rerun, six-job ARIA queue, Gate 3 and conditional S002 and S043 fa-IR retries remain pending. The previous S002 unused-role failure and S043 timeout above are still the latest ARIA evidence for this board. Real-player and device acceptance remain pending.

## Board 6 follow-up 3

Evidence root is `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/ui-c-fix3-selection-markers/`. The lab was on `missions/ui-c-fix3-selection-markers`, based on `b00747309`.

The fixture orders Move while the tank is selected, before the launcher’s lesson and missile cinematic. The Move touch now targets open ground east of the refinery. The fixture verifies saved frames after transient markers expire and publishes the capture result from the editor update. The gate still requires all eight states at the requested dimensions. Native validation of the sequencing and endpoint is pending.

The large red ring on the earlier Attack-accepted frame is the `Missile impact area` line renderer in `SplitFrontLauncherHudView.cs:20–31`. `UiShellEcsGateway.SplitFront.cs:34–38` feeds it `GroundMissileLauncherComponent.DamageRadius`, which the impact system uses for area damage. The Board 6 Attack glyph is the target bracket and centre pip. This follow-up makes only fixture changes.

| Check | Result and evidence |
| --- | --- |
| EN 1920×1080 | Stopped at Unity startup before compilation or harness entry. No capture gate marker. The log ends at `native-1920.log:42`. The process sample shows `HandleDanglingSceneBackups` at `native-1920-startup.sample:50` and the native modal at `:54`. The first occurrence was `../ui-c-fix2-selection-markers/editmode-final-startup.sample:50`. The lab’s `Temp/__Backupscenes` was empty before launch and after exit. The verified lab Editor received TERM at age 03:12, and the wrapper exited 143 (`native-1920-stop.txt:1` and `:2`). |
| EN 2400×1080 | Pending after the 1920×1080 startup stop. |
| ARIA and Gate 3 | Pending for all six jobs and conditional retries. |

The owner-approved Board 6 direction and earlier comparison frames remain available. Real-player and device acceptance are pending.
