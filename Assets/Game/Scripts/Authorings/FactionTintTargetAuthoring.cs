using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FactionTintTargetAuthoring : MonoBehaviour
{
    [SerializeField] private FactionTintTargetConfig config;
    [SerializeField, HideInInspector] private Color defaultColor = Color.white;

    private void OnValidate()
    {
        if (config != null)
            defaultColor = config.DefaultColor;
    }

    private sealed class Baker : Baker<FactionTintTargetAuthoring>
    {
        public override void Bake(FactionTintTargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<FactionTintTarget>(entity);
            AddComponent(entity, new FactionTintColor
            {
                Value = new float4(
                    authoring.defaultColor.r,
                    authoring.defaultColor.g,
                    authoring.defaultColor.b,
                    authoring.defaultColor.a)
            });
        }
    }
}
