using System;using System.Linq;using System.Text;using System.Collections.Generic;using Game.Components;namespace Unity.Entities{
 public sealed partial class EntityManager{
 const int QueryMembershipMaxEntries=256,QueryMembershipMaxRows=1048576;
 sealed class MembershipEntry{internal Entity[] Rows;internal Type[] Dependencies;internal int[] Structural,Enabled;internal long Identity,Metadata;internal Game.Runtime.CanonicalStorageBank Bank;internal bool[] Profiles;internal string[] EntityIds;}
 readonly Dictionary<string,MembershipEntry> membershipCache=new Dictionary<string,MembershipEntry>(StringComparer.Ordinal);
 readonly Dictionary<Type,int> queryTypeIds=new Dictionary<Type,int>();readonly Dictionary<Type,int> enabledRevisions=new Dictionary<Type,int>();long entityDomainRevision,metadataDomainRevision;int cachedMembershipRows;
 internal long MembershipHits,MembershipMisses,MembershipFallbacks;
 void QueryMetadataChanged(){metadataDomainRevision++;}
 void QueryEntityDomainChanged(){entityDomainRevision++;membershipCache.Clear();cachedMembershipRows=0;}
 void QueryEnablementChanged(Type type){enabledRevisions[type]=EnabledRevision(type)+1;}
 int EnabledRevision(Type type)=>enabledRevisions.TryGetValue(type,out int value)?value:0;
 string MembershipKey(EntityQueryDesc d,ComponentType[] all,ComponentType[] none,ComponentType[] any){var b=new StringBuilder();b.Append((int)d.Options);foreach(var list in new[]{all,none,any}){b.Append('|');foreach(var item in list){if(!queryTypeIds.TryGetValue(item.Type,out int id)){id=queryTypeIds.Count+1;queryTypeIds.Add(item.Type,id);}b.Append(id).Append(',');}}return b.ToString();}
 bool Valid(MembershipEntry entry){if(entry.Identity!=entityDomainRevision||entry.Metadata!=metadataDomainRevision||!ReferenceEquals(entry.Bank,bank))return false;for(int i=0;i<entry.Dependencies.Length;i++)if(entry.Structural[i]!=GetComponentOrderVersion(entry.Dependencies[i])||entry.Enabled[i]!=EnabledRevision(entry.Dependencies[i]))return false;if(bank!=null){if(entry.Profiles.Length!=bank.AdmittedSlotCount||entry.EntityIds.Length!=bank.EntityIds.Length)return false;for(int i=0;i<entry.Profiles.Length;i++)if(entry.Profiles[i]!=bank.ProfilePresent[i]||entry.EntityIds[i]!=bank.EntityIds[i])return false;}return true;}
 // Read-only internal iteration never exposes the cached array or changes membership.
 internal readonly struct OwnedReadOnlyRows {
  readonly Entity[] rows;internal OwnedReadOnlyRows(Entity[] value){rows=value;}
  public Enumerator GetEnumerator()=>new Enumerator(rows);
  internal struct Enumerator {readonly Entity[] rows;int index;internal Enumerator(Entity[] value){rows=value;index=-1;}public Entity Current=>rows[index];public bool MoveNext(){index++;return index<rows.Length;}}
 }
 internal OwnedReadOnlyRows ReadOnlyOwnedQueryRows(EntityQuery query)=>new OwnedReadOnlyRows(query.Description.HasValue?SelectMembership(query.Description.Value,true):query.Current);
 internal Entity[] Select(EntityQueryDesc d)=>SelectMembership(d,false);
 Entity[] SelectMembership(EntityQueryDesc d,bool readOnlyBorrow){if((d.Options&EntityQueryOptions.FilterWriteGroup)!=0)return SelectWithoutMembershipCache(d);var all=d.All??Array.Empty<ComponentType>();var none=d.None??Array.Empty<ComponentType>();var any=d.Any??Array.Empty<ComponentType>();
 // Native-fact reference worlds retain the original selector. No mutable unknown fact membership is cached.
 if(initialNativeFacts.Count!=0||all.Length+none.Length+any.Length>128||queryTypeIds.Count>2048||all.Concat(none).Concat(any).Any(x=>x.Type==null)){MembershipFallbacks++;return SelectWithoutMembershipCache(d);}
 string key=MembershipKey(d,all,none,any);if(membershipCache.TryGetValue(key,out var entry)&&Valid(entry)){MembershipHits++;return readOnlyBorrow?entry.Rows:(Entity[])entry.Rows.Clone();}
 if(entry!=null){cachedMembershipRows-=entry.Rows.Length;membershipCache.Remove(key);}MembershipMisses++;var rows=SelectWithoutMembershipCache(d);
 if(rows.Length>QueryMembershipMaxRows)return rows;if(membershipCache.Count>=QueryMembershipMaxEntries||cachedMembershipRows>QueryMembershipMaxRows-rows.Length){membershipCache.Clear();cachedMembershipRows=0;}
 // Type dependencies cover all query clauses and default exclusions, including same-full-name metadata types.
 var dependencies=all.Concat(none).Concat(any).Select(x=>x.Type).Concat(new[]{typeof(Prefab),typeof(Disabled)}).Concat(versions.Keys.Where(t=>t.FullName=="Unity.Entities.SystemInstance"||t.FullName=="Unity.Entities.ChunkHeader")).Distinct().ToArray();
 entry=new MembershipEntry{Rows=(Entity[])rows.Clone(),Dependencies=dependencies,Structural=dependencies.Select(GetComponentOrderVersion).ToArray(),Enabled=dependencies.Select(EnabledRevision).ToArray(),Identity=entityDomainRevision,Metadata=metadataDomainRevision,Bank=bank,Profiles=bank==null?null:bank.ProfilePresenceSnapshot(),EntityIds=bank==null?null:(string[])bank.EntityIds.Clone()};membershipCache.Add(key,entry);cachedMembershipRows+=rows.Length;return rows;}
 }
}
