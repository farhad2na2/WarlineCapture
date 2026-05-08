Lane:
FTUE / ARIA UI design targets

Task:
Rebuilt the six FTUE / ARIA mockup targets as high-quality focused panel/popup references over blurred WarlineCapture UI backgrounds, with no new slicing/layer-pack requirement.

Files changed:
Tools/UI/generate_ftue_assistant_visual_targets.py
Design/VisualLock/PREFAB-04_AssistantButton/*
Design/VisualLock/PREFAB-05_AssistantPanel/*
Design/VisualLock/PREFAB-06_TutorialCard/*
Design/VisualLock/PREFAB-07_TutorialHighlight/*
Design/VisualLock/POP-10_AssistantTakeover/*
Design/VisualLock/POP-11_CommanderIdentity/*
Design/VisualLockLayered/PREFAB-04_AssistantButton/*
Design/VisualLockLayered/PREFAB-05_AssistantPanel/*
Design/VisualLockLayered/PREFAB-06_TutorialCard/*
Design/VisualLockLayered/PREFAB-07_TutorialHighlight/*
Design/VisualLockLayered/POP-10_AssistantTakeover/*
Design/VisualLockLayered/POP-11_CommanderIdentity/*
Design/VisualLockLayered/README.md

Contracts touched:
VisualLock target manifests now use workflow `flat-panel-popup-target-over-blurred-warlinecapture-context` and `requiresSeparatedLayerPack: false`.
Flat reference prompts now use `prompts/flat_panel_popup_target.md`.

User-visible behavior:
The targets now show polished ARIA/tutorial/commander panels and popups over blurred WarlineCapture UI context instead of pretending to be full gameplay screens. Labels are readable, and the background is clearly only game UI context.

Validation run:
Regenerated targets with `python3 Tools/UI/generate_ftue_assistant_visual_targets.py`.
Checked PNG dimensions and reference/state-plate byte matches.
Ran `git diff --check`.
Visually inspected the contact sheet plus full Assistant Panel and Commander Identity targets.

Validation result:
Passed. All six VisualLock targets, layered references, and state reference plates are `1672x941`; reference copies match target PNGs; whitespace check passed.

Known gaps:
These are flat visual targets only. Runtime prefab implementation and final ARIA/commander portrait art are still future work.

Cross-lane impacts:
No runtime Unity prefab/code behavior changed. Documentation now marks these as flat panel/popup targets, not sliced layer-pack deliverables.

Next recommended task:
Pick one surface, probably `PREFAB-05_AssistantPanel`, and implement the actual Unity Canvas prefab using live TMP labels and existing WarlineCapture UI chrome conventions.
