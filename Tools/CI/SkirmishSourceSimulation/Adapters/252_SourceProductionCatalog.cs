using System;using System.Collections.Generic;using Game.Configs;using Game.Skirmish.Contracts;using Unity.Entities;
namespace Game.Runtime {
// Opaque source prefab identity plus captured complete ECS template, never a Unity GameObject.
public sealed class SourceUnitPrefab {
 internal readonly string name;internal readonly Entity Template;
 public readonly string SourceAssetPath,SourceAssetGuid;public readonly long SourceAssetLocalId;
 public bool IsAssetIdentityOnly=>Template==Entity.Null;
 internal SourceUnitPrefab(string key,Entity template){if(string.IsNullOrEmpty(key)||template==Entity.Null)throw new ArgumentException("Actual prefab key/template required");name=key;Template=template;}
 private SourceUnitPrefab(string key,string path,string guid,long localId){
  if(string.IsNullOrEmpty(key)||key.Length>256||key.IndexOf('\0')>=0||string.IsNullOrEmpty(path)||path.Length>1024||!path.StartsWith("Assets/",StringComparison.Ordinal)||path.IndexOf('\\')>=0||path.IndexOf('\0')>=0||Array.Exists(path.Split('/'),part=>part==".."||part=="."||part.Length==0)||guid==null||guid.Length!=32||Array.Exists(guid.ToCharArray(),c=>!(c>='0'&&c<='9'||c>='a'&&c<='f'))||localId==0)
   throw new ArgumentException("Exact authored building prefab asset identity required");
  name=key;SourceAssetPath=path;SourceAssetGuid=guid;SourceAssetLocalId=localId;Template=Entity.Null;
 }
 public static SourceUnitPrefab FromAssetIdentity(string key,string path,string guid,long localId)=>new SourceUnitPrefab(key,path,guid,localId);
}
public sealed class SkirmishProductionCatalogRecord:IComponentData {public SkirmishProductionCatalog Catalog;}
public sealed partial class SkirmishProductionCatalog {public SkirmishResolvedSetup Setup{get;}readonly Dictionary<string,SkirmishRoleOverlay> roles;public SkirmishProductionCatalog(SkirmishResolvedSetup setup,IReadOnlyDictionary<string,SkirmishRoleOverlay> actualRoleBindings,bool completeActualCatalog){if(setup==null||actualRoleBindings==null||!completeActualCatalog)throw new NotSupportedException("Complete actual compiled production catalog metadata required");Setup=setup;roles=new Dictionary<string,SkirmishRoleOverlay>(StringComparer.Ordinal);foreach(var p in actualRoleBindings){if(string.IsNullOrEmpty(p.Key)||roles.ContainsKey(p.Key))throw new NotSupportedException("Actual unique prefab catalog keys required");roles.Add(p.Key,p.Value);}}public bool TryRole(string key,out SkirmishRoleOverlay overlay)=>roles.TryGetValue(key,out overlay);}
}
