# Agent Pull Request Review And Merge Workflow

## Authority

This document is the authoritative git integration workflow for WarlineCapture agent tasks. It governs task branches, shared-object worktrees, implementation ownership, independent review, revisions, validation, tracker administration, merge, cleanup, and handoff evidence.

It supplements lane-specific product and validation contracts. When another live instruction conflicts on branching, committing, pushing, reviewing, or merging, this document wins. Historical implementation logs and reports remain evidence of the process used at the time and must not be rewritten to resemble this workflow.

## Activation And Grandfathering

- This workflow activates when the bootstrap commit containing this document reaches `main`.
- A task already in progress at that moment is grandfathered. It may finish using its existing worktree and direct `main` commit/push procedure.
- An explicitly active long-running tracker or automation program is one grandfathered assignment for its already-assigned program scope. The Architecture and Performance Hardening coordinator assignment closed on 2026-07-16 under its user-approved early-development rebaseline; its final tracker/report commit is the last grandfathered slice. The deferred pre-release certification backlog and any other substantive follow-up are future tasks, not extensions of that direct-main assignment.
- Outside that exception, a task starts when an implementation agent begins task-owned edits, not when it was discussed or placed in a backlog. Every task that starts after activation must use the pull request workflow below.
- The task handoff must say `Workflow path: grandfathered direct-main` or `Workflow path: pull request` so the transition is auditable.
- A grandfathered task is not converted mid-flight unless the user or review/merge coordinator explicitly chooses to move it to a feature branch.
- Work outside a grandfathered program's assigned scope, or substantive follow-up dispatched after that program closes, is a new task and must use a pull request.

Direct pushes to `main` remain technically open for now. GitHub branch protection or ruleset activation is deferred and may occur only after an explicit user instruction. No agent may claim that protection, required reviews, or push restrictions are active until that separate instruction has been implemented and verified.

## Identity And Role Separation

All agents currently use one shared GitHub identity. Independent review therefore means separation of agent role and conversational context, not a distinct GitHub account. Until separate identities exist:

- Do not require a GitHub approval count as evidence of independent review.
- The PR discussion, review handoff, validation evidence, and merge record provide the audit trail.
- The implementation agent and review/merge coordinator must still be different roles/contexts for the task, even though GitHub attributes their actions to the same account.

### Implementation Agent

The implementation agent owns substantive task work:

- creates or receives the short-lived task branch and isolated worktree;
- edits only the assigned file allowlist and preserves unrelated work;
- implements behavior, tests, assets, configuration, and task-owned documentation;
- runs focused, risk-appropriate validation and records exact evidence;
- commits and pushes the feature branch;
- opens and maintains the PR;
- responds to substantive review findings with new commits; and
- never merges its own PR or deletes the branch before merge disposition.

### Review/Merge Coordinator

The review/merge coordinator owns independent acceptance and integration:

- confirms task scope, baseline, ownership, and branch/worktree isolation;
- reviews findings first, ordered by severity and grounded in files and lines;
- returns every substantive correction to the implementation agent;
- verifies each revision rather than accepting a completion claim at face value;
- runs the risk-based integration gates against the final PR head;
- may add only administrative tracker or evidence commits to the feature branch;
- decides whether the PR is ready, blocked, or must return for revision;
- merges an accepted PR; and
- deletes the remote branch and removes the task worktree/local branch after merge.

The coordinator must not take over substantive implementation, tests, architecture, assets, configuration, CI behavior, or player-facing documentation. A typo inside coordinator-owned tracker/evidence administration is administrative; a change that can alter product behavior, test meaning, architecture, performance, assets, build behavior, or the substantive task explanation belongs to the implementation agent.

### Tracker Administration

The coordinator owns shared tracker administration: task reservation, progress snapshots, checklist state, decision records, implementation logs, accepted evidence links, and merge identity. The implementation agent supplies the evidence but does not mark its own shared tracker task accepted.

