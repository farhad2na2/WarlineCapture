# Comic portrait art contract

Current direction: requested by the user on 2026-09-28. This supersedes the earlier realistic and POLYGON portrait rendering directions for units, vehicles, buildings, and squad-selection artwork.

## Reference and rendering

Use the shipping campaign comic `Assets/Game/Resources/FutureMissionComics/CH04M02_SteelPush.png` as the visual authority. Portraits should look like close-up illustrations from that world: controlled ink contours, painted form shadows, readable human faces, convincing fabric and equipment, sand/olive/stone colors, warm highlights, and cool charcoal shadows. Avoid coarse triangular mesh shading and mannequin anatomy. Preserve each entity's existing equipment, color identity, silhouette, pose, and structural geometry.

## Runtime roles

- Primary: isolated subject, transparent background and corners, clean alpha edges, complete structural silhouette, no added environment or shadow.
- Card: calm identity/readiness scene with an opaque illustrated environment.
- Action: preserve the distinct existing operational activity and staging, with an opaque illustrated environment.
- Selection groups: preserve every represented category and the source subject count; keep category distinctions readable at thumbnail size.

All runtime files remain square 512×512 PNGs. Preserve existing paths and `.meta` files; historical words such as `Realistic`, `Polygon`, and `ChromaGreen` in filenames are compatibility names, not current art instructions. Do not bake text, UI frames, national flags, invented insignia, logos, or gameplay stats into artwork.

## Coverage and evidence

The runtime inventory contains 74 entities × three roles, 11 manual-selection fallbacks, and five squad-tray images: 238 total. `Design/AgentReports/ComicPortraitMigration/manifest.json` records exact paths, source hashes, metadata hashes, candidates, prompts, and application state. Candidates are produced with the built-in ImageGen tool. `Tools/PortraitMigration/comic_portraits.py` stages, checks, resizes, and applies them. The apply action requires complete generation and preserves concurrent work by checking the original image and metadata hashes.

Keep asset checks, visual review, native UI captures, normal-input gameplay, and real device/player acceptance separate. Generated artwork and technical import checks do not establish gameplay readiness.
