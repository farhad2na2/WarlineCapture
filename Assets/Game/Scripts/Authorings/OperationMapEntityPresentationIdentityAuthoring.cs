using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class OperationMapEntityPresentationIdentityAuthoring : MonoBehaviour
    {
        public const int NoPlacementIndex = -1;

        [SerializeField] private string operationMapId;
        [SerializeField] private string sourceGlobalObjectId;
        [SerializeField] private OperationMapEntityPresentationRole role;
        [SerializeField] private int placementIndex = NoPlacementIndex;

        public string OperationMapId => operationMapId;
        public string SourceGlobalObjectId => sourceGlobalObjectId;
        public OperationMapEntityPresentationRole Role => role;
        public int PlacementIndex => placementIndex;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string mapId,
            string sourceId,
            OperationMapEntityPresentationRole presentationRole,
            int sourcePlacementIndex)
        {
            operationMapId = mapId;
            sourceGlobalObjectId = sourceId;
            role = presentationRole;
            placementIndex = sourcePlacementIndex;
        }
#endif

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }

            if (!OperationMapIdentityRules.IsValidSourceGlobalObjectId(sourceGlobalObjectId))
            {
                error = "Source GlobalObjectId is missing or malformed.";
                return false;
            }

            if (role != OperationMapEntityPresentationRole.GameplayBuildings &&
                role != OperationMapEntityPresentationRole.GameplayVehicles &&
                role != OperationMapEntityPresentationRole.RenderOnly)
            {
                error = $"Unknown operation-map entity presentation role: {(byte)role}.";
                return false;
            }

            bool requiresPlacement = role == OperationMapEntityPresentationRole.GameplayBuildings ||
                                     role == OperationMapEntityPresentationRole.GameplayVehicles;
            if ((requiresPlacement && placementIndex < 0) ||
                (!requiresPlacement && placementIndex != NoPlacementIndex))
            {
                error = $"Placement index {placementIndex} is invalid for role {role}.";
                return false;
            }

            error = null;
            return true;
        }

        [BakingVersion("WarlineCapture", 1)]
        private sealed class IdentityBaker : Baker<OperationMapEntityPresentationIdentityAuthoring>
        {
            public override void Bake(OperationMapEntityPresentationIdentityAuthoring authoring)
            {
                if (!authoring.TryValidate(out _))
                    return;

                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new OperationMapEntityPresentationIdentity
                {
                    OperationMapId = new FixedString128Bytes(authoring.operationMapId),
                    SourceGlobalObjectId = new FixedString128Bytes(authoring.sourceGlobalObjectId),
                    Role = (byte)authoring.role,
                    PlacementIndex = authoring.placementIndex
                });
                OperationMapRenderSourceBakingMarkerBuilder.AddOwnerMarkers(
                    this,
                    authoring,
                    "acceptedmap|" + authoring.sourceGlobalObjectId,
                    authoring.role);
            }
        }
    }
}
