# Skirmish production coordination

Updated: 2026-10-14, Europe/Berlin. Execution enabled by the owner in this chat.

## Objective and authority

Complete all 120 existing Skirmish catalog entries as quickly as their governing contracts permit. Read root AGENTS.md, CAMPAIGN_MAP_REUSE_POLICY.md, SCALABLE_MISSION_PRODUCTION_HANDOFF.md, SCALABLE_MISSION_PRODUCTION_STRATEGY.md and ACCEPTANCE.md before resuming. Preserve catalog identities, setup vectors, gameplay budgets, Campaign physical geometry, coded bindings and full failed evidence.

The owner explicitly authorizes the coordinator to delegate work, continue implementation and technical validation without waiting for visual review, send screenshots for asynchronous review, and commit/push stable completed steps to main. Visual acceptance, device acceptance and unfamiliar-player acceptance remain separate pending gates until evidenced. Do not treat absence of feedback as visual approval. The latest owner instruction to commit stable steps to main supersedes the handoff's proposed separate integration branch; workers still need disjoint ownership.

Coordinator chat: 01a13bfb-c3de-7542-b082-b3e90bae7f5a. Heartbeat: complete-120-skirmish-missions, every 30 minutes. Respect explicit owner stop/pause; never use the heartbeat to undo it. A heartbeat is a wake-up opportunity, not proof that processes or agents survived an interruption.

## Resume procedure

1. Read this record and the two production handoffs. Inspect Git status, latest commits, live agents, current worker claims and Unity process ownership before changing files.
2. Reconcile source/candidate hashes and available evidence with actual filesystem state. Mac paths, PIDs, loaded assemblies and temporary trial packages are historical unless independently verified on that host.
3. Preserve unrelated work. Reuse live workers; replace completed/missing workers only after reconciling their files. Only the coordinator stages, integrates, commits and pushes.
4. Keep one controlled native gameplay lane per machine. Use the repository's platform wrappers with explicit logs, timeouts and exact pass markers, and keep Hub open/signed in. Never kill an active Editor to make room.
5. Freeze candidate/runtime/source bindings during counted trials. Integrate shared changes at batch boundaries and select affected regressions from dependencies. After two identical failure signatures, repair the smallest reproducer before another full match.
6. At stable boundaries update this record with commits, tests, artifacts, ownership, blockers and the exact next action, then commit/push only finished reviewed changes to main. Never force-push.
7. Stay quiet on unchanged/non-actionable scheduled wakeups. Report meaningful milestones, completion, failures or required decisions; include available native screenshots for asynchronous review.

## Baseline and work ownership

Initial HEAD: f8e0ab2c3bfb4bd7350eab4d085d4cc505a1debe on main, clean at start. Windows project: D:\Projects\WarlineCapture. Editor version: 6000.5.2f1. Sparse checkout initially excluded mission reports/contracts; coordinator is expanding it to the relevant Design folders.

| Worker | Reserved scope | Current task |
|---|---|---|
| /root coordinator | This record, integration, native lane scheduling; shared changes only after reservations | Establish Windows validation prerequisites; review and integrate stable worker output |
| /root/s005_completion | S005/WINDOWS_CONTINUATION_20261014.md and compact S005 continuation audit JSON; no runtime edits | Reconcile archived evidence and identify executable Windows north-flank/paid-air continuation |
| /root/qualification_pipeline | NEW Tools/CI/skirmish_production* files and their offline tests | Versioned job schema, fail-closed preflight, persistent queue and exclusive/recoverable worker claims |
| /root/authoring_foundation | PRODUCTION_CAPABILITY_AUDIT_20261014.md; reserve exact code files with coordinator before edits | Audit BA/FC/BT/CE capability and pilot fit, then bounded generic authored-set preservation |

Workers do not stage, commit, push, start Unity, mutate processes or contact other chats independently. Request native lane access from the coordinator. This roster is historical after a restart: inspect actual agents before reusing names.

## Strategy and sequence

