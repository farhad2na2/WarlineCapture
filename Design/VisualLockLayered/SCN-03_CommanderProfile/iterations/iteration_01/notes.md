# Commander Profile V3 — Iteration 1

Status: review candidate; not accepted until explicit user confirmation.

Target lock:

- `../../reference/SCN-03_CommanderProfileV3_Final_Target.png`

Runtime evidence:

- `commander_profile_v3_16x9.png` — actual Play Mode capture at 1920×1080.
- `commander_profile_v3_20x9.png` — actual Play Mode capture at 4800×2160 after setting the Unity Game View to that exact aspect before capture.

Changes represented by this iteration:

- Rebuilt the final V3 composition instead of the obsolete ornate TargetLockV01 structure.
- Reused the canonical baked commander/environment scene and tightened its masked profile crop to match the character scale in the lock.
- Restored all five left tabs without clipping.
- Replaced unsupported font glyphs with procedural vector marks, eliminating square fallback glyphs.
- Uses visible directional gradients and a uniform 3 px border contract.
- Includes the clean header, reward track, recent history, statistics rail, and the full-width Back / Open Armory / Change Commander footer.
- Verified that panels do not overlap at either locked runtime aspect.

Known visual variance from the concept lock: some target-specific decorative icon silhouettes are represented by the canonical shared V3 icon set so the prefab does not introduce screen-local duplicate bitmaps.
