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

internal static class S004AuthoredGridActorOwnerFixture
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
            int rifleIndex = Array.FindIndex(setup.Forces, row => row.FactionId == 1 && row.RoleKind == SkirmishRoleKind.Rifle);
            int helicopterIndex = Array.FindIndex(setup.Forces, row => row.FactionId == 1 && row.RoleKind == SkirmishRoleKind.TransportHeli);
            Need(rifleIndex >= 0 && helicopterIndex >= 0, "Compiled S004 opening force roles missing");
            var rifleForce = setup.Forces[rifleIndex]; var airForce = setup.Forces[helicopterIndex];
            var rifle = GeneratedTypedOpeningActorSource.CreateForceMember(em, sessionId, rifleForce, rifleIndex, 0, rifleForce.SupplyCost / rifleForce.Quantity, rifleForce.RuntimePrefabKey, imported.Prefabs, setup);
            var aircraft = GeneratedTypedOpeningActorSource.CreateForceMember(em, sessionId, airForce, helicopterIndex, 0, airForce.SupplyCost / airForce.Quantity, airForce.RuntimePrefabKey, imported.Prefabs, setup);
            Need(rifle != Entity.Null && aircraft != Entity.Null && em.Exists(rifle) && em.Exists(aircraft), "Original owner rejected fixture placements");
            Need(em.GetComponentData<SkirmishAttemptOwnedComponent>(rifle).SessionId == sessionId && em.GetComponentData<SkirmishAttemptOwnedComponent>(aircraft).SessionId == sessionId,
                "Created actor ownership mismatch");
            Need(em.GetComponentData<UnitSourcePrefabKey>(rifle).Value.ToString() == rifleForce.RuntimePrefabKey && em.GetComponentData<UnitSourcePrefabKey>(aircraft).Value.ToString() == airForce.RuntimePrefabKey,
                "Original owner changed template identities");
            Need(em.HasComponent<UnitAirMovement>(aircraft) && !em.HasComponent<UnitAirMovement>(rifle), "Actual ground/air template distinction lost");
            Console.WriteLine("[S004AuthoredGridActorOwnerFixture] result=Passed sourceSetup=1 ground=1 air=1 authoredGridProjection=1 sceneBlockers=0 sourceFit=0 mission=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
