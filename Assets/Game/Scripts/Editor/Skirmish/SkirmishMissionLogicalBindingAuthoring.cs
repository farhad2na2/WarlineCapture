#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishMissionLogicalBindingAuthoring
    {
        // Explicit API for a fully supplied logical asset. It creates no terrain,
        // route coordinates, camera scalars, proof records or publication status.
        public static void CreateExplicitBindingAsset(SkirmishScenarioDefinitionConfig definition,
            SkirmishMissionLogicalBindingConfig supplied,string assetPath,
            string reviewedSourceSemanticFingerprint,string reviewedLayoutSemanticFingerprint)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||definition==null||supplied==null||
                definition.ObjectiveConfig==null||(definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.BaseAssault&&!SkirmishMissionDispatch.RequiresSourceBoundDefinition(definition.CatalogId, definition.ContentVersion))||
                definition.CampaignMap==null||definition.MapLayout==null||
                !SkirmishMissionLogicalBindingCompiler.Hex(reviewedSourceSemanticFingerprint)||
                reviewedSourceSemanticFingerprint!=SkirmishMissionLogicalBindingCompiler.SourceFingerprint(definition.CampaignMap)||
                !SkirmishMissionLogicalBindingCompiler.Hex(reviewedLayoutSemanticFingerprint)||
                reviewedLayoutSemanticFingerprint!=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout)||
                supplied.SourceSemanticFingerprint!=reviewedSourceSemanticFingerprint||
                supplied.ReviewedLayoutSemanticFingerprint!=reviewedLayoutSemanticFingerprint||
                supplied.CatalogId!=definition.CatalogId||supplied.DefinitionId!=definition.DefinitionId||
                supplied.DefinitionVersion!=definition.ContentVersion||supplied.ObjectiveKind!=definition.ObjectiveConfig.Kind||
                supplied.SourceOperationMapId!=definition.CampaignMap.OperationMapId||
                supplied.SourceSceneGuid!=definition.CampaignMap.SourceSceneReference.AssetGUID||supplied.SourceContentHash!=definition.CampaignMap.ContentHash||
                supplied.LayoutId!=definition.MapLayout.LayoutId||
                supplied.Variants==null||supplied.Variants.Length==0)
                throw new ArgumentException("Exact supplied logical config and independent reviewed source/layout pins required.");
            string prefix="Assets/Game/Configs/SkirmishExpansion/Scenarios/"+definition.CatalogId+"/Logical/";
            string normalized=assetPath?.Replace('\\','/');
            if(normalized==null||!normalized.StartsWith(prefix,StringComparison.Ordinal)||normalized.Contains("..")||
                !normalized.EndsWith(".asset",StringComparison.Ordinal)||File.Exists(normalized)||AssetDatabase.Contains(supplied))
                throw new ArgumentException("Fresh mission-local logical asset path and transient supplied object required.");
            Directory.CreateDirectory(Path.GetDirectoryName(normalized));
            AssetDatabase.CreateAsset(supplied,normalized);
            // Binding the actual definition is a separate explicit authoring step.
            // A new reference still cannot compile until all variant/source/view pins match.
        }

        public static void RunReadOnlyInspection()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Idle owned Editor required.");
            string guid=Environment.GetEnvironmentVariable("WARLINE_LOGICAL_DEFINITION_GUID");
            string output=Environment.GetEnvironmentVariable("WARLINE_LOGICAL_INSPECTION_OUTPUT");
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var definition=AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(path);
            if(definition==null||!Enum.TryParse(Environment.GetEnvironmentVariable("WARLINE_LOGICAL_SIZE"),false,out SkirmishSizeId size)||
                !Enum.TryParse(Environment.GetEnvironmentVariable("WARLINE_LOGICAL_DIFFICULTY"),false,out SkirmishDifficultyId difficulty)||
                !int.TryParse(Environment.GetEnvironmentVariable("WARLINE_LOGICAL_SEED"),System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,out int seed))throw new ArgumentException("Explicit definition GUID/size/difficulty/seed required.");
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            if(!SkirmishSetupMatrixTable.TryLoad(project,out var matrix,out string error))throw new InvalidOperationException(error);
            if(!SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,difficulty,size,seed,
                new SkirmishContentManifest {RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var inputs,out var reasons))
                throw new InvalidOperationException("Logical preparation failed: "+string.Join(";",reasons));
            var receipt=new Inspection { DefinitionPath=path,DefinitionGuid=guid,DefinitionAssetSha256=HashFile(path),
                Inputs=inputs,CompilerReasons=string.Join(";",reasons) };
            string durable=Path.Combine(project,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar;
            if(string.IsNullOrEmpty(output)||!Path.IsPathRooted(output)||!Path.GetFullPath(output).StartsWith(durable,StringComparison.OrdinalIgnoreCase)||File.Exists(output))
                throw new ArgumentException("Fresh absolute persistent Logs/SkirmishProduction output required.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(receipt,true));
            using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))file.Write(bytes,0,bytes.Length);
            Debug.Log("[SkirmishMissionLogicalInspection] result=Passed launchable=0 nativeSourceFit=0 output="+output);
        }
        private static string HashFile(string path)
        {using var file=File.OpenRead(path);using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
        [Serializable] private sealed class Inspection
        {
            public int SchemaVersion=1;
            public string DefinitionPath,DefinitionGuid,DefinitionAssetSha256,CompilerReasons;
            public SkirmishMissionLogicalAuthoringInputs Inputs;
        }
    }
}
#endif
