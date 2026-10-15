#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    // QA adapter only. Legacy presets remain legacy; no runtime entities or content are authored here.
    public static class SkirmishLegacyProductionIdentity
    {
        public static bool IsProtected(string id)=>id=="S001"||id=="S025"||id=="S073";
        public static int Index(string id)=>id=="S001"?0:id=="S025"?1:id=="S073"?3:throw new InvalidDataException("Not a protected legacy player preset");
        public static string Resource(string id)=>id=="S001"?SkirmishPresetConfig.ResourceName:id=="S025"?SkirmishPresetConfig.SecondResourceName:id=="S073"?SkirmishPresetConfig.ThirdResourceName:throw new InvalidDataException("Unknown legacy binding");
        public static string AssetPath(string id)=>"Assets/Game/Resources/"+Resource(id)+".asset";
        static void Need(bool ok,string error){if(!ok)throw new InvalidDataException(error);}
        static void Exact(JObject row,string keys)=>Need(row!=null&&row.Properties().Select(p=>p.Name).OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(keys.Split(' ').OrderBy(v=>v,StringComparer.Ordinal)),"Legacy identity fields missing/unknown");
        static string Hash(JObject value)=>SkirmishProductionCanaryRequest.Hash(System.Text.Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(value)));
        public static JObject Configuration(QuickGameConfig value)=>JObject.Parse(JsonUtility.ToJson(value));
        public static SkirmishPresetConfig RequireBinding(string catalog)
        {
            var library=SkirmishBattleCatalogConfig.Load();
            Need(library!=null&&library.TryGet(catalog,out var row)&&row.IsPlayable&&row.PlayableScenarioIndex==Index(catalog)&&string.IsNullOrEmpty(row.DefinitionId)&&row.ContentVersion==0,"Actual protected catalog/preset binding differs; never manufacture an expanded definition");
            var preset=SkirmishPresetConfig.Load(Index(catalog));
            Need(preset!=null&&AssetDatabase.GetAssetPath(preset)==AssetPath(catalog),"Exact existing Resources preset missing: catalog="+catalog+" index="+Index(catalog)+" resource="+Resource(catalog)+" actualPath="+(preset==null?"<null>":AssetDatabase.GetAssetPath(preset)));
            return preset;
        }
        public static OperationMapDefinition ResolveSource(string catalog,out JObject resolution)
        {
            var preset=RequireBinding(catalog);
            if(preset.operationMap!=null)
            {
                resolution=new JObject{["mode"]="PresetOperationMap",["presetOperationMapPresent"]=true};
                return preset.operationMap;
            }
            // S001's unchanged asset intentionally omits operationMap. Read the SAME serialized
            // MatchSceneView catalog used by OperationMapLifecycle; never assign the preset.
            Need(catalog=="S001"&&!OperationMapDenseCityCandidateRuntimeOverride.CandidateBuildEnabled,"Only the actual protected S001 production compatibility fallback is admitted");
            const string scene="Assets/Game/Scenes/Match.unity",script="Assets/Game/Scripts/Composition/MatchSceneView.cs";
            string text=File.ReadAllText(scene),guid=AssetDatabase.AssetPathToGUID(script);
            var blocks=System.Text.RegularExpressions.Regex.Split(text,@"(?m)^--- !u!114 ");
            var matches=blocks.Where(b=>System.Text.RegularExpressions.Regex.IsMatch(b,@"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*"+guid+@"\s*,")).ToArray();
            Need(matches.Length==1,"Exact serialized normal MatchSceneView required for compatibility source identity");
            var refs=System.Text.RegularExpressions.Regex.Matches(matches[0],@"(?m)^  operationMapCatalog: \{fileID: 11400000, guid: ([a-f0-9]{32}), type: 2\}\s*$");
            Need(refs.Count==1,"Exact actual MatchSceneView operationMapCatalog reference required");
            string path=AssetDatabase.GUIDToAssetPath(refs[0].Groups[1].Value);
            var library=AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(path);
            OperationMapDefinition source=null;string sourceError=null;
            Need(library!=null&&library.TryValidate(out sourceError)&&library.TryResolve(SkirmishLaunchProjection.OperationMapId,out source)&&source!=null,"Normal compatibility catalog failed exact fallback operation-map resolution: catalog="+path+" operationMap="+SkirmishLaunchProjection.OperationMapId+" error="+sourceError);
            resolution=new JObject{["mode"]="CompatibilityCatalogFallback",["presetOperationMapPresent"]=false,["matchScenePath"]=scene,["matchSceneSha256"]=SkirmishProductionCanaryRequest.FileHash(scene),
                ["catalogPath"]=path,["catalogSha256"]=SkirmishProductionCanaryRequest.FileHash(path),["operationMapId"]=SkirmishLaunchProjection.OperationMapId,
                ["missionId"]=SkirmishLaunchProjection.MissionId,["scenarioId"]=SkirmishLaunchProjection.ScenarioId};
            return source;
        }
        static void VerifyOrdinarySourceSelection(EntityManager em,string catalog,OperationMapDefinition source,JObject resolution)
        {
            Need(MatchSceneView.OperationMapLaunchResolver.TryResolve(em.World,SkirmishLaunchProjection.MissionId,SkirmishLaunchProjection.ScenarioId,SkirmishLaunchProjection.OperationMapId,out var selected,out _,out _),"Actual ordinary legacy operation-map selection failed");
            if((string)resolution["mode"]=="CompatibilityCatalogFallback")
            {
                Need(selected.Definition==null&&selected.OperationMapId.ToString()==source.OperationMapId&&selected.MissionId.ToString()==SkirmishLaunchProjection.MissionId&&selected.ScenarioId.ToString()==SkirmishLaunchProjection.ScenarioId,"Actual legacy compatibility selection differs from native identity");
                foreach(var view in UnityEngine.Object.FindObjectsByType<MatchSceneView>(FindObjectsInactive.Exclude))
                    Need(view.OperationMapCatalog!=null&&view.OperationMapCatalog.TryResolve(source.OperationMapId,out var actual)&&actual==source,"Actual live MatchScene catalog differs from serialized normal source");
            }
            else Need(selected.Definition==source&&selected.OperationMapId.ToString()==source.OperationMapId,"Actual explicit preset map differs from normal launch source");
        }
        public static void ValidateJob(JObject job)
        {
            string catalog=(string)job["catalog_id"];Need(IsProtected(catalog),"Legacy schema only admits the three protected presets");
            Need(job["schema_version"]?.Type==JTokenType.Integer&&(int)job["schema_version"]==2&&
                job["legacy_preset_index"]?.Type==JTokenType.Integer&&(int)job["legacy_preset_index"]==Index(catalog)&&
                (string)job["legacy_preset_resource"]==Resource(catalog)&&
                !job.ContainsKey("definition_id")&&!job.ContainsKey("definition_version"),"Legacy request must carry actual preset identity and no expanded definition/version");
            RequireBinding(catalog);
            Need(SkirmishProductionCanaryRequest.FileHash(Path.GetFullPath(AssetPath(catalog)))==(string)job["legacy_preset_sha256"],"Native preset bytes differ from owned job");
            Need((string)job["size"]=="Standard"&&(string)job["difficulty"]=="Regular","Only requested baseline packet labels admitted; native legacy settings are recorded separately");
        }
        public static JObject CaptureConfiguration(string catalog,QuickGameConfig configuration,string actualLocale)
        {
            var preset=RequireBinding(catalog);Need(actualLocale=="en"||actualLocale=="fa-IR","Actual legacy locale unsupported");
            Need(configuration.ScenarioIndex==Index(catalog)&&configuration.MapSeed>0&&JToken.DeepEquals(Configuration(configuration),Configuration(configuration.NormalizeForBaseAssault())),"Actual legacy configuration differs from normal coded normalization");
            var source=ResolveSource(catalog,out var resolution);
            string mapPath=AssetDatabase.GetAssetPath(source);
            Need(!string.IsNullOrEmpty(mapPath)&&File.Exists(mapPath),"Actual legacy source asset missing");
            return new JObject{["schema_version"]=2,["identity_kind"]="SkirmishLegacyPresetNativeIdentity",["catalog_id"]=catalog,
                ["scenario_index"]=configuration.ScenarioIndex,["preset_resource"]=Resource(catalog),["preset_asset_path"]=AssetPath(catalog),
                ["preset_asset_sha256"]=SkirmishProductionCanaryRequest.FileHash(AssetPath(catalog)),["preset_dependency_identity"]=AssetDatabase.GetAssetDependencyHash(AssetPath(catalog)).ToString(),
                ["source_asset_path"]=mapPath,["source_asset_sha256"]=SkirmishProductionCanaryRequest.FileHash(mapPath),["source_content_identity"]=source.ContentHash,["source_identity"]=source.SourceIdentityHash,["source_resolution"]=resolution,
                ["seed"]=configuration.MapSeed,["locale"]=actualLocale=="en"?"EN":"FA",["native_configuration"]=Configuration(configuration)};
        }
        public static JObject CaptureLive(EntityManager em,Entity session,in SkirmishMatchState match,string catalog)
        {
            Need(em.Exists(session)&&em.HasComponent<SkirmishMatchState>(session)&&!match.SessionId.IsEmpty&&match.ScenarioIndex==Index(catalog)&&
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)&&!em.HasComponent<SkirmishResolvedSetupRecord>(session)&&em.HasComponent<SkirmishLaunchSnapshot>(session),"Actual legacy-only session/snapshot required");
            Need(SkirmishTerminalPresentationObservation.NativeSessionCount(em)==1,"Exact single native legacy attempt required");
            var snapshot=em.GetComponentObject<SkirmishLaunchSnapshot>(session).Configuration;
            Need(snapshot.ScenarioIndex==match.ScenarioIndex&&snapshot.MapSeed==match.Seed,"Actual MatchState and queued snapshot differ");
            var source=ResolveSource(catalog,out var resolution);VerifyOrdinarySourceSelection(em,catalog,source,resolution);
            return CaptureConfiguration(catalog,snapshot,GameLocalization.CurrentLocaleCode);
        }
        public static JObject ContentIdentity(JObject value)
        {
            ValidateShape(value);var copy=(JObject)value.DeepClone();copy.Remove("seed");copy.Remove("locale");copy.Remove("native_configuration");copy["identity_kind"]="SkirmishLegacyPresetNativeContentIdentity";return copy;
        }
        public static void ValidateShape(JObject value)
        {
            Exact(value,"schema_version identity_kind catalog_id scenario_index preset_resource preset_asset_path preset_asset_sha256 preset_dependency_identity source_asset_path source_asset_sha256 source_content_identity source_identity source_resolution seed locale native_configuration");
            string id=(string)value["catalog_id"];Need(IsProtected(id)&&value["schema_version"]?.Type==JTokenType.Integer&&(int)value["schema_version"]==2&&(string)value["identity_kind"]=="SkirmishLegacyPresetNativeIdentity","Unknown legacy native identity");
            Need(value["scenario_index"]?.Type==JTokenType.Integer&&(int)value["scenario_index"]==Index(id)&&(string)value["preset_resource"]==Resource(id)&&(string)value["preset_asset_path"]==AssetPath(id)&&value["seed"]?.Type==JTokenType.Integer&&(int)value["seed"]>0&&new[]{"EN","FA"}.Contains((string)value["locale"]),"Malformed native preset binding/seed/locale");
            foreach(string key in new[]{"preset_asset_sha256","source_asset_sha256"})Need(System.Text.RegularExpressions.Regex.IsMatch((string)value[key]??"","^[a-f0-9]{64}$"),"Actual legacy raw asset SHA absent");
            Need(System.Text.RegularExpressions.Regex.IsMatch((string)value["preset_dependency_identity"]??"","^[a-f0-9]{32}$")&&!string.IsNullOrEmpty((string)value["source_asset_path"])&&!string.IsNullOrEmpty((string)value["source_content_identity"])&&!string.IsNullOrEmpty((string)value["source_identity"]),"Actual native preset/source content identity absent");
            Need(value["source_resolution"] is JObject sourceResolution&&new[]{"PresetOperationMap","CompatibilityCatalogFallback"}.Contains((string)sourceResolution["mode"]),"Actual source resolution provenance required");
            Need(value["native_configuration"] is JObject,"Actual native configuration object required");
            var config=JsonUtility.FromJson<QuickGameConfig>(value["native_configuration"].ToString(Newtonsoft.Json.Formatting.None));
            Need(config.ScenarioIndex==Index(id)&&config.MapSeed==(int)value["seed"]&&JToken.DeepEquals(Configuration(config),value["native_configuration"])&&JToken.DeepEquals(Configuration(config),Configuration(config.NormalizeForBaseAssault())),"Native legacy settings are not actual Normal/default/full-vision coded settings");
        }
        public static void VerifyJobIdentity(JObject job,JObject native)
        {
            ValidateJob(job);ValidateShape(native);
            Need((string)native["catalog_id"]==(string)job["catalog_id"]&&(int)native["seed"]==(int)job["seed"]&&(string)native["locale"]==(string)job["locale"]&&
                (int)native["scenario_index"]==(int)job["legacy_preset_index"]&&(string)native["preset_asset_sha256"]==(string)job["legacy_preset_sha256"]&&
                Hash(native)==(string)job["setup_sha256"]&&Hash(ContentIdentity(native))==(string)job["content_sha256"],"Actual legacy setup/content identity differs from canonical claim");
        }
        public static bool SavedResultMatches(in SkirmishMatchState match,SkirmishResultSaveData saved)=>
            match.Phase==SkirmishPhase.Finished&&match.ResultSaved!=0&&saved!=null&&saved.sessionId==match.SessionId.ToString()&&saved.seed==match.Seed&&saved.scenarioIndex==match.ScenarioIndex&&
            saved.outcome==match.Outcome.ToString()&&saved.reason==match.Reason.ToString()&&Mathf.Abs(saved.elapsedSeconds-match.ElapsedSeconds)<.001f&&
            saved.playerUnitsLost==match.PlayerUnitsLost&&saved.enemyUnitsLost==match.EnemyUnitsLost&&saved.playerBuildingsLost==match.PlayerBuildingsLost&&saved.enemyBuildingsLost==match.EnemyBuildingsLost;
        public static bool ReadSavedResultMatches(in SkirmishMatchState match,out JObject facts)
        {
            string root=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");facts=new JObject{["saveRoot"]=root,["nativeResultSaved"]=match.ResultSaved,["storageKind"]="QuickGameData.lastResult"};
            if(string.IsNullOrEmpty(root)||!Path.IsPathRooted(root)||!File.Exists(Path.Combine(root,SaveService.QuickGameFileName))){facts["unavailable"]="Actual isolated legacy result file absent";return false;}
            var saved=SaveService.CreateDefault().LoadQuickGame().lastResult;
            facts["savedResult"]=saved==null?JValue.CreateNull():JObject.FromObject(saved);bool matches=SavedResultMatches(in match,saved);facts["matches"]=matches;return matches;
        }
        public static int BaseHp(EntityManager em,in SkirmishMatchState match,byte faction)
        {
            var entity=faction==1?match.PlayerMainBase:match.EnemyMainBase;
            if(entity==Entity.Null||!em.Exists(entity))return 0;
            Need(em.HasComponent<SkirmishMainBase>(entity)&&em.GetComponentData<SkirmishMainBase>(entity).FactionId==faction&&em.HasComponent<Faction>(entity)&&em.GetComponentData<Faction>(entity).Id==faction,"Actual legacy designated base/faction identity differs");
            return em.HasComponent<UnitHealth>(entity)?em.GetComponentData<UnitHealth>(entity).Current:0;
        }
        public static bool IsOperationallyComplete(in SkirmishS002AriaTerminalFacts facts,bool appended,bool terminal,bool verified)=>
            verified&&IsProtected(facts.CatalogId)&&facts.DefinitionVersion==0&&appended&&terminal&&facts.MatchFinished&&!facts.Aborted&&!facts.ForcedVictory&&
            new[]{"Victory","Defeat","Draw"}.Contains(facts.MatchOutcome)&&facts.NormalSpeed&&facts.InputViolations==0&&facts.HumanInterventions==0&&
            System.Text.RegularExpressions.Regex.IsMatch(facts.CodeHash??"","^[a-f0-9]{64}$")&&System.Text.RegularExpressions.Regex.IsMatch(facts.ConfigHash??"","^[a-f0-9]{64}$");
    }
}
#endif
