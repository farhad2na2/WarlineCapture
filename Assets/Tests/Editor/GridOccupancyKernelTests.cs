#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using Game.Components;
using Game.Runtime.Navigation;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
public sealed class GridOccupancyKernelTests
{
    public static void RunChecks()
    {
        var tests = new GridOccupancyKernelTests();
        tests.BoundsMatchProductionFootprints();
        tests.BurstDeltasMatchIndependentFullRebuild();
        var existing = new global::DynamicOccupancyRebuildSystemTests();
        existing.InitialRebuildMarksUnitFootprint(); existing.ChangedUnitGridMovesOccupancy();
        existing.DeathAnimationStateRemovesUnitFootprintFromOccupancy();
        existing.StationaryUnitKeepsOccupancyAcrossUpdates(); existing.MovingOneUnitKeepsStationaryNeighborOccupied();
        existing.EqualCountReplacementReconcilesOccupancy(); existing.SameSizeReplacementGridRebuildsStationaryOccupancy();
        Debug.Log("[GridOccupancyParity] result=Passed parity=2 nativeLifecycle=7 missionReady=0");
    }
    [Test] public void BoundsMatchProductionFootprints()
    {
        var random = new System.Random(741);
        for (int i = 0; i < 4096; i++)
        {
            int2 cell = new int2(random.Next(-20, 30), random.Next(-20, 30));
            int2 size = new int2(random.Next(-2, 20), random.Next(-2, 20));
            int2 min = UnitFootprintUtility.GetMinCell(cell, size), max = min + UnitFootprintUtility.ClampSize(size);
            min = math.clamp(min, 0, 8); max = math.clamp(max, 0, 8);
            var actual = GridOccupancyKernel.Bounds(8, 8, cell.x, cell.y, size.x, size.y);
            Assert.AreEqual(min.x, actual.MinX); Assert.AreEqual(min.y, actual.MinZ);
            Assert.AreEqual(max.x, actual.MaxX); Assert.AreEqual(max.y, actual.MaxZ);
        }
    }
    private struct Storage : IGridOccupancyStorage
    {
        public NativeArray<int> Counts;
        public NativeArray<byte> Occupied;
        public int GetCount(int i) => Counts[i];
        public void SetCount(int i, int value) => Counts[i] = value;
        public void SetOccupied(int i, bool value) => Occupied[i] = (byte)(value ? 1 : 0);
    }
    [BurstCompile(CompileSynchronously = true)] private struct DeltaJob : IJob
    {
        public NativeArray<OccupancyBounds> Bounds;
        public NativeArray<int> Counts, History;
        public NativeArray<byte> Occupied, MaskHistory, Compiled;
        [BurstDiscard] private static void Managed(ref byte compiled) { compiled = 0; }
        public void Execute()
        {
            byte compiled = 1; Managed(ref compiled); Compiled[0] = compiled;
            var store = new Storage { Counts = Counts, Occupied = Occupied };
            GridOccupancyKernel.Add(8, ref store, new OccupancyBounds(2, 2, 5, 5));
            GridOccupancyKernel.Add(8, ref store, new OccupancyBounds(3, 3, 6, 6));
            OccupancyBounds previous = default;
            for (int n = 0; n < Bounds.Length; n++)
            {
                GridOccupancyKernel.Update(8, ref store, previous, Bounds[n]); previous = Bounds[n];
                for (int i = 0; i < 64; i++) { History[n * 64 + i] = Counts[i]; MaskHistory[n * 64 + i] = Occupied[i]; }
            }
            GridOccupancyKernel.Remove(8, ref store, previous);
            GridOccupancyKernel.Remove(8, ref store, new OccupancyBounds(2, 2, 5, 5));
            GridOccupancyKernel.Remove(8, ref store, new OccupancyBounds(3, 3, 6, 6));
        }
    }
    [Test] public void BurstDeltasMatchIndependentFullRebuild()
    {
        Assert.IsTrue(BurstCompiler.IsEnabled);
        var random = new System.Random(746);
        var values = new OccupancyBounds[1024];
        for (int i = 0; i < values.Length; i++) values[i] = GridOccupancyKernel.Bounds(8, 8,
            random.Next(-3, 12), random.Next(-3, 12), random.Next(-1, 9), random.Next(-1, 9));
        using var bounds = new NativeArray<OccupancyBounds>(values, Allocator.TempJob);
        using var counts = new NativeArray<int>(64, Allocator.TempJob);
        using var occupied = new NativeArray<byte>(64, Allocator.TempJob);
        using var history = new NativeArray<int>(64 * values.Length, Allocator.TempJob);
        using var maskHistory = new NativeArray<byte>(64 * values.Length, Allocator.TempJob);
        using var compiled = new NativeArray<byte>(1, Allocator.TempJob);
        new DeltaJob { Bounds = bounds, Counts = counts, Occupied = occupied, History = history, MaskHistory = maskHistory, Compiled = compiled }.Schedule().Complete();
        Assert.AreEqual(1, compiled[0], "Actual compiled Burst required");
        for (int n = 0; n < values.Length; n++)
        for (int i = 0; i < 64; i++)
        {
            int x = i % 8, y = i / 8;
            int expected = (Inside(x,y,new OccupancyBounds(2,2,5,5)) ? 1 : 0) +
                (Inside(x,y,new OccupancyBounds(3,3,6,6)) ? 1 : 0) + (Inside(x,y,values[n]) ? 1 : 0);
            Assert.AreEqual(expected, history[n * 64 + i], "step=" + n + " cell=" + i);
            Assert.AreEqual(expected > 0 ? 1 : 0, maskHistory[n * 64 + i]);
        }
        for (int i = 0; i < 64; i++) { Assert.AreEqual(0, counts[i]); Assert.AreEqual(0, occupied[i]); }
    }
    private static bool Inside(int x, int y, OccupancyBounds b) => x >= b.MinX && x < b.MaxX && y >= b.MinZ && y < b.MaxZ;
}
}
#endif
