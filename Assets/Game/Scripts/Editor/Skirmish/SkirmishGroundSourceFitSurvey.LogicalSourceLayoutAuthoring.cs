#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Opt-in diagnostic input. The caller retains its layout; this input and each
        // candidate own independent clones. No persistent assets or measured flags change.
        public sealed class GroundBaLogicalSourceLayoutInput : IDisposable
        {
            readonly OperationMapDefinition source;
            readonly JObject pins;
            readonly string fingerprint;
            SkirmishMapLayoutConfig layout;
            bool disposed;
            internal GroundBaLogicalSourceLayoutInput(OperationMapDefinition source, JObject pins,
                string fingerprint, SkirmishMapLayoutConfig layout)
            { this.source=source;this.pins=(JObject)pins.DeepClone();this.fingerprint=fingerprint;this.layout=layout; }
            internal SkirmishMapLayoutConfig CloneForCandidate(OperationMapDefinition actualSource, string catalogId)
            {
                Require(!disposed&&layout!=null&&ReferenceEquals(source,actualSource),"Live owned logical input on the same Campaign source required");
                ValidateGroundBaLogicalSourceLayout(source,layout,pins,fingerprint);
                return CloneAirCombined80AdaptedLayout(layout,source,catalogId);
            }
            public void Dispose()
            { if(disposed)return;disposed=true;if(layout!=null)UnityEngine.Object.DestroyImmediate(layout);layout=null; }
        }

        public static bool TryCreateGroundBaLogicalSourceLayoutInput(OperationMapDefinition source,
            SkirmishMapLayoutConfig suppliedLayout,JObject reviewedSourcePins,string reviewedLayoutSemanticFingerprint,
            out GroundBaLogicalSourceLayoutInput input,out string error)
        {
            input=null;error=null;SkirmishMapLayoutConfig clone=null;
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle disposable logical authoring required");
                ValidateGroundBaLogicalSourceLayout(source,suppliedLayout,reviewedSourcePins,reviewedLayoutSemanticFingerprint);
                clone=UnityEngine.Object.Instantiate(suppliedLayout);
                Require(JToken.DeepEquals(JObject.Parse(JsonUtility.ToJson(suppliedLayout)),JObject.Parse(JsonUtility.ToJson(clone))),
                    "Owned input clone must retain the entire supplied typed layout");
                input=new GroundBaLogicalSourceLayoutInput(source,reviewedSourcePins,reviewedLayoutSemanticFingerprint,clone);
                clone=null;return true;
            }
            catch(Exception failure){if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);error=failure.ToString();return false;}
        }

        // Existing overloads and protected-counterpart routes remain unchanged.
        public static bool TryCreateGroundBa10Candidate(JObject mission,OperationMapDefinition source,
            GroundBaLogicalSourceLayoutInput input,out GroundObjective30CandidateOwner owner,out string error)
        {
            owner=null;error=null;var created=new GroundObjective30CandidateOwner();
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle disposable logical authoring required");
                RequireLogicalGroundBaMission(mission); // Exact original immutable BA/G row and protected-binding admission.
                Require(input!=null,"Explicit owned source/layout input required");
                created.Layout=created.Own(input.CloneForCandidate(source,(string)mission["catalog_id"]));
                var catalog=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
                Require(catalog!=null&&EditorUtility.IsPersistent(catalog)&&!EditorUtility.IsDirty(catalog),"Clean actual persistent catalog required");
                var profiles=catalog.ToAuthoredSet();
                foreach(var asset in new UnityEngine.Object[]{profiles.ObjectiveBa,profiles.ArmyGround,profiles.StartField,
                    profiles.StartEstablished,profiles.Economy,profiles.Upgrades,profiles.Intel,profiles.Roles})
                    Require(asset!=null&&EditorUtility.IsPersistent(asset)&&!EditorUtility.IsDirty(asset),"Clean actual persistent canonical profiles/role catalog required");
                var objective=created.Own(UnityEngine.Object.Instantiate(profiles.ObjectiveBa));
                objective.ConfigureBaseAssault();
                var serialized=new SerializedObject(objective);
                foreach(JObject row in mission["canonical_size_rows"])
                {
                    string size=(string)row["size_id"],field=size=="Standard"?"standardDeadlineSeconds":size=="War"?"warDeadlineSeconds":"largeWarDeadlineSeconds";
                    Require(size=="Standard"||size=="War"||size=="LargeWar","Canonical size required");
                    serialized.FindProperty(field).intValue=int.Parse((string)row["deadline_seconds"],CultureInfo.InvariantCulture);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                created.Definition=created.Own(ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>());
                created.Definition.Configure((string)mission["catalog_id"],objective,profiles.ArmyGround,
                    (string)mission["tuple"]["start_profile"]=="F"?profiles.StartField:profiles.StartEstablished,
                    profiles.Economy,profiles.Upgrades,profiles.Intel,profiles.Roles,created.Layout,
                    SkirmishSizeId.Standard,mission["required_capabilities"].ToObject<string[]>());
                created.Definition.BindCampaignMap(source);
                RequireCompleteLogicalFacilityUnion(created.Definition,source);
                Require(created.Definition.DefinitionId==(string)mission["definition_id"]&&created.Definition.ScenarioSetupId==(string)mission["scenario_setup_id"]&&
                    created.Definition.MissionLogicalBinding==null&&created.AreaDraft==null&&
                    !created.Layout.WorldBinding.HasMeasuredDeployments&&!created.Layout.WorldBinding.BindAllConfigurations,
                    "Exact unmeasured Ground BA candidate identity required; no publication or launch proof");
                owner=created;return true;
            }
            catch(Exception failure){created.Dispose();error="Explicit logical Ground BA candidate: "+failure;return false;}
        }

        static void RequireLogicalGroundBaMission(JObject mission)
        {
            // Reuse the existing immutable mission authority; this opt-in input owns
            // its explicit source/layout instead of needing an old FC geometry template.
            var rows=GroundBa10Rows().Where(r=>(string)r["catalog_id"]==(string)mission?["catalog_id"]).ToArray();
            Require(rows.Length==1&&JToken.DeepEquals(rows[0],mission),"Exact immutable Ground BA row required");
            Require(mission["protected_binding"].Type==JTokenType.Null,"Protected Ground BA binding is read-only except exact expanded counterpart authoring");
        }

        static void ValidateGroundBaLogicalSourceLayout(OperationMapDefinition source,SkirmishMapLayoutConfig layout,
            JObject pins,string fingerprint)
        {
            Require(pins!=null&&source!=null&&layout!=null,"Explicit typed layout and reviewed Campaign pins required");
            string Pin(string key){Require(pins[key]?.Type==JTokenType.String&&!string.IsNullOrEmpty((string)pins[key]),"Missing exact source pin: "+key);return(string)pins[key];}
            var request=new SkirmishConfigOnlyBatchBuilder.Request { CampaignSource=source,
                ExpectedSourceIdentityHash=Pin("sourceIdentityHash"),ExpectedSourceContentHash=Pin("contentHash"),
                ExpectedSourceAssetSha256=Pin("sourceAssetSha256"),ExpectedSceneGuid=Pin("sourceSceneGuid"),
                ExpectedGridGuid=Pin("gridAssetGuid"),ExpectedSurfaceGuid=Pin("surfaceAssetGuid") };
            var errors=new List<string>();SkirmishConfigOnlyBatchBuilder.ValidateCampaignSource(request,errors);
            Require(errors.Count==0,string.Join(";",errors));
            Require(AssetDatabase.GetAssetPath(source)==Pin("sourcePath")&&source.OperationMapId==Pin("operationMapId")&&
                SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source)==Pin("sourceSemanticFingerprint")&&
                source.NavigationMetadata.AuthoredSubSceneGuid==Pin("authoredSubSceneGuid"),"Exact Campaign source path/semantic/authored scene pin differs");
            foreach(var row in new[]{new[]{"gridPath","gridAssetSha256",request.ExpectedGridGuid},
                new[]{"surfacePath","surfaceAssetSha256",request.ExpectedSurfaceGuid}})
                Require(AssetDatabase.GUIDToAssetPath(row[2])==Pin(row[0])&&Hash(Pin(row[0]))==Pin(row[1]),"Actual grid/surface GUID, path or raw bytes differ");
            Require(SkirmishMissionLogicalBindingCompiler.Hex(fingerprint)&&
                SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)==fingerprint,"Exact reviewed full-layout semantic fingerprint required");
            Require(layout.OperationMapId==source.OperationMapId&&layout.HasWorldBinding&&layout.WorldBinding.SourceHash==source.ContentHash&&
                !layout.WorldBinding.HasMeasuredDeployments&&!layout.WorldBinding.BindAllConfigurations&&
                layout.ContentHash!=null&&layout.ContentHash.StartsWith("unqualified.",StringComparison.Ordinal),"Unqualified physical-source survey layout required");
            errors.Clear();SkirmishConfigOnlyBatchBuilder.ValidateLogicalGeometryDeclaration(layout,source,errors);
            Require(errors.Count==0,string.Join(";",errors));
            Require(layout.Anchors.All(x=>!string.IsNullOrWhiteSpace(x.RoleId))&&
                layout.Pads.All(x=>!string.IsNullOrWhiteSpace(x.PadId)&&x.FactionId<=2&&x.Kind!=SkirmishLegalPadKind.None&&Enum.IsDefined(typeof(SkirmishLegalPadKind),x.Kind))&&
                layout.Anchors.Select(x=>x.RoleId).Distinct(StringComparer.Ordinal).Count()==layout.Anchors.Length&&
                layout.Pads.Select(x=>x.PadId).Distinct(StringComparer.Ordinal).Count()==layout.Pads.Length,"Unique typed anchor roles and pad identities required");
            Require(layout.WorldWidthMetres>0&&layout.WorldDepthMetres>0&&layout.CellSize>0&&layout.CellSize==source.GridMetadata.CellSize&&
                LogicalFinite(layout.WorldWidthMetres)&&LogicalFinite(layout.WorldDepthMetres)&&LogicalFinite(layout.CellSize)&&
                LogicalFinite(layout.OriginX)&&LogicalFinite(layout.OriginZ),"Finite positive logical envelope required");
            foreach(var anchor in layout.Anchors)
                Require(anchor.NormalizedU>=0&&anchor.NormalizedU<=1&&anchor.NormalizedV>=0&&anchor.NormalizedV<=1&&layout.TryProjectLocalToMap(anchor.WorldX,anchor.WorldZ,out float storedX,out float storedZ)&&
                    layout.TryProjectToMap(anchor.NormalizedU,anchor.NormalizedV,out float normalizedX,out float normalizedZ)&&
                    Math.Abs(storedX-normalizedX)<.001f&&Math.Abs(storedZ-normalizedZ)<.001f&&InsideLogicalPlayable(source,storedX,storedZ),
                    "Stored and normalized anchor projections must agree inside the source");
            // Initial source/layout seam follows MaterializePilotClone's identity frame.
            // Compounds are world rectangles; pads/anchors are authoring-local.
            // Identity framing keeps both domains equal. Rotation/scaling is unsupported.
            var frame=layout.WorldBinding;
            Require(frame.ForwardX==1&&frame.ForwardZ==0&&frame.AcrossX==0&&frame.AcrossZ==1&&
                frame.OriginX==layout.OriginX&&frame.OriginZ==layout.OriginZ+layout.WorldDepthMetres*.5f&&
                frame.ForwardMetres==layout.WorldWidthMetres&&frame.AcrossMetres==layout.WorldDepthMetres,
                "Explicit identity survey frame required; rotated/scaled compounds unsupported");
            foreach(var route in layout.Routes)
            {
                Require(Enum.IsDefined(typeof(SkirmishMeasuredRouteKind),route.Kind)&&route.Kind!=SkirmishMeasuredRouteKind.None&&
                    route.WaypointAnchorIds!=null&&route.WaypointAnchorIds.Length>=2&&
                    route.WaypointAnchorIds.Distinct(StringComparer.Ordinal).Count()==route.WaypointAnchorIds.Length&&
                    LogicalFinite(route.WidthMetres)&&route.WidthMetres>0&&LogicalFinite(route.LengthMetres)&&route.LengthMetres>0&&
                    new[]{route.InfantryTransitSeconds,route.GroundTransitSeconds,route.AirTransitSeconds,
                        route.InfantryFirstContactSeconds,route.GroundFirstContactSeconds}.All(x=>LogicalFinite(x)&&x>=0),
                    "Finite provisional route scalars and valid typed anchor chain required");
            }
            foreach(byte faction in new byte[]{1,2})
            {
                Require(layout.TryGetFactionCompound(faction,out var compound),"Two exact source-bound typed compounds required");
                foreach(float worldX in new[]{compound.MinX,compound.MaxX})foreach(float worldZ in new[]{compound.MinZ,compound.MaxZ})
                    Require(InsideLogicalPlayable(source,worldX,worldZ),"World compound leaves actual playable source bounds");
                foreach(var group in layout.Pads.Where(p=>p.FactionId==faction&&p.Kind!=SkirmishLegalPadKind.ObjectiveTruckSpawn&&p.Kind!=SkirmishLegalPadKind.DefenseApproach).GroupBy(p=>p.Kind))
                    Require(group.Count()==1,"One typed category pad per faction/kind required; facilities use placement offsets");
            }
            Require(layout.HasFacilityPlacements,"Explicit full facility placement declarations required");
            var placements=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in layout.FacilityPlacements)
            {
                Require((row.FactionId==1||row.FactionId==2)&&!string.IsNullOrEmpty(row.StructureId)&&placements.Add(row.FactionId+"/"+row.StructureId)&&
                    !float.IsNaN(row.AcrossOffsetMetres)&&!float.IsInfinity(row.AcrossOffsetMetres)&&!float.IsNaN(row.ForwardOffsetMetres)&&!float.IsInfinity(row.ForwardOffsetMetres)&&
                    layout.Pads.Count(p=>p.PadId==row.PadId&&p.FactionId==row.FactionId)==1,"Unique finite typed facility-to-pad declaration required");
            }
            foreach(var row in layout.FacilityPlacements)
                ValidateLogicalFacilityCenter(layout,source,new SkirmishResolvedStructureEntry{FactionId=row.FactionId,StructureId=row.StructureId},row);
            foreach(var pad in layout.Pads)foreach(int dx in new[]{-1,1})foreach(int dz in new[]{-1,1})
            {
                Require(layout.TryProjectLocalToMap(pad.CenterX+dx*pad.WidthMetres*.5f,pad.CenterZ+dz*pad.DepthMetres*.5f,out float x,out float z)&&
                    InsideLogicalPlayable(source,x,z),"Typed pad leaves actual playable source bounds");
            }
            // Declarations only: no scenery/occupancy, per-unit cells, grounded Y,
            // normal facility/service placement, route traversal or measured approval.
        }
        static void RequireCompleteLogicalFacilityUnion(SkirmishScenarioDefinitionConfig definition,OperationMapDefinition source)
        {
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            var required=new HashSet<string>(StringComparer.Ordinal);var layout=definition.MapLayout;
            foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
            {
                Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,SkirmishDifficultyId.Regular,size,1,
                    new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                    budget!=null&&budget.ExactCanonicalQuantities&&!budget.NormalCompilerPassed,
                    "Exact original all-size diagnostic budget, without normal compiler acceptance, required: "+string.Join(";",reasons));
                foreach(var structure in budget.Structures)
                {
                    string key=structure.FactionId+"/"+structure.StructureId;required.Add(key);
                    var rows=layout.FacilityPlacements.Where(x=>x.FactionId==structure.FactionId&&x.StructureId==structure.StructureId).ToArray();
                    Require(rows.Length==1,"Every actual canonical facility must have one explicit placement declaration: "+key);
                    ValidateLogicalFacilityCenter(layout,source,structure,rows[0]);
                }
            }
            Require(required.SetEquals(layout.FacilityPlacements.Select(x=>x.FactionId+"/"+x.StructureId)),
                "Facility declarations must equal the actual three-size structure union; no invented structures");
        }
        static void ValidateLogicalFacilityCenter(SkirmishMapLayoutConfig layout,OperationMapDefinition source,
            SkirmishResolvedStructureEntry structure,SkirmishFacilityPlacementConfig row)
        {
            Require(layout.TryGetFactionCompound(row.FactionId,out var compound),"Actual own compound required");
            Require(Game.Configs.SkirmishMapLayoutBuilder.TryPadForStructure(layout,structure,out var pad)&&pad.PadId==row.PadId&&
                Array.IndexOf(compound.FacilityPadIds,row.PadId)>=0,
                "Original typed structure category and own compound member pad required");
            Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z),"Actual facility pad projection required");
            Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout,row.AcrossOffsetMetres,row.ForwardOffsetMetres,ref x,ref z);
            Require(InsideLogicalPlayable(source,x,z)&&x>=compound.MinX&&x<=compound.MaxX&&z>=compound.MinZ&&z<=compound.MaxZ,
                "Declared facility center must stay inside its own source-bound compound");
            if(structure.DesignatedBase)
            {
                string role=structure.FactionId==1?"base.player":"base.enemy";
                Require(layout.TryGetAnchor(role,out var anchor)&&structure.SpawnAnchorId==anchor.AnchorId&&
                    layout.TryProjectToMap(anchor.NormalizedU,anchor.NormalizedV,out float ax,out float az)&&Math.Abs(x-ax)<.01f&&Math.Abs(z-az)<.01f,
                    "Actual designated base and typed base anchor must resolve one position");
            }
        }
        static bool LogicalFinite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool InsideLogicalPlayable(OperationMapDefinition source,float x,float z) =>
            !float.IsNaN(x)&&!float.IsInfinity(x)&&!float.IsNaN(z)&&!float.IsInfinity(z)&&
            x>=source.Bounds.PlayableMin.x&&x<=source.Bounds.PlayableMax.x&&z>=source.Bounds.PlayableMin.z&&z<=source.Bounds.PlayableMax.z;
    }
}
#endif
