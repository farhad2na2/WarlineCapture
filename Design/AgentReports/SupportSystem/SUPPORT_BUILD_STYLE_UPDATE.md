# Support popup Build-style update

2026-09-28. Owner requested that the resource strips, selected detail panel, header, Close, ARIA and action buttons follow the Build popup. The previously approved Support mockup and the existing `BuildDrawerV3PrefabBuilder` supply the visual direction; no new mission-screen direction is introduced.

The four generated wide artworks remain in place. Each card now has two clearly separated groups: Charges left, and Cost per use (one Support charge plus its Fuel cost). These use the existing parachute and orange Fuel icons with separate live numeric fields. A compact lock/reason strip appears only when unavailable; cooldown uses a localized countdown. Cards remain selectable for inspection while unavailable.

The detail panel uses Build's title/amber role hierarchy, covered preview, medium-weight description, divided icon/value rows and a separate availability/collection message. Available scoped Fuel is an icon/label/value header slot. The square X close control uses Build's exact 72 × 72 geometry; header, panel borders and footer use its typography, spacing and procedural gradients. One compact cyan Stop ARIA control remains in the modal header. The primary action uses Build's green gradient, footer location and generous touch size. Stop and Close retain cancellation behavior; selection does not spend resources. The normal-input probe follows the renamed CloseButton.

## Evidence

- Final native visual capture: `Evidence/support-build-style-native-20260928-final-01.log`, wrapper and receipt 0, 10 captures. All four artwork viewports cover their panels; card numbers and availability match the read model; English and Persian text is not truncated. Screenshots: `Evidence/NativePopup/BuildStyleV2/`. English default and Persian Paratroopers lesson screenshots were visually inspected.
- Configuration/responsive checks: `Evidence/support-build-style-config-final-02.log`, wrapper and receipt 0, 2 checks pass.
- ARIA/consent/cancellation checks: `Evidence/support-build-style-aria-final-01.log`, wrapper and receipt 0, all 7 checks pass.
- Updated full normal-input mission and real player/device acceptance remain pending, separate from these native visual fixtures and focused checks. Earlier implementation evidence belongs to its recorded candidate.

Shared Editor source imports interrupted three layout-check attempts before Support assertions ran (bridge busy refusal, Pipeline dispatch timeout, bridge busy refusal). Full failed logs and status notes are preserved. Automatic refresh was paused briefly for the successful focused checks and restored afterward. The same existing GUI Editor and repository wrapper were used throughout; no new Editor/project was opened.
