using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Game.Components;
namespace Game.Runtime
{
    internal static class GeneratedReferenceTemplateImporter
    {
        internal sealed class Result
        {
            internal readonly Dictionary<string,Entity> Prefabs = new Dictionary<string,Entity>(StringComparer.Ordinal);
            internal readonly Dictionary<Entity,Entity> SourceToLogical = new Dictionary<Entity,Entity>();
            internal int ImportedEntities, ImportedComponents, OmittedPresentationComponents;
        }
        private sealed class Row { internal Entity Source; internal bool Enabled; internal readonly List<Cell> Cells = new List<Cell>(); }
        private sealed class Cell { internal Type Type;internal bool Buffer;internal bool? Enabled;internal object Value;internal object[] Items; }
        private static readonly HashSet<string> PresentationOnly = new HashSet<string>(StringComparer.Ordinal) {
            "Game.Components.UnitAttackImpactVfxReference", "Game.Components.UnitMuzzleFlashVfxReference" };
        private static void Need(bool value,string reason) { if(!value)throw new InvalidOperationException(reason); }
        internal static Result Import(EntityManager em,JObject source,string binaryDirectory,IEnumerable<string> requiredSourceKeys)
        {
            Need(em!=null && (int?)source["schema_version"]==1 && (string)source["kind"]=="AuthoredSkirmishReferenceData" &&
                (string)source["authority"]=="ActualSourceAssetsAndNativeBakers_NotMissionState","Actual source-asset bake profile required");
            var entities=source["entities"] as JArray;var roots=source["opening_prefabs"] as JObject;
            Need(entities!=null&&entities.Count>0&&entities.Count<=4096&&roots!=null&&roots.Count>0&&roots.Count<=256,"Bounded complete template graph required");
            var required=new HashSet<string>(requiredSourceKeys??throw new ArgumentNullException(nameof(requiredSourceKeys)),StringComparer.Ordinal);
            Need(required.Count>0&&required.All(k=>!string.IsNullOrEmpty(k)&&roots[k]!=null),"All actual force template keys required");
            var codec=new ExactInitialValueCodec(binaryDirectory,new JObject());
            var assembly=typeof(GeneratedReferenceTemplateImporter).Assembly;
            var rows=new List<Row>();var bySource=new Dictionary<Entity,Row>();int omitted=0;
            foreach(JObject input in entities)
            {
                Entity id=ExactInitialValueCodec.EntityId((string)input["id"]);
                Need(id!=Entity.Null&&!bySource.ContainsKey(id)&&input["entity_enabled"]?.Type==JTokenType.Boolean,"Unique complete asset entity identity/enablement required");
                var row=new Row {Source=id,Enabled=(bool)input["entity_enabled"]};
                var components=input["components"] as JObject;Need(components!=null&&components.Count<=256,"Complete bounded component map required");
                foreach(var property in components.Properties())
                {
                    var value=property.Value as JObject;Need(value!=null&&(bool?)value["present"]==true&&value["buffer"]?.Type==JTokenType.Boolean&&value["value"]!=null,"Exact present component record required");
                    if(PresentationOnly.Contains(property.Name)){omitted++;continue;}
                    Type type=assembly.GetType(property.Name,false,false);
                    Need(type!=null&&type.IsValueType&&(type.Namespace=="Game.Components"||type.Namespace=="Unity.Transforms"||type==typeof(Prefab)||type==typeof(LinkedEntityGroup)||type.FullName=="Unity.Entities.Simulate"),"Unsupported compiled template type: "+property.Name);
                    bool buffer=(bool)value["buffer"];Need(buffer?typeof(IBufferElementData).IsAssignableFrom(type):typeof(IComponentData).IsAssignableFrom(type),"Component/buffer source kind mismatch: "+property.Name);
                    bool? enabled=value["enabled"]?.Type==JTokenType.Null?null:(bool?)value["enabled"];
                    Need(enabled.HasValue==typeof(IEnableableComponent).IsAssignableFrom(type),"Exact enableable component facts required");
                    var cell=new Cell {Type=type,Buffer=buffer,Enabled=enabled};
                    if(buffer)
                    {
                        var envelope=value["value"] as JObject;var items=envelope?["items"] as JArray;
                        Need((string)envelope?["codec"]=="template-buffer-values-v1"&&(string)envelope?["element_type"]==type.FullName&&items!=null&&items.Count<=4096,"Exact bounded ordered buffer values required");
                        cell.Items=items.Select((x,i)=>codec.Decode(x,type,id+"/"+property.Name+"/"+i)).ToArray();
                    }
                    else cell.Value=codec.Decode(value["value"],type,id+"/"+property.Name);
                    row.Cells.Add(cell);
                }
                Need(row.Cells.Any(c=>c.Type==typeof(Prefab)),"Every source template entity must be a real Prefab");rows.Add(row);bySource.Add(id,row);
            }
            // Closure and every decode/type/shape are checked BEFORE allocating any world identity.
            foreach(var row in rows)foreach(var cell in row.Cells)
                foreach(var item in cell.Buffer?cell.Items:new[]{cell.Value})VisitEntities(item,cell.Type,e=>Need(e==Entity.Null||bySource.ContainsKey(e),"Unbound external source template Entity: "+e));
            foreach(var root in roots.Properties())
            {
                Entity id=ExactInitialValueCodec.EntityId((string)root.Value);Need(bySource.ContainsKey(id),"Missing real opening prefab root");
                var keyCell=bySource[id].Cells.FirstOrDefault(c=>c.Type==typeof(UnitSourcePrefabKey));
                Need(keyCell!=null&&((UnitSourcePrefabKey)keyCell.Value).Value.ToString()==root.Name,"Actual source prefab key/root mismatch");
            }
            var result=new Result {OmittedPresentationComponents=omitted};
            foreach(var row in rows)result.SourceToLogical.Add(row.Source,em.CreateEntity());
            try
            {
                foreach(var row in rows)
                {
                    Entity owner=result.SourceToLogical[row.Source];
                    foreach(var cell in row.Cells)
                    {
                        if(cell.Buffer)
                        {
                            object buffer=typeof(EntityManager).GetMethod("AddBuffer").MakeGenericMethod(cell.Type).Invoke(em,new object[]{owner});
                            var add=buffer.GetType().GetMethod("Add");foreach(var item in cell.Items)add.Invoke(buffer,new[]{Remap(item,cell.Type,result.SourceToLogical)});
                        }
                        else em.AddComponent(owner,cell.Type,Remap(cell.Value,cell.Type,result.SourceToLogical));
                        if(cell.Enabled.HasValue)typeof(EntityManager).GetMethod("SetComponentEnabled").MakeGenericMethod(cell.Type).Invoke(em,new object[]{owner,cell.Enabled.Value});
                        result.ImportedComponents++;
                    }
                    if(!row.Enabled)em.AddComponentData(owner,new Disabled());result.ImportedEntities++;
                }
                foreach(var root in roots.Properties()){Entity entity=result.SourceToLogical[ExactInitialValueCodec.EntityId((string)root.Value)];result.Prefabs.Add(root.Name,entity);em.SetName(entity,root.Name);}
                return result;
            }
            catch { foreach(var entity in result.SourceToLogical.Values)if(em.Exists(entity))em.DestroyEntity(entity);throw; }
        }
        private static void VisitEntities(object value,Type type,Action<Entity> visit)
        {
            if(type==typeof(Entity)){visit((Entity)value);return;}
            if(!type.IsValueType||type.IsPrimitive||type.IsEnum)return;
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                if(!field.IsStatic&&(field.FieldType==typeof(Entity)||field.FieldType.IsValueType&&!field.FieldType.IsPrimitive&&!field.FieldType.IsEnum))VisitEntities(field.GetValue(value),field.FieldType,visit);
        }
        private static object Remap(object value,Type type,Dictionary<Entity,Entity> map)
        {
            if(type==typeof(Entity)){var e=(Entity)value;return e==Entity.Null?e:map[e];}
            if(!type.IsValueType||type.IsPrimitive||type.IsEnum)return value;
            object copy=value;
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                if(!field.IsStatic&&(field.FieldType==typeof(Entity)||field.FieldType.IsValueType&&!field.FieldType.IsPrimitive&&!field.FieldType.IsEnum))field.SetValue(copy,Remap(field.GetValue(copy),field.FieldType,map));
            return copy;
        }
    }
}
