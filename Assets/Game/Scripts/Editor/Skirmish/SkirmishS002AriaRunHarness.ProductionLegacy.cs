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
using UnityEngine;
namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static Unity.Collections.FixedString64Bytes ReadProductionTerminalAttempt(EntityManager em,Entity session,in SkirmishMatchState match)
        {
            if(match.SessionId.IsEmpty||!em.Exists(session)||!em.HasComponent<SkirmishMatchState>(session)||em.GetComponentData<SkirmishMatchState>(session).SessionId!=match.SessionId)throw new InvalidDataException("Actual native terminal attempt missing");
            if(em.HasComponent<SkirmishExpandedSessionComponent>(session))
            {if(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId!=match.SessionId)throw new InvalidDataException("Expanded/MatchState native attempts differ");}
            else if(productionCanary?.Request.IsLegacyPreset!=true||match.ScenarioIndex!=SkirmishLegacyProductionIdentity.Index(productionCanary.Request.CatalogId))throw new InvalidDataException("Legacy terminal requires exact protected canonical production claim");
            return match.SessionId;
        }
        private static string LegacyCsv(string value)=>"\""+(value??"").Replace("\"","\"\"")+"\"";
        private static bool TryAppendLegacyProductionTerminal(string usualPath,in SkirmishS002AriaTerminalFacts facts,out string row,out string error)
        {
            row=null;error=null;
            if(productionCanary?.Request.IsLegacyPreset!=true||!SkirmishS002AriaRunLog.TryAcceptTerminal(in facts,out string outcome,out error))return false;
            string path=Path.Combine(Path.GetDirectoryName(usualPath),"legacy-native-runs.csv");
            const string header="run_id,catalog_id,native_identity_kind,preset_resource,scenario_index,code_hash,config_hash,seed,locale,native_difficulty,native_size_contract,requested_sampling_difficulty,requested_sampling_size,normal_speed,outcome,end_reason,duration_seconds,input_violations,human_interventions,actual_native_win,sampling_coverage_credit,trace_path,log_path";
            string existing=File.Exists(path)?File.ReadAllText(path):"";if(existing.Length!=0&&!existing.StartsWith(header+"\n",StringComparison.Ordinal)){error="Legacy native CSV schema differs";return false;}
            bool actualWin=SkirmishS002AriaRunLog.IsCountedAriaWin(in facts,outcome)&&productionCanary.LegacySavedResultVerified;
            var request=productionCanary.Request;
            row=string.Join(",",new[]{facts.RunId,facts.CatalogId,"LegacyPreset",SkirmishLegacyProductionIdentity.Resource(facts.CatalogId),SkirmishLegacyProductionIdentity.Index(facts.CatalogId).ToString(System.Globalization.CultureInfo.InvariantCulture),facts.CodeHash,facts.ConfigHash,facts.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture),facts.Locale,"Normal","CodedPreset",(string)request.Job["difficulty"],(string)request.Job["size"],facts.NormalSpeed?"1":"0",outcome,facts.EndReason,facts.DurationSeconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture),facts.InputViolations.ToString(),facts.HumanInterventions.ToString(),actualWin?"1":"0","0",facts.TracePath,facts.LogPath}.Select(LegacyCsv));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.AppendAllText(path,(existing.Length==0?header+"\n":"")+row+"\n",new System.Text.UTF8Encoding(false));
            Debug.Log("[SkirmishLegacyProductionRun] outcome="+outcome+" actualNativeWin="+(actualWin?1:0)+" nativeDifficulty=Normal nativeSize=CodedPreset samplingCoverageCredit=0 queueSuccessEnabled=0");return true;
        }
        private static void RecordProductionLegacySnapshot(EntityManager em,Entity session,in SkirmishMatchState match,bool simulationActive,float elapsed,string kind)
        {
            var request=productionCanary.Request;
            var observerBefore=BeginProductionObserverWindow(em,in match);
            var identity=SkirmishLegacyProductionIdentity.CaptureLive(em,session,in match,request.CatalogId);
            request.VerifyNativeSetupIdentity(identity);
            string configHash=SkirmishProductionCanaryRequest.Hash(System.Text.Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical((JObject)identity["native_configuration"])));
            productionCanary.CaptureVerifiedIdentity(identity,configHash);
            bool saved=false;JObject savedFacts=null;
            if(match.Phase==SkirmishPhase.Finished)saved=SkirmishLegacyProductionIdentity.ReadSavedResultMatches(in match,out savedFacts);
            if(saved)productionCanary.LegacySavedResultVerified=true;
            var facts=new JObject{["native_identity_kind"]="LegacyPreset",["native_world_sequence"]=em.WorldUnmanaged.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["native_session_id"]=match.SessionId.ToString(),["catalog_id"]=request.CatalogId,["native_legacy_preset_index"]=match.ScenarioIndex,
                ["native_setup_identity"]=identity,["native_config_sha256"]=configHash,["actual_native_locale"]=GameLocalization.CurrentLocaleCode,
                ["phase"]=match.Phase.ToString(),["outcome"]=match.Outcome.ToString(),["outcome_reason"]=match.Reason.ToString(),["simulation_active"]=simulationActive,
                ["elapsed_seconds"]=elapsed,["time_scale"]=Time.timeScale,["normal_speed_latch"]=speedLatch.Normal,["native_trace_path"]=LiveTracePath(),
                ["legacy_match_frozen"]=match.Phase==SkirmishPhase.Finished&&!simulationActive,["legacy_result_saved_matches"]=saved,["legacy_saved_result"]=savedFacts,
                ["legacy_player_base_entity"]=match.PlayerMainBase.ToString(),["legacy_enemy_base_entity"]=match.EnemyMainBase.ToString(),
                ["legacy_player_base_hp"]=SkirmishLegacyProductionIdentity.BaseHp(em,in match,1),["legacy_enemy_base_hp"]=SkirmishLegacyProductionIdentity.BaseHp(em,in match,2),
                ["requested_sampling_size"]=request.Job["size"].DeepClone(),["requested_sampling_difficulty"]=request.Job["difficulty"].DeepClone(),
                ["native_rules_scope"]="Unmodified legacy preset Normal/default/full-vision configuration. No expanded definition/version/setup hash or expanded-difficulty certification",["actual_save_root"]=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT")};
            productionCanary.Record(kind,facts);
            EndProductionObserverWindow(em,in match,observerBefore,"ProtectedLegacyActualCaptureLiveAndSavedResult");
        }
        // Pure native metadata preparation; actual launch/ARIA/result/return remain mandatory later.
        public static void PrepareLegacyProductionIdentitiesFromSamplingPlan()
        {
            if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||productionCanary!=null||productionBatch!=null)throw new InvalidOperationException("Idle unowned Editor required for legacy identity preparation");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string path=Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_LEGACY_SAMPLING_PLAN"),hash=Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_LEGACY_SAMPLING_SHA256"),output=Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_LEGACY_IDENTITY_OUTPUT");
            if(string.IsNullOrEmpty(output)||!Path.IsPathRooted(output)||!Path.GetFullPath(output).StartsWith(Path.Combine(root,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Fresh absolute legacy metadata output under Logs/SkirmishProduction required");
            var authority=new SkirmishProductionMatrixAuthority(root,path,hash);var plan=SkirmishProductionCanaryRequest.ReadObject(path);
            var slots=((JArray)plan["slots"]).OfType<JObject>().Where(s=>SkirmishLegacyProductionIdentity.IsProtected((string)s["catalog_id"])).ToArray();
            var rows=new JArray();string before=GameLocalization.CurrentLocaleCode;
            try
            {
                foreach(var slot in slots)
                {
                    string locale=(string)slot["locale"]=="EN"?"en":"fa-IR";
                    if(!GameLocalization.SetLocale(locale,false)||GameLocalization.CurrentLocaleCode!=locale)throw new InvalidOperationException("Actual metadata preparation locale did not resolve");
                    var config=QuickGameConfig.Defaults;config.ScenarioIndex=SkirmishLegacyProductionIdentity.Index((string)slot["catalog_id"]);config.MapSeed=(int)slot["seed"];config=config.NormalizeForBaseAssault();
                    var identity=SkirmishLegacyProductionIdentity.CaptureConfiguration((string)slot["catalog_id"],config,GameLocalization.CurrentLocaleCode);
                    rows.Add(new JObject{["kind"]="LegacyPresetExpectedNativeMetadata",["sampling_slot"]=slot.DeepClone(),["native_setup_identity"]=identity,
                        ["setup_sha256"]=SkirmishProductionCanaryRequest.Hash(System.Text.Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(identity))),
                        ["content_sha256"]=SkirmishProductionCanaryRequest.Hash(System.Text.Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(SkirmishLegacyProductionIdentity.ContentIdentity(identity)))),["live_session_observed"]=false});
                }
            }
            finally{if(!GameLocalization.SetLocale(before,false)||GameLocalization.CurrentLocaleCode!=before)throw new InvalidOperationException("Native legacy metadata locale restoration failed");}
            authority.Recheck();Directory.CreateDirectory(Path.GetDirectoryName(output));
            using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(new JObject{["schema_version"]=1,["sampling_plan_sha256"]=hash,["rows"]=rows,["native_jobs"]=0,["queue_success_enabled"]=false}.ToString(Newtonsoft.Json.Formatting.Indented));
            Debug.Log("[LegacyProductionIdentityPreparation] result=PreparedUnqualified rows="+rows.Count+" nativeJobs=0 publicationChanged=0");
        }
    }
}
#endif
