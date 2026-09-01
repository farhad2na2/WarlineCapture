# Main Menu V3 — Iteration 12

Target: `../../reference/SCN-02_MainMenuV3_Final_Target.png`

This iteration is the first candidate validated in the running Menu scene at
both required sizes:

- `main_menu_v3_runtime_16x9.png`: live 1920x1080 Game view
- `main_menu_v3_runtime_20x9.png`: live 4800x2160 Game view

Corrections included:

- commander/environment/tactical-table scene uses aspect-fill cover cropping,
  never non-uniform stretching
- header, left, middle, right, and footer sections reassert their authored
  1672x941 coordinates after shell-region mounting settles
- FIELD COMMANDER panel widened from 381 to 430 authored pixels while retaining
  its right edge; title, subtitle, and CHANGE control now keep explicit insets
- developer FPS overlay is opt-in and absent from player-facing review frames
- exact fixed-resolution Game-view presets and fit-to-window behavior are part
  of the repeatable QA command

Review status: candidate only; not accepted until the user explicitly confirms.
