using Unity.Collections;using Unity.Entities;
namespace Game.Components { public struct OperationMapGridSourceRecord:IComponentData { public FixedString64Bytes MapId,GridGuid;public FixedString128Bytes DefinitionHash,GridHash;public int Generation;public ulong WorldSequence; }}
