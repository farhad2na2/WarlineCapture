using System;using System.Linq;using Game.Configs;using Game.Skirmish.Contracts;using Newtonsoft.Json.Linq;
namespace Game.Runtime {
// Explicit source-only configuration adapter. It never assigns a playable dispatch index.
internal static class SourceS026TypedConfiguration {
 internal const string SourcePath="Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
 internal const string LayoutFingerprint="c74d05162216ff09c5b953966a35e7d56bf9f1935a55cf7076d23fe23badc18e";
 internal static SkirmishScenarioDefinitionConfig Configure(SourceConfigMaterializer values,JObject profiles,SkirmishMapLayoutConfig layout,OperationMapDefinition source){
 string[] keys={"objective","army","start","economy","upgrades","intel","roles"};
 if(values==null||profiles==null||layout==null||source==null||profiles.Properties().Count()!=keys.Length||profiles.Properties().Any(p=>!keys.Contains(p.Name)))throw new ArgumentException("Exact S026 typed profile references required");
 string[] paths={"SkirmishObjective_BA","SkirmishArmy_GroundManeuver","SkirmishStart_Established","SkirmishEconomy_V1","SkirmishUpgrade_V1","SkirmishIntel_SharedFog","SkirmishRoleCatalog_V1"};
 for(int i=0;i<keys.Length;i++)if((string)profiles[keys[i]]!="Assets/Game/Configs/SkirmishExpansion/Shared/"+paths[i]+".asset")throw new NotSupportedException("S026 CC/BA/G/E profile changed: "+keys[i]);
 var actual=values.ByPath<OperationMapDefinition>(SourcePath);
 if(!ReferenceEquals(actual,source))throw new NotSupportedException("Actual S026 DenseCity source identity required");
 var d=new SkirmishScenarioDefinitionConfig();d.Configure("S026",values.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),values.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),values.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),values.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),values.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),values.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),values.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]),layout);d.BindCampaignMap(source);return d;
 }
 internal static void RequireFrozenMeasuredLayout(SkirmishMapLayoutConfig layout){if(layout==null||layout.BindingValueFormat!=SkirmishBindingValueFormat.CanonicalBinaryV2||!layout.WorldBinding.HasMeasuredDeployments||!layout.WorldBinding.BindAllConfigurations||SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)!=LayoutFingerprint)throw new NotSupportedException("Frozen S026 measured layout required");}
 internal static int RequireHonestSourceOnlyCatalog(SkirmishBattleCatalogConfig catalog){if(catalog==null||!catalog.TryGet("S026",out var row)||row.Status!=SkirmishBattleCatalogStatus.Planned||row.PlayableScenarioIndex!=-1||row.IsPlayable)throw new NotSupportedException("S026 must retain canonical Planned/-1 source-only identity");if(SkirmishMissionDispatch.TryGetLegacyIndex("S026",out _))throw new InvalidOperationException("S026 must not alias a legacy playable index");return -1;}
}
}
