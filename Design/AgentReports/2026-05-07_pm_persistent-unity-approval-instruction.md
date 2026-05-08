Status: advisory
Topic: Reduce repeated Unity validation approval prompts
Docs updated:
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentTasks/gameplay_current.md`
Reason:
- The user does not want to manually approve every required Unity renderer test, capture builder, or focused validation pass while agents are continuing on heartbeat.
Operational clarification:
- PM cannot click or override Codex sandbox prompts inside another agent thread.
- Required Unity validation remains product-approved when it is listed in the active lane task and uses a dedicated WarlineCapture Unity workspace.
- If Codex/tool sandbox approval is required, agents should request a persistent remembered approval for the narrow Unity executable + `-batchmode` permission when the tool UI offers that option.
Cross-lane notice:
- Agents should not request broad shell approval or arbitrary script approval just to avoid prompts.
- Agents should still pause for destructive actions, network access, writes outside allowed workspaces, or sandbox prompts that are not part of required focused validation.
Next action:
- If an agent asks again, approve the prompt only if it is for required focused WarlineCapture Unity validation, and choose the UI option to remember/always allow the narrow Unity batchmode permission when available.
