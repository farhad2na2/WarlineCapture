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
    public static partial class CH04M05ArmorBreakConfigBuilder
    {
        public const string SupportContextPath="Assets/Game/Configs/Support/CH04M05_ArmorBreak_SupportContext.asset";
        private static void BuildSupportContext()
        {
            var map=Load<OperationMapDefinition>(MapPath);
            var context=AssetDatabase.LoadAssetAtPath<SupportMissionContextConfig>(SupportContextPath);
            if(context==null){context=ScriptableObject.CreateInstance<SupportMissionContextConfig>();AssetDatabase.CreateAsset(context,SupportContextPath);}
            context.MissionId=MissionId;context.OperationMapId=MapId;context.MissionSourceVersion=1;context.Revision=1;
            context.GroundMin=new Vector2(map.Bounds.PlayableMin.x,map.Bounds.PlayableMin.z);
            context.GroundMax=new Vector2(map.Bounds.PlayableMax.x,map.Bounds.PlayableMax.z);
            var relief=map.Anchors.ToArray().Single(a=>a.AnchorId==Prefix+"relief");
            var protectedMin=Vector2.Max(context.GroundMin,new Vector2(relief.Position.x,relief.Position.z)-Vector2.one*relief.Radius);
            var protectedMax=Vector2.Min(context.GroundMax,new Vector2(relief.Position.x,relief.Position.z)+Vector2.one*relief.Radius);
            context.Regions=new[]{new SupportAuthoredGroundRegion{Min=context.GroundMin,Max=context.GroundMax,Visible=true},
                new SupportAuthoredGroundRegion{Min=protectedMin,Max=protectedMax,Visible=true,Protected=true}};
            // High authored corridor over the bounded battlefield. Payload landing remains
            // validated separately against live occupancy and the protected relief region.
            context.Entry=new Vector3(1200,100,529);context.Release=new Vector3(1600,100,529);context.Exit=new Vector3(2000,100,529);
            context.Clearance=20;context.RouteAuthored=true;context.PopulationCeiling=48;
            if(!context.TryValidate(out string error))throw new InvalidOperationException(error);
            EditorUtility.SetDirty(context);
            var policies=Load<SupportMissionPolicyConfig>(SupportAbilityCatalogBuilder.PoliciesPath);
            var entries=policies.Missions.ToList();int index=entries.FindIndex(e=>e.MissionId=="CH04-M05"||e.MissionId==MissionId);
            var entry=new SupportMissionPolicyEntry{MissionId="CH04-M05",AllowedMask=7,LessonKind=SupportAbilityKind.Paratroopers,PopulationCeiling=48};
            if(index<0)entries.Add(entry);else entries[index]=entry;
            policies.Missions=entries.ToArray();EditorUtility.SetDirty(policies);
            var previous=SceneManager.GetActiveScene();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(EntityScenePath,OpenSceneMode.Additive);
                var existing=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SupportMissionContextAuthoring>(true)).FirstOrDefault(a=>a.Config!=null&&a.Config.MissionId==MissionId);
                if(existing==null){var go=new GameObject("Armor Break Support Context");SceneManager.MoveGameObjectToScene(go,scene);existing=go.AddComponent<SupportMissionContextAuthoring>();}
                existing.Config=context;EditorUtility.SetDirty(existing);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Armor Break Support context scene save failed");
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
            Debug.Log("[ArmorBreakSupportContext] result=Passed scope=mission,map,source finiteFuel=ownedReserve protected=relief route=authored allowed=existing-unlocked readiness=preserved");
        }
    }
}
