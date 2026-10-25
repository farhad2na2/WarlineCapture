using System;using System.Collections.Generic;using System.Reflection;using Game.Components;using Unity.Entities;
namespace Game.Runtime {
// One owner transfers these initial arrays once. No per-stage bank or native reseed API.
internal sealed class CanonicalStorageBank {
 internal readonly BuildingResourceStorageComponent[] Storage;
 internal readonly BuildingResourceProductionProfileComponent[] Profiles;
 internal readonly bool[] ProfilePresent;
 internal string[] EntityIds; // admitted metadata snapshot; backing storage arrays never resize
 readonly Dictionary<Entity,int> slots=new Dictionary<Entity,int>();
 readonly ArchetypeChunk[] chunks;
 internal CanonicalStorageBank(Entity[] entities,BuildingResourceStorageComponent[] storage,BuildingResourceProductionProfileComponent[] profiles,bool[] present){
 if(entities==null||storage==null||profiles==null||present==null||entities.Length>4096||storage.Length!=entities.Length||profiles.Length!=entities.Length||present.Length!=entities.Length)throw new ArgumentException("Complete bounded initial bank required");
 if(Type.GetType("Mono.Runtime")==null)throw new NotSupportedException("Pinned installed Mono required");
 for(int i=0;i<entities.Length;i++){var e=entities[i];if(e.Index<=0||e.Version<=0||slots.ContainsKey(e))throw new ArgumentException("Unique actual Entity version required");slots.Add(e,i);}
 Storage=storage;Profiles=profiles;ProfilePresent=present;EntityIds=new string[entities.Length];chunks=new ArchetypeChunk[entities.Length];
 for(int i=0;i<entities.Length;i++){EntityIds[i]=entities[i].Index+":"+entities[i].Version;chunks[i]=new ArchetypeChunk{Storage=Storage,Profile=Profiles,Offset=i,Count=1,ProfilePresent=ProfilePresent[i]};}
 }
 // The existing 4096-row admission bound is a host storage limit, not a gameplay budget.
 internal const int FreshSourceSlotCapacity=4096;
 EntityManager freshSourceManager;int sourceSlotCount;ulong sourceEpoch;
 bool freshSourceLifetime;readonly HashSet<int> retiredSlots=new HashSet<int>();
 internal bool FreshSourceLifetime=>freshSourceLifetime;
 internal int AdmittedSlotCount=>freshSourceLifetime?sourceSlotCount:Storage.Length;
 internal ulong SourceStructuralEpoch=>sourceEpoch;
 private CanonicalStorageBank(Entity[] entities,BuildingResourceStorageComponent[] storage,BuildingResourceProductionProfileComponent[] profiles,bool[] present,int capacity):this(entities,storage,profiles,present){
  if(capacity<entities.Length||capacity>FreshSourceSlotCapacity)throw new NotSupportedException("Explicit bounded fresh-source storage capacity required");
  Storage=new BuildingResourceStorageComponent[capacity];Array.Copy(storage,Storage,storage.Length);
  Profiles=new BuildingResourceProductionProfileComponent[capacity];Array.Copy(profiles,Profiles,profiles.Length);
  ProfilePresent=new bool[capacity];Array.Copy(present,ProfilePresent,present.Length);
  chunks=new ArchetypeChunk[capacity];sourceSlotCount=entities.Length;sourceEpoch=1;freshSourceLifetime=true;
  for(int i=0;i<entities.Length;i++)chunks[i]=MakeSourceChunk(entities[i],i);
 }
 internal static CanonicalStorageBank CreateFreshSourceConfig(FreshSourceStorageLifetimeAuthority authority,Entity[] entities,BuildingResourceStorageComponent[] storage,BuildingResourceProductionProfileComponent[] profiles,bool[] present){if(authority==null)throw new NotSupportedException("Fresh source storage lifetime token required");authority.Claim();return new CanonicalStorageBank(entities,storage,profiles,present,FreshSourceSlotCapacity);}
 ArchetypeChunk MakeSourceChunk(Entity entity,int slot)=>new ArchetypeChunk{Manager=freshSourceManager,SourceBank=this,SourceSlot=slot,SourceEpoch=sourceEpoch,Storage=Storage,Profile=Profiles,Entities=new[]{entity},Offset=slot,Count=1,ProfilePresent=ProfilePresent[slot]};
 void RefreshSourceChunkEpochs(){for(int i=0;i<sourceSlotCount;i++)if(!retiredSlots.Contains(i)){chunks[i].Manager=freshSourceManager;chunks[i].SourceEpoch=sourceEpoch;chunks[i].ProfilePresent=ProfilePresent[i];}}
 void RequireEpochSpace(){if(sourceEpoch==ulong.MaxValue)throw new NotSupportedException("Source storage structural epoch exhausted");}
 internal bool IsFreshSourceOwnedBy(EntityManager caller)=>freshSourceLifetime&&caller!=null&&freshSourceManager!=null&&ReferenceEquals(freshSourceManager,caller);
 void RequireSourceOwner(EntityManager caller){if(!IsFreshSourceOwnedBy(caller))throw new NotSupportedException("Only the attached fresh-source EntityManager may mutate the canonical domain");}
 internal void AttachFreshSourceManager(EntityManager manager){if(!freshSourceLifetime)return;if(freshSourceManager!=null||manager==null)throw new NotSupportedException("Fresh source bank has one EntityManager owner");for(int i=0;i<sourceSlotCount;i++){Entity entity=chunks[i].Entities[0];if(!manager.Exists(entity)||!manager.HasComponent<BuildingResourceStorageComponent>(entity)||!Same(manager.GetComponentData<BuildingResourceStorageComponent>(entity),Storage[i])||manager.HasComponent<BuildingResourceProductionProfileComponent>(entity)!=ProfilePresent[i]||(ProfilePresent[i]&&!Same(manager.GetComponentData<BuildingResourceProductionProfileComponent>(entity),Profiles[i])))throw new NotSupportedException("Complete raw source bank domain required before binding");}freshSourceManager=manager;RefreshSourceChunkEpochs();}
 internal bool IsLiveSourceSlot(int slot)=>freshSourceLifetime&&(uint)slot<(uint)sourceSlotCount&&!retiredSlots.Contains(slot);
 internal void RequireLiveSourceSlot(int slot){if(!IsLiveSourceSlot(slot))throw new InvalidOperationException("Retired or unused canonical storage view");}
 internal void RequireSourceView(int slot,ulong epoch){RequireLiveSourceSlot(slot);if(epoch!=sourceEpoch)throw new InvalidOperationException("Stale canonical source structural view");}
 internal int CurrentSlot(Entity entity)=>Slot(entity);
 internal ArchetypeChunk SourceChunk(Entity entity){RequireSourceOwner(freshSourceManager);int slot=Slot(entity);RequireLiveSourceSlot(slot);return chunks[slot];}
 internal bool[] ProfilePresenceSnapshot(){int n=AdmittedSlotCount;var result=new bool[n];Array.Copy(ProfilePresent,result,n);return result;}
 internal void AppendFreshSourceStorage(Entity entity,BuildingResourceStorageComponent value,EntityManager caller){
  RequireSourceOwner(caller);RequireEpochSpace();if(!caller.Exists(entity)||entity.Index<=0||entity.Version<=0)throw new NotSupportedException("Actual live factory actor required");
  string identity=entity.Index+":"+entity.Version;if(slots.ContainsKey(entity)||Array.IndexOf(EntityIds,identity)>=0)throw new InvalidOperationException("Canonical storage owner/version already admitted");
  if(sourceSlotCount>=Storage.Length)throw new NotSupportedException("Explicit fresh-source canonical storage host capacity exhausted");
  int slot=sourceSlotCount;var ids=new string[slot+1];Array.Copy(EntityIds,ids,slot);ids[slot]=identity;
  Storage[slot]=value;Profiles[slot]=default;ProfilePresent[slot]=false;slots.Add(entity,slot);sourceSlotCount++;EntityIds=ids;sourceEpoch++;
  chunks[slot]=MakeSourceChunk(entity,slot);RefreshSourceChunkEpochs();
 }
 internal void AddFreshSourceProfile(Entity entity,BuildingResourceProductionProfileComponent value,EntityManager caller){RequireSourceOwner(caller);RequireEpochSpace();int slot=Slot(entity);RequireLiveSourceSlot(slot);if(ProfilePresent[slot])throw new InvalidOperationException("Profile Add requires absent component");Profiles[slot]=value;ProfilePresent[slot]=true;sourceEpoch++;RefreshSourceChunkEpochs();}
 internal void RetireFreshSource(Entity entity,EntityManager caller){if(!freshSourceLifetime)throw new NotSupportedException("Canonical bank destruction not yet shared");RequireSourceOwner(caller);RequireEpochSpace();int slot=Slot(entity);slots.Remove(entity);retiredSlots.Add(slot);ProfilePresent[slot]=false;chunks[slot].Count=0;chunks[slot].ProfilePresent=false;sourceEpoch++;RefreshSourceChunkEpochs();}
 internal Unity.Collections.NativeArray<T> SourceView<T>(int slot)=>SourceView<T>(slot,sourceEpoch);
 internal Unity.Collections.NativeArray<T> SourceView<T>(int slot,ulong epoch){RequireSourceView(slot,epoch);T[] values;if(typeof(T)==typeof(BuildingResourceStorageComponent))values=(T[])(object)Storage;else if(typeof(T)==typeof(BuildingResourceProductionProfileComponent)&&ProfilePresent[slot])values=(T[])(object)Profiles;else throw new NotSupportedException("Canonical source chunk type absent");return new Unity.Collections.NativeArray<T>(1,i=>{RequireSourceView(slot,epoch);return values[slot];},(i,value)=>{RequireSourceView(slot,epoch);values[slot]=value;});}
 internal Unity.Collections.NativeArray<Entity> SourceEntityView(int slot)=>SourceEntityView(slot,sourceEpoch);
 internal Unity.Collections.NativeArray<Entity> SourceEntityView(int slot,ulong epoch){RequireSourceView(slot,epoch);var values=chunks[slot].Entities;return new Unity.Collections.NativeArray<Entity>(1,i=>{RequireSourceView(slot,epoch);return values[0];});}
 internal bool Contains(Entity e)=>slots.ContainsKey(e);
 int Slot(Entity e){int i;if(!slots.TryGetValue(e,out i))throw new NotSupportedException("Unknown/version-changed storage owner");return i;}
 internal bool HasProfile(Entity e)=>ProfilePresent[Slot(e)];
 internal BuildingResourceStorageComponent ReadStorage(Entity e)=>Storage[Slot(e)];
 internal BuildingResourceProductionProfileComponent? ReadProfile(Entity e){int i=Slot(e);return ProfilePresent[i]?Profiles[i]:(BuildingResourceProductionProfileComponent?)null;}
 internal void WriteStorage(Entity e,BuildingResourceStorageComponent value){Storage[Slot(e)]=value;}
 internal void WriteProfile(Entity e,BuildingResourceProductionProfileComponent value){int i=Slot(e);if(!ProfilePresent[i])throw new NotSupportedException("Profile structural change unsupported");Profiles[i]=value;}
 internal EntityQuery ProductionQuery(){if(!freshSourceLifetime)return new EntityQuery{Chunks=chunks};if(freshSourceManager==null)throw new NotSupportedException("Source bank must bind before live production queries");return freshSourceManager.CreateEntityQuery(typeof(BuildingResourceStorageComponent));}
 internal static bool Same<T>(T a,T b)where T:struct{foreach(var f in typeof(T).GetFields(BindingFlags.Public|BindingFlags.Instance)){object x=f.GetValue(a),y=f.GetValue(b);if(f.FieldType==typeof(float)){if(BitConverter.ToInt32(BitConverter.GetBytes((float)x),0)!=BitConverter.ToInt32(BitConverter.GetBytes((float)y),0))return false;}else if(!x.Equals(y))return false;}return true;}
}
}
