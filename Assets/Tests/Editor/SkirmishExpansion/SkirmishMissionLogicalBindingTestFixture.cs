#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Disposable compiler/dispatch fixtures only. These scalar ranges and local
    // digests are test inputs, never measured mission authoring or visual proof.
    public static class SkirmishMissionLogicalBindingTestFixture
    {
        public static void AttachGenericViewOnly(SkirmishScenarioDefinitionConfig definition,List<UnityEngine.Object> owned)
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104729,
                new SkirmishContentManifest {RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out var reasons),string.Join(";",reasons));
            var source=definition.CampaignMap;var view=ScriptableObject.CreateInstance<SkirmishMissionViewProfile>();owned.Add(view);
            view.AuthoredGenericBaseAssault=true;view.ObjectiveKind=SkirmishObjectiveKind.BaseAssault;
            view.CatalogId=definition.CatalogId;view.DefinitionId=definition.DefinitionId;view.DefinitionVersion=definition.ContentVersion;
            view.CampaignSourceContentHash=source.ContentHash;view.LayoutContentHash=definition.MapLayout.ContentHash;
            view.GroundReferenceY=source.SurfaceMetadata.MinimumHeight;view.Pitch=55;view.Yaw=35;
            view.MaximumGroundCenterSlantDistance=500;view.MinHeight=15;
            view.NormalHeight=view.BuildHeight=view.MaxHeight=Mathf.Max(50,view.GroundReferenceY+30);
            view.NormalFieldOfView=46;view.BuildFieldOfView=50;view.InfantryMinimumViewportHeight=.017f;view.PlaneMaximumViewportDimension=.45f;
            view.AircraftPitch=50;view.AircraftFieldOfView=60;view.AircraftMinimumDistance=80;view.AircraftMaximumDistance=300;
            Assert.IsTrue(view.IsValid);
            var variant=new SkirmishMissionLogicalVariant {Size=SkirmishSizeId.Standard,Difficulty=SkirmishDifficultyId.Regular,View=view,
                SourceMinimumElevation=source.SurfaceMetadata.MinimumHeight,SourceMaximumElevation=source.SurfaceMetadata.MaximumHeight,
                OpeningFocusSmoothSeconds=2.25f};
            for(int i=0;i<setup.Forces.Length;i++)if(setup.Forces[i].FactionId==1&&setup.Forces[i].Quantity>0)
            {variant.OpeningFocusRole=setup.Forces[i].RoleKind;variant.OpeningFocusStableActorId=setup.Forces[i].RoleId+".1."+i+".0";break;}
            variant.Framing=setup.Forces.Where(f=>f.Quantity>0).Select(f=>f.RoleKind).Concat(setup.RoleOverlays.Select(r=>r.RoleKind)).Distinct().Select(role=>
                new SkirmishRoleFramingRequirement { Role=role,ObservePlayer=true,ObserveEnemy=true }).ToArray();
            for(int i=0;i<variant.Framing.Length;i++)
            {
                var row=variant.Framing[i];row.MinimumSubjectElevation=variant.SourceMinimumElevation;row.MaximumSubjectElevation=variant.SourceMaximumElevation+100;
                row.MinimumViewportHeight=.017f;row.MaximumViewportDimension=.45f;row.SafeEdgeFraction=.03f;
                row.Stages=SkirmishViewCaptureStage.Opening|SkirmishViewCaptureStage.MinimumZoom|SkirmishViewCaptureStage.MaximumZoom|
                    SkirmishViewCaptureStage.MatchExitRestored|SkirmishViewCaptureStage.FollowParked|SkirmishViewCaptureStage.FollowCruising|SkirmishViewCaptureStage.ManualViewRestored;
                row.Evidence=SkirmishViewEvidenceRequirement.NativePng|SkirmishViewEvidenceRequirement.ActualMeshViewportBounds|
                    SkirmishViewEvidenceRequirement.ActualRenderKindAndRoot|SkirmishViewEvidenceRequirement.ObstructionCandidateInventory;
                variant.Framing[i]=row;
            }
            variant.ReviewedViewFingerprint=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
            var binding=ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();owned.Add(binding);
            binding.CatalogId=definition.CatalogId;binding.DefinitionId=definition.DefinitionId;binding.DefinitionVersion=definition.ContentVersion;
            binding.ObjectiveKind=SkirmishObjectiveKind.BaseAssault;binding.SourceOperationMapId=source.OperationMapId;
            binding.SourceSceneGuid=source.SourceSceneReference.AssetGUID;binding.SourceContentHash=source.ContentHash;
            binding.SourceSemanticFingerprint=SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source);binding.LayoutId=definition.MapLayout.LayoutId;
            binding.ReviewedLayoutSemanticFingerprint=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout);
            binding.Variants=new[]{variant};definition.BindMissionLogicalBinding(binding);
        }
    }
}
#endif
