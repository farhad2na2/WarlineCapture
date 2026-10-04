# Approved military UI materials

Owner approved the menu and match HUD material mockups on 2026-10-04 and requested implementation, prefab updates, commit, push and merge to main.

Five grayscale bitmap materials generated with the built-in ImageGen tool: fractured armored glass (red), ballistic woven composite (green), tactical etched glass (blue), anodized aircraft metal (gold), and ceramic armor (dark). Textures are in `Assets/Game/Resources/MilitaryUiMaterials`; source PNGs remain intact and native importers cap them at 512px, no mipmaps/CPU readback, ASTC 4x4 on Android and iOS. Existing gradient colors, icons, illustrations, typography, transforms and button listeners are retained.

The shared gradient renderer uses reusable materials, nine-slice edge detail (woven fibers repeat at a consistent 220 UI-unit scale) and the existing four border quads. Border UVs bypass material sampling, and the shader supports UI stencil masks, RectMask2D and alpha clipping. HUD role changes select their matching material; runtime ARIA controls use the same implementation. Clear illustration overlays, thin indicators and outlines are excluded. Main-menu/HUD builders retain material assignments when rebuilt.

## Validation

- Visual direction: owner approved `TextureMockups/menu-military-materials-v02.png` and `match-military-materials-v02.png`. Implementation reviewed from native English/Persian menu and match captures; existing illustration assets remain intact.
- Native authoring: `[MilitaryUiMaterials] result=Passed prefabs=8 surfaces=165 layout=Preserved input=Preserved textures=5 maxSize=512 mobile=ASTC4x4`. Shader compilation and import checks passed. Native serializer additions are default schema fields; original HUD Build lock and label alignment preserved.
- Native menu: final wrapper `/private/tmp/warline-premium-menu-native-03.log` passed; six actual menu screenshots, locales en/fa-IR, sizes 1920x1080, 1280x720 and 2400x1080. Captures and geometry findings in `NativeMenu/`.
- Native match: final wrapper `/private/tmp/warline-premium-hud-native-02.log` passed nine screens, locales en/fa-IR, sizes 1920x1080/1280x720, ARIA consent and command wheel, unobstructed touch targets and camera focus preserving troop orders. Final captures in `NativeMatch/`; first captures retained in `NativeMatchBeforeWeaveScale/`.
- Failed evidence retained: initial prefab snapshot failure from Unity unassigned sprite references, missing capture method before import, and Pipeline startup dispatch failures. Fixed before final checks; original validation logs remain in `Logs/`.
- Complete normal-input mission including ARIA, result and return: not established by this cosmetic screen review. Existing packed render database/readiness and virtualized building owner errors remain in match logs; this UI change does not claim to resolve them.
- Real player/device acceptance and Android/iOS shader/performance validation: pending. No device build or device acceptance claimed.
