# WarlineCapture Agent Coordination Workflow

Date: 2026-05-07

## Purpose

This document defines how the WarlineCapture PM assistant keeps the gameplay, UI, and support/documentation agents aligned.

Use it when an agent finishes work, gets blocked, changes a contract another lane depends on, or needs the next task. It does not replace the design docs. It is the operating layer that keeps those docs, the repo, and the active agents in sync.

## Source Of Truth

Primary project state:

- `Design/Project_State_Source.json`
- `Design/Project_State_Dashboard.md`
- `Tools/ProjectState/generate_project_state_dashboard.py`

Active agent task board:

- `Design/AgentTasks/README.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/build_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/pm_design-audit.md`

Primary cross-lane implementation contracts:

- `Design/M01_FirstContact_Production_Contract.md`
- `Design/FTUE_And_Command_Assistant_Design.md`
- `Design/UIUX_Gameplay_Element_Alignment.md`
- `Design/3D_SingleMap_Gameplay_Direction.md`
- `Design/Designer_Role_And_Documentation_Workflow.md`

Primary asset tracking:

- `Design/Art_Asset_Requirements_Register.md`
- `Design/Art_Asset_Requirements_Register.csv`

Rule: update the source document first, then regenerate generated outputs. Do not manually edit `Project_State_Dashboard.md`.

## Agent Lanes

| Lane | Owns | Must Coordinate With |
|---|---|---|
| Gameplay agent | ECS/gameplay systems, mission runtime, tactical metadata, command validation, objectives, rewards, persistence, camera bounds, tactical world/map rendering, terrain visibility, unit/target world scale, and gameplay camera framing. | UI for visible controls, reason codes, screen flows, capture requirements. Support/docs for FTUE steps, mission contracts, state tracking. |
| UI agent | Unity Canvas screens, prefabs, route wiring, tactical HUD, command controls, visual-lock implementation, UI tests, full-screen HUD/canvas composition, safe-area layout, and player-facing capture composition. | Gameplay for data sources, command APIs, tactical world/map visibility, unit scale, and gameplay camera framing. Support/docs for target updates, layer-pack state, asset register rows. |
| Art/Atlas agent | Sprite atlas source art, unit/building/VFX visual-state coverage, art-readiness packages, approval packages, visual scale/readability references, and atlas-source gaps. | Gameplay for runtime atlas integration, QA/HCI for public readability validation, PM/user for art approval decisions, Designer for design-language consistency. |
| Build agent | Jenkins-triggered builds, build queue/watch automation, build-log inspection, failure-summary reports, and CI handoff reporting for PM. It may trigger approved builds and investigate resulting logs, but it does not own gameplay/UI/art fixes unless PM explicitly reassigns them. | PM for priority and escalation, Gameplay/UI/QA when failures map to lane-owned code or validation gaps, Support/docs when CI workflow docs or task wiring must change. |
| Support/docs/FTUE agent | FTUE design, ARIA tutorial flow, contracts, asset checklist, project-state tracking, handoff docs, priority lists. | Gameplay for typed tutorial actions and mission ids. UI for surfaces, assistant button/panel/card visuals, validation captures. |
| Designer | Product/design coherence, README and design-index optimization, terminology alignment, source-of-truth hierarchy, player-facing flow clarity, UI/readability design review, and documentation pruning recommendations. | PM for priorities and accepted source-of-truth changes. Gameplay/UI/support/QA/art for cross-lane impacts, implementation feasibility, captures, and validation evidence. |
| PM assistant | Intake, sync review, validation gate, priority ordering, cross-lane impact calls, progress tracking. | All lanes. |

## Completion Report Required From Every Agent

When an agent says work is done, require this report before accepting it:

```text
Lane:
Task:
Files changed:
Contracts touched:
User-visible behavior:
Validation run:
Validation result:
Known gaps:
Cross-lane impacts:
Next recommended task:
```

`Contracts touched` means any changed API, prefab path, route id, mission id, data schema, asset path, reason code, UI element id, FTUE target id, or validation command.

