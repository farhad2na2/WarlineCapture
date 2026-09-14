using System;
using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class OperationMapVehicleVisualOwnershipTests
{
    [Test]
    public void DormantHelicopterKeepsOneBlueTreeAndUnrelatedAttachments()
    {
        using var world = new World("Dormant helicopter ownership");
        var em = world.EntityManager;
        var mesh = new Mesh();
        try
        {
            Entity unit = em.CreateEntity(typeof(OperationMapAuthoredVehiclePresentation), typeof(UnitDetailedVisualReference), typeof(Disabled));
            Entity original = Renderer(em, mesh, 0), detail = Renderer(em, mesh, 0), attachment = Renderer(em, mesh, 3);
            Entity originalRotor = Renderer(em, mesh, 1), detailRotor = Renderer(em, mesh, 1);
            em.SetComponentData(unit, new UnitDetailedVisualReference { Root = detail });
            var children = em.AddBuffer<Child>(unit);
            children.Add(new Child { Value = original }); children.Add(new Child { Value = detail }); children.Add(new Child { Value = attachment });
            em.AddBuffer<Child>(original).Add(new Child { Value = originalRotor });
            em.AddBuffer<Child>(detail).Add(new Child { Value = detailRotor });
            var blue = new float4(.12f, .72f, 1, 1);
            em.AddComponentData(original, new FactionTintColor { Value = blue });
            em.AddComponentData(detail, new URPMaterialPropertyBaseColor { Value = new float4(1) });
            world.GetOrCreateSystem<OperationMapVehicleVisualOwnershipSystem>().Update(world.Unmanaged);
            Assert.IsTrue(em.HasComponent<Disabled>(unit), "Rendering repair must not activate dormant gameplay.");
            Assert.IsTrue(em.HasComponent<DisableRendering>(original));
            Assert.IsTrue(em.HasComponent<DisableRendering>(originalRotor));
            Assert.IsFalse(em.HasComponent<DisableRendering>(detail));
            Assert.IsFalse(em.HasComponent<DisableRendering>(detailRotor));
            Assert.IsFalse(em.HasComponent<DisableRendering>(attachment), "A separate same-mesh attachment must survive.");
            Assert.AreEqual(blue, em.GetComponentData<FactionTintColor>(detail).Value);
            Assert.IsFalse(em.HasComponent<URPMaterialPropertyBaseColor>(detail), "_BaseColor must have one property owner.");
            world.GetOrCreateSystem<OperationMapVehicleVisualOwnershipSystem>().Update(world.Unmanaged);
            Assert.IsTrue(em.HasComponent<DisableRendering>(original));
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }

    private static Entity Renderer(EntityManager em, Mesh mesh, float x)
    {
        var e = em.CreateEntity(typeof(MaterialMeshInfo), typeof(LocalToWorld));
        em.SetComponentData(e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        em.SetComponentData(e, new LocalToWorld { Value = float4x4.Translate(new float3(x, 0, 0)) });
        em.AddSharedComponentManaged(e, new RenderMeshArray(new Material[] { null }, new[] { mesh }));
        return e;
    }

    public static void RunFocusedValidation()
    {
        try
        {
            new OperationMapVehicleVisualOwnershipTests().DormantHelicopterKeepsOneBlueTreeAndUnrelatedAttachments();
            Debug.Log("[M02HelicopterVisualOwnership] result=Passed dormant,duplicates,rotors,tint,attachments,idempotence");
            ValidationExit.Passed();
        }
        catch (Exception e) { Debug.LogException(e); ValidationExit.Failed(); }
    }
}
