# Reward Unlock V3 — Iteration 2

Status: current review candidate; not user-accepted yet.

Runtime evidence:

- `reward_unlock_v3_16x9.png` — 1920×1080
- `reward_unlock_v3_20x9.png` — 4800×2160

Visible corrections from Iteration 1:

- The complete `RANGER SQUAD` title fits without truncation.
- The ranger artwork uses a square, aspect-preserved presentation so helmets,
  boots, and weapons are not cropped or stretched.
- The primary action uses a visible directional green gradient.
- Outer frame, reward cards, header cells, and action use one 3 px border
  contract without intersecting neighboring panels.
- Four reward cards remain aligned and legible at both required aspect ratios.

Layering and reuse:

- `POP04_RangerSquad_V3.png` is an isolated content illustration: soldiers and
  blueprint plate only. Text, frames, rewards, borders, and interaction states
  remain live Unity UI layers.
- Shared resource icons are referenced from the canonical V3 art library; the
  screen does not create screen-local duplicates of them.
- The full-screen environment is an aspect-fill scene plate and is not packed
  into a small UI atlas.

Validation markers:

- `[RewardUnlockV3PrefabBuilder] validation=Passed gradients=11 rewards=4 art=aspect-preserved action=1`
- `[RewardUnlockV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 ranger=aspect-preserved actions=1`