The coordinator records an in-progress reservation in the dispatch/current-task context and the open PR. To avoid shared tracker conflicts, the final tracker reconciliation normally lands as an administrative commit on the implementation PR after substantive findings are resolved. If a tracker claim must be committed earlier, the coordinator may add that administrative commit to the same branch. Tracker administration must describe actual state and must never manufacture a pass, rewrite historical entries, weaken a budget, or replace raw evidence.

## Branch And Worktree Contract

Every new task uses this naming contract:

```text
Branch: codex/<task-id>-<slug>
Worktree: /Users/farhad/Projects/WarlineCapture-Worktrees/<task-id>-<slug>
```

Use a lowercase stable task ID and a short lowercase kebab-case slug. Examples:

```text
codex/aph-704-index-adoption
codex/ui-214-command-feedback
codex/qa-088-device-smoke
```

The standard worktree root is the durable sibling directory `/Users/farhad/Projects/WarlineCapture-Worktrees`. `/private/tmp` is acceptable for disposable logs and generated artifacts, but it must not be the default worktree root: operating-system cleanup can remove it during day-long or Unity tasks.

Each checkout is a `git worktree` attached to the canonical repository. Worktrees share the repository object database and refs; they are not independent clones. Consequently:

- One branch may be checked out in only one worktree at a time.
- A fetch, branch creation, commit, rebase, or branch deletion is visible to sibling worktrees through shared repository metadata.
- Never copy a dirty tree over another worktree or use one worktree to clean/revert another agent's files.
- Never delete, reset, restore, or reformat changes outside the task allowlist.
- Prefer the task worktree for Unity validation when its `Library`, editor lock, and lease state make that safe. A separate Unity project copy may be used when those states must not be shared, but before validation it must be clean and checked out or detached at the exact PR head or combined integration SHA being tested. Record its path plus the output of `git status --short --branch` and `git rev-parse HEAD`; stale, dirty, or wrong-revision workspaces cannot provide acceptance evidence.

### Create The Task Worktree

Start from the current remote baseline; the new task worktree itself must be clean:

```bash
cd /Users/farhad/Projects/WarlineCapture
git fetch origin main
mkdir -p /Users/farhad/Projects/WarlineCapture-Worktrees
git worktree add -b codex/<task-id>-<slug> /Users/farhad/Projects/WarlineCapture-Worktrees/<task-id>-<slug> origin/main
cd /Users/farhad/Projects/WarlineCapture-Worktrees/<task-id>-<slug>
git status --short --branch
git rev-parse HEAD
```

If the branch already exists, stop and inspect its owner and worktree registration with `git worktree list`; do not reuse or overwrite it. If the canonical checkout has unrelated changes, leave them untouched. Creating a worktree from `origin/main` does not require cleaning the canonical checkout.

Sparse checkout may be used when the task has a strict allowlist, but it must include every owned file, required contract, test, and tracker/evidence path. Sparse visibility never authorizes changes outside the allowlist.

## End-To-End Workflow

### 1. Dispatch And Claim

The coordinator gives the implementation agent:

- task ID and objective;
- branch and worktree path;
- baseline expectation;
- exact file allowlist and files intentionally excluded;
- required contracts and behavior that must remain unchanged;
- required focused validation and broader risk gates;
- Unity/device/Jenkins ownership and evidence expectations;
- tracker/report paths; and
- the review/merge coordinator context.

The coordinator checks for overlapping open PRs, worktrees, branches, Unity leases, and file claims before dispatch. Parallel work is allowed only when ownership and mutable validation resources do not overlap.

### 2. Implement On The Feature Branch

Before editing, the implementation agent verifies branch, worktree, baseline, status, and allowlist. During implementation it must:

- keep changes narrowly scoped and preserve existing user/agent work;
- add or update tests in proportion to behavior and architecture risk;
- preserve Unity `.meta` files and serialized identities;
- preserve existing Jenkins, CI, performance, GC, device, and evidence contracts unless the task explicitly owns a reviewed change to them;
- avoid adding GitHub Actions as part of this bootstrap or as a substitute for Jenkins; and
- record missing validation, environmental blockers, and untested paths instead of implying coverage.

