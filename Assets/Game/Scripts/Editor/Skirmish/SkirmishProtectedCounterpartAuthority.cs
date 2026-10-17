#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Admission provenance only. A queued EditMode fixture is never a played
    // mission, terminal result, successful touch journey or qualification credit.
    public static class SkirmishProtectedCounterpartAuthority
    {
        static void Need(bool ok,string reason){if(!ok)throw new InvalidDataException(reason);}
        static string Text(JObject row,string key)
        {Need(row?[key]?.Type==JTokenType.String&&!string.IsNullOrEmpty((string)row[key]),"Counterpart text missing: "+key);return (string)row[key];}
        static void Exact(JObject row,string keys)
        {Need(row!=null&&row.Properties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(keys.Split(' ').OrderBy(x=>x,StringComparer.Ordinal)),"Counterpart authority fields missing/unknown");}
        static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        static bool IsHash(string value)=>System.Text.RegularExpressions.Regex.IsMatch(value,"^[a-f0-9]{64}$");
        static string Relative(string root,string path)
        {
            Need(!string.IsNullOrEmpty(path)&&!Path.IsPathRooted(path)&&!path.Contains('\\')&&!path.Contains(':')&&
                !path.Split('/').Contains(".."),"Counterpart pin must be project-relative");
            string full=Path.GetFullPath(Path.Combine(root,path));
            Need(full.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Counterpart pin escapes project");
            return full;
        }
        static string ProjectRelative(string path)
        {
            string full=Path.GetFullPath(path),prefix=Root+Path.DirectorySeparatorChar;
            Need(full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase),"Counterpart evidence outside project");
            return full.Substring(prefix.Length).Replace('\\','/');
        }
        static JObject Pin(string path)=>new JObject{["path"]=ProjectRelative(path),["sha256"]=SkirmishProductionCanaryRequest.FileHash(path)};
        static string CheckPin(JToken token,string root)
        {
            var pin=token as JObject;Exact(pin,"path sha256");string path=Relative(root,Text(pin,"path"));
            Need(IsHash(Text(pin,"sha256"))&&File.Exists(path)&&
                SkirmishProductionCanaryRequest.FileHash(path)==(string)pin["sha256"],"Counterpart pinned file missing/changed: "+path);return path;
        }
        static JObject ReadPin(JToken token,string root)=>SkirmishProductionCanaryRequest.ReadObject(CheckPin(token,root));
        static JObject Asset(UnityEngine.Object value)
        {Need(value!=null&&AssetDatabase.Contains(value)&&!EditorUtility.IsDirty(value),"Exact clean persisted counterpart asset required");return Pin(AssetDatabase.GetAssetPath(value));}
        static int Index(string id)=>id=="S001"?0:id=="S025"?1:id=="S073"?3:throw new InvalidDataException("Only three exact counterpart IDs allowed");
        static int[] Seeds(string id)=>id=="S001"?new[]{104730,130364,155922}:id=="S025"?new[]{104754,130388,155946}:new[]{104802,130436,155994};
        static string Preset(string id)=>id=="S001"?"SkirmishBaseAssault":id=="S025"?"SkirmishCityCrossroads":"SkirmishIndustrialBasin";
        static JArray Views(SkirmishScenarioDefinitionConfig definition)=>new JArray(definition.MissionLogicalBinding.Variants
            .OrderBy(v=>v.Size.ToString(),StringComparer.Ordinal).Select(v=>Asset(v.View)));
        static JObject Assets(SkirmishScenarioDefinitionConfig definition,SkirmishExpansionCatalogConfig development)=>new JObject
        {
            ["development_catalog"]=Asset(development),["definition"]=Asset(definition),["layout"]=Asset(definition.MapLayout),
            ["binding"]=Asset(definition.MissionLogicalBinding),["views"]=Views(definition),
            ["practice_preset"]=Pin("Assets/Game/Resources/"+Preset(definition.CatalogId)+".asset"),
            ["main_catalog"]=Asset(Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName)),
            ["main_expansion_catalog"]=Asset(Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName))
        };
        static string BuildHash(string root)
        {
            string folder=Path.Combine(root,"Library","ScriptAssemblies");
            var rows=new JArray(Directory.GetFiles(folder,"*.dll").OrderBy(p=>p,StringComparer.Ordinal).Select(p=>new JObject
                {["path"]=p.Substring(root.Length+1).Replace('\\','/'),["sha256"]=SkirmishProductionCanaryRequest.FileHash(p)}));
            Need(rows.Count>0,"Actual compiled DLL set required");return Digest(rows);
        }
        static string Digest(JToken value)=>SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(value)));

        // Explicit fresh-Editor persisted readback emits this only after actual
        // normal setup public methods have queued three distinct native sessions.
        public static void Write(SkirmishScenarioDefinitionConfig definition,SkirmishExpansionCatalogConfig development,JArray readbacks)
        {
            Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle persisted counterpart readback required");
            string output=Environment.GetEnvironmentVariable("WARLINE_PROTECTED_COUNTERPART_AUTHORITY_OUTPUT");
            string report=Environment.GetEnvironmentVariable("WARLINE_ALL120_MATERIALIZATION_REPORT");
            string reviewed=Environment.GetEnvironmentVariable("WARLINE_ALL120_MATERIALIZATION_REPORT_SHA256");
            string prefix=Path.Combine(Root,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar;
            Need(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&Path.GetFullPath(output).StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&
                !File.Exists(output)&&!string.IsNullOrEmpty(report)&&SkirmishProductionCanaryRequest.FileHash(report)==reviewed,
                "Fresh authority output and exact reviewed materialization report required");
            var value=new JObject{["schema_version"]=1,["kind"]="MeasuredExpandedCounterpartDevelopmentAuthority",
                ["catalog_id"]=definition.CatalogId,["definition_id"]=definition.DefinitionId,["definition_version"]=definition.ContentVersion,
                ["runtime_index"]=Index(definition.CatalogId),["source_pins"]=SkirmishGroundSourceFitSurvey.ProtectedCounterpartSourcePins(definition),
                ["layout_semantic_sha256"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout),["candidate_sha256"]=SkirmishCandidateSourceHash.Compute(Root),["build_sha256"]=BuildHash(Root),
                ["assets"]=Assets(definition,development),["measurement_report"]=Pin(report),
                ["materialization_receipt"]=Pin(report+"."+definition.CatalogId+".materialization.json"),
                ["normal_selection"]=new JObject{["kind"]="ExpandedDefinition",["driver"]="PublicSetupMethods_EditModeFixture",
                    ["actual_normal_input_play"]=false,["baseline_credit"]=0,["readbacks"]=readbacks.DeepClone()}};
            ValidateFacts(value,Root);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(file,new UTF8Encoding(false)))
            {writer.Write(value.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            Debug.Log("[ProtectedCounterpartAdmissionAuthority] result=Recorded catalog="+definition.CatalogId+
                " publicSetupFixtureStarts=3 actualNormalInputPlay=0 baselineCredit=0 sha256="+SkirmishProductionCanaryRequest.FileHash(output)+" output="+output);
        }

        static SkirmishScenarioDefinitionConfig ValidateFacts(JObject authority,string root)
        {
            Exact(authority,"schema_version kind catalog_id definition_id definition_version runtime_index source_pins layout_semantic_sha256 candidate_sha256 build_sha256 assets measurement_report materialization_receipt normal_selection");
            string id=Text(authority,"catalog_id");int index=Index(id);
            Need(authority["schema_version"]?.Type==JTokenType.Integer&&(int)authority["schema_version"]==1&&
                Text(authority,"kind")=="MeasuredExpandedCounterpartDevelopmentAuthority"&&Text(authority,"definition_id")=="skirmish."+id.ToLowerInvariant()&&
                authority["definition_version"]?.Type==JTokenType.Integer&&(int)authority["definition_version"]>0&&
                authority["runtime_index"]?.Type==JTokenType.Integer&&(int)authority["runtime_index"]==index&&
                IsHash(Text(authority,"candidate_sha256"))&&IsHash(Text(authority,"build_sha256")),"Counterpart identity/schema differs");
            var assets=authority["assets"] as JObject;Exact(assets,"development_catalog definition layout binding views practice_preset main_catalog main_expansion_catalog");
            string path="Assets/Game/Configs/SkirmishExpansion/Scenarios/"+id+"/";
            var expected=new[]{("development_catalog",path+"Resources/SkirmishProtectedCounterpart_"+id+".asset"),
                ("definition",path+"SkirmishScenario_"+id+".asset"),("layout",path+"Logical/Layout_"+id+".asset"),
                ("binding",path+"Logical/Binding_"+id+".asset"),("practice_preset","Assets/Game/Resources/"+Preset(id)+".asset"),
                ("main_catalog","Assets/Game/Resources/SkirmishBattleCatalog.asset"),("main_expansion_catalog","Assets/Game/Resources/SkirmishExpansionCatalog.asset")};
            foreach(var item in expected){CheckPin(assets[item.Item1],root);Need((string)assets[item.Item1]["path"]==item.Item2,"Counterpart asset role/path differs");}
            var views=assets["views"] as JArray;Need(views!=null&&views.Count==3,"Three counterpart views required");
            foreach(var view in views)CheckPin(view,root);
            Need(views.Select(v=>(string)v["path"]).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(new[]{"Standard","War","LargeWar"}
                .Select(s=>path+"Logical/View_"+id+"_"+s+"_Regular.asset").OrderBy(x=>x,StringComparer.Ordinal)),"Counterpart view sizes/roles differ");
            var development=SkirmishProtectedExpandedCounterpartScope.LoadDevelopmentCatalog(id);
            Need(development.TryGetDefinition(id,out var definition)&&definition.DefinitionId==(string)authority["definition_id"]&&
                definition.ContentVersion==(int)authority["definition_version"]&&JToken.DeepEquals(assets,Assets(definition,development)),
                "Actual clean persisted counterpart resource graph differs");
            ValidateMeasurement(authority,root,id,index,definition);
            var selection=authority["normal_selection"] as JObject;Exact(selection,"kind driver actual_normal_input_play baseline_credit readbacks");
            Need(Text(selection,"kind")=="ExpandedDefinition"&&Text(selection,"driver")=="PublicSetupMethods_EditModeFixture"&&
                selection["actual_normal_input_play"]?.Type==JTokenType.Boolean&&!(bool)selection["actual_normal_input_play"]&&
                selection["baseline_credit"]?.Type==JTokenType.Integer&&(int)selection["baseline_credit"]==0,"Fixture is not gameplay/touch/coverage evidence");
            var readbacks=selection["readbacks"] as JArray;Need(readbacks!=null&&readbacks.Count==3,"Three independent public setup readbacks required");
            foreach(var token in readbacks)
            {
                var row=token as JObject;Exact(row,"seed scenario_index catalog_id definition_id definition_version size difficulty native_session_id native_world_sequence native_setup_hash_uint32 native_content_identity");
                Need(row["seed"]?.Type==JTokenType.Integer&&row["scenario_index"]?.Type==JTokenType.Integer&&(int)row["scenario_index"]==index&&
                    Text(row,"catalog_id")==id&&Text(row,"definition_id")==definition.DefinitionId&&row["definition_version"]?.Type==JTokenType.Integer&&
                    (int)row["definition_version"]==definition.ContentVersion&&Text(row,"size")=="Standard"&&Text(row,"difficulty")=="Regular"&&
                    ulong.TryParse(Text(row,"native_world_sequence"),out var world)&&world>0&&row["native_setup_hash_uint32"]?.Type==JTokenType.Integer&&
                    (long)row["native_setup_hash_uint32"]>=0&&(long)row["native_setup_hash_uint32"]<=uint.MaxValue,"Actual typed setup readback malformed");
                Text(row,"native_session_id");Text(row,"native_content_identity");
                var setup=Compile(definition,(int)row["seed"]);
                Need(setup.SetupHash==(uint)row["native_setup_hash_uint32"]&&setup.ContentHash==(string)row["native_content_identity"],"Persisted compiler no longer matches readback");
            }
            Need(readbacks.Select(v=>(int)v["seed"]).OrderBy(v=>v).SequenceEqual(Seeds(id).OrderBy(v=>v))&&
                readbacks.Select(v=>(string)v["native_session_id"]).Distinct().Count()==3&&readbacks.Select(v=>(string)v["native_world_sequence"]).Distinct().Count()==3,
                "Exact packet seeds/fresh native session and World readbacks required");return definition;
        }
        static SkirmishResolvedSetup Compile(SkirmishScenarioDefinitionConfig definition,int seed)
        {
            Need(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out var error),error);
            Need(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,seed,
                new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out var reasons),
                "Counterpart normal compiler refused: "+string.Join(";",reasons));return setup;
        }
        static void ValidateMeasurement(JObject authority,string root,string id,int index,SkirmishScenarioDefinitionConfig definition)
        {
            var report=ReadPin(authority["measurement_report"],root);var receipt=ReadPin(authority["materialization_receipt"],root);
            Need((bool?)report["cleanupVerified"]==true&&(string)report["result"]=="CollectedUnqualified","Source ownership cleanup missing");
            var rows=(report["missions"] as JArray)?.OfType<JObject>().Where(v=>(string)v["catalogId"]==id).ToArray();
            var artifacts=(report["missionArtifacts"] as JArray)?.OfType<JObject>().Where(v=>(string)v["catalogId"]==id).ToArray();
            Need(rows?.Length==1&&artifacts?.Length==1&&(string)rows[0]["result"]=="CollectedUnqualified"&&rows[0]["error"]==null,"Exact successful durable counterpart row required");
            string artifact=Text(artifacts[0],"path");if(Path.IsPathRooted(artifact))artifact=ProjectRelative(artifact);
            Need(JToken.DeepEquals(rows[0],ReadPin(new JObject{["path"]=artifact,["sha256"]=artifacts[0]["sha256"]},root)),"Durable counterpart mission row differs");
            var measurement=rows[0]["measurement"] as JObject;
            SkirmishGroundSourceFitSurvey.ValidateProtectedCounterpartSourceLayoutReport(definition,report["sourcePins"],measurement);
            Need(JToken.DeepEquals(authority["source_pins"],report["sourcePins"])&&Text(authority,"layout_semantic_sha256")==
                SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout),"Authority actual source/layout semantic pins differ");
            Need(measurement?["strictThreeSizePlacementPass"]?.Type==JTokenType.Integer&&
                (bool?)measurement["normalThreeSizeCompilerPassed"]==true,"Strict measured three-size/compiler pass missing");
            var passes=(measurement["passes"] as JArray)?.OfType<JObject>().Where(v=>(int?)v["pass"]==(int)measurement["strictThreeSizePlacementPass"]).ToArray();
            Need(passes?.Length==1,"Strict pass missing/ambiguous");var sizes=passes[0]["sizes"] as JArray;
            Need(sizes?.Count==3&&sizes.OfType<JObject>().Select(v=>(string)v["size"]).OrderBy(v=>v,StringComparer.Ordinal)
                .SequenceEqual(new[]{"LargeWar","Standard","War"}),"Exact three measured configurations required");
            foreach(var size in sizes.OfType<JObject>())
            {
                var queries=size["routeQueriesWithSameFacilityReservations"]?["queries"] as JArray;
                Need((bool?)size["completeFacilities"]==true&&(bool?)size["allActorsInsidePads"]==true&&queries?.Count>0&&queries.All(v=>
                    (string)v["status"]=="NativeQueryFoundExactGoal"&&v["failureCode"]?.Type==JTokenType.Integer&&(int)v["failureCode"]==0),
                    "Actual complete facility/army/same-reservation paths required");
            }
            var assets=authority["assets"];
            Need((string)receipt["catalogId"]==id&&(string)receipt["result"]=="MaterializedDevelopmentOnly"&&receipt["error"]==null&&
                (string)receipt["reviewedReportSha256"]==(string)authority["measurement_report"]["sha256"]&&
                (bool?)receipt["publicationManifestChanged"]==false&&(bool?)receipt["developmentOnly"]==true&&(bool?)receipt["normalMissionReady"]==false,
                "Measured counterpart materialization receipt differs");
            var resource=receipt["counterpartDevelopmentResource"];var materialized=receipt["developmentCatalogMaterialization"];
            Need((string)resource?["path"]==(string)assets["development_catalog"]["path"]&&(string)resource?["sha256"]==(string)assets["development_catalog"]["sha256"]&&
                (bool?)resource?["mainCatalogChanged"]==false&&(bool?)resource?["practiceBindingChanged"]==false&&
                (string)materialized?["definitionPath"]==(string)assets["definition"]["path"]&&(string)materialized?["definitionSha256"]==(string)assets["definition"]["sha256"]&&
                (string)materialized?["layoutPath"]==(string)assets["layout"]["path"]&&(string)materialized?["bindingPath"]==(string)assets["binding"]["path"]&&
                (int?)materialized?["runtimeIndex"]==index&&(bool?)materialized?["catalogPlayableChanged"]==false,"Persisted graph/practice preservation receipt differs");
        }
        public static void Validate(JObject job,string root)
        {
            Need(job["schema_version"]?.Type==JTokenType.Integer&&(int)job["schema_version"]==3,"Explicit schema3 required");
            var authority=ReadPin(job["counterpart_authority"],root);var definition=ValidateFacts(authority,root);
            foreach(string key in new[]{"catalog_id","definition_id","definition_version","candidate_sha256","build_sha256"})
                Need(JToken.DeepEquals(job[key],authority[key]),"Counterpart authority/job identity differs: "+key);
            var manifest=job["source_manifest"] as JArray;Need(manifest!=null,"Counterpart source manifest missing");
            var assets=(JObject)authority["assets"];
            foreach(var pin in assets.Properties().Where(p=>p.Name!="views").Select(p=>p.Value).Concat((JArray)assets["views"]))
                Need(manifest.Count(v=>JToken.DeepEquals(v,pin))==1,"Counterpart actual asset graph absent from source manifest");
            // Actual current source/DLL endpoints are also checked by the
            // existing request/batch VerifyNativePins, not inferred from receipt.
            var setup=Compile(definition,(int)job["seed"]);
            var native=SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,setup.DefinitionId,setup.ContentVersion,
                setup.DifficultyId.ToString(),setup.SizeId.ToString(),setup.Seed,(string)job["locale"]=="EN"?"en":"fa-IR",setup.SetupHash,setup.ContentHash);
            Need(Digest(native)==(string)job["setup_sha256"]&&Digest(SkirmishProductionCanaryRequest.NativeContentIdentity(native))==(string)job["content_sha256"],
                "Actual persisted compiler identity differs from claimed counterpart job");
        }
    }
}
#endif
