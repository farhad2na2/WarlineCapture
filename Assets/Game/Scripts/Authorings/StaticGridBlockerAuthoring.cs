using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class StaticGridBlockerAuthoring : MonoBehaviour
{
    [SerializeField] private StaticGridBlockerAuthoringConfig config;
    [SerializeField, HideInInspector] private Vector2Int cell = new(5, 5);
    [SerializeField, HideInInspector] private Vector2Int size = new(1, 1);

    private void OnValidate()
    {
        ApplyConfigIfAvailable();
    }

    private void ApplyConfigIfAvailable()
    {
        if (config == null)
            return;

        cell = config.Cell;
        size = config.Size;
    }

    private class StaticGridBlockerBaker : Baker<StaticGridBlockerAuthoring>
    {
        public override void Bake(StaticGridBlockerAuthoring authoring)
        {
            authoring.ApplyConfigIfAvailable();
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new UnitGrid
            {
                Cell = new int2(authoring.cell.x, authoring.cell.y)
            });

            AddComponent<StaticGridBlocker>(entity);

            var blockerSize = new int2(math.max(1, authoring.size.x), math.max(1, authoring.size.y));
            AddComponent(entity, new GridBlockerSize { Size = blockerSize });
        }
    }
}
