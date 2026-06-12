using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using UnityEngine;

[UpdateAfter(typeof(UnitModelSpawnSystem))]
public partial struct UnitHelicopterBladeSpinSystem : ISystem
{
    private const float FlyingHeightEpsilon = 0.25f;
    private static bool s_DiagnosticLogged;

    public void OnCreate(ref SystemState state)
    {
        s_DiagnosticLogged = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        float radians = math.radians(1440f) * SystemAPI.Time.DeltaTime;
        var bladeLookup = SystemAPI.GetBufferLookup<UnitHelicopterBladeReference>(true);
        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        var modelLookup = SystemAPI.GetComponentLookup<UnitModelInstanceReference>(true);
        var detailLookup = SystemAPI.GetComponentLookup<UnitDetailedVisualReference>(true);
        var midLookup = SystemAPI.GetComponentLookup<UnitMidLodInstanceReference>(true);
        var lowLookup = SystemAPI.GetComponentLookup<UnitLowLodInstanceReference>(true);
        var sourceLookup = SystemAPI.GetComponentLookup<UnitSourcePrefabKey>(true);
        var displayLookup = SystemAPI.GetComponentLookup<UnitDisplayInfo>(true);
        var visualStateLookup = SystemAPI.GetComponentLookup<UnitRenderVisualComponent>(true);
        var airLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true);
        var airStateLookup = SystemAPI.GetComponentLookup<UnitAirComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        bool shouldLogDiagnostics = SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
                                    SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
        using var rotatedBlades = new NativeHashSet<Entity>(32, Allocator.Temp);

        foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<UnitAirMovement>>()
                     .WithNone<UnitDeathAnimationComponent>()
                     .WithEntityAccess()
                     )
        {
            bool shouldSpin = ShouldSpinBlades(entity, transformLookup, airStateLookup);
            if (!shouldSpin)
            {
                if (shouldLogDiagnostics &&
                    !s_DiagnosticLogged &&
                    IsHelicopterDiagnosticCandidate(em, childLookup, entity, bladeLookup, detailLookup, modelLookup, midLookup, lowLookup, sourceLookup))
                {
                    s_DiagnosticLogged = true;
                    LogHelicopterBladeDiagnostic(
                        em,
                        childLookup,
                        entity,
                        bladeLookup,
                        detailLookup,
                        modelLookup,
                        midLookup,
                        lowLookup,
                        sourceLookup,
                        displayLookup,
                        visualStateLookup,
                        airLookup,
                        airStateLookup,
                        transformLookup,
                        false,
                        0,
                        0,
                        0,
                        SystemAPI.Time.DeltaTime);
                }

                continue;
            }

            rotatedBlades.Clear();

            int detailRotated = 0;
            int modelRotated = 0;
            int bakedRotated = 0;

            if (detailLookup.HasComponent(entity))
                detailRotated = RotateBladeDescendants(em, childLookup, detailLookup[entity].Root, radians, rotatedBlades);

            if (modelLookup.HasComponent(entity))
                modelRotated = RotateBladeDescendants(em, childLookup, modelLookup[entity].Instance, radians, rotatedBlades);

            if (bladeLookup.HasBuffer(entity))
                bakedRotated = RotateBakedBlades(em, bladeLookup[entity], radians, rotatedBlades);

            if (shouldLogDiagnostics &&
                !s_DiagnosticLogged &&
                IsHelicopterDiagnosticCandidate(em, childLookup, entity, bladeLookup, detailLookup, modelLookup, midLookup, lowLookup, sourceLookup))
            {
                s_DiagnosticLogged = true;
                LogHelicopterBladeDiagnostic(
                    em,
                    childLookup,
                    entity,
                    bladeLookup,
                    detailLookup,
                    modelLookup,
                    midLookup,
                    lowLookup,
                    sourceLookup,
                    displayLookup,
                    visualStateLookup,
                    airLookup,
                    airStateLookup,
                    transformLookup,
                    true,
                    detailRotated,
                    modelRotated,
                    bakedRotated,
                    SystemAPI.Time.DeltaTime);
            }
        }

        if (shouldLogDiagnostics && !s_DiagnosticLogged)
        {
            foreach (var (sourceKey, entity) in SystemAPI
                         .Query<RefRO<UnitSourcePrefabKey>>()
                         .WithAll<UnitGrid>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess())
            {
                if (!sourceKey.ValueRO.Value.ToString().Contains("Helicopter", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                s_DiagnosticLogged = true;
                LogHelicopterBladeDiagnostic(
                    em,
                    childLookup,
                    entity,
                    bladeLookup,
                    detailLookup,
                    modelLookup,
                    midLookup,
                    lowLookup,
                    sourceLookup,
                    displayLookup,
                    visualStateLookup,
                    airLookup,
                    airStateLookup,
                    transformLookup,
                    false,
                    0,
                    0,
                    0,
                    SystemAPI.Time.DeltaTime);
                break;
            }
        }
    }

    private static bool ShouldSpinBlades(
        Entity entity,
        ComponentLookup<LocalTransform> transformLookup,
        ComponentLookup<UnitAirComponent> airStateLookup)
    {
        if (!airStateLookup.HasComponent(entity))
            return false;

        UnitAirComponent airState = airStateLookup[entity];
        if (airState.Airborne != 0 ||
            airState.TakeoffRolling != 0 ||
            airState.LandingRolling != 0)
        {
            return true;
        }

        if (airState.HomeInitialized == 0 || !transformLookup.HasComponent(entity))
            return false;

        return transformLookup[entity].Position.y > airState.HomePosition.y + FlyingHeightEpsilon;
    }

    private static int RotateBakedBlades(
        EntityManager em,
        DynamicBuffer<UnitHelicopterBladeReference> blades,
        float radians,
        NativeHashSet<Entity> rotatedBlades)
    {
        int spunCount = 0;
        for (int i = 0; i < blades.Length; i++)
        {
            UnitHelicopterBladeReference bladeRef = blades[i];
            if (!em.Exists(bladeRef.Blade) ||
                !em.HasComponent<LocalTransform>(bladeRef.Blade) ||
                !rotatedBlades.Add(bladeRef.Blade))
            {
                continue;
            }

            LocalTransform transform = em.GetComponentData<LocalTransform>(bladeRef.Blade);
            transform.Rotation = math.mul(transform.Rotation, CreateBladeDeltaRotation(bladeRef.Axis, radians));
            em.SetComponentData(bladeRef.Blade, transform);
            spunCount++;
        }

        return spunCount;
    }

    private static int RotateBladeDescendants(
        EntityManager em,
        BufferLookup<Child> childLookup,
        Entity root,
        float radians,
        NativeHashSet<Entity> rotatedBlades)
    {
        if (root == Entity.Null || !em.Exists(root))
            return 0;

        int spunCount = 0;
        using var stack = new NativeList<Entity>(Allocator.Temp);
        stack.Add(root);
        while (stack.Length > 0)
        {
            Entity current = stack[stack.Length - 1];
            stack.RemoveAt(stack.Length - 1);

            FixedString64Bytes name = em.GetName(current);
            if (TryGetBladeAxis(name.ToString(), out byte axis) &&
                em.HasComponent<LocalTransform>(current) &&
                rotatedBlades.Add(current))
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

    private static bool IsHelicopterDiagnosticCandidate(
        EntityManager em,
        BufferLookup<Child> childLookup,
        Entity entity,
        BufferLookup<UnitHelicopterBladeReference> bladeLookup,
        ComponentLookup<UnitDetailedVisualReference> detailLookup,
        ComponentLookup<UnitModelInstanceReference> modelLookup,
        ComponentLookup<UnitMidLodInstanceReference> midLookup,
        ComponentLookup<UnitLowLodInstanceReference> lowLookup,
        ComponentLookup<UnitSourcePrefabKey> sourceLookup)
    {
        if (sourceLookup.HasComponent(entity) &&
            sourceLookup[entity].Value.ToString().Contains("Helicopter", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (bladeLookup.HasBuffer(entity) && bladeLookup[entity].Length > 0)
            return true;

        if (detailLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, detailLookup[entity].Root) > 0)
            return true;
        if (modelLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, modelLookup[entity].Instance) > 0)
            return true;
        if (midLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, midLookup[entity].Instance) > 0)
            return true;
        if (lowLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, lowLookup[entity].Instance) > 0)
            return true;

        return false;
    }

    private static void LogHelicopterBladeDiagnostic(
        EntityManager em,
        BufferLookup<Child> childLookup,
        Entity entity,
        BufferLookup<UnitHelicopterBladeReference> bladeLookup,
        ComponentLookup<UnitDetailedVisualReference> detailLookup,
        ComponentLookup<UnitModelInstanceReference> modelLookup,
        ComponentLookup<UnitMidLodInstanceReference> midLookup,
        ComponentLookup<UnitLowLodInstanceReference> lowLookup,
        ComponentLookup<UnitSourcePrefabKey> sourceLookup,
        ComponentLookup<UnitDisplayInfo> displayLookup,
        ComponentLookup<UnitRenderVisualComponent> visualStateLookup,
        ComponentLookup<UnitAirMovement> airLookup,
        ComponentLookup<UnitAirComponent> airStateLookup,
        ComponentLookup<LocalTransform> transformLookup,
        bool shouldSpin,
        int detailRotated,
        int modelRotated,
        int bakedRotated,
        float deltaTime)
    {
        string source = sourceLookup.HasComponent(entity) ? sourceLookup[entity].Value.ToString() : "<none>";
        string display = displayLookup.HasComponent(entity) ? displayLookup[entity].Name.ToString() : "<none>";
        string visual = visualStateLookup.HasComponent(entity)
            ? $"{(UnitRenderVisualKind)visualStateLookup[entity].Current}->{(UnitRenderVisualKind)visualStateLookup[entity].Desired}"
            : "<none>";
        bool hasAir = airLookup.HasComponent(entity);
        string airState = FormatAirState(entity, airStateLookup, transformLookup);
        int bakedCount = bladeLookup.HasBuffer(entity) ? bladeLookup[entity].Length : 0;
        Entity detailRoot = detailLookup.HasComponent(entity) ? detailLookup[entity].Root : Entity.Null;
        Entity modelRoot = modelLookup.HasComponent(entity) ? modelLookup[entity].Instance : Entity.Null;
        Entity midRoot = midLookup.HasComponent(entity) ? midLookup[entity].Instance : Entity.Null;
        Entity lowRoot = lowLookup.HasComponent(entity) ? lowLookup[entity].Instance : Entity.Null;

        Debug.Log(
            "[HeliBladeDiag] " +
            $"unit={FormatEntity(entity)} source={source} display={display} air={hasAir} spin={shouldSpin} state={airState} visual={visual} dt={deltaTime:F4} " +
            $"detail={FormatRoot(em, childLookup, detailRoot)} detailRot={detailRotated} " +
            $"model={FormatRoot(em, childLookup, modelRoot)} modelRot={modelRotated} " +
            $"mid={FormatRoot(em, childLookup, midRoot)} " +
            $"low={FormatRoot(em, childLookup, lowRoot)} " +
            $"bakedBlades={bakedCount} bakedRot={bakedRotated}");
    }

    private static string FormatAirState(
        Entity entity,
        ComponentLookup<UnitAirComponent> airStateLookup,
        ComponentLookup<LocalTransform> transformLookup)
    {
        if (!airStateLookup.HasComponent(entity))
            return "<none>";

        UnitAirComponent airState = airStateLookup[entity];
        float currentY = transformLookup.HasComponent(entity) ? transformLookup[entity].Position.y : float.NaN;
        return $"homeInit={airState.HomeInitialized}:airborne={airState.Airborne}:takeoff={airState.TakeoffRolling}:landing={airState.LandingRolling}:returning={airState.ReturningHome}:y={currentY:F2}:homeY={airState.HomePosition.y:F2}";
    }

    private static string FormatRoot(EntityManager em, BufferLookup<Child> childLookup, Entity root)
    {
        if (root == Entity.Null)
            return "null";
        if (!em.Exists(root))
            return $"{FormatEntity(root)} missing";

        string name = em.GetName(root).ToString();
        bool disabled = em.HasComponent<Disabled>(root);
        bool hidden = em.HasComponent<DisableRendering>(root);
        bool culled = em.HasComponent<UnitRenderBudgetCulledTag>(root);
        int directChildren = childLookup.HasBuffer(root) ? childLookup[root].Length : 0;
        int bladeCount = CountBladeDescendants(em, childLookup, root);
        return $"{FormatEntity(root)}:{name}:disabled={disabled}:hidden={hidden}:culled={culled}:children={directChildren}:blades={bladeCount}";
    }

    private static int CountBladeDescendants(EntityManager em, BufferLookup<Child> childLookup, Entity root)
    {
        if (root == Entity.Null || !em.Exists(root))
            return 0;

        int count = 0;
        using var stack = new NativeList<Entity>(Allocator.Temp);
        stack.Add(root);
        while (stack.Length > 0)
        {
            Entity current = stack[stack.Length - 1];
            stack.RemoveAt(stack.Length - 1);

            FixedString64Bytes name = em.GetName(current);
            if (TryGetBladeAxis(name.ToString(), out _))
                count++;

            if (!childLookup.HasBuffer(current))
                continue;

            DynamicBuffer<Child> children = childLookup[current];
            for (int i = 0; i < children.Length; i++)
                stack.Add(children[i].Value);
        }

        return count;
    }

    private static string FormatEntity(Entity entity)
    {
        return $"{entity.Index}:{entity.Version}";
    }
}