Codex completion reports must use this exact same template in final responses after implementation work, reviews, fixes, validation passes, or handoff/spec updates. If a field does not apply, write `None` or `Not run` instead of omitting it.

Codex must also save the same completion report to:

```text
Design/AgentReports/<YYYY-MM-DD>_<lane>_<short-task>.md
```

Use lowercase kebab-case or snake_case for `<lane>` and `<short-task>`, keep the filename short, and mention the report file path in the final response.

## Immediate Post-Validation Report Rule

Validation close-out is part of the task, not a later PM step.

After any lane finishes a required validation command, capture pass, Unity test run, build, screenshot/contact-sheet generation, log scan, or focused manual/device check, the next action must be to create or update the required report under `Design/AgentReports/` before doing any of the following:

- Starting a new implementation task.
- Reporting `done`, `waiting`, `blocked`, or `idle`.
- Asking PM/QA to review the work.
- Handing work to another lane.
- Running optional polish or broader validation not required by the active task.

If validation passes, the report must include the exact commands or checks, output paths, pass counts or capture list, and any remaining gaps.

If validation fails, is interrupted, or cannot run because Codex/tool sandbox approval is required, the report must be written immediately with `Validation result: blocked` or `Validation result: failed`, the exact command/log path, the owner of the next action, and whether the lane can continue fallback work.

If a heartbeat fires and the agent has changed files or generated captures but has not yet written the report, the heartbeat task is to finish the report first. Unreported validated work is treated as in-progress and must not be counted as accepted.

## Continue Workflow

When the user tells an agent `continue`, that agent should read its lane file under `Design/AgentTasks/` and continue that task unless the user gives a newer direct instruction.

The PM assistant owns task dispatch updates in `Design/AgentTasks/` after reviewing reports and cross-lane dependencies.

The Designer may recommend README/design-index restructuring and source-of-truth cleanup, but PM owns accepting cross-lane documentation changes before they become coordination rules.

## Heartbeat Ownership

Lane heartbeats are persistent lane monitors. They are not the current task, and they must not be deleted, paused, disabled, or stopped by the lane agent just because `Design/AgentTasks/*_current.md` is complete, stale, blocked, or waiting for refresh.

Only the user or PM assistant may explicitly stop, pause, delete, or retire a lane heartbeat. When a current task is complete or stale, the correct behavior is to keep the heartbeat active, report the waiting state, and stay quiet unless new lane work or a blocker appears.

Required waiting wording:

```text
My current lane task is complete/stale and I am waiting for PM to refresh the lane file. I will keep the heartbeat active and stay quiet unless new work or a blocker appears.
```

Incorrect behavior:

- Deleting `Auto Continue Gameplay`, `Auto Continue UI`, `Auto Continue QA/HCI`, or `Auto Continue Support/FTUE` because a P1 task completed.
- Deleting the Build lane heartbeat because the latest Jenkins run passed or because no fresh failure is visible yet.
- Treating a completed handoff as a reason to retire the lane monitor.
- Stopping a heartbeat before PM has refreshed the lane task list.

## Waiting And Blocker Ownership Rule

Agents must not report `waiting`, `blocked`, or `idle` until they identify who owns the next concrete deliverable.

Required ownership check before reporting a wait/block:

```text
Waiting on lane:
Waiting on exact file/report/asset/command:
Owner of next action:
Can my lane still continue fallback work? yes/no
If my lane owns the next action, I must continue or write the exact technical blocker in my required report file.
```

If the missing deliverable belongs to the agent's own lane, the agent must not wait on another lane. It must continue the active task or produce the required handoff with the exact technical blocker.

Examples:

