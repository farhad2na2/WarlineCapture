using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SelectionMarkerAuthoring : MonoBehaviour
{
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
                VisibleScale = visibleScale
            });
        }
    }
}
