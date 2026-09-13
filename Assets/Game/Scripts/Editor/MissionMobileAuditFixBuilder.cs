using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class MissionMobileAuditFixBuilder
    {
        public static void Build()
        {
            M04AirliftConfigBuilder.Build();
            M03RadarWarningGuideBuilder.Build();
            M03RadarWarningUiBuilder.RepairHud();
            M04AirliftPresentationBuilder.Build();
            BuildDrawerV3PrefabBuilder.Build();
            MissionUiSerializedBindingsAuthoring.RepairCommittedPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("[MissionMobileAuditFixBuilder] result=Passed");
        }
    }
}
