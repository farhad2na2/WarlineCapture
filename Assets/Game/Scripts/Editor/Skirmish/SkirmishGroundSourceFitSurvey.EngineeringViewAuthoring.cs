#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static bool TryAttachRefinementLogicalBinding(SkirmishScenarioDefinitionConfig definition,
            SkirmishMissionLogicalVariant[] variants,IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            out SkirmishMissionLogicalBindingConfig binding,out string error)
        {
            binding=null;error="Exact disposable definition and all three measured logical variants required";
            if(definition==null||definition.ObjectiveConfig==null||definition.CampaignMap==null||definition.CampaignMap.SourceSceneReference==null||definition.MapLayout==null||variants==null||
                variants.Length!=3||variants.Any(v=>v==null)||!variants.Select(v=>v.Size).OrderBy(v=>(byte)v).SequenceEqual(
                    new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar}.OrderBy(v=>(byte)v)))return false;
            var source=definition.CampaignMap;var prior=definition.MissionLogicalBinding;
            var candidate=ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();
            candidate.CatalogId=definition.CatalogId;candidate.DefinitionId=definition.DefinitionId;candidate.DefinitionVersion=definition.ContentVersion;
            candidate.ObjectiveKind=definition.ObjectiveConfig.Kind;candidate.SourceOperationMapId=source.OperationMapId;
            candidate.SourceSceneGuid=source.SourceSceneReference.AssetGUID;candidate.SourceContentHash=source.ContentHash;
            candidate.SourceSemanticFingerprint=SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source);
            candidate.LayoutId=definition.MapLayout.LayoutId;
            candidate.ReviewedLayoutSemanticFingerprint=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout);
            candidate.Variants=variants;definition.BindMissionLogicalBinding(candidate);
            try
            {
                foreach(var variant in variants)
                    if(!SkirmishSetupCompiler.TryCompile(definition,variant.Difficulty,variant.Size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out _,out var reasons))
                    {error=string.Join(";",reasons);return false;}
                binding=candidate;error=null;return true;
            }
            finally
            {
                if(binding==null){definition.BindMissionLogicalBinding(prior);UnityEngine.Object.DestroyImmediate(candidate);}
            }
        }

        // Inputs come from the existing source/layout compiler authoring API after
        // geometric layout validation. This creates authored requirements, never
        // a PNG acceptance flag. Caller owns the returned disposable View asset.
        public static SkirmishMissionLogicalVariant CreateRefinementEngineeringView(
            SkirmishScenarioDefinitionConfig definition,SkirmishMissionLogicalAuthoringInputs inputs,
            EntityManager em,SkirmishObjectiveAreaConfig measuredAreas,
            IReadOnlyDictionary<SkirmishRoleKind,float> authoredAircraftMinimumViewportHeights)
        {
            var view=SkirmishEngineeringViewProfileFactory.Create(definition,inputs);
            try
            {
                Require(view.IsValid,"Engineering view invalid for actual source height; explicit per-source tuning required");
                var variant=new SkirmishMissionLogicalVariant{Size=inputs.Size,Difficulty=inputs.Difficulty,Areas=measuredAreas,View=view,
                    SourceMinimumElevation=inputs.SourceMinimumElevation,SourceMaximumElevation=inputs.SourceMaximumElevation,
                    OpeningFocusRole=SkirmishRoleKind.Rifle,OpeningFocusSmoothSeconds=2.25f};
                variant.OpeningFocusStableActorId=inputs.ActualOpeningPlayerActorIds.FirstOrDefault(id=>id.StartsWith(SkirmishRoleIds.Rifle+".1.",StringComparison.Ordinal));
                Require(!string.IsNullOrEmpty(variant.OpeningFocusStableActorId),"Actual compiled opening Rifle identity required");
                var framing=new List<SkirmishRoleFramingRequirement>();
                foreach(var role in inputs.RequiredFramingRoles)
                {
                    bool air=SkirmishRoleIds.Category(role)==SkirmishPopulationCategory.Air;
                    float minimum=SkirmishRoleIds.Category(role)==SkirmishPopulationCategory.Infantry?.03f:.01f;
                    float maxElevation=inputs.SourceMaximumElevation;
                    var stages=SkirmishViewCaptureStage.Opening|SkirmishViewCaptureStage.MinimumZoom|
                        SkirmishViewCaptureStage.MaximumZoom|SkirmishViewCaptureStage.MatchExitRestored;
                    if(air)
                    {
                        Require(authoredAircraftMinimumViewportHeights!=null&&authoredAircraftMinimumViewportHeights.TryGetValue(role,out minimum)&&
                            float.IsFinite(minimum)&&minimum>0&&minimum<=.45f,"Explicit measured-role aircraft comfort target required: "+role);
                        var prefab=FindNativePrefab(em,SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                        Require(prefab!=Entity.Null&&em.HasComponent<UnitAirMovement>(prefab),"Actual native aircraft altitude owner missing: "+role);
                        float cruise=em.GetComponentData<UnitAirMovement>(prefab).CruiseHeight;
                        Require(float.IsFinite(cruise)&&cruise>0,"Finite positive actual role cruise height required");
                        maxElevation+=cruise;
                        stages|=SkirmishViewCaptureStage.FollowParked|SkirmishViewCaptureStage.FollowCruising|SkirmishViewCaptureStage.ManualViewRestored;
                    }
                    framing.Add(new SkirmishRoleFramingRequirement{Role=role,Stages=stages,ObservePlayer=true,
                        ObserveEnemy=role!=SkirmishRoleKind.ConvoyLogic,MinimumViewportHeight=minimum,MaximumViewportDimension=air?.45f:.8f,
                        SafeEdgeFraction=.02f,MinimumSubjectElevation=inputs.SourceMinimumElevation,MaximumSubjectElevation=maxElevation,
                        Evidence=SkirmishViewEvidenceRequirement.NativePng|SkirmishViewEvidenceRequirement.ActualMeshViewportBounds|
                            SkirmishViewEvidenceRequirement.ActualRenderKindAndRoot|SkirmishViewEvidenceRequirement.ObstructionCandidateInventory});
                }
                variant.Framing=framing.ToArray();
                if(measuredAreas!=null)
                {
                    Require(SkirmishObjectiveAreaCompiler.TryCompile(measuredAreas,inputs.ActualAreaBinding,inputs.ObjectiveKind,out var areas,out string error),error);
                    variant.ReviewedAreaLogicalFingerprint=areas.LogicalFingerprint;variant.ReviewedGeometryFingerprint=areas.GeometryFingerprint;
                }
                else Require(inputs.ObjectiveKind==SkirmishObjectiveKind.BaseAssault,"Non-BA requires exact geometry-bound area artifact, not a fabricated measurement");
                variant.ReviewedViewFingerprint=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
                return variant;
            }
            catch{UnityEngine.Object.DestroyImmediate(view);throw;}
        }
    }
}
#endif
