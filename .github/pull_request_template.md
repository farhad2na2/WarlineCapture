## Scope And Baseline

- Task ID:
- Objective:
- Workflow path: `pull request` (grandfathered direct-main tasks do not open or use this template unless explicitly converted to the PR workflow)
- Baseline commit:
- Head commit tested:
- Branch: `codex/<task-id>-<slug>`
- Worktree:
- File allowlist:
- Files intentionally not touched:
- Related tracker/report:

## Ownership

- Implementation agent role/context:
- Review/merge coordinator role/context:
- Substantive revision owner:
- Tracker/evidence administration owner:

All agents currently share one GitHub identity. Independent review is role/context separation; no GitHub approval count is required until separate identities exist.

## Behavior

- User-visible behavior before:
- User-visible behavior after:
- Behavior intentionally unchanged:
- Contracts/API/schema/prefab/route/asset IDs touched:
- Cross-lane impacts:

## Architecture

- Ownership/dependency impact:
- ECS/Burst/job impact:
- Assembly/composition/UI boundary impact:
- Lifecycle/static state/runtime-loop impact:
- Serialization/Unity `.meta` impact:
- Source-growth/exception impact:
- Architecture gates and result:

## Tests

- Focused tests:
- Regression/integration tests:
- Negative/failure-path tests:
- Exact commands and pass markers/counts:
- Test logs/artifacts:

## Compiler And Build

- Assemblies/build targets checked:
- Exact commands:
- Result and error count:
- Warnings introduced or changed:
- Build logs/artifacts:

## GC And Performance

- Hot-path or allocation impact:
- Before/after methodology and values:
- Warmup/sample/scenario details:
- Budget/accepted-baseline result:
- Memory/startup/package impact:
- Profiler/performance artifacts and revision:

## Unity, Device, And Visual Evidence

- Unity version/workspace:
- Hierarchy/console/Play Mode inspection:
- Public launch/manual path:
- Device/build/configuration identity:
- Screenshots/video/contact sheets:
- Visual/readability/safe-area result:
- Evidence not applicable and why:

## Jenkins And CI

- Existing Jenkins jobs/stages/contracts affected:
- Jenkins build ID/status/artifacts:
- Existing CI/performance contract result:
- GitHub Actions added: `No` (this bootstrap does not add or substitute GitHub Actions)

## Risks And Gaps

- Known risks:
- Untested paths:
- Environmental/tooling blockers:
- Rollback or containment:
- Follow-up owner/task:

## Implementation-Agent Checklist

- [ ] Diff is limited to the declared scope and allowlist.
- [ ] Unrelated user/agent work is preserved.
- [ ] Every intended PR change is committed and the task worktree is clean; excluded local artifacts are named.
- [ ] `git diff --check origin/main...HEAD` passes.
- [ ] Required focused tests and compiler/build checks pass or blockers are explicit.
- [ ] Architecture, GC, performance, Unity, visual, device, and Jenkins evidence is included when triggered by risk.
- [ ] Evidence identifies the exact tested commit and is not stale or from a dirty build unless explicitly rejected/qualified.
- [ ] Risks and untested paths are explicit.
- [ ] Tracker/report evidence is supplied without self-accepting the task.
- [ ] Feature branch is pushed and this PR targets `main`.
- [ ] I did not merge this PR or add GitHub Actions.

## Review Findings And Revisions

Record findings first. Map substantive fixes to implementation-agent commits.

| Finding | Severity | Resolution commit | Validation rerun | Status |
|---|---|---|---|---|
| None yet | - | - | - | Pending review |

## Reviewer/Merge Checklist

- [ ] Reviewer is operating in a separate role/context from the implementation agent.
- [ ] Complete `origin/main...HEAD` diff and allowlist were reviewed; findings were reported first.
- [ ] Substantive fixes were returned to the implementation agent and the complete revised diff was rereviewed.
- [ ] Final head SHA matches the revision used for integration validation.
- [ ] Risk-based tests, compiler, architecture, GC/performance, Unity/device/visual, and Jenkins gates pass or have an explicitly accepted non-required gap.
- [ ] No required budget, guardrail, test, or evidence contract was weakened to obtain a pass.
- [ ] Only administrative tracker/evidence commits, if any, were added by the coordinator.
- [ ] Historical reports and implementation-log evidence were preserved.
- [ ] Existing Jenkins and CI/performance contracts remain intact; no GitHub Actions substitute was added.
- [ ] Direct pushes remain technically open; this PR does not claim branch protection or rulesets are active.
- [ ] Branch protection/rulesets were not enabled without explicit user instruction.
- [ ] Tracker/evidence administration records the actual PR, head, validation, and merge disposition.
- [ ] Risks and untested paths are acceptable and have owners where needed.
- [ ] PR is ready for coordinator merge; implementation agent has not merged it.
- [ ] After merge, remote branch and clean local worktree/branch will be deleted and cleanup recorded.
