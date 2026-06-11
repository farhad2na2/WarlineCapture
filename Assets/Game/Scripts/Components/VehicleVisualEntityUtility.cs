using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public static class VehicleVisualEntityUtility
{
    public static void DestroyVisualTree(EntityManager em, Entity root)
    {
        if (root == Entity.Null || !em.Exists(root))
            return;

        var entities = new NativeList<Entity>(Allocator.Temp);
        Collect(em, root, ref entities);
        for (int i = entities.Length - 1; i >= 0; i--)
        {
            Entity entity = entities[i];
            if (em.Exists(entity))
                em.DestroyEntity(entity);
        }

        entities.Dispose();
    }

    private static void Collect(EntityManager em, Entity entity, ref NativeList<Entity> entities)
    {
        entities.Add(entity);
        if (!em.HasBuffer<Child>(entity))
            return;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
        {
            Entity child = children[i].Value;
            if (em.Exists(child))
                Collect(em, child, ref entities);
        }
    }
}
