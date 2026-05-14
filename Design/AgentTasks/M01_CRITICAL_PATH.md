# M01 Critical Path

Date: 2026-05-14
Status: corrected for step-by-step mockup flow
Goal: Produce user-approved M01 step-by-step gameplay mockups before Gameplay implementation.

## Rule

No Gameplay implementation starts from a PM-authored draft package.

The package under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/` is draft reference only until Designer reviews it against approved visual lock targets and publishes the authoritative step-by-step spec.

## Current Gate

Active owner:
Designer

Next production owner after Designer:
Art/Atlas

User approval required before:

- adding mockup output to the project as accepted visual lock
- routing Gameplay implementation
- routing QA/HCI runtime validation

## Correct Sequence

1. Designer writes `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`.
2. Art/Atlas creates step-by-step mockup images/contact sheets from the Designer spec and reports `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`.
3. User approves or rejects the mockup images.
4. PM routes Gameplay to implement only the approved mockups.
5. QA/HCI validates readability and runtime behavior after implementation, or reviews mockups only if PM explicitly routes a pre-implementation review.

## Source Authority

Draft reference:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/README.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/M01_StepByStepGameplayMockup_Manifest.json`

Design source of truth, once delivered:

- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`

Art approval artifact, once delivered:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

## Blocked Lanes

Gameplay is blocked until user-approved Art mockup images exist.

QA/HCI is blocked until PM routes either a mockup readability review or a runtime validation task.

## Superseded Routing

Any prior 2026-05-07 through 2026-05-09 M01 routing that makes Gameplay or QA/HCI the next owner for this step-by-step mockup flow is superseded by this correction.

Older runtime proof, soldier atlas, and launch-path reports remain historical evidence, but they are not authority for the missing step-by-step mockup package.
