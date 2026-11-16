using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004AuthoredGridFullRosterFixture
{
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static int Main(string[] args)
    {
        try
        {
            var setup = SourceS004CompiledSetupInput.Read(Path.GetFullPath(args[0]), args[1]);
            var source = JObject.Parse(File.ReadAllText(args[2]));
            var keys = ((JArray)source["requested_prefab_keys"]).Values<string>().ToArray();
            int next = 200000; var em = new EntityManager { LogicalAllocatorAdmitted = true };
            em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var imported = GeneratedReferenceTemplateImporter.Import(em, source, Path.GetDirectoryName(Path.GetFullPath(args[2])), keys);
            var sessionId = new FixedString64Bytes("s004-opening-owner-fixture");
            var session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent { SessionId = sessionId, Phase = SkirmishSessionPhase.Spawning });
            const int width = 2048, height = 1024, count = width * height;
            byte[] mask = File.ReadAllBytes(args[3]);
            Need(mask.Length == count && mask.All(value => value == 0 || value == 1) && mask.Count(value => value == 0) == 22840, "Exact authored-grid projection required");
            var gridEntity = em.CreateEntity();
            em.AddComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = new float3(0, 0, 0) });
            var walkable = em.AddBuffer<GridWalkable>(gridEntity);
            for (int i = 0; i < count; i++) walkable.Add(new GridWalkable { Value = mask[i] });
            var blocked = new NativeBitArray(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var occupied = new NativeBitArray(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            em.AddComponentData(gridEntity, new DynamicBlockerComponent { GridSize = count, Blocked = blocked });
            em.AddComponentData(gridEntity, new DynamicOccupancyComponent { GridSize = count, Occupied = occupied });
            using (var all = em.CreateEntityQuery(new EntityQueryDesc { Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities }))
            using (var entities = all.ToEntityArray(Allocator.Temp))
                RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em, true, true, entities.ToArray(),
                    entity => em.HasComponentNamed(entity, "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
            SkirmishArmyGroupSystem.EnsureSession(em, session, setup);
            var forces = setup.Forces.Select(force => new Game.Simulation.SkirmishBootstrapForce(force.RoleId, force.FactionId, force.RoleKind, force.Quantity, force.SupplyCost, force.RuntimePrefabKey)).ToArray();
            var opening = new Game.Simulation.SkirmishMissionBootstrapKernel(forces);
            var records = new JArray(); string failure = null;
            while (opening.TryBeginMember(out var member))
            {
                var force = setup.Forces[member.ForceIndex];
                var actor = GeneratedTypedOpeningActorSource.CreateForceMember(em, sessionId, force, member.ForceIndex, member.MemberIndex, member.PerMemberSupply, member.SourceKey, imported.Prefabs, setup);
                if (actor == Entity.Null || !em.Exists(actor)) { failure = member.StableId; opening.Abort(); break; }
                uint group = opening.RequiresNewGroup ? SkirmishArmyGroupSystem.OpenGroup(em, session, member.Faction, member.Role, member.Category) : opening.OpenGroupId;
                SkirmishArmyGroupSystem.BindMember(em, session, actor, group);
                Need(em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor) && em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId == group, "Original group owner did not bind actual actor");
                var cell = em.GetComponentData<UnitGrid>(actor).Cell;
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                Need(owned.SessionId == sessionId && owned.StableObjectId.ToString() == member.StableId, "Created actor provenance differs");
                opening.RecordCreatedAndBound(group);
                records.Add(new JObject { ["actor_id"] = member.StableId, ["faction"] = member.Faction, ["role"] = member.Role.ToString(), ["prefab"] = member.SourceKey, ["cell_x"] = cell.x, ["cell_y"] = cell.y, ["group"] = group, ["air"] = em.HasComponent<UnitAirMovement>(actor) });
            }
            var result = new JObject { ["status"] = opening.Completed ? "AllFixtureActorsCreatedAndGrouped" : "FixturePlacementRejected", ["actual_created_and_bound"] = opening.CompletedMembers,
                ["expected"] = setup.Forces.Sum(force => Math.Max(1, force.Quantity)), ["first_failed_actor"] = failure, ["actors"] = records,
                ["player_infantry"] = opening.Player.Infantry, ["player_ground"] = opening.Player.Ground, ["player_air"] = opening.Player.Air,
                ["enemy_infantry"] = opening.Enemy.Infantry, ["enemy_ground"] = opening.Enemy.Ground, ["enemy_air"] = opening.Enemy.Air,
                ["authored_grid_projection"] = true, ["scene_blockers_included"] = false, ["mission_world_created"] = false, ["mission_ready"] = false };
            File.WriteAllText(args[4], result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Need(opening.Completed && opening.CompletedMembers == setup.Forces.Sum(force => Math.Max(1, force.Quantity)), "Actual source opening loop did not complete");
            Console.WriteLine("[S004AuthoredGridFullRosterFixture] result=Passed actualActors=" + opening.CompletedMembers + " sceneBlockers=0 sourceFit=0 mission=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
