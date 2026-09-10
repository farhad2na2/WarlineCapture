using System;
using Game.Runtime;
using Unity.Mathematics;
using UnityEngine;
using NUnit.Framework;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using Unity.Entities;
using Unity.Transforms;
using System.Reflection;

public sealed class MissionCameraOverviewTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            new MissionCameraOverviewTests().BothMissionOverviewsKeepSubjectsOutsideHudOcclusion();
            new MissionCameraOverviewTests().ExtractionFocusUsesTheMovingSubjectsAndTransport();
            Debug.Log("[MissionCameraOverview] result=Passed missions=2 aspects=2 liveExtractionFocus=true");
            ValidationExit.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("[MissionCameraOverview] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void BothMissionOverviewsKeepSubjectsOutsideHudOcclusion()
    {
        float3[][] missions = {
            new[] { new float3(930,1,402), new float3(937,1,348), new float3(876,1,426) },
            new[] { new float3(960,1,426), new float3(801,1,427), new float3(1029,1,428) }
        };
        var go = new GameObject("Mission overview projection check");
        using var world = new World(nameof(BothMissionOverviewsKeepSubjectsOutsideHudOcclusion));
        var requests = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
        var controller = world.GetOrCreateSystemManaged<RtsCameraSystem>();
        var boundsEntity = world.EntityManager.CreateEntity(typeof(ActiveOperationMapComponent), typeof(OperationMapBoundsComponent));
        try
        {
            Camera camera = go.AddComponent<Camera>();
            foreach (float aspect in new[] { 16f/9f, 20f/9f })
            foreach (var subjects in missions)
            {
                var request = CampaignMissionSpawnSystem.CreateMissionOverviewRequest(subjects[0], subjects[1], subjects[2]);
                world.EntityManager.SetComponentData(boundsEntity, new OperationMapBoundsComponent {
                    CameraMin = Game.Editor.MissionCameraBoundsAuthoring.Minimum,
                    CameraMax = Game.Editor.MissionCameraBoundsAuthoring.Maximum });
                var p = request.Perspective;
                camera.aspect = aspect;
                controller.MatchIntroZoomSettlePending = true;
                controller.IsZoomTransitionActive = true;
                controller.SetSmoothPerspectiveTarget(44, 40, 10, 36, .1f, true);
                RuntimeCameraFocusRequestUtility.Queue(requests, world.EntityManager, request, request.World, 44, 40, 10, 36);
                requests.ProcessPendingRequests(world.EntityManager, controller, camera, null);
                Assert.IsFalse(controller.MatchIntroZoomSettlePending, "The generic intro must not overwrite an authored mission camera.");
                Assert.IsFalse(controller.IsZoomTransitionActive);
                Assert.IsFalse(controller.HasSmoothPerspectiveTarget);
                Assert.That(camera.transform.position.y, Is.GreaterThan(40));
                foreach (var subject in subjects)
                {
                    Vector3 viewport = camera.WorldToViewportPoint(subject);
                    Assert.That(viewport.z, Is.GreaterThan(0));
                    Assert.That(viewport.x, Is.InRange(.26f,.74f), $"subject={subject}, aspect={aspect}");
                    Assert.That(viewport.y, Is.InRange(.18f,.88f), $"subject={subject}, aspect={aspect}");
                }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void ExtractionFocusUsesTheMovingSubjectsAndTransport()
    {
        using var world = new World(nameof(ExtractionFocusUsesTheMovingSubjectsAndTransport));
        var em = world.EntityManager;
        Entity root = em.CreateEntity();
        Entity passenger = CreateActor(em, new float3(801,1,427));
        Entity carrier = CreateActor(em, new float3(1000,1,428));
        Entity aircraft = CreateActor(em, new float3(1080,12,450));
        em.AddBuffer<CampaignMissionExtractionMember>(root).Add(new CampaignMissionExtractionMember { Entity = passenger, Kind = 1 });
        var extraction = new CampaignMissionExtractionState { Carrier = carrier, Aircraft = aircraft,
            LandingCenter = new float3(1029,1,428), DepartureCenter = new float3(1090,1,465) };
        var resolve = typeof(UiShellEcsGateway).GetMethod("ResolveExtractionCameraTarget", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(resolve);
        float3 Focus(UiMissionExtractionAction action, int lesson) =>
            (float3)resolve.Invoke(null, new object[] { em, root, extraction, action, lesson });
        Assert.AreEqual(new float3(1000,1,428), Focus(UiMissionExtractionAction.ShowLesson, 2));
        em.SetComponentData(passenger, LocalTransform.FromPosition(new float3(820,1,430)));
        Assert.AreEqual(new float3(820,1,430), Focus(UiMissionExtractionAction.FocusTeam, 3));
        em.AddComponentData(passenger, new UnitTransportPassenger { Transport = carrier });
        Assert.AreEqual(new float3(1000,1,428), Focus(UiMissionExtractionAction.FocusTeam, 6), "Must not show the empty pickup anchor.");
        Assert.AreEqual(new float3(1080,12,450), Focus(UiMissionExtractionAction.ShowLesson, 11));
        Assert.AreEqual(extraction.DepartureCenter, Focus(UiMissionExtractionAction.FocusDeparture, 11));
    }

    private static Entity CreateActor(EntityManager em, float3 position)
    {
        var actor = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
        em.SetComponentData(actor, LocalTransform.FromPosition(position));
        em.SetComponentData(actor, new UnitHealth { Current = 100, Max = 100 });
        return actor;
    }
}
