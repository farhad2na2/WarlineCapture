#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Game.Editor
{
    // Read only at an externally synchronized boundary. No job completion, structural
    // changes, allocator reconstruction, query-order substitution or portability claim.
    internal static unsafe class S002InitialWorldEntityExport
    {
        const int MaxEntities=131072,MaxTypes=16384,MaxDepth=32,MaxNodes=2000000,MaxItems=8388608;
        const long MaxBinary=268435456;
        internal static JObject Capture(EntityManager em)=>Capture(em,null,null);
        internal static JObject Capture(EntityManager em,string directory,JObject artifacts)
        {
            uint globalVersion=em.GlobalSystemVersion;int entityOrder=em.EntityOrderVersion;
            var c=new Context(new JArray(),directory,artifacts);var entities=new JArray();var schemas=new JObject();var refs=new JArray();
            var options=EntityQueryOptions.IncludePrefab|EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IncludeSystems|EntityQueryOptions.IncludeMetaChunks|EntityQueryOptions.IgnoreComponentEnabledState;
            using var query=em.CreateEntityQuery(new EntityQueryDesc{Options=options});
            // No filter or enableable requirements: this chunk copy has no dependency
            // to complete (unlike GetAllEntities and ToEntityArray).
            using var chunks=query.ToArchetypeChunkArray(Allocator.Temp);
            var entityHandle=em.GetEntityTypeHandle();var known=new HashSet<string>(StringComparer.Ordinal);int count=0;
            for(int chunkIndex=0;chunkIndex<chunks.Length;chunkIndex++)
            {
                var chunk=chunks[chunkIndex];var ids=chunk.GetNativeArray(entityHandle);
                Need(count<=MaxEntities-ids.Length,"Global entity census limit");count+=ids.Length;
                for(int i=0;i<ids.Length;i++)
                {
                    var entity=ids[i];string id=Id(entity);Need(known.Add(id),"Duplicate global entity");
                    using var types=em.GetComponentTypes(entity,Allocator.Temp);Need(types.Length<=1024,"Per-entity component limit");
                    var components=new JObject();var names=new List<string>();
                    foreach(var component in types)
                    {
                        var info=TypeManager.GetTypeInfo(component.TypeIndex);var type=TypeManager.GetType(component.TypeIndex);string name=(component.IsChunkComponent?"chunk::":"")+Name(type);names.Add(name);
                        Need(schemas.Count<MaxTypes||schemas[name]!=null,"Type census limit");
                        if(schemas[name]==null){var schema=Schema(type,info);schema["chunk_component"]=component.IsChunkComponent;if(type.IsValueType&&!component.TypeIndex.IsZeroSized&&!component.TypeIndex.HasNativeContainer)schema["native_layout"]=c.Layout(type,"type_schema/"+name,0);else schema["native_layout_status"]="not_raw_unmanaged_payload";schemas[name]=schema;}
                        string path="entities/"+id+"/"+name;var record=new JObject{["present"]=true,["chunk_component"]=component.IsChunkComponent,["type_hash"]=info.StableTypeHash.ToString("x16",CultureInfo.InvariantCulture)};
                        try
                        {
                            var handle=em.GetDynamicComponentTypeHandle(ComponentType.ReadOnly(component.TypeIndex));
                            record["change_version"]=chunk.GetChangeVersion(ref handle);
                            record["enabled"]=component.TypeIndex.IsEnableable?(JToken)chunk.IsComponentEnabled(ref handle,i):JValue.CreateNull();
                            if(component.IsChunkComponent)record["value"]=c.Missing(path,"chunk_component_value_on_meta_entity",type);
                            else if(info.Category==TypeManager.TypeCategory.EntityData)record["value"]=c.Value(entity,path,0);
                            else if(info.Category==TypeManager.TypeCategory.BufferData)
                            {
                                var accessor=chunk.GetUntypedBufferAccessor(ref handle);int length=accessor.GetBufferLength(i);c.Items(length);
                                record["length"]=length;
                                if(component.TypeIndex.HasNativeContainer||info.BlobAssetRefOffsetCount!=0||info.HasUnityObjectRefs||!SafeRaw(type,true))
                                    record["value"]=c.Missing(path,"buffer_requires_native_or_blob_or_unity_ref_codec",type);
                                else
                                {
                                    long bytes=checked((long)length*info.ElementSize);c.Bytes(bytes);var data=new byte[checked((int)bytes)];
                                    if(bytes!=0)Marshal.Copy((IntPtr)accessor.GetUnsafeReadOnlyPtr(i),data,0,data.Length);
                                    var offsets=EntityOffsets(component.TypeIndex);var references=new JArray();
                                    foreach(int offset in offsets)for(int element=0;element<length;element++)
                                    {
                                        int at=checked(element*info.ElementSize+offset);Need(at>=0&&at<=data.Length-8,"Entity offset outside buffer");
                                        var reference=new Entity{Index=BitConverter.ToInt32(data,at),Version=BitConverter.ToInt32(data,at+4)};
                                        references.Add(c.Reference(reference,path+"/"+element+"/@"+offset));
                                    }
                                    record["value"]=c.Binary(data,"ecs-buffer-le-v1",type,info.ElementSize,length,path);
                                    record["entity_references"]=references;
                                }
                            }
                            else if(info.Category==TypeManager.TypeCategory.ComponentData&&component.TypeIndex.IsZeroSized)record["value"]=new JObject{["codec"]="tag-v1"};
                            else record["value"]=c.Value(em.Debug.GetComponentBoxed(entity,component),path,0);
                        }
                        catch(LimitException){throw;}
                        catch(Exception error){record["value"]=c.Missing(path,"getter_failed:"+error.GetType().FullName,type);}
                        components[name]=record;
                    }
                    entities.Add(new JObject{["id"]=id,["chunk_order_version"]=chunk.GetOrderVersion(),["chunk_ordinal"]=chunkIndex,["chunk_sequence"]=chunk.SequenceNumber.ToString(CultureInfo.InvariantCulture),["index_in_chunk"]=i,["prefab"]=names.Contains("Unity.Entities.Prefab"),["disabled"]=names.Contains("Unity.Entities.Disabled"),["system"]=names.Contains("Unity.Entities.SystemInstance"),["meta_chunk"]=names.Contains("Unity.Entities.ChunkHeader"),["components"]=components});
                }
            }
            foreach(JObject reference in c.References){string id=(string)reference["id"];reference["in_census"]=id=="0:0"||known.Contains(id);if(!(bool)reference["in_census"])c.Missing((string)reference["path"],"entity_reference_outside_census",typeof(Entity));refs.Add(reference);}
            c.Missing("world/entity_allocator","future_allocation_free_list_versions_not_exported",null);
            Need(em.GlobalSystemVersion==globalVersion&&em.EntityOrderVersion==entityOrder,"World versions changed across read-only census");
            return new JObject{["global_system_version"]=globalVersion,["entity_order_version"]=entityOrder,["entities"]=entities,["type_schema"]=schemas,["reference_map"]=refs,["missing"]=c.Errors,["census_complete"]=true,["authority_closure_complete"]=false,["boundary_precondition"]="caller_already_synchronized_no_pending_jobs",["query_order_authority"]=false,["byte_order"]="little-endian",["counts"]=new JObject{["entities"]=count,["chunks"]=chunks.Length,["types"]=schemas.Count,["value_nodes"]=c.Nodes,["binary_bytes"]=c.BinaryBytes}};
        }
        internal static JToken CaptureValue(object value,string path,JArray missing)=>new Context(missing,null,null).Value(value,path,0);
        internal static JToken CaptureValue(object value,string path,JArray missing,string directory,JObject artifacts)=>new Context(missing,directory,artifacts).Value(value,path,0);
        static JObject Schema(Type type,TypeManager.TypeInfo info)
        {
            var fields=new JArray();foreach(var field in Fields(type))fields.Add(new JObject{["name"]=field.Name,["declared_type"]=Name(field.FieldType)});
            return new JObject{["name"]=Name(type),["assembly"]=type.Assembly.GetName().FullName,["module_mvid"]=type.Module.ModuleVersionId.ToString("D"),["stable_type_hash"]=info.StableTypeHash.ToString("x16",CultureInfo.InvariantCulture),["category"]=info.Category.ToString(),["size_in_chunk"]=info.SizeInChunk,["element_size"]=info.ElementSize,["zero_sized"]=info.IsZeroSized,["enableable"]=info.TypeIndex.IsEnableable,["managed"]=info.TypeIndex.IsManagedType,["native_container"]=info.TypeIndex.HasNativeContainer,["entity_offsets"]=new JArray(EntityOffsets(info.TypeIndex)),["blob_reference_count"]=info.BlobAssetRefOffsetCount,["has_unity_references"]=info.HasUnityObjectRefs,["fields"]=fields,["source_hashes"] = JValue.CreateNull(),["decode_policy"]="compiled_allowlist_only_source_mapping_required"};
        }
        static int[] EntityOffsets(TypeIndex index){var pointer=TypeManager.GetEntityOffsets(index,out int count);Need(count>=0&&count<=4096,"Entity offset count");var values=new int[count];for(int i=0;i<count;i++)values[i]=pointer[i].Offset;return values;}
        static string Name(Type type)=>type==null?null:type.FullName??type.Name;
        static string Id(Entity value)=>value.Index.ToString(CultureInfo.InvariantCulture)+":"+value.Version.ToString(CultureInfo.InvariantCulture);
        static IEnumerable<FieldInfo> Fields(Type type){for(var t=type;t!=null&&t!=typeof(object);t=t.BaseType)foreach(var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).OrderBy(f=>f.Name,StringComparer.Ordinal))if(!f.IsStatic)yield return f;}
        static void Need(bool value,string message){if(!value)throw new LimitException(message);}
        sealed class LimitException:InvalidOperationException{internal LimitException(string message):base(message){}}
        sealed class Context
        {
            internal readonly JArray Errors,References=new JArray();internal int Nodes;internal long BinaryBytes;
            readonly string directory;readonly JObject artifacts;readonly HashSet<object> stack=new HashSet<object>(ReferenceEqualityComparer.Instance);
            internal Context(JArray errors,string output,JObject metadata){Errors=errors??throw new ArgumentNullException(nameof(errors));directory=output;artifacts=metadata;Need(BitConverter.IsLittleEndian,"Only little-endian capture supported");if(output!=null)Need(Path.IsPathRooted(output)&&Directory.Exists(output)&&metadata!=null,"Existing owned artifact directory and table required");}
            internal void Items(int count){Need(count>=0&&count<=MaxItems,"Collection item limit");}
            internal void Bytes(long count){Need(count>=0&&count<=MaxBinary-BinaryBytes,"Aggregate binary limit");BinaryBytes+=count;}
            internal JToken Missing(string path,string reason,Type type){Need(Errors.Count<1000000,"Missing-record limit");Errors.Add(new JObject{["path"]=path,["reason"]=reason,["type"]=Name(type)});return new JObject{["codec"]="unsupported-v1",["type"]=Name(type),["reason"]=reason};}
            internal JObject Reference(Entity value,string path){var record=new JObject{["id"]=Id(value),["path"]=path};References.Add(record);return new JObject{["codec"]="entity-v1",["id"]=Id(value)};}
            internal JToken Value(object value,string path,int depth)
            {
                Need(++Nodes<=MaxNodes&&depth<=MaxDepth,"Value node/depth limit");if(value==null)return new JObject{["codec"]="null-v1"};var type=value.GetType();
                if(type==typeof(Entity))return Reference((Entity)value,path);
                if(type==typeof(float))return new JObject{["codec"]="f32-bits-v1",["bits"]=BitConverter.ToUInt32(BitConverter.GetBytes((float)value),0).ToString("x8",CultureInfo.InvariantCulture)};
                if(type==typeof(double))return new JObject{["codec"]="f64-bits-v1",["bits"]=BitConverter.ToUInt64(BitConverter.GetBytes((double)value),0).ToString("x16",CultureInfo.InvariantCulture)};
                if(type==typeof(string)){Need(((string)value).Length<=1048576,"String length limit");return new JObject{["codec"]="utf16-v1",["value"]=(string)value};}
                if(type==typeof(bool))return new JObject{["codec"]="bool-v1",["value"]=(bool)value};
                if(type.IsEnum)return new JObject{["codec"]="enum-v1",["type"]=Name(type),["underlying"]=Value(Convert.ChangeType(value,Enum.GetUnderlyingType(type),CultureInfo.InvariantCulture),path,depth+1)};
                if(type.IsPrimitive&&type!=typeof(IntPtr)&&type!=typeof(UIntPtr))return new JObject{["codec"]="integer-v1",["type"]=Name(type),["value"]=Convert.ToString(value,CultureInfo.InvariantCulture)};
                if(type.IsPointer||type==typeof(IntPtr)||type==typeof(UIntPtr)||value is Delegate)return Missing(path,"pointer_or_delegate",type);
                if(IsFixedList(type))return FixedList(value,type,path,depth);
                if(type.IsGenericType&&(type.GetGenericTypeDefinition()==typeof(HashSet<>)||type.GetGenericTypeDefinition()==typeof(Stack<>)||type.GetGenericTypeDefinition()==typeof(Queue<>)))return BclCollection(value,type,path,depth);
                if(value is UnityEngine.Transform transform)return TransformSpatial(transform,path,depth);
                if(value is UnityEngine.GameObject gameObject)return GameObjectSpatial(gameObject,path,depth);
                if(value is UnityEngine.ScriptableObject config&&(type.Namespace??"").StartsWith("Game.",StringComparison.Ordinal))return Config(config,path,depth);
                if(value is UnityEngine.Object unity)return UnityReference(unity,path);
                string full=Name(type);
                if(full.StartsWith("Unity.Entities.BlobAssetReference`",StringComparison.Ordinal))return Blob(value,type,path);
                if(full.StartsWith("Unity.Entities.UnityObjectRef`",StringComparison.Ordinal))return Missing(path,"unity_object_ref_requires_bound_source_identity",type);
                if(full.StartsWith("Unity.Collections.NativeBitArray",StringComparison.Ordinal))return Bits(value,path);
                if(type.IsGenericType&&(full.StartsWith("Unity.Collections.NativeArray`",StringComparison.Ordinal)||full.StartsWith("Unity.Collections.NativeList`",StringComparison.Ordinal)))return NativeArray(value,type,path,depth);
                if(type.IsGenericType&&full.StartsWith("Unity.Collections.NativeParallelHashMap`",StringComparison.Ordinal))return NativeMap(value,type,path,depth);
                if(full.StartsWith("Unity.Collections.",StringComparison.Ordinal)&&full.Contains("FixedString"))return FixedString(value,type,path);
                if(full.StartsWith("Unity.Collections.",StringComparison.Ordinal))return Missing(path,"unhandled_native_collection",type);
                if(value is Array array){Items(array.Length);if(array.Rank!=1)return Missing(path,"multidimensional_array",type);if(array.Length>=128&&SafeRaw(type.GetElementType()))return ArrayBinary(array,type.GetElementType(),path);var items=new JArray();for(int i=0;i<array.Length;i++)items.Add(Value(array.GetValue(i),path+"/"+i,depth+1));return new JObject{["codec"]="array-v1",["type"]=full,["items"]=items};}
                if(!type.IsValueType&&!stack.Add(value))return Missing(path,"managed_cycle",type);
                try
                {
                    if(value is IDictionary dictionary){Items(dictionary.Count);var entries=new JArray();int i=0;foreach(DictionaryEntry entry in dictionary)entries.Add(new JObject{["key"]=Value(entry.Key,path+"/key/"+i,depth+1),["value"]=Value(entry.Value,path+"/value/"+i++,depth+1)});return new JObject{["codec"]="managed-map-v1",["type"]=full,["iteration_order"]=entries};}
                    if(value is IList list){Items(list.Count);var items=new JArray();for(int i=0;i<list.Count;i++)items.Add(Value(list[i],path+"/"+i,depth+1));return new JObject{["codec"]="list-v1",["type"]=full,["items"]=items};}
                    if(!type.IsValueType&&!(type.Namespace??"").StartsWith("Game.",StringComparison.Ordinal))return Missing(path,"unknown_managed_type",type);
                    var fields=new JObject();foreach(var field in Fields(type))fields[field.DeclaringType.FullName+"::"+field.Name]=field.FieldType.IsPointer?Missing(path+"/"+field.Name,"pointer_field",field.FieldType):Value(field.GetValue(value),path+"/"+field.Name,depth+1);
                    return new JObject{["codec"]=type.IsValueType?"struct-fields-v1":"managed-fields-v1",["type"]=full,["fields"]=fields};
                }
                finally{if(!type.IsValueType)stack.Remove(value);}
            }
            JToken FixedList(object value,Type type,string path,int depth)
            {
                int length=(int)type.GetProperty("Length").GetValue(value),capacity=(int)type.GetProperty("Capacity").GetValue(value);Items(length);Need(capacity>=0&&capacity<=MaxItems&&length<=capacity&&length<=MaxNodes-Nodes,"FixedList length/capacity/node limit before copy");
                string width=type.GetGenericTypeDefinition().Name.Substring("FixedList".Length).Split('B')[0];var method=typeof(S002InitialWorldEntityExport).GetMethod("FixedItems"+width,BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(type.GetGenericArguments()[0]);var values=(Array)method.Invoke(null,new object[]{value});Need(values.Length==length,"FixedList snapshot length mismatch");var items=new JArray();for(int i=0;i<length;i++)items.Add(Value(values.GetValue(i),path+"/"+i,depth+1));return new JObject{["codec"]="fixed-list-v1",["type"]=Name(type),["element_type"]=Name(type.GetGenericArguments()[0]),["length"]=length,["capacity"]=capacity,["items"]=items,["unused_storage_exported"]=false};
            }
            readonly Dictionary<Type,JObject> layouts=new Dictionary<Type,JObject>();readonly HashSet<Type> layoutActive=new HashSet<Type>();int layoutNodes;
            internal JObject Layout(Type type,string path,int depth)
            {
                Need(++layoutNodes<=16384&&depth<=MaxDepth,"Native layout node/depth limit");if(layouts.TryGetValue(type,out var existing))return (JObject)existing.DeepClone();
                string name=Name(type);
                if(!type.IsValueType||type.IsPointer||type==typeof(IntPtr)||type==typeof(UIntPtr)||name.StartsWith("Unity.Collections.Native",StringComparison.Ordinal)||name.StartsWith("Unity.Entities.BlobAssetReference",StringComparison.Ordinal)||name.StartsWith("Unity.Entities.UnityObjectRef",StringComparison.Ordinal)||!layoutActive.Add(type))return (JObject)Missing(path,"native_layout_requires_explicit_pointer_managed_or_recursive_codec",type);
                try
                {
                    int size=(int)typeof(S002InitialWorldEntityExport).GetMethod(nameof(ElementSize),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(type).Invoke(null,null);int align=(int)typeof(S002InitialWorldEntityExport).GetMethod(nameof(ElementAlign),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(type).Invoke(null,null);Need(size>=0&&size<=MaxBinary&&align>0&&align<=65536,"Native type size/alignment bounds");
                    var result=new JObject{["codec"]="unity-native-layout-v1",["type"]=name,["assembly"]=type.Assembly.FullName,["module_mvid"]=type.Module.ModuleVersionId.ToString("D"),["size"]=size,["alignment"]=align,["byte_order"]="little-endian",["complete"]=true};
                    if(type.IsPrimitive){result["scalar_type"]=name;result["scalar_width_bytes"]=size;}
                    else if(type.IsEnum){result["enum_underlying"]=Layout(Enum.GetUnderlyingType(type),path+"/enum",depth+1);}
                    else
                    {
                        var fields=Fields(type).ToArray();Need(fields.Length<=1024,"Native layout field limit");var output=new JArray();
                        foreach(var field in fields)
                        {
                            Need(++layoutNodes<=16384,"Native layout field node limit");int offset=UnsafeUtility.GetFieldOffset(field);Need(offset>=0&&offset<=size,"Native field offset bounds");var child=Layout(field.FieldType,path+"/"+field.Name,depth+1);var row=new JObject{["name"]=field.Name,["declared_type"]=Name(field.FieldType),["offset"]=offset,["layout"]=child};
                            if((string)child["codec"]!="unity-native-layout-v1"||(bool?)child["complete"]!=true)result["complete"]=false;else Need((long)offset+(int)child["size"]<=size,"Native field extends beyond containing type");
                            var fixedBuffer=field.GetCustomAttributes(typeof(FixedBufferAttribute),false).Cast<FixedBufferAttribute>().SingleOrDefault();
                            if(fixedBuffer!=null){Need(fixedBuffer.Length>=0&&fixedBuffer.Length<=MaxItems,"Fixed buffer length bound");var element=Layout(fixedBuffer.ElementType,path+"/"+field.Name+"/fixed_element",depth+1);row["fixed_buffer"]=new JObject{["element_type"]=Name(fixedBuffer.ElementType),["length"]=fixedBuffer.Length,["element_layout"]=element};if((bool?)element["complete"]!=true)result["complete"]=false;else Need((long)offset+(long)(int)element["size"]*fixedBuffer.Length<=size,"Fixed buffer range exceeds native parent");}
                            output.Add(row);
                        }
                        result["fields"]=output;
                    }
                    if(type.IsGenericType&&(name.StartsWith("Unity.Entities.BlobArray",StringComparison.Ordinal)||name.StartsWith("Unity.Entities.BlobPtr",StringComparison.Ordinal))){var element=Layout(type.GetGenericArguments()[0],path+"/blob_element",depth+1);result["blob_element_layout"]=element;if((bool?)element["complete"]!=true)result["complete"]=false;}
                    if(IsFixedList(type)){var element=Layout(type.GetGenericArguments()[0],path+"/fixed_list_element",depth+1);result["fixed_list_element_layout"]=element;if((bool?)element["complete"]!=true)result["complete"]=false;}
                    layouts[type]=(JObject)result.DeepClone();return result;
                }
                catch(LimitException){throw;}
                catch(Exception error){return (JObject)Missing(path,"native_layout_unavailable:"+error.GetType().FullName,type);}
                finally{layoutActive.Remove(type);}
            }
            JToken BclCollection(object value,Type type,string path,int depth)
            {
                Type definition=type.GetGenericTypeDefinition(),element=type.GetGenericArguments()[0];int count=(int)type.GetProperty("Count").GetValue(value);Items(count);Need(count<=MaxNodes-Nodes,"BCL collection node limit before enumeration");
                if(!stack.Add(value))return Missing(path,"reference_cycle",type);
                try
                {
                    bool hash=definition==typeof(HashSet<>),lifo=definition==typeof(Stack<>);var entries=new JArray();int n=0;
                    foreach(object item in (IEnumerable)value){Need(n<count,"BCL collection grew during enumeration");entries.Add(Value(item,path+"/entry/"+n++,depth+1));}
                    Need(n==count&&(int)type.GetProperty("Count").GetValue(value)==count,"BCL collection changed during enumeration");
                    var result=new JObject{["codec"]=hash?"bcl-hash-set-v1":lifo?"bcl-stack-v1":"bcl-queue-v1",["type"]=Name(type),["element_type"]=Name(element),["count"]=count,["entries"]=entries,["iteration_order"]=hash?"actual_bcl_enumerator_order":lifo?"top_to_bottom":"front_to_back",["restore_order"]=hash?"captured_membership_order_is_not_a_future_hashset_iteration_proof":lifo?"push_in_reverse_captured_order":"enqueue_in_captured_order"};
                    if(hash)
                    {
                        object comparer=type.GetProperty("Comparer").GetValue(value);object defaultComparer=typeof(EqualityComparer<>).MakeGenericType(element).GetProperty("Default").GetValue(null);bool isDefault=ReferenceEquals(comparer,defaultComparer);
                        result["comparer"]=new JObject{["type"]=Name(comparer.GetType()),["assembly"]=comparer.GetType().Assembly.FullName,["module_mvid"]=comparer.GetType().Module.ModuleVersionId.ToString("D"),["is_default_bcl_comparer"]=isDefault,["key_semantics"]=isDefault?"default_bcl_equality_for_declared_element_requires_compiled_element_binding":"custom_comparer_requires_explicit_source_binding"};
                        if(!isDefault)result["comparer_binding"]=Missing(path+"/comparer","custom_hashset_comparer_source_binding_required",comparer.GetType());
                        result["future_mutation_iteration_authority"]=false;result["future_iteration_binding"]="source_backend_or_explicit_model_proof_required; no internal capacity/free-list/unused-slot bytes exported";
                    }
                    return result;
                }
                finally{stack.Remove(value);}
            }
            JObject UnityIdentity(UnityEngine.Object value)
            {
                var result=new JObject{["type"]=Name(value.GetType()),["unity_entity_id"]=value.GetEntityId().ToString()};
                if(UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long local)){result["asset_guid"]=guid;result["local_id"]=local.ToString(CultureInfo.InvariantCulture);result["asset_path"]=UnityEditor.AssetDatabase.GetAssetPath(value);}return result;
            }
            JObject Frames(UnityEngine.Transform t,string path,int depth)=>new JObject
            {
                ["local_position"]=Value(t.localPosition,path+"/local_position",depth+1),["local_rotation"]=Value(t.localRotation,path+"/local_rotation",depth+1),["local_scale"]=Value(t.localScale,path+"/local_scale",depth+1),
                ["world_position"]=Value(t.position,path+"/world_position",depth+1),["world_rotation"]=Value(t.rotation,path+"/world_rotation",depth+1),["lossy_scale"]=Value(t.lossyScale,path+"/lossy_scale",depth+1),
                ["local_to_world"]=Value(t.localToWorldMatrix,path+"/local_to_world",depth+1),["world_to_local"]=Value(t.worldToLocalMatrix,path+"/world_to_local",depth+1)
            };
            JObject TransformSpatial(UnityEngine.Transform t,string path,int depth)
            {
                var ancestry=new JArray();var current=t;int n=0;while(current!=null){Need(++n<=MaxDepth,"Transform ancestry limit");ancestry.Add(new JObject{["identity"]=UnityIdentity(current),["name"]=Value(current.name,path+"/ancestor_name",depth+1),["sibling_index"]=current.GetSiblingIndex()});current=current.parent;}
                return new JObject{["codec"]="unity-transform-spatial-v1",["identity"]=UnityIdentity(t),["ancestry_leaf_to_root"]=ancestry,["frames"]=Frames(t,path,depth),["scope"]="actual_readonly_spatial_snapshot_not_engine_math_backend"};
            }
            JToken GameObjectSpatial(UnityEngine.GameObject go,string path,int depth)
            {
                var rows=new JArray();int componentVisits=0;
                void Walk(UnityEngine.Transform t,int parent,int level)
                {
                    Need(level<=MaxDepth&&rows.Count<8192&&++Nodes<=MaxNodes,"Spatial hierarchy node/depth limit");int ordinal=rows.Count;int children=t.childCount;Items(children);Need(children<=8192-rows.Count,"Spatial child-count limit");
                    int count=t.gameObject.GetComponentCount();Need(count>=0&&count<=1024&&count<=65536-componentVisits,"Spatial component census limit before indexed read");componentVisits+=count;var components=new JArray();var renderers=new JArray();
                    for(int i=0;i<count;i++)
                    {
                        var component=t.gameObject.GetComponentAtIndex(i);components.Add(new JObject{["index"]=i,["identity"]=component!=null?UnityIdentity(component):null});
                        if(component is UnityEngine.Renderer renderer)renderers.Add(new JObject{["component_index"]=i,["identity"]=UnityIdentity(renderer),["enabled"]=renderer.enabled,["local_bounds"]=Value(renderer.localBounds,path+"/renderer_local_bounds",depth+1),["world_bounds"]=Value(renderer.bounds,path+"/renderer_world_bounds",depth+1)});
                    }
                    rows.Add(new JObject{["ordinal"]=ordinal,["parent_ordinal"]=parent,["sibling_index"]=t.GetSiblingIndex(),["identity"]=UnityIdentity(t.gameObject),["transform_identity"]=UnityIdentity(t),["name"]=Value(t.name,path+"/name",depth+1),["active_self"]=t.gameObject.activeSelf,["active_in_hierarchy"]=t.gameObject.activeInHierarchy,["layer"]=t.gameObject.layer,["frames"]=Frames(t,path+"/node/"+ordinal,depth),["components"]=components,["renderers"]=renderers});
                    for(int i=0;i<children;i++)Walk(t.GetChild(i),ordinal,level+1);
                }
                Walk(go.transform,-1,0);return new JObject{["codec"]="unity-gameobject-spatial-template-v1",["identity"]=UnityIdentity(go),["root_transform"]=TransformSpatial(go.transform,path+"/root_transform",depth),["ordered_preorder_hierarchy"]=rows,["component_visits"]=componentVisits,["spatial_snapshot_complete"]=true,["template_complete"]=false,["binding"]=Missing(path,"gameobject_nonspatial_components_source_template_binding_required",go.GetType())};
            }
            JToken UnityReference(UnityEngine.Object value,string path)
            {
                var result=new JObject{["codec"]="unity-object-reference-v1",["type"]=Name(value.GetType()),["unity_entity_id"]=value.GetEntityId().ToString()};
                if(UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long local)){result["asset_guid"]=guid;result["local_id"]=local.ToString(CultureInfo.InvariantCulture);string asset=UnityEditor.AssetDatabase.GetAssetPath(value);result["asset_path"]=asset;}
                result["binding"]=Missing(path,"unity_object_template_or_asset_binding_required",value.GetType());return result;
            }
            JToken Config(UnityEngine.ScriptableObject value,string path,int depth)
            {
                if(!stack.Add(value))return Missing(path,"config_reference_cycle_requires_identity_binding",value.GetType());
                try
                {
                    var fields=new JObject();for(Type type=value.GetType();type!=null&&type!=typeof(UnityEngine.ScriptableObject)&&type!=typeof(UnityEngine.Object);type=type.BaseType)
                    foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).OrderBy(f=>f.Name,StringComparer.Ordinal))
                    if(!field.IsStatic&&!field.IsNotSerialized&&(field.IsPublic||field.IsDefined(typeof(UnityEngine.SerializeField),false)||field.IsDefined(typeof(UnityEngine.SerializeReference),false)))fields[type.FullName+"::"+field.Name]=Value(field.GetValue(value),path+"/"+field.Name,depth+1);
                    var result=new JObject{["codec"]="source-config-fields-v1",["type"]=Name(value.GetType()),["unity_entity_id"]=value.GetEntityId().ToString(),["fields"]=fields};
                    if(UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long local)){result["asset_guid"]=guid;result["local_id"]=local.ToString(CultureInfo.InvariantCulture);result["asset_path"]=UnityEditor.AssetDatabase.GetAssetPath(value);}
                    return result;
                }
                finally{stack.Remove(value);}
            }
            JToken FixedString(object value,Type type,string path){int n=(int)type.GetProperty("Length").GetValue(value);Items(n);var bytes=new byte[n];var item=type.GetProperty("Item");for(int i=0;i<n;i++)bytes[i]=(byte)item.GetValue(value,new object[]{i});Bytes(n);return Binary(bytes,"fixed-utf8-v1",typeof(byte),1,n,path);}
            JToken Bits(object value,string path){var type=value.GetType();bool created=(bool)type.GetProperty("IsCreated").GetValue(value);if(!created)return new JObject{["codec"]="native-bits-v1",["created"]=false};int n=(int)type.GetProperty("Length").GetValue(value);Items(n);Bytes((n+7)/8);var bytes=new byte[(n+7)/8];var get=type.GetMethod("IsSet",new[]{typeof(int)});for(int i=0;i<n;i++)if((bool)get.Invoke(value,new object[]{i}))bytes[i>>3]|=(byte)(1<<(i&7));var result=Binary(bytes,"native-bits-lsb0-v1",typeof(byte),1,bytes.Length,path);result["bit_length"]=n;return result;}
            JToken NativeArray(object value,Type type,string path,int depth)
            {
                bool created=(bool)type.GetProperty("IsCreated").GetValue(value);if(!created)return new JObject{["codec"]="native-array-v1",["created"]=false};int n=(int)type.GetProperty("Length").GetValue(value);Items(n);Type element=type.GetGenericArguments()[0];
                int size=(int)typeof(S002InitialWorldEntityExport).GetMethod(nameof(ElementSize),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(element).Invoke(null,null);Need((long)n*size<=MaxBinary-BinaryBytes,"Native array copy byte limit");
                object view=value;if(type.Name.StartsWith("NativeList",StringComparison.Ordinal))view=type.GetMethod("AsArray",Type.EmptyTypes).Invoke(value,null);
                if(!SafeRaw(element)){var items=new JArray();var getter=view.GetType().GetProperty("Item");for(int i=0;i<n;i++)items.Add(Value(getter.GetValue(view,new object[]{i}),path+"/"+i,depth+1));return new JObject{["codec"]="native-array-fields-v1",["element_type"]=Name(element),["items"]=items};}
                var array=(Array)view.GetType().GetMethod("ToArray",Type.EmptyTypes).Invoke(view,null);return ArrayBinary(array,element,path);
            }
            JToken Blob(object value,Type type,string path)
            {
                bool created=(bool)type.GetProperty("IsCreated").GetValue(value);if(!created)return new JObject{["codec"]="unity-entities-blob-v1",["created"]=false};if(!SafeBlob(type.GetGenericArguments()[0]))return Missing(path,"blob_root_pointer_or_entity_reference_requires_explicit_codec",type);
                using var writer=new BlobWriter(MaxBinary-BinaryBytes);typeof(S002InitialWorldEntityExport).GetMethod(nameof(WriteBlob),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(null,new[]{(object)writer,value});
                var bytes=writer.ToArray();Bytes(bytes.Length);var result=Binary(bytes,"unity-entities-blob-v1",type.GetGenericArguments()[0],0,1,path);result["header_contract"]="BlobAssetHeader.CreateForSerialize strips allocation pointer";result["root_native_layout"]=Layout(type.GetGenericArguments()[0],path+"/blob_root_native_layout",0);return result;
            }
            JToken NativeMap(object value,Type type,string path,int depth){bool created=(bool)type.GetProperty("IsCreated").GetValue(value);if(!created)return new JObject{["codec"]="native-map-v1",["created"]=false};int n=(int)type.GetMethod("Count",Type.EmptyTypes).Invoke(value,null);Items(n);Need(n<=MaxNodes-Nodes,"Native map entry copy limit");long pairSize=0;foreach(var generic in type.GetGenericArguments())pairSize+=(int)typeof(S002InitialWorldEntityExport).GetMethod(nameof(ElementSize),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(generic).Invoke(null,null);Need(pairSize*n<=MaxBinary-BinaryBytes,"Native map copy byte limit");var method=type.GetMethods().Single(x=>x.Name=="GetKeyValueArrays"&&x.GetParameters().Length==1);object copy=method.Invoke(value,new object[]{(AllocatorManager.AllocatorHandle)Allocator.Temp});try{var keys=copy.GetType().GetField("Keys").GetValue(copy);var values=copy.GetType().GetField("Values").GetValue(copy);return new JObject{["codec"]="native-map-v1",["count"]=n,["keys"]=Value(keys,path+"/keys",depth+1),["values"]=Value(values,path+"/values",depth+1),["order"]="actual_GetKeyValueArrays_order"};}finally{((IDisposable)copy).Dispose();}}
            JToken ArrayBinary(Array array,Type element,string path){int size=(int)typeof(S002InitialWorldEntityExport).GetMethod(nameof(ElementSize),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(element).Invoke(null,null);long length=checked((long)size*array.Length);Bytes(length);var bytes=(byte[])typeof(S002InitialWorldEntityExport).GetMethod(nameof(ArrayBytes),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(element).Invoke(null,new object[]{array});return Binary(bytes,"typed-array-le-v1",element,size,array.Length,path);}
            internal JObject Binary(byte[] bytes,string encoding,Type element,int size,int count,string owner)
            {
                var nativeLayout=Layout(element,owner+"/native_layout",0);
                string hash=Digest(bytes);var result=new JObject{["codec"]="binary-v1",["encoding"]=encoding,["element_type"]=Name(element),["element_size"]=size,["count"]=count,["byte_count"]=bytes.Length,["sha256"]=hash,["owner_path"]=owner};
                result["native_layout"]=nativeLayout;
                if(directory==null||bytes.Length==0){result["base64"]=Convert.ToBase64String(bytes);return result;}
                foreach(var property in artifacts.Properties())if((string)property.Value["sha256"]==hash&&(long?)property.Value["bytes"]==bytes.Length&&Path.GetFileName(property.Name)==property.Name)
                {var existing=Path.Combine(directory,property.Name);if(File.Exists(existing)&&ArtifactMatches(existing,bytes.Length,hash)){result["artifact"]=property.Name;result["artifact_encoding"]=property.Value["encoding"].DeepClone();return result;}}
                string name="ecs-"+hash+".bin",path=Path.Combine(directory,name);if(!File.Exists(path)){using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read);file.Write(bytes,0,bytes.Length);file.Flush(true);}else Need(ArtifactMatches(path,bytes.Length,hash),"Artifact name collision");
                artifacts[name]=new JObject{["bytes"]=bytes.Length,["sha256"]=hash,["encoding"]=encoding};result["artifact"]=name;return result;
            }
        }
        sealed class BlobWriter:Unity.Entities.Serialization.BinaryWriter
        {
            readonly long limit;readonly MemoryStream stream=new MemoryStream();internal BlobWriter(long bound){limit=bound;}
            public long Position{get=>stream.Position;set{Need(value==stream.Position,"Blob writer cannot seek");}}
            public void WriteBytes(void* pointer,int bytes){Need(bytes>=0&&bytes<=limit-stream.Length,"Blob payload limit before copy");var chunk=new byte[Math.Min(bytes,65536)];int at=0;while(at<bytes){int n=Math.Min(chunk.Length,bytes-at);Marshal.Copy((IntPtr)((byte*)pointer+at),chunk,0,n);stream.Write(chunk,0,n);at+=n;}}
            internal byte[] ToArray()=>stream.ToArray();public void Dispose()=>stream.Dispose();
        }
        static void WriteBlob<T>(BlobWriter writer,object value)where T:unmanaged=>Unity.Entities.BlobAssetSerializeExtensions.Write(writer,(BlobAssetReference<T>)value);
        static bool ArtifactMatches(string path,int length,string hash)
        {
            var info=new FileInfo(path);info.Refresh();if(!info.Exists||(info.Attributes&(FileAttributes.Directory|FileAttributes.Device|FileAttributes.ReparsePoint))!=0||info.Length!=length||length<=0||length>MaxBinary)return false;
            using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);if(!stream.CanSeek||stream.Length!=length)return false;var bytes=new byte[length];int at=0;while(at<length){int n=stream.Read(bytes,at,length-at);if(n<=0)return false;at+=n;}return stream.ReadByte()==-1&&stream.Length==length&&Digest(bytes)==hash;
        }
        static bool SafeBlob(Type type)
        {
            string name=Name(type);if(type==typeof(Entity)||type.IsPointer||type==typeof(IntPtr)||type==typeof(UIntPtr)||!type.IsValueType)return false;
            if(type.IsPrimitive||type.IsEnum)return true;
            if(name.StartsWith("Unity.Collections.Native",StringComparison.Ordinal)||name.StartsWith("Unity.Entities.BlobAssetReference",StringComparison.Ordinal)||name.StartsWith("Unity.Entities.UnityObjectRef",StringComparison.Ordinal))return false;
            if(type.IsGenericType&&(name.StartsWith("Unity.Entities.BlobArray",StringComparison.Ordinal)||name.StartsWith("Unity.Entities.BlobPtr",StringComparison.Ordinal)))return SafeBlob(type.GetGenericArguments()[0]);
            return Fields(type).All(f=>SafeBlob(f.FieldType));
        }
        static bool SafeRaw(Type type,bool entities=false)
        {
            if(type==typeof(Entity))return entities;
            if(type.IsPointer||type==typeof(IntPtr)||type==typeof(UIntPtr))return false;
            if(type.IsPrimitive||type.IsEnum)return true;
            string name=Name(type);if(!type.IsValueType||name.StartsWith("Unity.Entities.BlobAssetReference",StringComparison.Ordinal)||name.StartsWith("Unity.Entities.UnityObjectRef",StringComparison.Ordinal))return false;
            if(name.StartsWith("Unity.Collections.",StringComparison.Ordinal)&&!name.Contains("FixedString")&&!name.Contains("FixedList"))return false;
            return Fields(type).All(f=>SafeRaw(f.FieldType,entities));
        }
        static bool IsFixedList(Type type)=>type.IsGenericType&&(type.GetGenericTypeDefinition()==typeof(FixedList32Bytes<>)||type.GetGenericTypeDefinition()==typeof(FixedList64Bytes<>)||type.GetGenericTypeDefinition()==typeof(FixedList128Bytes<>)||type.GetGenericTypeDefinition()==typeof(FixedList512Bytes<>)||type.GetGenericTypeDefinition()==typeof(FixedList4096Bytes<>));
        static T[] FixedItems32<T>(object value)where T:unmanaged{var list=(FixedList32Bytes<T>)value;var items=new T[list.Length];for(int i=0;i<items.Length;i++)items[i]=list[i];return items;}
        static T[] FixedItems64<T>(object value)where T:unmanaged{var list=(FixedList64Bytes<T>)value;var items=new T[list.Length];for(int i=0;i<items.Length;i++)items[i]=list[i];return items;}
        static T[] FixedItems128<T>(object value)where T:unmanaged{var list=(FixedList128Bytes<T>)value;var items=new T[list.Length];for(int i=0;i<items.Length;i++)items[i]=list[i];return items;}
        static T[] FixedItems512<T>(object value)where T:unmanaged{var list=(FixedList512Bytes<T>)value;var items=new T[list.Length];for(int i=0;i<items.Length;i++)items[i]=list[i];return items;}
        static T[] FixedItems4096<T>(object value)where T:unmanaged{var list=(FixedList4096Bytes<T>)value;var items=new T[list.Length];for(int i=0;i<items.Length;i++)items[i]=list[i];return items;}
        static int ElementAlign<T>()where T:unmanaged=>UnsafeUtility.AlignOf<T>();
        static int ElementSize<T>()where T:unmanaged=>UnsafeUtility.SizeOf<T>();
        static byte[] ArrayBytes<T>(T[] array)where T:unmanaged{var bytes=new byte[checked(array.Length*UnsafeUtility.SizeOf<T>())];if(bytes.Length!=0)fixed(T* pointer=array)Marshal.Copy((IntPtr)pointer,bytes,0,bytes.Length);return bytes;}
        static string Digest(byte[] bytes){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        sealed class ReferenceEqualityComparer:IEqualityComparer<object>{internal static readonly ReferenceEqualityComparer Instance=new ReferenceEqualityComparer();public new bool Equals(object a,object b)=>ReferenceEquals(a,b);public int GetHashCode(object value)=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);}
    }
}
#endif
