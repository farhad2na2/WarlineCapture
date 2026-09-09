using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Authoring;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Editor
{
    public static class M03RadarWarningGuideBuilder
    {
        public const string Path="Assets/Game/Configs/Missions/Chapter01/M03_FieldGuide.asset";
        [Serializable] private sealed class Balance { public Identity[] units; }
        [Serializable] private sealed class Identity { public string id,category,implementationStatus; }

        public static void Build()
        {
            var source=JsonUtility.FromJson<Balance>(File.ReadAllText("Design/BalanceConfigs/Combat_Balance_Config_v0_1.json"));
            if(source.units.Length!=57 || source.units.Select(x=>x.id).Distinct().Count()!=57)
                throw new InvalidOperationException("M3 guide requires every one of the 57 distinct canonical identities.");
            var settings=AddressableAssetSettingsDefaultObject.Settings ?? throw new InvalidOperationException("Addressables settings missing.");
            var group=settings.FindGroup("M03 Field Guide") ?? settings.CreateGroup("M03 Field Guide",false,false,false,null,
                typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
            group.GetSchema<BundledAssetGroupSchema>().BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            var classes=source.units.Select(x=>
            {
                bool future=x.implementationStatus=="designReadyNeedsUnityPrefab";
                var prefab=future ? null : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/"+(x.id.StartsWith("Unit_Chr_",StringComparison.Ordinal) ? "Characters/" : "Vehicles/")+x.id+".prefab");
                var authoring=prefab!=null ? prefab.GetComponent<UnitGridAuthoring>() : null;
                var unit=authoring!=null ? new SerializedObject(authoring).FindProperty("config").objectReferenceValue as UnitGridAuthoringConfig : null;
                if(unit==null && !future) throw new InvalidOperationException("Missing guide runtime config: "+x.id);
                string guid=unit==null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(unit));
                if(unit!=null && settings.FindAssetEntry(guid)==null) settings.CreateOrMoveEntry(guid,group).address="guide.m03."+x.id;
                return new MissionGuideClass {Id=x.id,NameKey="mission.m03.class."+x.id,CategoryKey="mission.m03.guide.category."+x.category,
                    UnitAsset=new AssetReferenceT<UnitGridAuthoringConfig>(guid),Availability=future ? MissionGuideAvailability.Unavailable : Availability(x.id)};
            }).ToArray();
            var topics=Enumerable.Range(1,12).Select(i=>new MissionGuideTopic {TitleKey=Key(i,"title"),BodyKey=Key(i,"body"),
                ExampleKey=Key(i,"example"),MistakeKey=Key(i,"mistake"),DiagramKey=Key(i,"diagram")}).ToArray();
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(Path);
            if(guide==null) {guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>(); AssetDatabase.CreateAsset(guide,Path);}
            guide.Configure(topics,classes); EditorUtility.SetDirty(guide); AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(settings);
            Debug.Log("[M03GuideBuilder] result=Passed topics=12 identities=57 runtimeConfigs="+classes.Count(x=>x.UnitAsset.RuntimeKeyIsValid()));
        }
        private static string Key(int i,string field)=>"mission.m03.guide.topic."+i+"."+field;
        private static MissionGuideAvailability Availability(string id)
        {
            if(id.StartsWith("Unit_Chr_Civilian_",StringComparison.Ordinal)) return MissionGuideAvailability.Protected;
            if(id is "Unit_Veh_Radar_Tank" or "Unit_Chr_Soldier_Male_02_Alt_02" or "Unit_Chr_Soldier_Male_02_Alt_04" or
                "Unit_Chr_Soldier_Female_01_Alt_01" or "Unit_Chr_Soldier_Female_02_Alt_01") return MissionGuideAvailability.Friendly;
            if(id is "Unit_Veh_Light_Armored_Car" or "Unit_Veh_APC_Fast" or "Unit_Chr_Insurgent_Male_03" or
                "Unit_Chr_Insurgent_Female_01" or "Unit_Chr_Insurgent_Female_02") return MissionGuideAvailability.Hostile;
            return MissionGuideAvailability.Reference;
        }
    }
}
