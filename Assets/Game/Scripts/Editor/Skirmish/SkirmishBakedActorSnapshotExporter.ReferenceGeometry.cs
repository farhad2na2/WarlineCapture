#if UNITY_EDITOR
using System;
using System.Reflection;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace Game.Editor {
 public static partial class SkirmishBakedActorSnapshotExporter {
  static string BuildingReferenceArgument(string[] args,string flag,string fallback) {
   int at=Array.IndexOf(args,flag);
   if(at<0)return fallback;
   if(at+1>=args.Length||string.IsNullOrWhiteSpace(args[at+1])||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("Value required for "+flag);
   return args[at+1];
  }
  static JObject BuildingReferenceVector(Vector3 value)=>new JObject{{"x",value.x},{"y",value.y},{"z",value.z}};
  static JObject BuildingReferenceBounds(Bounds value)=>new JObject{{"center",BuildingReferenceVector(value.center)},{"size",BuildingReferenceVector(value.size)}};
  static JObject CaptureBuildingReferenceGeometry(GameObject prefab,Component authoring) {
   bool measured=BuildingModelBounds.TryMeasure(prefab,out Bounds bounds);
   Vector2Int cells=measured?BuildingModelBounds.EnclosingCells(bounds):default;
   object configured=authoring==null?null:authoring.GetType().GetProperty("ConfiguredFootprintCells",BindingFlags.Public|BindingFlags.Instance)?.GetValue(authoring);
   var renderers=new JArray();
   foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)) {
    Mesh mesh=null;Bounds local=default;
    if(renderer is SkinnedMeshRenderer skin&&skin.sharedMesh!=null){mesh=skin.sharedMesh;local=skin.localBounds;}
    else if(renderer is MeshRenderer&&renderer.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null){mesh=filter.sharedMesh;local=mesh.bounds;}
    if(mesh==null)continue;
    var matrix=prefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;var matrixValues=new JArray();
    for(int r=0;r<4;r++)for(int c=0;c<4;c++)matrixValues.Add(matrix[r,c]);
    bool active=true;for(var t=renderer.transform;t!=null&&t!=prefab.transform;t=t.parent)if(!t.gameObject.activeSelf)active=false;
    renderers.Add(new JObject{{"path",AnimationUtility.CalculateTransformPath(renderer.transform,prefab.transform)},{"renderer_type",renderer.GetType().FullName},{"enabled",renderer.enabled},{"active_below_root",active},{"participates_in_original_measurement",renderer.enabled&&active},{"mesh",SourceAsset(mesh)},{"mesh_or_skin_local_bounds",BuildingReferenceBounds(local)},{"root_relative_matrix_row_major",matrixValues}});
   }
   string path=AssetDatabase.GetAssetPath(prefab);var footprint=configured is Vector2Int v?new JObject{{"x",v.x},{"y",v.y}}:null;
   return new JObject{{"authority","OriginalBuildingModelBounds.TryMeasure_AndRuntimeDefinitionFactory"},{"prefab_dependency_hash",AssetDatabase.GetAssetDependencyHash(path).ToString()},{"configured_footprint_cells",footprint},{"has_evaluated_bounds",measured},{"evaluated_prefab_local_bounds",measured?BuildingReferenceBounds(bounds):null},{"original_enclosing_cells",new JObject{{"x",cells.x},{"y",cells.y}}},{"renderers",renderers},{"final_footprint_authority","definition.fields.FootprintCells; configured dimensions are separate"}};
  }
 }
}
#endif
