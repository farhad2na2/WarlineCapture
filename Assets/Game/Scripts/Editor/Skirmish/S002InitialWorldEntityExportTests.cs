#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace Game.Editor
{
    public static class S002InitialWorldEntityExportTests
    {
        [DisableAutoCreation]
        public partial class VersionWriter:SystemBase
        {
            public Entity Target;
            protected override void OnUpdate()=>EntityManager.SetComponentData(Target,new Scalar{Value=19,Other=Entity.Null});
        }
        sealed class CustomComparer:IEqualityComparer<int>
        {
            public static int Calls;public bool Equals(int x,int y){Calls++;return x==y;}public int GetHashCode(int x){Calls++;return x;}
        }
        unsafe struct FixedAbi{public bool Flag;public int Tail;public fixed byte Data[5];}
        struct Tag:IComponentData{}
        struct EnabledTag:IComponentData,IEnableableComponent{}
        struct Scalar:IComponentData{public float Value;public Entity Other;}
        struct Item:IBufferElementData,IEnableableComponent{public int Value;public Entity Other;}
        struct BlobRoot{public int Value;}
        sealed class Profile:ScriptableObject
        {
            [SerializeField]string[] allowedRoleIds;
            [NonSerialized]public int Cache;
            public void Initialize(){allowedRoleIds=new[]{"role.rifle","role.tanker"};Cache=987;}
        }
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);Debug.Log("[InitialWorldCodec] PASS "+message);}
        // Root-owned checked native validation only; this source packet does not run it.
        public static void RunFocusedValidation()
        {
            string output=Environment.GetEnvironmentVariable("WARLINE_INITIAL_WORLD_CODEC_TEST_OUTPUT");
            Check(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&!Directory.Exists(output),"fresh absolute test output");Directory.CreateDirectory(output);
            var previous=World.DefaultGameObjectInjectionWorld;int checks=0;
            Check(!DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default).Contains(typeof(VersionWriter)),"test writer excluded from automatic mission-world system inventory");checks++;
            using(var world=new World("InitialWorldCodecFocusedValidation"))
            {
                var em=world.EntityManager;var target=em.CreateEntity(typeof(Tag),typeof(Prefab),typeof(Disabled));
                var actor=em.CreateEntity(typeof(Scalar),typeof(EnabledTag));em.SetComponentEnabled<EnabledTag>(actor,false);
                em.SetComponentData(actor,new Scalar{Value=BitConverter.ToSingle(BitConverter.GetBytes(0x7fc01234u),0),Other=target});
                em.AddChunkComponentData<Scalar>(actor);em.SetChunkComponentData(em.GetChunk(actor),new Scalar{Value=41,Other=Entity.Null});
                var buffer=em.AddBuffer<Item>(actor);buffer.Add(new Item{Value=73,Other=target});em.SetComponentEnabled<Item>(actor,false);
                var artifacts=new JObject();var capture=S002InitialWorldEntityExport.Capture(em,output,artifacts);var rows=(JArray)capture["entities"];
                JObject Row(Entity e)=>rows.OfType<JObject>().Single(x=>(string)x["id"]==e.Index+":"+e.Version);
                Check((bool)Row(target)["prefab"]&&(bool)Row(target)["disabled"],"global prefab and disabled entities retained");checks++;
                var components=(JObject)Row(actor)["components"];
                Check((bool)components[typeof(EnabledTag).FullName]["enabled"]==false&&(string)Row(target)["components"][typeof(Tag).FullName]["value"]["codec"]=="tag-v1","zero-sized tags and false enablement retained");checks++;
                Check((bool)components[typeof(Item).FullName]["enabled"]==false&&(int)components[typeof(Item).FullName]["length"]==1,"disabled buffer presence and length retained");checks++;
                Check(components[typeof(Scalar).FullName]["value"].ToString().Contains("7fc01234"),"NaN payload bits retained without JSON float");checks++;
                Check(((JArray)capture["reference_map"]).OfType<JObject>().Any(x=>(string)x["id"]==target.Index+":"+target.Version&&(bool)x["in_census"]),"scalar and buffer Entity references resolve within full census");checks++;
                Check((bool)capture["census_complete"]&&!(bool)capture["authority_closure_complete"]&&capture["missing"].ToString().Contains("future_allocation"),"presence completeness does not invent allocator authority");checks++;
                Check(components[typeof(Scalar).FullName]!=null&&components["chunk::"+typeof(Scalar).FullName]!=null&&(bool)components["chunk::"+typeof(Scalar).FullName]["chunk_component"],"normal and chunk component presence identities remain distinct");checks++;
                uint previousVersion=(uint)components[typeof(Scalar).FullName]["change_version"];
                var writer=world.GetOrCreateSystemManaged<VersionWriter>();writer.Target=actor;writer.Update();
                var versioned=S002InitialWorldEntityExport.Capture(em,output,artifacts);var versionRow=((JArray)versioned["entities"]).OfType<JObject>().Single(x=>(string)x["id"]==actor.Index+":"+actor.Version);
                var versionHandle=em.GetDynamicComponentTypeHandle(ComponentType.ReadOnly<Scalar>());var actualChunk=em.GetChunk(actor);uint actualChange=actualChunk.GetChangeVersion(ref versionHandle);
                Check((uint)versionRow["components"][typeof(Scalar).FullName]["change_version"]==actualChange&&ChangeVersionUtility.DidChange(actualChange,previousVersion)&&(uint)versionRow["chunk_order_version"]==actualChunk.GetOrderVersion()&&(uint)versioned["global_system_version"]==em.GlobalSystemVersion&&(int)versioned["entity_order_version"]==em.EntityOrderVersion,"source SetComponentData and chunk/world version metadata retained exactly");checks++;
                var itemLayout=components[typeof(Item).FullName]["value"]["native_layout"];var layoutFields=(JArray)itemLayout["fields"];var otherField=layoutFields.OfType<JObject>().Single(x=>(string)x["name"]=="Other");
                Check((bool)itemLayout["complete"]&&(int)itemLayout["size"]==Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<Item>()&&(int)otherField["offset"]==Unity.Collections.LowLevel.Unsafe.UnsafeUtility.GetFieldOffset(typeof(Item).GetField("Other"))&&layoutFields.OfType<JObject>().All(x=>(int)x["offset"]+(int)x["layout"]["size"]<=(int)itemLayout["size"]),"source buffer native field offsets and recursive Entity layout measured without worker ABI assumptions");checks++;
                var abiMissing=new JArray();var abiArray=new FixedAbi[128];abiArray[0].Flag=true;abiArray[0].Tail=0x12345678;var abiValue=S002InitialWorldEntityExport.CaptureValue(abiArray,"fixed_abi",abiMissing);var abiLayout=abiValue["native_layout"];var abiFields=((JArray)abiLayout["fields"]).OfType<JObject>();var dataField=abiFields.Single(x=>(string)x["name"]=="Data");var boolField=abiFields.Single(x=>(string)x["name"]=="Flag");var abiBytes=Convert.FromBase64String((string)abiValue["base64"]);
                Check((bool)abiLayout["complete"]&&(int)abiLayout["size"]==Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<FixedAbi>()&&(int)boolField["layout"]["scalar_width_bytes"]==Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<bool>()&&(int)dataField["fixed_buffer"]["length"]==5&&(string)dataField["fixed_buffer"]["element_type"]=="System.Byte"&&abiBytes[(int)boolField["offset"]]==1&&BitConverter.ToInt32(abiBytes,(int)abiFields.Single(x=>(string)x["name"]=="Tail")["offset"])==0x12345678&&abiMissing.Count==0,"actual native bool width and fixed-buffer element count/offset retained");checks++;
                var fixedList=new FixedList512Bytes<int>();fixedList.Add(71);fixedList.Add(-9);var fixedMissing=new JArray();var fixedValue=S002InitialWorldEntityExport.CaptureValue(fixedList,"fixed_list",fixedMissing);
                Check((string)fixedValue["codec"]=="fixed-list-v1"&&(int)fixedValue["length"]==2&&(int)fixedValue["capacity"]==fixedList.Capacity&&(string)fixedValue["items"][0]["value"]=="71"&&(string)fixedValue["items"][1]["value"]=="-9"&&!(bool)fixedValue["unused_storage_exported"]&&fixedMissing.Count==0,"source FixedList current typed items/length/capacity without unused storage");checks++;
                var bclMissing=new JArray();var pool=new Stack<int>();pool.Push(3);pool.Push(2);pool.Push(1);var fifo=new Queue<int>();fifo.Enqueue(4);fifo.Enqueue(5);var set=new HashSet<int>{9,2,7};
                var poolValue=S002InitialWorldEntityExport.CaptureValue(pool,"pool",bclMissing);var fifoValue=S002InitialWorldEntityExport.CaptureValue(fifo,"fifo",bclMissing);var setValue=S002InitialWorldEntityExport.CaptureValue(set,"set",bclMissing);
                Check((string)poolValue["codec"]=="bcl-stack-v1"&&(string)poolValue["entries"][0]["value"]=="1"&&(string)poolValue["entries"][2]["value"]=="3"&&pool.Peek()==1&&pool.Count==3,"exact BCL Stack top order preserved without pop");checks++;
                Check((string)fifoValue["codec"]=="bcl-queue-v1"&&(string)fifoValue["entries"][0]["value"]=="4"&&(string)fifoValue["entries"][1]["value"]=="5"&&fifo.Peek()==4&&fifo.Count==2,"exact BCL Queue front order preserved without dequeue");checks++;
                Check((string)setValue["codec"]=="bcl-hash-set-v1"&&((JArray)setValue["entries"]).Select(x=>int.Parse((string)x["value"])).SequenceEqual(set)&&(bool)setValue["comparer"]["is_default_bcl_comparer"]&&!(bool)setValue["future_mutation_iteration_authority"]&&set.Count==3&&bclMissing.Count==0,"HashSet actual readonly iteration/default comparer distinguished from future storage order");checks++;
                var customSet=new HashSet<int>(new CustomComparer()){11};CustomComparer.Calls=0;var customMissing=new JArray();var customValue=S002InitialWorldEntityExport.CaptureValue(customSet,"custom_set",customMissing);
                Check(!(bool)customValue["comparer"]["is_default_bcl_comparer"]&&customMissing.Count==1&&CustomComparer.Calls==0,"custom HashSet comparer binding rejected without invoking user equality/hash code");checks++;
                var spatialRoot=new GameObject("CodecSpatialRoot");
                try
                {
                    spatialRoot.transform.SetLocalPositionAndRotation(new Vector3(7.25f,-3.5f,11),Quaternion.Euler(19,37,11));spatialRoot.transform.localScale=new Vector3(2,3,0.5f);
                    var mount=new GameObject("Mount");mount.transform.SetParent(spatialRoot.transform,false);mount.transform.localPosition=new Vector3(-0.5f,2,4);mount.transform.localRotation=Quaternion.Euler(33,7,9);mount.transform.localScale=new Vector3(0.7f,1.2f,2);
                    var door=new GameObject("DoorZ");door.transform.SetParent(mount.transform,false);door.transform.localPosition=new Vector3(0.1f,0.2f,0.3f);
                    var renderer=mount.AddComponent<MeshRenderer>();renderer.localBounds=new Bounds(new Vector3(2,3,4),new Vector3(5,6,7));int originalComponents=mount.GetComponentCount();
                    var spatialMissing=new JArray();var template=S002InitialWorldEntityExport.CaptureValue(spatialRoot,"spatial",spatialMissing);var hierarchy=(JArray)template["ordered_preorder_hierarchy"];
                    Check((string)template["codec"]=="unity-gameobject-spatial-template-v1"&&hierarchy.Count==3&&(int)hierarchy[2]["parent_ordinal"]==1&&(string)hierarchy[2]["name"]["value"]=="DoorZ"&&JToken.DeepEquals(hierarchy[1]["frames"]["local_to_world"],S002InitialWorldEntityExport.CaptureValue(mount.transform.localToWorldMatrix,"expected_matrix",new JArray()))&&JToken.DeepEquals(hierarchy[1]["renderers"][0]["local_bounds"],S002InitialWorldEntityExport.CaptureValue(renderer.localBounds,"expected_bounds",new JArray()))&&mount.GetComponentCount()==originalComponents&&spatialMissing.Count==1&&!(bool)template["template_complete"],"ordered nonidentity hierarchy and readonly renderer bounds without claiming component-template closure");checks++;
                    var transformMissing=new JArray();var transformValue=S002InitialWorldEntityExport.CaptureValue(door.transform,"door",transformMissing);var absent=S002InitialWorldEntityExport.CaptureValue(null,"absent_door",transformMissing);
                    Check((string)transformValue["codec"]=="unity-transform-spatial-v1"&&((JArray)transformValue["ancestry_leaf_to_root"]).Count==3&&JToken.DeepEquals(transformValue["frames"]["world_position"],S002InitialWorldEntityExport.CaptureValue(door.transform.position,"expected_door",new JArray()))&&(string)absent["codec"]=="null-v1"&&transformMissing.Count==0,"actual door Transform frame and optional absence preserved");checks++;
                }
                finally{UnityEngine.Object.DestroyImmediate(spatialRoot);}
                var missing=new JArray();var profile=ScriptableObject.CreateInstance<Profile>();profile.hideFlags=HideFlags.HideAndDontSave;profile.Initialize();
                try{var value=S002InitialWorldEntityExport.CaptureValue(profile,"profile",missing,output,artifacts);Check(value.ToString().Contains("allowedRoleIds")&&!value.ToString().Contains("::Cache")&&missing.Count==0,"transient private serialized config fields without nonserialized cache");checks++;}finally{UnityEngine.Object.DestroyImmediate(profile);}
                var values=new NativeArray<int>(256,Allocator.Temp);try
                {values[255]=-17;var encoded=S002InitialWorldEntityExport.CaptureValue(values,"private/counts",missing,output,artifacts);Check((string)encoded["codec"]=="binary-v1"&&(int)encoded["count"]==256&&(int)encoded["byte_count"]==1024,"native int array uses bound binary artifact");checks++;}finally{values.Dispose();}
                var managed=new int[256];managed[4]=int.MinValue;var managedValue=S002InitialWorldEntityExport.CaptureValue(managed,"private/managed-counts",missing,output,artifacts);Check((int)managedValue["byte_count"]==1024,"large managed primitive array uses binary artifact");checks++;
                using(var bits=new NativeBitArray(9,Allocator.Temp))
                {bits.Set(8,true);var encoded=S002InitialWorldEntityExport.CaptureValue(bits,"bits",missing,output,artifacts);Check((int)encoded["bit_length"]==9&&File.ReadAllBytes(Path.Combine(output,(string)encoded["artifact"]))[1]==1,"bit array packs lsb0 and preserves logical length");checks++;}
                using(var builder=new BlobBuilder(Allocator.Temp))
                {
                    ref var root=ref builder.ConstructRoot<BlobRoot>();root.Value=421;
                    using(var blob=builder.CreateBlobAssetReference<BlobRoot>(Allocator.Temp))
                    {var encoded=S002InitialWorldEntityExport.CaptureValue(blob,"blob",missing,output,artifacts);Check((string)encoded["encoding"]=="unity-entities-blob-v1"&&(int)encoded["byte_count"]>4,"official serialized blob header and payload artifact");checks++;}
                }
                string name=artifacts.Properties().First().Name;var bytes=File.ReadAllBytes(Path.Combine(output,name));bytes[0]^=1;File.WriteAllBytes(Path.Combine(output,name),bytes);
                bool refused=false;try{S002InitialWorldEntityExport.Capture(em,output,artifacts);}catch(InvalidOperationException){refused=true;}Check(refused,"tampered existing content artifact fails closed");checks++;
            }
            Check(ReferenceEquals(previous,World.DefaultGameObjectInjectionWorld),"default World unchanged");
            Debug.Log("[InitialWorldCodec] result=Passed checks="+checks+" sourceOnlyPacket=True allocatorAuthority=False nativeWorldTest=True");
        }
    }
}
#endif
