import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
OBSERVER = ROOT / "Assets/Game/Scripts/Systems/S073NativeProductionDeliveryObserver.cs"
SEARCH_OWNER = ROOT / "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs"
LAUNCH_OWNER = ROOT / "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs"
TICK_OWNER = ROOT / "Assets/Game/Scripts/Systems/BuildingProductionTickCompositionSystemHelper.cs"
RNG_FIELD_OWNER = ROOT / "Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs"
PREPARE_HELI = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/prepare_heli.py"
GEOMETRY_DIAGNOSTIC = ROOT / "Assets/Game/Scripts/Editor/Skirmish/S073AircraftGeometryDependencyDiagnostic.cs"


class S073NativeDeliveryLaunchInputContractTests(unittest.TestCase):
    def test_static_bounds_observation_is_bound_to_provider_and_grid_revision(self):
        observer = OBSERVER.read_text(encoding="utf-8")
        owner = SEARCH_OWNER.read_text(encoding="utf-8")

        for field in (
            "static_bounds_provider_type",
            "static_bounds_provider_identity",
            "static_bounds_provider_reference_stable",
            "static_bounds_grid_entity_index",
            "static_bounds_grid_entity_version",
            "static_bounds_blocker_storage_epoch",
            "static_bounds_blocker_applied_storage_epoch",
            "static_bounds_blocker_applied_revision",
        ):
            self.assertIn(field, observer)
        self.assertIn("provider.GetType().AssemblyQualifiedName", observer)
        self.assertIn("ReferenceEquals(provider, Game.Rendering.Contracts.RenderBoundsGateway.Provider)", observer)
        self.assertIn("if (!enabled) return", observer)
        self.assertIn("catch (Exception error) { RecordObserverError(\"StaticBounds\", error); }", observer)
        self.assertLess(owner.index("provider.CollectStaticBounds("), owner.index("ObserveStaticRenderBounds("))
        self.assertIn("blockerData.AppliedRevision", observer)

    def test_static_bounds_capture_is_bounded_session_scoped_and_accounted(self):
        observer = OBSERVER.read_text(encoding="utf-8")
        window = (ROOT / "Assets/Game/Scripts/Editor/Skirmish/S073NativeProductionDeliveryWindow.cs").read_text(encoding="utf-8")
        finalizer = (ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/finalize_s073_native_delivery_launch_capture.py").read_text(encoding="utf-8")

        self.assertIn("MaxStaticBoundsObservationsPerSession = 32", observer)
        self.assertIn("staticBoundsSession", observer)
        self.assertIn("staticBoundsObservationCount >= MaxStaticBoundsObservationsPerSession", observer)
        self.assertIn("staticBoundsSkippedCount++", observer)
        self.assertIn("if (Write(observation)) staticBoundsObservationCount++", observer)
        self.assertLess(observer.index("if (staticBoundsObservationCount >= MaxStaticBoundsObservationsPerSession)"),
                        observer.index("var observation = new TickSnapshot", observer.index("ObserveStaticRenderBounds")))
        self.assertIn("static_bounds_observation_count", window)
        self.assertIn("static_bounds_skipped_overflow_count", window)
        self.assertIn("StaticBoundsObservationCountFor(expectedSession)", window)
        self.assertIn("StaticBoundsSkippedCountFor(expectedSession)", window)
        self.assertIn("launch_time == bounds[\"unity_time\"]", finalizer)
        self.assertIn("ambiguous_pair", finalizer)
        self.assertIn("static_bounds_capture_accounting", finalizer)

    def test_launch_observation_captures_entry_rng_camera_and_same_frame(self):
        observer = OBSERVER.read_text(encoding="utf-8")
        owner = LAUNCH_OWNER.read_text(encoding="utf-8")
        tick_owner = TICK_OWNER.read_text(encoding="utf-8")
        rng_field_owner = RNG_FIELD_OWNER.read_text(encoding="utf-8")

        for field in (
            "launch_random_state_before",
            "launch_random_state_after",
            "launch_random_state_observed",
            "launch_frame",
            "launch_camera_present",
            "launch_camera_position",
            "launch_camera_forward",
            "launch_camera_right",
        ):
            self.assertIn(field, observer)
        self.assertIn("CaptureHelicopterLaunchInputs(context.WorldCamera, randomState)", owner)
        self.assertIn("camera.transform.forward", observer)
        self.assertIn("camera.transform.right", observer)
        self.assertIn("random_state_observed = false", observer)
        for field in (
            "random_state_source_hooks_available",
            "random_state_getter_before_observed",
            "random_state_getter_before_matches_tick_input",
            "random_state_getter_after_observed",
            "random_state_getter_after_matches_tick_output",
            "random_state_getter_verification_passed",
        ):
            self.assertIn(field, observer)
        self.assertIn("current.random_state_observed = current.random_state_getter_verification_passed", observer)
        self.assertIn("currentRandomStateGetter = context.GetRandomState", observer)
        self.assertIn("getterBeforeMatchesTickInput = getterBefore == randomState", observer)
        self.assertIn("getterAfterMatchesTickOutput = getterAfter == randomState", observer)
        self.assertIn("() => source.BuildingSpawnRandomState", tick_owner)
        self.assertIn("value => source.BuildingSpawnRandomState = value", tick_owner)
        self.assertIn("internal uint BuildingSpawnRandomState", rng_field_owner)
        self.assertIn("launch_random_state_local_value_observed", observer)
        self.assertIn("launch_random_state_source_hooks_available", observer)

    def test_source_extraction_replacement_tracks_exact_native_call_once(self):
        recipe = PREPARE_HELI.read_text(encoding="utf-8")
        self.assertIn(
            "static_bounds_call='ReserveStaticRenderDropBuffers(em, ref reserved, gridEntity, grid, blockerData, true, preferredCell);'",
            recipe,
        )
        self.assertIn("if b.count(static_bounds_call)!=1:raise ValueError", recipe)
        self.assertIn("if(reserveStatic!=null)reserveStatic(owner.World,preferredCell,ref reserved);", recipe)

    def test_geometry_dependency_diagnostic_is_read_only_and_reports_exact_source_and_draw_diffs(self):
        diagnostic = GEOMETRY_DIAGNOSTIC.read_text(encoding="utf-8")
        for field in (
            "RunInstalledVsMeasuredDiagnostic",
            "SkirmishAircraftRotorGeometryAuthoring.Measure",
            "SkirmishCanonicalAircraftGeometryAuthoring.Measure",
            "DependencyHash",
            "source_path",
            "dependency_hash",
            "GeometrySha256",
            "LocalBoundsCenter",
            "DrawnIndexCount",
            "MeshToBlade",
            "BladeToVisualRoot",
            "[S073AircraftGeometryDiagnostic] result=Completed mismatch_count=",
            "[S073AircraftGeometryDiagnostic] result=Failed error=",
            "FileMode.CreateNew",
            "path.StartsWith(\"/private/tmp/\"",
            "EditorApplication.Exit(exitCode)",
        ):
            self.assertIn(field, diagnostic)
        for mutation in ("AssetDatabase.CreateAsset", "AssetDatabase.SaveAssets",
                         "AssetDatabase.SaveAssetIfDirty", "EditorUtility.SetDirty",
                         "EditorApplication.isPlaying = true"):
            self.assertNotIn(mutation, diagnostic)


if __name__ == "__main__":
    unittest.main()