The implementation agent may make multiple focused commits. Commit messages must identify coherent behavior or evidence, and commits must not contain unrelated cleanup.

### 3. Validate Before Opening Review

At minimum, every task runs:

```bash
git status --short
git diff --check origin/main...HEAD
git diff --stat origin/main...HEAD
```

It also runs the risk-based rows below. Validation must identify exact commands, pass markers/counts, logs, artifacts, environment, and the commit tested. A failed, skipped, interrupted, stale, dirty-build, wrong-revision, or unavailable check is not a pass.

Before requesting review, commit every intended PR change and leave the worktree clean. Deliberately excluded local artifacts must be named in the handoff and must not appear in the PR.

### 4. Push And Open The PR

The implementation agent pushes its branch and opens a PR against `main`:

```bash
git push -u origin codex/<task-id>-<slug>
gh pr create --base main --head codex/<task-id>-<slug>
```

Complete `.github/pull_request_template.md`; do not delete sections or convert unknowns into passes. A draft PR may be opened early to publish a claim, but review begins only when the implementation agent marks the evidence ready. The implementation agent reports the branch, head commit, PR URL, changed files, validation, risks, and untested paths. It does not merge.

### 5. Independent Review: Findings First

The review/merge coordinator checks the PR head and exact `origin/main...HEAD` diff in an independent context. The response starts with findings, highest severity first, with file/line references. It then lists questions/assumptions and validation gaps. A summary comes last.

Review covers at least:

- behavior correctness and regression risk;
- scope/allowlist compliance and preservation of unrelated work;
- contract, API, serialized-data, asset, and assembly impacts;
- architecture ownership, lifecycle, ECS/Burst, source-growth, and lookup rules;
- test quality, compiler status, and meaningful negative/failure coverage;
- managed allocation, performance, memory, package, and device implications;
- Unity hierarchy, console, Play Mode, visual, and safe-area evidence where applicable;
- Jenkins and existing CI contract compatibility; and
- provenance, exact revision, residual risk, and untested paths.

No finding means no material defect was found in the reviewed scope; it does not erase explicitly untested paths or unavailable device/CI evidence.

### 6. Return Substantive Revisions To The Implementer

For any substantive finding, the coordinator sets the PR to `needs fixes` and returns it to the implementation agent. The implementation agent makes the correction in the same worktree/branch, reruns affected validation, commits, pushes, and updates the PR evidence.

The revision handoff maps each finding to:

```text
Finding:
Resolution commit:
Files changed:
Validation rerun:
Result:
Remaining risk:
```

The coordinator rereviews the complete final diff, not only the newest commit. Repeat until substantive findings are resolved or the PR is explicitly blocked/closed. Do not force-push reviewed history unless the coordinator explicitly requests it and no other context has added commits.

### 7. Integration Gate And Administrative Reconciliation

After substantive review passes, the coordinator must validate the actual combined result with the latest `origin/main`; overlap inspection alone is not acceptance. Use one of these methods:

1. **Implementation-branch integration:** the implementation agent integrates the fetched latest `origin/main` into the feature branch, resolves every content-changing conflict, and reruns all task-required integration gates plus every gate affected by the integration. The resulting feature head is the combined validation SHA.
2. **Temporary merge-result validation:** the coordinator creates a clean temporary integration branch/worktree at fetched latest `origin/main`, merges the final PR head without changing the feature branch, then runs the full required integration gates at that exact commit and tree. The temporary merge commit is never pushed as task work.

For either method, the coordinator:

