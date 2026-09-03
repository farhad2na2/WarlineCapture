# V3 UI localization

## One catalog, every screen

The runtime source of truth is:

`Assets/Game/Resources/Localization/V3UiLocalizationCatalog.asset`

Each entry has a stable key. Every locale table uses the same keys and carries its own display
name, short Settings label, text direction, font, and translated values. Menu, match HUD, popups,
loading, first-launch, comic, briefing, pause, build, victory, and defeat UI resolve through this
catalog.

`V3LocalizedTextBinding` is stored on authored prefab text. A small runtime binder covers temporary
text created after a scene loads, such as match tutorial prompts, warnings, and build panels. Text
that is not in the catalog is not silently presented as translated. Configured format templates
also cover live values such as passenger capacity, resource exchange rates, mission statistics,
and timers. The binding remembers the English source even when a runtime view writes a localized
value, so switching languages does not strand dynamic UI in the previous language.

## Rebuild and validate

With the project already open in Unity and Pipeline connected, run:

```bash
unity command menu --project-path /Users/farhad/Projects/WarlineCapture \
  --path "Game/UI/V3/Localization/Rebuild Catalog And Bind All Screens"

unity command menu --project-path /Users/farhad/Projects/WarlineCapture \
  --path "Game/UI/V3/Localization/Validate Bindings And Coverage"
```

The rebuild command:

1. Imports the existing keyed English game strings.
2. Imports the reviewed English/Farsi match voice text.
3. Imports the reviewed first-launch and M02 narrative text.
4. Applies the project-owned Farsi UI glossary.
5. Adds stable bindings to every player-facing TMP text in all UI prefabs.
6. Registers code-driven match, tutorial, resource, and result templates.
7. Regenerates the central catalog.
8. Writes `Documentation/V3_UI_LOCALIZATION_MISSING_FA.md`.

Validation fails when any audited UI text has no binding, no English key, or no Farsi value. A
failure is intentional: missing localization must not be hidden behind an English fallback during
release QA.

## Adding another language

1. Add one `GameLocaleTable` to `V3UiLocalizationCatalog.asset`.
2. Give it a BCP-47-style locale code, display name, short label, direction, and optional font.
3. Copy the English key list and translate the values without changing the keys.
4. Open Settings. The language labels and selection indices are read from the catalog; no screen
   prefab or screen controller needs a language-specific edit.
5. Exercise the screen audit at both 16:9 and the project widescreen target. RTL languages also
   require overflow and alignment review.

Catalog rebuilds replace only the managed English and Farsi tables. Additional configured locale
tables are preserved.

## Authoring rules

- Never create a second localization catalog for a screen.
- Never branch a screen hierarchy by language.
- Use `GameText.Get(key, fallback)` for code-driven text.
- Use `V3LocalizedTextBinding` for authored TMP text.
- Reuse a key when the English source and meaning are the same.
- Use a new key when identical English text has a different meaning or grammar context.
- Keep player names, callsigns, IDs, file names, and resource numbers as data rather than UI copy.
- Run coverage validation before committing regenerated prefabs or locale data.
