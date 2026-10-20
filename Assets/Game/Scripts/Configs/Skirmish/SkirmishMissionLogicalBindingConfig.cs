using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Flags] public enum SkirmishViewCaptureStage : byte
    {
        None=0, Opening=1, MinimumZoom=2, MaximumZoom=4, FollowParked=8,
        FollowCruising=16, ManualViewRestored=32, MatchExitRestored=64
    }
    [Serializable] public struct SkirmishRoleFramingRequirement
    {
        public SkirmishRoleKind Role;
        public SkirmishViewCaptureStage Stages;
        public bool ObservePlayer, ObserveEnemy;
        // Authored engineering constraints, never an assertion of visual acceptance.
        public float MinimumViewportHeight, MaximumViewportDimension, SafeEdgeFraction;
        public float MinimumSubjectElevation, MaximumSubjectElevation;
        public SkirmishViewEvidenceRequirement Evidence;
        // Native Detail/Mid/Low samples and actual meshes remain raw evidence.
        // A Low 3D mesh is not automatically a Far billboard or an unreadable dot.
    }
    [Flags] public enum SkirmishViewEvidenceRequirement : byte
    { None=0, NativePng=1, ActualMeshViewportBounds=2, ActualRenderKindAndRoot=4, ObstructionCandidateInventory=8 }
    [Serializable] public sealed class SkirmishMissionLogicalVariant
    {
        public SkirmishSizeId Size;
        public SkirmishDifficultyId Difficulty;
        [SerializeReference] public SkirmishObjectiveAreaConfig Areas;
        public string ReviewedAreaLogicalFingerprint, ReviewedGeometryFingerprint;
        public SkirmishMissionViewProfile View;
        public SkirmishRoleKind OpeningFocusRole;
        public string OpeningFocusStableActorId;
        public float OpeningFocusSmoothSeconds;
        public float SourceMinimumElevation, SourceMaximumElevation;
        public SkirmishRoleFramingRequirement[] Framing;
        public string ReviewedViewFingerprint;
    }
    [CreateAssetMenu(menuName="Game/SkirmishExpansion/Mission Logical Binding")]
    public sealed class SkirmishMissionLogicalBindingConfig : ScriptableObject
    {
        public int SchemaVersion=1, ContentVersion=1, DefinitionVersion;
        public string CatalogId, DefinitionId, SourceOperationMapId, SourceSceneGuid, SourceContentHash;
        public string SourceSemanticFingerprint, LayoutId, ReviewedLayoutSemanticFingerprint;
        public SkirmishObjectiveKind ObjectiveKind;
        public SkirmishMissionLogicalVariant[] Variants;
    }

    public sealed class SkirmishCompiledMissionLogicalBinding
    {
        public readonly SkirmishCompiledObjectiveAreas Areas;
        public readonly SkirmishMissionViewProfile View;
        public readonly string ViewFingerprint, Fingerprint;
        public readonly SkirmishRoleKind OpeningFocusRole;
        public readonly string OpeningFocusStableActorId;
        public readonly float OpeningFocusSmoothSeconds;
        private readonly float sourceMinimumElevation,sourceMaximumElevation;
        private readonly SkirmishRoleFramingRequirement[] framing;
        internal SkirmishCompiledMissionLogicalBinding(SkirmishCompiledObjectiveAreas areas,
            SkirmishMissionLogicalVariant variant,string fingerprint)
        {
            Areas=areas;View=variant.View;ViewFingerprint=variant.ReviewedViewFingerprint;Fingerprint=fingerprint;
            OpeningFocusRole=variant.OpeningFocusRole;OpeningFocusStableActorId=variant.OpeningFocusStableActorId;OpeningFocusSmoothSeconds=variant.OpeningFocusSmoothSeconds;
            sourceMinimumElevation=variant.SourceMinimumElevation;sourceMaximumElevation=variant.SourceMaximumElevation;
            framing=(SkirmishRoleFramingRequirement[])variant.Framing.Clone();
        }
        public SkirmishRoleFramingRequirement[] ReadFraming()=>(SkirmishRoleFramingRequirement[])framing.Clone();
        public bool TryReadFraming(SkirmishRoleKind role,out SkirmishRoleFramingRequirement requirement)
        {foreach(var row in framing)if(row.Role==role){requirement=row;return true;}requirement=default;return false;}
        public bool MatchesCurrentViewValues()=>ViewFingerprint==SkirmishMissionLogicalBindingCompiler.ViewFingerprint(
            new SkirmishMissionLogicalVariant {View=View,Framing=framing,OpeningFocusRole=OpeningFocusRole,
                OpeningFocusStableActorId=OpeningFocusStableActorId,OpeningFocusSmoothSeconds=OpeningFocusSmoothSeconds,
                SourceMinimumElevation=sourceMinimumElevation,SourceMaximumElevation=sourceMaximumElevation});
    }

    public static class SkirmishMissionLogicalBindingCompiler
    {
        public static bool RequiresBinding(SkirmishResolvedSetup setup)=>setup!=null &&
            (setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault||SkirmishMissionDispatch.RequiresSourceBoundDefinition(setup.CatalogId));
        // Compile identity only. Actual native source fit/camera pictures/mission
        // success are recorded separately and never inferred from these digests.
        public static bool TryCompile(SkirmishScenarioDefinitionConfig definition,SkirmishResolvedSetup setup,
            out SkirmishCompiledMissionLogicalBinding compiled,out string error)
        {
            compiled=null;error="Explicit definition-referenced logical binding required.";
            var config=definition?.MissionLogicalBinding;
            var source=definition?.CampaignMap;var layout=definition?.MapLayout;
            if(config==null||source==null||source.SourceSceneReference==null||layout==null||setup==null||!RequiresBinding(setup)||
                config.SchemaVersion!=1||config.ContentVersion<=0||config.CatalogId!=definition.CatalogId||
                config.DefinitionId!=definition.DefinitionId||config.DefinitionVersion!=definition.ContentVersion||
                setup.CatalogId!=definition.CatalogId||setup.DefinitionId!=definition.DefinitionId||setup.ContentVersion!=definition.ContentVersion||
                definition.ObjectiveConfig==null||config.ObjectiveKind!=definition.ObjectiveConfig.Kind||config.ObjectiveKind!=setup.ObjectiveKind||
                config.SourceOperationMapId!=source.OperationMapId||config.SourceSceneGuid!=source.SourceSceneReference.AssetGUID||
                config.SourceContentHash!=source.ContentHash||config.LayoutId!=layout.LayoutId||setup.LayoutId!=layout.LayoutId||
                setup.OperationMapId!=layout.OperationMapId||!Hex(config.SourceSemanticFingerprint)||
                config.SourceSemanticFingerprint!=SourceFingerprint(source)||!Hex(config.ReviewedLayoutSemanticFingerprint)||
                config.ReviewedLayoutSemanticFingerprint!=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)||
                !source.TryValidateMetadata(out error)||!source.TryValidateLocalContentReferences(out error)||config.Variants==null||config.Variants.Length==0||config.Variants.Length>12)return false;
            // Independently recompute the canonical layout on a value copy. Never
            // mutate the actual setup or import/author physical source geometry.
            var clone=JsonUtility.FromJson<SkirmishResolvedSetup>(JsonUtility.ToJson(setup));
            var reasons=new List<SkirmishCompileReason>();
            if(!SkirmishMeasuredLayoutBinder.TryBind(clone,definition,reasons,out string actualLayout)||
                actualLayout!=setup.LayoutBindingFingerprint||
                JsonUtility.ToJson(new Actors{Forces=clone.Forces,Structures=clone.Structures,Bays=clone.ObjectiveTruckBays})!=
                JsonUtility.ToJson(new Actors{Forces=setup.Forces,Structures=setup.Structures,Bays=setup.ObjectiveTruckBays}))
            {error="Actual source/layout/actor projection differs from compiled setup.";return false;}
            SkirmishMissionLogicalVariant selected=null;var keys=new HashSet<int>();
            foreach(var variant in config.Variants)
            {
                if(variant==null||!Enum.IsDefined(typeof(SkirmishSizeId),variant.Size)||variant.Size==SkirmishSizeId.None||
                    !Enum.IsDefined(typeof(SkirmishDifficultyId),variant.Difficulty)||variant.Difficulty==SkirmishDifficultyId.None||
                    !keys.Add(((byte)variant.Size<<8)|(byte)variant.Difficulty))
                {error="Null or ambiguous logical variant.";return false;}
                if(variant.Size==setup.SizeId&&variant.Difficulty==setup.DifficultyId)selected=variant;
            }
            if(selected==null){error="Exact size/difficulty logical variant unavailable.";return false;}
            var bounds=source.Bounds;
            var binding=new SkirmishObjectiveAreaBinding { DefinitionId=definition.DefinitionId,
                SourceConfigId=source.OperationMapId,SourceAssetGuid=source.SourceSceneReference.AssetGUID,
                SourceContentHash=source.ContentHash,SourceSemanticFingerprint=config.SourceSemanticFingerprint,
                LayoutId=layout.LayoutId,LayoutBindingFingerprint=actualLayout,
                SourceMinX=bounds.PlayableMin.x,SourceMinZ=bounds.PlayableMin.z,SourceMaxX=bounds.PlayableMax.x,SourceMaxZ=bounds.PlayableMax.z };
            SkirmishCompiledObjectiveAreas areas=null;
            if(setup.ObjectiveKind==SkirmishObjectiveKind.BaseAssault)
            {
                if(selected.Areas!=null||!string.IsNullOrEmpty(selected.ReviewedAreaLogicalFingerprint)||!string.IsNullOrEmpty(selected.ReviewedGeometryFingerprint))
                {error="Generic BA view binding must not invent non-BA typed areas.";return false;}
            }
            else if(!SkirmishObjectiveAreaCompiler.TryCompile(selected.Areas,binding,setup.ObjectiveKind,out areas,out error)||
                !Hex(selected.ReviewedAreaLogicalFingerprint)||areas.LogicalFingerprint!=selected.ReviewedAreaLogicalFingerprint||
                !Hex(selected.ReviewedGeometryFingerprint)||areas.GeometryFingerprint!=selected.ReviewedGeometryFingerprint)return false;
            if(selected.SourceMinimumElevation!=source.SurfaceMetadata.MinimumHeight||selected.SourceMaximumElevation!=source.SurfaceMetadata.MaximumHeight||
                !float.IsFinite(selected.SourceMinimumElevation)||!float.IsFinite(selected.SourceMaximumElevation)||selected.SourceMinimumElevation>selected.SourceMaximumElevation||
                selected.View==null||!(selected.View.AuthoredNonBaseAssault||selected.View.AuthoredGenericBaseAssault)||selected.View.ObjectiveKind!=setup.ObjectiveKind||
                !selected.View.Matches(setup,definition)||selected.View.GroundReferenceY!=selected.SourceMinimumElevation||
                !ValidateFraming(selected,setup,out error))return false;
            string view=ViewFingerprint(selected);
            if(!Hex(selected.ReviewedViewFingerprint)||selected.ReviewedViewFingerprint!=view)
            {error="Mission view/role/elevation semantic pin differs.";return false;}
            string fingerprint=Hash("mission-logical-v1|"+config.ContentVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)+"|"+
                actualLayout+"|"+config.SourceSemanticFingerprint+"|"+(areas?.LogicalFingerprint??"")+"|"+view);
            compiled=new SkirmishCompiledMissionLogicalBinding(areas,selected,fingerprint);error=null;return true;
        }

        public static bool ValidateFraming(SkirmishMissionLogicalVariant variant,SkirmishResolvedSetup setup,out string error)
        {
            error="Explicit role framing/elevation/normal-view capture requirements missing.";
            if(variant?.Framing==null||setup?.Forces==null||setup.RoleOverlays==null)return false;
            var required=new HashSet<SkirmishRoleKind>(setup.Forces.Where(x=>x.Quantity>0).Select(x=>x.RoleKind));
            foreach(var overlay in setup.RoleOverlays)required.Add(overlay.RoleKind);
            var seen=new HashSet<SkirmishRoleKind>();
            var normal=SkirmishViewCaptureStage.Opening|SkirmishViewCaptureStage.MinimumZoom|SkirmishViewCaptureStage.MaximumZoom|SkirmishViewCaptureStage.MatchExitRestored;
            foreach(var row in variant.Framing)
            {
                if(!Enum.IsDefined(typeof(SkirmishRoleKind),row.Role)||row.Role==SkirmishRoleKind.None||!seen.Add(row.Role)||
                    !row.ObservePlayer||(row.Stages&normal)!=normal||((byte)row.Stages&128)!=0||
                    row.Evidence!=(SkirmishViewEvidenceRequirement.NativePng|SkirmishViewEvidenceRequirement.ActualMeshViewportBounds|
                        SkirmishViewEvidenceRequirement.ActualRenderKindAndRoot|SkirmishViewEvidenceRequirement.ObstructionCandidateInventory)||
                    !Fraction(row.MinimumViewportHeight)||!Fraction(row.MaximumViewportDimension)||row.MinimumViewportHeight>row.MaximumViewportDimension||
                    !float.IsFinite(row.SafeEdgeFraction)||row.SafeEdgeFraction<0||row.SafeEdgeFraction>=.5f||
                    row.MaximumViewportDimension>1-2*row.SafeEdgeFraction||
                    !float.IsFinite(row.MinimumSubjectElevation)||!float.IsFinite(row.MaximumSubjectElevation)||row.MinimumSubjectElevation>row.MaximumSubjectElevation)return false;
                if(setup.Forces.Any(force=>force.FactionId==2&&force.Quantity>0&&force.RoleKind==row.Role)&&!row.ObserveEnemy)return false;
                if(row.Role==SkirmishRoleKind.ConvoyLogic&&row.ObserveEnemy)return false; // CE has no enemy objective trucks.
                if(Aircraft(row.Role))
                {
                    var follow=SkirmishViewCaptureStage.FollowParked|SkirmishViewCaptureStage.FollowCruising|SkirmishViewCaptureStage.ManualViewRestored;
                    if((row.Stages&follow)!=follow||row.MaximumSubjectElevation<=row.MinimumSubjectElevation)return false;
                }
            }
            if(!required.IsSubsetOf(seen))return false;
            if(!seen.Contains(variant.OpeningFocusRole)||string.IsNullOrEmpty(variant.OpeningFocusStableActorId)||
                !float.IsFinite(variant.OpeningFocusSmoothSeconds)||variant.OpeningFocusSmoothSeconds<2.25f)return false;
            bool focus=false;
            for(int forceIndex=0;forceIndex<setup.Forces.Length;forceIndex++)
            {
                var force=setup.Forces[forceIndex];if(force.FactionId!=1||force.RoleKind!=variant.OpeningFocusRole)continue;
                for(int member=0;member<force.Quantity;member++)
                {
                    string id=force.RoleId+".1."+forceIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)+"."+member.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if(id==variant.OpeningFocusStableActorId)focus=true;
                }
            }
            if(!focus)return false;
            error=null;return true;
        }
        private static bool Aircraft(SkirmishRoleKind role)=>SkirmishRoleIds.Category(role)==SkirmishPopulationCategory.Air||
            role==SkirmishRoleKind.TransportPlane||role==SkirmishRoleKind.TransportHeli||role==SkirmishRoleKind.Drone;
        public static string SourceFingerprint(OperationMapDefinition source)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            return Hash(JsonUtility.ToJson(new Source { Id=source.OperationMapId,Identity=source.SourceIdentityHash,Content=source.ContentHash,
                SchemaVersion=source.SchemaVersion,ContentVersion=source.ContentVersion,
                Metadata=source.GeneratedMetadataHash,Bounds=source.Bounds,Grid=source.GridMetadata,Surface=source.SurfaceMetadata,Navigation=source.NavigationMetadata,
                Scene=source.SourceSceneReference.AssetGUID }));
        }
        public static string ViewFingerprint(SkirmishMissionLogicalVariant variant)
        {
            if(variant?.View==null||variant.Framing==null)throw new ArgumentException("Explicit authored view required.");
            using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                writer.Write("mission-view-v1");var view=variant.View;
                writer.Write((byte)variant.OpeningFocusRole);writer.Write(variant.OpeningFocusStableActorId??"");writer.Write(variant.OpeningFocusSmoothSeconds);
                writer.Write(view.SchemaVersion);writer.Write(view.ContentVersion);writer.Write(view.DefinitionVersion);
                writer.Write(view.CatalogId??"");writer.Write(view.DefinitionId??"");writer.Write(view.CampaignSourceContentHash??"");writer.Write(view.LayoutContentHash??"");
                writer.Write(view.AuthoredNonBaseAssault);writer.Write(view.AuthoredGenericBaseAssault);writer.Write((byte)view.ObjectiveKind);
                foreach(float value in new[]{view.MinHeight,view.MaxHeight,view.NormalHeight,view.BuildHeight,view.Pitch,view.Yaw,view.NormalFieldOfView,view.BuildFieldOfView,
                    view.GroundReferenceY,view.MaximumGroundCenterSlantDistance,view.InfantryMinimumViewportHeight,view.PlaneMaximumViewportDimension,
                    view.AircraftPitch,view.AircraftFieldOfView,view.AircraftMinimumDistance,view.AircraftMaximumDistance,variant.SourceMinimumElevation,variant.SourceMaximumElevation})writer.Write(value);
                writer.Write(variant.Framing.Length);
                foreach(var row in variant.Framing.OrderBy(x=>(byte)x.Role))
                {
                    writer.Write((byte)row.Role);writer.Write((byte)row.Stages);writer.Write(row.ObservePlayer);writer.Write(row.ObserveEnemy);
                    writer.Write((byte)row.Evidence);
                    writer.Write(row.MinimumViewportHeight);writer.Write(row.MaximumViewportDimension);writer.Write(row.SafeEdgeFraction);
                    writer.Write(row.MinimumSubjectElevation);writer.Write(row.MaximumSubjectElevation);
                }
            }
            using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();
        }
        public static bool Hex(string value)
        {if(value==null||value.Length!=64)return false;foreach(char c in value)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;return true;}
        private static bool Fraction(float value)=>float.IsFinite(value)&&value>0&&value<1;
        private static string Hash(string text){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        [Serializable] private sealed class Actors {public SkirmishResolvedForceEntry[] Forces;public SkirmishResolvedStructureEntry[] Structures;public SkirmishResolvedConvoyBay[] Bays;}
        [Serializable] private sealed class Source
        {
            public string Id,Identity,Content,Metadata,Scene;
            public int SchemaVersion,ContentVersion;
            public OperationMapBoundsConfig Bounds;public OperationMapGridMetadataConfig Grid;
            public OperationMapSurfaceMetadataConfig Surface;public OperationMapNavigationMetadataConfig Navigation;
        }
    }
}
