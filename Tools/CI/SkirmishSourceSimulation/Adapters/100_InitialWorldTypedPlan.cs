using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using Newtonsoft.Json.Linq;using Unity.Entities;using Game.Components;
namespace Game.Runtime{
 internal sealed class InitialWorldTypedPlan{
  internal sealed class Value{internal Entity Entity;internal Type Type;internal object Data;internal bool Buffer;internal bool? Enabled;internal uint ChangeVersion;}
  internal readonly InitialWorldEnvelope Envelope;internal readonly List<Value> Values;internal readonly HashSet<string> OpaqueTypes;bool committed;
  InitialWorldTypedPlan(InitialWorldEnvelope envelope,List<Value> values,HashSet<string> opaque){Envelope=envelope;Values=values;OpaqueTypes=opaque;}
  // Source field/layout and managed adapters are compiled inputs. No wire assembly loading.
  internal static InitialWorldTypedPlan Decode(InitialWorldEnvelope envelope,ExactInitialValueCodec codec,IReadOnlyDictionary<string,Type> compiledComponents,Func<Entity,Type,JObject,object> explicitSourceAdapter=null){var values=new List<Value>();var opaque=new HashSet<string>(StringComparer.Ordinal);
   foreach(var entity in envelope.Entities)foreach(var property in((JObject)envelope.Records[entity]["components"]).Properties()){
    var record=(JObject)property.Value;var schema=envelope.Schemas[property.Name];
    if(property.Name=="Unity.Entities.Entity"){if((Entity)codec.Decode(record["value"],typeof(Entity),property.Name)!=entity)throw new InvalidDataException("Census EntityData mismatch");continue;}
    if(!compiledComponents.TryGetValue(property.Name,out var type)){opaque.Add(property.Name);continue;}
    if((bool)record["chunk_component"])throw new NotSupportedException("Compiled consumer chunk component needs meta-entity access binding");bool buffer=typeof(IBufferElementData).IsAssignableFrom(type)||typeof(ICleanupBufferElementData).IsAssignableFrom(type);object data;
    if(buffer){data=codec.Decode(record["value"],type.MakeArrayType(),entity+"/"+property.Name);if(record["length"]==null||record["length"].Type!=JTokenType.Integer||(int)record["length"]!=((Array)data).Length)throw new InvalidDataException("Exact native buffer count");}
    else{data=explicitSourceAdapter?.Invoke(entity,type,record);if(data==null)data=codec.Decode(record["value"],type,entity+"/"+property.Name);}
    bool? enabled=record["enabled"].Type==JTokenType.Null?(bool?)null:(bool)record["enabled"];if(enabled.HasValue!=typeof(IEnableableComponent).IsAssignableFrom(type))throw new InvalidDataException("Compiled source enableable declaration mismatch");values.Add(new Value{Entity=entity,Type=type,Data=data,Buffer=buffer,Enabled=enabled,ChangeVersion=(uint)record["change_version"]});
   }
   return new InitialWorldTypedPlan(envelope,values,opaque);
  }
  internal EntityManager Commit(out CanonicalStorageBank bank){if(committed)throw new InvalidOperationException("Initial world commits once; no native reseed API");var storage=Values.Where(v=>v.Type==typeof(BuildingResourceStorageComponent)).ToArray();var profiles=Values.Where(v=>v.Type==typeof(BuildingResourceProductionProfileComponent)).ToDictionary(v=>v.Entity);if(profiles.Keys.Any(e=>!storage.Any(v=>v.Entity==e)))throw new NotSupportedException("Profile without storage owner needs a separate source bank component domain");bank=new CanonicalStorageBank(storage.Select(v=>v.Entity).ToArray(),storage.Select(v=>(BuildingResourceStorageComponent)v.Data).ToArray(),storage.Select(v=>profiles.TryGetValue(v.Entity,out var p)?(BuildingResourceProductionProfileComponent)p.Data:default).ToArray(),storage.Select(v=>profiles.ContainsKey(v.Entity)).ToArray());
   var em=new EntityManager();foreach(var entity in Envelope.Entities)em.ImportEntity(entity);em.BindStorageBank(bank);
   foreach(var value in Values){if(value.Type==typeof(BuildingResourceStorageComponent)||value.Type==typeof(BuildingResourceProductionProfileComponent))continue;if(value.Buffer){var method=typeof(InitialWorldTypedPlan).GetMethod(nameof(Attach),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(value.Type);method.Invoke(null,new[]{(object)em,value.Entity,value.Data});}else em.AddComponent(value.Entity,value.Type,value.Data);if(value.Enabled.HasValue&&!value.Enabled.Value)typeof(EntityManager).GetMethod(nameof(EntityManager.SetComponentEnabled)).MakeGenericMethod(value.Type).Invoke(em,new object[]{value.Entity,false});}
   foreach(var entity in Envelope.Entities)foreach(var property in((JObject)Envelope.Records[entity]["components"]).Properties())em.PreserveInitialNativeFact(entity,property.Name,(JObject)property.Value);
   em.SeedInitialVersions((uint)Envelope.Document["world"]["global_system_version"],Values.Select(v=>(v.Entity,v.Type,v.ChangeVersion)));committed=true;return em;
  }
  static void Attach<T>(EntityManager em,Entity entity,object data)=>em.AttachBuffer(entity,new Unity.Entities.DynamicBuffer<T>((T[])data));
 }
}
namespace Unity.Entities{
 public sealed partial class EntityManager{
  readonly Dictionary<Entity,Dictionary<string,JObject>> initialNativeFacts=new Dictionary<Entity,Dictionary<string,JObject>>();
  internal void PreserveInitialNativeFact(Entity entity,string type,JObject record){if(!Exists(entity)||record==null)throw new InvalidOperationException();if(!initialNativeFacts.TryGetValue(entity,out var row)){row=new Dictionary<string,JObject>(StringComparer.Ordinal);initialNativeFacts.Add(entity,row);}row.Add(type,(JObject)record.DeepClone());}
  internal bool HasInitialNativeFact(Entity entity,string type)=>initialNativeFacts.TryGetValue(entity,out var row)&&row.ContainsKey(type);
  internal void RemoveInitialNativeFact(Entity entity,Type type){if(initialNativeFacts.TryGetValue(entity,out var row))row.Remove(type.FullName);}
  internal void SeedInitialVersions(uint version,IEnumerable<(Entity Entity,Type Type,uint Version)> captured){if(initialVersionSeeded)throw new InvalidOperationException("Initial version seed once");writes.Clear();changeVersion=version;foreach(var item in captured){if(!writes.TryGetValue(item.Entity,out var row)){row=new Dictionary<Type,uint>();writes.Add(item.Entity,row);}row.Add(item.Type,item.Version);}initialVersionSeeded=true;}
  bool initialVersionSeeded;
 }
}
