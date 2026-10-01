# Grounded Signal production content audit

Audit timestamp: 2026-10-02T00:27:34+02:00.

Scope: read-only source, generated-artifact and retained validation-evidence audit. This audit did not execute Unity or build/test a player. The separate root-owned English native normal-input result is recorded below.

## Passed checks

- Current map `opmap.ch04.grounded_signal` content hash matches the packed report: `70188ed3b2fd8c2819bd927810d084363615fe2795079620febdd42b4c28c810`.
- Entity scene GUID matches its saved scene metadata: `436aa193ca6964810ac18c51cf428952`.
- Unit fixture GUID matches its saved scene metadata: `15afd4ca691294d2babaf59732227abb`; fixture registry contains 14 unique scenario prefab references, including Pilot, Bombsuit, transport plane and Heavy APC.
- Runtime binding GUID matches its saved scene metadata: `1df7b83e8dbcb483fb195a45fa9403c7`. The binding uses a disabled, unbound SubScene placeholder; the explicit loader owns its independent entity scene.
- Saved entity scene contains 11,117 `DenseCityPresentationIdentityAuthoring` components with 11,117 unique stable IDs. Additional occurrences of building stable-ID fields belong to the corresponding building authoring and are not duplicate identity components.
- Both packed catalogs exist and their actual SHA-256 values match `packed-content.json`:

| Catalog | Actual SHA-256 |
|---|---|
| Entities: `Library/GroundedSignalPreparedContent/Entities/ContentArchives/archive_dependencies.bin` | `6028485d969ec276f7f353ff8ce5f5e4f30e8a7e9bd63d79346a5219e748a54e` |
| Addressables: `Library/GroundedSignalPreparedContent/Addressables/catalog.bin` | `0d30b13ae0bd9cf09ed194a0c570f1cb41ab2fdf81e0cc554e09cd4ab7afad24` |

- Native isolated content build report: `Passed`, target `StandaloneOSX`, entity content size 168,630,435 bytes. This is content-build evidence.
- Production Addressables registration reports seven own assets registered with GUID lookup; existing entries are preserved.
- `OperationMapEntitySceneBuildAdditions` includes the independent map and mandatory unit-fixture GUIDs in normal production Entities builds. Invalid map identity or a missing fixture fails the build. Explicit candidate scene overrides retain their isolated scene set.

No current GUID, catalog-hash, generated-identity or source-delivery mismatch was found.

## Native English mission evidence

Evidence review updated: 2026-10-02T01:00:55+02:00.

[English normal-input result](Evidence/20261001-224822-en-manual/result.txt) records:

```text
[GroundedSignalInput] result=Passed normalInput=Passed dialogue=8 voices=8 result=Passed settlement=Passed return=Passed route=RunwayUnloadAPC humanDeviceAcceptance=pending
```

The root-owned native English playthrough passed the runway unload, relay/hardware recovery and APC extraction path, eight comic dialogue/voice bindings, victory, settlement and Campaign return. Retained captures and the Editor log are in the same evidence directory. This establishes that native English mission lane; it does not establish a standalone production player build, human listening review or device acceptance.

## Native Persian and ARIA mission evidence

Final evidence review: 2026-10-02T01:39:33+02:00.

[Persian + ARIA normal-input result](Evidence/20261001-232658-fa-IR-aria/result.txt) records:

```text
[GroundedSignalInput] result=Passed normalInput=Passed dialogue=8 voices=8 result=Passed settlement=Passed return=Passed route=RunwayUnloadAPC humanDeviceAcceptance=pending
```

The combined Persian + ARIA native lane passed the full ordinary mission, all eight complete Persian voices, real victory at 387,663 ms, a 6,004 ms secure extraction hold, settlement and Campaign return. The approved wrapper's [Persian + ARIA log](Evidence/persian-aria-wrapper-02.log) records receipt 0 and process exit 0. ARIA was validated in this completed Persian lane; a separate English ARIA rerun is not an outstanding readiness gate. Earlier failed English/ARIA attempts remain retained and do not replace this completed evidence.

The final [packed-content wrapper log](Evidence/packed-wrapper.log) also passed with receipt 0. The catalog SHA-256 values and content size above were refreshed against the final files and `packed-content.json`. These native and content-build passes do not establish a standalone normal production player build, human listening acceptance or device acceptance.

## Pending production and acceptance gates

1. Run the ordinary production player build through the approved repository wrapper. Confirm the normal Entities build includes Grounded Signal's map and unit fixture, and that its shipped runtime catalog resolves both GUIDs. The isolated Library content package does not establish this result.
2. Launch that built player, load Grounded Signal through the ordinary Campaign menu and confirm map, grid, surface, unit prefabs, comics and English/Persian audio resolve from shipped content. Confirm a single surface owner, normal mission/result/return and retry loading.
3. Qualify each intended device/build target separately. The existing isolated `StandaloneOSX` content build does not prove Android/iOS packaging, performance, input or playback.
4. Obtain real player/device acceptance after those checks. No human listening or device acceptance is claimed here.

Retain failed probe/build evidence and report these gates separately from compilation, visual review and content-build checks.
