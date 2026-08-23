using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class UnitHealthBarAuthoring : MonoBehaviour
    {
        [SerializeField] private UnitHealthBarConfig config;
        [SerializeField, HideInInspector, Range(0f, 1f)] private float DefaultFill = 1f;

        private void OnValidate()
        {
            if (config != null)
                DefaultFill = config.DefaultFill;
        }

        [BakingVersion("WarlineCapture", 1)]
        private sealed class UnitHealthBarBaker : Baker<UnitHealthBarAuthoring>
        {
            public override void Bake(UnitHealthBarAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealthBarFill { Value = Mathf.Clamp01(authoring.DefaultFill) });
            }
        }
    }
}
