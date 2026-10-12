using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    // Audit instrumentation only. Programmatic menu dispatch and an isolated unlocked
    // profile are disclosed; these captures do not establish normal-input acceptance.
    public static class CampaignVisualAudit20261011
    {
        const string Root="Design/AgentReports/CampaignVisualAudit20261011";
        [Serializable] public sealed class Request { public int index; public string run="01"; }
        [Serializable] public sealed class RoleRow { public string role,group; public Vector3 position; }
        [Serializable] public sealed class ViewRow { public string name,file; public Vector3 position,euler,focus;public float fov,orthoSize;public bool orthographic; }
        [Serializable] public sealed class MissionRow
        {
            public int index;public string missionId,titleKey,definitionPath,scenarioPath,mapPath,mapId,sourceScene,sourceBinding;
            public string missionJson,scenarioJson,mapJson;
        }
        [Serializable] public sealed class Inventory { public MissionRow[] missions; }
        [Serializable] public sealed class Receipt
        {
            public string result,missionId,activeMissionId,activeMapId,run,error;
            public bool gameplayStarted;public string quality,graphics,scope="Native visual inspection; isolated unlocked profile; programmatic menu deployment; camera-only inspection views; no mission win claimed";
            public float hour;public int frames;public RoleRow[] roles;public ViewRow[] views;
        }
        static TaskCompletionSource<int> completion;
        static Request request;static MissionRow mission;static Receipt receipt;
        static string directory,previousSaveRoot;
        static double deadline,nextAction;static int phase,frames,settle;
        static bool prepared,submitted,ending,oldOptionsEnabled;
        static EnterPlayModeOptions oldOptions;
        static SceneSetup[] oldScenes;
        static List<(UnityEngine.Object asset,string json)> preserved;
        static List<ViewRow> views;

        public static void ExportInventory()
        {
            Directory.CreateDirectory(Root);
            var maps=AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>("Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset");
            var definitions=AssetDatabase.FindAssets("t:MissionDefinitionConfig",new[]{"Assets/Game/Configs/Missions"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>).Where(x=>x!=null).ToArray();
            var scenarios=AssetDatabase.FindAssets("t:ScenarioSetupConfig").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>).Where(x=>x!=null).ToArray();
            var rows=new List<MissionRow>();
            for(int i=0;i<25;i++)
            {
                string id=CampaignMissionSequence.IdAt(i);var def=definitions.Single(d=>d.MissionId==id);
                if(!maps.TryResolve(def.OperationMapId,out var map))throw new Exception("Map not in shipping catalog: "+def.OperationMapId);
                var scenario=scenarios.Single(d=>d.ScenarioId==def.ScenarioId);
                rows.Add(new MissionRow{index=i,missionId=id,titleKey=def.DisplayNameKey,definitionPath=AssetDatabase.GetAssetPath(def),scenarioPath=AssetDatabase.GetAssetPath(scenario),mapPath=AssetDatabase.GetAssetPath(map),mapId=map.OperationMapId,sourceScene=AssetDatabase.GUIDToAssetPath(map.SourceSceneReference.AssetGUID),sourceBinding=map.SourceBinding.SourceOperationMapId,missionJson=EditorJsonUtility.ToJson(def,true),scenarioJson=EditorJsonUtility.ToJson(scenario,true),mapJson=EditorJsonUtility.ToJson(map,true)});
            }
            File.WriteAllText(Root+"/inventory.json",JsonUtility.ToJson(new Inventory{missions=rows.ToArray()},true));
            Debug.Log("[CampaignVisualInventory] result=Passed missions=25");
        }

        public static Task<int> CaptureMission()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Audit Editor must be idle");
            request=JsonUtility.FromJson<Request>(File.ReadAllText(Root+"/request.json"));
            if(!File.Exists(Root+"/inventory.json"))ExportInventory();
            mission=JsonUtility.FromJson<Inventory>(File.ReadAllText(Root+"/inventory.json")).missions.Single(x=>x.index==request.index);
            directory=Root+"/captures/"+mission.missionId+"/run-"+request.run;
            if(Directory.Exists(directory))throw new Exception("Use a unique run directory to preserve evidence");
            Directory.CreateDirectory(directory);views=new List<ViewRow>();
            receipt=new Receipt{missionId=mission.missionId,run=request.run};
            oldScenes=EditorSceneManager.GetSceneManagerSetup();oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
            preserved=new List<(UnityEngine.Object,string)>();
            var paths=new[]{"ProjectSettings/QualitySettings.asset","ProjectSettings/GraphicsSettings.asset","Assets/Game/Data/UI/RuntimeUiConfig.asset"}
                .Concat(AssetDatabase.FindAssets("t:RenderPipelineAsset",new[]{"Assets/Settings","Assets/Game/Rendering"}).Select(AssetDatabase.GUIDToAssetPath))
                .Concat(AssetDatabase.FindAssets("t:ScriptableRendererData",new[]{"Assets/Settings","Assets/Game/Rendering"}).Select(AssetDatabase.GUIDToAssetPath)).Distinct();
            foreach(var path in paths)foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset!=null)preserved.Add((asset,EditorJsonUtility.ToJson(asset)));
            previousSaveRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",Path.GetFullPath(directory+"/isolated-profile"));
            var saves=SaveService.CreateDefault();var profile=saves.LoadProfile();
            profile.firstLaunchStatus=FirstLaunchProfileState.Completed;profile.firstLaunchWatched=true;profile.firstLaunchLanguage="English";profile.firstLaunchCommanderDisplayName="Commander";saves.SaveProfile(profile);
            var store=new CampaignMissionProgressStore(saves);for(int i=0;i<25;i++)store.EnsureAvailable(CampaignMissionSequence.IdAt(i));
            completion=new TaskCompletionSource<int>();phase=0;frames=0;settle=0;prepared=false;submitted=false;ending=false;nextAction=0;deadline=EditorApplication.timeSinceStartup+300;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=oldOptions|EnterPlayModeOptions.DisableDomainReload;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1600,900);
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity",OpenSceneMode.Single);
            EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();return completion.Task;
        }

        static void Tick()
        {
            if(ending)
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                EditorApplication.update-=Tick;
                try
                {
                    Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",previousSaveRoot);
                    EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
                    EditorSceneManager.RestoreSceneManagerSetup(oldScenes);
                    foreach(var p in preserved)if(p.asset!=null&&EditorJsonUtility.ToJson(p.asset)!=p.json){EditorJsonUtility.FromJsonOverwrite(p.json,p.asset);EditorUtility.SetDirty(p.asset);}
                    AssetDatabase.SaveAssets();
                    receipt.views=views.ToArray();File.WriteAllText(directory+"/receipt.json",JsonUtility.ToJson(receipt,true));
                    Debug.Log("[CampaignVisualAudit] result="+receipt.result+" mission="+mission.missionId+" captures="+views.Count);
                    completion.TrySetResult(receipt.result=="Passed"?0:1);
                }
                catch(Exception e){Debug.LogError(e);completion.TrySetResult(1);}
                return;
            }
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("phase="+phase+" frames="+frames+" submitted="+submitted);
                if(!EditorApplication.isPlaying)return;
                frames++;
                if(frames<30)return;
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;
                var em=world.EntityManager;
                if(!prepared)
                {
                    using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionProgressStoreReferenceComponent));
                    if(roots.CalculateEntityCount()!=1)return;
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(roots.GetSingletonEntity()).Store=new CampaignMissionProgressStore(SaveService.CreateDefault());
                    GameLocalization.SetLocale("en",false);prepared=true;
                }
                if(!submitted)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var model)||!model.IsValid)return;
                    if(EditorApplication.timeSinceStartup<nextAction)return;nextAction=EditorApplication.timeSinceStartup+.4;
                    if(model.SelectedMission.MissionId!=mission.missionId){UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,mission.missionId);return;}
                    submitted=UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,mission.missionId);phase=1;return;
                }
                SkipNarrative();
                var scene=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();if(scene==null||scene.WorldCamera==null||!scene.GameplayStartComplete)return;
                using var active=em.CreateEntityQuery(typeof(ActiveOperationMapComponent));if(active.CalculateEntityCount()!=1)return;
                var value=active.GetSingleton<ActiveOperationMapComponent>();if(value.MissionId.ToString()!=mission.missionId)return;
                if(UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions)&&restrictions.CinematicInteractionLocked)return;
                if(phase==1){phase=2;settle=frames+60;return;}
                if(frames<settle)return;
                if(phase==2)
                {
                    receipt.activeMissionId=value.MissionId.ToString();receipt.activeMapId=value.OperationMapId.ToString();receipt.gameplayStarted=scene.GameplayStartComplete;receipt.frames=frames;
                    receipt.quality=QualitySettings.names[QualitySettings.GetQualityLevel()];receipt.graphics=SystemInfo.graphicsDeviceType.ToString();
                    if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new Exception("No native graphics device");
                    receipt.roles=ReadRoles(em);
                    Capture(scene.WorldCamera,"opening",null,0);
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(directory+"/opening-hud.png"));
                    WriteEnvironment(scene.WorldCamera);
                    phase=3;settle=frames+8;return;
                }
                if(phase==3)
                {
                    if(!File.Exists(directory+"/opening-hud.png"))return;
                    var roles=receipt.roles;
                    var objective=roles.Where(r=>!r.role.Contains("friendly")&&!r.role.Contains("player")).ToArray();if(objective.Length==0)objective=roles;
                    var focus=objective.Length>0?objective.Aggregate(Vector3.zero,(a,r)=>a+r.position)/objective.Length:GroundCenter(scene.WorldCamera);
                    Capture(scene.WorldCamera,"objective-context",focus,155);
                    var first=objective.FirstOrDefault();Capture(scene.WorldCamera,"objective-detail",first==null?focus:first.position,75);
                    var map=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(mission.mapPath);
                    Vector3 min=map.Bounds.PlayableMin,max=map.Bounds.PlayableMax;
                    Capture(scene.WorldCamera,"mission-overview",(min+max)*.5f,Mathf.Clamp(Vector3.Distance(min,max)*.6f,180,750));
                    Finish(true,null);
                }
            }
            catch(Exception e){Finish(false,e.ToString());}
        }
        static RoleRow[] ReadRoles(EntityManager em)
        {
            using var q=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(LocalTransform));using var entities=q.ToEntityArray(Allocator.Temp);
            return entities.Select(e=>new RoleRow{role=em.GetComponentData<CampaignMissionUnitRoleComponent>(e).MissionRoleId.ToString(),group=em.GetComponentData<CampaignMissionUnitRoleComponent>(e).UnitGroupId.ToString(),position=em.GetComponentData<LocalTransform>(e).Position}).ToArray();
        }
        static Vector3 GroundCenter(Camera c){var ray=c.ViewportPointToRay(new Vector3(.5f,.5f));return new Plane(Vector3.up,0).Raycast(ray,out float d)?ray.GetPoint(d):c.transform.position+c.transform.forward*80;}
        static void Capture(Camera c,string name,Vector3? focus,float distance)
        {
            var oldPosition=c.transform.position;var oldRotation=c.transform.rotation;float oldSize=c.orthographicSize,oldFar=c.farClipPlane;var target=c.targetTexture;var oldActive=RenderTexture.active;
            RenderTexture rt=null;Texture2D tex=null;
            try
            {
                if(focus.HasValue){c.transform.position=focus.Value-c.transform.forward*distance;if(c.orthographic)c.orthographicSize=distance*.45f;c.farClipPlane=Mathf.Max(oldFar,distance*3);}
                rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);tex=new Texture2D(1600,900,TextureFormat.RGBA32,false);
                c.targetTexture=rt;RenderTexture.active=rt;c.Render();tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply(false,false);
                File.WriteAllBytes(directory+"/"+name+".png",tex.EncodeToPNG());
                views.Add(new ViewRow{name=name,file=name+".png",position=c.transform.position,euler=c.transform.eulerAngles,focus=focus??GroundCenter(c),fov=c.fieldOfView,orthographic=c.orthographic,orthoSize=c.orthographicSize});
            }
            finally{c.targetTexture=target;RenderTexture.active=oldActive;c.transform.SetPositionAndRotation(oldPosition,oldRotation);c.orthographicSize=oldSize;c.farClipPlane=oldFar;if(rt!=null)UnityEngine.Object.DestroyImmediate(rt);if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);}
        }
        static void WriteEnvironment(Camera c)
        {
            var method=typeof(MobileVisualQualityPlayModeCapture).GetMethod("WriteEnvironmentDiagnostics",BindingFlags.Static|BindingFlags.NonPublic);method.Invoke(null,new object[]{c,directory+"/environment.txt"});
        }
        static bool Visible(object owner,string field){var g=owner?.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;return g!=null&&g.gameObject.activeInHierarchy&&g.alpha>.5f&&g.interactable;}
        static void SkipNarrative()
        {
            if(EditorApplication.timeSinceStartup<nextAction)return;
            var n=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);if(n==null||!Visible(n,"rootGroup"))return;
            Transform root=Visible(n.SkipConfirmationView,"group")?n.SkipConfirmationView.transform:n.PlaybackControlsView?.transform;
            string name=Visible(n.SkipConfirmationView,"group")?"ConfirmButton":"SkipButton";
            var button=root?.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(x=>x.name==name&&x.interactable&&x.gameObject.activeInHierarchy);
            if(button!=null){button.onClick.Invoke();nextAction=EditorApplication.timeSinceStartup+.5;}
        }
        static void Finish(bool passed,string error)
        {
            receipt.result=passed?"Passed":"Failed";receipt.error=error;ending=true;
            if(!passed)Debug.LogError("[CampaignVisualAudit] "+error);
            EditorApplication.ExitPlaymode();
        }
    }
}
