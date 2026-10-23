#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Combat;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class StandardAttackPlannerTests
    {
        private const string BaselineSha = "c9339825109bd5123470264ef9b0ad12327e3e6d36aea9f9b7cebdeb0fb18e67";
        private const string BaselineRelative = "Assets/Tests/Editor/StandardAttackPlannerOriginalOracle.cs.txt";

        public static void RunFocusedValidation()
        {
            try
            {
                var tests = new StandardAttackPlannerTests();
                tests.OriginalOracleIsPinnedVerbatimProduction();
                tests.InstalledMathNaNEqualAndClampSemanticsMatchKernel();
                tests.ManagedRandomAndBoundaryPlansMatchEveryField();
                tests.BurstScheduledPlansMatchOriginalAndManagedResults();
                tests.ActualPrivateRuntimeAdapterPreservesEveryPlanField();
                var combat = new global::UnitCombatFocusedEditModeTests();
                combat.StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();
                combat.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
                combat.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();
                combat.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();
                combat.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();
                combat.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
                Debug.Log("[StandardAttackPlannerParity] result=Passed parityTests=5 existingDomainTest=1 existingFocusedTests=5 nativeRuns=1 missionReady=0");
                ValidationExit.Passed();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[StandardAttackPlannerParity] result=Failed missionReady=0");
                ValidationExit.Failed();
            }
        }

        [Test]
        public void OriginalOracleIsPinnedVerbatimProduction()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            byte[] raw = File.ReadAllBytes(Path.Combine(root, BaselineRelative));
            using var sha = SHA256.Create();
            string actual = BitConverter.ToString(sha.ComputeHash(raw)).Replace("-", "").ToLowerInvariant();
            string original = System.Text.Encoding.UTF8.GetString(raw).Replace("\r\n", "\n");
            string canonical = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-", "").ToLowerInvariant();
            Assert.AreEqual(BaselineSha, canonical, "Original full source LF-normalized snapshot changed");
            Debug.Log($"[StandardAttackPlannerOracle] rawSha256={actual} canonicalLfSha256={canonical}");
            string tests = File.ReadAllText(Path.Combine(root, "Assets/Tests/Editor/StandardAttackPlannerTests.cs")).Replace("\r\n", "\n");
            foreach (string marker in new[] { "        private enum StandardAttackPlanKind", "        private struct StandardAttackCandidate", "        private struct StandardAttackPlan\n", "        private struct PlanStandardAttacksJob" })
            {
                int start = original.IndexOf(marker, StringComparison.Ordinal);
                Assert.GreaterOrEqual(start, 0, marker);
                int end = original.IndexOf('{', start), depth = 1;
                for (end++; depth > 0; end++)
                { if (original[end] == '{') depth++; else if (original[end] == '}') depth--; }
                StringAssert.Contains(original.Substring(start, end - start), tests, "Oracle block is no longer exact original code");
            }
        }

        [Test]
        public void InstalledMathNaNEqualAndClampSemanticsMatchKernel()
        {
            Assert.AreEqual(0f, math.max(0f, float.NaN), "Installed module must select non-NaN operand");
            Assert.AreEqual(0f, math.min(0f, float.NaN));
            Assert.AreEqual(.18f, math.clamp(float.NaN, .05f, .18f));
            Assert.AreEqual(unchecked((int)0x80000000), math.asint(math.max(0f, math.asfloat(unchecked((int)0x80000000)))));
            Assert.AreEqual(0, math.asint(math.min(math.asfloat(unchecked((int)0x80000000)), 0f)));
            var cases = new List<StandardAttackInput>();
            foreach (float value in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f, math.asfloat(unchecked((int)0x80000000)), .1f })
            {
                var input = Ready(); input.Range = value; cases.Add(input);
                input = Ready(); input.State.TraceTimeRemaining = value; cases.Add(input);
                input = Ready(); input.HasAnimationOrder = 1; input.CooldownSeconds = value; cases.Add(input);
                input = Ready(); input.AttackAnimationSeconds = value; cases.Add(input);
                input = Ready(); input.TraceVisibleSeconds = value; cases.Add(input);
                input = Ready(); input.State.TracePhase = value; cases.Add(input);
            }
            CompareManaged(cases.ToArray(), .016f, 1f);
        }

        [Test]
        public void ManagedRandomAndBoundaryPlansMatchEveryField()
        {
            CompareManaged(RandomCases(4096), .016f, 1f);
            var cases = Boundaries();
            foreach (float dt in new[] { 0f, .016f, .125f, 1f, -.016f })
                foreach (float cell in new[] { .5f, 1f, 2f }) CompareManaged(cases, dt, cell);
        }

        [Test]
        public void BurstScheduledPlansMatchOriginalAndManagedResults()
        {
            Assert.IsTrue(BurstCompiler.IsEnabled, "Burst disabled: scheduled parity cannot be credited");
            var data = new List<StandardAttackInput>(RandomCases(4096)); data.AddRange(Boundaries());
            using var inputs = new NativeArray<StandardAttackInput>(data.ToArray(), Allocator.TempJob);
            using var results = new NativeArray<StandardAttackResult>(inputs.Length, Allocator.TempJob);
            using var compiled = new NativeArray<byte>(inputs.Length, Allocator.TempJob);
            new KernelBatch { Inputs = inputs, Results = results, Compiled = compiled, DeltaTime = .016f, CellSize = 1f }.Schedule(inputs.Length, 64).Complete();
            for (int i = 0; i < compiled.Length; i++) Assert.AreEqual(1, compiled[i], "Managed fallback cannot receive Burst execution credit");
            var original = EvaluateOriginal(inputs, .016f, 1f, true);
            for (int i = 0; i < inputs.Length; i++)
            {
                Compare(original[i], results[i], "Burst original/shared " + i);
                Compare(original[i], StandardAttackPlanner.Plan(inputs[i], .016f, 1f), "Burst original/managed " + i);
            }
        }

        [Test]
        public void ActualPrivateRuntimeAdapterPreservesEveryPlanField()
        {
            var data = new List<StandardAttackInput>(RandomCases(512)); data.AddRange(Boundaries());
            using var inputs = new NativeArray<StandardAttackInput>(data.ToArray(), Allocator.TempJob);
            var original = EvaluateOriginal(inputs, .016f, 1f, false);
            Type system = typeof(UnitAttackSystem);
            Type candidate = system.GetNestedType("StandardAttackCandidate", BindingFlags.NonPublic);
            Type plan = system.GetNestedType("StandardAttackPlan", BindingFlags.NonPublic);
            Assert.NotNull(candidate); Assert.NotNull(plan);
            MethodInfo adapter = typeof(StandardAttackPlannerTests).GetMethod(nameof(EvaluateActualAdapter), BindingFlags.NonPublic | BindingFlags.Static);
            var actual = (StandardAttackPlan[])adapter.MakeGenericMethod(candidate, plan).Invoke(null, new object[] { data.ToArray(), .016f, 1f });
            for (int i = 0; i < original.Length; i++) CompareComplete(original[i], actual[i], "Actual runtime adapter " + i);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct KernelBatch : IJobParallelFor
        {
            [ReadOnly] public NativeArray<StandardAttackInput> Inputs;
            public NativeArray<StandardAttackResult> Results;
            public NativeArray<byte> Compiled;
            public float DeltaTime, CellSize;
            [BurstDiscard] private static void ManagedOnly(ref bool compiled) { compiled = false; }
            public void Execute(int index)
            {
                bool compiled = true; ManagedOnly(ref compiled); Compiled[index] = compiled ? (byte)1 : (byte)0;
                Results[index] = StandardAttackPlanner.Plan(Inputs[index], DeltaTime, CellSize);
            }
        }

        private static StandardAttackInput Ready() => new StandardAttackInput
        {
            SelfHealth = 100, TargetHealth = 100, HasTarget = 1, TargetExists = 1,
            TargetHasHealth = 1, TargetHasTransform = 1, Range = 8f, CooldownSeconds = .7f,
            Damage = 9, TraceVisibleSeconds = .2f, TracerEveryNthShot = 1,
            AttackAnimationSeconds = .15f, TargetX = 1f,
            State = new StandardAttackState { TraceTimeRemaining = .1f, TracePhase = .2f }
        };

        private static StandardAttackInput[] Boundaries()
        {
            var result = new List<StandardAttackInput>();
            void Add(ActionRef edit) { var value = Ready(); edit(ref value); result.Add(value); }
            result.Add(Ready());
            Add((ref StandardAttackInput x) => x.SelfHealth = 0);
            Add((ref StandardAttackInput x) => x.HasTarget = 0);
            Add((ref StandardAttackInput x) => x.TargetExists = 0);
            Add((ref StandardAttackInput x) => x.TargetHasHealth = 0);
            Add((ref StandardAttackInput x) => x.TargetHasTransform = 0);
            Add((ref StandardAttackInput x) => x.TargetHealth = 0);
            Add((ref StandardAttackInput x) => x.TargetIsStaticGridBlocker = 1);
            Add((ref StandardAttackInput x) => { x.TargetIsStaticGridBlocker = 2; x.IsDebugFireTarget = 255; });
            Add((ref StandardAttackInput x) => x.AircraftAttackWindowBlocked = 255);
            Add((ref StandardAttackInput x) => x.Range = 0f);
            Add((ref StandardAttackInput x) => x.Range = -1f);
            Add((ref StandardAttackInput x) => x.TargetX = 8.25f);
            Add((ref StandardAttackInput x) => x.TargetX = math.asfloat(math.asint(8.25f) - 1));
            Add((ref StandardAttackInput x) => x.TargetX = math.asfloat(math.asint(8.25f) + 1));
            Add((ref StandardAttackInput x) => { x.HasAnimationOrder = 1; x.State.CooldownRemaining = .01f; });
            Add((ref StandardAttackInput x) => { x.HasAnimationOrder = 1; x.State.CooldownRemaining = 0f; });
            Add((ref StandardAttackInput x) => { x.HasAnimationOrder = 255; x.State.AnimationTimeRemaining = .01f; });
            foreach (float cooldown in new[] { -.1f, 0f, .1f, .36f, 3f })
                Add((ref StandardAttackInput x) => { x.HasAnimationOrder = 1; x.CooldownSeconds = cooldown; });
            foreach (int count in new[] { -1, 0, 1, 2, int.MaxValue, int.MinValue })
                foreach (int every in new[] { -1, 0, 1, 3, int.MaxValue })
                    Add((ref StandardAttackInput x) => { x.State.ShotCounter = count; x.TracerEveryNthShot = every; });
            Add((ref StandardAttackInput x) => x.Damage = 0);
            Add((ref StandardAttackInput x) => x.Damage = -17);
            Add((ref StandardAttackInput x) => x.State.TracePhase = -.9f);
            Add((ref StandardAttackInput x) => x.State.TracePhase = .9f);
            Add((ref StandardAttackInput x) => x.State.CooldownRemaining = .016f);
            return result.ToArray();
        }
        private delegate void ActionRef(ref StandardAttackInput input);

        private static StandardAttackInput[] RandomCases(int count)
        {
            var random = new System.Random(77312023); var result = new StandardAttackInput[count];
            float F(float a, float b) => a + (float)random.NextDouble() * (b - a);
            for (int i = 0; i < count; i++)
            {
                var x = Ready(); x.State.CooldownRemaining = F(-.2f, .2f);
                x.State.TraceTimeRemaining = F(-1f, 1f); x.State.TracePhase = F(-4f, 4f);
                x.State.AnimationTimeRemaining = F(-.2f, .2f); x.State.ShotCounter = random.Next(-10000, 10000);
                x.AttackerX = F(-10000f, 10000f); x.AttackerZ = F(-10000f, 10000f);
                x.TargetX = x.AttackerX + F(-15f, 15f); x.TargetZ = x.AttackerZ + F(-15f, 15f);
                x.Range = F(-1f, 20f); x.SelfCombatRadius = F(0f, 3f); x.TargetCombatRadius = F(0f, 3f);
                x.CooldownSeconds = F(-.1f, 2f); x.AttackAnimationSeconds = F(-.1f, 1f);
                x.TraceVisibleSeconds = F(-.1f, .5f); x.Damage = random.Next(-1, 100);
                x.TracerEveryNthShot = random.Next(-2, 8); x.HasAnimationOrder = (byte)random.Next(0, 3);
                if (i % 7 == 0)
                {
                    x.SelfHealth = random.Next(-1, 2); x.TargetHealth = random.Next(-1, 2);
                    x.HasTarget = (byte)random.Next(0, 3); x.TargetExists = (byte)random.Next(0, 3);
                    x.TargetHasHealth = (byte)random.Next(0, 3); x.TargetHasTransform = (byte)random.Next(0, 3);
                    x.TargetIsStaticGridBlocker = (byte)random.Next(0, 3); x.IsDebugFireTarget = (byte)random.Next(0, 3);
                    x.AircraftAttackWindowBlocked = (byte)random.Next(0, 3);
                }
                result[i] = x;
            }
            return result;
        }

        private static StandardAttackCandidate Candidate(in StandardAttackInput input, int index)
        {
            var target = new Entity { Index = index + 1001, Version = 3 };
            return new StandardAttackCandidate
            {
                Attacker = new Entity { Index = index + 1, Version = 7 }, Target = target,
                Engage = new EngageTarget { Target = input.HasTarget != 0 ? target : Entity.Null,
                    Cell = new int2(index + 31, -index - 11), Position = new float3(9f, -3f, 5f), IsCommanded = 255 },
                AttackState = new UnitAttackCooldownComponent { CooldownRemaining = input.State.CooldownRemaining },
                TraceState = new UnitAttackTraceComponent { TimeRemaining = input.State.TraceTimeRemaining, Phase = input.State.TracePhase, ShotCounter = input.State.ShotCounter },
                AttackAnimationState = new UnitAttackAnimationComponent { TimeRemaining = input.State.AnimationTimeRemaining },
                Attack = new UnitAttack { Range = input.Range, CooldownSeconds = input.CooldownSeconds, Damage = input.Damage,
                    TraceVisibleSeconds = input.TraceVisibleSeconds, TracerEveryNthShot = input.TracerEveryNthShot },
                SelfHealth = input.SelfHealth, TargetHealth = input.TargetHealth,
                AttackerPosition = new float3(input.AttackerX, index * 91f, input.AttackerZ),
                TargetPosition = new float3(input.TargetX, -index * 71f, input.TargetZ),
                AttackerCell = new int2(index + 5, -index - 7), TargetCell = new int2(index + 8, index + 9),
                SelfCombatRadius = input.SelfCombatRadius, TargetCombatRadius = input.TargetCombatRadius,
                AttackAnimationSeconds = input.AttackAnimationSeconds, TargetExists = input.TargetExists,
                TargetHasHealth = input.TargetHasHealth, TargetHasTransform = input.TargetHasTransform,
                TargetIsStaticGridBlocker = input.TargetIsStaticGridBlocker, IsDebugFireTarget = input.IsDebugFireTarget,
                HasAnimationOrder = input.HasAnimationOrder, AircraftAttackWindowBlocked = input.AircraftAttackWindowBlocked
            };
        }

        private static StandardAttackPlan[] EvaluateOriginal(NativeArray<StandardAttackInput> input, float dt, float cell, bool scheduled)
        {
            using var candidates = new NativeArray<StandardAttackCandidate>(input.Length, Allocator.TempJob);
            using var plans = new NativeArray<StandardAttackPlan>(input.Length, Allocator.TempJob);
            var writableCandidates = candidates;
            for (int i = 0; i < input.Length; i++) writableCandidates[i] = Candidate(input[i], i);
            var job = new PlanStandardAttacksJob { Candidates = candidates, Plans = plans, DeltaTime = dt, CellSize = cell };
            if (scheduled) job.Schedule(input.Length, 64).Complete(); else for (int i = 0; i < input.Length; i++) job.Execute(i);
            return plans.ToArray();
        }

        private static StandardAttackPlan[] EvaluateActualAdapter<TCandidate, TPlan>(StandardAttackInput[] values, float dt, float cell)
            where TCandidate : unmanaged where TPlan : unmanaged
        {
            using var candidates = new NativeArray<TCandidate>(values.Length, Allocator.TempJob);
            using var plans = new NativeArray<TPlan>(values.Length, Allocator.TempJob);
            var writableCandidates = candidates;
            for (int i = 0; i < values.Length; i++)
            {
                object encoded = Activator.CreateInstance(typeof(TCandidate)); object original = Candidate(values[i], i);
                foreach (FieldInfo field in typeof(StandardAttackCandidate).GetFields(BindingFlags.Instance | BindingFlags.Public))
                    typeof(TCandidate).GetField(field.Name).SetValue(encoded, field.GetValue(original));
                writableCandidates[i] = (TCandidate)encoded;
            }
            Type jobType = typeof(UnitAttackSystem).GetNestedType("PlanStandardAttacksJob", BindingFlags.NonPublic);
            Assert.NotNull(jobType); object job = Activator.CreateInstance(jobType);
            jobType.GetField("Candidates").SetValue(job, candidates); jobType.GetField("Plans").SetValue(job, plans);
            jobType.GetField("DeltaTime").SetValue(job, dt); jobType.GetField("CellSize").SetValue(job, cell);
            MethodInfo execute = jobType.GetMethod("Execute");
            for (int i = 0; i < values.Length; i++) execute.Invoke(job, new object[] { i });
            var result = new StandardAttackPlan[values.Length];
            for (int i = 0; i < result.Length; i++)
            {
                object boxed = new StandardAttackPlan(); object actual = plans[i];
                foreach (FieldInfo field in typeof(StandardAttackPlan).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    object value = typeof(TPlan).GetField(field.Name).GetValue(actual);
                    field.SetValue(boxed, field.Name == "Kind" ? Enum.ToObject(typeof(StandardAttackPlanKind), Convert.ToInt32(value)) : value);
                }
                result[i] = (StandardAttackPlan)boxed;
            }
            return result;
        }

        private static void CompareManaged(StandardAttackInput[] values, float dt, float cell)
        {
            using var input = new NativeArray<StandardAttackInput>(values, Allocator.TempJob);
            var expected = EvaluateOriginal(input, dt, cell, false);
            for (int i = 0; i < input.Length; i++) Compare(expected[i], StandardAttackPlanner.Plan(input[i], dt, cell), "Managed " + i);
        }
        private static void Float(float expected, float actual, string label)
        {
            if (float.IsNaN(expected)) { Assert.IsTrue(float.IsNaN(actual), label); return; }
            Assert.AreEqual(math.asint(expected), math.asint(actual), label + " (float bits)");
        }
        private static void State(UnitAttackCooldownComponent cooldown, UnitAttackTraceComponent trace, UnitAttackAnimationComponent animation, StandardAttackState actual, string label)
        {
            Float(cooldown.CooldownRemaining, actual.CooldownRemaining, label + " cooldown");
            Float(trace.TimeRemaining, actual.TraceTimeRemaining, label + " trace time");
            Float(trace.Phase, actual.TracePhase, label + " phase");
            Assert.AreEqual(trace.ShotCounter, actual.ShotCounter, label + " shot counter");
            Float(animation.TimeRemaining, actual.AnimationTimeRemaining, label + " animation");
        }
        private static void Compare(StandardAttackPlan expected, StandardAttackResult actual, string label)
        {
            Assert.AreEqual((byte)expected.Kind, (byte)actual.Kind, label + " kind");
            State(expected.BaseAttackState, expected.BaseTraceState, expected.BaseAttackAnimationState, actual.BaseState, label + " base");
            State(expected.AttackState, expected.TraceState, expected.AttackAnimationState, actual.State, label + " final");
            Assert.AreEqual(expected.Damage, actual.Damage, label + " damage");
            Assert.AreEqual(expected.ShowTracer, actual.ShowTracer, label + " tracer");
        }
        private static void CompareComplete(StandardAttackPlan expected, StandardAttackPlan actual, string label)
        {
            var scalar = new StandardAttackResult { Kind = (StandardAttackKind)(byte)actual.Kind,
                BaseState = new StandardAttackState { CooldownRemaining = actual.BaseAttackState.CooldownRemaining,
                    TraceTimeRemaining = actual.BaseTraceState.TimeRemaining, TracePhase = actual.BaseTraceState.Phase,
                    ShotCounter = actual.BaseTraceState.ShotCounter, AnimationTimeRemaining = actual.BaseAttackAnimationState.TimeRemaining },
                State = new StandardAttackState { CooldownRemaining = actual.AttackState.CooldownRemaining,
                    TraceTimeRemaining = actual.TraceState.TimeRemaining, TracePhase = actual.TraceState.Phase,
                    ShotCounter = actual.TraceState.ShotCounter, AnimationTimeRemaining = actual.AttackAnimationState.TimeRemaining },
                Damage = actual.Damage, ShowTracer = actual.ShowTracer };
            Compare(expected, scalar, label);
            Assert.AreEqual(expected.AttackerCell, actual.AttackerCell, label + " attacker cell");
            Assert.AreEqual(expected.Engage.Target, actual.Engage.Target, label + " engage target");
            Assert.AreEqual(expected.Engage.Cell, actual.Engage.Cell, label + " engage cell");
            Float(expected.Engage.Position.x, actual.Engage.Position.x, label + " engage X");
            Float(expected.Engage.Position.y, actual.Engage.Position.y, label + " engage Y");
            Float(expected.Engage.Position.z, actual.Engage.Position.z, label + " engage Z");
            Assert.AreEqual(expected.Engage.IsCommanded, actual.Engage.IsCommanded, label + " commanded");
        }

        // Test-only original code, verbatim blocks from the full pinned source snapshot.
        private enum StandardAttackPlanKind : byte
        {
            None = 0,
            ClearTarget = 1,
            Windup = 2,
            Shot = 3
        }

        private struct StandardAttackCandidate
        {
            public Entity Attacker;
            public Entity Target;
            public EngageTarget Engage;
            public UnitAttackCooldownComponent AttackState;
            public UnitAttackTraceComponent TraceState;
            public UnitAttackAnimationComponent AttackAnimationState;
            public UnitAttack Attack;
            public int SelfHealth;
            public int TargetHealth;
            public float3 AttackerPosition;
            public float3 TargetPosition;
            public int2 AttackerCell;
            public int2 TargetCell;
            public float SelfCombatRadius;
            public float TargetCombatRadius;
            public float AttackAnimationSeconds;
            public byte TargetExists;
            public byte TargetHasHealth;
            public byte TargetHasTransform;
            public byte TargetIsStaticGridBlocker;
            public byte IsDebugFireTarget;
            public byte HasAnimationOrder;
            public byte AircraftAttackWindowBlocked;
        }

        private struct StandardAttackPlan
        {
            public StandardAttackPlanKind Kind;
            public UnitAttackCooldownComponent BaseAttackState;
            public UnitAttackTraceComponent BaseTraceState;
            public UnitAttackAnimationComponent BaseAttackAnimationState;
            public UnitAttackCooldownComponent AttackState;
            public UnitAttackTraceComponent TraceState;
            public UnitAttackAnimationComponent AttackAnimationState;
            public EngageTarget Engage;
            public int Damage;
            public int2 AttackerCell;
            public byte ShowTracer;
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct PlanStandardAttacksJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<StandardAttackCandidate> Candidates;
            public NativeArray<StandardAttackPlan> Plans;
            public float DeltaTime;
            public float CellSize;

            public void Execute(int index)
            {
                StandardAttackCandidate candidate = Candidates[index];
                UnitAttackCooldownComponent baseAttackState = candidate.AttackState;
                UnitAttackTraceComponent baseTraceState = candidate.TraceState;
                UnitAttackAnimationComponent baseAnimationState = candidate.AttackAnimationState;
                baseAttackState.CooldownRemaining -= DeltaTime;
                baseTraceState.TimeRemaining = math.max(0f, baseTraceState.TimeRemaining - DeltaTime);

                StandardAttackPlan plan = new()
                {
                    BaseAttackState = baseAttackState,
                    BaseTraceState = baseTraceState,
                    BaseAttackAnimationState = baseAnimationState,
                    AttackState = baseAttackState,
                    TraceState = baseTraceState,
                    AttackAnimationState = baseAnimationState,
                    Engage = candidate.Engage,
                    AttackerCell = candidate.AttackerCell
                };

                if (candidate.SelfHealth <= 0 ||
                    candidate.Engage.Target == Entity.Null)
                {
                    Plans[index] = plan;
                    return;
                }

                if (candidate.TargetExists == 0 ||
                    candidate.TargetHasHealth == 0 ||
                    candidate.TargetHasTransform == 0 ||
                    (candidate.TargetIsStaticGridBlocker != 0 && candidate.IsDebugFireTarget == 0) ||
                    candidate.TargetHealth <= 0)
                {
                    plan.Kind = StandardAttackPlanKind.ClearTarget;
                    plan.Engage.Target = Entity.Null;
                    Plans[index] = plan;
                    return;
                }

                float attackRange = math.max(0f, candidate.Attack.Range);
                if (attackRange <= 0f)
                {
                    Plans[index] = plan;
                    return;
                }

                if (candidate.AircraftAttackWindowBlocked != 0)
                {
                    Plans[index] = plan;
                    return;
                }

                float3 delta = candidate.TargetPosition - candidate.AttackerPosition;
                delta.y = 0f;
                float range = attackRange + candidate.SelfCombatRadius + candidate.TargetCombatRadius + CellSize * 0.25f;
                if (math.lengthsq(delta) > range * range ||
                    baseAttackState.CooldownRemaining > 0f)
                {
                    Plans[index] = plan;
                    return;
                }

                if (baseAnimationState.TimeRemaining <= 0f && candidate.HasAnimationOrder != 0 &&
                    candidate.AttackState.CooldownRemaining <= 0f)
                {
                    plan.Kind = StandardAttackPlanKind.Windup;
                    plan.AttackAnimationState.TimeRemaining = math.max(0.01f, candidate.AttackAnimationSeconds);
                    plan.AttackState.CooldownRemaining = math.clamp(candidate.Attack.CooldownSeconds * 0.5f, 0.05f, 0.18f);
                    Plans[index] = plan;
                    return;
                }

                plan.Kind = StandardAttackPlanKind.Shot;
                plan.AttackState.CooldownRemaining = math.max(0.01f, candidate.Attack.CooldownSeconds);
                plan.TraceState.ShotCounter++;
                int tracerInterval = math.max(1, candidate.Attack.TracerEveryNthShot);
                bool tracerShown = plan.TraceState.ShotCounter % tracerInterval == 0;
                if (tracerShown)
                {
                    plan.TraceState.TimeRemaining = math.max(0.01f, candidate.Attack.TraceVisibleSeconds);
                    plan.TraceState.Phase = math.frac(plan.TraceState.Phase + 0.371f);
                }

                plan.AttackAnimationState.TimeRemaining = math.max(
                    math.max(0.01f, candidate.AttackAnimationSeconds),
                    candidate.Attack.CooldownSeconds + 0.15f);
                plan.Damage = candidate.Attack.Damage;
                plan.ShowTracer = (byte)(tracerShown ? 1 : 0);
                Plans[index] = plan;
            }
        }

    }
}
#endif
