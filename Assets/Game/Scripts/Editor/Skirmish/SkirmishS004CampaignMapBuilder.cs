#if UNITY_EDITOR
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishS004CampaignMapBuilder
    {
        public const string SourcePath = "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset";
        public const string MapPath = "Assets/Game/Resources/SkirmishS004OperationMap.asset";
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Configure campaign reuse in Edit mode.");
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourcePath);
            if (source == null || !source.TryValidateMetadata(out _)) throw new InvalidOperationException("Campaign airfield source missing or invalid.");
            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapPath);
            if (map == null) { map = ScriptableObject.CreateInstance<OperationMapDefinition>(); AssetDatabase.CreateAsset(map,MapPath); }
            EditorUtility.CopySerialized(source,map);
            map.name = "SkirmishS004OperationMap";
            var data = new SerializedObject(map);
            data.FindProperty("operationMapId").stringValue = "opmap.skirmish.desert_base_01";
            var binding=data.FindProperty("sourceBinding");
            binding.FindPropertyRelative("sourceOperationMapId").stringValue=source.OperationMapId;
            binding.FindPropertyRelative("sourceIdentityHash").stringValue=source.SourceIdentityHash;
            binding.FindPropertyRelative("sourceContentHash").stringValue=source.ContentHash;
            data.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;
            data.FindProperty("contentHash").stringValue="";
            data.FindProperty("generatedMetadataHash").stringValue="";
            data.ApplyModifiedPropertiesWithoutUndo();
            using var sha=SHA256.Create();
            string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();
            data.Update(); data.FindProperty("contentHash").stringValue=hash;
            data.FindProperty("generatedMetadataHash").stringValue=hash; data.ApplyModifiedPropertiesWithoutUndo();
            if(!map.TryValidateMetadata(out var error)||!map.TryValidateLocalContentReferences(out error)) throw new InvalidOperationException(error);
            var layout=AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(SkirmishDefinitionBuilder.ScenarioFolderS004+"/SkirmishLayout_S004.asset");
            if(layout==null) throw new InvalidOperationException("S004 layout missing.");
            var frame=new SkirmishLayoutWorldBindingConfig { OriginX=378.50526f,OriginZ=540,ForwardX=1,ForwardZ=0,AcrossX=0,AcrossZ=1,
                ForwardMetres=595.7895f,AcrossMetres=160,PlayerAnchorId="anchor.skirmish.db.base_player",EnemyAnchorId="anchor.skirmish.db.base_enemy",SourceHash="campaign.cityedgeairfield.s004.townedge.v2" };
            var ld=new SerializedObject(layout);var wb=ld.FindProperty("worldBinding");
            wb.FindPropertyRelative("OriginX").floatValue=frame.OriginX;
            wb.FindPropertyRelative("OriginZ").floatValue=frame.OriginZ;
            wb.FindPropertyRelative("ForwardX").floatValue=frame.ForwardX;
            wb.FindPropertyRelative("ForwardZ").floatValue=frame.ForwardZ;
            wb.FindPropertyRelative("AcrossX").floatValue=frame.AcrossX;
            wb.FindPropertyRelative("AcrossZ").floatValue=frame.AcrossZ;
            wb.FindPropertyRelative("ForwardMetres").floatValue=frame.ForwardMetres;
            wb.FindPropertyRelative("AcrossMetres").floatValue=frame.AcrossMetres;
            wb.FindPropertyRelative("PlayerAnchorId").stringValue=frame.PlayerAnchorId;
            wb.FindPropertyRelative("EnemyAnchorId").stringValue=frame.EnemyAnchorId;
            wb.FindPropertyRelative("SourceHash").stringValue=frame.SourceHash;
            ld.ApplyModifiedPropertiesWithoutUndo();
            var definition=AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(SkirmishDefinitionBuilder.ScenarioFolderS004+"/SkirmishScenario_S004.asset");
            var sd=new SerializedObject(definition);sd.FindProperty("mapLayout").objectReferenceValue=layout;sd.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(map);EditorUtility.SetDirty(layout);EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
            Debug.Log("[SkirmishS004CampaignReuse] result=Passed source="+source.OperationMapId+" scene="+AssetDatabase.GUIDToAssetPath(map.SourceSceneReference.AssetGUID)+" additionalTown=0 campaignAssetsUnchanged=1");
        }
    }
}
#endif
