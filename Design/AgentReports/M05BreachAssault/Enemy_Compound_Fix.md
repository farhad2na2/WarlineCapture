# M5: make the playable assault match the comic

## Problem

M5 reused M3's highway and placed a standalone road barrier in the traffic lane. Its transmitter and archive were not enclosed by an enemy base. The comic promised a breach into a defended relay compound, but the level did not provide that space.

## Implemented layout

- A mission-specific compound north of the highway, approximately x994–1054 / z444–476.
- Continuous four-metre concrete perimeter, two eight-metre guard posts, an archive building, and an eight-metre closed gate facing the approach.
- Transmitter and archive recovery area inside the perimeter. The post-breach movement destination is inside the entrance.
- Defenders and responding reinforcements start inside the compound; the friendly rifle squads and APC stage outside it.
- The archive requires a survivor within six metres for twenty seconds. Recovery cannot be completed from the highway.
- English and Farsi objective copy describes entering the base; existing approved spoken lesson bodies remain applicable.

## Implementation

`M05EnemyCompoundBuilder` generates prefabs and authored building placements from existing Polygon Military assets. The logical M5 map references this additional placement config; M1–M4 and the shared physical map do not receive these structures.

The existing building runtime registers the perimeter's navigation footprints. New placements explicitly use geometric footprint centres, avoiding the legacy baked placement convention's one-cell offset. The gate is a hostile runtime objective: destroying it removes its blocker through the normal building destruction path. Its prefab remains excluded from the player build catalogue. A narrowly scoped enemy-spawn request flag permits mission authoring to spawn such non-buildable prefabs.

## Validation

- Focused geometry checks: closed perimeter cannot be bypassed, gate opening has APC clearance, and approach/transmitter/archive anchors are inside.
- Live Editor check: actual registered building footprints and the gate's navigation cell seal the compound before combat.
- Real attack and move orders, without editing health, objectives or unit positions: gate breach, transmitter destruction, eight defenders defeated, archive recovery, rewards and campaign return.
- English first clear and fresh Farsi replay, including reset targets and roster.
- Visual inspection of the compound and corrected continuous wall faces. QA images remain outside Design.

Final status: **Passed in the isolated Unity Editor on 2026-09-15**.

- Geometry/configuration pass: `/private/tmp/m05-compound-navigation-final.log` (`[M05EnemyCompoundTests] result=Passed`).
- Clean final playthrough: `/private/tmp/m05-compound-clean-playthrough.log` (wrapper exit 0, live closed-gate check passed, English first-clear victory, Farsi replay victory, rewards and campaign return).
- Combat QA issues ordinary attack/move orders; it does not substitute for an end-to-end manual tutorial-button audit. The mission's changed movement destination and objective text were checked separately.
- Main-project runtime sources, map/scenario assets and compound prefabs were checked against the QA copy. No Android validation was requested.
- An intermediate run was invalidated by an Editor asset import during Play Mode. The final run above used unchanged assets throughout.
- The open main Editor did not acknowledge CLI refresh requests; the completed playthrough used the isolated Editor, with the same saved source/assets.

## Follow-up: gate pointer regression

The original entity-order playthrough missed gate-face screen picking. See [Gate_Screen_Targeting_Fix.md](Gate_Screen_Targeting_Fix.md) for the reproduced failure, correction, and English/Farsi screen-input regression results.
