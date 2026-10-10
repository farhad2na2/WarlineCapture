using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        // Visual integration evidence only: this helper does not claim normal-input mission acceptance.
        public static Task<int> CaptureRefinementRuntime()
        {
            Check();
            var completion=new TaskCompletionSource<int>();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            bool optionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;
            var options=EditorSettings.enterPlayModeOptions;
            var environment=new[]{"WARLINE_MOBILE_VISUAL_CAPTURE_MISSION","WARLINE_MOBILE_VISUAL_CAPTURE_DIR","WARLINE_MOBILE_VISUAL_CAPTURE_WIDTH","WARLINE_MOBILE_VISUAL_CAPTURE_HEIGHT","WARLINE_MOBILE_VISUAL_CAPTURE_MODE"};
            var previous=environment.Select(Environment.GetEnvironmentVariable).ToArray();
            var config=AssetDatabase.LoadMainAssetAtPath("Assets/Game/Data/UI/RuntimeUiConfig.asset");
            if(config==null)throw new InvalidOperationException("Missing runtime UI configuration");
            string configJson=EditorJsonUtility.ToJson(config);
            var preservedPaths=new[]{"ProjectSettings/QualitySettings.asset","ProjectSettings/GraphicsSettings.asset"}
                .Concat(AssetDatabase.FindAssets("t:RenderPipelineAsset",new[]{"Assets/Settings","Assets/Game/Rendering"}).Select(AssetDatabase.GUIDToAssetPath))
                .Concat(AssetDatabase.FindAssets("t:ScriptableRendererData",new[]{"Assets/Settings","Assets/Game/Rendering"}).Select(AssetDatabase.GUIDToAssetPath)).Distinct();
            var preserved=preservedPaths.SelectMany(AssetDatabase.LoadAllAssetsAtPath).Where(a=>a!=null).Distinct()
                .Select(a=>(asset:a,json:EditorJsonUtility.ToJson(a))).ToArray();
            var helper=typeof(MobileVisualQualityPlayModeCapture);
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            helper.GetField("pendingBatchExitCode",flags).SetValue(null,int.MinValue);
            Environment.SetEnvironmentVariable(environment[0],"m02");
            Environment.SetEnvironmentVariable(environment[1],Path.GetFullPath(RefinementReport+"/runtime-m02"));
            Environment.SetEnvironmentVariable(environment[2],"1600");
            Environment.SetEnvironmentVariable(environment[3],"1000");
            Environment.SetEnvironmentVariable(environment[4],null);
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=options|EnterPlayModeOptions.DisableDomainReload;
            double started=EditorApplication.timeSinceStartup;
            EditorApplication.CallbackFunction poll=null;
            void Restore()
            {
                EditorApplication.update-=poll;
                for(int i=0;i<environment.Length;i++)Environment.SetEnvironmentVariable(environment[i],previous[i]);
                EditorSettings.enterPlayModeOptionsEnabled=optionsEnabled;EditorSettings.enterPlayModeOptions=options;
                if(EditorJsonUtility.ToJson(config)!=configJson){EditorJsonUtility.FromJsonOverwrite(configJson,config);EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();}
                foreach(var state in preserved)
                    if(state.asset!=null&&EditorJsonUtility.ToJson(state.asset)!=state.json){EditorJsonUtility.FromJsonOverwrite(state.json,state.asset);EditorUtility.SetDirty(state.asset);}
                AssetDatabase.SaveAssets();RestoreVisualReviewSetup(setup);
                // Play-mode quality restoration can reserialize the active editor tier
                // after the captured asset JSON. Reapply this project's Mobile default last.
                RepairRefinementMobileTier();
            }
            poll=()=>
            {
                int result=(int)helper.GetField("pendingBatchExitCode",flags).GetValue(null);
                if(result==int.MinValue&&EditorApplication.timeSinceStartup-started>420)
                {
                    helper.GetMethod("Complete",flags).Invoke(null,new object[]{false,"Refinement capture timeout"});return;
                }
                if(result==int.MinValue||EditorApplication.isPlayingOrWillChangePlaymode)return;
                Restore();
                Debug.Log("[MapRefinementRuntime] result="+(result==0?"Passed":"Failed")+" mission=m02 evidence=visual-integration");
                completion.SetResult(result);
            };
            EditorApplication.update+=poll;
            try{MobileVisualQualityPlayModeCapture.CaptureCurrent();}
            catch{Restore();throw;}
            return completion.Task;
        }
    }
}