- QA/HCI may wait for `Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md` because UI owns that capture matrix.
- UI may not wait for QA/HCI to validate a capture matrix that UI has not delivered yet. UI owns the matrix and must either produce it or report the exact UI route/capture tooling blocker in the required UI report file.
- Gameplay may wait for UI only when a named UI surface/control/report is missing and Gameplay cannot produce it. Gameplay may not wait if the missing item is a gameplay API, command result, reason code, runtime id, or validation report owned by Gameplay.
- Support/FTUE may wait for Gameplay/UI only when a named typed command, runtime id, UI surface, or validation capture is missing and cannot be authored in Support/FTUE docs alone.
- Build may wait for another lane only after it names the failing build id/log path, the exact owning file/system or missing deliverable, and the next report expected from that lane. Build may not stay idle just because Jenkins is red; it must convert the failure into a named PM-facing report.

## Sync Rules

1. Any changed contract must be announced to the affected lanes before the next task starts.
2. A lane cannot mark work complete if another lane needs a missing API, prefab, button, asset, id, or data source.
3. If gameplay adds a command, reason code, objective, tutorial target, map anchor, or result field, UI and support/docs must be notified.
4. If UI adds, removes, renames, or disables a button/surface/prefab/controller, gameplay and FTUE must be notified.
5. If FTUE changes a tutorial step, ARIA action, target id, or mission teaching order, gameplay and UI must confirm the required runtime and surface hooks exist.
6. If an asset row changes from `missing` to `exists_needs_review`, it is not complete until approval and runtime wiring are recorded.
7. If a validation capture, Unity test, or build fails, mark the task blocked or in progress. Do not call it done.
8. A lane that reports `waiting`, `blocked`, or `idle` must name the lane and exact deliverable it is waiting on. If that deliverable is owned by the reporting lane, the PM assistant should mark the report `needs fixes` and tell the lane to continue.

## Scene Reference Rule

