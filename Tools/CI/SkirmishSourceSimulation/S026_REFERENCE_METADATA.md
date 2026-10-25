# S026 compiler and metadata references

The checked-in `Fixtures/S026/ground-layout.v2.json` is the finite typed layout input used by source-config diagnostics and native reference capture. Its canonical V2 fingerprint is `c74d05162216ff09c5b953966a35e7d56bf9f1935a55cf7076d23fe23badc18e`; its exact JSON SHA256 is `669c8b20f55890b37657605f9c0d67087f6d4da672d2e0eae2ab9d78ca2ca14e`. It references the unchanged existing DenseCity Campaign source. It does not publish S026 or grant player readiness.

The `author` command in README.md accepts this layout with its actual source-config graph and exact BA/Ground/Established profiles. `ConfigurationCompiled` reports original compiler/capture consistency, with source fit, mission validation, World execution and native acceptance false. The ordinary S026 smoke retains a Blocked startup until its complete source-world admission is implemented.

Native metadata is captured once per changed consumed authoring domain through the repository wrappers. The macOS command below requires Unity Hub open; it captures source metadata only and creates no mission or player build:

```sh
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 600 --log /absolute/new/reference.log -- \
  -quit -executeMethod Game.Editor.SkirmishBakedActorSnapshotExporter.RunS026MetadataQualification \
  -skirmishS026Metadata /absolute/new/reference-output \
  -skirmishS026MetadataLayout /absolute/repository/Tools/CI/SkirmishSourceSimulation/Fixtures/S026/ground-layout.v2.json
```

Require wrapper exit zero, `[SkirmishS026Metadata] result=Passed contexts=12 negatives=8 scope=MetadataOnly`, the output artifact and unchanged input pins. S026's actual native catalog contains nine building types and sixteen unit types. Its recipes, delivery timing, production slots and unit ordering differ from S002; copying S002's eight/twelve references is rejected. The exporter records the actual selected domain and a fresh S002 comparison. It never transfers opening positions, storage or mission entities.

`RunCompiledSourceContextQualification` separately validates actual native Session/Match/Setup/Launch records. It requires `-skirmishCompiledContext /absolute/new/context-output` and the same layout argument, and the marker `[SkirmishCompiledContext] result=Passed contexts=12 negatives=120 S002=2 scope=IdentityOnly`. This records-only ECS test retains S026's actual Planned/-1 status and the original native playable-catalog gate. It does not establish a mission outcome, visual/input/save/return journey or device acceptance.

Compiler checks, physical authoring proofs, native reference captures, source outcomes and native player acceptance are distinct evidence. Route contact/transit values in the fixture remain authored/provisional. Earlier failed importer/domain/identity test evidence is retained separately; no pass marker or outcome is inferred from process exit alone.
