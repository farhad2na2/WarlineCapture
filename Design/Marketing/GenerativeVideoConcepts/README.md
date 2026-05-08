# WarlineCapture Generative Cinematic Video Workflow

This folder defines the workflow for AI-generated marketing videos that are inspired by WarlineCapture's concept, not built from gameplay screenshots or UI mockups.

## Goal

Create short cinematic 3D-style trailer clips that communicate the WarlineCapture fantasy:

- Commanding a damaged near-future city under pressure.
- Directing squads, convoys, drones, and defensive units.
- Recovering districts through Operation choices.
- Presenting the game as a premium mobile tactical RTS without implying false gameplay footage.

The video may be more cinematic than the actual 2D isometric gameplay, but it must stay faithful to the design documents and avoid showing mechanics, resources, rewards, or store claims that the game does not support.

## Files

- `WarlineCapture_Generative_Cinematic_Brief.md` - creative direction, constraints, and approval criteria.
- `WarlineCapture_Generative_Cinematic_Shots.json` - API-ready shot prompts, negative prompts, durations, and QA tags.
- `WarlineCapture_Generative_Cinematic_QA.md` - validation checklist for generated clips and assembled trailers.
- `Outputs/` - generated job plans, provider manifests, downloaded clips, QA reports, and assembled drafts.

## Runner

Dry-run the concept package from the repository root:

```bash
/Users/farhad/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3 Tools/Marketing/generate_concept_video_jobs.py --provider dry-run
```

When an API key is available, submit and poll OpenAI Sora jobs:

```bash
OPENAI_API_KEY=... /Users/farhad/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3 Tools/Marketing/generate_concept_video_jobs.py --provider openai-sora --submit --poll --download
```

The runner writes a manifest and QA report so the agent can verify that jobs finished, clips downloaded, and outputs are ready for human validation.

## Alignment Rules

- Do not use UI screenshots, gameplay target screens, or store mockups as video frames.
- Do not show readable UI text unless it is part of a final editable title card.
- Do not show deprecated or unapproved economy terms.
- Do not imply paid victory, sold mission stars, or direct Operation metric purchases.
- Use the concept language from `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`.
- Use resource and reward rules from `Design/WarlineCapture_Economy_Reward_Design.md`.
- Use monetization guardrails from `Design/Monetization/WarlineCapture_Monetization_Strategy.md`.
- Use visual tone from `Design/WarlineCapture_2D_Isometric_Production_Direction.md` and `Design/WarlineCapture_2D_Isometric_Art_Bible.md`.
