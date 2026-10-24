using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Newtonsoft.Json.Serialization;
// INERT API TEST DOUBLES ONLY. No Unity/native compiler, AssetDatabase load/bake,
// metadata correctness, actual budget arithmetic, rendering or gameplay acceptance.
namespace UnityEngine{
 [AttributeUsage(AttributeTargets.Field)]public sealed class SerializeField:Attribute{}
 [AttributeUsage(AttributeTargets.Class)]public sealed class CreateAssetMenuAttribute:Attribute{public string menuName;}
 public class Object{internal string AssetPath;internal bool Dirty,Dead;public static int Created,Destroyed;public Object(){Created++;}public static T Instantiate<T>(T source)where T:Object{var value=(T)Activator.CreateInstance(source.GetType(),true);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source),value);return value;}public static void DestroyImmediate(Object value){if(ReferenceEquals(value,null)||value.Dead)return;value.Dead=true;Destroyed++;}public static bool operator==(Object a,Object b){bool an=ReferenceEquals(a,null)||(!ReferenceEquals(a,null)&&a.Dead),bn=ReferenceEquals(b,null)||(!ReferenceEquals(b,null)&&b.Dead);return an||bn?an==bn:ReferenceEquals(a,b);}public static bool operator!=(Object a,Object b)=>!(a==b);public override bool Equals(object x)=>ReferenceEquals(this,x);public override int GetHashCode()=>base.GetHashCode();}
 public class ScriptableObject:Object{public static T CreateInstance<T>()where T:ScriptableObject,new()=>new T();}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public static class Mathf{public static float Abs(float x)=>Math.Abs(x);}
 public static class Resources{public static readonly Dictionary<string,Object> Values=new Dictionary<string,Object>();public static T Load<T>(string name)where T:Object=>Values.TryGetValue(name,out var value)?(T)value:null;}
 sealed class FieldsResolver:DefaultContractResolver{protected override IList<JsonProperty>CreateProperties(Type type,MemberSerialization member){var result=new List<JsonProperty>();for(var t=type;t!=null&&t!=typeof(Object);t=t.BaseType)foreach(var f in t.GetFields(BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))if(f.IsPublic||f.GetCustomAttribute<SerializeField>()!=null){var p=base.CreateProperty(f,member);p.Readable=true;p.Writable=true;result.Add(p);}return result;}}
 public static class JsonUtility{static readonly JsonSerializerSettings Settings=new JsonSerializerSettings{ContractResolver=new FieldsResolver(),ReferenceLoopHandling=ReferenceLoopHandling.Error};public static string ToJson(object value)=>JsonConvert.SerializeObject(value,Formatting.None,Settings);public static void FromJsonOverwrite(string text,object target)=>JsonConvert.PopulateObject(text,target,Settings);}
}
namespace UnityEditor{
 public static class EditorApplication{public static bool isPlayingOrWillChangePlaymode;}
 public static class EditorUtility{public static bool IsDirty(UnityEngine.Object x)=>x.Dirty;public static bool IsPersistent(UnityEngine.Object x)=>!string.IsNullOrEmpty(x.AssetPath);}
 public static class AssetDatabase{public static readonly Dictionary<string,string> Guids=new Dictionary<string,string>();public static string GetAssetPath(UnityEngine.Object x)=>x.AssetPath??"";public static string GUIDToAssetPath(string id)=>Guids.TryGetValue(id,out var p)?p:"";}
 public sealed class SerializedObject{readonly object value;public SerializedObject(object value){this.value=value;}public SerializedProperty FindProperty(string name)=>new SerializedProperty(value,name);public void ApplyModifiedPropertiesWithoutUndo(){}}
 public sealed class SerializedProperty{readonly object value;readonly FieldInfo field;public SerializedProperty(object value,string name){this.value=value;field=value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)??throw new Exception("Missing actual serialized field "+name);}public string stringValue{set{this.field.SetValue(this.value,value);}}public int intValue{set{this.field.SetValue(this.value,this.field.FieldType.IsEnum?Enum.ToObject(this.field.FieldType,value):(object)value);}}}
}
namespace Game.Configs{
 public sealed class AssetRef{public string AssetGUID;}
 public struct OperationMapBoundsConfig{public UnityEngine.Vector3 WorldMin,WorldMax,PlayableMin,PlayableMax,CameraMin,CameraMax;}
 public struct OperationMapGridMetadataConfig{public string AssetGuid,ContentHash;public UnityEngine.Vector3 Origin;public float CellSize;public int AuthoredBlockedCellCount;}
 public struct OperationMapSurfaceMetadataConfig{public string AssetGuid,ContentHash,RuntimeBlobHash;public int SurfaceCount;public float MinimumHeight,MaximumHeight;}
 public struct OperationMapNavigationMetadataConfig{public string AuthoredSubSceneGuid;}
 public sealed class OperationMapDefinition:UnityEngine.ScriptableObject{public string OperationMapId,SourceIdentityHash,ContentHash,GeneratedMetadataHash;public int SchemaVersion=1,ContentVersion=4;public OperationMapBoundsConfig Bounds;public OperationMapGridMetadataConfig GridMetadata;public OperationMapSurfaceMetadataConfig SurfaceMetadata;public OperationMapNavigationMetadataConfig NavigationMetadata;public AssetRef SourceSceneReference,MapSurfaceDataReference;public bool TryValidateMetadata(out string e){e=null;return true;}public bool TryValidateLocalContentReferences(out string e){e=null;return true;}}
 public class SkirmishArmyProfileConfig:UnityEngine.ScriptableObject{}public class SkirmishStartPackageConfig:UnityEngine.ScriptableObject{}public class SkirmishEconomyConfig:UnityEngine.ScriptableObject{}public class SkirmishUpgradeConfig:UnityEngine.ScriptableObject{}public class SkirmishIntelConfig:UnityEngine.ScriptableObject{}public class SkirmishRoleCatalogConfig:UnityEngine.ScriptableObject{}
 public class SkirmishMissionLogicalBindingConfig:UnityEngine.ScriptableObject{}public class SkirmishObjectiveAreaConfig{}public class SkirmishCompiledObjectiveAreas{}public class SkirmishMissionViewProfile{}
 public sealed class SkirmishExpansionAuthoredSet{public SkirmishObjectiveConfig ObjectiveBa;public SkirmishArmyProfileConfig ArmyGround;public SkirmishStartPackageConfig StartField,StartEstablished;public SkirmishEconomyConfig Economy;public SkirmishUpgradeConfig Upgrades;public SkirmishIntelConfig Intel;public SkirmishRoleCatalogConfig Roles;}
 public sealed class SkirmishExpansionCatalogConfig:UnityEngine.ScriptableObject{public const string ResourceName="SkirmishExpansionCatalog";public SkirmishExpansionAuthoredSet Profiles;public SkirmishExpansionAuthoredSet ToAuthoredSet()=>Profiles;}
 public struct SkirmishResolvedForceEntry{}public sealed class SkirmishSetupMatrixRow{}
 public static class SkirmishSetupMatrixTable{public static bool TryLoadPackaged(out IReadOnlyList<SkirmishSetupMatrixRow> rows,out string error){rows=new SkirmishSetupMatrixRow[0];error=null;return true;}}
 public static class SkirmishSetupCompiler{
  public static int DiagnosticCalls;public static readonly List<Game.Skirmish.Contracts.SkirmishSizeId> Sizes=new List<Game.Skirmish.Contracts.SkirmishSizeId>();public static SkirmishResolvedStructureEntry[] ExpectedUnion;
  // Inert DIAGNOSTIC OUTPUT DOUBLE: returned structure identities come from the
  // reviewed test fixture, not a claimed production compiler/budget execution.
  public static bool TryInspectStaticAuthoringBudget(SkirmishScenarioDefinitionConfig definition,Game.Skirmish.Contracts.SkirmishDifficultyId difficulty,Game.Skirmish.Contracts.SkirmishSizeId size,int seed,Game.Skirmish.Contracts.SkirmishContentManifest manifest,IReadOnlyList<SkirmishSetupMatrixRow> matrix,out SkirmishStaticAuthoringBudget budget,out List<Game.Skirmish.Contracts.SkirmishCompileReason> reasons){DiagnosticCalls++;Sizes.Add(size);reasons=new List<Game.Skirmish.Contracts.SkirmishCompileReason>();var entries=ExpectedUnion.Where(x=>size!=Game.Skirmish.Contracts.SkirmishSizeId.Standard||x.StructureId!=Game.Skirmish.Contracts.SkirmishStructureIds.RefineryModule).ToArray();for(int i=0;i<entries.Length;i++)if(entries[i].DesignatedBase){definition.MapLayout.TryGetAnchor(entries[i].FactionId==1?"base.player":"base.enemy",out var a);entries[i].SpawnAnchorId=a.AnchorId;}budget=new SkirmishStaticAuthoringBudget(definition.CatalogId,definition.DefinitionId,size,new SkirmishResolvedForceEntry[0],entries,false,new string[0]);return true;}
 }

}
namespace Game.Editor{public static class SkirmishMapLayoutBuilder{} public static partial class SkirmishGroundSourceFitSurvey{
 static void Require(bool condition,string error){if(!condition)throw new ArgumentException(error);}
 static string Hash(string path){using var h=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static bool TryCreateGroundObjective30Candidate(JObject row,Game.Configs.OperationMapDefinition source,out GroundObjective30CandidateOwner owner,out string error){owner=null;error="Unexecuted legacy source-specific constructor in portable API harness";return false;}
}}
