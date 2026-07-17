# Static Map Presentation Manifest Schema 2

Date: 2026-07-17
Result: Passed

## Scope

- Advanced `StaticMapPresentationManifest` from schema 1 to schema 2.
- Added stable `operationMapId` and canonical scene GUID alongside the existing canonical scene path.
- Preserved schema-1 readability for rollback and provided an explicit editor migration overload.
- Rejected incomplete schema-2 identity in the runtime index, Android build resolver, and renderer ownership boundary.
- Kept chunk-content compatibility on its independent schema so a manifest metadata migration does not rewrite presentation scenes.

## Canonical Migration

- Operation map: `opmap.skirmish.desert_base_01`
- Scene GUID: `cc4f48a57793d4597b4ffac2906c515e`
- Scene path: `Assets/Game/Scenes/Match.unity`
- Presentation content hash remained `9eebc7c8aa774d5f505cb684099d133a`.
- Both accepted bakes reused all `514` chunk scenes with `0` writes and `0` deletes.
- The only generated diff is the manifest schema/map-id/scene-GUID identity; chunk scenes and the integrity ledger are byte-stable.

The first development bake exposed that manifest schema had been coupled to chunk-content reuse and attempted to rewrite all chunks. That output was rejected and restored. The final implementation separates those contracts and the accepted validation contains no chunk churn.

## Validation

- Affected EditMode suite: `111 / 111` passed.
- Explicit migration, legacy readability, incomplete-current-identity, runtime index, structural parity, ownership, streamer, and Android resolver coverage passed.
- Second schema-2 bake: `16,542` sources, `514` chunks, `0` writes, `0` deletes.
- Non-ECS naming architecture: `9 / 9` passed.
- Production source-growth architecture: `14 / 17` passed; three unrelated upstream failures remain in `MatchBootstrapCompositionSystemHelper.cs`, `ThreatDetectionWarningSystem.cs`, and `GameplayRuntimeUpdateCompositionSystemHelper.cs`. No file changed by this slice appears in the failure set.
- Unity compile and `git diff --check`: passed.

Logs:

- `/private/tmp/opmap-manifest-schema2-bake1-fixed.log`
- `/private/tmp/opmap-manifest-schema2-editmode.log`
- `/private/tmp/opmap-manifest-schema2-results.xml`
- `/private/tmp/opmap-manifest-schema2-bake2.log`
- `/private/tmp/opmap-manifest-schema2-architecture.log`
- `/private/tmp/opmap-manifest-schema2-architecture-results.xml`
