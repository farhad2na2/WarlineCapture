# WarlineCapture Generative Cinematic Brief

Date: 2026-05-05

## Purpose

This brief defines an AI-generative marketing-video direction for WarlineCapture that is concept-driven rather than screenshot-driven. The output should feel like a high-end animated game trailer inspired by WarlineCapture's design docs, not a recording of actual gameplay or a literal copy of UI targets.

## Source Design Inputs

- `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`
- `Design/WarlineCapture_Gameplay_Features_High_Level_Spec.md`
- `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- `Design/WarlineCapture_Economy_Reward_Design.md`
- `Design/Monetization/WarlineCapture_Monetization_Strategy.md`
- Active 3D Unity scene direction and current `Design/VisualLock` references.

## Creative Positioning

WarlineCapture is a mobile-first tactical RTS about defending and stabilizing a damaged city. The cinematic should sell command pressure, district recovery, readable squad tactics, and strategic consequence.

The video can use a premium stylized 3D cinematic style, but it must keep the identity of a tactical RTS:

- Clear city blocks, roads, rooftops, barricades, convoys, drones, APCs, squads, and command markers.
- A commander perspective, not a superhero fantasy.
- Military-rescue tone, not horror, crime, or generic sci-fi destruction.
- Warm directional light, readable silhouettes, and controlled tactical color accents.
- Hopeful recovery beat by the end.

## Must Show

- A damaged near-future city district.
- Squad or convoy movement through streets.
- Tactical command planning or holographic district map.
- A district recovery action such as aid convoy, repair work, drone scan, patrol, or defensive stabilization.
- A final sense of victory through objectives and recovery.

## Must Not Show

- Actual WarlineCapture UI screenshots or visual-lock target screens as footage.
- Readable fake game UI, fake currencies, fake store labels, or unapproved resources.
- Any deprecated terms, including old token-style terminology.
- Paid victory, sold mission stars, direct district metric purchases, random loot, hidden odds, or casino-style presentation.
- Real-world brands, recognizable copyrighted characters, real people, or public figures.
- Graphic civilian harm or gore.

## Trailer Structure

Target first test: 5 clips, 8 seconds each, assembled into a 40 second rough trailer. The OpenAI video API supports fixed clip durations, so 8 seconds is the default WarlineCapture generative test length.

| Clip | Beat | Purpose |
|---|---|---|
| `GV-01` | City Crisis Establishing Shot | Establish the damaged city and command stakes. |
| `GV-02` | Commander War Room | Show tactical decision-making and district map pressure. |
| `GV-03` | Squad Advance | Sell RTS unit control and readable movement. |
| `GV-04` | Operation Recovery | Show aid, repair, and stabilization as the strategic layer. |
| `GV-05` | Victory Recovery | End on objective success and city recovery, not monetization. |

## Approval Criteria

- Looks like a premium animated game trailer inspired by WarlineCapture.
- Does not look like a literal recording of current gameplay or UI mockups.
- Each clip has clear subject, camera motion, and tactical action.
- The full trailer remains honest: "concept cinematic" or "marketing concept" labeling is required in internal review.
- QA report confirms duration, resolution, nonblank frames, downloaded files, and banned-term scan.
