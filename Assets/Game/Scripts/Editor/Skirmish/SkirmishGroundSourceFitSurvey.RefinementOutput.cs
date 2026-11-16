#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static Dictionary<SkirmishRoleKind,float> CreateRefinementAircraftEngineeringTargets(
            IEnumerable<SkirmishMissionLogicalAuthoringInputs> inputs,EntityManager em,out JArray targetFacts)
        {
            var targets=new Dictionary<SkirmishRoleKind,float>();targetFacts=new JArray();
            foreach(var role in inputs.SelectMany(i=>i.RequiredFramingRoles).Distinct().Where(r=>SkirmishRoleIds.Category(r)==SkirmishPopulationCategory.Air))
            {
                var prefab=FindNativePrefab(em,SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                Require(prefab!=Entity.Null&&em.HasComponent<UnitSelectionHitbox>(prefab),"Actual native role body bounds required for authored aircraft target: "+role);
                var hitbox=em.GetComponentData<UnitSelectionHitbox>(prefab);
                Require(math.all(math.isfinite(hitbox.Extents))&&math.all(hitbox.Extents>0),"Finite actual body extents required");
                // A role-specific initial engineering lower bound derived from its
                // actual body at the authored far lens. Swept rotor/near-clip and
                // comfortable native imagery remain independent required gates.
                float projectedRadius=hitbox.Extents.y*Mathf.Cos(50*Mathf.Deg2Rad)+math.max(hitbox.Extents.x,hitbox.Extents.z)*Mathf.Sin(50*Mathf.Deg2Rad);
                float minimum=projectedRadius/(300*Mathf.Tan(30*Mathf.Deg2Rad));
                Require(float.IsFinite(minimum)&&minimum>0&&minimum<.45f,"Actual role bounds cannot support proposed aircraft engineering lens");
                targets.Add(role,minimum);targetFacts.Add(new JObject{["role"]=role.ToString(),["nativePrefabIndex"]=prefab.Index,["nativePrefabVersion"]=prefab.Version,
                    ["bodyExtents"]=new JArray(hitbox.Extents.x,hitbox.Extents.y,hitbox.Extents.z),["minimumViewportHeightEngineering"]=minimum,
                    ["scope"]="ActualBodyProjectionAtPitch50Fov60Distance300_NotRotorOrComfortAcceptance"});
            }
            return targets;
        }

        static void FinalizeRefinementAuthoring(GroundObjective30CandidateOwner owner,EntityManager em,GridConfig grid,
            MapSurfaceComponent surface,JObject output,string artifactPrefix)
        {
            if(output["strictThreeSizePlacementPass"]==null)return;
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(artifactPrefix)&&Path.IsPathRooted(artifactPrefix)&&Path.GetFullPath(artifactPrefix).StartsWith(allowed,StringComparison.OrdinalIgnoreCase),"Owned persistent logical artifact prefix required");
            var definition=owner.Definition;var layout=owner.Layout;
            var frame=EncodePilotFrame(layout.WorldBinding);
            foreach(var pair in new[]{("base.player","Player"),("base.enemy","Enemy")})
            {
                Require(layout.TryGetAnchor(pair.Item1,out var anchor),"Actual measured deployment anchor required");
                Require(layout.TryProjectLocalToMap(anchor.WorldX,anchor.WorldZ,out float x,out float z),"Actual measured deployment projection required");
                frame[pair.Item2+"AnchorId"]=anchor.AnchorId;frame[pair.Item2+"Deployment"]=new JObject{["x"]=x,["y"]=z};
            }
            // These flags describe the just-collected three-size static deployment
            // evidence. They do not assert route independence, live fit or camera acceptance.
            frame["HasMeasuredDeployments"]=true;frame["BindAllConfigurations"]=true;
            JsonUtility.FromJsonOverwrite(new JObject{["worldBinding"]=frame,
                ["contentHash"]="logical.refinement.v1."+SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)}.ToString(Newtonsoft.Json.Formatting.None),layout);
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            var inputs=new List<SkirmishMissionLogicalAuthoringInputs>();
            foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
            {
                Require(SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,SkirmishDifficultyId.Regular,size,1,
                    new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var actual,out var reasons),string.Join(";",reasons));
                inputs.Add(actual);
            }
            var targets=CreateRefinementAircraftEngineeringTargets(inputs,em,out var targetFacts);
            output["aircraftEngineeringTargetFacts"]=targetFacts;
            Require(owner.AreaDraft!=null||definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.BaseAssault,"Only BA may omit typed areas");
            var boundAreas=inputs.Select(actual=>owner.AreaDraft==null?null:BindRefinementAreaGeometry(owner.AreaDraft,actual)).ToArray();
            string path=null,artifactSha=null;
            if(owner.AreaDraft!=null)
            {
            var areaArtifact=new JObject{["scope"]="ActualSourceBoundTypedPerimeterSampling_NotNativeSourceFitCertification",
                ["sourcePins"]=output["sourcePins"].DeepClone(),["variantGeometry"]=new JArray(inputs.Select((actual,index)=>new JObject{
                    ["size"]=actual.Size.ToString(),["difficulty"]=actual.Difficulty.ToString(),
                    ["geometryFingerprint"]=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(boundAreas[index])})),
                ["layoutSemanticFingerprint"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout),
                ["nativeGrid"]=new JObject{["width"]=grid.Width,["height"]=grid.Height,["cellSize"]=grid.CellSize,["origin"]=new JArray(grid.Origin.x,grid.Origin.y,grid.Origin.z)},
                ["samples"]=Ground30AreaSamples(owner.AreaDraft,grid,surface),["sourceFitCertified"]=false};
            path=artifactPrefix+".typed-perimeters.json";Directory.CreateDirectory(Path.GetDirectoryName(path));
            using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
            {writer.Write(areaArtifact.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            artifactSha=Hash(path);output["typedPerimeterArtifact"]=new JObject{["path"]=path,["sha256"]=artifactSha};
            }
            else output["typedPerimeterArtifact"]=null;
            var variants=new List<SkirmishMissionLogicalVariant>();
            for(int index=0;index<inputs.Count;index++)
            {
                var actual=inputs[index];var areas=boundAreas[index];
                if(areas!=null)
                {
                    string geometry=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(areas);
                    areas.Measurement=new SkirmishObjectiveAreaEvidence{ArtifactId=path,ArtifactSha256=artifactSha,GeometryFingerprint=geometry};
                }
                var variant=CreateRefinementEngineeringView(definition,actual,em,areas,targets);
                owner.Own(variant.View);variants.Add(variant);
            }
            Require(TryAttachRefinementLogicalBinding(definition,variants.ToArray(),matrix,out var binding,out error),error);
            owner.Own(binding);
            output["logicalSeedReadback"]=AssertAll120DefinitionBindings(definition,false);
            output["authoredDifficulty"]="Regular";
            // Explicit primitive graph; never persist transient Unity instance IDs
            // from JsonUtility serializing a ScriptableObject reference graph.
            var graph=new JObject{["SchemaVersion"]=binding.SchemaVersion,["ContentVersion"]=binding.ContentVersion,["DefinitionVersion"]=binding.DefinitionVersion,
                ["CatalogId"]=binding.CatalogId,["DefinitionId"]=binding.DefinitionId,["ObjectiveKind"]=(int)binding.ObjectiveKind,
                ["SourceOperationMapId"]=binding.SourceOperationMapId,["SourceSceneGuid"]=binding.SourceSceneGuid,["SourceContentHash"]=binding.SourceContentHash,
                ["SourceSemanticFingerprint"]=binding.SourceSemanticFingerprint,["LayoutId"]=binding.LayoutId,
                ["ReviewedLayoutSemanticFingerprint"]=binding.ReviewedLayoutSemanticFingerprint};
            var encoded=new JArray();graph["variants"]=encoded;
            foreach(var variant in variants)
            {
                var flat=new RefinementVariantValues{Size=variant.Size,Difficulty=variant.Difficulty,Areas=variant.Areas,Framing=variant.Framing,
                    ReviewedAreaLogicalFingerprint=variant.ReviewedAreaLogicalFingerprint,ReviewedGeometryFingerprint=variant.ReviewedGeometryFingerprint,
                    ReviewedViewFingerprint=variant.ReviewedViewFingerprint,OpeningFocusRole=variant.OpeningFocusRole,OpeningFocusStableActorId=variant.OpeningFocusStableActorId,
                    OpeningFocusSmoothSeconds=variant.OpeningFocusSmoothSeconds,SourceMinimumElevation=variant.SourceMinimumElevation,SourceMaximumElevation=variant.SourceMaximumElevation};
                var value=JObject.Parse(JsonUtility.ToJson(flat));if(variant.Areas==null)value["Areas"]=JValue.CreateNull();value["View"]=JObject.Parse(JsonUtility.ToJson(variant.View));encoded.Add(value);
            }
            output["authoredDefinitionLogicalBindingGraph"]=graph;
            output["normalThreeSizeCompilerPassed"]=true;output["authoredEngineeringOnly"]=true;
        }
        public static SkirmishObjectiveAreaConfig BindRefinementAreaGeometry(SkirmishObjectiveAreaConfig draft,SkirmishMissionLogicalAuthoringInputs actual)
        {
            Require(draft!=null&&actual!=null&&!string.IsNullOrEmpty(actual.ActualAreaBinding.LayoutBindingFingerprint),"Actual variant area binding required");
            var areas=JsonUtility.FromJson<SkirmishObjectiveAreaConfig>(JsonUtility.ToJson(draft));
            areas.Binding=actual.ActualAreaBinding;areas.Measurement=default;return areas;
        }
        [Serializable]sealed class RefinementVariantValues
        {
            public SkirmishSizeId Size;public SkirmishDifficultyId Difficulty;public SkirmishObjectiveAreaConfig Areas;
            public SkirmishRoleFramingRequirement[] Framing;public string ReviewedAreaLogicalFingerprint,ReviewedGeometryFingerprint,ReviewedViewFingerprint;
            public SkirmishRoleKind OpeningFocusRole;public string OpeningFocusStableActorId;public float OpeningFocusSmoothSeconds,SourceMinimumElevation,SourceMaximumElevation;
        }
    }
}
#endif
