# Menu UI/UX handoff workspace start

Date: 2026-10-03 (Europe/Berlin)
Workspace: `/Volumes/T7/Projects/WarlineCapture-MenuUiUx`
Branch: `codex/menu-ui-ux`
Base commit: `af41cb62b016bcc176795cebd33cf739d4797a21`
Source: `/Users/farhad/Projects/WarlineCapture`

## Scope and visual gate

Read `AGENTS.md` and `Design/AgentReports/MenuUiUxAudit/HANDOFF.md`.
The five existing mockups were presented in the requesting chat for visual review.
Visual direction approved on 2026-10-03 with one aligned Credits/Commander/ARIA/Store/Armory column. Main Menu column alignment implemented and prefab rebuilt; capture-tool hardening also started. Remaining screen redesigns are pending.
Retain the handoff corrections: use real Commander values, choose one Campaign node style,
and remove the redundant Skirmish logo beside Back.

## Preparation completed

- Created an isolated Git worktree on the requested T7 drive.
- Copied the current working source, including existing uncommitted changes and audit evidence.
- Copied Unity Library separately; excluded source Temp, Logs, obj, build and .codex-worktrees.
- The source remains the Git common directory: this worktree still depends on its `.git` directory.
- Three transient Library files disappeared during the initial transfer: UndoData.bin, UndoStack.bin and Artifacts/ce/ce7b8306aed587a958051af66e2af91e.
  Reconciliation succeeded with session-only undo files excluded. A follow-up dry run found no
  differences. This is reusable cache material, not a proven Unity import.

## Implementation entry points

1. Shared layout: `MainMenuV3SectionLayoutView`, `CommanderProfileResponsiveLayoutView`,
   `UIShellContentView.RefreshMountedLayouts`, and each screen builder.
   Reuse `UISafeAreaView`, but inspect the region ownership before adding margins so that safe-area
   adjustment and shell region offsets are not applied twice.
2. Shared header: `MenuAccountHeaderAuthoring` already binds credits across the five builders.
   Preserve its account projection while introducing consistent geometry and back/title placement.
3. Commander: `UiAccountProfileProjectionSystem` reads the real account profile.
   `UiShellCommanderProfileModel` and `UiShellCommanderProfileComponent` currently expose identity only.
   `CommanderProfileContentView.Bind` binds name/subtitle only; the route binds the middle section once.
   Extend the read model to project saved level/XP/stats and the saved portrait into every affected section.
   `PlayerProfileSaveData` has commanderLevel, commanderXp, victories, defeats, missionsCompleted,
   starsEarned, enemiesDefeated, unitsLost, buildingsBuilt and resourcesEarned.
   No XP threshold rule or general battle-history feed was found in the reviewed files.
   Avoid inventing a next-level denominator or history rows; show available saved facts or a localized empty state.
4. Update editor builders and rebuild prefabs through `Tools/CI/invoke_unity_macos.sh`.
   Keep Hub open and signed in. No direct Unity execution, batchmode, reset or process termination.
5. Apply en/fa-IR layouts at 20:9, 16:9 and 4:3, with consistent readable text and actual safe-area checks.

## Validation plan and current evidence

- Visual review: existing mockups presented; direction approved; after captures pending.
- Automated checks: not run in this worktree. Reuse `MenuHeaderRemediationTests.RunFocusedValidation`
  for account projection/routing regressions, add focused data-binding checks only as needed,
  and run `MenuUiAuditCapture.Run` after rebuilding.
- Hardened the capture tool: unreachable routes set a failed result, screenshot files must exist and
  be nonempty, and captured count must equal requested views times five screens. It keeps partial
  findings on failure and reports capture completeness. This edit has not been compiled or run in Unity.
- Normal-input walkthrough: pending in both en and fa-IR. `MenuCompletionInputReview` is a useful
  physical-input observer, but its completion condition omits Commander and en/fa-IR coverage.
  Add that coverage before claiming the handoff walkthrough complete.
- Real phone/device acceptance: pending. Editor captures do not establish notch/safe-area acceptance.
- Preserve wrapper logs and check explicit pass markers, process exit status and expected outputs.

## Workspace copy verification

Source checksum comparison passed for 5,725 files. A final source refresh copied 14 files that changed during transfer.
Library reconciliation passed and its dry run found no remaining differences (session-only undo files excluded).
Capture-file whitespace check passed. See `WorkspaceSetupEvidence/` for failed initial cache-copy evidence,
reconciliation logs, and the sandboxed Git LFS check failure. Unity compilation and all player-readiness gates remain pending.

The broad Git whitespace check reported inherited Skirmish prefab/catalog whitespace failures and
was stopped after recording those failures (exit 130). The new capture edit passes its focused whitespace check.
No Unity Editor or Hub process was stopped.

## First approved implementation

Main Menu five-panel column geometry is implemented and captured in `After/main-menu-columns-20261003-175133/`.
Six geometry/capture checks and focused whitespace checks passed. See its `REVIEW.md` for visual review and pending acceptance gates.
