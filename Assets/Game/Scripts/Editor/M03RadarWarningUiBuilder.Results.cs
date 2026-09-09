using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningUiBuilder
    {
        private static void BuildResultGuide()=>Edit(MissionResultV3PrefabBuilder.PrefabPath,root=>
        {
            var composition=Find(root,"V3Composition");
            var prior=composition.Find("M03ResultGuide"); if(prior!=null) Object.DestroyImmediate(prior.gameObject);
            var guideButton=Button("M03ResultGuide",composition,32,680,420,54,"mission.m03.guide.open");
            guideButton.gameObject.SetActive(false);
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
                Binding(text).Configure("",text.text,true);
            Binding(guideButton.GetComponentInChildren<TMP_Text>(true)).Configure("mission.m03.guide.open","Field guide",false);
            var data=new SerializedObject(root.GetComponent<MissionResultPopupView>());
            Ref(data,"defenseGuideButton",guideButton);
            Ref(data,"defenseRewardsPanel",composition.Find("RewardsPanel").GetComponent<RectTransform>());
            Ref(data,"outcomeBackdrop",Find(root,"BattlefieldBackdrop").GetComponent<UnityEngine.UI.RawImage>());
            Ref(data,"m03ResultBackdrop",AssetDatabase.LoadAssetAtPath<Texture>(M03RadarWarningMediaImporter.ArtRoot+"/M03-D01.png"));
            var labels=new (string Path,string Key)[]
            {
                ("ObjectivesPanel/SectionTitleText","result.objectives"),
                ("PerformancePanel/SectionTitleText","result.performance"),
                ("RewardsPanel/SectionTitleText","result.rewards"),
                ("ObjectivesPanel/Objective_DestroyHostilePatrol/Label","objective.stop_convoy"),
                ("ObjectivesPanel/Objective_KeepCommandSquadAlive/Label","result.post"),
                ("ObjectivesPanel/Objective_CityConsequenceNeutral/Label","result.civilian_goal"),
                ("PerformancePanel/UnitsLostCard/Label","result.squad_losses"),
                ("PerformancePanel/EnemiesDefeatedCard/Label","result.enemies"),
                ("PerformancePanel/CiviliansLostCard/Label","result.civilians")
            };
            var targets=data.FindProperty("defenseLabels"); targets.arraySize=labels.Length;
            for(int i=0;i<labels.Length;i++)
            {
                var item=targets.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("Text").objectReferenceValue=composition.Find(labels[i].Path).GetComponent<TMP_Text>();
                item.FindPropertyRelative("Key").stringValue="mission.m03."+labels[i].Key;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        });
    }
}
