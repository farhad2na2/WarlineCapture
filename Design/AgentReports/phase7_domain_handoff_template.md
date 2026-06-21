# Phase 7 Domain Agent Handoff Template

Use this template for every Agent B-F handoff before Agent A merges a domain branch.

Report path:
`Design/AgentReports/YYYY-MM-DD_phase7_agent_<lane>_handoff.md`

## Summary

- Agent lane: `AgentB|AgentC|AgentD|AgentE|AgentF`
- Source branch:
- Source commit:
- Handoff date:
- Domain scope:
- Integration readiness: `Ready|Blocked|DeferredWithReason`

## Inventory Rows Touched

| Id | Type | Path | Starting disposition/status | Result disposition/status | Notes |
| --- | --- | --- | --- | --- | --- |
| `P7-0000` | `TypeName` | `Assets/Game/Scripts/...` | `DirectConvert/Open` | `Converted/Converted` |  |

## Files Changed

| File | Change type | Reason |
| --- | --- | --- |
| `Assets/Game/Scripts/...` | `Modified|Added|Deleted` |  |

## Systems Converted, Split, Or Retired

- Converted to `ISystem`:
- Split into unmanaged processors plus managed exception:
- Retired/folded:
- Managed presentation/config/camera exceptions retained or created:

## Shared Contracts

Declare whether this branch touched any shared contracts.

- Shared components/contracts touched: `Yes|No`
- Asmdefs touched: `Yes|No`
- Tests touched: `Yes|No`
- Generated inventory touched: `Yes|No`
- Details:

## Validation

| Gate | Command | Log path | Result |
| --- | --- | --- | --- |
| Compile | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` | `/private/tmp/warline-phase7-agent-<lane>-compile.log` | `NotRun|Passed|Failed|BlockedProjectLocked|BlockedMissingRunner|DeferredWithReason` |
| Architecture guard | `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-<lane>-architecture.log` | `/private/tmp/warline-phase7-agent-<lane>-architecture.log` | `NotRun` |
| Domain focused gate | `<Unity command from Agent A validation matrix>` | `/private/tmp/warline-phase7-agent-<lane>-<target>-<validation>.log` | `NotRun` |

## Expected Conflicts

- Inventory rows likely to conflict:
- Shared files likely to conflict:
- Contract changes that affect another lane:

## Blockers And Deferred Work

- Blockers:
- Deferred validation:
- Deferred conversion rows:

## Agent A Integration Checklist

- [ ] Source branch is up to date with its base.
- [ ] Handoff lists every touched inventory id.
- [ ] Handoff declares shared contracts, asmdefs, tests, and inventory edits.
- [ ] Handoff lists expected conflicts.
- [ ] Agent A merged this branch into `codex/phase7-integration`.
- [ ] Agent A regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- [ ] Agent A ran `git diff --check`.
- [ ] Agent A ran Phase 7 architecture guardrails.
- [ ] Agent A ran affected focused domain validations or recorded explicit deferred owners.
- [ ] Agent A updated only relevant inventory rows and main tracker progress.
