# Skirmish map title font — 2026-09-18

Fixed the map name below the setup header rendering as squares in Farsi. `QuickCustomScreenView.Bind` assigned translated characters directly to a Latin TMP label. It now uses the shared localization presentation binding with `ui.skirmish.base_assault_map`, which selects the locale font/material and shapes Farsi. The setup builder persists that same binding; legacy setup titles also use the shared text presentation path.

Validation: three focused checks passed, including a cold Farsi bind from a Latin label, glyph coverage, English/Farsi switching and regenerated prefab binding. The real setup screen was inspected in both languages. The Farsi title renders **پایگاه کویری** using NotoSansArabic-Narrative SDF. No translation string was changed.

- [Focused results](skirmish-title-tests.txt)
- [Live title readback](skirmish-title-live.txt)
- [Farsi capture](skirmish-title-fixed-fa.png)
- [English capture](skirmish-title-fixed-en.png)

Unity was opened through the repository wrapper; a pre-existing scene-backup prompt was accepted to preserve backups in Assets/_Recovery. Those backups and pre-existing crash artifacts were left untouched. QA used an isolated save and was restored to Edit mode afterward.
