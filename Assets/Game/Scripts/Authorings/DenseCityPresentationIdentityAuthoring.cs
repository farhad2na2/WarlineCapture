using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class DenseCityPresentationIdentityAuthoring : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private OperationMapEntityPresentationRole role;
        [SerializeField] private DenseCityPresentationSemanticCategory category;
        [SerializeField] private DenseCityPresentationSemanticFlags flags;

        public string StableId => stableId;
        public OperationMapEntityPresentationRole Role => role;
        public DenseCityPresentationSemanticCategory Category => category;
        public bool AllowsProtectedOverlap =>
            (flags & DenseCityPresentationSemanticFlags.AllowsProtectedOverlap) != 0;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string generatedStableId,
            OperationMapEntityPresentationRole presentationRole,
            DenseCityPresentationSemanticCategory presentationCategory,
            bool allowsProtectedOverlap = false)
        {
            stableId = generatedStableId;
            role = presentationRole;
            category = presentationCategory;
            flags = allowsProtectedOverlap
                ? DenseCityPresentationSemanticFlags.AllowsProtectedOverlap
                : DenseCityPresentationSemanticFlags.None;
        }
#endif

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidGeneratedStableId(stableId))
            {
                error = "A valid generated dense-city stable id is required.";
                return false;
            }

            if (role != OperationMapEntityPresentationRole.GameplayBuildings &&
                role != OperationMapEntityPresentationRole.RenderOnly)
            {
                error = $"Unsupported dense-city presentation role: {(byte)role}.";
                return false;
            }
            bool categoryMatchesRole = role == OperationMapEntityPresentationRole.GameplayBuildings
                ? category == DenseCityPresentationSemanticCategory.GameplayBuildingIntact
                : category is DenseCityPresentationSemanticCategory.Infrastructure or
                    DenseCityPresentationSemanticCategory.Vegetation or
                    DenseCityPresentationSemanticCategory.Prop or
                    DenseCityPresentationSemanticCategory.Horizon;
            if (!categoryMatchesRole)
            {
                error = $"Dense-city category {category} does not match role {role}.";
                return false;
            }
            if (AllowsProtectedOverlap &&
                (role != OperationMapEntityPresentationRole.RenderOnly ||
                 category != DenseCityPresentationSemanticCategory.Infrastructure))
            {
                error = "Only render-only infrastructure may allow protected overlap.";
                return false;
            }
            if ((flags & ~DenseCityPresentationSemanticFlags.AllowsProtectedOverlap) != 0)
            {
                error = $"Unsupported dense-city semantic flags: {(byte)flags}.";
                return false;
            }

            error = null;
            return true;
        }

        [BakingVersion("WarlineCapture", 2)]
        private sealed class IdentityBaker : Baker<DenseCityPresentationIdentityAuthoring>
        {
            public override void Bake(DenseCityPresentationIdentityAuthoring authoring)
            {
                if (!authoring.TryValidate(out _))
                    return;

                bool airliftSocket=authoring.stableId==OperationMapMissionPresentationSocket.AirliftTransportSceneryStableId;
                // Keep this exact scenery owner's render hierarchy addressable so a
                // mission can replace it without editing or deleting the physical source.
                Entity entity = GetEntity(airliftSocket?TransformUsageFlags.Dynamic:TransformUsageFlags.Renderable);
                if(airliftSocket)AddComponent(entity,new OperationMapMissionPresentationSocket {MissionId="saga.ch01.m04.airlift"});
                AddComponent(entity, new DenseCityPresentationIdentity
                {
                    StableId = new FixedString128Bytes(authoring.stableId),
                    Role = (byte)authoring.role,
                    Category = (byte)authoring.category,
                    Flags = (byte)authoring.flags
                });
                OperationMapRenderSourceBakingMarkerBuilder.AddOwnerMarkers(
                    this,
                    authoring,
                    "densegenerated|" + authoring.stableId,
                    authoring.role);
            }
        }
    }
}
