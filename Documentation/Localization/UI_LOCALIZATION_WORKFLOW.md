# UI localization workflow

The shared runtime catalog is `Assets/Game/Resources/Localization/V3UiLocalizationCatalog.asset`. Screens resolve it through `IUiLocalization` / `UiShellRuntimeGateway.Localization`; gameplay and ECS status data retain their original identifiers.

## Authoring and adding languages

1. Add UI text to JSON under `Assets/Game/Configs/Localization/`. Each entry has a stable `key`, an English `source`, and a `translations` array of `{ "locale": "fa-IR", "value": "…" }` records. `StartThroughM02UiStrings.json` owns the September 2026 audit fixes. These values override legacy generated source aliases during catalog regeneration.
2. Add every translation for every published locale. To publish another language, add its metadata/font and complete key table to the shared catalog using Unity, and add that locale's translations to each JSON entry. A new locale does not require a screen-specific enum or a Farsi/English branch. Do not copy English into missing translations to satisfy validation.
3. Use stable keys with `Get` / `Format` for authored messages. Translate catalog item names with `UiLocalizedText.CatalogLabel` before passing them into a translated template. Preserve user-entered names, callsigns, mission IDs, coordinates, quantities, and agreed acronyms.
4. Write dynamic TMP labels through `UiLocalizedText.Set`, which updates the shared binding immediately and applies the locale font, direction and sizing. Direct `.text` writes can bypass or overwrite a serialized binding. Keep raw ECS loading/status strings compact; localize them at the screen boundary.
5. First-launch identity/guidance labels have explicit narrative target/key/fallback arrays. `ExtendConfiguredNarrativeBindings` extends those arrays from the same JSON config; the first-launch prefab builder calls it too. Dialogue, player input and the CC icon have separate ownership or are invariant.
6. In Unity, run **Game → UI → V3 → Localization → Apply Configured UI Strings**, then **Validate Bindings And Coverage**. Full catalog regeneration also imports the JSON. Do not hand-edit generated prefab/catalog YAML.
7. Run `StartThroughM02LocalizationTests.RunFocusedValidation` through the repository Unity wrapper, followed by rendered and interactive checks for changed screens.

The build preprocessor rejects incomplete locale tables, unknown or duplicate keys, mismatched format argument indexes, stale/unimported config values, and missing first-launch static bindings. Existing V3 validation also checks prefab bindings and Farsi font coverage. These checks catch missing data and wiring; they do not replace human review of translation quality, text embedded in images, layout, or newly introduced runtime strings.

## macOS validation

Keep Unity Hub open. Use an isolated QA project if the main Editor owns this project:

```sh
Tools/CI/invoke_unity_macos.sh --project /private/tmp/warline-campaign-return-qa \
  --timeout 180 --log /private/tmp/warline-localization-tests.log -- \
  -quit -executeMethod StartThroughM02LocalizationTests.RunFocusedValidation
```

Require `[StartM02Localization] result=Passed`; exit code alone is insufficient because GUI Unity may exit zero after an execute-method exception. Never add `-batchmode` on this machine. Keep screenshots outside the repository.
