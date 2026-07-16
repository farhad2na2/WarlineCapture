# Current Operation-Map Authored Behavior Validation

Date: 2026-07-16
Result: Passed
Scope: Loader-neutral current-map compatibility content

## Correction

The compact map-surface payload exposed one Faction 1 `Unit_Veh_Tank_USA`
placement whose authoritative 3x3 footprint overlapped two non-traversable
cells. Unity serialization moved only
`Map/Vehicles/MapVehicle_Tank_USA/SM_Veh_Tank_USA_01 (1)` one grid cell left:

- world position: `(841.3336, 1.0320015, 372.8972)` -> `(840.3336, 1.0320015, 372.8972)`;
- world center: `(842.4027, 2.4460812, 372.8972)` -> `(841.4027, 2.4460812, 372.8972)`.

The canonical and staged scenes were updated through Unity APIs. The canonical
and staged vehicle placement configs were regenerated and retain 29 entries.
The regression test now reads both legacy and compact map-surface encodings
through `MapSurfaceBlobAccess` and validates every authored USA tank footprint.

## Final Identities

| Asset | SHA-256 / identity |
|---|---|
| `Assets/Game/Scenes/Match.unity` | `182f3b4cb50f48e1a573e1e90ee0c13baf9d62fce46e35b1850ef72097db5d75` |
| `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity` | `10c1ea3fa662a787018a30e2b3ffc71abc83a00b8c0308bda9e6806ad18a9b15` |
| `Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset` | `4052c490406520d6959d1ac5b9c8490e6b77fb95d68fdbe817b5938e609b9b62` |
| `Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_VehiclePlacements.asset` | `f97406550584d043d316e11b352a029e6d6d2ba9732bf83a7232147ad3b1ea0f` |
| Compatibility definition | version `2`; SHA-256 `74dfa2ec58e8eed8a186ec246afdfdca3704f19b1c98ef116f44c081928d6fe4` |
| Staged definition | version `2`; SHA-256 `dc3f3c884f8de8db277e964d77e926ce2cc98c9175b7293543a71a6a429378da` |
| Static presentation manifest | SHA-256 `494e0052e1c55578238fd1200517999a437fb35465aac3eb295ec0c79e0cc715` |

The final manifest dependency hash is
`c6334797e2ba64aabd6cf41377674c88`. Presentation content remains
`9eebc7c8aa774d5f505cb684099d133a` with 514 chunks and 16,542 sources.
The final bake reused all content, wrote zero chunk scenes, and deleted zero
scenes. A subsequent definition rebuild was byte-identical.

## Validation

- Tank surface regression: `1 / 1` passed.
  - `/private/tmp/opmap-current-authored-tank-surface-fixed.log`
  - `/private/tmp/opmap-current-authored-tank-surface-fixed.xml`
- Building, vehicle, aircraft, runway, helipad, blocker, placement ownership,
  and definition checks: `102 / 103` passed in the combined batch.
  - `/private/tmp/opmap-current-authored-behavior-final-2.log`
  - `/private/tmp/opmap-current-authored-behavior-final-2.xml`
  - The only combined failure was the staging fixture's intentional closed-scene
    precondition after another fixture left `Match.unity` loaded.
- Staging validation in its required isolated editor state: `10 / 10` passed.
  - `/private/tmp/opmap-current-authored-stager-isolated.log`
  - `/private/tmp/opmap-current-authored-stager-isolated.xml`
- Static presentation structural and source-hiding validation: `2 / 2` passed.
  - `/private/tmp/opmap-current-authored-static-structural.log`
  - `/private/tmp/opmap-current-authored-static-structural.xml`
- Menu -> Match -> Menu lifecycle: `1 / 1` passed.
  - `/private/tmp/opmap-current-authored-lifecycle.log`
  - `/private/tmp/opmap-current-authored-lifecycle.xml`
- Final Android build-scene resolver: `23 / 23` passed.
  - `/private/tmp/opmap-current-authored-android-resolver-final.log`
  - `/private/tmp/opmap-current-authored-android-resolver-final.xml`
- Operation-map ECS, contract, architecture, and naming gates: `57 / 60` passed.
  - `/private/tmp/opmap-current-authored-architecture.log`
  - `/private/tmp/opmap-current-authored-architecture.xml`
  - The three failures are the previously recorded `RuntimeCity*` R&D
    source-growth authorization debt; this slice changes no runtime helper.
- Phase 0 ownership evidence was deterministically refreshed with the new
  direct scene/tracker hashes; status remains `NeedsDecision` with the same
  four historical decisions and report SHA-256
  `4109f4c9840c620975aad4e033b160464cbbd3fb2e8d97eebafed28bbb7f0b6a`.
  - `/private/tmp/opmap-current-authored-ownership-refresh.log`
  - Ownership shape/hash tests: `26 / 26` passed in
    `/private/tmp/opmap-current-authored-ownership-tests.xml`.
- Compiler errors: zero.
- Screenshots: none required; acceptance is deterministic scene/config,
  pathability, lifecycle, source-hiding, and build-resolution evidence.

This slice does not add scene loading/unloading, Addressables, generation,
remote content, a runtime update loop, or a staged-map bake.
