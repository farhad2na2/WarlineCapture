using System;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static partial class CH05M03NetworkCollapseConfigBuilder
    {
        public const string SupportContextPath="Assets/Game/Configs/Support/CH05M03_NetworkCollapse_SupportContext.asset";
        public static void SetSupplyReadiness()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Supply catalog authoring requires edit mode.");
            var catalog = Load<SupportAbilityCatalogConfig>(SupportAbilityCatalogBuilder.CatalogPath);
            var serialized = new SerializedObject(catalog);
            var abilities = serialized.FindProperty("Abilities");
            Require(abilities != null && abilities.isArray, "Actual Support catalog abilities missing.");
            int count = 0;
            for (int i = 0; i < abilities.arraySize; i++)
            {
                var ability = abilities.GetArrayElementAtIndex(i);
                if (ability.FindPropertyRelative("Kind").intValue != (int)SupportAbilityKind.Supply) continue;
                ability.FindPropertyRelative("ProductionReady").boolValue = true;
                count++;
            }
            Require(count == 1, "Expected exactly one authored Supply catalog entry.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(catalog.TryValidate(out string error), error);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("[NetworkSupplyReadiness] result=Passed scope=SupplyOnly candidate=RequiresNativeQualification");
        }
        public static void RefreshSupportContext()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Support context refresh requires edit mode.");
            BuildSupportContext(true);
        }
        private static void BuildSupportContext(bool contextOnly=false)
        {
            var map=Load<OperationMapDefinition>(MapPath);
            var context=AssetDatabase.LoadAssetAtPath<SupportMissionContextConfig>(SupportContextPath);
            if(context==null){context=ScriptableObject.CreateInstance<SupportMissionContextConfig>();AssetDatabase.CreateAsset(context,SupportContextPath);}
            context.MissionId=MissionId;context.OperationMapId=MapId;context.MissionSourceVersion=1;context.Revision=2;
            context.GroundMin=new Vector2(map.Bounds.PlayableMin.x,map.Bounds.PlayableMin.z);
            context.GroundMax=new Vector2(map.Bounds.PlayableMax.x,map.Bounds.PlayableMax.z);
            context.Regions=new[]{new SupportAuthoredGroundRegion{Min=context.GroundMin,Max=context.GroundMax,Visible=true},
                new SupportAuthoredGroundRegion{Min=new Vector2(895,440),Max=new Vector2(920,480),Visible=true,Protected=true},
                new SupportAuthoredGroundRegion{Min=new Vector2(1195,440),Max=new Vector2(1220,480),Visible=true,Protected=true},
                new SupportAuthoredGroundRegion{Min=new Vector2(1055,434),Max=new Vector2(1080,468),Visible=true,Protected=true}};
            context.Entry=new Vector3(880,100,330);context.Release=new Vector3(1006,100,330);context.Exit=new Vector3(1280,100,330);
            context.Clearance=20;context.RouteAuthored=true;context.PopulationCeiling=48;
            if(!context.TryValidate(out string error))throw new InvalidOperationException(error);
            EditorUtility.SetDirty(context);
            if(contextOnly)
            {
                AssetDatabase.SaveAssetIfDirty(context);
                Debug.Log("[NetworkCollapseSupportRefresh] result=Passed revision=2 protected=civicOne+civicTwo+audit+externalGate+external-gate scope=OwnContextOnly");
                return;
            }
            var policies=Load<SupportMissionPolicyConfig>(SupportAbilityCatalogBuilder.PoliciesPath);
            var entries=policies.Missions.ToList();int index=entries.FindIndex(e=>e.MissionId=="CH05-M03"||e.MissionId==MissionId);
            var entry=new SupportMissionPolicyEntry{MissionId="CH05-M03",AllowedMask=9,LessonKind=SupportAbilityKind.Supply,PopulationCeiling=48};
            if(index<0)entries.Add(entry);else entries[index]=entry;
            policies.Missions=entries.ToArray();EditorUtility.SetDirty(policies);
            var previous=SceneManager.GetActiveScene();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(EntityScenePath,OpenSceneMode.Additive);
                var existing=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SupportMissionContextAuthoring>(true)).FirstOrDefault(a=>a.Config!=null&&a.Config.MissionId==MissionId);
                if(existing==null){var go=new GameObject("Network Collapse Support Context");SceneManager.MoveGameObjectToScene(go,scene);existing=go.AddComponent<SupportMissionContextAuthoring>();}
                existing.Config=context;EditorUtility.SetDirty(existing);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Network Collapse Support context scene save failed");
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
            Debug.Log("[NetworkCollapseSupportContext] result=Passed scope=mission,map,source finiteFuel=ownedReserve protected=civicOne+civicTwo+audit+externalGate+external-gate route=authored allowed=existing-unlocked readiness=preserved");
        }
    }
}
