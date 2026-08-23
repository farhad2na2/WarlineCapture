using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Authoring
{
    public sealed class UnitSafeVisibleCharacterLodAuthoring : MonoBehaviour
    {
        [BakingVersion("WarlineCapture", 1)]
        private sealed class Baker : Baker<UnitSafeVisibleCharacterLodAuthoring>
        {
            public override void Bake(UnitSafeVisibleCharacterLodAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<UnitSafeVisibleCharacterLodTag>(entity);
            }
        }
    }
}
