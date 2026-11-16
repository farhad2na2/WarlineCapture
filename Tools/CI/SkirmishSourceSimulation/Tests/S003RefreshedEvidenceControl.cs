using System;
using System.IO;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;

internal static class S003RefreshedEvidenceControl
{
    static int Main(string[] args)
    {
        try
        {
            string path = Path.GetFullPath(args[0]);
            string hash = AutonomousWorld.Hash(File.ReadAllBytes(path));
            var descriptor = new JObject { ["path"] = path, ["sha256"] = hash };
            var result = SourceConfigurationQualification.ValidateRefreshedS003Evidence(descriptor);
            if ((string)result["target"]?["catalog_id"] != "S003" || (bool?)result["mission_ready"] != false)
                throw new Exception("Actual source-only evidence mismatch");
            Reject(new JObject { ["path"] = path, ["sha256"] = new string('0', 64) });
            Reject(new JObject { ["path"] = path + ".alias", ["sha256"] = hash });
            Reject(new JObject { ["path"] = path, ["sha256"] = hash, ["extra"] = true });
            var initial=JObject.Parse(File.ReadAllText("T Logs/SkirmishProduction/s003-current-data-bundle-rebind-20261103-T24/initialization.json"));
            initial["metadata_compiler_context"]=descriptor.DeepClone();initial["configuration_qualification"]=descriptor.DeepClone();initial["source_config_graph"]=result["runtime_graph"].DeepClone();
            SourceConfigurationQualification.RequireInitialization(initial,104732,2,1);
            var refreshedContext=SourceConfigurationQualification.Context(initial);
            if(!JToken.DeepEquals(FreshSourceStorageLifetimeAuthority.ResolveS003StorageGraphDescriptor(initial,refreshedContext),result["runtime_graph"]))throw new Exception("Wrong storage graph");
            var wrong=(JObject)initial.DeepClone();wrong["source_config_graph"]["sha256"]=new string('0',64);
            try { SourceConfigurationQualification.RequireInitialization(wrong,104732,2,1); throw new Exception("Wrong runtime graph accepted"); }
            catch (InvalidDataException) { }
            var artifacts=(JObject)result["factory_artifacts"];initial["building_definitions"]=artifacts["building-definitions.json"].DeepClone();initial["unit_production_metadata"]=artifacts["unit-production-metadata.json"].DeepClone();initial["building_request_metadata"]=artifacts["configured-metadata.json"].DeepClone();
            var compiled=AutonomousWorld.ReadCompiledInitialization(initial);var metadata=SourceMetadataCompatibility.Load(initial,compiled.setup,Directory.GetCurrentDirectory(),Directory.GetCurrentDirectory());
            if(!metadata.S003SourceOnly||!metadata.FactoryDomainQualified||metadata.NativeQualified||metadata.BuildingCount!=10||metadata.UnitCount!=19)throw new Exception("Refreshed actual source metadata mismatch");
            var wrongMetadata=(JObject)initial.DeepClone();wrongMetadata["unit_production_metadata"]["sha256"]=new string('0',64);
            try { SourceMetadataCompatibility.Load(wrongMetadata,compiled.setup,Directory.GetCurrentDirectory(),Directory.GetCurrentDirectory());throw new Exception("Wrong metadata accepted"); }
            catch (InvalidDataException) { }
            var prefabDescriptor=JObject.Parse(File.ReadAllText("T Logs/SkirmishProduction/s003-refreshed-prefab-projection-20261104-T1/reference-template-descriptor.json"));
            var prefab=GeneratedReferenceTemplateView.Read(prefabDescriptor,out var binaryDirectory,(JObject)refreshedContext["source_graph"]);if(((JArray)prefab["entities"]).Count!=647||string.IsNullOrEmpty(binaryDirectory))throw new Exception("Refreshed template view mismatch");
            var badPrefab=(JObject)prefabDescriptor.DeepClone();badPrefab["refreshed_capture_proof"]["importer_control_receipt"]["sha256"]=new string('0',64);
            try{GeneratedReferenceTemplateView.Read(badPrefab,out _,(JObject)refreshedContext["source_graph"]);throw new Exception("Wrong importer proof accepted");}catch(InvalidDataException){}
            var guard=new JObject{["can_request"]=true};var sites=new JArray();
            for(int i=0;i<6;i++)sites.Add(new JObject{["site_index"]=i,["screen_projection_known"]=true,["diagnostic_screen"]=new JArray(100+i,200,1),["diagnostic_pointer_values"]=new JObject{["ray_hits_grid"]=i==0}});
            var view=new AriaS002Observation{Scope=AriaS002Scope.LogicalPublicData};
            SourceAriaS002Controller.BindS003LogicalDefense(ref view,guard,sites,false,false);
            if(!view.DefenseControlsKnown||!view.DefenseBuild.Admitted||!view.Site0.Admitted||view.Site1.Admitted||view.PlacementOpen)throw new Exception("Logical source defense projection differs");
            SourceAriaS002Controller.BindS003LogicalDefense(ref view,guard,sites,true,true);
            if(view.DefenseBuild.Admitted||!view.PlacementConfirm.Admitted||!view.PlacementCancel.Admitted)throw new Exception("Actual source preview state differs");
            var invalid=(JArray)sites.DeepClone();invalid[0]["diagnostic_pointer_values"]=null;
            try{SourceAriaS002Controller.BindS003LogicalDefense(ref view,guard,invalid,false,false);throw new Exception("Unknown source ray accepted");}catch(NotSupportedException){}
            var native=new AriaS002Observation{Scope=AriaS002Scope.NativeRendered};
            try{SourceAriaS002Controller.BindS003LogicalDefense(ref native,guard,sites,false,false);throw new Exception("Native UI fabricated");}catch(NotSupportedException){}
            Console.WriteLine("[S003RefreshedEvidenceControl] result=Passed positive=7 negative=8 world=0 readiness=0");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    static void Reject(JObject descriptor)
    {
        try { SourceConfigurationQualification.ValidateRefreshedS003Evidence(descriptor); }
        catch (Exception) { return; }
        throw new Exception("Invalid evidence descriptor accepted");
    }
}
