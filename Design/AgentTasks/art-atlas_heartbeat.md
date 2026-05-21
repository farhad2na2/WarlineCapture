# Art/Atlas Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/art-atlas_current.md` as the only source of current Art/Atlas priorities.

## 2026-05-22 Reset Guard

If `Design/AgentTasks/art-atlas_current.md` says `Status: held`, stop. Do not scan `Design/AgentReports/` for new work, do not generate assets, do not write a report, and do not route another lane. Respond only that Art/Atlas is held for the 3D fresh-start reset and waiting for PM/user dispatch.

## On Every Heartbeat

- Read `Design/AgentTasks/art-atlas_current.md`.
- For any target-lock bitmap mockup, VisualLockLayered reference image, contact sheet, or flattened review PNG, use the imagegen skill/workflow as the source of the visual. This is mandatory.
- Never create target-lock visuals with deterministic local rendering, scripted compositing, manual shape overlays, pixel patching, HTML/CSS screenshots, layout-debug renders, programmatic HUD assembly, or other non-imagegen visual construction. Deterministic tooling may only be used after an imagegen result is selected to package metadata, inspect dimensions, prepare layer manifests, or validate files.
- If Art/Atlas cannot access or use imagegen for a routed visual target task, stop and write a blocker report. Do not substitute deterministic output.
- Check `Design/AgentReports/` for new Art/Atlas-relevant handoffs or blocker reports.
- Assess new relevant handoffs as accepted, needs fixes, or blocked.
- Continue the current Art/Atlas task if actionable.
- Anti-idle rule: if Art/Atlas is `Status: active`, every heartbeat must either advance the task, write the expected handoff, or write a blocker report with the exact failed command, workspace, log path, missing dependency, and unblock owner.
- Unity licensing workaround is mandatory when PM assigns Art/Atlas Unity validation: follow `Design/AgentTasks/AUTO_CONTINUE.md` and use the PM-assigned workspace with Codex escalation/out-of-sandbox execution before reporting a licensing blocker.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if PM/user attention or art approval is needed, a blocker appears, or the Art/Atlas handoff is ready.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/art-atlas_current.md`.
- Imagegen-only rule is permanent for visual target art: no deterministic generated art, no patched local composites, no programmer-looking placeholders, and no assembled mockups presented as final target-lock imagery.
- Layer manifests, source notes, slicing specs, and metadata can be deterministic documentation, but the visual target bitmap itself must come from imagegen.
- Do not modify source docs or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to Art/Atlas for a named file set.
