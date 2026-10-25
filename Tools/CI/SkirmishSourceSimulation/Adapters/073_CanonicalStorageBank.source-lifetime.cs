using System;using System.Collections.Generic;using System.Reflection;using Game.Components;using Unity.Entities;
namespace Game.Runtime {
// One owner transfers these initial arrays once. No per-stage bank or native reseed API.
internal sealed class CanonicalStorageBank {
 internal readonly BuildingResourceStorageComponent[] Storage;
 internal readonly BuildingResourceProductionProfileComponent[] Profiles;
 internal readonly bool[] ProfilePresent;
 internal readonly string[] EntityIds;
 readonly Dictionary<Entity,int> slots=new Dictionary<Entity,int>();
 readonly ArchetypeChunk[] chunks;
 internal CanonicalStorageBank(Entity[] entities,BuildingResourceStorageComponent[] storage,BuildingResourceProductionProfileComponent[] profiles,bool[] present){
 if(entities==null||storage==null||profiles==null||present==null||entities.Length>4096||storage.Length!=entities.Length||profiles.Length!=entities.Length||present.Length!=entities.Length)throw new ArgumentException("Complete bounded initial bank required");
 if(Type.GetType("Mono.Runtime")==null)throw new NotSupportedException("Pinned installed Mono required");
 for(int i=0;i<entities.Length;i++){var e=entities[i];if(e.Index<=0||e.Version<=0||slots.ContainsKey(e))throw new ArgumentException("Unique actual Entity version required");slots.Add(e,i);}
 Storage=storage;Profiles=profiles;ProfilePresent=present;EntityIds=new string[entities.Length];chunks=new ArchetypeChunk[entities.Length];
 for(int i=0;i<entities.Length;i++){EntityIds[i]=entities[i].Index+":"+entities[i].Version;chunks[i]=new ArchetypeChunk{Storage=Storage,Profile=Profiles,Offset=i,Count=1,ProfilePresent=ProfilePresent[i]};}
 }
 EntityManager freshSourceManager;
 bool freshSourceLifetime;readonly System.Collections.Generic.HashSet<int> retiredSlots=new System.Collections.Generic.HashSet<int>();
 internal static CanonicalStorageBank CreateFreshSourceConfig(FreshSourceStorageLifetimeAuthority authority,Entity[] entities,BuildingResourceStorageComponent[] storage,BuildingResourceProductionProfileComponent[] profiles,bool[] present){if(authority==null)throw new NotSupportedException("Fresh source storage lifetime token required");authority.Claim();var value=new CanonicalStorageBank(entities,storage,profiles,present);value.freshSourceLifetime=true;for(int i=0;i<entities.Length;i++){value.chunks[i].SourceBank=value;value.chunks[i].SourceSlot=i;value.chunks[i].Entities=new[]{entities[i]};}return value;}
 internal void AttachFreshSourceManager(EntityManager manager){if(!freshSourceLifetime)return;if(freshSourceManager!=null||manager==null)throw new NotSupportedException("Fresh source bank has one EntityManager owner");for(int i=0;i<chunks.Length;i++){Entity entity=chunks[i].Entities[0];if(!manager.Exists(entity)||!manager.HasComponent<BuildingResourceStorageComponent>(entity)||!Same(manager.GetComponentData<BuildingResourceStorageComponent>(entity),Storage[i])||manager.HasComponent<BuildingResourceProductionProfileComponent>(entity)!=ProfilePresent[i]||(ProfilePresent[i]&&!Same(manager.GetComponentData<BuildingResourceProductionProfileComponent>(entity),Profiles[i])))throw new NotSupportedException("Complete raw source bank domain required before binding");}freshSourceManager=manager;}
 internal bool IsLiveSourceSlot(int slot)=>freshSourceLifetime&&(uint)slot<(uint)Storage.Length&&!retiredSlots.Contains(slot);
 internal void RequireLiveSourceSlot(int slot){if(!IsLiveSourceSlot(slot))throw new InvalidOperationException("Retired canonical storage view");}
 internal void RetireFreshSource(Entity entity,EntityManager caller){if(!freshSourceLifetime)throw new NotSupportedException("Canonical bank destruction not yet shared");if(!ReferenceEquals(freshSourceManager,caller))throw new NotSupportedException("Only owning source EntityManager can retire storage");int slot=Slot(entity);slots.Remove(entity);retiredSlots.Add(slot);ProfilePresent[slot]=false;chunks[slot].Count=0;chunks[slot].ProfilePresent=false;}
 internal Unity.Collections.NativeArray<T> SourceView<T>(int slot){RequireLiveSourceSlot(slot);T[] values;if(typeof(T)==typeof(BuildingResourceStorageComponent))values=(T[])(object)Storage;else if(typeof(T)==typeof(BuildingResourceProductionProfileComponent)&&ProfilePresent[slot])values=(T[])(object)Profiles;else throw new NotSupportedException("Canonical source chunk type absent");return new Unity.Collections.NativeArray<T>(1,i=>{RequireLiveSourceSlot(slot);return values[slot];},(i,value)=>{RequireLiveSourceSlot(slot);values[slot]=value;});}
 internal Unity.Collections.NativeArray<Entity> SourceEntityView(int slot){RequireLiveSourceSlot(slot);var values=chunks[slot].Entities;return new Unity.Collections.NativeArray<Entity>(1,i=>{RequireLiveSourceSlot(slot);return values[0];});}
 internal bool Contains(Entity e)=>slots.ContainsKey(e);
 int Slot(Entity e){int i;if(!slots.TryGetValue(e,out i))throw new NotSupportedException("Unknown/version-changed storage owner");return i;}
 internal bool HasProfile(Entity e)=>ProfilePresent[Slot(e)];
 internal BuildingResourceStorageComponent ReadStorage(Entity e)=>Storage[Slot(e)];
 internal BuildingResourceProductionProfileComponent? ReadProfile(Entity e){int i=Slot(e);return ProfilePresent[i]?Profiles[i]:(BuildingResourceProductionProfileComponent?)null;}
 internal void WriteStorage(Entity e,BuildingResourceStorageComponent value){Storage[Slot(e)]=value;}
 internal void WriteProfile(Entity e,BuildingResourceProductionProfileComponent value){int i=Slot(e);if(!ProfilePresent[i])throw new NotSupportedException("Profile structural change unsupported");Profiles[i]=value;}
 internal EntityQuery ProductionQuery()=>freshSourceLifetime?new EntityQuery{Manager=freshSourceManager,Chunks=chunks}:new EntityQuery{Chunks=chunks};
 internal static bool Same<T>(T a,T b)where T:struct{foreach(var f in typeof(T).GetFields(BindingFlags.Public|BindingFlags.Instance)){object x=f.GetValue(a),y=f.GetValue(b);if(f.FieldType==typeof(float)){if(BitConverter.ToInt32(BitConverter.GetBytes((float)x),0)!=BitConverter.ToInt32(BitConverter.GetBytes((float)y),0))return false;}else if(!x.Equals(y))return false;}return true;}
}
}