1. Records the latest `origin/main` SHA, final PR-head SHA, integration method, exact combined validation commit SHA, and combined tree SHA.
2. Inspects the complete combined diff, allowlist, unrelated-work exclusions, and commits merged since the task baseline.
3. Returns substantive integration or conflict work to the implementation agent; the coordinator resolves only purely administrative tracker/evidence conflicts.
4. Runs `git diff --check` and every risk-based integration gate required by the task against the exact combined validation tree.
5. Confirms compiler logs contain zero task-attributable errors and all test, Unity, device, performance, and visual evidence belongs to that revision. Any separate Unity validation workspace must satisfy the clean status and exact-SHA contract above.
6. Confirms Jenkins-required work still uses the established Jenkins contracts. It may wait for or inspect Jenkins evidence; it does not add a GitHub Actions substitute.
7. Adds only truthful tracker/evidence administration, if required, as a focused coordinator commit on the feature branch.
8. Repeats combined-result validation when that administrative commit changes the feature head, and reruns every affected gate.
9. Fetches `origin/main` again immediately before merge. If its SHA or the remote PR-head SHA differs from the recorded inputs, the combined result is stale and this gate must be repeated.
10. Updates the PR checklist with commit-bound evidence and the recorded input and combined-validation SHAs.

If a required gate fails, the PR does not merge. The coordinator returns substantive fixes to the implementer or records the exact external blocker and owner.

### 8. Merge

The review/merge coordinator alone merges an accepted PR. Immediately before merge it confirms:

- the PR targets `main` from the expected `codex/<task-id>-<slug>` branch;
- the reviewed head SHA matches the current remote head;
- the current `origin/main`, PR head, and validated combined result match the recorded integration inputs and combined tree;
- all substantive findings are resolved;
- required risk gates and final diff checks pass;
- tracker/evidence administration identifies the actual PR and revision;
- no unrelated files entered the PR; and
- residual risks and untested paths are explicit and acceptable for the task.

Use a normal merge commit by default so implementation and administrative evidence commits remain attributable. Use another merge strategy only when the user or repository policy explicitly requires it. The implementation agent never performs this step.

### 9. Cleanup

After confirming the PR is merged, the coordinator deletes the remote branch and removes the local worktree/branch:

```bash
git worktree remove /Users/farhad/Projects/WarlineCapture-Worktrees/<task-id>-<slug>
git branch -d codex/<task-id>-<slug>
git push origin --delete codex/<task-id>-<slug>
git worktree prune
```

If the worktree is dirty, stop and identify the owner; never force-remove it merely to complete cleanup. If the hosting merge command already deleted the remote branch, verify that state instead of treating the second delete as a failure.

### 10. Final Handoff

The coordinator records the merge commit, PR URL, deleted branch/worktree state, tracker update, retained evidence, and residual risks. Historical reports and implementation logs remain in place.

## Risk-Based Validation Matrix

Apply every row triggered by the change. The coordinator may widen validation based on blast radius; it may not narrow an explicit task or tracker contract without user approval.

| Risk | Implementation evidence | Coordinator integration evidence |
|---|---|---|
| Documentation or tracker administration only | scoped text/contract checks, link/path checks when available, `git diff --check` | exact diff and authority/conflict search; verify no historical evidence was rewritten |
| Bounded code or tooling with no shared runtime contract | focused unit tests, owning build/compiler check, negative path | rerun focused test/build and inspect complete diff |
| Shared runtime, ECS, UI shell, composition, config, or serialization | affected builds, focused behavior tests, architecture/assembly/source-growth gates, relevant smoke | dependent builds, integrated architecture gates, smoke, compiler/log scan |
| GC, hot path, memory, package, startup, or performance | exact before/after methodology, warmed measurements, accepted budgets, artifact revision | rerun or independently verify contract at final SHA; reject stale, dirty, or incomparable evidence |
| Unity scene, prefab, importer, visual, input, or route | focused Unity tests, hierarchy/console/Play Mode inspection, fixed-view captures and public path where applicable | independent Unity/log inspection and required visual/device matrix on final content |
| Android/device or Jenkins contract | exact build revision/config, device/build metadata, logs/artifacts, existing Jenkins job/stage evidence | verify provenance and required Jenkins/device acceptance; no GitHub Actions substitute |
| High-risk cross-domain or release behavior | all applicable rows plus broader regression matrix and rollback/residual-risk statement | full task/tracker integration gate and any release/device evidence required by the owning contract |

