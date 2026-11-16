using System;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    // Source-relative engineering configuration only. Callers own the returned object;
    // profile validity, framing, source fit and native acceptance remain separate gates.
    public static class SkirmishEngineeringViewProfileFactory
    {
        public static SkirmishMissionViewProfile Create(
            SkirmishScenarioDefinitionConfig definition, SkirmishMissionLogicalAuthoringInputs inputs)
        {
            Require(definition!=null&&inputs!=null&&inputs.CatalogId==definition.CatalogId&&
                inputs.DefinitionId==definition.DefinitionId&&inputs.LayoutSemanticFingerprint==
                SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout),"Exact current compiled authoring inputs required");
            var source=definition.CampaignMap;
            Require(source!=null&&inputs.ValueFormat==definition.MapLayout.BindingValueFormat&&
                inputs.SourceSemanticFingerprint==SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source,inputs.ValueFormat)&&
                inputs.SourceMinimumElevation==source.SurfaceMetadata.MinimumHeight&&inputs.SourceMaximumElevation==source.SurfaceMetadata.MaximumHeight,
                "Exact source/elevation pins required");
            var view = ScriptableObject.CreateInstance<SkirmishMissionViewProfile>();
                view.CatalogId=definition.CatalogId;view.DefinitionId=definition.DefinitionId;view.DefinitionVersion=definition.ContentVersion;
                view.AuthoredNonBaseAssault=inputs.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault;
                view.AuthoredGenericBaseAssault=!view.AuthoredNonBaseAssault;view.ObjectiveKind=inputs.ObjectiveKind;
                view.CampaignSourceContentHash=source.ContentHash;view.LayoutContentHash=definition.MapLayout.ContentHash;
                view.GroundReferenceY=inputs.SourceMinimumElevation;
                // Initial source-relative engineering intent, not a universal
                // accepted distance. Actual actor elevation is projected by the
                // existing role-focus owner; all normal zoom PNG gates remain.
                view.MinHeight=view.GroundReferenceY+12;view.NormalHeight=view.GroundReferenceY+18;
                view.MaxHeight=view.GroundReferenceY+20;view.BuildHeight=view.MaxHeight;
                view.Pitch=65;view.Yaw=35;view.NormalFieldOfView=46;view.BuildFieldOfView=50;
                view.MaximumGroundCenterSlantDistance=32;view.InfantryMinimumViewportHeight=.03f;
                view.PlaneMaximumViewportDimension=.45f;
                view.AircraftPitch=50;view.AircraftFieldOfView=60;
                // Positive floor only: role-specific actual-bounds/near-clip fit
                // determines usable aircraft distance, not an inherited80m clamp.
                view.AircraftMinimumDistance=1;view.AircraftMaximumDistance=300;
            return view;
        }

        static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException(reason); }
    }
}
