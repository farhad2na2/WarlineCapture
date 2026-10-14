// Logical mission override proposal; no Campaign asset or global LOD mutation.
using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Mission View Profile")]
    public sealed class SkirmishMissionViewProfile : ScriptableObject
    {
        public int SchemaVersion = 1, ContentVersion = 1, DefinitionVersion;
        public string CatalogId, DefinitionId, CampaignSourceContentHash, LayoutContentHash;
        public float MinHeight, MaxHeight, NormalHeight, BuildHeight, Pitch, Yaw, NormalFieldOfView, BuildFieldOfView;
        public float GroundReferenceY, MaximumGroundCenterSlantDistance;
        public float InfantryMinimumViewportHeight, PlaneMaximumViewportDimension;
        public float AircraftPitch, AircraftFieldOfView, AircraftMinimumDistance, AircraftMaximumDistance;
        public static SkirmishMissionViewProfile Load(string catalogId) =>
            Resources.Load<SkirmishMissionViewProfile>("SkirmishMissionViewProfile_" + catalogId);

        // Initial engineering targets requested by owner/coordinator. Configurable;
        // neither these targets nor factory execution certify a native view.
        public static SkirmishMissionViewProfile CreateS005EngineeringCandidate(SkirmishScenarioDefinitionConfig definition)
        {
            if (definition == null || definition.CatalogId != "S005" || definition.CampaignMap == null || definition.MapLayout == null)
                throw new ArgumentException("Explicit actual S005 definition/source/layout required.");
            var value = CreateInstance<SkirmishMissionViewProfile>();
            value.CatalogId = definition.CatalogId; value.DefinitionId = definition.DefinitionId; value.DefinitionVersion = definition.ContentVersion;
            value.CampaignSourceContentHash = definition.CampaignMap.ContentHash; value.LayoutContentHash = definition.MapLayout.ContentHash;
            value.GroundReferenceY = definition.CampaignMap.SurfaceMetadata.MinimumHeight;
            value.MaximumGroundCenterSlantDistance = 63; // Below observed shared enemy Far threshold64; centerline guard only.
            value.Pitch = 55; value.Yaw = 35; value.NormalFieldOfView = 46; value.BuildFieldOfView = 50;
            value.MinHeight = 15;
            value.MaxHeight = Mathf.Min(50, Mathf.Floor(value.GroundReferenceY +
                value.MaximumGroundCenterSlantDistance * Mathf.Sin(value.Pitch * Mathf.Deg2Rad)));
            value.NormalHeight = Mathf.Min(47, value.MaxHeight - 2); value.BuildHeight = value.MaxHeight;
            value.InfantryMinimumViewportHeight = .017f; value.PlaneMaximumViewportDimension = .45f;
            value.AircraftPitch = 50; value.AircraftFieldOfView = 60; value.AircraftMinimumDistance = 80; value.AircraftMaximumDistance = 300;
            return value;
        }
        public bool Matches(SkirmishResolvedSetup setup, SkirmishScenarioDefinitionConfig definition) =>
            IsValid && setup != null && definition != null && setup.CatalogId == CatalogId && definition.CatalogId == CatalogId &&
            setup.DefinitionId == DefinitionId && setup.ContentVersion == DefinitionVersion && definition.ContentVersion == DefinitionVersion &&
            definition.CampaignMap != null && definition.CampaignMap.ContentHash == CampaignSourceContentHash &&
            definition.MapLayout != null && definition.MapLayout.ContentHash == LayoutContentHash &&
            setup.LayoutId == definition.MapLayoutId && setup.OperationMapId == definition.OperationMapId;
        public bool IsValid => SchemaVersion == 1 && ContentVersion > 0 && DefinitionVersion > 0 &&
            CatalogId == "S005" && DefinitionId == "skirmish.s005" && !string.IsNullOrWhiteSpace(CampaignSourceContentHash) &&
            !string.IsNullOrWhiteSpace(LayoutContentHash) && Positive(MinHeight) && Positive(MaxHeight) && MaxHeight >= MinHeight &&
            NormalHeight >= MinHeight && NormalHeight <= MaxHeight && BuildHeight >= NormalHeight && BuildHeight <= MaxHeight &&
            Positive(Pitch) && Pitch < 90 && float.IsFinite(Yaw) && Positive(NormalFieldOfView) && NormalFieldOfView < 179 &&
            float.IsFinite(GroundReferenceY) && Positive(MaximumGroundCenterSlantDistance) &&
            MaxHeight <= GroundReferenceY + MaximumGroundCenterSlantDistance * Mathf.Sin(Pitch * Mathf.Deg2Rad) &&
            Positive(BuildFieldOfView) && BuildFieldOfView < 179 && Positive(InfantryMinimumViewportHeight) && InfantryMinimumViewportHeight < 1 &&
            Positive(PlaneMaximumViewportDimension) && PlaneMaximumViewportDimension < 1 &&
            Positive(AircraftPitch) && AircraftPitch < 90 && Positive(AircraftFieldOfView) && AircraftFieldOfView < 179 &&
            Positive(AircraftMinimumDistance) && Positive(AircraftMaximumDistance) && AircraftMaximumDistance >= AircraftMinimumDistance;
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}

