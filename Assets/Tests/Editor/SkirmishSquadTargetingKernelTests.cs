using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSquadTargetingKernelTests
    {
        public static int RunChecks()
        {
            var t = new SkirmishSquadTargetingKernelTests();
            t.OriginalSourcePinned(); t.SeededSelectionMatchesOriginalInBothBackends();
            t.NativeAdapterMatchesOriginal(); t.TiesOpeningAndMemberPriority();
            var old = new global::SkirmishCombatPolicyTests();
            old.EnemyWaitsForOpeningButDefendsImmediatelyAndReturnsToTheMainBaseObjective();
            old.AttackingSquadFightsNearbyDefendersBeforeResumingBaseAttack();
            old.AttackersEngageArmedTowersOnTheirRouteAndIgnoreUnarmedSupplyBuildings();
            Debug.Log("[SkirmishSquadTargetingParity] result=Passed parity=4 regressions=3 missionReady=0");
            return 7;
        }
        [Test] public void OriginalSourcePinned()
        {
            string root = Path.Combine(Application.dataPath, "Tests/Editor");
            string original = File.ReadAllText(Path.Combine(root, "SkirmishSquadTargetingOriginalSource.cs.txt")).Replace("\r\n", "\n");
            using var sha = SHA256.Create();
            Assert.AreEqual("e927b5b8347ae385c237ecc9b6a6d24fb874795c16c014e75f471781bc421098", BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-", "").ToLowerInvariant());
            Assert.AreEqual(original.Replace("public static class SkirmishCombatPolicy", "public static class SkirmishSquadTargetingOriginalOracle"), File.ReadAllText(Path.Combine(root, "SkirmishSquadTargetingOriginalOracle.cs")).Replace("\r\n", "\n"));
        }
        private struct Context : ISquadTargetContext
        {
            public NativeArray<SquadTargetCandidate> Candidates;
            public NativeArray<SquadTargetMember> Members;
            public int CandidateCount => Candidates.Length;
            public int MemberCount => Members.Length;
            public SquadTargetCandidate Candidate(int i) => Candidates[i];
            public SquadTargetMember Member(int i) => Members[i];
        }
        // Scalar fixture oracle is the original two loops using native float2 distances.
        private static SquadTargetResult Original(ref Context context, in SquadTargetInput input)
        {
            float best = input.DefenseRadius * input.DefenseRadius;
            var result = new SquadTargetResult { Target = input.OpposingBase };
            foreach (var candidate in context.Candidates)
            {
                if (candidate.Faction == 0 || candidate.Faction == input.Faction || candidate.Health <= 0 || candidate.Damage <= 0) continue;
                float distance = math.distancesq(new float2(candidate.X, candidate.Z), new float2(input.OwnBaseX, input.OwnBaseZ));
                if (distance >= best) continue;
                best = distance; result.Target = candidate.Entity; result.Threatened = true;
            }
            if (!result.Threatened)
            {
                best = float.MaxValue;
                foreach (var member in context.Members)
                {
                    if (!member.Eligible) continue;
                    float radius = member.Range + 8;
                    foreach (var candidate in context.Candidates)
                    {
                        if (candidate.Faction == 0 || candidate.Faction == input.Faction || candidate.Health <= 0 || candidate.Damage <= 0) continue;
                        float distance = math.distancesq(new float2(candidate.X, candidate.Z), new float2(member.X, member.Z));
                        if (distance >= radius * radius || distance >= best) continue;
                        best = distance; result.Target = candidate.Entity; result.Threatened = true;
                    }
                }
            }
            if (!result.Threatened && input.ElapsedSeconds < input.FirstAttackSeconds) result.Target = default;
            return result;
        }
        [BurstCompile(CompileSynchronously = true)]
        private struct PairJob : IJob
        {
            public Context Context;
            public SquadTargetInput Input;
            public NativeArray<SquadTargetResult> Results;
            public NativeArray<byte> Compiled;
            [BurstDiscard] private static void ManagedOnly(ref bool value) { value = false; }
            public void Execute()
            {
                bool compiled = true; ManagedOnly(ref compiled); Compiled[0] = compiled ? (byte)1 : (byte)0;
                Results[0] = Original(ref Context, Input);
                Results[1] = SkirmishSquadTargetingKernel.Select(ref Context, Input);
            }
        }
        private static void Equal(SquadTargetResult a, SquadTargetResult b)
        {
            Assert.AreEqual(a.Target.Index, b.Target.Index); Assert.AreEqual(a.Target.Version, b.Target.Version); Assert.AreEqual(a.Threatened, b.Threatened);
        }
        [Test] public void SeededSelectionMatchesOriginalInBothBackends()
        {
            using var candidates = new NativeArray<SquadTargetCandidate>(24, Allocator.TempJob);
            using var members = new NativeArray<SquadTargetMember>(4, Allocator.TempJob);
            using var results = new NativeArray<SquadTargetResult>(2, Allocator.TempJob);
            using var compiled = new NativeArray<byte>(1, Allocator.TempJob);
            var context = new Context { Candidates = candidates, Members = members };
            var random = new System.Random(53821);
            for (int i = 0; i < 2048; i++)
            {
                for (int j = 0; j < candidates.Length; j++) context.Candidates[j] = new SquadTargetCandidate {
                    Entity = new SquadTargetIdentity(j+1, i%3+1), Faction = (byte)random.Next(4), Health = random.Next(-1, 20), Damage = random.Next(-1, 20),
                    X = (float)(random.NextDouble()*300-150), Z = (float)(random.NextDouble()*300-150) };
                for (int j = 0; j < members.Length; j++) context.Members[j] = new SquadTargetMember { Eligible = random.Next(4) != 0,
                    X = (float)(random.NextDouble()*300-150), Z = (float)(random.NextDouble()*300-150), Range = random.Next(-10, 40) };
                var input = new SquadTargetInput { Faction = (byte)(i%2+1), OwnBaseX = 30, OwnBaseZ = -10,
                    DefenseRadius = i%7*8, ElapsedSeconds = i%4, FirstAttackSeconds = 2, OpposingBase = new SquadTargetIdentity(100, 9) };
                Equal(Original(ref context, input), SkirmishSquadTargetingKernel.Select(ref context, input));
                new PairJob { Context = context, Input = input, Results = results, Compiled = compiled }.Schedule().Complete();
                Assert.AreEqual(1, compiled[0], "Actual Burst required"); Equal(results[0], results[1]);
            }
        }
        private static Entity Unit(EntityManager em, byte faction, float x, float z, int health, int damage)
        {
            var e = em.CreateEntity(typeof(Faction), typeof(UnitHealth), typeof(UnitAttack), typeof(UnitGrid), typeof(LocalTransform));
            em.SetComponentData(e, new Faction { Id = faction }); em.SetComponentData(e, new UnitHealth { Current = health, Max = 100 });
            em.SetComponentData(e, new UnitAttack { Range = 20, Damage = damage }); em.SetComponentData(e, new UnitGrid { Cell = new int2((int)x, (int)z) });
            em.SetComponentData(e, LocalTransform.FromPosition(new float3(x, 0, z))); return e;
        }
        [Test] public void NativeAdapterMatchesOriginal()
        {
            using var world = new World("SquadTargetingAdapterParity"); var em = world.EntityManager;
            Assert.IsNotNull(SkirmishPresetConfig.Load(0));
            var own = Unit(em, 2, 400, 400, 100, 0); var opposing = Unit(em, 1, 20, 20, 100, 0);
            var session = em.CreateEntity(typeof(SkirmishMatchState));
            var squad = em.CreateEntity(typeof(AISquad)); em.AddBuffer<AISquadUnit>(squad);
            var units = new Entity[24]; for (int i = 0; i < units.Length; i++) units[i] = Unit(em, (byte)(i%3), 0, 0, 100, 10);
            em.AddComponent<UnitResourceHauler>(units[1]); em.AddComponent<OperationMapBuildingComponent>(units[2]);
            var random = new System.Random(9301);
            for (int i = 0; i < 96; i++)
            {
                var match = new SkirmishMatchState { Phase = i%11==0 ? SkirmishPhase.Finished : SkirmishPhase.Playing,
                    PlayerMainBase = opposing, EnemyMainBase = own, ElapsedSeconds = i%3==0 ? 0 : 300 };
                em.SetComponentData(session, match);
                var buffer = em.GetBuffer<AISquadUnit>(squad); buffer.Clear();
                for (int j = 0; j < units.Length; j++)
                {
                    em.SetComponentData(units[j], LocalTransform.FromPosition(new float3(random.Next(0, 500), 0, random.Next(0, 500))));
                    em.SetComponentData(units[j], new UnitHealth { Current = random.Next(-1, 20), Max = 100 });
                    em.SetComponentData(units[j], new UnitAttack { Range = random.Next(0, 30), Damage = random.Next(-1, 20) });
                    if (j%5==0) buffer.Add(new AISquadUnit { Unit = units[j] });
                }
                buffer.Add(new AISquadUnit { Unit = Entity.Null });
                var initial = new AISquad { FactionId = (byte)(i%2+1), TargetEntity = own, TargetCell = new int2(91, 92), TargetKind = 19, TargetFactionId = 7, TargetScore = 431, LastLogTime = 11 };
                em.SetComponentData(squad, initial); bool expectedHandled = SkirmishSquadTargetingOriginalOracle.TryAssignTargets(em); var expected = em.GetComponentData<AISquad>(squad);
                em.SetComponentData(squad, initial); Assert.AreEqual(expectedHandled, SkirmishCombatPolicy.TryAssignTargets(em)); var actual = em.GetComponentData<AISquad>(squad);
                Assert.AreEqual(expected.TargetEntity, actual.TargetEntity); Assert.AreEqual(expected.TargetCell, actual.TargetCell);
                Assert.AreEqual(expected.TargetFactionId, actual.TargetFactionId); Assert.AreEqual(expected.TargetKind, actual.TargetKind);
                Assert.AreEqual(expected.TargetScore, actual.TargetScore); Assert.AreEqual(expected.LastLogTime, actual.LastLogTime);
            }
        }
        [Test] public void TiesOpeningAndMemberPriority()
        {
            using var candidates = new NativeArray<SquadTargetCandidate>(2, Allocator.Temp);
            using var members = new NativeArray<SquadTargetMember>(1, Allocator.Temp);
            var c = new Context { Candidates = candidates, Members = members };
            c.Candidates[0] = new SquadTargetCandidate { Entity = new SquadTargetIdentity(4, 2), Faction = 1, Health = 1, Damage = 1, X = 10 };
            c.Candidates[1] = new SquadTargetCandidate { Entity = new SquadTargetIdentity(4, 3), Faction = 1, Health = 1, Damage = 1, X = -10 };
            var input = new SquadTargetInput { Faction = 2, DefenseRadius = 10, FirstAttackSeconds = 5, OpposingBase = new SquadTargetIdentity(90, 6) };
            Assert.AreEqual(0, SkirmishSquadTargetingKernel.Select(ref c, input).Target.Index, "Exact radius is excluded; wait for opening");
            input.DefenseRadius = 11; Assert.AreEqual(2, SkirmishSquadTargetingKernel.Select(ref c, input).Target.Version, "Tie preserves first identity version");
            input.DefenseRadius = 0; c.Members[0] = new SquadTargetMember { Eligible = true, X = 10, Range = 1 };
            Assert.AreEqual(2, SkirmishSquadTargetingKernel.Select(ref c, input).Target.Version);
            c.Members[0] = default; input.ElapsedSeconds = 5;
            Assert.AreEqual(90, SkirmishSquadTargetingKernel.Select(ref c, input).Target.Index, "Opening boundary selects opposing base");
        }
    }
}
