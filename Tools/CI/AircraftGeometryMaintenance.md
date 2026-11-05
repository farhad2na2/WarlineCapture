# Aircraft geometry dependency maintenance

Aircraft resources record Unity's source dependency hashes. Source script changes
and line-ending normalization can invalidate these stamps while the measured
geometry stays unchanged. Play/build guards deliberately reject stale resources.

Use this explicit preparation at a stable source checkpoint. Keep Unity Hub open.
Choose an output directory outside the repository for diagnostic reports and
backups, then run from the project root:

```sh
task_out=$(mktemp -d /private/tmp/warline-aircraft-maintenance.XXXXXX)
rtk proxy env WARLINE_AIRCRAFT_MAINTENANCE_OUTPUT="$task_out" \
  Tools/CI/invoke_unity_macos.sh --timeout 360 --log "$task_out/diagnosis-editor.log" -- \
  --burst-force-sync-compilation -quit \
  -executeMethod Game.Editor.AircraftGeometryDependencyMaintenance.Diagnose
```

Check the log for `[AircraftGeometryDependencyDiagnosis] result=Collected` and
inspect `diagnosis.json`. All three `equalExceptDependencyHashes` values must be
true. Object references use persistent asset GUIDs and local file IDs so the
diagnosis can be checked in another Editor session. The source/importer report
explicitly records that it did not create or observe a native gameplay World.

After reviewing the diagnosed changes, use the emitted byte-pinned manifest:

```sh
rtk proxy env WARLINE_AIRCRAFT_MAINTENANCE_OUTPUT="$task_out" \
  WARLINE_AIRCRAFT_MAINTENANCE_MANIFEST="$task_out/maintenance-manifest.json" \
  Tools/CI/invoke_unity_macos.sh --timeout 360 --log "$task_out/refresh-editor.log" -- \
  --burst-force-sync-compilation -quit \
  -executeMethod Game.Tests.Editor.AircraftGeometryDependencyMaintenanceTests.RunRefreshAndVerify
```

Require `[AircraftGeometryMaintenanceIntegration] result=Passed` and a successful
wrapper exit. The maintenance preflights every resource, rejects changed geometry,
source identities, transforms or provenance, preserves original resource/meta
bytes in a backup, updates only dependency stamps, and runs the original strict
verifiers. It refuses an already frozen runtime lookup or a source change since
diagnosis. Review and commit the three resource diffs; keep bulky reports/backups
outside Git. A changed geometric fact requires its own authoritative preparation.

The synchronous compilation flag keeps Burst enabled and completes Editor
compilation during validation setup. Runtime jobs and scheduling are unchanged.
This preparation preserves historical native provenance; fresh actor, menu,
mission, camera and device acceptance remain separate checks.
