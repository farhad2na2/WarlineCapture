#!/usr/bin/env python3

from __future__ import annotations

import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
THREAT_STATE = ROOT / "Assets/Game/Scripts/Systems/ThreatWarningRuntimeState.cs"
THREAT_SYSTEM = ROOT / "Assets/Game/Scripts/Systems/ThreatDetectionWarningSystem.cs"
GAMEPLAY_UPDATE = ROOT / "Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs"
THREAT_PRESENTATION_STATE = ROOT / "Assets/Game/Scripts/Systems/ThreatWarningPresentationState.cs"
MATCH_INTRO_QUERY = ROOT / "Assets/Game/Scripts/Composition/MatchIntroEcsStateQuery.cs"
MATCH_BOOTSTRAP = ROOT / "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs"
MATCH_SCENE = ROOT / "Assets/Game/Scripts/Composition/MatchSceneView.cs"
BEHAVIOR_TESTS = ROOT / "Assets/Tests/Editor/ThreatWarningValidationTests.cs"
PERFORMANCE_TESTS = ROOT / "Assets/Tests/Editor/WorldScopedComponentQueryCachePerformanceValidation.cs"


class ArchitectureWorldOwnedRuntimeStateTests(unittest.TestCase):
    def test_threat_warning_mailbox_is_world_owned_and_explicit(self) -> None:
        state = THREAT_STATE.read_text(encoding="utf-8")
        self.assertIn("ThreatWarningRuntimeStateComponent : IComponentData", state)
        self.assertIn("EntityManager entityManager", state)
        self.assertIn("EntityQuery query", state)
        self.assertNotIn("World.DefaultGameObjectInjectionWorld", state)
        for forbidden in (
            "public static bool HasPendingWarning",
            "public static ThreatWarningType PendingType",
            "public static float PendingEtaSeconds",
            "public static int PendingThreatCount",
        ):
            self.assertNotIn(forbidden, state)

    def test_unmanaged_detection_system_owns_singleton_creation_and_writes(self) -> None:
        system = THREAT_SYSTEM.read_text(encoding="utf-8")
        self.assertIn("partial struct ThreatDetectionWarningSystem : ISystem", system)
        self.assertNotIn("SystemBase", system)
        self.assertIn("EntityQuery _warningStateQuery", system)
        self.assertIn("ThreatWarningRuntimeState.EnsureSingleton", system)
        self.assertIn("state.EntityManager,\n                    _warningStateQuery", system)

    def test_presentation_cache_has_explicit_world_and_disposal_owner(self) -> None:
        update = GAMEPLAY_UPDATE.read_text(encoding="utf-8")
        presentation = THREAT_PRESENTATION_STATE.read_text(encoding="utf-8")
        self.assertIn(
            "WorldScopedComponentQueryCache<ThreatWarningRuntimeStateComponent>",
            presentation,
        )
        self.assertIn("Present(World world", presentation)
        self.assertIn("_queryCache.Dispose();", presentation)
        self.assertIn("ThreatWarningPresentationState _threatWarningPresentation", update)
        self.assertIn("_threatWarningPresentation.Dispose();", update)
        self.assertNotIn("ThreatWarningRuntimeState.HasPendingWarning", update)
        self.assertNotIn("World.DefaultGameObjectInjectionWorld", presentation)
        self.assertNotIn("World.DefaultGameObjectInjectionWorld", update)

    def test_match_intro_query_uses_composition_supplied_world(self) -> None:
        query = MATCH_INTRO_QUERY.read_text(encoding="utf-8")
        bootstrap = MATCH_BOOTSTRAP.read_text(encoding="utf-8")
        scene = MATCH_SCENE.read_text(encoding="utf-8")
        self.assertIn("public void Bind(World world)", query)
        self.assertIn("WorldScopedComponentQueryCache<MatchIntroTransitionComponent>", query)
        self.assertNotIn("World.DefaultGameObjectInjectionWorld", query)
        self.assertIn("matchIntroStateQuery.Bind(runtimeWorld);", bootstrap)
        self.assertNotIn("World.DefaultGameObjectInjectionWorld", bootstrap)
        self.assertIn("matchBootstrapSystem.Awake(world, this, transform, gameObject.layer);", scene)

    def test_world_isolation_recovery_fail_closed_and_allocations_are_covered(self) -> None:
        behavior = BEHAVIOR_TESTS.read_text(encoding="utf-8")
        for required in (
            "ThreatWarningRuntimeState_IsIsolatedAcrossWorlds",
            "ThreatWarningRuntimeState_RecreatedWorldStartsClear",
            "ThreatWarningRuntimeState_ResetIsIdempotent",
            "ThreatWarningRuntimeState_MissingOrDuplicateSingletonFailsClosed",
            "MatchIntroEcsStateQuery_UnboundFailsClosed",
            "MatchIntroEcsStateQuery_UsesExplicitBoundWorld",
            "MatchIntroEcsStateQuery_RecoversFromMissingAndRebinds",
            "MatchIntroEcsStateQuery_DuplicateBoundaryFailsClosed",
        ):
            self.assertIn(required, behavior)

        performance = PERFORMANCE_TESTS.read_text(encoding="utf-8")
        self.assertIn("ThreatWarningStateWarmAccess_AllocatesZeroManagedBytes", performance)
        self.assertIn("MatchIntroStateWarmAccess_AllocatesZeroManagedBytes", performance)
        self.assertIn("typeof(ThreatWarningPresentationState)", performance)
        self.assertIn('field.Name == "_queryCache"', performance)
        self.assertNotIn('"_threatWarningStateQueryCache"', performance)
        self.assertIn("phase=threat-warning-state", performance)
        self.assertIn("phase=match-intro-state", performance)


if __name__ == "__main__":
    unittest.main()