Existing performance and CI contracts remain authoritative for their exact budgets, commands, scenarios, and pass markers. This workflow controls ownership and integration; it does not relax those contracts.

## Required Handoff Fields

Use these fields for implementation completion, revision, review, and merge handoffs. Use `None`, `Not run`, or `Blocked: <reason>` rather than omitting a field.

```text
Role and context:
Lane:
Task ID and objective:
Workflow path: pull request / grandfathered direct-main
Branch:
Worktree:
Baseline commit:
Head commit tested:
Latest origin/main SHA used for integration:
Combined integration commit and tree SHAs tested:
PR URL:
File allowlist:
Files changed:
Files intentionally not touched:
Contracts touched:
User-visible behavior:
Architecture impact:
Tests run:
Compiler/build result:
GC/performance/memory result:
Unity hierarchy/console/Play Mode evidence:
Unity validation workspace status and HEAD:
Device/visual evidence:
Jenkins/CI evidence:
Artifacts and logs:
Tracker/evidence administration:
Review findings and resolution commits:
Risks:
Untested paths:
Merge disposition:
Branch/worktree cleanup:
Next owner and action:
```

## Copy-Ready Implementation-Agent Prompt

```text
Implement task <task-id>: <objective>.

Use the authoritative workflow in Design/Architecture/agent_pull_request_review_merge_workflow.md. This task started after that workflow reached main and is not grandfathered.

Branch: codex/<task-id>-<slug>
Shared-object git worktree: /Users/farhad/Projects/WarlineCapture-Worktrees/<task-id>-<slug>
Baseline: current origin/main at dispatch
File allowlist: <paths>
Files intentionally excluded: <paths>
Contracts to preserve: <contracts>
Required validation: <commands/gates/evidence>
Unity/device/Jenkins ownership: <details>
Tracker/report paths: <paths>

Own all substantive implementation and revisions. Preserve unrelated work and do not edit outside the allowlist. Run risk-based validation, commit coherent changes, push the feature branch, and open a PR against main using .github/pull_request_template.md. Report every required handoff field, including risks and untested paths. Do not merge, delete the branch, mark your own tracker task accepted, or add GitHub Actions.
```

## Copy-Ready Review/Merge Invocation

```text
Act as the independent review/merge coordinator for <task-id> in a separate role/context from the implementation agent.

Authority: Design/Architecture/agent_pull_request_review_merge_workflow.md
PR: <url-or-number>
Branch: codex/<task-id>-<slug>
Worktree: /Users/farhad/Projects/WarlineCapture-Worktrees/<task-id>-<slug>
Expected baseline: <hash>
File allowlist: <paths>
Required contracts and gates: <contracts/commands/evidence>

Review the complete origin/main...PR-head diff and report findings first, ordered by severity with file/line references. Return every substantive fix to the implementation agent and rereview all revisions. Validate the actual combined result with latest origin/main using one of the two authorized integration methods; overlap inspection alone is insufficient. Record origin/main, PR-head, and combined-validation SHAs, and run all required gates at the exact combined tree. Prefer the task worktree for Unity; any separate Unity workspace must be clean at that exact SHA with status and rev-parse recorded. You may add only administrative tracker/evidence commits. Preserve Jenkins and existing CI/performance contracts; do not add GitHub Actions. If accepted, merge the PR, delete the remote branch, remove the clean worktree/local branch, and report the required handoff fields, final merge commit, PR URL, validation, cleanup, and residual risks. Do not require a GitHub approval count because all agents currently share one GitHub identity. Do not enable or claim branch protection/rulesets without explicit user instruction.
```
