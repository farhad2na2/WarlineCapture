using System;
using Game.Editor;
using UnityEditor;
using UnityEngine;

public static class BuildingFootprintReadinessValidation
{
    public static void Run()
    {
        try
        {
            BuildingFootprintAudit.Run();
            var tests = new BuildingDefinitionPlacementFootprintTests();
            tests.CanonicalBarracksRuntimeAndCatalogFootprintsFitTheM2Lot();
            tests.FootprintFitsRenderedGeometryInsteadOfStaleAuthoredReservation(10, 8, 2, 3, 2, 3);
            tests.FootprintFitsRenderedGeometryInsteadOfStaleAuthoredReservation(2, 3, 10, 8, 10, 8);
            tests.FootprintFitsRenderedGeometryInsteadOfStaleAuthoredReservation(10, 3, 2, 8, 2, 8);
            tests.FootprintIgnoresInactiveGeometryAndParticleEffects();
            tests.RadarFitsItsModelAndGateKeepsMovingArmClearance();
            var origins = new BuildingPlacementOriginSearchTests();
            origins.CompactRuntimeBuildingStillFindsClearLandBeyondOccupiedCompound();
            origins.S004EnemyRefinerySearchRespectsBakedBuildingPlacement(true);
            origins.S004EnemyRefinerySearchRespectsBakedBuildingPlacement(false);
            origins.PlacedAirportOverlapUsesRotatedRunwayAndStoredModelDimensions();
            Debug.Log("[BuildingFootprintReadiness] result=Passed catalog=25 geometry=7 spawn=3");
            MissionReadinessArchitectureValidation.Run();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
