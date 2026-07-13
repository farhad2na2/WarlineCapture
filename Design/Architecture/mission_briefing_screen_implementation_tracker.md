# SCN-06 Mission Briefing Screen Implementation Tracker

## Scope

Build the first production Canvas Mission Briefing screen as the immediate continuation of SCN-05 Campaign Operations, without recreating the absent Campaign runtime layer inside UI code.

## Progress

| Stage | Status | Progress | Acceptance |
|---|---|---:|---|
| 1. Audit mission authorities and active runtime | Complete | 100% | SCN-06 archive, Campaign catalog, active routes, and missing launch contract verified. |
| 2. Lock current command-base visual target | Complete | 100% | New SCN-06 target preserves SCN-05 style and the legacy briefing information hierarchy. |
| 3. Add route and serialized screen contract | Complete | 100% | `UIRoute.MissionBriefing`, shell ownership, and the typed `MissionBriefingScreenView` surface are implemented without inventing launch behavior. |
| 4. Build deterministic Canvas prefab | Complete | 100% | The builder produces the responsive body overlay with live TMP text, reusable sprites, explicit serialized references, and no hierarchy-search API. |
| 5. Wire Campaign navigation | Complete | 100% | Mission 01 opens SCN-06; Back returns to SCN-05 and consumes nested route history. |
| 6. Focused validation and visual QA | Complete | 100% | Unity compile, Mission and Campaign focused suites, and 1920x1080 plus 2400x1080 routed captures pass. |

Overall: 100%

## Canonical Screen Content

- Screen: `SCN-06 Mission Briefing`.
- Chapter: `First Response`.
- Mission: `Mission 01`, presented as `Blackout at Sahrin` with operation codename `First Contact`.
- Location: `Old Market District`.
- Objectives: intercept the hostile patrol, secure the civilian route, preserve the starting squad.
- Conditions: civilian risk medium; visibility reduced.
- Enemy intel: Ash Line infantry moderate; light vehicles moderate; air threat low.
- Preview rewards: Commander XP +260, Credits +1,200, Intel +1.

## Validation Checklist

- [x] Mission Briefing prefab exists and is assigned in `Menu.unity`.
- [x] Campaign selected mission action opens `UIRoute.MissionBriefing`.
- [x] Back returns to Campaign and consumes route history.
- [x] Shared header remains installed and unchanged.
- [x] Deploy Operation is visible but non-interactable.
- [x] No default Skirmish launch component exists in SCN-06.
- [x] No text/icon overlap at 1920x1080 or 2400x1080.
- [x] Focused Mission Briefing validation passes.
- [x] Campaign focused validation still passes.

## Validation Evidence

- Prefab rebuild: `/private/tmp/warline-scn06-final-rebuild.log` (`result=Passed`).
- Mission focused suite: `/private/tmp/warline-scn06-final-focused.log` (`result=Passed tests=4`).
- Campaign regression suite: `/private/tmp/warline-scn05-after-scn06-final-focused.log` (`result=Passed tests=4`).
- Standard capture: `/private/tmp/warline-scn06-1920x1080-final.png`.
- Ultrawide capture: `/private/tmp/warline-scn06-2400x1080-final.png`.
- The global `Label_FPS` diagnostic remains visible in captures by design; the SCN-06 progress rail reserves space so the overlay does not cover mission content.

## Known Product Boundary

- `Deploy Operation` remains disabled because no authoritative Campaign mission-start payload, progression contract, or reward settlement contract exists. SCN-06 deliberately does not reuse the Skirmish startup path.

## Out Of Scope

- Mission launch payloads or active mission sessions.
- Campaign progression, persistence, rewards, and unlock evaluation.
- Android builds.
- Namespace or unrelated architecture work.
