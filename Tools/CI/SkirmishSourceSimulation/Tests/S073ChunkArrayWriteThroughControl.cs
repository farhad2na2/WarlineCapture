using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;

internal static class S073ChunkArrayWriteThroughControl
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Main()
    {
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        manager.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        Entity squadEntity = manager.CreateEntity();
        var initial = new AISquad { SquadId = 4, TargetScore = 10, TargetEntity = new Entity { Index = 90, Version = 2 } };
        manager.AddComponentData(squadEntity, initial);
        ArchetypeChunk chunk = manager.Chunk(squadEntity);

        ComponentTypeHandle<AISquad> writable = manager.GetComponentTypeHandle<AISquad>(false);
        ComponentTypeHandle<AISquad> copiedWritable = writable;
        NativeArray<AISquad> writableValues = chunk.GetNativeArray(ref copiedWritable);
        AISquad updated = writableValues[0];
        updated.TargetScore = 25;
        updated.TargetEntity = new Entity { Index = 91, Version = 3 };
        writableValues[0] = updated;
        AISquad persisted = manager.GetComponentData<AISquad>(squadEntity);
        Need(persisted.TargetScore == 25 && persisted.TargetEntity.Index == 91 && persisted.TargetEntity.Version == 3,
            "Writable chunk array did not persist the complete component value");

        ComponentTypeHandle<AISquad> readOnly = manager.GetComponentTypeHandle<AISquad>(true);
        ComponentTypeHandle<AISquad> copiedReadOnly = readOnly;
        NativeArray<AISquad> readOnlyValues = chunk.GetNativeArray(ref copiedReadOnly);
        {
            Need(readOnlyValues[0].TargetScore == 25, "Read-only chunk view did not read current storage");
            bool rejected = false;
            try
            {
                AISquad accidental = readOnlyValues[0];
                accidental.TargetScore = -1;
                readOnlyValues[0] = accidental;
            }
            catch (NotSupportedException) { rejected = true; }
            Need(rejected, "Read-only handle allowed a component write");
        }
        Need(manager.GetComponentData<AISquad>(squadEntity).TargetScore == 25,
            "Read-only view changed source storage");

        // Handle copies carry their own access mode. Altering one copy cannot
        // silently change the permission of an already copied job handle.
        writable = manager.GetComponentTypeHandle<AISquad>(true);
        writableValues = chunk.GetNativeArray(ref copiedWritable);
        updated = writableValues[0];
        updated.TargetScore = 26;
        writableValues[0] = updated;
        Need(manager.GetComponentData<AISquad>(squadEntity).TargetScore == 26,
            "Copied writable handle lost write-through after another handle changed");
        Console.WriteLine("[S073ChunkArrayWriteThroughControl] result=Passed writableRoundTrip=1 readonlyRejected=1 copiedHandleIsolation=1");
    }
}
