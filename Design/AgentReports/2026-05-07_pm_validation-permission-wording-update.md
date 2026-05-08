# PM Dispatch: Validation Permission Wording Update

Date: 2026-05-07

## Trigger

The user noticed agents still asking for permission with wording such as running a focused Unity prefab builder because a CodexUnity clone is already open.

## Clarification

Agents are authorized to run required focused Unity validation, prefab builders, capture builders, and report-generation commands for their active lane task. They should not pause to ask whether required validation should run.

Agents may still need a user approval click when Codex/tool sandbox requires it. That is a tool permission issue, not a product decision.

## Updated Docs

- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`

## Required Agent Behavior

Agents should phrase future pauses like:

```text
Codex needs tool approval to run the focused Unity validation command because the target Unity project/clone requires elevated access. Please approve the tool prompt so validation can continue.
```

Agents should not phrase this as:

```text
Should I run the required Unity tests?
```

## User Decision Needed

No. This is a workflow clarification.
