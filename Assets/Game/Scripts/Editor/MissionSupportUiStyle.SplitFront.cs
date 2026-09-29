using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    internal static partial class MissionSupportUiStyle
    {
        private static void RepairTransientMissingScripts(GameObject root)
        {
            string source=File.ReadAllText("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            foreach(string block in Regex.Split(source,@"(?=^--- !u!)",RegexOptions.Multiline))
                if(block.Contains("m_Script: {fileID: 0}") && !block.Contains("Game.UI.Runtime.UiDisabledMaterialStateView") && !block.Contains("Game.UI.Runtime.UiDisabledSelectableVisualStateView"))
                    throw new InvalidOperationException("Unrecognized missing HUD script; preserve its serialized data");
            int removed=0;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))removed+=GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            if(removed>0)Debug.Log("[SplitFrontHudRepair] transientDisabledCachesRemoved="+removed+" serializedGameplayData=none");
        }
        internal static void BuildSplitFrontHud()
        {
            var root=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try
            {
                RepairTransientMissingScripts(root);
                var header=Find(root,"HeaderContent");var old=header.Find("SplitFrontLauncherPanel");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                if(header.GetComponent<SplitFrontLauncherHudView>()==null)header.gameObject.AddComponent<SplitFrontLauncherHudView>();
                if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="SplitFrontLauncherPanel"||t.name=="ConfirmSplitFrontShot"||t.name=="CancelSplitFrontShot"))
                    throw new InvalidOperationException("Obsolete launcher controls remain in native HUD");
                if(PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab")==null)throw new InvalidOperationException("Split Front HUD save failed");
                Debug.Log("[SplitFrontNativeHud] result=Passed launcherPanel=removed attack=existing hold=existing");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
