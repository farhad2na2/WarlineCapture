#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Movement;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class GridPathMovementKernelTests
    {
        public static void RunChecks()
        {
            var tests = new GridPathMovementKernelTests();
            tests.OriginalSourceAndOracleArePinned();
            tests.ManagedActualAdapterMatchesOriginalForSeededAndBoundaryPaths();
            tests.BurstKernelMatchesOriginalWithActualCompiledExecution();
            tests.RangeGroupingBlockerAndStallPoliciesMatchOriginal();
            new UnitGroundMovementPauseTests().NativeMovementConservesPausedStateAndRetainsPositiveTimeBehavior();
            var existing = new global::UnitMovementBlockerValidationTests();
            existing.UnitMovementTargetRejectsBuildingBlockerCells();
            existing.InfantryMovementDoesNotStallOnOwnPreviousOccupancySnapshot();
            existing.VehicleTurnInPlaceDoesNotRepathBeforeAlignmentWindow();
            existing.InfantryOpenPathMovementAdvancesEveryFrame();
            Debug.Log("[GridPathMovementParity] result=Passed parityTests=4 existingMovementTests=5 missionReady=0");
        }

        public static void RunFocusedValidation() => RunEntry(false);
        public static void RunCombinedValidation() => RunEntry(true);
        private static void RunEntry(bool combined)
        {
            try
            {
                RunChecks();
                if (combined)
                {
                    DirectDamageSequencerTests.RunChecks();
                    var attack = new StandardAttackPlannerTests();
                    attack.OriginalOracleIsPinnedVerbatimProduction();
                    attack.InstalledMathNaNEqualAndClampSemanticsMatchKernel();
                    attack.ManagedRandomAndBoundaryPlansMatchEveryField();
                    attack.BurstScheduledPlansMatchOriginalAndManagedResults();
                    attack.ActualPrivateRuntimeAdapterPreservesEveryPlanField();
                    var combat = new global::UnitCombatFocusedEditModeTests();
                    combat.StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();
                    combat.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
                    combat.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();
                    combat.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();
                    combat.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();
                    combat.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
                    Debug.Log("[SharedCombatMovementParity] result=Passed movementParity=4 movementRegressions=5 damageParity=5 attackParity=5 combatRegressions=6 missionReady=0");
                }
                ValidationExit.Passed();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[SharedCombatMovementParity] result=Failed missionReady=0");
                ValidationExit.Failed();
            }
        }

        [Test]
        public void OriginalSourceAndOracleArePinned()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string source = ReadPinned(root, "GridPathMovementOriginalUnitGridMovementSystem.cs.txt", "cfd49d0e66820fc50aaf21aee4b2a2c60e95382a6cbea13671cae509c2393ede");
            ReadPinned(root, "GridPathMovementOriginalUnitGridMovementPolicy.cs.txt", "83a7afce24fb5b6c6053b84001167e2751daead4028f0d449548f93a4bf7a3d4");
            int a = source.IndexOf("        private void MoveInfantryAlongPath(", StringComparison.Ordinal);
            int b = source.IndexOf("\n        internal static bool ShouldBlockPathCell", a, StringComparison.Ordinal);
            Assert.GreaterOrEqual(a, 0); Assert.Greater(b, a);
            StringAssert.Contains(source.Substring(a, b - a), File.ReadAllText(Path.Combine(root, "Assets/Tests/Editor/GridPathMovementOriginalOracle.cs")).Replace("\r\n", "\n"));
        }
        private static string ReadPinned(string root, string name, string expected)
        {
            byte[] raw = File.ReadAllBytes(Path.Combine(root, "Assets/Tests/Editor", name));
            string canonical = System.Text.Encoding.UTF8.GetString(raw).Replace("\r\n", "\n");
            using var sha = SHA256.Create();
            string Hash(byte[] bytes) => BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            Assert.AreEqual(expected, Hash(System.Text.Encoding.UTF8.GetBytes(canonical)));
            Debug.Log($"[GridPathMovementOracle] file={name} rawSha256={Hash(raw)} canonicalLfSha256={expected}");
            return canonical;
        }

        private struct Case
        {
            public GridConfig Grid;
            public LocalTransform Transform;
            public UnitGrid UnitGrid;
            public UnitPathFollow Follow;
            public UnitPathRange Range;
            public UnitVehicleKinematics Kinematics;
            public UnitMove Move;
            public float Speed, DeltaTime;
            public byte Grouped;
        }
        private struct Snapshot
        {
            public LocalTransform Transform;
            public UnitGrid Grid;
            public UnitPathFollow Follow;
            public UnitVehicleKinematics Kinematics;
        }

        [Test]
        public void ManagedActualAdapterMatchesOriginalForSeededAndBoundaryPaths()
        {
            var cases = Cases();
            using var pool = new NativeArray<int2>(16, Allocator.TempJob);
            FillPool(pool);
            var method = typeof(UnitGridMoveJob).GetMethod("MoveInfantryAlongPath", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            for (int caseIndex = 0; caseIndex < cases.Length; caseIndex++)
            {
                var value = cases[caseIndex];
                var expected = Original(value, pool);
                object job = new UnitGridMoveJob { Grid = value.Grid, Pool = pool, DeltaTime = value.DeltaTime };
                object[] args = { value.Transform, value.UnitGrid, value.Follow, value.Kinematics, value.Move, value.Range, value.Speed, value.Grouped != 0 };
                method.Invoke(job, args);
                var actual = new Snapshot { Transform = (LocalTransform)args[0], Grid = (UnitGrid)args[1], Follow = (UnitPathFollow)args[2], Kinematics = (UnitVehicleKinematics)args[3] };
                Compare(expected, actual, "actual private production adapter case=" + caseIndex + " " + JsonUtility.ToJson(value));
                Compare(expected, Kernel(value, pool), "value-kernel adapter");
            }
        }

        [Test]
        public void BurstKernelMatchesOriginalWithActualCompiledExecution()
        {
            Assert.IsTrue(BurstCompiler.IsEnabled, "Burst disabled: no execution credit");
            using var cases = new NativeArray<Case>(Cases(), Allocator.TempJob);
            using var pool = new NativeArray<int2>(16, Allocator.TempJob); FillPool(pool);
            using var originals = new NativeArray<Snapshot>(cases.Length, Allocator.TempJob);
            using var kernels = new NativeArray<Snapshot>(cases.Length, Allocator.TempJob);
            using var compiled = new NativeArray<byte>(cases.Length, Allocator.TempJob);
            new ParityJob { Cases = cases, Pool = pool, Originals = originals, Kernels = kernels, Compiled = compiled }.Schedule(cases.Length, 64).Complete();
            int originalBackendPositionDifferences = 0, originalBackendRotationDifferences = 0;
            for (int i = 0; i < cases.Length; i++)
            {
                Assert.AreEqual(1, compiled[i], "Managed fallback cannot receive Burst credit");
                Compare(originals[i], kernels[i], "Burst original/kernel " + i);
                var managedOriginal = Original(cases[i], pool);
                Compare(managedOriginal, Kernel(cases[i], pool), "managed original/managed kernel " + i);
                // Preserve strict same-backend parity. The original quaternion/math
                // operations themselves can differ by bits between Mono and Burst.
                for (int axis = 0; axis < 3; axis++)
                    if (math.asint(managedOriginal.Transform.Position[axis]) != math.asint(originals[i].Transform.Position[axis])) originalBackendPositionDifferences++;
                for (int axis = 0; axis < 4; axis++)
                    if (math.asint(managedOriginal.Transform.Rotation.value[axis]) != math.asint(originals[i].Transform.Rotation.value[axis])) originalBackendRotationDifferences++;
            }
            Debug.Log($"[GridPathMovementBackendBaseline] cases={cases.Length} originalMonoVsBurstPositionFields={originalBackendPositionDifferences} originalMonoVsBurstRotationFields={originalBackendRotationDifferences} sameBackendParity=Bitwise");
        }
        [BurstCompile(CompileSynchronously = true)]
        private struct ParityJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Case> Cases;
            [ReadOnly] public NativeArray<int2> Pool;
            public NativeArray<Snapshot> Originals, Kernels;
            public NativeArray<byte> Compiled;
            [BurstDiscard] private static void ManagedOnly(ref bool value) { value = false; }
            public void Execute(int index)
            {
                bool compiled = true; ManagedOnly(ref compiled); Compiled[index] = compiled ? (byte)1 : (byte)0;
                Originals[index] = Original(Cases[index], Pool);
                Kernels[index] = Kernel(Cases[index], Pool);
            }
        }

        [Test]
        public void RangeGroupingBlockerAndStallPoliciesMatchOriginal()
        {
            foreach (int start in new[] { int.MinValue, -1, 0, 1, 15, 16, int.MaxValue })
            foreach (int length in new[] { -1, 0, 1, 2, 16, int.MaxValue })
            foreach (int index in new[] { -1, 0, 1, 16, int.MaxValue })
            {
                var expected = length <= 0 || (uint)index >= (uint)length ? GridPathRangeDecision.NoWork :
                    start < 0 || start > 16 || length > 16 - start ? GridPathRangeDecision.InvalidRange : GridPathRangeDecision.Ready;
                Assert.AreEqual(expected, GridPathMovementKernel.EvaluateRange(start, length, index, 16));
            }
            for (int flags = 0; flags < 16; flags++)
            {
                bool vehicle = (flags & 1) != 0, guided = (flags & 2) != 0, group = (flags & 4) != 0, boarding = (flags & 8) != 0;
                Assert.AreEqual(!vehicle && !guided && group && !boarding, GridPathMovementKernel.ShouldUseGroupedManualStop(vehicle, guided, group, boarding));
                Assert.AreEqual(!guided && group, GridPathMovementKernel.ShouldBlockPathCell(guided, group));
            }
            foreach (bool vehicle in new[] { false, true })
            foreach (float stall in new[] { -.1f, 0f, .349f, .35f, .351f, float.NaN, float.PositiveInfinity })
            foreach (float dt in new[] { -.1f, 0f, .001f, .016f, 1f })
            {
                float expectedSpeed = 3f, expectedStall = stall;
                var expected = GridPathOccupancyDecision.Repath;
                if (!vehicle)
                { expectedSpeed = 0; expectedStall += dt; if (expectedStall < .35f) expected = GridPathOccupancyDecision.Wait; else expectedStall = 0; }
                float actualSpeed = 3f, actualStall = stall;
                Assert.AreEqual(expected, GridPathMovementKernel.AdvanceOccupiedStall(vehicle, dt, ref actualSpeed, ref actualStall));
                Float(expectedSpeed, actualSpeed, "occupied speed"); Float(expectedStall, actualStall, "occupied stall");
            }
            foreach (float speed in new[] { -1f, 0f, math.asfloat(unchecked((int)0x80000000)), .01f, 1f, float.NaN })
                Float(math.max(0f, speed * .1f), GridPathMovementKernel.BeginInfantryDistance(speed, .1f), "distance budget");
        }

        private static Snapshot Original(in Case input, NativeArray<int2> pool)
        {
            var result = new Snapshot { Transform = input.Transform, Grid = input.UnitGrid, Follow = input.Follow, Kinematics = input.Kinematics };
            new GridPathMovementOriginalOracle { Grid = input.Grid, Pool = pool, DeltaTime = input.DeltaTime }.Run(
                ref result.Transform, ref result.Grid, ref result.Follow, ref result.Kinematics, input.Move, input.Range, input.Speed, input.Grouped != 0);
            return result;
        }

        // Scalar integration counterpart. Grid conversions and quaternion construction remain
        // the exact native adapter operations; no blocker/pathfinding authority is fabricated.
        private static Snapshot Kernel(in Case input, NativeArray<int2> pool)
        {
            var result = new Snapshot { Transform = input.Transform, Grid = input.UnitGrid, Follow = input.Follow, Kinematics = input.Kinematics };
            result.Kinematics.CurrentSpeed = 0; result.Kinematics.StallSeconds = 0;
            var state = new InfantryPathState { X = input.Transform.Position.x, Y = input.Transform.Position.y, Z = input.Transform.Position.z,
                CellX = input.UnitGrid.Cell.x, CellZ = input.UnitGrid.Cell.y, PathIndex = input.Follow.PathIndex,
                RemainingDistance = GridPathMovementKernel.BeginInfantryDistance(input.Speed, input.DeltaTime) };
            for (int i = 0; i < GridPathMovementKernel.InfantryMaxWaypointAdvancesPerFrame && state.RemainingDistance > 0; i++)
            {
                if ((uint)state.PathIndex >= (uint)input.Range.Length) return result;
                int at = input.Range.Start + state.PathIndex, finalAt = input.Range.Start + input.Range.Length - 1;
                if ((uint)at >= (uint)pool.Length || (uint)finalAt >= (uint)pool.Length) return result;
                int2 target = pool[at]; var t = GridUtils.CellToWorldCenter(input.Grid, target); var f = GridUtils.CellToWorldCenter(input.Grid, pool[finalAt]);
                var step = GridPathMovementKernel.StepInfantryWaypoint(state, new InfantryWaypointInput { PathLength = input.Range.Length,
                    PoolIndexValid = true, FinalPoolIndexValid = true, GroupedManualMove = input.Grouped != 0,
                    TargetCellX = target.x, TargetCellZ = target.y, TargetX = t.x, TargetZ = t.z,
                    FinalTargetX = f.x, FinalTargetZ = f.z, ArriveDistance = input.Move.ArriveDistance, CellSize = input.Grid.CellSize });
                state = step.State; result.Transform.Position = new float3(state.X, state.Y, state.Z);
                result.Grid.Cell = new int2(state.CellX, state.CellZ); result.Follow.PathIndex = state.PathIndex;
                if (step.WriteFacing) result.Transform.Rotation = quaternion.LookRotationSafe(new float3(step.DirectionX, step.DirectionY, step.DirectionZ), math.up());
                if (step.SettleCurrentCell)
                { int2 settled = GridUtils.WorldToCell(input.Grid, result.Transform.Position); if (GridUtils.InBounds(settled, input.Grid.Width, input.Grid.Height) && !settled.Equals(result.Grid.Cell)) result.Grid.Cell = settled; }
                if (step.Action == InfantryWaypointAction.Stop || step.Action == InfantryWaypointAction.GroupedStop) return result;
            }
            return result;
        }

        private static void FillPool(NativeArray<int2> pool)
        {
            for (int i = 0; i < pool.Length; i++) pool[i] = new int2(4 + i / 2, 4 + i / 3);
        }
        private static Case Ready()
        {
            return new Case { Grid = new GridConfig { Width = 64, Height = 64, CellSize = 1, Origin = float3.zero },
                Transform = LocalTransform.FromPositionRotationScale(new float3(3.5f, 7f, 4.5f), quaternion.RotateY(.37f), 1.3f),
                UnitGrid = new UnitGrid { Cell = new int2(3, 4) }, Follow = new UnitPathFollow { PathIndex = 0 },
                Range = new UnitPathRange { Start = 0, Length = 12 }, Move = new UnitMove { ArriveDistance = .05f },
                Kinematics = new UnitVehicleKinematics { CurrentSpeed = 3f, StallSeconds = .2f }, Speed = 3, DeltaTime = .1f };
        }
        private static Case[] Cases()
        {
            var result = new List<Case>(); var random = new System.Random(9021047);
            float F(float a, float b) => a + (float)random.NextDouble() * (b - a);
            for (int i = 0; i < 1024; i++)
            {
                var c = Ready(); c.Range.Start = random.Next(0, 3); c.Range.Length = random.Next(1, 14);
                c.Follow.PathIndex = random.Next(0, c.Range.Length); c.Grouped = (byte)random.Next(0, 2);
                c.Transform.Position = new float3(F(0, 20), F(-8, 8), F(0, 20));
                c.Grid.Origin = new float3(F(-3, 3), F(-1, 1), F(-3, 3)); c.Grid.CellSize = F(.25f, 3);
                c.Speed = F(-1, 30); c.DeltaTime = F(0, 2); c.Move.ArriveDistance = F(-.1f, 2); result.Add(c);
            }
            foreach (float dt in new[] { -1f, 0f, .016f, .1f, 1f, 10f })
            foreach (float speed in new[] { -1f, 0f, .00001f, 1f, 100f })
            { var c = Ready(); c.DeltaTime = dt; c.Speed = speed; result.Add(c); }
            foreach (float distance in new[] { 0f, .0001f, .05f, math.asfloat(math.asint(.05f)-1), math.asfloat(math.asint(.05f)+1), 1.25f, math.asfloat(math.asint(1.25f)-1), math.asfloat(math.asint(1.25f)+1) })
            foreach (byte grouped in new byte[] { 0, 1 })
            { var c = Ready(); c.Range.Length = 1; c.Transform.Position = new float3(4.5f-distance, -0f, 4.5f); c.Grouped = grouped; result.Add(c); }
            foreach (int index in new[] { -1, 0, 11, 12, int.MaxValue })
            { var c = Ready(); c.Follow.PathIndex = index; result.Add(c); }
            foreach (int start in new[] { -1, 5, 16, int.MaxValue })
            { var c = Ready(); c.Range.Start = start; result.Add(c); }
            return result.ToArray();
        }
        private static void Float(float expected, float actual, string label)
        { if (float.IsNaN(expected)) Assert.IsTrue(float.IsNaN(actual), label); else Assert.AreEqual(math.asint(expected), math.asint(actual), label); }
        private static void Compare(in Snapshot expected, in Snapshot actual, string label)
        {
            for (int i = 0; i < 3; i++) Float(expected.Transform.Position[i], actual.Transform.Position[i], label + " position" + i);
            for (int i = 0; i < 4; i++) Float(expected.Transform.Rotation.value[i], actual.Transform.Rotation.value[i], label + " rotation" + i);
            Float(expected.Transform.Scale, actual.Transform.Scale, label + " scale");
            Float(expected.Kinematics.CurrentSpeed, actual.Kinematics.CurrentSpeed, label + " speed");
            Float(expected.Kinematics.StallSeconds, actual.Kinematics.StallSeconds, label + " stall");
            Assert.AreEqual(expected.Grid.Cell, actual.Grid.Cell, label + " cell"); Assert.AreEqual(expected.Follow.PathIndex, actual.Follow.PathIndex, label + " follow");
        }
    }
}
#endif
