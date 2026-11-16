extern alias ActualCore;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Game.Runtime;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Quaternion = ActualCore::UnityEngine.Quaternion;

internal static class BuildingActorFactoryCleanupControl
{
    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static int CountLive(EntityManager em)
    {
        using var all = em.CreateEntityQuery(new EntityQueryDesc
        {
            Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
        });
        using var entities = all.ToEntityArray(Allocator.Temp);
        return entities.Length;
    }

    static Type CreateLegacyBirthProviderType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("BuildingActorFactoryCleanupFaultInjection"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("FaultInjection");
        var type = module.DefineType(
            "Game.Runtime.RuntimeActorBirthSourceProvider",
            TypeAttributes.Public | TypeAttributes.Class);
        type.DefineDefaultConstructor(MethodAttributes.Public);
        return type.CreateTypeInfo().AsType();
    }

    public static int Main()
    {
        try
        {
            int nextEntity = 1;
            var em = new EntityManager { LogicalAllocatorAdmitted = true };
            em.AllocateLogicalIdentity = () => new Entity { Index = nextEntity++, Version = 1 };
            Entity existing = em.CreateEntity();
            Type providerType = CreateLegacyBirthProviderType();
            MethodInfo add = typeof(EntityManager).GetMethod("AddComponent", BindingFlags.Instance | BindingFlags.NonPublic);
            Need(add != null, "source-host EntityManager AddComponent injection seam missing");
            add.Invoke(em, new[] { (object)existing, providerType, Activator.CreateInstance(providerType) });

            var context = new BuildingRuntimeEntityCompositionSystemHelper.Context
            {
                TryGetEntityManager = delegate (out EntityManager manager) { manager = em; return true; },
                TryGetGridData = delegate (out Entity gridEntity, out GridConfig grid,
                    out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockers)
                {
                    gridEntity = existing;
                    grid = default;
                    roads = default;
                    blockers = default;
                    return true;
                },
                GetFootprintCenter = (origin, footprint, grid) => Vector3.zero
            };
            var helper = new BuildingRuntimeEntityCompositionSystemHelper();

            int beforeBlocker = CountLive(em);
            Exception blockerFailure = null;
            try
            {
                helper.CreateBlockerEntity(context, null, new Vector2Int(2, 3), new Vector2Int(1, 1),
                    new SourceBuildingBirthContext(true));
            }
            catch (Exception error) { blockerFailure = error; }
            Need(blockerFailure is NotSupportedException && blockerFailure.Message == "Legacy birth provider unsupported",
                "blocker factory did not preserve the injected StampFresh exception");
            Need(CountLive(em) == beforeBlocker, "blocker factory leaked its partially initialized entity");

            int beforeCombat = CountLive(em);
            var definition = new RuntimeDefinition
            {
                DisplayName = new string('x', 62),
                FootprintCells = new Vector2Int(1, 1),
                MaxHealth = 1
            };
            Exception combatFailure = null;
            try
            {
                helper.CreateBuildingCombatEntity(context, 7, new Vector2Int(1, 1), definition, 1,
                    Quaternion.identity);
            }
            catch (Exception error) { combatFailure = error; }
            Need(combatFailure is ArgumentException,
                "combat factory did not preserve the injected FixedString component-construction exception");
            Need(CountLive(em) == beforeCombat, "combat factory leaked its partially initialized entity");

            Console.WriteLine("[BuildingActorFactoryCleanupControl] result=Passed blocker_stamp_failure=cleaned combat_component_failure=cleaned original_exceptions=preserved fixture_only=1 readiness=0");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
