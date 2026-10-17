#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishS005OpeningViewPolicyAuthoring
    {
        public static void EnableExplicitOpeningRiflePolicy()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Idle Editor preparation required.");
            const string path="Assets/Game/Resources/SkirmishMissionViewProfile_S005.asset";
            var profile=AssetDatabase.LoadAssetAtPath<SkirmishMissionViewProfile>(path);
            if(profile==null||!profile.IsValid||profile.CatalogId!="S005"||profile.AuthoredNonBaseAssault||profile.AuthoredGenericBaseAssault)
                throw new InvalidOperationException("Existing valid scoped S005 profile required; no camera scalar defaults invented.");
            var definition=AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
            if(definition==null||!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _)||
                !SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,155926,
                    new SkirmishContentManifest {RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out _)||
                !profile.Matches(setup,definition))
                throw new InvalidOperationException("Actual packaged S005 definition/source/layout profile pins required.");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=Environment.GetEnvironmentVariable("WARLINE_S005_OPENING_FOCUS_PREPARATION_OUTPUT");
            if(string.IsNullOrEmpty(output)||!Path.GetFullPath(output).StartsWith(Path.Combine(root,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Fresh persistent preparation evidence path required.");
            string before=JsonUtility.ToJson(profile);bool changed=profile.OpeningFocusPolicy!=SkirmishMissionOpeningFocusPolicy.S005FirstOpeningOwnedRifle;
            // Reserve evidence before changing this one logical field. Failures
            // preserve the partial preparation; the source snapshot comes later.
            using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
            {
                writer.WriteLine("{\"scope\":\"S005OpeningLogicalViewPreparation\",\"before\":"+before+",\"nativeVisualAccepted\":false}");writer.Flush();
                if(changed)
                {
                    profile.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.S005FirstOpeningOwnedRifle;
                    EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
                }
                writer.WriteLine("{\"after\":"+JsonUtility.ToJson(profile)+",\"cameraScalarsChanged\":false,\"nativeVisualAccepted\":false}");
            }
            Debug.Log("[SkirmishS005OpeningViewPolicyAuthoring] result="+(changed?"Prepared":"Unchanged")+" policy=S005FirstOpeningOwnedRifle cameraScalarsChanged=0 nativeVisualAccepted=0");
        }
    }
}
#endif
