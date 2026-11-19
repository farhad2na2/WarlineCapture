# Windows dev results — W30 S075–S078: stop offering the Airport and fixed-wing aircraft (2026-11-19)

Branch `missions/win-w30-ib-air-exclusions` (from integration `d1d4bad2d`). The Windows dev lead did this task. Read-only; no Unity run, no draft or code change.

**Outcome: stopped, as briefed.** S004 drops the Airport and fixed-wing aircraft in shared code, not data. Doing the same for S075–S078 needs a code change and the owner's OK.

## How S004 does it
- **The exclusion list.** `Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogFactory.cs:67-104`, `SkirmishMissionRoleExclusions`.
  - It hard-codes `S004 = { Fighter, Strike, TransportPlane }`.
  - `UnavailableRoleIds(catalogId)` returns that list for `"S004"` only, and an empty list for every other ID.
  - The class comment: "Kept in code, not on the definition asset, so the protected coded definition bytes stay exact. A producer structure (the Airport) leaves the mission catalog with its last role."
- **Consumers.**
  - `SkirmishSetupCompiler.cs:408` (`ApplyUnavailableRoles`, explained at :453) removes those roles, and with them the Airport, from the resolved setup.
  - `SkirmishResearchService.cs:485-489` caps research at `HighestUsefulReadiness`, so Full Arsenal is not sold when it only unlocks the withheld roles.
- **No data hook.** No draft, scenario-asset or InitialSetupMatrix field controls this. A draft-only change cannot reproduce it.

## What the change would be (not done; needs the owner's OK)
- Add `S075`–`S078` to `SkirmishMissionRoleExclusions.UnavailableRoleIds`, with the same three roles. This is a one-line pattern in shared Configs code.
- Re-author S075–S078 (the resolved setup changes), then run one S077 menu launch and check that `building.airport`, `role.fighter`, `role.strike` and `role.transport_plane` are gone from `unused=`.
- The current S077 run (`w29-s075-s078`) lists all four as unused.

## Open
- Owner's OK for the shared-code exclusion (S075–S078).
