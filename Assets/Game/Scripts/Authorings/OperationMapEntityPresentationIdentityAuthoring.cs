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

            if (!IsValidSourceGlobalObjectId(sourceGlobalObjectId))
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

        private static bool IsValidSourceGlobalObjectId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] segments = value.Split('-');
            if (segments.Length != 5 ||
                !string.Equals(segments[0], "GlobalObjectId_V1", System.StringComparison.Ordinal) ||
                !uint.TryParse(segments[1], out _) ||
                segments[2].Length != 32 ||
                !ulong.TryParse(segments[3], out _) ||
                !ulong.TryParse(segments[4], out _))
            {
                return false;
            }

            for (int i = 0; i < segments[2].Length; i++)
            {
                char character = segments[2][i];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }

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
            }
        }
    }
}
