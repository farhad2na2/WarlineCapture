// Logical mission override proposal; no Campaign asset or global LOD mutation.
using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Mission View Profile")]
    public sealed class SkirmishMissionViewProfile : ScriptableObject
    {
        public SkirmishMissionOpeningFocusPolicy OpeningFocusPolicy;
        // Optional scoped S005 hint; empty preserves the established first-actor policy.
        public string OpeningFocusStableActorId;
        // Optional exact active-map lane; never changes the scenario physical source.
        public string ActiveOperationMapId, ActiveMapContentHash, ActiveMapSourceIdentityHash;
        public bool UsesActiveMapIdentity => !string.IsNullOrEmpty(ActiveOperationMapId);
        // Scoped override preserves aircraft heading when this ground candidate rotates.
        public bool UseSeparateAircraftYaw;
        public float AircraftYaw;
        public float EffectiveAircraftYaw => UseSeparateAircraftYaw ? AircraftYaw : Yaw;
        public bool AuthoredNonBaseAssault, AuthoredGenericBaseAssault;
        public Game.Skirmish.Contracts.SkirmishObjectiveKind ObjectiveKind;
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
            !UsesActiveMapIdentity && IsValid && setup != null && definition != null && setup.CatalogId == CatalogId && definition.CatalogId == CatalogId &&
            setup.DefinitionId == DefinitionId && setup.ContentVersion == DefinitionVersion && definition.ContentVersion == DefinitionVersion &&
            definition.CampaignMap != null && definition.CampaignMap.ContentHash == CampaignSourceContentHash &&
            definition.MapLayout != null && definition.MapLayout.ContentHash == LayoutContentHash &&
            setup.LayoutId == definition.MapLayoutId && setup.OperationMapId == definition.OperationMapId &&
            (!(AuthoredNonBaseAssault || AuthoredGenericBaseAssault) || setup.ObjectiveKind == ObjectiveKind && definition.ObjectiveConfig != null && definition.ObjectiveConfig.Kind == ObjectiveKind);
        public bool MatchesActiveMap(SkirmishResolvedSetup setup, SkirmishScenarioDefinitionConfig definition,
            string mapId,string contentHash,string sourceIdentityHash) =>
            UsesActiveMapIdentity && IsValid && setup!=null && definition!=null &&
            setup.CatalogId==CatalogId && definition.CatalogId==CatalogId &&
            setup.DefinitionId==DefinitionId && definition.DefinitionId==DefinitionId &&
            setup.ContentVersion==DefinitionVersion && definition.ContentVersion==DefinitionVersion &&
            setup.OperationMapId==ActiveOperationMapId && definition.OperationMapId==ActiveOperationMapId && mapId==ActiveOperationMapId &&
            definition.MapLayout!=null && setup.LayoutId==definition.MapLayoutId && definition.MapLayout.LayoutId==setup.LayoutId &&
            definition.MapLayout.OperationMapId==setup.OperationMapId && definition.MapLayout.ContentHash==LayoutContentHash &&
            setup.ObjectiveKind==ObjectiveKind && definition.ObjectiveConfig!=null && definition.ObjectiveConfig.Kind==ObjectiveKind &&
            contentHash==ActiveMapContentHash && sourceIdentityHash==ActiveMapSourceIdentityHash;
        public bool IsValid => System.Enum.IsDefined(typeof(SkirmishMissionOpeningFocusPolicy),OpeningFocusPolicy) &&
            (OpeningFocusPolicy==SkirmishMissionOpeningFocusPolicy.FirstOpeningOwnedRifleGroup&&UsesActiveMapIdentity||OpeningFocusPolicy==SkirmishMissionOpeningFocusPolicy.None||!AuthoredNonBaseAssault&&!AuthoredGenericBaseAssault&&CatalogId=="S005"&&DefinitionId=="skirmish.s005") &&
            SchemaVersion == 1 && ContentVersion > 0 && DefinitionVersion > 0 &&
            ValidIdentity &&
            (string.IsNullOrEmpty(OpeningFocusStableActorId) || OpeningFocusPolicy==SkirmishMissionOpeningFocusPolicy.S005FirstOpeningOwnedRifle) &&
            (!UseSeparateAircraftYaw || !AuthoredNonBaseAssault && !AuthoredGenericBaseAssault && CatalogId=="S005" && float.IsFinite(AircraftYaw)) &&
            (UsesActiveMapIdentity ? !AuthoredNonBaseAssault&&!AuthoredGenericBaseAssault&&!string.IsNullOrWhiteSpace(ActiveMapContentHash)&&!string.IsNullOrWhiteSpace(ActiveMapSourceIdentityHash) : !string.IsNullOrWhiteSpace(CampaignSourceContentHash)) &&
            !string.IsNullOrWhiteSpace(LayoutContentHash) && Positive(MinHeight) && Positive(MaxHeight) && MaxHeight >= MinHeight &&
            NormalHeight >= MinHeight && NormalHeight <= MaxHeight && BuildHeight >= NormalHeight && BuildHeight <= MaxHeight &&
            Positive(Pitch) && Pitch < 90 && float.IsFinite(Yaw) && Positive(NormalFieldOfView) && NormalFieldOfView < 179 &&
            float.IsFinite(GroundReferenceY) && Positive(MaximumGroundCenterSlantDistance) &&
            MaxHeight <= GroundReferenceY + MaximumGroundCenterSlantDistance * Mathf.Sin(Pitch * Mathf.Deg2Rad) &&
            Positive(BuildFieldOfView) && BuildFieldOfView < 179 && Positive(InfantryMinimumViewportHeight) && InfantryMinimumViewportHeight < 1 &&
            Positive(PlaneMaximumViewportDimension) && PlaneMaximumViewportDimension < 1 &&
            Positive(AircraftPitch) && AircraftPitch < 90 && Positive(AircraftFieldOfView) && AircraftFieldOfView < 179 &&
            Positive(AircraftMinimumDistance) && Positive(AircraftMaximumDistance) && AircraftMaximumDistance >= AircraftMinimumDistance;
        private bool ValidIdentity
        {
            get
            {
                if (!AuthoredNonBaseAssault && !AuthoredGenericBaseAssault && !UsesActiveMapIdentity) return CatalogId == "S005" && DefinitionId == "skirmish.s005";
                if (AuthoredNonBaseAssault && AuthoredGenericBaseAssault) return false;
                bool kind = UsesActiveMapIdentity ? !AuthoredNonBaseAssault&&!AuthoredGenericBaseAssault : AuthoredGenericBaseAssault ? ObjectiveKind == Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault &&
                    SkirmishMissionDispatch.RequiresSourceBoundDefinition(CatalogId, DefinitionVersion) :
                    ObjectiveKind == Game.Skirmish.Contracts.SkirmishObjectiveKind.FrontlineControl ||
                    ObjectiveKind == Game.Skirmish.Contracts.SkirmishObjectiveKind.Breakthrough || ObjectiveKind == Game.Skirmish.Contracts.SkirmishObjectiveKind.ConvoyEscort;
                return kind &&
                    CatalogId != null && CatalogId.Length == 4 && CatalogId[0] == 'S' &&
                    int.TryParse(CatalogId.Substring(1),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out int id) && id >= 1 && id <= 120 &&
                    DefinitionId == "skirmish." + CatalogId.ToLowerInvariant();
            }
        }
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}

