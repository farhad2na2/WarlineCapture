# SCN-03 Commander Profile Layer Pack Prompt V01

Use the active `VisualLockLayered V15 3D Green-Screen Workflow`.

Surface id: `SCN-03_CommanderProfile`
Surface name: `Commander Profile`
Target reference: `reference/SCN-03_CommanderProfile_Landscape_Target.png`

Generate implementation source assets for a Unity Canvas build of SCN-03. Do not crop or cut the target reference into implementation layers.

Required source groups:

- `SCN-03_CommanderProfile_Background_21x9_NoUI.png`: opaque wide command-base background with no UI, logo, text, labels, or numbers.
- `SCN-03_CommanderProfile_ChromeSource_Green.png`: green-background sheet of blank frames, panels, buttons, tabs, reward nodes, meter frames, strips, and chips.
- `SCN-03_CommanderProfile_IconSource_Green.png`: green-background sheet of separate non-brand icons for commander rank, resources, navigation, profile actions, roster categories, rewards, history, and disabled states.
- `SCN-03_CommanderProfile_CommanderPortraitSource_Green.png`: green-background commander portrait/scan card source.

Live text and runtime values:

- Screen title, tab names, profile name/title, level, XP, resource labels and values, stat labels and values, reward node labels, recent history rows, button labels, route hint, and disabled reasons must be live UI text.
- Runtime data includes `PlayerProfileState`, `CommanderProgression`, `AccountStats`, `RewardTrackProgress`, `PlayerInventory`, `ResourceWalletState`, and route availability for `SCN-19 Armory`.

Layer rules:

- Frames contain no labels or counters.
- Buttons contain no text.
- Icons are separate from frames.
- Brand/logo art is a separate approved source asset, not part of this icon sheet.
- Progress bars use separate frame/fill assets.
- Reward nodes are separate visual states.
- Background art is the only opaque full-screen layer.
