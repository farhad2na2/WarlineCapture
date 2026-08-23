using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class SelectionMarkerAuthoring : MonoBehaviour
    {
        [BakingVersion("WarlineCapture", 1)]
        private sealed class Baker : Baker<SelectionMarkerAuthoring>
        {
            public override void Bake(SelectionMarkerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<SelectionMarkerTag>(entity);

                if (authoring.transform.childCount <= 0)
                    return;

                Transform visualChild = authoring.transform.GetChild(0);
                if (visualChild == null)
                    return;

                Entity visualEntity = GetEntity(visualChild, TransformUsageFlags.Dynamic);
                float visibleScale = visualChild.localScale.x;
                if (Mathf.Approximately(visibleScale, 0f))
                    visibleScale = 1f;

                AddComponent(entity, new SelectionMarkerVisualChild
                {
                    Value = visualEntity,
                    VisibleScale = visibleScale,
                    VisibleScaleX = visibleScale,
                    VisibleScaleZ = visibleScale
                });

                AddComponent(entity, new SelectionMarkerVariantVisuals
                {
                    InfantryGroundRing = GetOptionalVisualEntity(visualChild, "InfantryGroundRing"),
                    VehicleFootprintFill = GetOptionalVisualEntity(visualChild, "VehicleFootprintFill"),
                    VehicleCornerBrackets = GetOptionalVisualEntity(visualChild, "VehicleCornerBrackets"),
                    VehicleBoundsFrame = GetOptionalVisualEntity(visualChild, "VehicleBoundsFrame")
                });
            }

            private Entity GetOptionalVisualEntity(Transform visualRoot, string exactName)
            {
                for (int index = 0; index < visualRoot.childCount; index++)
                {
                    Transform child = visualRoot.GetChild(index);
                    if (child != null && child.name == exactName)
                        return GetEntity(child, TransformUsageFlags.Dynamic);
                }

                return Entity.Null;
            }
        }
    }
}
