# SCN-05 Campaign Operations Screen Implementation Tracker

## Scope

Build the first production Canvas Campaign Map screen from the approved command-base visual language without recreating the removed Campaign runtime layer inside UI code.

## Progress

| Stage | Status | Progress | Acceptance |
|---|---|---:|---|
| 1. Audit existing Campaign/runtime state | Complete | 100% | Canonical docs, route inventory, Main Menu entry, and deleted runtime gap verified. |
| 2. Create visual target and production art | Complete | 100% | SCN-05 target lock, Sahrin map, and relay preview saved in the project. |
| 3. Build deterministic Canvas prefab | Complete | 100% | Builder creates the full 4800x2160 responsive body overlay from reusable sprites and separate art assets. |
| 4. Wire navigation | Complete | 100% | Campaign card opens `UIRoute.Campaign`; Back returns to Main Menu through ECS route history. |
| 5. Focused validation and visual QA | Complete | 100% | Functional validation passed; the rejected first visual handoff was replaced and inspected at 1920x1080 and 2400x1080. |

Overall: 100%

## Canonical Information Used

- Screen ID: `SCN-05 Campaign Map`.
- Product-facing mode name: `Campaign`.
- Chapter set: First Response, Broken Grid, Hidden Network, Air and Armor, Citywide Command.
- Chapter I mission count: five.
- Current Campaign product layer: not implemented in active runtime code.
- Historical Campaign controllers/progression were removed and must not be reintroduced as UI-local state.

## Implementation Contract

1. Append a Campaign route enum value without changing existing serialized enum values.
2. Add a dedicated campaign content prefab reference to `UIShellContentView` and `Menu.unity`.
3. Route `Card_Campaign` to the Campaign screen with history enabled.
4. Install the Campaign body in the existing shell popup/body overlay while preserving the shared header instance.
5. Keep all Campaign-only commands disabled until the future Campaign composition layer supplies contracts.
6. Validate that General Deploy remains a direct Match action and Skirmish still opens SCN-13.

## Out Of Scope

- Campaign progression persistence.
- Mission launch payloads and active mission sessions.
- Briefing, loadout, result, reward, story archive, and chapter-intel runtime systems.
- Android builds.
- Namespace or unrelated architecture changes.

## Validation Checklist

- [x] Campaign prefab exists and is assigned in `Menu.unity`.
- [x] Shared header remains installed and unchanged.
- [x] Main Menu Campaign opens SCN-05.
- [x] Back returns to Main Menu and consumes route history.
- [x] Skirmish route still opens SCN-13.
- [x] General Deploy still targets Match directly.
- [x] Locked chapters and unavailable Campaign actions are non-interactable.
- [x] No text or icon overlaps at 1920x1080 and 2400x1080 after the target-lock alignment revision.
- [x] Focused EditMode validation passes.
- [x] Replacement visual captures are inspected against the target lock.

## Validation Evidence

- `CampaignOperationsScreenTests.RunFocusedValidation`: passed, 4/4 after the alignment revision.
- `SkirmishSetupScreenTests.RunFocusedValidation`: passed.
- `UIShellCurrentContentLoadTests.RunFocusedValidation`: passed.
- Unity compilation completed without errors during focused validation.
- Final route capture inspected at `1920x1080`: `/private/tmp/warline-scn05-campaign-v4-1920x1080.png`.
- Final route capture inspected at `2400x1080`: `/private/tmp/warline-scn05-campaign-v4-2400x1080.png`.
- Campaign launch, Story Archive, and Chapter Intel remain visibly disabled because the active Campaign runtime/composition contracts do not exist yet.

## Target-Lock Alignment Revision

- The original 1920x1080 capture occurred before the popup scale/fade animation finished and was not valid visual evidence.
- The initial prefab also used a wide HUD rail sprite as a large panel frame, default Liberation Sans typography, fixed-height content, square mission nodes, and a fixed footer position. Those choices did not meet the SCN-05 target lock.
- Revision work replaces them with Oxanium typography, proper nine-sliced command-panel chrome, circular progression nodes, responsive vertical panel anchors, a bottom-anchored footer, stronger contrast, and time-based capture settling.
- The final layout fills the shell body at both target resolutions, preserves the shared header, keeps all text and icons inside their panel bounds, and leaves unavailable Campaign-only actions visibly disabled.
