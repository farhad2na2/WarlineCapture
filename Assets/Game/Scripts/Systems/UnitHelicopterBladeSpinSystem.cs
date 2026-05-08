using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[UpdateAfter(typeof(UnitModelSpawnSystem))]
public partial struct UnitHelicopterBladeSpinSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitAirMovement>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;
        float radians = math.radians(1440f) * dt;
        var bladeLookup = SystemAPI.GetBufferLookup<UnitHelicopterBladeReference>(true);
        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        var modelLookup = SystemAPI.GetComponentLookup<UnitModelInstanceReference>(true);
        var airStateLookup = SystemAPI.GetComponentLookup<UnitAirState>(true);
        var targetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true);
        var engageLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);

        foreach (var (transform, moveVisual, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<UnitMoveVisualState>>()
                     .WithAll<UnitAirMovement>()
                     .WithNone<UnitDeathAnimationState>()
                     .WithEntityAccess()
                     )
        {
            bool hasModel = modelLookup.HasComponent(entity) && em.Exists(modelLookup[entity].Instance);
            bool shouldSpin = moveVisual.ValueRO.IsMoving != 0;
            if (airStateLookup.HasComponent(entity))
            {
                UnitAirState airState = airStateLookup[entity];
                bool isAboveHomeGround = transform.ValueRO.Position.y > airState.HomePosition.y + 0.25f;
                shouldSpin =
                    shouldSpin ||
                    isAboveHomeGround ||
                    airState.Airborne != 0 ||
                    airState.ReturningHome != 0 ||
                    airState.TakeoffRolling != 0 ||
                    airState.LandingRolling != 0;
            }
            shouldSpin = shouldSpin || targetLookup.HasComponent(entity) || engageLookup.HasComponent(entity);

            if (!shouldSpin)
                continue;

            int spunCount = 0;
            if (bladeLookup.HasBuffer(entity))
                spunCount = RotateBakedBlades(em, bladeLookup[entity], radians);

            if (spunCount == 0 && hasModel)
                spunCount = RotateBladeDescendants(em, childLookup, modelLookup[entity].Instance, radians);
        }
    }

    private static int RotateBakedBlades(EntityManager em, DynamicBuffer<UnitHelicopterBladeReference> blades, float radians)
    {
        int spunCount = 0;
        for (int i = 0; i < blades.Length; i++)
        {
            UnitHelicopterBladeReference bladeRef = blades[i];
            if (!em.Exists(bladeRef.Blade) || !em.HasComponent<LocalTransform>(bladeRef.Blade))
                continue;

            LocalTransform transform = em.GetComponentData<LocalTransform>(bladeRef.Blade);
            transform.Rotation = math.mul(transform.Rotation, CreateBladeDeltaRotation(bladeRef.Axis, radians));
            em.SetComponentData(bladeRef.Blade, transform);
            spunCount++;
        }

        return spunCount;
    }

    private static int RotateBladeDescendants(EntityManager em, BufferLookup<Child> childLookup, Entity root, float radians)
    {
        int spunCount = 0;
        using var stack = new NativeList<Entity>(Allocator.Temp);
        stack.Add(root);
        while (stack.Length > 0)
        {
            Entity current = stack[stack.Length - 1];
            stack.RemoveAt(stack.Length - 1);

            FixedString64Bytes name = em.GetName(current);
            if (TryGetBladeAxis(name.ToString(), out byte axis) && em.HasComponent<LocalTransform>(current))
            {
                LocalTransform transform = em.GetComponentData<LocalTransform>(current);
                transform.Rotation = math.mul(transform.Rotation, CreateBladeDeltaRotation(axis, radians));
                em.SetComponentData(current, transform);
                spunCount++;
            }

            if (!childLookup.HasBuffer(current))
                continue;

            DynamicBuffer<Child> children = childLookup[current];
            for (int i = 0; i < children.Length; i++)
                stack.Add(children[i].Value);
        }

        return spunCount;
    }

    private static quaternion CreateBladeDeltaRotation(byte axis, float radians)
    {
        if (axis == 0)
            return quaternion.AxisAngle(math.right(), radians);
        if (axis == 1)
            return quaternion.AxisAngle(math.up(), radians);
        return quaternion.AxisAngle(math.forward(), radians);
    }

    private static bool TryGetBladeAxis(string name, out byte axis)
    {
        axis = 0;
        if (string.IsNullOrEmpty(name) || !name.Contains("Blade", System.StringComparison.Ordinal))
            return false;
        if (name.EndsWith("_X", System.StringComparison.Ordinal))
        {
            axis = 0;
            return true;
        }
        if (name.EndsWith("_Y", System.StringComparison.Ordinal))
        {
            axis = 1;
            return true;
        }
        if (name.EndsWith("_Z", System.StringComparison.Ordinal))
        {
            axis = 2;
            return true;
        }

        return false;
    }
}
