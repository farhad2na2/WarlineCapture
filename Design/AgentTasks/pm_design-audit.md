# PM Design Audit Task

Date: 2026-05-07
Status: ongoing
Priority: continuous quality control

## Assignment

When there is no new agent handoff to review, audit WarlineCapture design docs for AAA production risks: undefined behavior, conflicting contracts, missing ids, missing validation gates, agent guesswork, UI/gameplay mismatch, asset approval ambiguity, or gaps that could send agents in different directions.

## Audit Scope

Prioritize docs that active agents depend on:

- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md`
- `Design/WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`
- active lane tasks under `Design/AgentTasks/`

## What To Look For

- A feature says what should happen visually but not which runtime data/source owns it.
- A UI element exists but no gameplay event, command result, id, or controller owns it.
- Gameplay logic exists but no UI state, error state, or validation message is defined.
- FTUE says `Show Me` or `Do It` but does not define a typed command intent.
- A design says "approved", "ready", "complete", or "visual lock" without a validation gate.
- A flat visual reference is treated as an implementation-ready sliced layer pack.
- Strategic map and tactical gameplay map responsibilities are mixed.
- Asset rows can be marked complete without approval plus runtime wiring.
- Agents would need to invent names, ids, paths, reason codes, states, or acceptance criteria.

## Output Format

Write audit findings to:

`Design/AgentReports/<YYYY-MM-DD>_pm_design-audit-<topic>.md`

Use this format:

```text
Status: accepted / needs fixes / blocked / advisory
Topic:
Docs reviewed:
Finding:
Why it matters:
Recommended fix:
Affected lanes:
Needs user decision:
Next task update needed:
```

## Operating Rule

Do not rewrite large design areas during an idle audit. Surface the ambiguity first, suggest the smallest fix, and only update task files or docs when the user asks or when the fix is clearly mechanical and low-risk.
