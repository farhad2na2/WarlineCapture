using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningConfigBuilder
    {
        private static void PopulateGroup(SerializedProperty group,int index)
        {
            string[] groups = {"squad_a","squad_b","ground_sensor","civilians","vanguard","main_body"};
            string name = groups[index];
            Set(group,"groupId",GroupPrefix+name); Set(group,"factionIndex",index == 3 ? 0 : index < 4 ? 1 : 2);
            string[] prefabs = index switch
            {
                0 => new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},
                1 => new[]{"Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Female_02_Alt_01","Chr_Soldier_Female_01_Alt_01"},
                2 => new[]{"Veh_Radar_Tank"},
                3 => new[]{"Chr_Civilian_Female_01","Chr_Civilian_Female_02","Chr_Civilian_Male_01","Chr_Civilian_Male_02"},
                4 => new[]{"Veh_Light_Armored_Car","Chr_Insurgent_Male_03","Chr_Insurgent_Female_01"},
                _ => new[]{"Veh_Light_Armored_Car","Veh_APC_Fast","Chr_Insurgent_Male_03","Chr_Insurgent_Female_02"}
            };
            Array(group.FindPropertyRelative("units"),prefabs.Length,(unit,i) =>
            {
                string key = "Unit_"+prefabs[i];
                string path = "Assets/Game/Prefabs/"+(prefabs[i].StartsWith("Veh_") ? "Vehicles/" : "Characters/")+key+".prefab";
                Require(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null,"Missing canonical prefab "+path);
                Set(unit,"unitConfigKey","unit.m03."+prefabs[i].ToLowerInvariant());
                Set(unit,"runtimePrefabSourceKey",key); Set(unit,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));
                Set(unit,"spawnAnchorId",AnchorPrefix+(index == 4 ? "vanguard_spawn" : index == 5 ? "main_spawn" : name));
                Set(unit,"missionRoleId",index switch { 2 => "role.friendly.ground_sensor",3 => "role.civilian.protected",4 or 5 => "role.hostile.convoy",_ => "role.friendly.command_squad" });
                Set(unit,"count",1);
            });
        }
    }
}
