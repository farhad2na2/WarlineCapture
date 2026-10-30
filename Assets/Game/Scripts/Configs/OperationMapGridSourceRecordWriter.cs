using Game.Components;using Unity.Collections;using Unity.Entities;
namespace Game.Configs { public static class OperationMapGridSourceRecordWriter {
 public static bool TryBindGeneration(GridWalkableSourceWitness witness,OperationMapGridSourceRecord source,DynamicBuffer<GridWalkable> cells,out GridWalkableSourceWitness bound){bound=default;if(source.Generation<=0||source.WorldSequence==0||(witness.WorldSequence!=0&&witness.WorldSequence!=source.WorldSequence)||!witness.AssetGuid.Equals(source.GridGuid)||!witness.AssetHash.Equals(source.GridHash)||!witness.WalkableHash.Equals(GridWalkableSourceWitness.Digest(cells)))return false;bound=witness;bound.Generation=source.Generation;bound.WorldSequence=source.WorldSequence;return true;}
 public static ulong WorldSequence(EntityManager em){
#if WARLINE_SOURCE_DECLARED_COMPILER_EXPORT || WARLINE_SOURCE_GRID_WITNESS
  return em.WorldUnmanaged.SequenceNumber;
#else
  return em.World.Unmanaged.SequenceNumber;
#endif
 }
 public static void Bind(EntityManager em,Entity root,OperationMapDefinition definition,int generation){
  // This is optional S003 source evidence. Unrelated or incomplete map metadata
  // remains governed by the existing publication path and creates no record.
  if(definition==null||root==Entity.Null||!em.Exists(root)||generation<=0||definition.OperationMapId!="opmap.skirmish.desert_base_01"||string.IsNullOrEmpty(definition.ContentHash)||string.IsNullOrEmpty(definition.GridMetadata.AssetGuid)||string.IsNullOrEmpty(definition.GridMetadata.ContentHash))return;
  if(System.Text.Encoding.UTF8.GetByteCount(definition.OperationMapId)>61||System.Text.Encoding.UTF8.GetByteCount(definition.GridMetadata.AssetGuid)>61||System.Text.Encoding.UTF8.GetByteCount(definition.ContentHash)>125||System.Text.Encoding.UTF8.GetByteCount(definition.GridMetadata.ContentHash)>125)return;
  var value=new OperationMapGridSourceRecord{MapId=new FixedString64Bytes(definition.OperationMapId),GridGuid=new FixedString64Bytes(definition.GridMetadata.AssetGuid),DefinitionHash=new FixedString128Bytes(definition.ContentHash),GridHash=new FixedString128Bytes(definition.GridMetadata.ContentHash),Generation=generation,WorldSequence=WorldSequence(em)};
  if(em.HasComponent<OperationMapGridSourceRecord>(root))em.SetComponentData(root,value);else em.AddComponentData(root,value);
  // Only the publisher can bind/rebind a baked witness. Missing, foreign, or
  // changed grids remain unavailable; this does not create or repair a grid.
  using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkableSourceWitness),typeof(GridWalkable));
  if(grids.CalculateEntityCount()==1){var grid=grids.GetSingletonEntity();var witness=em.GetComponentData<GridWalkableSourceWitness>(grid);var cells=em.GetBuffer<GridWalkable>(grid,true);if(TryBindGeneration(witness,value,cells,out var bound))em.SetComponentData(grid,bound);}
 }
}}
