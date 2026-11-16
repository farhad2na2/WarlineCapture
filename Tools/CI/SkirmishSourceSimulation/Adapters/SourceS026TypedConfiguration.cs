using System;using System.Linq;using Game.Configs;using Game.Skirmish.Contracts;using Newtonsoft.Json.Linq;
namespace Game.Runtime {
// Explicit source-only configuration adapter. It never assigns a playable dispatch index.
internal static class SourceS026TypedConfiguration {
 internal const string SourcePath="Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
 internal const string AliasPath="Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset";
 internal const string LayoutFingerprint="c74d05162216ff09c5b953966a35e7d56bf9f1935a55cf7076d23fe23badc18e";
 internal static SkirmishScenarioDefinitionConfig Configure(SourceConfigMaterializer values,JObject profiles,SkirmishMapLayoutConfig layout,OperationMapDefinition source){
 string[] keys={"objective","army","start","economy","upgrades","intel","roles"};
 if(values==null||profiles==null||layout==null||source==null||profiles.Properties().Count()!=keys.Length||profiles.Properties().Any(p=>!keys.Contains(p.Name)))throw new ArgumentException("Exact S026 typed profile references required");
 string[] paths={"SkirmishObjective_BA","SkirmishArmy_GroundManeuver","SkirmishStart_Established","SkirmishEconomy_V1","SkirmishUpgrade_V1","SkirmishIntel_SharedFog","SkirmishRoleCatalog_V1"};
 for(int i=0;i<keys.Length;i++)if((string)profiles[keys[i]]!="Assets/Game/Configs/SkirmishExpansion/Shared/"+paths[i]+".asset")throw new NotSupportedException("S026 CC/BA/G/E profile changed: "+keys[i]);
 var actual=values.ByPath<OperationMapDefinition>(SourcePath);
 if(!ReferenceEquals(actual,source)){
  var alias=values.ByPath<OperationMapDefinition>(AliasPath);var binding=alias.SourceBinding;
  if(!ReferenceEquals(alias,source)||!binding.IsConfigured||!binding.TryValidate(alias.OperationMapId,out _)||
     alias.OperationMapId!=layout.OperationMapId||binding.SourceOperationMapId!=actual.OperationMapId||
     binding.SourceIdentityHash!=actual.SourceIdentityHash||alias.SourceIdentityHash!=actual.SourceIdentityHash||
     binding.SourceContentHash!=actual.ContentHash||layout.WorldBinding.SourceHash!=actual.ContentHash)
   throw new NotSupportedException("Exact S026 logical alias and DenseCity source binding required");
 }
 return ConfigureDeclared(values,profiles,layout,source,"S026");
 }
 // Shared data-only construction seam. The original setup compiler, source
 // bindings, and catalog admission still validate the resulting definition.
 internal static SkirmishScenarioDefinitionConfig ConfigureDeclared(SourceConfigMaterializer values,JObject profiles,SkirmishMapLayoutConfig layout,OperationMapDefinition source,string catalogId){
 return SourceTypedMissionConfiguration.ConfigureDeclared(values,profiles,layout,source,catalogId);
 }
 internal static void RequireFrozenMeasuredLayout(SkirmishMapLayoutConfig layout){if(layout==null||layout.BindingValueFormat!=SkirmishBindingValueFormat.CanonicalBinaryV2||!layout.WorldBinding.HasMeasuredDeployments||!layout.WorldBinding.BindAllConfigurations||SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)!=LayoutFingerprint)throw new NotSupportedException("Frozen S026 measured layout required");}
 internal static int RequireHonestSourceOnlyCatalog(SkirmishBattleCatalogConfig catalog){if(catalog==null||!catalog.TryGet("S026",out var row)||row.Status!=SkirmishBattleCatalogStatus.Planned||row.PlayableScenarioIndex!=-1||row.IsPlayable)throw new NotSupportedException("S026 must retain canonical Planned/-1 source-only identity");if(SkirmishMissionDispatch.TryGetLegacyIndex("S026",out _))throw new InvalidOperationException("S026 must not alias a legacy playable index");return -1;}
}
}
