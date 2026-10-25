using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Game.Configs
{
 // Four exact native-input facts from the pinned 6000.5.8f1 asset-only capture.
 // This is not an Euler implementation, approximation, or runtime fallback.
 public sealed class SourceOperationMapRotationReference
 {
  public const string QualifiedReferenceSha256="da68028389ba33bd0940fe3f98c6450981a8fc9b848b6f777464cd5f3fe687a5";
  public const string QualifiedToQuaternionSourceSha256="b7db53745458f3c55d34354a9ec363fb7f85c9995c481cff2230b3ae0116a8f3";
  readonly Dictionary<string,quaternion> values;
  public string ReferenceSha256 {get;}
  public string SourceAssetSha256 {get;}
  public string CapturedEngineSha256 {get;}
  SourceOperationMapRotationReference(Dictionary<string,quaternion> source,string referenceSha,string assetSha,string engineSha){values=source;ReferenceSha256=referenceSha;SourceAssetSha256=assetSha;CapturedEngineSha256=engineSha;}
  static void Require(bool condition,string reason){if(!condition)throw new NotSupportedException("Operation-map rotation reference: "+reason);}
  static string Sha(byte[] bytes){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(bytes).Select(x=>x.ToString("x2")));}
  static int[] IntBits(JToken token,int count){var array=token as JArray;Require(array!=null&&array.Count==count,"raw-bit width");var result=new int[count];for(int i=0;i<count;i++){Require(array[i].Type==JTokenType.Integer,"integer raw bits");long v=(long)array[i];Require(v>=int.MinValue&&v<=int.MaxValue,"int32 raw bits");result[i]=(int)v;}return result;}
  static string Key(int[] v)=>string.Join(",",v.Select(x=>x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
  static float Float(int bits)=>BitConverter.ToSingle(BitConverter.GetBytes(bits),0);
  static int Bits(float value)=>BitConverter.ToInt32(BitConverter.GetBytes(value),0);
  public static SourceOperationMapRotationReference LoadPinned(string path,string referenceSha256)
  {
   Require(referenceSha256==QualifiedReferenceSha256,"qualified native capture identity");
   Require(Path.IsPathRooted(path),"absolute reference path");var info=new FileInfo(path);Require(info.Exists&&info.Length<=1048576,"bounded reference file");byte[] bytes=File.ReadAllBytes(path);Require(bytes.Length<=1048576,"bounded bytes after read");Require(Sha(bytes)==referenceSha256,"reference bytes SHA256");
   using(var text=new StringReader(new UTF8Encoding(false,true).GetString(bytes)))using(var reader=new JsonTextReader(text){DateParseHandling=DateParseHandling.None,MaxDepth=32}){
    var root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});Require(!reader.Read(),"trailing JSON data");
    Require((int?)root["schema_version"]==1&&(string)root["kind"]=="NativeOriginalOperationMapMetadataAssetReference","schema/kind");Require((string)root["scope"]=="SourceAssetDataOnly_NotMissionStartup_NotNativeRootAbsence","asset-only scope");Require((bool?)root["world_created"]==false&&(bool?)root["mission_run"]==false&&(bool?)root["native_root_state_captured"]==false,"no world/root authority");Require((bool?)root["little_endian"]==true&&BitConverter.IsLittleEndian,"raw bit byte order");
    const string asset="Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset",assetSha="68a631ca108e01c9a17839d2359e409226cdb009193f68ba910fa6f0f0a082b8";
    Require((string)root["source_asset"]?["path"]==asset&&(string)root["source_asset"]?["sha256"]==assetSha&&(string)root["source_pins"]?[asset]==assetSha&&(string)root["source_asset"]?["guid"]=="cbfc2f62e3142413485019bd83197539"&&(long?)root["source_asset"]?["local_id"]==11400000,"qualified source asset");
    Require((string)root["rotation_provider"]?["declaring_type"]=="Game.Configs.OperationMapDefinition"&&(string)root["rotation_provider"]?["name"]=="ToQuaternion"&&(string)root["rotation_provider"]?["il_sha256"]=="194144440d5aac4db713a2ededd4be53581646b18548e6e41d3b0104ea8a7570","captured conversion IL authority");
    const string engine="c2d46bbaa42a8f5abfdfa04ea0f6ebc98765188c350e0fd803bb08440a2a17af";
    Require((string)root["unity_version"]=="6000.5.8f1"&&(string)root["editor_executable"]?["sha256"]==engine&&(string)root["producer_assemblies"]?["UnityEngine.CoreModule"]?["sha256"]=="8aa998f99dbe14e2c5dcfa03786094ba9711e2f88d72fcc4d2f410361101f018"&&(string)root["producer_assemblies"]?["Unity.Mathematics"]?["sha256"]=="87bdf65c5fc23bbef3afd323cc2da9cb1e2cfb94f3f36eddf29f49c58bc79661","captured native engine authority");
    var rows=root["rotations"]as JArray;var anchors=root["anchors"]as JArray;var cameras=root["cameras"]as JArray;Require(rows!=null&&rows.Count==9&&anchors?.Count==8&&cameras?.Count==1,"finite source rows");
    var values=new Dictionary<string,quaternion>(StringComparer.Ordinal);var outputBits=new Dictionary<string,int[]>(StringComparer.Ordinal);
    for(int index=0;index<9;index++){
     var row=rows[index];bool anchor=index<8;int item=anchor?index:index-8;var original=anchor?anchors[item]:cameras[item];Require((string)row["domain"]==(anchor?"anchor":"camera")&&(int?)row["index"]==item&&(string)row["id"]==(string)original["id"],"source row identity/order");int[] input=IntBits(row["euler_bits"],3),output=IntBits(row["quaternion_bits"],4);Require(output.SequenceEqual(IntBits(original["rotation_bits"],4)),"original factory output correspondence");string key=Key(input);if(outputBits.TryGetValue(key,out var old)){Require(old.SequenceEqual(output),"duplicate input has contradictory output");continue;}outputBits.Add(key,output);values.Add(key,new quaternion(Float(output[0]),Float(output[1]),Float(output[2]),Float(output[3])));
    }
    Require(values.Count==4,"finite four input keys");return new SourceOperationMapRotationReference(values,referenceSha256,assetSha,engine);
   }
  }
  public quaternion Resolve(Vector3 euler,string currentToQuaternionSourceSha256)
  {
   Require(currentToQuaternionSourceSha256==QualifiedToQuaternionSourceSha256,"current native conversion-member drift");string key=Key(new[]{Bits(euler.x),Bits(euler.y),Bits(euler.z)});if(!values.TryGetValue(key,out var value))throw new NotSupportedException("Operation-map rotation input raw bits not captured: "+key);return value;
  }
 }
}
