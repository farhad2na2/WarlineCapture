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

        public string StableId => stableId;
        public OperationMapEntityPresentationRole Role => role;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string generatedStableId,
            OperationMapEntityPresentationRole presentationRole)
        {
            stableId = generatedStableId;
            role = presentationRole;
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

            error = null;
            return true;
        }

        private sealed class IdentityBaker : Baker<DenseCityPresentationIdentityAuthoring>
        {
            public override void Bake(DenseCityPresentationIdentityAuthoring authoring)
            {
                if (!authoring.TryValidate(out _))
                    return;

                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new DenseCityPresentationIdentity
                {
                    StableId = new FixedString128Bytes(authoring.stableId),
                    Role = (byte)authoring.role
                });
            }
        }
    }
}