1. Reconcile S005 and stabilize portable preflight/runner/queue in parallel; missing Mac-only packages are not evidence of a game defect.
2. Prove one generic normal-input adapter across two authored missions, and a config-only builder with idempotence, identity/setup integrity and explicit unsupported reasons.
3. Prepare and qualify ground Base Assault pilot S026, S049, S050, S074, S097 and S098 after native source-fit checks. Preserve S001/S002/S025/S073 and S005's frozen candidates.
4. Establish measured throughput, then prepare six-entry packets while frozen batches qualify. Implement missing shared FC/BT/CE and army capabilities once with one owner each; reassign workers as prerequisites clear.
5. Track every entry and certified combination independently. Required ARIA/manual/Support/recovery/edge/visual/device/player coverage remains intact. Draft generation and shared fixtures never certify all 120 entries.

## Confirmed findings and pending gates

- S005 HANDOFF_20261014.md explicitly says playerReady=false. Its paid north-flank and paid-air work remains open, along with native visual/result/return and separate owner/device/player acceptance.
- S005 observer1630 and fixture1631 temporary packages are not tracked in HEAD. Recorded Mac PID/assembly state cannot be resumed as live Windows state. Existing native1628 evidence and prior source helpers are archived and being audited.
- Validation foundation commit f8ac9f2e672cdad6ac6b7995835180dfcaa9f90e is not available in the initial local object database. Inspect remote availability before declaring it lost; avoid depending on an absent implementation.
- The initial process inventory found no Unity/Hub processes. Checked resolver found `D:\Program Files\Unity\6000.5.2f1\Editor\Unity.exe`. Coordinator opened Hub; current Hub logs confirm token-based authentication and licensing pipe/entitlement connection. Pipeline 0.6.0-exp.1 is installed; CLI status/list/command initially had no connected Editor.
- No new mission is claimed qualified or player ready by this startup step. No native screenshots have been generated yet.

## Next stable delivery

Review worker reports and the offline queue slice; run meaningful negative controls and documentation/whitespace checks, then commit/push the coordination record and only complete validated deliverables. Resolve installed Editor/Hub through checked tools. Reserve shared file edits before assigning the normal-input adapter or generic builder implementation.

## Startup delivery evidence

- `rtk proxy python Tools/CI/skirmish_production_queue_test.py`: all 17 offline tests passed independently under the coordinator, including eight-worker claim contention, interruption ownership, retry cutoff and source/input/result/return negative controls. This is offline tooling evidence, not mission qualification. Native success transitions remain unavailable pending adapter canaries.
- Generic authored-definition preservation and exact-ID fallback are implemented in CatalogFactory/CatalogConfig with five focused tests. Checked GUI-licensing validation is running: `Game.Tests.Editor.SkirmishAuthoredDefinitionLookupTests.RunFocusedValidation`; required marker `[SkirmishAuthoredDefinitionLookupTests] result=Passed`; full log `Logs/SkirmishProduction/authored-lookup-20261014-01.log`; timeout 900 seconds. C# is frozen until this run finishes. Do not commit that code as validated before the marker and wrapper completion.
- Foundation branch `codex/skirmish-validation-foundation` was queried on origin and returned no matching ref. Existing native-contact auditor is reused by the new queue.
- Source audit found active Base Assault objective behavior and explicit BA-only state gates; FC/BT/CE enum names do not supply native implementations. Detailed evidence and six pilot records are in PRODUCTION_CAPABILITY_AUDIT_20261014.md.
- S005 worker prepared a dispatch-disabled Windows north1632 observer draft and preparation helper, retaining historical1628 failure evidence. It requires coordinator review and portable runner/qualifier before any live trial. No release or normal-input victory is inferred.

Next assignments while Unity compiles: authoring worker prepares minimal config-only builder/launch proposal; qualification worker prepares generic adapter contract and packet coverage controls; S005 worker reconstructs portable runner/qualifier offline. Keep current C# frozen.
