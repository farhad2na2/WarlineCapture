using System;
using System.Linq;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    // Diagnostic preparation only: contains no native setup/hash, launch payload,
    // readiness flag, physical map derivative or permission to launch.
    [Serializable] public sealed class SkirmishMissionLogicalAuthoringInputs
    {
        public string CatalogId, DefinitionId, SourceSemanticFingerprint, LayoutSemanticFingerprint;
        public SkirmishSizeId Size;public SkirmishDifficultyId Difficulty;
        public SkirmishObjectiveKind ObjectiveKind;
        public SkirmishObjectiveAreaBinding ActualAreaBinding;
        public float SourceMinimumElevation, SourceMaximumElevation;
        public SkirmishRoleKind[] RequiredFramingRoles;
        public string[] ActualOpeningPlayerActorIds;
        internal static SkirmishMissionLogicalAuthoringInputs Capture(SkirmishScenarioDefinitionConfig definition,SkirmishResolvedSetup setup)
        {
            var source=definition.CampaignMap;var bounds=source.Bounds;
            var roles=setup.Forces.Where(force=>force.Quantity>0).Select(force=>force.RoleKind).Concat(setup.RoleOverlays.Select(role=>role.RoleKind)).Distinct().OrderBy(role=>(byte)role).ToArray();
            var ids=new System.Collections.Generic.List<string>();
            for(int i=0;i<setup.Forces.Length;i++)
            {
                var force=setup.Forces[i];if(force.FactionId!=1)continue;
                for(int member=0;member<force.Quantity;member++)ids.Add(force.RoleId+".1."+
                    i.ToString(System.Globalization.CultureInfo.InvariantCulture)+"."+member.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            string semantic=SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source);
            return new SkirmishMissionLogicalAuthoringInputs { CatalogId=setup.CatalogId,DefinitionId=setup.DefinitionId,Size=setup.SizeId,Difficulty=setup.DifficultyId,
                ObjectiveKind=setup.ObjectiveKind,SourceSemanticFingerprint=semantic,LayoutSemanticFingerprint=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout),
                SourceMinimumElevation=source.SurfaceMetadata.MinimumHeight,SourceMaximumElevation=source.SurfaceMetadata.MaximumHeight,
                RequiredFramingRoles=roles,ActualOpeningPlayerActorIds=ids.ToArray(),ActualAreaBinding=new SkirmishObjectiveAreaBinding {
                    DefinitionId=definition.DefinitionId,SourceConfigId=source.OperationMapId,SourceAssetGuid=source.SourceSceneReference.AssetGUID,
                    SourceContentHash=source.ContentHash,SourceSemanticFingerprint=semantic,LayoutId=setup.LayoutId,LayoutBindingFingerprint=setup.LayoutBindingFingerprint,
                    SourceMinX=bounds.PlayableMin.x,SourceMinZ=bounds.PlayableMin.z,SourceMaxX=bounds.PlayableMax.x,SourceMaxZ=bounds.PlayableMax.z } };
        }
    }
}
