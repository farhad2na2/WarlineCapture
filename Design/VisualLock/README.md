# WarlineCapture VisualLock

Date: 2026-05-22

This folder is reset for the new full 3D single-map direction.

Use `Design/VisualLockLayered` as the primary active home for new screen targets, separated layers, manifests, and implementation-ready packs. Use this `VisualLock` folder only for temporary single-image drafts, quick review boards, or source references before a target graduates into a layered pack.

Rules:

- New targets must be mobile landscape and built for the command-base / 3D operation-map direction.
- Gameplay-facing images must show actual 3D operation-map context, runtime-style units/buildings, metadata-backed minimap/planning content, or config-backed roster content.
- Player-facing persistent economy text uses Credits and Command. Match economy text uses Materials, Fuel, and Oil. Campaign stars remain progression, not currency.
- Do not add old 2D/isometric, strategic/tactical split, or legacy Saga/Quick Custom visual targets here.

Legacy visual targets were moved to:

- `Design/Archive/LegacyVisualLock_2026-05-22/VisualLock/`
- `Design/Archive/LegacyVisualLock_2026-05-22/VisualLockLayered/`
