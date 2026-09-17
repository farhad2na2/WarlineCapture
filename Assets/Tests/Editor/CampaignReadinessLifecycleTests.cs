using System;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public sealed class CampaignReadinessLifecycleTests
{
    [Test]
    public void ClonedDisabledControlRecoversItsNormalMaterialWithoutARestrictionOwner()
    {
        var assembly = typeof(Game.UI.Runtime.MatchOverlayCommandControlsView).Assembly;
        var utility = assembly.GetType("Game.UI.Runtime.UiDisabledMaterialUtility");
        var reasonType = assembly.GetType("Game.UI.Runtime.UiDisabledVisualReason");
        var set = utility.GetMethod("SetDisabled", BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(GameObject), reasonType, typeof(bool) }, null);
        var mission = Enum.Parse(reasonType, "MissionRestriction");
        var cinematic = Enum.Parse(reasonType, "CinematicInteractionLock");
        var original = new GameObject("Build", typeof(RectTransform), typeof(Image));
        GameObject clone = null;
        try
        {
            set.Invoke(null, new[] { original, mission, (object)true });
            clone = UnityEngine.Object.Instantiate(original);
            set.Invoke(null, new[] { clone, mission, (object)false });
            Assert.That(clone.GetComponent<Image>().material.shader.name, Is.Not.EqualTo("Warline/UI/Disabled Grayscale"));
            set.Invoke(null, new[] { original, cinematic, (object)true });
            set.Invoke(null, new[] { original, mission, (object)false });
            Assert.That(original.GetComponent<Image>().material.shader.name, Is.EqualTo("Warline/UI/Disabled Grayscale"), "A remaining cinematic lock still owns the disabled visual.");
            set.Invoke(null, new[] { original, cinematic, (object)false });
            Assert.That(original.GetComponent<Image>().material.shader.name, Is.Not.EqualTo("Warline/UI/Disabled Grayscale"));
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); UnityEngine.Object.DestroyImmediate(original); }
    }

    [Test]
    public void PathMaintenanceIgnoresRetiredGridUntilItsCleanupRuns()
    {
        using var world = new World("Readiness retiring and live grids");
        using var oldCells = new NativeList<int2>(4, Allocator.TempJob);
        using var liveCells = new NativeList<int2>(4, Allocator.TempJob);
        oldCells.Add(new int2(1, 1)); liveCells.Add(new int2(2, 2));
        var em = world.EntityManager;
        var retired = em.CreateEntity(typeof(GridConfig), typeof(PathPoolComponent));
        var live = em.CreateEntity(typeof(GridConfig), typeof(PathPoolComponent));
        em.SetComponentData(retired, new PathPoolComponent { Cells = oldCells });
        em.SetComponentData(live, new PathPoolComponent { Cells = liveCells });
        em.DestroyEntity(retired);
        world.GetOrCreateSystem<PathPoolMaintenanceSystem>().Update(world.Unmanaged);
        Assert.That(oldCells.Length, Is.EqualTo(1), "Retired storage belongs to cleanup, not active navigation.");
        Assert.That(liveCells.Length, Is.Zero);
    }

    [Test]
    public void DestroyingSceneGridReleasesNativeStorageWithoutWaitingForWorldShutdown()
    {
        using var world = new World("Readiness scene-grid lifetime");
        var em = world.EntityManager;
        var init = world.GetOrCreateSystem<DynamicBlockerInitSystem>();
        var cleanup = world.GetOrCreateSystem<RuntimeGridStorageCleanupSystem>();
        var grid = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(grid, new GridConfig { Width = 8, Height = 8, CellSize = 1 });
        init.Update(world.Unmanaged);
        var counts = em.GetComponentData<DynamicBlockerComponent>(grid).Counts;
        Assert.That(counts.Length, Is.EqualTo(64));
        em.DestroyEntity(grid);
        Assert.That(em.Exists(grid), Is.True, "Cleanup components retain ownership after scene unload.");
        cleanup.Update(world.Unmanaged);
        Assert.That(em.Exists(grid), Is.False, "All cleanup components must be removed after disposal.");
        Assert.Throws<ObjectDisposedException>(() => { _ = counts[0]; });
        cleanup.Update(world.Unmanaged); // Idempotent across menu frames.
    }
}
