# PM Message To Designer

Date: 2026-05-14
Priority: P0
Status: assigned

This file is a direct PM-to-Designer command supplement. On `continue`, read this file with `Design/AgentTasks/designer_current.md` and treat the assignment as already dispatched. Do not report waiting for a separate handoff; the Designer current task and this message are the handoff. Older or newer Gameplay/runtime reports are context only and do not cancel this task.

Designer is now the active owner for M01 step-by-step gameplay specification.

The PM-authored package under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/` is draft reference only. It is not the source of truth until you review it against approved visual lock targets and either accept, correct, or replace it.

Read first:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/README.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/M01_StepByStepGameplayMockup_Manifest.json`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`

Deliver:

- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`

Your report must include:

- exact ordered M01 gameplay steps from start state through result state
- player input/action per step
- camera/framing per step
- units visible and their poses, facing, selection state, and survival state
- move, attack, objective, invalid-command, ARIA, minimap, and result states
- HUD, command panel, objective panel, minimap, assistant, and log states
- transitions, timing, and recovery/error states Art must show
- what must stay aligned with approved VisualLock targets
- Art acceptance checklist for mockup images
- explicit decision on whether the PM draft is accepted as-is, corrected, or replaced

Use this exact delivery format:

1. `# M01 Step-By-Step Gameplay Spec`
2. `## Sources Reviewed`
3. `## Design Authority Decision`
   - `Decision:` one of `accept PM draft`, `correct PM draft`, or `replace PM draft`
   - `Reason:`
   - `VisualLock alignment notes:`
4. `## Step Table For Art`
   - one row per gameplay frame/step
   - columns: `Step ID`, `Gameplay beat`, `Player input/action`, `Camera/framing`, `Units/poses/facing/selection/survival`, `HUD/panels/minimap/assistant/log`, `Visual feedback/FX/ARIA`, `Transition/timing/recovery`, `Art mockup notes`
5. `## Required Mockup Frames`
   - exact filenames Art should create under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`
   - one sentence per frame describing the expected image
6. `## VisualLock Constraints`
7. `## Art/Atlas Acceptance Checklist`
8. `## Blocked Lanes And Handoff`
   - `Next lane: Art/Atlas`
   - `Still blocked: Gameplay, QA/HCI`
   - `User approval required before project import or Gameplay implementation: yes`

Do not generate final mockup images. Do not hand work to Gameplay. Your report unblocks Art/Atlas only.
