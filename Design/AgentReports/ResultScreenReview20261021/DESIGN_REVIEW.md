# Campaign, Skirmish and Operations result design review

Design proposals only. The shipped result UI has not been changed. The built-in ImageGen tool produced these boards from the actual native UI, existing Campaign art and the current HUD/menu design language. Each board shows victory above defeat; values are representative layout examples, not new gameplay rewards or acceptance evidence.

| Mode | Final proposed victory and defeat | Mode-specific content |
| --- | --- | --- |
| Campaign | [Campaign v02](Proposals/campaign-v02.png) | Existing mission comic, authored aftermath, clear rewards and mandatory debrief route |
| Skirmish | [Skirmish v02](Proposals/skirmish-v02.png) | Existing mission thumbnail, real objective facts, first-award medal and Battlefield return |
| Operations | [Operations v03](Proposals/operations-v03.png) | Mission thumbnail, signal/evidence/extraction facts and District Map return |

[Native-versus-proposal gallery](REVIEW.html) · [Exact prompts](PROMPTS.json) · [Capture manifest](MANIFEST.json)

## What the current native screens show

Fresh native inspection renders use the current Campaign prefab and the same result-card runtime class and mode projection methods used by Skirmish and Operations. Six representative models cover victory/defeat in all modes. These are view fixtures, not recorded gameplay victories or losses. A separate recent M01 victory from the complete normal-input playthrough is retained as the live Campaign reference.

- Campaign has a full-screen authored result prefab; Skirmish and Operations mount a separately constructed result card.
- The runtime card's emblem is created green and does not change on defeat. Its objective section heading also remains green, and unearned star outlines become red.
- Retry and Replay are both visible on either outcome. The repeated restart actions compete with returning to the mission map.
- The runtime card leaves a large empty band above the footer; mission imagery is absent. Campaign's imagery is richer, but the current board gives much of its area to exposed background.
- The generic Campaign fallback derives some objective row states and the failure reason from the overall win/loss flag. Mission-specific overrides already exist; those must be preserved and replaced only with authoritative projected facts.
- Save/settlement states and mode-specific route permissions differ; visual consistency must not flatten those rules.

Source: MissionResultPopupView.cs; CampaignStyleResultCard.cs; SkirmishMatchView.cs and its PublicObjectiveHud partial; OperationsMissionScreenView.cs.

## Shared proposed design

Use the same result header, duration chip, compact star row, three-column body and footer rules in every mode. Left: modest mission art and concise aftermath. Middle: large objective rows. Right: performance and the appropriate reward/medal/report module.

Use the existing squared typography and navy military materials. White text is primary, cyan is information/navigation, green is completion or a positive gameplay action, red identifies failure, and amber means incomplete. Gold is reserved for actually earned stars, medals and authored rewards. Defeat uses a failure emblem and neutral unearned stars; completed objective rows retain their completion state.

Keep separate frames with a consistent 16–24 logical px gutter, 1–2 px steel strokes and at least 24 logical px text padding. Preserve right padding for Persian, mirror textual alignment without reversing numeric fractions or durations, and keep icons and text in separate rectangles. Buttons share heights and icon alignment. The dominant next action stays on the right; reverse consistently for the final RTL layout if the established locale policy requires it. The full frame stays within the device safe area.

Do not shrink text to fit longer mission descriptions: use the mission's concise authored summary and a scrollable objective area when necessary. The three sample rows do not define a new objective-count limit.

## Routing and data constraints for implementation

- Campaign: Continue must still enter any required debrief or first-launch reveal. Chapters/Replay are only exposed when the existing flow permits them; mockup secondary buttons are not permission to skip mandatory story progress. Use each mission's existing comic asset or actual preview fallback.
- Skirmish: use the applicable mission projection, including public-goal objectives. Never hardcode the base-assault checklist for every mission. Show a medal grant only for an actual first award; repeats say no new medal as appropriate. No new XP or currency rewards are proposed.
- Operations: the O001 sample uses the currently exposed evidence/extraction facts. Other Operations outcomes may have their own authored district deltas; display only available authoritative data. Do not invent Trust, Heat or credit grants to fill a panel. Retry/practice/replay appear only where the operation allows them.
- A victory/defeat art treatment cannot certify the mission. Preserve partial/withdrawn/resolved distinctions and all settlement/save failures. Save confirmation appears only after confirmed persistence; failed saving gets its actual recovery action.

## Review and validation status

- Current native view inspection: captured six views, checked wrapper exit 0 and required capture marker.
- Proposed design visual QA: inspected all six outcomes; corrected failed-row status symbols and Operations proportions/primary-action placement. Earlier generated variants are retained.
- Implementation, final native layout/RTL checks, normal-input result/return coverage and player/device acceptance: pending visual-direction approval and implementation. No runtime UI assets or gameplay were changed in this design task.
