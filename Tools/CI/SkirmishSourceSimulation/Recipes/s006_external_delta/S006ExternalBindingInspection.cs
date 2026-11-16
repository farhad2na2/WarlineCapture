using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Disposable CLI worker. This constructs configuration values only; it never
// allocates a World or promotes the Planned S006 catalog entry.
internal static class S006ExternalBindingInspection
{
    private static JObject ReadJson(string path, string sha, long limit)
    {
        var bytes=AutonomousWorld.PinnedFile(path,sha,limit);
        return JObject.Parse(new UTF8Encoding(false,true).GetString(bytes),new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
    }
    private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=9) throw new ArgumentException("Pinned graph, matrix, binding, size, difficulty and output required");
            var graph=ReadJson(args[0],args[1],16*1024*1024);
            var graphPins=graph["source_pins"] as JObject ?? throw new InvalidDataException("Source graph pins required");
            foreach(var pin in graphPins.Properties()) AutonomousWorld.PinnedFile(pin.Name,(string)pin.Value,256L*1024*1024);
            if(!Enum.TryParse(args[6],out SkirmishSizeId size)||!Enum.TryParse(args[7],out SkirmishDifficultyId difficulty)||
                size==SkirmishSizeId.None||difficulty==SkirmishDifficultyId.None||!Enum.IsDefined(typeof(SkirmishSizeId),size)||!Enum.IsDefined(typeof(SkirmishDifficultyId),difficulty))
                throw new InvalidDataException("Exact source size and difficulty required");
            var materializer=new SourceConfigMaterializer(graph);
            var combined=materializer.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
            var established=materializer.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset");
            var catalog=materializer.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");
            if(combined.CatalogId!="S005"||established.CatalogId!="S002"||combined.MapLayout==null||combined.CampaignMap==null||established.StartPackageConfig==null)
                throw new InvalidDataException("Explicit source candidate profiles required");
            UnityEngine.Resources.BindSourceResources(new Dictionary<string,object> { ["SkirmishExpansionCatalog"]=catalog });
            var layout=Activator.CreateInstance<SkirmishMapLayoutConfig>();
            foreach(var field in typeof(SkirmishMapLayoutConfig).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly)) field.SetValue(layout,field.GetValue(combined.MapLayout));
            var format=typeof(SkirmishMapLayoutConfig).GetField("bindingValueFormat",BindingFlags.Instance|BindingFlags.NonPublic);
            if(format==null||format.FieldType!=typeof(SkirmishBindingValueFormat)) throw new InvalidDataException("Exact candidate layout format field required");
            format.SetValue(layout,SkirmishBindingValueFormat.CanonicalBinaryV2);
            var definition=Activator.CreateInstance<SkirmishScenarioDefinitionConfig>();
            definition.Configure("S006",combined.ObjectiveConfig,combined.ArmyProfileConfig,established.StartPackageConfig,
                combined.EconomyConfig,combined.UpgradeCatalog,combined.IntelConfig,combined.RoleCatalog,layout,featureIds:combined.RequiredFeatureIds);
            definition.BindCampaignMap(combined.CampaignMap);
            var matrixBytes=AutonomousWorld.PinnedFile(args[2],args[3],4*1024*1024);
            if(!SkirmishSetupMatrixTable.TryLoadFromText(new UTF8Encoding(false,true).GetString(matrixBytes),out var matrix,out string matrixError))
                throw new InvalidDataException("Original setup matrix rejected: "+matrixError);
            var manifest=new SkirmishContentManifest { RequiredFeatureIds=definition.RequiredFeatureIds };
            bool compiled=SkirmishSetupCompiler.TryCompile(definition,difficulty,size,104735,manifest,matrix,out var setup,out var reasons);
            if(!compiled||setup==null) throw new InvalidDataException("Original setup compiler rejected candidate: "+String.Join(" | ",reasons));
            var binding=SourceUnqualifiedLogicalBindingCandidate.Load(args[4],args[5],setup);
            definition.BindMissionLogicalBinding(binding);
            bool accepted=SkirmishMissionLogicalBindingCompiler.TryCompile(definition,setup,out var result,out string error);
            definition.BindMissionLogicalBinding(null);
            foreach(var pin in graphPins.Properties()) AutonomousWorld.PinnedFile(pin.Name,(string)pin.Value,256L*1024*1024);
            AutonomousWorld.PinnedFile(args[0],args[1],16*1024*1024);
            AutonomousWorld.PinnedFile(args[2],args[3],4*1024*1024);
            AutonomousWorld.PinnedFile(args[4],args[5],2*1024*1024);
            var document=new JObject { ["kind"]="S006UnqualifiedExternalBindingInspection",["status"]=accepted?"OriginalBindingCompilerAcceptedCandidate":"OriginalBindingCompilerRejectedCandidate",
                ["catalog_id"]=setup.CatalogId,["definition_id"]=setup.DefinitionId,["size"]=size.ToString(),["difficulty"]=difficulty.ToString(),["seed"]=setup.Seed,
                ["setup_hash"]=setup.SetupHash,["binding_compiler_accepted"]=accepted,["binding_compiler_error"]=error,["binding_fingerprint"]=result?.Fingerprint,
                ["campaign_source_identity"]=combined.CampaignMap.OperationMapId,["source_profile_scope"]="S005MapAndCombinedProfiles_S002Start_UnreviewedS006Candidate",
                ["world_allocated"]=false,["source_fit_verified"]=false,["visual_reviewed"]=false,["runtime_admission"]=false,["mission_ready"]=false };
            using(var stream=new FileStream(args[8],FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream))writer.Write(document.ToString(Formatting.Indented));
            Console.WriteLine("[S006ExternalBindingInspection] result="+document["status"]+" world=0 admission=0 readiness=0");return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
