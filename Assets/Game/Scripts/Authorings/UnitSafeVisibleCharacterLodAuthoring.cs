using Unity.Entities;
using UnityEngine;

public sealed class UnitSafeVisibleCharacterLodAuthoring : MonoBehaviour
{
    private sealed class Baker : Baker<UnitSafeVisibleCharacterLodAuthoring>
    {
        public override void Bake(UnitSafeVisibleCharacterLodAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<UnitSafeVisibleCharacterLodTag>(entity);
        }
    }
}
