# WarlineCapture Designer Role And Documentation Workflow

Date: 2026-05-08

## Purpose

This document defines the designer role for WarlineCapture and how that role keeps the README, design index, product direction, and implementation-facing design docs clear and aligned.

The designer is not a separate source of truth. The designer maintains coherence across the existing source-of-truth docs and turns scattered implementation state into readable product/design direction.

## Designer Ownership

The designer owns:

- Product/design coherence across the root `README.md` and `Design/README.md`.
- Design-doc hierarchy, reading order, and source-of-truth clarity.
- Terminology alignment across gameplay, UI, art, FTUE, monetization, marketing, and agent handoff docs.
- Player-facing design quality: mode purpose, core loop clarity, UI hierarchy, tactical readability, onboarding, reward communication, and district consequence clarity.
- Documentation pruning recommendations when README or design docs become duplicated, stale, or too implementation-heavy.
- Design-review notes for visual-lock targets, tactical captures, safe-area captures, HUD readability, and mode flow.

The designer does not own:

- Gameplay implementation details, ECS architecture, or runtime code contracts.
- UI prefab construction or Unity Canvas implementation.
- Balance numbers, catalog ids, or save schemas.
- Generated project-state dashboard values.
- Replacing PM, QA/HCI, gameplay, UI, support/FTUE, or art-atlas lane ownership.

## Primary Inputs

Read these first:

1. `README.md`
2. `Design/README.md`
3. `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
4. `Design/WarlineCapture_Level_And_Mission_Content_Plan.md`
5. `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
6. `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
7. `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
8. `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
9. `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`
10. `Design/WarlineCapture_Project_State_Source.json`

Use generated dashboards and agent reports as evidence, not as design sources to rewrite by hand.

## Review Checklist

When reviewing or optimizing docs, check:

- The root README summarizes the current project accurately without becoming a full design dump.
- `Design/README.md` remains the complete design index and source-of-truth map.
- The opening project description matches the gameplay north star: a field commander preparing and executing operations while protecting civilians and infrastructure.
- The three-mode structure remains clear: Campaign teaches, Operations prove persistent command, Skirmish supports replay/testing.
- Planning, briefing, minimap, deployment, threat, and battle views are described as UI/camera layers on one 3D operation map.
- 3D operation maps resolve through metadata-backed operation-map packages.
- Visual-lock docs are referenced through the canonical index instead of duplicated stale lists.
- UI mockups are described as targets/references, not shippable full-screen screenshots.
- Design docs separate balance data, visual data, implementation contracts, and player-facing intent.
- New docs include a clear purpose, owner/audience, dependencies, acceptance criteria, and update rules.

## Documentation Rules

- Update `Design/README.md` whenever a design doc is added, renamed, retired, or promoted.
- Update the root `README.md` only when the top-level product direction, source-of-truth map, setup instructions, or contributor workflow changes.
- Prefer linking to canonical docs over copying long file inventories into README.
- If two docs disagree, identify the newer source of truth and either update the stale doc or add an explicit superseded/legacy note.
- Do not manually edit generated files such as `Design/WarlineCapture_Project_State_Dashboard.md`; update their source and regenerate.
- Keep player-facing concepts in design docs, implementation contracts in handoff docs, and lane task status in `Design/AgentTasks`.

## Designer Report Template

Designer reviews should use this format:

```text
Lane: Designer
Task:
Files reviewed:
Files changed:
Design decision:
Alignment findings:
README impact:
Design index impact:
Cross-lane impacts:
Validation run:
Known gaps:
Next recommended task:
```

Save accepted designer reports under:

```text
Design/AgentReports/<YYYY-MM-DD>_designer_<short-task>.md
```

## First Optimization Targets

1. Keep the root README aligned with `Design/README.md` without duplicating every visual-lock target.
2. Add stale/legacy notes where old 2D isometric, macro-tile, strategic/tactical-map, desert/current-asset, or old-mode assumptions conflict with the 3D single-map direction.
3. Audit docs for ambiguous use of `Mission`, `ScenarioSetup`, `Level`, `Map`, `OperationMapId`, planning camera ids, and minimap projection ids.
4. Keep M01 First Contact documentation focused on the playable vertical slice and avoid presenting it as the whole game.
5. Turn recurring design-audit findings into durable update rules in the relevant canonical docs.
