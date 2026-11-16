using System;using System.Linq;using Game.Configs;using Game.Skirmish.Contracts;using Newtonsoft.Json.Linq;
namespace Game.Runtime {
// Catalog-agnostic construction of the exact typed definition graph. Context,
// source, layout and logical-binding authority are validated by the caller.
internal static class SourceTypedMissionConfiguration {
 internal static SkirmishScenarioDefinitionConfig ConfigureDeclared(AuthoredSourceConfigGraphMaterializer values,JObject profiles,SkirmishMapLayoutConfig layout,OperationMapDefinition source,string catalogId){
  string[] keys={"objective","army","start","economy","upgrades","intel","roles"};
  if(values==null||profiles==null||layout==null||source==null||!SkirmishMissionDispatch.Canonical(catalogId)||profiles.Properties().Count()!=keys.Length||profiles.Properties().Any(p=>!keys.Contains(p.Name)||p.Value.Type!=JTokenType.String))throw new ArgumentException("Exact canonical mission and typed profile references required");
  // Ownership of this transient ScriptableObject transfers to the caller.
  var d=UnityEngine.ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();d.Configure(catalogId,values.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),values.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),values.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),values.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),values.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),values.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),values.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]),layout);d.BindCampaignMap(source);return d;
 }
}
}
