# Designer Current Task

Date: 2026-05-14
Status: active
Priority: P0 Design-owned M01 step-by-step gameplay spec before Art mockups

## Assignment

PM correction: the existing PM-authored package under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/` is draft reference only. It is not design-approved, not implementation-ready, and must not be used to route Gameplay.

Designer owns the exact M01 step-by-step gameplay spec that Art/Atlas will use to produce review mockup images.

Read first:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/README.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/M01_StepByStepGameplayMockup_Manifest.json`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`

Required Designer output:

- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`

The report must include:

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

## Required Report Format

Use this exact section order in `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md` so PM can hand the result to Art/Atlas without interpretation:

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

Do not create final mockup images. Do not route Gameplay. Do not commit or push.

## Waiting On

Waiting on lane:
Designer

Next lane after Designer delivery:
Art/Atlas

Blocked lanes:
Gameplay and QA/HCI remain blocked until Art produces mockup images and the user approves them.

Completion report expected:
`Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`
