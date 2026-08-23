using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Authoring
{
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

        [BakingVersion("WarlineCapture", 1)]
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
}
