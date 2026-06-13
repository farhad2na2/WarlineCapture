#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class MapSurfaceDiagnosticsSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MapSurfaceDiagnosticsSystemTests();
            tests.DiagnosticsSystemAddsAndUpdatesDiagnosticsComponent();
            Debug.Log("[MapSurfaceDiagnosticsFocusedValidation] result=Passed tests=1");
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MapSurfaceDiagnosticsFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void DiagnosticsSystemAddsAndUpdatesDiagnosticsComponent()
    {
        using World world = new("MapSurfaceDiagnosticsSystemTests");
        EntityManager em = world.EntityManager;
        Entity surfaceEntity = em.CreateEntity(typeof(MapSurfaceComponent));
        em.SetComponentData(surfaceEntity, new MapSurfaceComponent
        {
            Dimensions = new int2(8, 6),
            HasSurfaceData = 0
        });

        SystemHandle endSimulationEcbSystem = world.CreateSystem<EndSimulationEntityCommandBufferSystem>();
        SystemHandle system = world.CreateSystem<MapSurfaceDiagnosticsSystem>();
        system.Update(world.Unmanaged);
        endSimulationEcbSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<MapSurfaceDiagnosticsComponent>(surfaceEntity));
        MapSurfaceDiagnosticsComponent diagnostics =
            em.GetComponentData<MapSurfaceDiagnosticsComponent>(surfaceEntity);
        Assert.AreEqual(0, diagnostics.HasSurfaceData);
        Assert.AreEqual(0, diagnostics.CellCount);

        em.SetComponentData(surfaceEntity, new MapSurfaceComponent
        {
            Dimensions = new int2(10, 6),
            HasSurfaceData = 0
        });
        for (int i = 0; i < 80; i++)
            system.Update(world.Unmanaged);

        diagnostics = em.GetComponentData<MapSurfaceDiagnosticsComponent>(surfaceEntity);
        Assert.AreEqual(0, diagnostics.HasSurfaceData);
        Assert.AreEqual(0, diagnostics.CellCount);
    }
}
#endif