Agents must not introduce runtime scene searches such as `FindObjectOfType`, `FindObjectsOfType`, `Object.FindObjectOfType`, `Object.FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, or name/tag-based object lookups in production gameplay/UI/FTUE paths.

Required pattern:

- Use serialized fields, prefab wiring, constructor/service injection, explicit runtime registries, mission/session references, or typed provider APIs.
- Builder/editor scripts may use scene searches only when constructing or validating generated scenes/prefabs, and the generated runtime component must store explicit references afterward.
- If a warning appears for a new or touched path, the lane must fix it before reporting done unless the PM explicitly accepts a temporary blocker.
- Completion reports for affected runtime work must state whether scene-search warnings were checked and resolved.

## Validation Gate

Before the PM assistant accepts finished work:

- Check repo diff and changed files.
- Confirm the work matches the active contract docs.
- Confirm tests, Unity validation, or rendered captures were run for the affected surface/system.
- For playable-slice gates, confirm at least one user-facing launch path reaches the intended production slice, not only an editor harness, prefab capture, test fixture, or internal router state. Public launch paths include main-menu, campaign/saga map, mission briefing, loadout, quick-start/test-launch, or any path the user is asked to manually test.
- Reject new `Object.Find*`, `Resources.FindObjectsOfTypeAll`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, name/tag lookup warnings, obsolete scene-search overload warnings, and `FindObjectsSortMode` usage in production runtime paths, editor validation builders, and Unity test files unless the report documents an accepted blocker and a concrete follow-up.
- If validation needs scene evidence, prefer explicit serialized references, known bootstrap/context references, scene root references from the loaded scene, typed runtime registries, ECS component objects, or task-owned fixture objects. Do not prove readiness by adding broad scene-wide lookup APIs that create warnings or hide missing ownership boundaries.
- Ask for missing validation points if the agent did not provide them.
- Identify cross-lane follow-ups and assign them to the correct lane.
- Update `Project_State_Source.json` only when project state changed.
- Regenerate `Project_State_Dashboard.md` after state-source updates.
- Update `Art_Asset_Requirements_Register.csv` only when asset status, approval, or completion changed.

## Validation Permission

Agents are pre-authorized to run focused Unity EditMode/PlayMode validation, prefab builders, capture builders, graphics-enabled Unity capture passes, and report-generation commands required by their active lane task.

Agents should not ask the user whether to run required tests or validation. Required validation is part of the task definition.

Use the dedicated Unity workspace assigned to the lane before asking the user to resolve an open/locked clone:

| Lane | Primary Unity workspace | Fallback rule |
|---|---|---|
| Gameplay | `/Users/farhad/Projects/WarlineCapture-CodexUnity1` | If locked by a stale process, stop only that stale process when safe or report blocked. Do not take UI/QA workspaces unless PM explicitly reassigns. |
| UI | `/Users/farhad/Projects/WarlineCapture-CodexUnity2` | If locked by a stale process, stop only that stale process when safe or report blocked. Do not take Gameplay/QA workspaces unless PM explicitly reassigns. |
| QA/HCI | `/Users/farhad/Projects/WarlineCapture-CodexUnity3` | If locked by a stale process, stop only that stale process when safe or report blocked. Do not take Gameplay/UI workspaces unless PM explicitly reassigns. |
| Build | No default Unity workspace. | Trigger Jenkins builds, inspect Jenkins artifacts/logs, and write failure summaries/reports. If local reproduction is required, PM assigns a temporary workspace or routes the failure to the owning lane. |
| Support/FTUE | No default Unity workspace. | Run docs/tests that do not need Unity. If Unity validation becomes necessary, PM assigns a temporary workspace before the command runs. |

Agents are authorized to use their assigned workspace for focused validation without asking for product permission. Do not run validation in another active lane's primary workspace just because it is available; workspace switching is a PM coordination decision.

Agents may still need to pause when Codex/tool sandbox approval is required. Examples include writing outside the allowed workspace, network access, or destructive operations. In that case, the agent should make clear that the request is for sandbox/tool permission, not for product approval.

Build-specific loop:

- The Build lane may run on intervals/heartbeats to trigger the current approved Jenkins build job, inspect the resulting Jenkins console/artifacts/logs, and report status under `Design/AgentReports/`.
- On success, Build writes a concise pass report with job/build id, target, artifact path if available, and any warnings worth PM awareness.
- On failure, Build writes a concise failure report with job/build id, failing stage, exact failing command/test when visible, log/artifact paths, and the most likely owning lane. Report facts and probable cause; do not silently start fixing product code unless PM explicitly assigns that follow-up.
- Build should notify PM/user only when a fresh failure appears, the failure reason changes materially, or a build blocker is preventing a required gate.

Unity licensing workaround is mandatory. QA/HCI confirmed on 2026-05-08 that Unity licensing is healthy for the project, but Codex sandboxed batchmode cannot always reach the user's Unity Licensing Client/keychain/session services. Required Unity batchmode validation should therefore run with Codex escalation/out-of-sandbox execution when licensing access is needed. This is the standard fix for `LicenseClient-farhad`, unsupported protocol, reconnect, or timeout loops and is not a reason to retry other lane workspaces, ask a product approval question, or come back to PM/user before the workaround is attempted.

Graphics-enabled capture passes may trigger Codex/tool approval because Unity needs GPU/window access. If the capture is required by the active lane task, uses a dedicated WarlineCapture Unity workspace, and writes to `/private/tmp` or expected project capture output paths, it is product-approved. The agent should request only tool approval, not ask whether the capture should happen. For runtime/visual capture proof, do not use `-nographics` if the capture path needs GPU/window access or if prior attempts show blank/headless output or missing headless package errors.

If Unity validation enters a repeated licensing reconnect, unsupported protocol, or license-client loop before tests start from a sandboxed command, do not report the first sandboxed loop as the final blocker. Rerun the same required Unity batchmode command once with Codex escalation/out-of-sandbox execution in the assigned lane workspace. If the escalated rerun reaches licensing and tests, continue normally and report that escalated batchmode was used. If the escalated rerun still stalls before tests start, stop the stuck Unity process, write or update the required `Design/AgentReports/` report with `Validation result: blocked`, include the exact command, workspace, log path, licensing-loop symptom, and confirmation that the escalated assigned-workspace workaround was attempted. Stopping a stuck Unity validation process is product-approved cleanup; if Codex asks, request only tool permission to stop that process.

Correct wording:

```text
Codex needs tool approval to run the focused Unity batchmode validation outside the sandbox so Unity can access the user's Licensing Client/session services. This validation is required by the active lane task; please approve the tool prompt so validation can continue.
```

Correct stuck-process wording:

```text
Codex needs tool permission to stop the stuck Unity validation process after repeated licensing reconnect loops. This is cleanup for a blocked required validation, not a product decision.
```

Avoid wording that sounds like the agent is asking whether validation should happen.

If a Unity lane workspace is missing, recreate it as a plain sibling Unity project copy from `/Users/farhad/Projects/WarlineCapture`, excluding generated folders such as `Library`, `Temp`, `Obj`, `Logs`, `Build`, `Builds`, and `UserSettings` so Unity generates an independent Library for that lane.

## Visual Target Lock Quality Gate

All final target mockups, visual locks, popup targets, HUD targets, prefab targets, and regenerated UI target images must be AAA-quality WarlineCapture mockups aligned with the existing approved visual language.

Art source note: current WarlineCapture art assets, mockups, target locks, tactical map concepts, unit sprite concepts, and generated UI production images are AI image-generation outputs unless a specific asset row or handoff explicitly documents another source. Agents must preserve that source assumption in reports and must not present AI-generated assets as hand-authored, vendor-final, or automatically approved.

Required standard:

- Match the active WarlineCapture military RTS UI language: command-base black/olive metal, gold action accents, restrained blue information accents, Oxanium-style typography, dense but readable tactical composition, and high-quality 3D operation-map or command-room context where the target calls for a popup or target-lock presentation.
- Preserve functional clarity: dynamic text, ids, state labels, icon slots, control names, and runtime-owned data surfaces must be readable and implementable.
- Do not accept state boards, wireframes, flat scripted placeholders, generic sci-fi UI sheets, unstyled layout diagrams, or low-detail mockups as final target locks.
- Do not accept a target only because it looks high quality. If it looks like a beautiful standalone sci-fi mockup but does not belong to the existing WarlineCapture target family, mark it `needs fixes`.
- For regenerated target locks, require a side-by-side/contact-sheet comparison against nearby accepted targets when style drift is a risk. The comparison must show that the new target can sit inside the same product family without looking foreign.
- Do not treat a flat visual target as a sliced implementation asset or mark art rows complete until the asset register records review/approval plus runtime wiring when required.
- When a target is meant to show gameplay, maps, units, commander identity, ARIA assistant, or tactical HUD context, it must align with `3D_SingleMap_Gameplay_Direction.md`, `UIUX_MainMenu_Visual_Contract.md`, and accepted command-base visual locks instead of inventing a new style.

If a generated target does not meet this bar, mark the lane `needs fixes` and keep the asset register status unapproved.

## Public Launch Path Smoke Rule

For any gate that claims a mission, playable slice, route, onboarding flow, or QA/HCI pass is ready for user/manual validation, at least one public launch path must be tested and reported.

Required report content:

- Entry path used, for example `Main Menu -> Campaign -> Mission Briefing -> Deploy` or `Main Menu -> Skirmish -> Launch`.
- Expected slice, mission id, visual direction, and route, for example `campaign.ch01.m01.first_contact`, current 3D operation-map M01 direction, mounted HUD/assistant route.
- Actual first visible gameplay state after launch.
- Whether legacy/sandbox UI, legacy 3D world, old prototype scene, or wrong mission content appeared.
- Screenshot or capture path when practical.

If the public path lands in a legacy prototype, sandbox route, old 3D scene, wrong mission, or editor-only harness mismatch, the gate is blocked. Do not ask the user for manual HCI/balance feedback until the public path reaches the intended slice or is explicitly labeled as legacy/sandbox and a separate production test path exists.

Editor-only route captures, prefab screenshots, PlayMode tests, and controller tests are useful evidence, but they cannot replace this public launch smoke for manual QA readiness.

## Visible Gameplay HCI Gate

QA/HCI must validate what a player can actually see and operate before asking the user for manual feedback. Internal route state, ECS component presence, inactive legacy UI objects, test XML, or prefab/editor screenshots are not enough by themselves.

For any user-facing playable path, QA/HCI must include visible gameplay evidence:

- The exact human path clicked or simulated, using menu/screen names.
- A screenshot, graphics-enabled capture, video, or explicit manual visual observation after the launch/input action.
- Confirmation that the visible scene/camera/rendered content matches the intended design direction.
- Confirmation that old prototype content, wrong scenes, wrong cameras, legacy 3D visuals, sandbox labels, or placeholder/mock-only surfaces are not visible unless explicitly intended and labeled.
- A basic HCI pass: the player can identify the objective, locate selectable units, understand the next action, perform the primary input, see command feedback, recover from an invalid command, and reach or understand the result state.
- Performance observation for the visible path: no obvious freeze, multi-frame input stall, severe frame drop, or first-interaction hitch that invalidates usability feedback.

QA/HCI must reject a handoff as `needs fixes` or `blocked` when it only proves code/state and does not prove visible player behavior for the promised manual path. The owner lane must then provide a player-visible capture/manual observation path, not ask the user to discover the issue.

A capture also fails this gate when it shows HUD chrome over a flat/blank/brown world, a tiny unreadable play area, missing authored terrain, mismatched camera scale, or a camera-only render that does not represent the actual full-screen player composition. For tactical gameplay gates, visible evidence must preserve the accepted gameplay camera/readability direction and the accepted HUD composition unless the report explicitly documents a PM-approved design change.

For public tactical launch fixes, ownership is split:

- Gameplay owns the world under the HUD: authored terrain/map visibility, tactical-map loader output, old-world suppression, unit/target world scale, gameplay camera bounds, and camera zoom/framing.
- UI owns the canvas over the world: HUD/objective/threat/assistant/command surfaces, safe-area layout, route/button flow, and full-screen player-facing capture composition.
- A lane may not accept its own work as complete by fixing the other lane's surface or by proving only its own half. Public launch readiness requires both a readable gameplay world and a readable HUD/canvas in the same full-screen evidence set.

WarlineCapture tactical gameplay is ECS-first. Only Canvas UI is allowed to be non-ECS GameObjects. Gameplay readiness must be proven through ECS entities/components, authored tactical metadata, and mission runtime systems. `SpriteRenderer` or GameObject presentation objects may be used only as ECS-driven visual objects for ECS entities, not as independent gameplay objects, tactical world objects, marker state, objective state, terrain/map state, or screenshot-only stand-ins. For M01, every non-Canvas visible world object must trace back to ECS source-of-truth data such as `MissionRuntimeEntityId`, `MissionRuntimeSpritePresenter`, `LocalTransform`, `UnitGrid`, `UnitHealth`, selection/command components, and tactical metadata. If visible gameplay is not backed by ECS entity state, the handoff fails even if the capture looks acceptable.

## Current Priority Order

1. M01 First Contact production slice.
2. Tactical metadata and camera bounds for `iso.ch01.district_edge_01`.
3. SCN-08 tactical HUD missing controls: selected entity panel, command mode banner, world command markers, invalid command feedback, minimap camera bridge.
4. Command validation reason codes shared between gameplay and UI.
5. FTUE M01 ARIA steps using typed ids, not screen coordinates.
6. M01 validation scene and capture set: art-only, metadata overlay, playable select/move/attack/objective/result flow.
7. Asset register updates for M01 tactical ground, metadata, HUD controls, ARIA/tutorial visuals, and required unit/building sprites.

## Speed Rules

- Finish and validate M01 before expanding to M02-M05. Do not start later mission implementation until the PM assistant marks `Design/AgentTasks/M01_CRITICAL_PATH.md` ready to expand.
- Prefer narrow tasks that close one named gate over broad polish tasks.
- Treat visual target drift, missing validation, missing reports, and cross-lane contract ambiguity as schedule risks, not minor cleanup.
- Keep legacy systems isolated unless they directly advance current M01: random city/road generation, day/night, legacy 3D `Model` children, and separate `Destroyed` child objects must not re-enter production gameplay by accident.
- Update progress estimates only from accepted reports, validated captures/tests, or explicit user approval.
- Bounded agent context packs: anchor on the active lane task file under `Design/AgentTasks/` plus only the smallest file set needed for the change (target **≤ five** implementation paths unless the PM task explicitly widens scope). Prefer deep links (`path` + heading/section ids) into one contract/design doc instead of pasting large excerpts.
- Local validation first before agent loops: run the lane’s compile/test/build checks locally when possible and paste **only failing excerpts/logs** plus the command used.
- Canonical handoffs live in repo files: use `Design/AgentReports/` as durable context for the next session; avoid long repeated narrative summaries in chat when the report already captures `done` state.
- Separate design edits from implementation: if the design source-of-truth needs to change, do that as an explicit task slice first, then code, to avoid expensive mixed-scope iterations.

## Workflow Change Discipline (Avoid Rule Stacking)

- Prefer **editing this coordination document** over adding new agent instruction files. If a new file is required, the PM assistant must name what problem it solves and which section here would be overloaded without it.
- **Replace, do not stack**: when a new rule overlaps an old one, merge into the existing bullet or delete the obsolete bullet in the **same** change-set. Deprecated rules must be removed or marked `DEPRECATED:` with removal date—not left parallel “just in case.”
- Keep net-new procedural bullets **small**: if an addition would exceed roughly **seven** bullets per new topic, split the topic only when two different owners/lanes genuinely need distinct rules; otherwise compress.
- Periodic hygiene: whenever the PM assistant closes a major milestone slice (example: moving off M01 critical path), skim this document for rules that never triggered during the slice and propose deletes or merges.
- Token/credit instrumentation is optional and lightweight: per-provider dashboards already give ground-truth spend. Do **not** require per-message token counting unless tooling exposes it reliably. Prefer **cheap proxy metrics** you can jot in weekly notes: lane, task id, approximate round trips to acceptance, oversize-context incidents (whole-tree scans), rework count ( reopened reports). Review proxies against weekly spend totals and adjust habits, not individual chat micro-optimizations.

## Commit And Push Policy

The PM assistant should commit and push coherent accepted WarlineCapture work when it is safe to do so.

Lane agents must not run `git add`, `git commit`, or `git push` unless the user or PM assistant explicitly asks that lane to do so for a named file set. By default, lane agents write reports and leave changed files in the worktree for PM review.

Safe means:

- The task or PM update is complete enough to preserve.
- Validation has passed, or any missing/failing validation is explicitly documented in the report.
- The staged set is related and does not mix unrelated user changes into the PM batch.
- Active task files and critical-path routing are not left stale.
- The worktree is not in the middle of a partially edited file set.
- The PM assistant has checked `git status --short` and staged only the accepted, coherent file set for the commit.

Default behavior:

- Commit after accepted handoff reviews, PM task-board reconciliation, tracking/dashboard updates, or validated implementation batches.
- Push the commit to the active remote branch after the commit succeeds.
- Do not commit/push if the repo contains unrelated or ambiguous changes that need user confirmation first.
- Keep commits scoped by lane or PM coordination topic. Do not sweep all dirty files into a single mixed commit.

## PM Assistant Review Response

When reviewing an agent handoff, the PM assistant should respond with:

```text
Status: accepted / needs fixes / blocked
Reason:
Validation accepted:
Validation still needed:
Cross-lane notices:
Tracking updates:
Next task:
```

If the agent is accepted and has no next task, assign the next highest-priority task from this workflow and the current project-state source.

## PM Assistant Limitation

The PM assistant can only see work that is available in this repo, in terminal output, or pasted into the thread. Agents working in separate conversations must either commit/edit shared files, provide their changed file list and validation report, or paste their completion report here for review.
