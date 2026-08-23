using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Authoring
{
    public enum OperationMapBuildingVisualState : byte
    {
        Intact = 0,
        Destroyed = 1
    }

    [DisallowMultipleComponent]
    public sealed class OperationMapBuildingAttachmentAuthoring : MonoBehaviour
    {
        [SerializeField] private OperationMapBuildingAuthoring buildingOwner;
        [SerializeField] private OperationMapBuildingVisualState visualState;

        public OperationMapBuildingAuthoring BuildingOwner => buildingOwner;
        public OperationMapBuildingVisualState VisualState => visualState;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            OperationMapBuildingAuthoring owner,
            OperationMapBuildingVisualState state)
        {
            buildingOwner = owner;
            visualState = state;
        }
#endif

        public bool TryValidate(out string error)
        {
            if (buildingOwner == null)
            {
                error = "A building attachment requires one building owner.";
                return false;
            }
            if (visualState is not (OperationMapBuildingVisualState.Intact or
                OperationMapBuildingVisualState.Destroyed))
            {
                error = $"Unsupported building attachment visual state: {(byte)visualState}.";
                return false;
            }

            Transform expectedRoot = visualState == OperationMapBuildingVisualState.Intact
                ? buildingOwner.IntactVisualRoot != null ? buildingOwner.IntactVisualRoot.transform : null
                : buildingOwner.DestroyedVisualRoot != null ? buildingOwner.DestroyedVisualRoot.transform : null;
            if (expectedRoot == null || transform.parent != expectedRoot)
            {
                error =
                    "A building attachment must be an immediate child of its declared visual-state root.";
                return false;
            }
            if (GetComponentsInParent<OperationMapBuildingAuthoring>(true).Length != 1 ||
                GetComponentInParent<OperationMapBuildingAuthoring>(true) != buildingOwner)
            {
                error = "A building attachment must resolve to exactly one hierarchy owner.";
                return false;
            }
            if (GetComponents<OperationMapEntityPresentationIdentityAuthoring>().Length != 0)
            {
                error = "A building attachment cannot also be an independent presentation owner.";
                return false;
            }

            error = null;
            return true;
        }

        [BakingVersion("WarlineCapture", 1)]
        private sealed class AttachmentBaker : Baker<OperationMapBuildingAttachmentAuthoring>
        {
            public override void Bake(OperationMapBuildingAttachmentAuthoring authoring)
            {
                if (!authoring.TryValidate(out _))
                    return;

                Entity attachment = GetEntity(TransformUsageFlags.Renderable);
                Entity building = GetEntity(authoring.buildingOwner, TransformUsageFlags.Dynamic);
                AddComponent(attachment, new OperationMapBuildingAttachment
                {
                    Building = building,
                    VisualState = (byte)authoring.visualState
                });
            }
        }
    }
}
