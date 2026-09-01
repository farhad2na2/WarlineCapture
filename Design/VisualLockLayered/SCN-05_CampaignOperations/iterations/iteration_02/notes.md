# Campaign Operations V3 — Iteration 02

Status: review candidate only; not accepted.

Change from Iteration 01:

- ARIA's 893×1236 portrait now preserves its source aspect ratio in the large ARIA Protocol panel.
- The smaller ARIA mission-card portrait remains crop-safe through its `AspectRatioFitter`.
- Prefab validation now rejects every ARIA portrait that has neither `Image.preserveAspect` nor an `AspectRatioFitter`.

Runtime validation:

- Chapter Select recaptured at 1920×1080.
- Chapter Select recaptured with the actual Unity Game View set to 4800×2160.
- Mission Select is unchanged from Iteration 01 and is carried forward for a complete review set.
