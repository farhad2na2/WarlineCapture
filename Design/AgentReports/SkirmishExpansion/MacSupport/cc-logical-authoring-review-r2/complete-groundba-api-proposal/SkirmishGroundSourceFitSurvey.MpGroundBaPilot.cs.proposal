#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    // PROPOSAL ONLY. Integrate and compile before invoking; no native credit yet.
    public static partial class SkirmishGroundSourceFitSurvey
    {

        // Proposal: six logical anchors only; native legality and later hulls remain gates.
        static JObject ApplyMpPilotEndpointRevision(SkirmishScenarioDefinitionConfig definition,GridConfig grid)
        {
            var layout=definition.MapLayout;
            Require(!UnityEditor.EditorUtility.IsPersistent(definition)&&!UnityEditor.EditorUtility.IsPersistent(layout),
                "Only actual disposable candidate definition/layout may be revised");
            Require((definition.CatalogId=="S049"||definition.CatalogId=="S050")&&
                definition.CampaignMap.OperationMapId=="opmap.ch04.armor_break"&&
                definition.CampaignMap.ContentHash=="da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86"&&
                layout.OperationMapId==definition.CampaignMap.OperationMapId&&
                layout.WorldBinding.SourceHash==definition.CampaignMap.ContentHash&&
                !layout.WorldBinding.HasMeasuredDeployments&&!layout.WorldBinding.BindAllConfigurations,
                "Exact unqualified MP pilot source/layout required");
            Require(grid.Width==1280&&grid.Height==520&&grid.CellSize==1&&grid.Origin.x==740&&grid.Origin.z==260,
                "Exact reviewed ArmorBreak grid required");
            Require(layout.Routes.Length==3&&layout.Routes.Select(r=>r.RouteId).OrderBy(v=>v)
                .SequenceEqual(new[]{"route.pilot.0","route.pilot.1","route.pilot.2"}),"Exact three pilot routes required");
            var before=JObject.Parse(JsonUtility.ToJson(layout));
            var anchors=(JArray)before["anchors"].DeepClone();var changed=new JArray();
            for(int route=0;route<3;route++)
            {
                var actual=layout.Routes.Single(r=>r.RouteId=="route.pilot."+route);
                Require(actual.WaypointAnchorIds.Length==4&&actual.ProvisionalTimes,"Reviewed four-point provisional pilot route required");
                for(int side=0;side<2;side++)
                {
                    string id="anchor.pilot.approach."+route+"."+(side==0?0:3);
                    Require(actual.WaypointAnchorIds[side==0?0:3]==id,"Exact typed route endpoint identity required");
                    var anchor=anchors.OfType<JObject>().Single(a=>(string)a["AnchorId"]==id);
                    float oldX=(float)anchor["WorldX"],oldZ=(float)anchor["WorldZ"];
                    float x=side==0?1381.5f:1667.5f,z=676.5f;
                    Require((math.abs(oldX-(side==0?1402:1646))<.002f&&math.abs(oldZ-676)<.002f)||
                        (math.abs(oldX-x)<.002f&&math.abs(oldZ-z)<.002f),"Unexpected endpoint revision; do not overwrite");
                    Require(layout.TryProjectLocalToMap(x,z,out float px,out float pz)&&
                        math.abs(px-x)<.002f&&math.abs(pz-z)<.002f,"Reviewed source-aligned local frame required");
                    float u=(x-layout.OriginX)/layout.WorldWidthMetres,v=(z-layout.OriginZ)/layout.WorldDepthMetres;
                    Require(u>=0&&u<=1&&v>=0&&v<=1,"Logical endpoint outside existing envelope");
                    anchor["WorldX"]=x;anchor["WorldZ"]=z;anchor["NormalizedU"]=u;anchor["NormalizedV"]=v;
                    changed.Add(new JObject{["anchorId"]=id,["oldWorld"]=new JArray(oldX,oldZ),
                        ["newWorld"]=new JArray(x,z),["cell"]=new JArray(side==0?641:927,416)});
                }
            }
            // Validate a disposable trial before changing the caller's disposable layout.
            var trial=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            JObject policy;
            try
            {
                JsonUtility.FromJsonOverwrite(before.ToString(Newtonsoft.Json.Formatting.None),trial);
                JsonUtility.FromJsonOverwrite(new JObject{["anchors"]=anchors}.ToString(Newtonsoft.Json.Formatting.None),trial);
                Require(TryPreflightAuthoredRouteEndpoints(trial,grid,out policy),"Revised endpoint policy must admit before native queries");
            }
            finally{UnityEngine.Object.DestroyImmediate(trial);}
            JsonUtility.FromJsonOverwrite(new JObject{["anchors"]=anchors}.ToString(Newtonsoft.Json.Formatting.None),layout);
            Require(TryPreflightAuthoredRouteEndpoints(layout,grid,out policy),"Actual revised layout preflight required");
            return new JObject{["scope"]="SixActualTypedLogicalEndpointsOnly_NotNativeFit",
                ["changes"]=changed,["endpointPolicy"]=policy,["padsUnchanged"]=true,
                ["geometryProved"]=false,["sourceFitCertified"]=false,["missionReady"]=false,
                ["laterHullRequirement"]="Every later actual layout must pass shared endpoint policy again; no pad resize or recenter waiver"};
        }


        public static void RunMpGroundBaEndpointAdmissionControls()
        {
            Require(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode only");
            const string sourcePath="Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset";
            Require(Hash(sourcePath)=="181418e5b316d8a5625913d0251e5f5a9a83ab81f56baec49e4adcb2bb89e3b4"&&
                Hash("Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Grid.asset")=="6ad20b46d11a34488ecec7e656fa3dcf2eb39b4c55c10d6583099cf01213861c"&&
                Hash("Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Surface.asset")=="219420a7ab9069ffbda9d48409a665b4b819853b192a3a553e85588250b67fa0",
                "Reviewed existing physical source pins required");
            var source=UnityEditor.AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            Require(source!=null&&!UnityEditor.EditorUtility.IsDirty(source),"Clean immutable source required");
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string matrixError),matrixError);
            var grid=new GridConfig{Width=1280,Height=520,CellSize=1,Origin=new float3(740,0,260)};
            int controls=0;
            void Check(bool ok,string message){Require(ok,message);controls++;}
            foreach(string id in new[]{"S049","S050"})
            {
                var mission=GroundBa10Rows().Single(m=>(string)m["catalog_id"]==id);
                Require(TryCreateGroundBa10Candidate(mission,source,out var owner,out string failure),failure);
                using(owner)
                {
                    var definition=owner.Definition;var layout=owner.Layout;
                    var old=JObject.Parse(JsonUtility.ToJson(layout));
                    var budgets=new List<SkirmishStaticAuthoringBudget>();
                    foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                    {
                        Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,SkirmishDifficultyId.Regular,size,1,
                            new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                            budget.ExactCanonicalQuantities,"Original exact canonical budget required: "+string.Join(";",reasons));
                        budgets.Add(budget);
                    }
                    Check(!TryPreflightAuthoredRouteEndpoints(layout,grid,out _),"Original outside-zone endpoints must refuse");
                    ApplyMpPilotEndpointRevision(definition,grid);
                    Check(TryPreflightAuthoredRouteEndpoints(layout,grid,out _),"Revised actual endpoints must admit policy only");
                    var revised=JObject.Parse(JsonUtility.ToJson(layout));
                    Check(JToken.DeepEquals(old["pads"],revised["pads"])&&JToken.DeepEquals(old["routes"],revised["routes"]),
                        "Pads, dimensions, route identities, interiors and provisional metadata must remain fixed");
                    var endpointIds=new HashSet<string>(Enumerable.Range(0,3).SelectMany(r=>new[]{
                        "anchor.pilot.approach."+r+".0","anchor.pilot.approach."+r+".3"}));
                    var oldAnchors=((JArray)old["anchors"]).OfType<JObject>().ToDictionary(a=>(string)a["AnchorId"]);
                    var newAnchors=((JArray)revised["anchors"]).OfType<JObject>().ToArray();
                    Check(newAnchors.Length==oldAnchors.Count&&newAnchors.Count(a=>!JToken.DeepEquals(a,oldAnchors[(string)a["AnchorId"]]))==6&&
                        newAnchors.Where(a=>!endpointIds.Contains((string)a["AnchorId"])).All(a=>JToken.DeepEquals(a,oldAnchors[(string)a["AnchorId"]])),
                        "Exactly six endpoints may change; other authored anchors fixed");
                    string stable=JsonUtility.ToJson(layout);ApplyMpPilotEndpointRevision(definition,grid);
                    Check(JsonUtility.ToJson(layout)==stable,"Endpoint revision must be idempotent");
                    foreach(var before in budgets)
                    {
                        Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,SkirmishDifficultyId.Regular,before.Size,1,
                            new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var after,out var reasons),
                            "Revised actual typed compiler required: "+string.Join(";",reasons));
                        Check(after.ExactCanonicalQuantities&&JToken.DeepEquals(JToken.FromObject(before.Forces),JToken.FromObject(after.Forces))&&
                            JToken.DeepEquals(JToken.FromObject(before.Structures),JToken.FromObject(after.Structures)),
                            "Actual three-size canonical forces and structures must remain exact");
                    }
                    void Refused(Action action)
                    {
                        string snapshot=JsonUtility.ToJson(layout);bool refused=false;
                        try{action();}catch(InvalidOperationException){refused=true;}
                        Check(refused&&JsonUtility.ToJson(layout)==snapshot,"Bad endpoint candidate must refuse without further mutation");
                    }
                    var bad=(JObject)revised.DeepClone();
                    ((JArray)bad["anchors"]).OfType<JObject>().Single(a=>(string)a["AnchorId"]=="anchor.pilot.approach.0.0")["WorldX"]=1382.5f;
                    JsonUtility.FromJsonOverwrite(bad.ToString(Newtonsoft.Json.Formatting.None),layout);
                    Refused(()=>ApplyMpPilotEndpointRevision(definition,grid));
                    JsonUtility.FromJsonOverwrite(stable,layout);
                    var badGrid=grid;badGrid.CellSize=2;
                    Refused(()=>ApplyMpPilotEndpointRevision(definition,badGrid));
                    bad=(JObject)revised.DeepClone();((JObject)bad["routes"][0])["RouteId"]="route.unreviewed";
                    JsonUtility.FromJsonOverwrite(bad.ToString(Newtonsoft.Json.Formatting.None),layout);
                    Refused(()=>ApplyMpPilotEndpointRevision(definition,grid));
                    JsonUtility.FromJsonOverwrite(stable,layout);
                    string originalDefinition=JsonUtility.ToJson(definition);
                    try
                    {
                        JsonUtility.FromJsonOverwrite("{\"catalogId\":\"S051\"}",definition);
                        Refused(()=>ApplyMpPilotEndpointRevision(definition,grid));
                    }
                    finally{JsonUtility.FromJsonOverwrite(originalDefinition,definition);}
                }
            }
            Require(controls==24,"Exact MP admission control count required");
            Debug.Log("[MpGroundBaEndpointAdmissionControls] result=Passed controls=24 sizes=6 nativeQueries=0 sourceFitCertified=0 normalMissionReady=0");
        }


        public static void RunMpGroundBaZeroDistanceQueryControls()
        {
            Require(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode query-shape controls only");
            var footprint=new int2(3,3);var behavior=default(UnitMovementBehavior);
            var origin=new int2(17,23);var other=new int2(18,23);
            JObject OneCell(int2 from,int2 to,int2 pathCell)=>new JObject{
                ["status"]="NativeQueryFoundExactGoal",["failureCode"]=0,["nativeQueryFaction"]=1,
                ["footprintX"]=footprint.x,["footprintZ"]=footprint.y,
                ["exactInputKey"]=Ground30PathQueryKey(1,footprint,behavior,from,to),
                ["fromCell"]=new JArray(from.x,from.y),["toCell"]=new JArray(to.x,to.y),
                ["plannedFacilityBlockerScope"]="ExactAcceptedStaticCandidateRectangles_NotLiveDynamicOccupancy",
                ["pathCells"]=new JArray{new JArray(pathCell.x,pathCell.y)}};
            Require(MpPilotQueryMatches(OneCell(origin,origin,origin),1,footprint,behavior,origin,origin),
                "Exact zero-distance one-cell native result shape must admit");
            Require(!MpPilotQueryMatches(OneCell(origin,origin,other),1,footprint,behavior,origin,origin),
                "Zero-distance wrong returned endpoint must refuse");
            Require(!MpPilotQueryMatches(OneCell(origin,other,origin),1,footprint,behavior,origin,other),
                "Nonzero-distance one-cell native result shape must refuse");
            Debug.Log("[MpGroundBaZeroDistanceQueryControls] result=Passed controls=3 nativeQueries=0 sourceFitCertified=0 normalMissionReady=0");
        }


        // Spatial authority: VehicleFieldRepairService.TryContext uses distance squared<=144.
        // This adapter changes no runtime behavior and certifies static witnesses only.
        static bool MpPilotRepairWorldInRange(float2 point,float2 center)
            =>math.all(math.isfinite(center))&&VehicleFieldRepairService.IsWithinServiceRegion(new float3(point.x,0,point.y),center);
        static List<int2> MpPilotRepairRegionCells(GridConfig grid,float2 center)
        {
            Require(grid.CellSize==1&&math.all(math.isfinite(center)),"Pinned one-metre MP service grid required");
            var lo=GridUtils.WorldToCell(grid,new float3(center.x-12,0,center.y-12));
            var hi=GridUtils.WorldToCell(grid,new float3(center.x+12,0,center.y+12));
            var cells=new List<int2>();
            for(int z=math.max(0,lo.y);z<=math.min(grid.Height-1,hi.y);z++)
                for(int x=math.max(0,lo.x);x<=math.min(grid.Width-1,hi.x);x++)
                {
                    var c=new int2(x,z);
                    if(MpPilotRepairWorldInRange(GridUtils.CellToWorldCenter(grid,c).xz,center))cells.Add(c);
                }
            Require(cells.Count<=512,"Bounded MP repair-region enumeration exceeded reviewed diagnostic cap");
            return cells.OrderBy(c=>math.distancesq(GridUtils.CellToWorldCenter(grid,c).xz,center)).ThenBy(c=>c.y).ThenBy(c=>c.x).ToList();
        }
        // Diagnostic admission only. Raw source/grid/surface SHA and unique native grid
        // are already checked by CollectAll120Refinement before this disposable owner.
        // No coordinates, source selection, layout edits or publication credit here.
        static bool ApPairServiceAuthority(SkirmishScenarioDefinitionConfig definition)
        {
            if(definition==null||(definition.CatalogId!="S097"&&definition.CatalogId!="S098"))return false;
            var authority=GroundBa10Rows().Single(r=>(string)r["catalog_id"]==definition.CatalogId);
            Require((string)authority["tuple"]["map_id"]=="AP"&&(string)authority["tuple"]["army_profile"]=="G"&&
                (string)authority["tuple"]["objective_id"]=="BA","Exact AP Ground BA authority required");
            var pin=(JObject)GroundBaTemplate(authority)["source_candidate"];
            var source=definition.CampaignMap;var layout=definition.MapLayout;
            Require(source!=null&&layout!=null&&definition.MissionLogicalBinding==null&&
                definition.DefinitionId==(string)authority["definition_id"]&&
                definition.ScenarioSetupId==(string)authority["scenario_setup_id"]&&
                definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.BaseAssault&&definition.ArmyProfileConfig.ProfileId=="G"&&
                UnityEditor.AssetDatabase.GetAssetPath(source)==(string)pin["sourcePath"]&&
                source.OperationMapId==(string)pin["operationMapId"]&&source.SourceIdentityHash==(string)pin["sourceIdentityHash"]&&
                source.ContentHash==(string)pin["contentHash"]&&
                source.SourceSceneReference.AssetGUID==(string)pin["sourceSceneGuid"]&&
                source.NavigationMetadata.AuthoredSubSceneGuid==(string)pin["authoredSubSceneGuid"]&&
                layout.OperationMapId==source.OperationMapId&&layout.WorldBinding.SourceHash==source.ContentHash&&
                !layout.WorldBinding.HasMeasuredDeployments&&!layout.WorldBinding.BindAllConfigurations,
                "Exact unqualified AP disposable source/definition/layout authority required");
            return true;
        }
        static SkirmishRoleBindingConfig[] MpPilotExposedServiceRoles(SkirmishScenarioDefinitionConfig definition)
        {
            Require(((definition.CatalogId=="S049"||definition.CatalogId=="S050")&&definition.ArmyProfileConfig.ProfileId=="G")||ApPairServiceAuthority(definition)||CompleteGroundBaServiceAuthority(definition),
                "Exact MP or authority-bound AP Ground BA authoring required");
            var rows=definition.RoleCatalog.Roles.Where(r=>definition.ArmyProfileConfig.Allows(r.RoleId)&&
                (SkirmishRoleIds.Category(r.Kind)==SkirmishPopulationCategory.Ground||
                 SkirmishRoleIds.Category(r.Kind)==SkirmishPopulationCategory.LogisticsSupport)).ToArray();
            var expected=new[]{SkirmishRoleKind.Car,SkirmishRoleKind.ApcFast,SkirmishRoleKind.ApcArmored,
                SkirmishRoleKind.ApcHeavy,SkirmishRoleKind.Tank,SkirmishRoleKind.Radar,SkirmishRoleKind.Siege,
                SkirmishRoleKind.LogisticsTruck,SkirmishRoleKind.Tanker};
            Require(rows.Length==9&&rows.Select(r=>r.Kind).OrderBy(r=>(int)r).SequenceEqual(expected.OrderBy(r=>(int)r))&&
                rows.All(r=>r.Producer==SkirmishProducerKind.GroundStaging&&r.RuntimePrefabKey==SkirmishRoleCatalogConfig.RuntimePrefabKey(r.Kind)&&
                    r.RequiredReadiness==SkirmishProductionEligibility.RequiredReadiness(r.Kind)),
                "All nine actually exposed G ground profiles, including later paid readiness, required; no starting-roster-only reduction");
            return rows;
        }
        static bool MpPilotRepairPoseLegal(EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,
            Entity prefab,byte faction,int2 cell,NativeBitArray blockers=default)
        {
            if(!GridUtils.InBounds(cell,grid.Width,grid.Height))return false;
            var footprint=em.GetComponentData<UnitFootprint>(prefab);
            bool vehicle=UnitVehicleMovementUtility.IsVehicle(footprint,em.GetComponentData<UnitMovementBehavior>(prefab));
            return UnitPathPlacementValidation.CanPlaceForPathing(grid,em.GetBuffer<GridWalkable>(gridEntity,true).AsNativeArray(),
                blockers,default,default,default,default,default,default,Entity.Null,cell,footprint.Size,new int2(-100000,-100000),vehicle,true,faction)&&
                new Game.Runtime.Pathfinding.MapSurfaceTraversalValidation().CanTraverseFootprint(surface,1,grid,cell,footprint.Size,vehicle);
        }
        static bool MpPilotRepairInteractionFootprints(SkirmishScenarioDefinitionConfig definition,SkirmishMapLayoutConfig layout,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,byte faction,
            out List<RectInt> rectangles,JArray evidence,NativeBitArray blockers=default)
        {
            Require(definition.MapLayout==layout,"Exact disposable service layout owner required");
            Require(layout.Pads.Count(p=>p.FactionId==faction&&p.Kind==SkirmishLegalPadKind.Service)==1,"Unique actual Service pad required");
            Require(layout.TryGetPad(faction,SkirmishLegalPadKind.Service,out var pad),"Actual faction Service pad required");
            Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z),"Actual Service point projection required");
            var center=new float2(x,z);var candidates=MpPilotRepairRegionCells(grid,center);
            rectangles=new List<RectInt>();bool available=true;
            foreach(var binding in MpPilotExposedServiceRoles(definition))
            {
                var row=new JObject{["roleId"]=binding.RoleId,["faction"]=faction,["servicePadId"]=pad.PadId,
                    ["runtimePrefabKey"]=binding.RuntimePrefabKey,["requiredReadiness"]=binding.RequiredReadiness.ToString(),
                    ["serviceWorldXZ"]=new JArray(x,z),["allowedActorPivotRadiusSquared"]=144,
                    ["scope"]="ActualExposedGroundProfile_StaticWithinRuntime12m_NoPaidOrPathCertificate"};evidence.Add(row);
                var prefab=FindNativePrefab(em,binding.RuntimePrefabKey);
                if(prefab==Entity.Null||!em.HasComponent<UnitFootprint>(prefab)||!em.HasComponent<UnitMovementBehavior>(prefab)||
                    em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion==0||em.HasComponent<UnitAirMovement>(prefab))
                {row["status"]="UnavailableActualExposedGroundPrefab";available=false;continue;}
                bool found=false;var size=em.GetComponentData<UnitFootprint>(prefab).Size;
                foreach(var cell in candidates)
                {
                    if(!MpPilotRepairPoseLegal(em,gridEntity,grid,surface,prefab,faction,cell,blockers))continue;
                    var min=UnitFootprintUtility.GetMinCell(cell,size);var rect=new RectInt(min.x,min.y,size.x,size.y);
                    rectangles.Add(rect);var world=GridUtils.CellToWorldCenter(grid,cell);
                    row["witnessCell"]=new JArray(cell.x,cell.y);row["actorPivotWorldXZ"]=new JArray(world.x,world.z);
                    row["distanceSquared"]=math.distancesq(world.xz,center);row["reservedInteractionRect"]=RectJson(rect);
                    row["nativePrefabIndex"]=prefab.Index;row["nativePrefabVersion"]=prefab.Version;
                    row["footprintCells"]=new JArray(size.x,size.y);row["nativeStaticEndpointLegal"]=true;
                    row["insideRuntimeRepairRegion"]=true;row["status"]="StaticRuntimeRegionWitnessFound_LiveRepairAndProfileIngressPending";
                    found=true;break;
                }
                if(!found){row["status"]="Unresolved_NoLegalStaticPoseInEnumeratedRuntimeRegion";available=false;}
            }
            return available;
        }
        public static void RunMpGroundBaServiceRegionControls()
        {
            var zero=float2.zero;
            Require(MpPilotRepairWorldInRange(new float2(12,0),zero),"Inclusive runtime12m boundary must admit");
            Require(!MpPilotRepairWorldInRange(new float2(12.001f,0),zero),"Outside runtime radius must refuse");
            Require(!MpPilotRepairWorldInRange(new float2(9,9),zero),"Radius square corners must refuse");
            Require(!MpPilotRepairWorldInRange(new float2(float.NaN,0),zero)&&
                !MpPilotRepairWorldInRange(new float2(0,0),new float2(float.PositiveInfinity,0))&&
                !VehicleFieldRepairService.IsWithinServiceRegion(new float3(0,float.NaN,0),zero)&&
                !VehicleFieldRepairService.IsWithinServiceRegion(new float3(0,float.PositiveInfinity,0),zero),"Nonfinite actor XYZ and diagnostic center must refuse");
            Require(MpPilotRepairWorldInRange(new float2(6,0),zero),"Valid non-center pose must admit spatial policy");
            var grid=new GridConfig{Width=40,Height=40,CellSize=1,Origin=float3.zero};
            var cells=MpPilotRepairRegionCells(grid,new float2(20,20));
            Require(cells.Count>0&&cells.Count<=512&&cells.Distinct().Count()==cells.Count&&
                cells.All(c=>MpPilotRepairWorldInRange(GridUtils.CellToWorldCenter(grid,c).xz,new float2(20,20))),
                "Bounded unique cell-center region enumeration required");
            Debug.Log("[MpGroundBaServiceRegionControls] result=Passed controls=6 nativeQueries=0 sourceFitCertified=0 normalMissionReady=0");
        }

        // Generic Ground BA scope only. MP/S049 geometry remains in its dedicated entries.
        static JObject completeGroundBaAuthority;
        static JObject completeGroundBaSourcePins;
        static bool CompleteGroundBaTrue(JToken value)=>value?.Type==JTokenType.Boolean&&(bool)value;

        static void ValidateCompleteGroundBaAuthorityRow(JObject authority)
        {
            var tuple=authority?["tuple"] as JObject;var rows=authority?["canonical_size_rows"] as JArray;
            string id=(string)authority?["catalog_id"];
            Require(!string.IsNullOrEmpty(id)&&authority["protected_binding"]?.Type==JTokenType.Null&&
                !ProtectedRefinementCatalog(id)&&tuple!=null&&(string)tuple["army_profile"]=="G"&&
                (string)tuple["objective_id"]=="BA"&&new[]{"F","E"}.Contains((string)tuple["start_profile"])&&
                new[]{"DB","CC","MP","IB","AP"}.Contains((string)tuple["map_id"])&&
                (string)authority["definition_id"]=="skirmish."+id.ToLowerInvariant()&&
                (string)authority["scenario_setup_id"]=="scenario.skirmish."+id.ToLowerInvariant(),
                "Exact unprotected Ground BA tuple and saved identity required");
            Require(rows!=null&&rows.Count==3&&rows.OfType<JObject>().Count()==3&&
                rows.Select(r=>(string)r["size_id"]).SequenceEqual(new[]{"Standard","War","LargeWar"})&&
                rows.All(r=>(string)r["catalog_id"]==id&&(string)r["army_profile"]=="G"&&
                    (string)r["objective_id"]=="BA"&&(string)r["start_profile"]==(string)tuple["start_profile"])&&
                CanonicalBudgetHash(rows)==(string)authority["canonical_size_rows_sha256"],
                "Exact own three canonical string budgets required; no template quantities");
        }

        static bool CompleteGroundBaServiceAuthority(SkirmishScenarioDefinitionConfig definition)
        {
            if(completeGroundBaAuthority==null)return false;
            var authority=completeGroundBaAuthority;var pin=completeGroundBaSourcePins;
            Require(definition!=null&&pin!=null,"Owned Ground BA source scope required");
            ValidateCompleteGroundBaAuthorityRow(authority);
            var source=definition.CampaignMap;var layout=definition.MapLayout;
            var catalog=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            Require(catalog!=null&&UnityEditor.EditorUtility.IsPersistent(catalog)&&!UnityEditor.EditorUtility.IsDirty(catalog),
                "Actual clean persistent profile catalog required");
            var profiles=catalog.ToAuthoredSet();
            Require(definition.CatalogId==(string)authority["catalog_id"]&&
                definition.DefinitionId==(string)authority["definition_id"]&&
                definition.ScenarioSetupId==(string)authority["scenario_setup_id"]&&
                definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.BaseAssault&&
                definition.ArmyProfileConfig==profiles.ArmyGround&&definition.ArmyProfileConfig.ProfileId=="G"&&
                definition.StartPackageConfig==((string)authority["tuple"]["start_profile"]=="F"?profiles.StartField:profiles.StartEstablished)&&
                definition.StartPackageConfig.PackageId==(string)authority["tuple"]["start_profile"]&&
                definition.RoleCatalog==profiles.Roles&&definition.EconomyConfig==profiles.Economy&&definition.UpgradeCatalog==profiles.Upgrades&&definition.IntelConfig==profiles.Intel&&
                definition.RequiredFeatureIds!=null&&definition.RequiredFeatureIds.SequenceEqual(authority["required_capabilities"].ToObject<string[]>())&&
                source!=null&&layout!=null&&definition.MissionLogicalBinding==null&&
                !UnityEditor.EditorUtility.IsPersistent(definition)&&!UnityEditor.EditorUtility.IsPersistent(layout)&&
                UnityEditor.EditorUtility.IsPersistent(source)&&!UnityEditor.EditorUtility.IsDirty(source)&&
                UnityEditor.AssetDatabase.GetAssetPath(source)==(string)pin["sourcePath"]&&
                source.OperationMapId==(string)pin["operationMapId"]&&source.SourceIdentityHash==(string)pin["sourceIdentityHash"]&&
                source.ContentHash==(string)pin["contentHash"]&&source.SourceSceneReference.AssetGUID==(string)pin["sourceSceneGuid"]&&
                source.NavigationMetadata.AuthoredSubSceneGuid==(string)pin["authoredSubSceneGuid"]&&
                UnityEditor.AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid)==(string)pin["gridPath"]&&
                UnityEditor.AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID)==(string)pin["surfacePath"]&&
                Hash((string)pin["sourcePath"])==(string)pin["sourceAssetSha256"]&&
                Hash((string)pin["gridPath"])==(string)pin["gridAssetSha256"]&&
                Hash((string)pin["surfacePath"])==(string)pin["surfaceAssetSha256"]&&
                source.GridMetadata.CellSize==1&&layout.OperationMapId==source.OperationMapId&&
                layout.WorldBinding.SourceHash==source.ContentHash&&!layout.WorldBinding.HasMeasuredDeployments&&!layout.WorldBinding.BindAllConfigurations,
                "Exact caller-owned Ground BA source/grid/profile/binding required; no raw-hash normalization");
            return true;
        }

        static JObject CompleteGroundBaBudgetReceipt(SkirmishScenarioDefinitionConfig definition,SkirmishStaticAuthoringBudget budget,GridConfig grid)
        {
            Require(CompleteGroundBaServiceAuthority(definition)&&budget.CatalogId==definition.CatalogId&&budget.ExactCanonicalQuantities,
                "Actual exact canonical compiler budget and owned source required");
            var source=definition.CampaignMap;var expected=source.GridMetadata;
            Require(grid.Width==expected.Dimensions.x&&grid.Height==expected.Dimensions.y&&grid.CellSize==expected.CellSize&&
                math.all(grid.Origin==(float3)expected.Origin)&&math.all(math.isfinite(grid.Origin)),"Actual native grid must match own source metadata");
            var row=((JArray)completeGroundBaAuthority["canonical_size_rows"]).OfType<JObject>().Single(r=>(string)r["size_id"]==budget.Size.ToString());
            var exposed=MpPilotExposedServiceRoles(definition); // unchanged nine-profile producer/readiness contract
            return new JObject{["catalogId"]=definition.CatalogId,["definitionId"]=definition.DefinitionId,
                ["scenarioSetupId"]=definition.ScenarioSetupId,["group"]=completeGroundBaAuthority["tuple"]["map_id"].DeepClone(),
                ["armyProfile"]=definition.ArmyProfileConfig.ProfileId,["startProfile"]=definition.StartPackageConfig.PackageId,
                ["objective"]= "BA",["canonicalSizeRow"]=row.DeepClone(),["canonicalRowsSha256"]=CanonicalBudgetHash(completeGroundBaAuthority["canonical_size_rows"]),
                ["exactCanonicalQuantities"]=budget.ExactCanonicalQuantities,["budgetSignature"]=ApPreferenceBudgetKey(budget),
                ["sourcePins"]=completeGroundBaSourcePins.DeepClone(),["actualGridMatchesSource"]=true,
                ["exposedProfileKinds"]=new JArray(exposed.Select(r=>r.Kind.ToString())),
                ["sourceFitCertified"]=false,["normalMissionReady"]=false};
        }

        // Independent expected signatures: actual clean candidate + canonical compiler,
        // captured before the allocator runs, never reconstructed from its report.
        static JObject CompileCompleteGroundBaExpectedSignatures(JObject authority,JObject sourcePins)
        {
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)sourcePins["sourcePath"]);
            Require(TryCreateGroundBa10Candidate(authority,source,out var owner,out string error),error);
            using(owner)
            {
                Require(CompleteGroundBaServiceAuthority(owner.Definition),"Own canonical signature candidate required");
                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out error),error);
                var signatures=new JObject();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                        budget.ExactCanonicalQuantities,"Actual expected canonical compiler signature required: "+string.Join(";",reasons));
                    signatures[size.ToString()]=ApPreferenceBudgetKey(budget).DeepClone();
                }
                return signatures;
            }
        }

        static void ValidateCompleteGroundBaBudgetReceipt(JObject receipt,JObject authority,JObject sourcePins,string size,JObject expectedSignatures)
        {
            ValidateCompleteGroundBaAuthorityRow(authority);
            var own=((JArray)authority["canonical_size_rows"]).OfType<JObject>().Single(r=>(string)r["size_id"]==size);
            string id=(string)authority["catalog_id"];
            Require(receipt!=null&&(string)receipt["catalogId"]==id&&(string)receipt["definitionId"]==(string)authority["definition_id"]&&
                (string)receipt["scenarioSetupId"]==(string)authority["scenario_setup_id"]&&(string)receipt["group"]==(string)authority["tuple"]["map_id"]&&
                (string)receipt["armyProfile"]=="G"&&(string)receipt["startProfile"]==(string)authority["tuple"]["start_profile"]&&(string)receipt["objective"]=="BA"&&
                JToken.DeepEquals(receipt["canonicalSizeRow"],own)&&(string)receipt["canonicalRowsSha256"]==(string)authority["canonical_size_rows_sha256"]&&
                CompleteGroundBaTrue(receipt["exactCanonicalQuantities"])&&CompleteGroundBaTrue(receipt["actualGridMatchesSource"])&&
                JToken.DeepEquals(receipt["sourcePins"],sourcePins)&&receipt["sourceFitCertified"]?.Type==JTokenType.Boolean&&!(bool)receipt["sourceFitCertified"]&&
                receipt["normalMissionReady"]?.Type==JTokenType.Boolean&&!(bool)receipt["normalMissionReady"],"Own typed tuple/source/grid/budget receipt required");
            var kinds=receipt["exposedProfileKinds"] as JArray;
            var expected=new[]{"Car","ApcFast","ApcArmored","ApcHeavy","Tank","Radar","Siege","LogisticsTruck","Tanker"};
            Require(kinds!=null&&kinds.Count==9&&kinds.All(k=>k.Type==JTokenType.String)&&
                kinds.Select(k=>(string)k).OrderBy(k=>k,StringComparer.Ordinal).SequenceEqual(expected.OrderBy(k=>k,StringComparer.Ordinal)),
                "All nine exposed Ground profiles required, not only starting actors");
            var signature=receipt["budgetSignature"] as JObject;
            Require(signature!=null&&(string)signature["catalog"]==id&&(string)signature["definition"]==(string)authority["definition_id"]&&
                (string)signature["size"]==size&&signature["structures"] is JArray&&signature["forces"] is JArray,"Actual existing compiler signature required");
            var expectedSignature=expectedSignatures?[size] as JObject;
            Require(expectedSignatures!=null&&expectedSignatures.Properties().Count()==3&&expectedSignature!=null&&
                (string)expectedSignature["catalog"]==id&&(string)expectedSignature["definition"]==(string)authority["definition_id"]&&
                (string)expectedSignature["size"]==size&&JToken.DeepEquals(signature,expectedSignature),
                "Exact independently compiled force/structure signature required; totals alone confer no authority");
            var structures=((JArray)signature["structures"]).OfType<JObject>().ToArray();var forces=((JArray)signature["forces"]).OfType<JObject>().ToArray();
            Require(structures.Length==((JArray)signature["structures"]).Count&&forces.Length==((JArray)signature["forces"]).Count&&
                structures.All(s=>(int?)s["FactionId"]==1||(int?)s["FactionId"]==2)&&forces.All(f=>(int?)f["FactionId"]==1||(int?)f["FactionId"]==2),
                "Exact typed two-faction compiler rows required");
            foreach(int side in new[]{1,2})
            {
                string prefix=side==1?"player_":"enemy_";
                int actors=int.Parse((string)own[prefix+"combat"])+int.Parse((string)own[prefix+"logistics_support"])+int.Parse((string)own[prefix+"objective_trucks"]);
                Require(structures.Count(s=>(int)s["FactionId"]==side)==int.Parse((string)own[prefix+"starting_structures"])&&
                    forces.Where(f=>(int)f["FactionId"]==side).All(f=>f["Quantity"]?.Type==JTokenType.Integer&&(int)f["Quantity"]>0)&&
                    forces.Where(f=>(int)f["FactionId"]==side).Sum(f=>(int)f["Quantity"])==actors,"Exact own facilities and actor totals required for each faction");
            }
        }

        static void ValidateCompleteGroundBaReport(JObject report,JObject authority,JObject sourcePins,JObject expectedSignatures)
        {
            string id=(string)authority["catalog_id"];var pass=RequireCompleteGroundBaStaticPass(report,id);
            Require(pass["discoveryOnly"]?.Type==JTokenType.Boolean&&!(bool)pass["discoveryOnly"],"Non-discovery pass required");
            var measured=(JObject)report["missions"][0]["measurement"];
            Require((string)measured["catalogId"]==id&&JToken.DeepEquals(measured["sourcePins"],sourcePins),"Actual own measured source pins required");
            foreach(JObject row in (JArray)pass["sizes"])
            {
                Require((string)row["catalogId"]==id&&(string)row["group"]==(string)authority["tuple"]["map_id"],"No cross-catalog or source-group size splice");
                ValidateCompleteGroundBaBudgetReceipt((JObject)row["completeGroundBaAuthority"],authority,sourcePins,(string)row["size"],expectedSignatures);
            }
        }

        public static void RunGroundBaCompleteStaticPilot()
        {
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require(completeGroundBaAuthority==null&&completeGroundBaSourcePins==null&&!mpGroundBaPilotRequested&&!s049BoundedCompoundRouteRequested&&
                !strictLogicalCandidatesRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay&&!refinementJointPadSearchRequested&&
                !refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested&&!apGroundJointPreferencesRequested&&
                !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,"Idle unowned Ground BA authoring scope required");
            if(id=="S049"||id=="S050"){RunMpGroundBaCompleteStaticPilot();return;}
            var authority=GroundBa10Rows().Single(r=>(string)r["catalog_id"]==id);
            ValidateCompleteGroundBaAuthorityRow(authority);
            var pin=(JObject)GroundBaTemplate(authority)["source_candidate"].DeepClone();
            completeGroundBaAuthority=(JObject)authority.DeepClone();completeGroundBaSourcePins=pin;
            JObject expectedSignatures;
            try
            {
                expectedSignatures=CompileCompleteGroundBaExpectedSignatures(authority,pin);
                RunAll120BoundedFacilityPreferences();
            }
            finally{completeGroundBaAuthority=null;completeGroundBaSourcePins=null;}
            var report=JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT")));
            ValidateCompleteGroundBaReport(report,authority,pin,expectedSignatures);
            Debug.Log("[GroundBaCompleteStaticPilot] result=Passed catalog="+id+" group="+(string)authority["tuple"]["map_id"]+" sizes=3 sourceFitCertified=0 normalMissionReady=0");
        }

        public static void RunCompleteGroundBaTupleControls()
        {
            // Synthetic JSON controls only: no source world, native query or fit certificate.
            var authority=(JObject)GroundBa10Rows().Single(r=>(string)r["catalog_id"]=="S026").DeepClone();
            var pins=new JObject{["controlFixtureOnly"]=true};int controls=0;
            ValidateCompleteGroundBaAuthorityRow(authority);controls++;
            var sizes=new JArray();var expectedSignatures=new JObject();
            foreach(JObject own in (JArray)authority["canonical_size_rows"])
            {
                var structures=new JArray();var forces=new JArray();
                foreach(int side in new[]{1,2})
                {
                    string prefix=side==1?"player_":"enemy_";
                    for(int i=0;i<int.Parse((string)own[prefix+"starting_structures"]);i++)structures.Add(new JObject{["FactionId"]=side,["StructureId"]="fixture.structure."+i,["SpawnAnchorId"]="fixture.anchor."+side,["ObjectiveRoleId"]="fixture.base",["DesignatedBase"]=i==0,["AcrossOffsetMetres"]=i,["ForwardOffsetMetres"]=0});
                    forces.Add(new JObject{["FactionId"]=side,["RoleId"]="fixture.rifle",["RoleKind"]=1,["RuntimePrefabKey"]="fixture.rifle.prefab",["SupplyCost"]=3,["SpawnAnchorId"]="fixture.anchor."+side,["ObjectiveRoleId"]="fixture.force",["Quantity"]=int.Parse((string)own[prefix+"combat"])+int.Parse((string)own[prefix+"logistics_support"])+int.Parse((string)own[prefix+"objective_trucks"])});
                }
                var receipt=new JObject{["catalogId"]="S026",["definitionId"]="skirmish.s026",["scenarioSetupId"]="scenario.skirmish.s026",["group"]="CC",
                    ["armyProfile"]="G",["startProfile"]="E",["objective"]="BA",["canonicalSizeRow"]=own.DeepClone(),
                    ["canonicalRowsSha256"]=authority["canonical_size_rows_sha256"].DeepClone(),["exactCanonicalQuantities"]=true,
                    ["budgetSignature"]=new JObject{["catalog"]="S026",["definition"]="skirmish.s026",["size"]=own["size_id"].DeepClone(),["structures"]=structures,["forces"]=forces},
                    ["sourcePins"]=pins.DeepClone(),["actualGridMatchesSource"]=true,["exposedProfileKinds"]=new JArray("Car","ApcFast","ApcArmored","ApcHeavy","Tank","Radar","Siege","LogisticsTruck","Tanker"),
                    ["sourceFitCertified"]=false,["normalMissionReady"]=false};
                expectedSignatures[(string)own["size_id"]]=receipt["budgetSignature"].DeepClone();
                sizes.Add(new JObject{["catalogId"]="S026",["group"]="CC",["size"]=own["size_id"].DeepClone(),["completeFacilities"]=true,["allActorsInsidePads"]=true,
                    ["mpPilotCanonicalRouteCoverage"]=true,["actualProfileRouteIndependence"]=new JObject{["allProfilesIndependent"]=true},
                    ["mpPilotServiceAccess"]=new JObject{["allMembersHaveExactServiceIngressAndReturn"]=true},["completeGroundBaAuthority"]=receipt});
            }
            var report=new JObject{["cleanupVerified"]=true,["sourcePinsRecheckedAfterMeasurement"]=true,["missions"]=new JArray(new JObject{["catalogId"]="S026",
                ["measurement"]=new JObject{["catalogId"]="S026",["sourcePins"]=pins.DeepClone(),["strictThreeSizePlacementPass"]=0,["normalThreeSizeCompilerPassed"]=true,
                    ["passes"]=new JArray(new JObject{["pass"]=0,["discoveryOnly"]=false,["sizes"]=sizes})}})};
            ValidateCompleteGroundBaReport(report,authority,pins,expectedSignatures);controls++;
            void Refuse(Action action){bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}Require(refused,"Malformed tuple/strict receipt must fail closed");controls++;}
            void Authority(Action<JObject> change){var a=(JObject)authority.DeepClone();change(a);Refuse(()=>ValidateCompleteGroundBaAuthorityRow(a));}
            void Report(Action<JObject> change){var r=(JObject)report.DeepClone();change(r);Refuse(()=>ValidateCompleteGroundBaReport(r,authority,pins,expectedSignatures));}
            JObject Row(JObject r)=>(JObject)r["missions"][0]["measurement"]["passes"][0]["sizes"][0];
            JObject Receipt(JObject r)=>(JObject)Row(r)["completeGroundBaAuthority"];
            Authority(a=>a["tuple"]["army_profile"]="C");
            Authority(a=>a["tuple"]["objective_id"]="FC");
            Authority(a=>a["tuple"]["start_profile"]="unknown");
            Authority(a=>a["tuple"]["map_id"]="foreign");
            Authority(a=>a["protected_binding"]=new JObject());
            Authority(a=>a["definition_id"]="foreign");
            Authority(a=>((JArray)a["canonical_size_rows"]).RemoveAt(0));
            Authority(a=>a["canonical_size_rows"][0]["size_id"]="War");
            Authority(a=>a["canonical_size_rows"][0]["catalog_id"]="S050");
            Authority(a=>a["canonical_size_rows"][0]["materials_each"]="901");
            Report(r=>r["cleanupVerified"]=false);
            Report(r=>r["missions"][0]["measurement"]["normalThreeSizeCompilerPassed"]=false);
            Report(r=>((JArray)r["missions"][0]["measurement"]["passes"][0]["sizes"]).RemoveAt(0));
            Report(r=>r["missions"][0]["measurement"]["passes"][0]["discoveryOnly"]=true);
            Report(r=>Row(r)["catalogId"]="S050");
            Report(r=>Row(r)["group"]="MP");
            Report(r=>r["missions"][0]["measurement"]["sourcePins"]["foreign"]=true);
            Report(r=>Receipt(r)["canonicalSizeRow"]["materials_each"]="901");
            Report(r=>Receipt(r)["exactCanonicalQuantities"]="true");
            Report(r=>Receipt(r)["actualGridMatchesSource"]=false);
            Report(r=>((JArray)Receipt(r)["exposedProfileKinds"]).RemoveAt(0));
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["Quantity"]=1);
            Report(r=>((JArray)Receipt(r)["budgetSignature"]["structures"]).RemoveAt(0));
            Report(r=>Row(r)["allActorsInsidePads"]=false);
            Report(r=>Row(r)["mpPilotCanonicalRouteCoverage"]=false);
            Report(r=>Row(r)["actualProfileRouteIndependence"]["allProfilesIndependent"]=false);
            Report(r=>Row(r)["mpPilotServiceAccess"]["allMembersHaveExactServiceIngressAndReturn"]=false);
            Report(r=>Receipt(r)["sourceFitCertified"]=true);
            Report(r=>Receipt(r)["normalMissionReady"]=true);
            Report(r=>Row(r)["size"]="War");
            // Same-count swaps must reject even with exactCanonicalQuantities still true.
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["RoleId"]="fixture.tank");
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["RoleKind"]=99);
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["RuntimePrefabKey"]="foreign.prefab");
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["SupplyCost"]=999);
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["SpawnAnchorId"]="foreign.anchor");
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["ObjectiveRoleId"]="foreign.objective");
            Report(r=>Receipt(r)["budgetSignature"]["structures"][0]["StructureId"]="foreign.structure");
            Report(r=>Receipt(r)["budgetSignature"]["structures"][0]["SpawnAnchorId"]="foreign.anchor");
            Report(r=>Receipt(r)["budgetSignature"]["structures"][0]["ObjectiveRoleId"]="foreign.objective");
            Report(r=>Receipt(r)["budgetSignature"]["structures"][0]["DesignatedBase"]=false);
            Report(r=>Receipt(r)["budgetSignature"]["structures"][0]["AcrossOffsetMetres"]=999);
            Report(r=>Receipt(r)["budgetSignature"]["structures"][0]["ForwardOffsetMetres"]=999);
            Report(r=>((JObject)Receipt(r)["budgetSignature"]["forces"][0]).Remove("RoleId"));
            Report(r=>Receipt(r)["budgetSignature"]["forces"][0]["SupplyCost"]="3");
            Report(r=>Receipt(r)["budgetSignature"]["structures"][0]["FactionId"]=2);
            Require(controls==47,"Exact forty-seven tuple/receipt controls");
            Debug.Log("[CompleteGroundBaTupleControls] result=Passed controls=47 nativeQueries=0 sourceWorlds=0 sourceFitCertified=0");
        }

        static bool mpGroundBaPilotRequested;
        static bool s049BoundedCompoundRouteRequested;
        public static void RunMpGroundBaCompleteStaticPilot()
        {
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require((id=="S049"||id=="S050")&&!mpGroundBaPilotRequested,"One exact MP Ground BA pilot required");
            RunAuthoredRouteEndpointPreflightControls();
            RunMpGroundBaEndpointAdmissionControls();
            RunMpGroundBaZeroDistanceQueryControls();
            RunMpGroundBaServiceRegionControls();
            mpGroundBaPilotRequested=true;
            // Field has no Helipad/AirReturn facility hint; do not invent one.
            try { if(id=="S049")RunAll120SimultaneousLogicalRefinement();else RunAll120BoundedFacilityPreferences(); }
            finally { mpGroundBaPilotRequested=false; }
            ValidateMpGroundBaStaticPilotResult(id);
        }

        // S049-only logical compound search; unchanged source/frame/budgets.
        public static void RunS049BoundedCompoundStaticPilot()
        {
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require(id=="S049"&&!mpGroundBaPilotRequested&&!refinementCompoundSearchRequested&&
                !refinementJointPadSearchRequested&&!refinementLogicalFrameSearchRequested&&
                !refinementFacilityPreferencesRequested&&!refinementPreferenceReplay,
                "Exact idle unowned S049 bounded compound pilot required");
            Require(RefinementCompoundMargins.SequenceEqual(new[]{0f,16f,32f,64f}),
                "Existing four bounded compound candidates required");
            RunAuthoredRouteEndpointPreflightControls();
            RunMpGroundBaEndpointAdmissionControls();
            RunMpGroundBaZeroDistanceQueryControls();
            RunMpGroundBaServiceRegionControls();
            mpGroundBaPilotRequested=true;s049BoundedCompoundRouteRequested=true;
            try { RunAll120BoundedCompoundRefinement(); }
            finally { s049BoundedCompoundRouteRequested=false;mpGroundBaPilotRequested=false; }
            ValidateMpGroundBaStaticPilotResult(id);
            Debug.Log("[S049BoundedCompoundStaticPilot] result=Passed catalog=S049 sizes=3 compoundCandidates=4 sourceFrameChanged=0 sourceFitCertified=0 normalMissionReady=0");
        }

        // Dedicated strict MP experiment using the existing Campaign logical-frame policy.
        public static void RunS049BoundedLogicalFrameStaticPilot()
        {
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require(id=="S049"&&!mpGroundBaPilotRequested&&!s049BoundedCompoundRouteRequested&&
                !refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested&&
                !refinementJointPadSearchRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay,
                "Exact idle disposable S049 logical-frame pilot required");
            Require(RefinementLogicalFrameMargins.SequenceEqual(new[]{0f,32f,64f,128f}),
                "Existing finite logical-frame policy required; overlapping factions still refuse");
            RunAuthoredRouteEndpointPreflightControls();
            RunMpGroundBaEndpointAdmissionControls();
            RunMpGroundBaZeroDistanceQueryControls();
            RunMpGroundBaServiceRegionControls();
            mpGroundBaPilotRequested=true;s049BoundedCompoundRouteRequested=true;
            try { RunAll120BoundedLogicalFrameRefinement(); }
            finally { s049BoundedCompoundRouteRequested=false;mpGroundBaPilotRequested=false; }
            ValidateMpGroundBaStaticPilotResult(id);
            Debug.Log("[S049BoundedLogicalFrameStaticPilot] result=Passed catalog=S049 sizes=3 logicalFramePolicyMargins=4 overlapWaiver=0 physicalSourceChanged=0 sourceFitCertified=0 normalMissionReady=0");
        }

        static JObject ApplyS049NativeCorridorMainSegments(SkirmishScenarioDefinitionConfig definition,GridConfig grid)
        {
            var layout=definition.MapLayout;
            Require(s049BoundedCompoundRouteRequested&&mpGroundBaPilotRequested&&definition.CatalogId=="S049"&&
                !UnityEditor.EditorUtility.IsPersistent(definition)&&!UnityEditor.EditorUtility.IsPersistent(layout),
                "Only owned disposable S049 bounded candidate can receive logical segmentation");
            Require(grid.Width==1280&&grid.Height==520&&grid.CellSize==1&&grid.Origin.x==740&&grid.Origin.z==260&&
                definition.CampaignMap.OperationMapId=="opmap.ch04.armor_break"&&layout.WorldBinding.SourceHash==definition.CampaignMap.ContentHash,
                "Exact current ArmorBreak source/frame required");
            var before=JObject.Parse(JsonUtility.ToJson(layout));
            var anchors=(JArray)before["anchors"].DeepClone();var routes=(JArray)before["routes"].DeepClone();
            var main=routes.OfType<JObject>().Single(r=>(string)r["RouteId"]=="route.pilot.0");
            string[] oldIds=((JArray)main["WaypointAnchorIds"]).Values<string>().ToArray();
            Require(oldIds.SequenceEqual(new[]{"anchor.pilot.approach.0.0","anchor.pilot.approach.0.1",
                "anchor.pilot.approach.0.2","anchor.pilot.approach.0.3"}),"Exact unchanged original main topology required");
            JObject Anchor(string id)=>anchors.OfType<JObject>().Single(a=>(string)a["AnchorId"]==id);
            int2 Cell(string id)
            {
                var a=Anchor(id);Require(layout.TryProjectLocalToMap((float)a["WorldX"],(float)a["WorldZ"],out float x,out float z),
                    "Original typed source projection required");
                return GridUtils.WorldToCell(grid,new float3(x,0,z));
            }
            Require(Cell(oldIds[1]).Equals(new int2(686,416))&&Cell(oldIds[2]).Equals(new int2(882,416)),
                "Original measured central control identity required");
            string[] added={"anchor.pilot.s049.main.nativecorridor.a","anchor.pilot.s049.main.nativecorridor.b"};
            int2[] cells={new int2(698,410),new int2(876,410)};
            for(int i=0;i<2;i++)
            {
                Require(!anchors.OfType<JObject>().Any(a=>(string)a["AnchorId"]==added[i]),"Fresh named typed steering anchor required");
                float x=grid.Origin.x+(cells[i].x+.5f)*grid.CellSize,z=grid.Origin.z+(cells[i].y+.5f)*grid.CellSize;
                float u=(x-layout.OriginX)/layout.WorldWidthMetres,v=(z-layout.OriginZ)/layout.WorldDepthMetres;
                Require(u>=0&&u<=1&&v>=0&&v<=1&&layout.TryProjectLocalToMap(x,z,out float px,out float pz)&&
                    math.abs(px-x)<.002f&&math.abs(pz-z)<.002f,"Candidate cell must remain in the exact existing logical source frame");
                anchors.Add(new JObject{["AnchorId"]=added[i],["RoleId"]="route.steer.s049.main."+i,
                    ["WorldX"]=x,["WorldZ"]=z,["NormalizedU"]=u,["NormalizedV"]=v});
            }
            var ids=new[]{oldIds[0],oldIds[1],added[0],added[1],oldIds[2],oldIds[3]};
            main["WaypointAnchorIds"]=new JArray(ids);
            float length=0,oldLength=(float)main["LengthMetres"];
            for(int i=1;i<ids.Length;i++)
            {
                var a=Anchor(ids[i-1]);var b=Anchor(ids[i]);float x=(float)b["WorldX"]-(float)a["WorldX"],z=(float)b["WorldZ"]-(float)a["WorldZ"];
                length+=(float)Math.Sqrt(x*x+z*z);
            }
            Require(float.IsFinite(oldLength)&&oldLength>0&&float.IsFinite(length)&&length>0,"Actual graph length and existing rates required");
            foreach(string metric in new[]{"InfantryTransitSeconds","GroundTransitSeconds","AirTransitSeconds","InfantryFirstContactSeconds","GroundFirstContactSeconds"})
            {
                float value=(float)main[metric];Require(float.IsFinite(value)&&value>=0,"Finite unchanged existing route rate required");
                main[metric]=value*(length/oldLength);
            }
            main["LengthMetres"]=length;main["ProvisionalTimes"]=true;
            var patch=new JObject{["anchors"]=anchors,["routes"]=routes};
            var trial=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite(before.ToString(Newtonsoft.Json.Formatting.None),trial);
                JsonUtility.FromJsonOverwrite(patch.ToString(Newtonsoft.Json.Formatting.None),trial);
                Require(TryPreflightAuthoredRouteEndpoints(trial,grid,out _),"Segmented actual route must retain exact shared endpoint admission");
                var after=JObject.Parse(JsonUtility.ToJson(trial));
                Require(JToken.DeepEquals(before["pads"],after["pads"])&&JToken.DeepEquals(before["worldBinding"],after["worldBinding"])&&
                    JToken.DeepEquals(before["routes"][1],after["routes"][1])&&JToken.DeepEquals(before["routes"][2],after["routes"][2]),
                    "Facilities/source frame/other routes cannot change during segmentation");
            }
            finally{UnityEngine.Object.DestroyImmediate(trial);}
            JsonUtility.FromJsonOverwrite(patch.ToString(Newtonsoft.Json.Formatting.None),layout);
            return new JObject{["scope"]="ActualDisposableMainRouteSegmentation_NotWalkabilityOrFit",
                ["routeId"]="route.pilot.0",["addedAnchorIds"]=new JArray(added),["lengthMetres"]=length,
                ["originalCentralFromCell"]=new JArray(686,416),["originalCentralToCell"]=new JArray(882,416),
                ["geometryProved"]=false,["sourceFrameChanged"]=false,["sourceFitCertified"]=false};
        }

        static JObject ProbeS049OriginalCentralLegControls(SkirmishStaticAuthoringBudget budget,EntityManager em,
            Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,JArray facilities)
        {
            // Diagnostic original leg only. Never enters route coverage/independence.
            using var blocked=new Unity.Collections.NativeBitArray(grid.Width*grid.Height,Allocator.Persistent,NativeArrayOptions.ClearMemory);
            Ground30OccupyFacilities(facilities,grid,blocked);
            var queries=new JArray();var refs=new JArray();var cache=new Dictionary<string,int>(StringComparer.Ordinal);
            int index=0;
            foreach(var force in budget.Forces)
            {
                int forceIndex=index++;var prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                Require(prefab!=Entity.Null,"Actual canonical original-leg prefab required");
                if(em.HasComponent<UnitAirMovement>(prefab))continue;
                Require(em.HasComponent<UnitFootprint>(prefab)&&em.HasComponent<UnitMovementBehavior>(prefab)&&
                    (force.FactionId==1||force.FactionId==2),"Actual ground profile/faction required");
                var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;var behavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                var from=force.FactionId==1?new int2(686,416):new int2(882,416);var to=force.FactionId==1?new int2(882,416):new int2(686,416);
                string key=Ground30PathQueryKey(force.FactionId,footprint,behavior,from,to);
                if(!cache.TryGetValue(key,out int queryId))
                {
                    Require(queries.Count<64,"Bounded original-leg diagnostic context cap");
                    queryId=queries.Count;cache.Add(key,queryId);
                    var q=new JObject{["queryId"]=queryId,["exactInputKey"]=key,["fromCell"]=new JArray(from.x,from.y),["toCell"]=new JArray(to.x,to.y)};
                    QueryPath(em,gridEntity,grid,surface,prefab,from,to,q,force.FactionId,blocked);queries.Add(q);
                }
                refs.Add(new JObject{["forceIndex"]=forceIndex,["faction"]=force.FactionId,["roleId"]=force.RoleId,
                    ["objectiveRoleId"]=force.ObjectiveRoleId,["nativePrefabIndex"]=prefab.Index,["nativePrefabVersion"]=prefab.Version,
                    ["nativeQueryId"]=queryId,["kind"]="DiagnosticOriginalUnsegmentedCentralLeg"});
            }
            return new JObject{["scope"]="OriginalCentralLegDiagnostic_SameActualFacilityBlockers_NotAuthoredRouteFit",
                ["queries"]=queries,["roleReferences"]=refs,["size"]=budget.Size.ToString(),
                ["entersCoverageOrIndependenceGate"]=false,["sourceFitCertified"]=false};
        }

        static JObject RequireCompleteGroundBaStaticPass(JObject report,string id)
        {
            Require((bool?)report["cleanupVerified"]==true&&(bool?)report["sourcePinsRecheckedAfterMeasurement"]==true,"Source cleanup and physical pins must pass");
            var missions=(JArray)report["missions"];Require(missions.Count==1&&(string)missions[0]["catalogId"]==id,"Exact one mission receipt required");
            var measured=(JObject)missions[0]["measurement"];
            Require(measured["strictThreeSizePlacementPass"]!=null&&measured["logicalFinalizationUnresolved"]==null&&
                (bool?)measured["normalThreeSizeCompilerPassed"]==true,"Complete three-size static replay and logical compiler required");
            var pass=((JArray)measured["passes"]).OfType<JObject>().Single(r=>(int)r["pass"]==(int)measured["strictThreeSizePlacementPass"]);
            var sizes=((JArray)pass["sizes"]).OfType<JObject>().ToArray();
            Require(sizes.Select(r=>(string)r["size"]).OrderBy(v=>v).SequenceEqual(new[]{"LargeWar","Standard","War"})&&
                sizes.All(r=>(bool?)r["completeFacilities"]==true&&(bool?)r["allActorsInsidePads"]==true&&
                    (bool?)r["mpPilotCanonicalRouteCoverage"]==true&&(bool?)(r["actualProfileRouteIndependence"]?["allProfilesIndependent"])==true&&
                    (bool?)(r["mpPilotServiceAccess"]?["allMembersHaveExactServiceIngressAndReturn"])==true),"Exact non-discovery facilities/actors/routes/service/corridors required for all sizes");
            return pass;
        }

        static void ValidateMpGroundBaStaticPilotResult(string id)
        {
            string path=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT");
            var report=JObject.Parse(File.ReadAllText(path));
            RequireCompleteGroundBaStaticPass(report,id);
            Debug.Log("[MpGroundBaCompleteStaticPilot] result=Passed catalog="+id+" sizes=3 sourceFitCertified=0 normalMissionReady=0");
        }

        static bool MpPilotCanonicalRouteCoverage(SkirmishStaticAuthoringBudget budget,SkirmishMapLayoutConfig layout,
            EntityManager em,GridConfig grid,JObject routes)
        {
            if(layout.Routes.Length!=3||layout.Routes.Select(r=>r.RouteId).Distinct().Count()!=3||!JointRoutesExact(routes))return false;
            var references=((JArray)routes["roleReferences"]).OfType<JObject>().ToArray();var queries=(JArray)routes["queries"];
            int expected=0;var used=new HashSet<int>();
            for(int index=0;index<budget.Forces.Length;index++)
            {
                var force=budget.Forces[index];var prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                if(prefab==Entity.Null||!em.HasComponent<UnitFootprint>(prefab)||!em.HasComponent<UnitMovementBehavior>(prefab)||em.HasComponent<UnitAirMovement>(prefab))return false;
                var behavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;
                foreach(var route in layout.Routes)
                {
                    if(behavior.UsesVehicleMotion!=0&&!route.AllowsVehicles)return false;
                    var anchors=force.FactionId==2?route.WaypointAnchorIds.Reverse().ToArray():route.WaypointAnchorIds;
                    for(int leg=1;leg<anchors.Length;leg++)
                    {
                        expected++;
                        var matches=references.Where(r=>(int?)r["forceIndex"]==index&&(int?)r["faction"]==force.FactionId&&
                            (string)r["roleId"]==force.RoleId&&(string)r["objectiveRoleId"]==force.ObjectiveRoleId&&
                            (string)r["routeId"]==route.RouteId&&(int?)r["leg"]==leg&&(int?)r["nativePrefabIndex"]==prefab.Index&&
                            (int?)r["nativePrefabVersion"]==prefab.Version&&r["nativeQueryId"]!=null).ToArray();
                        if(matches.Length!=1)return false;
                        int queryId=(int)matches[0]["nativeQueryId"];if(queryId<0||queryId>=queries.Count)return false;
                        if(!layout.TryGetAnchorById(anchors[leg-1],out var a)||!layout.TryGetAnchorById(anchors[leg],out var b)||
                            !layout.TryProjectLocalToMap(a.WorldX,a.WorldZ,out float ax,out float az)||
                            !layout.TryProjectLocalToMap(b.WorldX,b.WorldZ,out float bx,out float bz))return false;
                        var from=GridUtils.WorldToCell(grid,new float3(ax,0,az));var to=GridUtils.WorldToCell(grid,new float3(bx,0,bz));
                        if(!MpPilotQueryMatches((JObject)queries[queryId],force.FactionId,footprint,behavior,from,to))return false;
                        used.Add(queryId);
                    }
                }
            }
            return expected>0&&references.Length==expected&&used.Count==queries.Count;
        }

        static bool MpPilotCellMatches(JToken value,int2 cell)=>value is JArray a&&a.Count==2&&(int?)a[0]==cell.x&&(int?)a[1]==cell.y;
        static bool MpPilotQueryMatches(JObject query,byte faction,int2 footprint,UnitMovementBehavior behavior,int2 from,int2 to)
        {
            if(query==null||(string)query["status"]!="NativeQueryFoundExactGoal"||(int?)query["failureCode"]!=0||
                (int?)query["nativeQueryFaction"]!=faction||(int?)query["footprintX"]!=footprint.x||(int?)query["footprintZ"]!=footprint.y||
                (string)query["exactInputKey"]!=Ground30PathQueryKey(faction,footprint,behavior,from,to)||
                !MpPilotCellMatches(query["fromCell"],from)||!MpPilotCellMatches(query["toCell"],to)||
                (string)query["plannedFacilityBlockerScope"]!="ExactAcceptedStaticCandidateRectangles_NotLiveDynamicOccupancy")return false;
            return query["pathCells"] is JArray path&&path.Count>=(from.Equals(to)?1:2)&&MpPilotCellMatches(path[0],from)&&MpPilotCellMatches(path[path.Count-1],to);
        }


        static JObject MpPilotServiceAccess(SkirmishStaticAuthoringBudget budget,SkirmishScenarioDefinitionConfig definition,
            SkirmishMapLayoutConfig layout,EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,JArray facilities,JArray packed)
        {
            const int maximumPoseAttemptsPerMember=8; // diagnostic work cap; native path budgets unchanged
            var queries=new JArray();var membersProof=new JArray();var futureProfiles=new JArray();var supportRows=new JArray();
            var result=new JObject{["scope"]="ActualVehicleMembers_ExactIngressAndReturn_WithinRuntime12m_SameCompleteFacilities_StaticOnly",
                ["queries"]=queries,["memberProofs"]=membersProof,["exposedProfileFootprintWitnesses"]=futureProfiles,
                ["supportGroundStagingStaticReceipts"]=supportRows,["normalPaidRepairCertified"]=false,
                ["stoppedNoThreatEconomyAndRuntimeOwnershipProved"]=false,["liveOccupancyIncluded"]=false,
                ["allExposedProfileIngressCertified"]=false,["sourceFitCertified"]=false,
                ["maximumPoseAttemptsPerMember"]=maximumPoseAttemptsPerMember,["allowedActorPivotRadiusSquared"]=144};
            Require(facilities.Count==budget.Structures.Length&&facilities.OfType<JObject>().All(f=>(bool?)f["entireConfigurationFound"]==true),
                "Same exact complete canonical facility candidate required");
            using var blockers=new NativeBitArray(grid.Width*grid.Height,Allocator.Persistent,NativeArrayOptions.ClearMemory);
            Ground30OccupyFacilities(facilities,grid,blockers);
            foreach(byte faction in new byte[]{1,2})
            {
                Require(budget.Structures.Count(s=>s.FactionId==faction&&s.StructureId==SkirmishStructureIds.GroundStaging)==1,
                    "One actual canonical GroundStaging support per faction required");
                var support=facilities.OfType<JObject>().Where(f=>(byte?)f["faction"]==faction&&
                    (string)f["catalogId"]==budget.CatalogId&&(string)f["size"]==budget.Size.ToString()&&
                    (string)f["structureId"]==SkirmishStructureIds.GroundStaging).ToArray();
                Require(support.Length==1&&support[0]["resolvedEffectiveRect"] is JObject&&
                    support[0]["resolvedWorldCenterXZ"] is JArray&&support[0]["prefabPath"]!=null&&
                    Hash((string)support[0]["prefabPath"])==(string)support[0]["prefabSha256"],
                    "Exact accepted GroundStaging catalog/size/faction/structure/prefab/resolved-pose join required");
                supportRows.Add(support[0].DeepClone());
                var witness=new JArray();
                bool available=MpPilotRepairInteractionFootprints(definition,layout,em,gridEntity,grid,surface,faction,out _,witness,blockers);
                foreach(var row in witness)futureProfiles.Add(row);
                if(!available)
                {
                    result["allMembersHaveExactServiceIngressAndReturn"]=false;
                    result["status"]="Unresolved_ExposedProfileHasNoLegalRuntimeRegionFootprint_WithSameFacilities";
                    return result;
                }
            }
            Require(futureProfiles.Count==18,"All nine exposed ground profiles for both factions required at every size");
            result["allExposedProfileFootprintsLegalWithSameFacilities"]=true;
            int expectedMembers=0;
            for(int index=0;index<budget.Forces.Length;index++)
            {
                var force=budget.Forces[index];var prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                Require(prefab!=Entity.Null&&em.HasComponent<UnitMovementBehavior>(prefab),"Actual canonical movable role required");
                var behavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                if(behavior.UsesVehicleMotion==0||em.HasComponent<UnitAirMovement>(prefab))continue;
                Require(layout.TryGetPad(force.FactionId,SkirmishLegalPadKind.Service,out var pad),"Exact faction Service pad required");
                Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z),"Exact faction Service projection required");
                var center=new float2(x,z);var centerCell=GridUtils.WorldToCell(grid,new float3(x,0,z));
                var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;
                var packing=packed.OfType<JObject>().Single(r=>(int)r["forceIndex"]==index);
                Require((int?)packing["faction"]==force.FactionId&&(string)packing["roleId"]==force.RoleId&&
                    (int?)packing["nativePrefabEntityIndex"]==prefab.Index&&(int?)packing["nativePrefabEntityVersion"]==prefab.Version&&
                    (int?)packing["footprintX"]==footprint.x&&(int?)packing["footprintZ"]==footprint.y,"Exact canonical packing/prefab join required");
                var members=((JArray)packing["candidateCells"]).OfType<JObject>().ToArray();
                Require(members.Length==force.Quantity&&members.All(m=>(bool?)m["normalFallbackAccepted"]==true&&
                    (bool?)m["insideActualTypedPad"]==true&&(bool?)m["surfaceTraversable"]==true)&&
                    members.Select(m=>(int)m["member"]).OrderBy(v=>v).SequenceEqual(Enumerable.Range(0,force.Quantity)),
                    "Every actual unique vehicle member must be legally packed");
                var witness=futureProfiles.OfType<JObject>().Single(r=>(int)r["faction"]==force.FactionId&&(string)r["roleId"]==force.RoleId);
                Require((int)witness["nativePrefabIndex"]==prefab.Index&&(int)witness["nativePrefabVersion"]==prefab.Version,
                    "Actual canonical member must match the exposed native profile");
                var reservedPose=new int2((int)witness["witnessCell"][0],(int)witness["witnessCell"][1]);
                var region=MpPilotRepairRegionCells(grid,center);
                foreach(var member in members)
                {
                    expectedMembers++;var origin=new int2((int)member["x"],(int)member["z"]);
                    Require(MpPilotRepairPoseLegal(em,gridEntity,grid,surface,prefab,force.FactionId,origin,blockers),
                        "Packed origin full footprint must remain source/legal under identical facilities, including zero-distance requests");
                    var proof=new JObject{["forceIndex"]=index,["member"]=member["member"].DeepClone(),["faction"]=force.FactionId,
                        ["roleId"]=force.RoleId,["servicePadId"]=pad.PadId,["originCell"]=new JArray(origin.x,origin.y),
                        ["nativePrefabIndex"]=prefab.Index,["nativePrefabVersion"]=prefab.Version,
                        ["memberKey"]=force.RoleId+"."+force.FactionId+"."+index+"."+(int)member["member"],
                        ["serviceWorldXZ"]=new JArray(x,z),["allowedActorPivotRadiusSquared"]=144,
                        ["exactCenterStaticLegalDiagnosticOnly"]=MpPilotRepairPoseLegal(em,gridEntity,grid,surface,prefab,force.FactionId,centerCell,blockers),
                        ["passed"]=false};membersProof.Add(proof);
                    var candidates=new List<int2>();
                    if(MpPilotRepairWorldInRange(GridUtils.CellToWorldCenter(grid,origin).xz,center))candidates.Add(origin);
                    candidates.Add(reservedPose);candidates.AddRange(region.OrderBy(c=>math.distancesq(GridUtils.CellToWorldCenter(grid,c),GridUtils.CellToWorldCenter(grid,origin))));
                    int attempts=0,staticRejected=0;
                    foreach(var pose in candidates.Distinct())
                    {
                        if(!MpPilotRepairWorldInRange(GridUtils.CellToWorldCenter(grid,pose).xz,center)||
                            !MpPilotRepairPoseLegal(em,gridEntity,grid,surface,prefab,force.FactionId,pose,blockers)){staticRejected++;continue;}
                        if(attempts==maximumPoseAttemptsPerMember)break;
                        attempts++;bool pairExact=true;var pairIds=new JArray();
                        for(int direction=0;direction<2;direction++)
                        {
                            var from=direction==0?origin:pose;var to=direction==0?pose:origin;
                            var query=new JObject{["forceIndex"]=index,["member"]=member["member"].DeepClone(),["faction"]=force.FactionId,
                                ["roleId"]=force.RoleId,["memberKey"]=proof["memberKey"].DeepClone(),["attempt"]=attempts,
                                ["direction"]=direction==0?"Ingress":"ReturnToPackedOrigin",["servicePadId"]=pad.PadId,
                                ["nativePrefabIndex"]=prefab.Index,["nativePrefabVersion"]=prefab.Version,
                                ["fromCell"]=new JArray(from.x,from.y),["toCell"]=new JArray(to.x,to.y),
                                ["exactInputKey"]=Ground30PathQueryKey(force.FactionId,footprint,behavior,from,to)};
                            pairIds.Add(queries.Count);queries.Add(query);
                            QueryPath(em,gridEntity,grid,surface,prefab,from,to,query,force.FactionId,blockers);
                            bool exact=MpPilotQueryMatches(query,force.FactionId,footprint,behavior,from,to);
                            query["actualProfileAndEndpointsMatch"]=exact;pairExact&=exact;
                        }
                        if(!pairExact)continue; // preserve failed attempts; never mix poses/directions
                        var world=GridUtils.CellToWorldCenter(grid,pose);
                        proof["passed"]=true;proof["selectedServiceCell"]=new JArray(pose.x,pose.y);
                        proof["selectedActorPivotWorldXZ"]=new JArray(world.x,world.z);
                        proof["distanceSquared"]=math.distancesq(world.xz,center);proof["exactPairQueryIds"]=pairIds;
                        break;
                    }
                    proof["poseAttempts"]=attempts;proof["staticCandidateRejections"]=staticRejected;
                    if((bool?)proof["passed"]!=true)proof["status"]="Unresolved_BoundedServicePoseSearchExhausted_NotSourceUnusable";
                }
            }
            result["expectedMemberProofs"]=expectedMembers;result["nativeRequestsExecuted"]=queries.Count;
            result["allAttemptRequestsExact"]=queries.Count>0&&queries.OfType<JObject>().All(q=>(bool?)q["actualProfileAndEndpointsMatch"]==true);
            result["allMembersHaveExactServiceIngressAndReturn"]=expectedMembers>0&&membersProof.Count==expectedMembers&&
                membersProof.OfType<JObject>().All(p=>(bool?)p["passed"]==true);
            result["status"]=(bool)result["allMembersHaveExactServiceIngressAndReturn"]
                ?"StaticRuntimeRegionMemberAccessProved_LiveRepairAndFutureProductionPathsPending"
                :"Unresolved_ActualMemberServiceAccess";
            return result;
        }

    }
}
#endif
