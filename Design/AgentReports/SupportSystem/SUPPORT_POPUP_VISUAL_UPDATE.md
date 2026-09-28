# Support popup artwork and ARIA update

2026-09-28 follow-up to `a299f9c5f`, requested by the owner.

Four opaque landscape artworks were generated with the built-in ImageGen tool using the actual project grenade, jet, transport and crate images as references. Each original is 1989 × 790 (2.518:1); all are saved in `Assets/Game/Art/UI/Support/` as `support-{smoke,strike,paratroopers,supply}-wide-v1.png`. The exact prompts and reference paths are in `support_artwork_prompts_v1.json`. Original source icons remain available and the gameplay catalog still uses them.

The popup cards now fill their 480 × 190 artwork viewports without stretching or letterboxing. A clipped, centered aspect-preserving cover layout also fills the 392 × 218 detail preview. Subjects were composed to remain visible in that narrower crop. Titles, costs, charge counts and availability remain live localized UI text.

The bottom ARIA portrait/waiting/Play/Stop strip is replaced by one compact cyan `Stop ARIA` header area, using the same localized label and modal-stop treatment as Build. The ordinary battlefield Play control remains the entry point for Watch. The new header button stops Watch and declines pending Support consent; it never deploys an ability. A reachable Support stop suppresses the floating duplicate. The footer is now the instruction area and the existing next-action button.

Localization rebuild now replaces every managed Support-copy key, including the Smoke reward key, so repeated popup builds cannot duplicate it. Both English and Persian retain the original catalog entries and have 3275 unique matching keys after this update.

## Evidence

- `support-wide-native-popup-20260928-2116.log`: wrapper/receipt 0; 10 English/Persian native popup and lesson captures, no truncated text, all four card viewports geometrically covered. Captures are in `Evidence/NativePopup/WideArtworkV1/`; prior screenshots remain preserved.
- `support-wide-ui-aria-20260928-2118.log`: wrapper/receipt 0, all 7 consent/cancellation checks pass.
- `support-wide-config-20260928-2119.log`: wrapper/receipt 0, both configuration/layout checks pass.
- Normal-input updated-popup mission evidence remains pending. The additional `support-wide-enter-20260928-2127.log` retry timed out (wrapper 124, no completion receipt); its log reports compiler errors prevented Play mode. The shared Editor is reserved for the concurrent mission-economy validation work. This does not change the native visual and focused UI passes above.

An initial compiler error used a sprite importer property not available in this Editor version. The importer now follows the repository's existing `TextureImporterSettings` pattern; the final native captures and focused validations passed on the corrected code. No new Editor or project was opened.

The first Play transition lost its receipt across domain reload (wrapper 124), and the first mission dispatch returned Pipeline acknowledgement timeout 6 before a concurrent mission-economy compilation reset the runner. Both failures remain in Evidence with explicit status notes; no gameplay acceptance is inferred. The transition source now returns its own completed Task receipt, while the mission retains its full async completion receipt; the transition fix was not confirmed in a successfully completed retry. The macOS wrapper retains dispatch JSON and can continue only when the newly created log has the exact project/method ownership header; missing ownership, missing receipt, nonzero receipt or timeout still fail. The temporary automatic-refresh pause was restored after the retry. No second Editor or project was opened.
