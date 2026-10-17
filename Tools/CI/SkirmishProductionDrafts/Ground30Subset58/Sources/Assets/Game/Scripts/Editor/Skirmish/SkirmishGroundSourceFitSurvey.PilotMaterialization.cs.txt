#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        const string MaterializedPilotConfig = "Design/AgentReports/SkirmishExpansion/GroundPilots/measured-pad-bounds-proposals-survey11-v2.json";
        const string MaterializedPilotConfigSha = "9303662a122caaa1256f67fc21eeb8bd684364958ecd5f42ae8753d626ef7634";
        static bool materializedPilotRequested;
        static int materializedPilotCollected,materializedPilotErrors,materializedPilotNegativeControls;

        // No reflection serializer may traverse Unity vector computed properties.
        static JObject EncodePilotAnchor(SkirmishLayoutAnchorConfig a)=>new JObject{["AnchorId"]=a.AnchorId,["RoleId"]=a.RoleId,["NormalizedU"]=a.NormalizedU,["NormalizedV"]=a.NormalizedV,["WorldX"]=a.WorldX,["WorldZ"]=a.WorldZ};
        static JObject EncodePilotPad(SkirmishLegalPadConfig p)=>new JObject{["PadId"]=p.PadId,["AnchorId"]=p.AnchorId,["RoleId"]=p.RoleId,["Kind"]=(int)p.Kind,["CenterX"]=p.CenterX,["CenterZ"]=p.CenterZ,["WidthMetres"]=p.WidthMetres,["DepthMetres"]=p.DepthMetres,["FactionId"]=p.FactionId};
        static JObject EncodePilotRoute(SkirmishLayoutRouteConfig p)=>new JObject{["RouteId"]=p.RouteId,["WaypointAnchorIds"]=new JArray(p.WaypointAnchorIds),["Kind"]=(int)p.Kind,
            ["LengthMetres"]=p.LengthMetres,["WidthMetres"]=p.WidthMetres,["InfantryTransitSeconds"]=p.InfantryTransitSeconds,["GroundTransitSeconds"]=p.GroundTransitSeconds,["AirTransitSeconds"]=p.AirTransitSeconds,
            ["InfantryFirstContactSeconds"]=p.InfantryFirstContactSeconds,["GroundFirstContactSeconds"]=p.GroundFirstContactSeconds,["ProvisionalTimes"]=p.ProvisionalTimes,["AllowsVehicles"]=p.AllowsVehicles,["AllowsAircraft"]=p.AllowsAircraft};
        static JObject EncodePilotFrame(SkirmishLayoutWorldBindingConfig p)=>new JObject{["OriginX"]=p.OriginX,["OriginZ"]=p.OriginZ,["ForwardX"]=p.ForwardX,["ForwardZ"]=p.ForwardZ,
            ["AcrossX"]=p.AcrossX,["AcrossZ"]=p.AcrossZ,["ForwardMetres"]=p.ForwardMetres,["AcrossMetres"]=p.AcrossMetres,["PlayerAnchorId"]=p.PlayerAnchorId,["EnemyAnchorId"]=p.EnemyAnchorId,
            ["SourceHash"]=p.SourceHash,["HasMeasuredDeployments"]=p.HasMeasuredDeployments,["PlayerDeployment"]=new JObject{["x"]=p.PlayerDeployment.x,["y"]=p.PlayerDeployment.y},
            ["EnemyDeployment"]=new JObject{["x"]=p.EnemyDeployment.x,["y"]=p.EnemyDeployment.y},["BindAllConfigurations"]=p.BindAllConfigurations};

        static string[] CaptureMaterializationWorldCensus()
        {
            // Unity's NoAllocReadOnlyCollection rejects IEnumerable boxing.
            // Pattern-based foreach uses its supported native enumerator.
            var values=new List<string>();
            foreach(var world in World.All)values.Add(world.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
            values.Sort(StringComparer.Ordinal);return values.ToArray();
        }

        public static void RunMaterializedPilotSerializationControls()
        {
            Require(!materializedPilotRequested&&!EditorApplication.isPlayingOrWillChangePlaymode,"Idle non-streaming schema validation required");
            Require(Hash(MaterializedPilotConfig)==MaterializedPilotConfigSha,"Exact immutable six-config JSON required");
            var input=JObject.Parse(File.ReadAllText(Inventory));var packet=JObject.Parse(File.ReadAllText(MaterializedPilotConfig));
            var worldBefore=CaptureMaterializationWorldCensus();var defaultBefore=World.DefaultGameObjectInjectionWorld;
            int scenesBefore=UnityEngine.SceneManagement.SceneManager.sceneCount,previewsBefore=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            int count=0;materializedPilotNegativeControls=0;
            foreach(var group in new[]{"CC","AP","MP","IB"})
            {
                var pin=(JObject)input["sources"][group];string path=(string)pin["path"];
                Require(Hash(path)==(string)pin["assetSha256"],"Actual fast-schema Campaign source pin changed");
                var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);Require(source!=null&&!EditorUtility.IsDirty(source),"Clean actual schema source required");
                using var owner=new PilotCandidateOwner();SkirmishMapLayoutConfig template;
                if(input["layoutSnapshots"]?[group] is JObject snapshot)
                {
                    string layoutPath=(string)snapshot["templatePath"];Require(Hash(layoutPath)==(string)snapshot["templateAssetSha256"],"Actual template pin changed");
                    template=AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
                    Require(template!=null&&!EditorUtility.IsDirty(template),"Clean pinned loaded schema template required");
                }
                else template=CreatePilotLogicalCandidate(source,group,new JObject {
                    ["group"]=group,["sourcePath"]=path,["sourceAssetSha256"]=Hash(path)
                },owner);
                Require(template!=null,"Actual schema template required");
                foreach(var candidate in ((JArray)packet["candidates"]).OfType<JObject>().Where(v=>(string)v["group"]==group))
                {
                    RunMaterializedSchemaControls(template,source,candidate);
                    var evidence=new JObject();var clone=MaterializePilotClone(template,source,candidate,evidence);
                    try
                    {
                        var readback=JObject.Parse(JsonUtility.ToJson(clone));
                        Require(readback["worldBinding"] is JObject&&readback["worldBinding"]["PlayerDeployment"]?["x"]!=null&&
                            readback["worldBinding"]["EnemyDeployment"]?["y"]!=null,"Actual typed vector scalar readback missing");
                        Require(clone.WorldBinding.SourceHash==source.ContentHash&&clone.WorldBinding.ForwardX==1&&clone.WorldBinding.ForwardZ==0&&
                            clone.WorldBinding.AcrossX==0&&clone.WorldBinding.AcrossZ==1&&!clone.WorldBinding.HasMeasuredDeployments&&!clone.WorldBinding.BindAllConfigurations,"Actual typed frame readback differs");
                        Require(JToken.DeepEquals(new JArray(template.Routes.Select(EncodePilotRoute)),new JArray(clone.Routes.Select(EncodePilotRoute))),"Actual route IDs/waypoints/metrics/flags changed during schema conversion");
                        foreach(var proposed in ((JArray)candidate["typed_pad_world_axis_candidates"]).OfType<JObject>())
                        {
                            var matches=clone.Pads.Where(v=>v.PadId==(string)proposed["pad_id"]&&v.FactionId==(byte)proposed["faction"]&&(int)v.Kind==(int)proposed["kind"]).ToArray();
                            Require(matches.Length==1,"Actual typed pad identity/faction/kind readback differs");var pad=matches[0];var rect=(JArray)proposed["world_rect"];
                            Require(math.abs(pad.WidthMetres-((float)rect[2]-(float)rect[0]))<.001f&&math.abs(pad.DepthMetres-((float)rect[3]-(float)rect[1]))<.001f&&
                                math.abs(pad.CenterX-((float)rect[0]+(float)rect[2])/2)<.001f&&math.abs(pad.CenterZ-((float)rect[1]+(float)rect[3])/2)<.001f,"Actual typed pad dimensions/center readback differs");
                        }
                        count++;
                    }
                    finally{UnityEngine.Object.DestroyImmediate(clone);}
                }
            }
            Require(count==6&&materializedPilotNegativeControls==12,"All six actual materializations and twelve schema negatives required");
            Require(worldBefore.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(defaultBefore,World.DefaultGameObjectInjectionWorld)&&
                scenesBefore==UnityEngine.SceneManagement.SceneManager.sceneCount&&previewsBefore==UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount,"Schema validation changed World/scene ownership");
            Debug.Log("[SkirmishPilotMaterializationSchema] result=Passed candidates=6 negativeControls=12 streamingWorldsCreated=0 sourceFitCertified=0");
        }

        public static void RunMaterializedPilotCandidates()
        {
            Require(!materializedPilotRequested, "Materialization entry cannot nest");
            materializedPilotRequested = true;
            materializedPilotCollected=materializedPilotErrors=materializedPilotNegativeControls=0;
            try
            {
                Run();
                Require(materializedPilotCollected==6&&materializedPilotErrors==0&&materializedPilotNegativeControls==12,"Six actual candidate collections and twelve negative schema controls required");
                Debug.Log("[SkirmishPilotMaterialization] result=Collected missions=6 negativeControls=12 sourceFitCertified=0");
            }
            finally { materializedPilotRequested = false; }
        }

        static void CollectMaterializedPilotCandidates(JObject input, JObject[] requests, OperationMapDefinition source,
            SkirmishMapLayoutConfig template, SkirmishRoleCatalogConfig roles, EntityManager em,
            Entity gridEntity, GridConfig grid, MapSurfaceComponent surface, JObject original)
        {
            if (!materializedPilotRequested) return;
            Require(Hash(MaterializedPilotConfig) == MaterializedPilotConfigSha, "Exact six-candidate JSON changed");
            var packet = JObject.Parse(File.ReadAllText(MaterializedPilotConfig));
            Require((string)packet["survey_sha256"] == "18b520ff6cb45a25f645502cba1787b27375cb52a20843195dd501e3b7029f73", "Candidate measurement authority differs");
            var all = ((JArray)packet["candidates"]).OfType<JObject>().ToArray();
            Require(all.Length == 6 && all.Select(v => (string)v["catalog_id"]).Distinct().Count() == 6,
                "Exact six unique logical candidates required");
            Require(all.Select(v => (string)v["catalog_id"]).OrderBy(v => v).SequenceEqual(new[] { "S026", "S049", "S050", "S074", "S097", "S098" }), "Candidate ID set differs");
            var rows = new JArray(); original["materializedLogicalCandidates"] = rows;
            foreach (var candidate in all.Where(v => (string)v["group"] == (string)original["group"]))
            {
                var row = new JObject { ["catalogId"] = candidate["catalog_id"], ["group"] = original["group"],
                    ["candidateConfigSha256"] = MaterializedPilotConfigSha, ["runtimeFitCertified"] = false,
                    ["readiness"] = "Pending", ["newLogicalCandidateMaterialized"] = true };
                rows.Add(row);
                SkirmishMapLayoutConfig clone = null;
                try
                {
                    Require((string)candidate["source_path"] == (string)original["sourcePath"] &&
                        (string)candidate["source_asset_sha256"] == Hash((string)original["sourcePath"]) &&
                        (string)candidate["grid_sha256"] == Hash((string)original["gridPath"]) &&
                        (string)candidate["surface_sha256"] == Hash((string)original["surfacePath"]) &&
                        (string)candidate["source_identity"] == source.SourceIdentityHash && (string)candidate["content_hash"] == source.ContentHash,
                        "Actual source/grid/surface/content binding differs");
                    var selected = requests.Where(v => (string)v["catalogId"] == (string)candidate["catalog_id"]).ToArray();
                    Require(selected.Length == 1 && CanonicalBudgetHash(selected[0]["canonicalSetupRows"]) == (string)candidate["canonical_budget_rows_sha256"], "Actual canonical matrix row binding differs");
                    RunMaterializedSchemaControls(template,source,candidate);
                    clone = MaterializePilotClone(template, source, candidate, row);
                    var facilities = MaterializedPreferredFacilities(clone, selected[0], original, grid);
                    row["allSizeFacilities"] = facilities;
                    row["candidateCompoundWorldRects"] = CandidateCompoundRects(candidate);
                    ProbePilotNormalFacilityRelocation(input, selected, clone, roles, em, gridEntity, grid, surface, row);
                    var sourceOnly=new JObject();
                    ProbePilotNormalTerrainFallback(input, selected, clone, roles, em, gridEntity, grid, surface, new JArray(), sourceOnly);
                    row["sourceOnlyNormalTerrainFallbackForcePacking"]=sourceOnly["normalTerrainFallbackForcePacking"];
                    row["sourceOnlyPackingScope"]="NewLogicalPadsOnStaticSource_NoCandidateFacilityOccupancy_FullFitNotEstablished";
                    MeasureMaterializedRoutes(clone, em, gridEntity, grid, surface, row);
                    RecordMeasuredPadRefinement(clone,grid,row);
                    row["collectionCompleted"] = true;materializedPilotCollected++;
                }
                catch (Exception error) { row["error"] = error.ToString(); row["collectionCompleted"] = false;materializedPilotErrors++; }
                finally { if (clone != null) UnityEngine.Object.DestroyImmediate(clone); }
            }
            Require(rows.Count == requests.Length, "Every source-bound mission must emit a candidate result");
            // Failed candidate rows are evidence; this collection marker is not fit.
            original["materializedCandidateErrors"] = rows.OfType<JObject>().Count(v => (bool?)v["collectionCompleted"] != true);
            Require((int)original["materializedCandidateErrors"]==0,"Candidate materialization errors preserved in per-mission rows");
        }

        static void RunMaterializedSchemaControls(SkirmishMapLayoutConfig template,OperationMapDefinition source,JObject candidate)
        {
            for(int mutation=0;mutation<2;mutation++)
            {
                var bad=(JObject)candidate.DeepClone();
                if(mutation==0)((JArray)bad["proposed_logical_world_rect"])[0]=source.Bounds.PlayableMin.x-1;
                else ((JArray)bad["typed_pad_world_axis_candidates"]).Add(bad["typed_pad_world_axis_candidates"][0].DeepClone());
                bool rejected=false;SkirmishMapLayoutConfig accidental=null;
                try{accidental=MaterializePilotClone(template,source,bad,new JObject());}
                catch(InvalidOperationException){rejected=true;}
                finally{if(accidental!=null)UnityEngine.Object.DestroyImmediate(accidental);}
                Require(rejected,"Out-of-source or duplicate typed pad candidate must reject before native placement");materializedPilotNegativeControls++;
            }
        }

        static SkirmishMapLayoutConfig MaterializePilotClone(SkirmishMapLayoutConfig template, OperationMapDefinition source, JObject candidate, JObject evidence)
        {
            var clone = UnityEngine.Object.Instantiate(template);
            try
            {
                var box = (JArray)candidate["proposed_logical_world_rect"];
                float minX = (float)box[0], minZ = (float)box[1], width = (float)box[2] - minX, depth = (float)box[3] - minZ;
                Require(math.isfinite(width) && math.isfinite(depth) && width > 0 && depth > 0, "Finite candidate envelope required");
                Require(minX >= source.Bounds.PlayableMin.x && minZ >= source.Bounds.PlayableMin.z &&
                    minX + width <= source.Bounds.PlayableMax.x && minZ + depth <= source.Bounds.PlayableMax.z, "Candidate escapes existing physical playable bounds");
                var data = JObject.Parse(JsonUtility.ToJson(clone));
                Require(data["layoutId"] != null, "Ordinary typed layout JSON required, not an Editor envelope");
                var anchors = new JArray();
                foreach (var anchor in template.Anchors)
                {
                    Require(template.TryProjectLocalToMap(anchor.WorldX, anchor.WorldZ, out float x, out float z), "Actual original anchor projection required");
                    var value = EncodePilotAnchor(anchor); value["WorldX"] = x; value["WorldZ"] = z;
                    value["NormalizedU"] = (x - minX) / width; value["NormalizedV"] = (z - minZ) / depth; anchors.Add(value);
                }
                var pads = new JArray();
                foreach (var pad in template.Pads)
                {
                    var matches = ((JArray)candidate["typed_pad_world_axis_candidates"]).OfType<JObject>()
                        .Where(v => (string)v["pad_id"] == pad.PadId && (byte)v["faction"] == pad.FactionId && (int)v["kind"] == (int)pad.Kind).ToArray();
                    Require(matches.Length == 1, "Every exact original typed pad must bind once");
                    var rect = (JArray)matches[0]["world_rect"]; var value = EncodePilotPad(pad);
                    value["CenterX"] = ((float)rect[0] + (float)rect[2]) / 2; value["CenterZ"] = ((float)rect[1] + (float)rect[3]) / 2;
                    value["WidthMetres"] = (float)rect[2] - (float)rect[0]; value["DepthMetres"] = (float)rect[3] - (float)rect[1]; pads.Add(value);
                }
                data["layoutId"] = "layout." + ((string)candidate["catalog_id"]).ToLowerInvariant() + ".measured-pad-candidate.v2";
                data["operationMapId"]=source.OperationMapId;
                data["contentHash"] = "unqualified." + MaterializedPilotConfigSha;
                data["worldWidthMetres"] = width; data["worldDepthMetres"] = depth; data["originX"] = minX; data["originZ"] = minZ;
                data["anchors"] = anchors; data["pads"] = pads; data["routes"] = new JArray(template.Routes.Select(EncodePilotRoute));
                var frame = EncodePilotFrame(template.WorldBinding);
                // TryProjectToMap centers Across at v=0.5; envelope origin remains minZ.
                frame["OriginX"] = minX; frame["OriginZ"] = minZ + depth * .5f; frame["ForwardX"] = 1; frame["ForwardZ"] = 0;
                frame["AcrossX"] = 0; frame["AcrossZ"] = 1; frame["ForwardMetres"] = width; frame["AcrossMetres"] = depth;
                frame["SourceHash"] = source.ContentHash; frame["HasMeasuredDeployments"] = false; frame["BindAllConfigurations"] = false;
                data["worldBinding"] = frame;
                JsonUtility.FromJsonOverwrite(data.ToString(Formatting.None), clone);
                Require(!clone.WorldBinding.HasMeasuredDeployments && !clone.WorldBinding.BindAllConfigurations, "Measurement cannot publish readiness flags");
                evidence["materializedLogicalJson"] = data; evidence["logicalSemanticSha256"] = SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(clone);
                evidence["unresolvedLogicalPadOverlapWitnesses"] = candidate["overlap_witnesses"].DeepClone();
                evidence["camera"] = candidate["camera"].DeepClone();
                var anchorChecks = new JArray(); evidence["retainedAnchorWorldPointChecks"] = anchorChecks;
                foreach (var anchor in template.Anchors)
                {
                    Require(template.TryProjectLocalToMap(anchor.WorldX, anchor.WorldZ, out float x, out float z),
                        "Original anchor projection missing: " + candidate["catalog_id"] + "/" + anchor.AnchorId);
                    Require(clone.TryGetAnchorById(anchor.AnchorId, out var retained),
                        "Retained anchor missing: " + candidate["catalog_id"] + "/" + anchor.AnchorId);
                    bool projected = clone.TryProjectLocalToMap(retained.WorldX, retained.WorldZ, out float rx, out float rz);
                    var check = new JObject { ["catalogId"] = candidate["catalog_id"], ["anchorId"] = anchor.AnchorId,
                        ["field"] = "worldBinding.Across centered projection", ["originalStoredX"] = anchor.WorldX, ["originalStoredZ"] = anchor.WorldZ,
                        ["expectedWorldX"] = x, ["expectedWorldZ"] = z, ["retainedStoredX"] = retained.WorldX, ["retainedStoredZ"] = retained.WorldZ,
                        ["actualWorldX"] = rx, ["actualWorldZ"] = rz, ["projectionSucceeded"] = projected,
                        ["envelopeOriginX"] = clone.OriginX, ["envelopeOriginZ"] = clone.OriginZ,
                        ["frameOriginX"] = clone.WorldBinding.OriginX, ["frameOriginZ"] = clone.WorldBinding.OriginZ,
                        ["forwardMetres"] = clone.WorldBinding.ForwardMetres, ["acrossMetres"] = clone.WorldBinding.AcrossMetres };
                    anchorChecks.Add(check);
                    Require(projected && math.abs(x - rx) < .001f && math.abs(z - rz) < .001f,
                        "Materialization changed an original role/route anchor world point: " + check.ToString(Formatting.None));
                }
                // Exercise actual projection at both envelope corners, center, and every pad corner.
                RequireMaterializedIdentityPoint(clone, candidate, "envelope.minimum", minX, minZ);
                RequireMaterializedIdentityPoint(clone, candidate, "envelope.center", minX + width * .5f, minZ + depth * .5f);
                RequireMaterializedIdentityPoint(clone, candidate, "envelope.maximum", minX + width, minZ + depth);
                foreach (var pad in clone.Pads)
                    foreach (float cornerX in new[] { pad.CenterX-pad.WidthMetres*.5f, pad.CenterX+pad.WidthMetres*.5f })
                        foreach (float cornerZ in new[] { pad.CenterZ-pad.DepthMetres*.5f, pad.CenterZ+pad.DepthMetres*.5f })
                            RequireMaterializedIdentityPoint(clone, candidate, "pad."+pad.PadId+"/"+pad.FactionId, cornerX, cornerZ);
                return clone;
            }
            catch { UnityEngine.Object.DestroyImmediate(clone); throw; }
        }

        static void RequireMaterializedIdentityPoint(SkirmishMapLayoutConfig layout, JObject candidate, string field, float x, float z)
        {
            bool projected = layout.TryProjectLocalToMap(x, z, out float actualX, out float actualZ);
            Require(projected && math.abs(x-actualX)<.001f && math.abs(z-actualZ)<.001f,
                "Materialized identity frame differs: " + new JObject { ["catalogId"] = candidate["catalog_id"], ["field"] = field,
                    ["expectedX"] = x, ["expectedZ"] = z, ["actualX"] = actualX, ["actualZ"] = actualZ,
                    ["envelopeOriginZ"] = layout.OriginZ, ["frameOriginZ"] = layout.WorldBinding.OriginZ,
                    ["depth"] = layout.WorldDepthMetres }.ToString(Formatting.None));
        }

        static JArray MaterializedPreferredFacilities(SkirmishMapLayoutConfig layout, JObject request, JObject original, GridConfig grid)
        {
            var catalog = SkirmishExpansionCatalogConfig.Load(); var profiles = catalog.ToAuthoredSet();
            Require(SkirmishSetupMatrixTable.TryLoad(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), out var matrix, out string error), error);
            var build = typeof(SkirmishSetupCompiler).GetMethod("BuildStructures", BindingFlags.Static | BindingFlags.NonPublic);
            Require(build != null, "Actual canonical structure planner required");
            var definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
            try
            {
                definition.Configure((string)request["catalogId"], profiles.ObjectiveBa, profiles.ArmyGround,
                    (string)request["start"] == "F" ? profiles.StartField : profiles.StartEstablished,
                    profiles.Economy, profiles.Upgrades, profiles.Intel, profiles.Roles, layout);
                var rows = new JArray();
                foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
                {
                    Require(SkirmishSetupMatrixTable.TryFind(matrix, definition.CatalogId, size, out var vector), "Exact actual size row required");
                    foreach (var structure in (SkirmishResolvedStructureEntry[])build.Invoke(null, new object[] { definition, vector, false }))
                    {
                        float x=0,z=0;
                        Require(Game.Configs.SkirmishMapLayoutBuilder.TryPadForStructure(layout, structure, out var pad) &&
                            layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out x, out z), "Exact candidate structure pad required");
                        Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout, structure.AcrossOffsetMetres, structure.ForwardOffsetMetres, ref x, ref z);
                        var old = ((JArray)original["allSizeFacilities"]).OfType<JObject>().Where(v => (string)v["catalogId"] == definition.CatalogId &&
                            (string)v["size"] == size.ToString() && (byte)v["faction"] == structure.FactionId && (string)v["structureId"] == structure.StructureId).ToArray();
                        Require(old.Length > 0 && old.All(v => (string)v["prefabPath"] == (string)old[0]["prefabPath"] && (string)v["prefabSha256"] == (string)old[0]["prefabSha256"]), "Exact original native prefab lineage required");
                        rows.Add(new JObject { ["catalogId"] = definition.CatalogId, ["size"] = size.ToString(), ["faction"] = structure.FactionId,
                            ["structureId"] = structure.StructureId, ["x"] = x, ["z"] = z, ["padId"] = pad.PadId,
                            ["prefabPath"] = old[0]["prefabPath"], ["prefabSha256"] = old[0]["prefabSha256"],
                            ["status"] = "MaterializedPreferredPoint_NotClearanceMeasured" });
                    }
                }
                return rows;
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        static JObject CandidateCompoundRects(JObject candidate)
        {
            var result = new JObject();
            foreach (byte faction in new byte[] { 1, 2 })
            {
                var rectangles = ((JArray)candidate["typed_pad_world_axis_candidates"]).OfType<JObject>().Where(v => (byte)v["faction"] == faction)
                    .Select(v => (JArray)v["world_rect"]).ToArray();
                Require(rectangles.Length > 0, "Explicit faction compound pads required");
                result[faction.ToString()] = new JArray(rectangles.Min(v => (float)v[0]), rectangles.Min(v => (float)v[1]),
                    rectangles.Max(v => (float)v[2]), rectangles.Max(v => (float)v[3]));
            }
            return result;
        }

        static void MeasureMaterializedRoutes(SkirmishMapLayoutConfig layout, EntityManager em, Entity gridEntity, GridConfig grid, MapSurfaceComponent surface, JObject evidence)
        {
            var configurations = ((JArray)evidence["normalFacilityRelocationProposals"]).OfType<JObject>().GroupBy(v => (string)v["size"]);
            var output = new JArray(); evidence["materializedRoutesWithPlacedFacilityBlockers"] = output;
            foreach (var configuration in configurations)
            {
                var result = new JObject { ["size"] = configuration.Key, ["liveOccupancyIncluded"] = false, ["sourceFitCertified"] = false }; output.Add(result);
                if (configuration.Any(v => (bool?)v["entireConfigurationFound"] != true))
                { result["status"] = "UnavailableCompleteAcceptedFacilityConfiguration"; continue; }
                using var blockers = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                foreach (var facility in configuration)
                {
                    var rect = (JObject)facility["resolvedEffectiveRect"];
                    for (int z = (int)rect["z"]; z < (int)rect["z"] + (int)rect["depth"]; z++)
                        for (int x = (int)rect["x"]; x < (int)rect["x"] + (int)rect["width"]; x++) blockers.Set(z * grid.Width + x, true);
                }
                var routes = new JArray(); result["routes"] = routes;
                foreach (var route in layout.Routes.Where(v => v.AllowsVehicles))
                    foreach (var role in new[] { SkirmishRoleKind.Tank, SkirmishRoleKind.Siege, SkirmishRoleKind.ApcHeavy })
                    {
                        var prefab = FindNativePrefab(em, SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                        Require(prefab != Entity.Null, "Exact native route actor required");
                        for (int leg = 1; leg < route.WaypointAnchorIds.Length; leg++)
                        {
                            SkirmishLayoutAnchorConfig a=default,b=default;
                            Require(layout.TryGetAnchorById(route.WaypointAnchorIds[leg - 1], out a) && layout.TryGetAnchorById(route.WaypointAnchorIds[leg], out b), "Exact retained route anchors required");
                            float ax=0,az=0,bx=0,bz=0;
                            Require(layout.TryProjectLocalToMap(a.WorldX, a.WorldZ, out ax, out az) && layout.TryProjectLocalToMap(b.WorldX, b.WorldZ, out bx, out bz), "Exact retained route projection required");
                            var row = new JObject { ["routeId"] = route.RouteId, ["role"] = role.ToString(), ["leg"] = leg }; routes.Add(row);
                            QueryPath(em, gridEntity, grid, surface, prefab, GridUtils.WorldToCell(grid, new float3(ax, 0, az)), GridUtils.WorldToCell(grid, new float3(bx, 0, bz)), row, 1, blockers);
                        }
                    }
                var serviceQueries=new JArray();result["serviceApproachesWithPlacedFacilities"]=serviceQueries;
                foreach(byte faction in new byte[]{1,2})
                {
                    Require(layout.TryGetPad(faction,SkirmishLegalPadKind.VehicleSpawn,out var spawn),"Actual candidate vehicle pad required");
                    Require(layout.TryGetPad(faction,SkirmishLegalPadKind.Service,out var service),"Actual candidate service pad required");
                    Require(layout.TryProjectLocalToMap(spawn.CenterX,spawn.CenterZ,out float sx,out float sz),"Vehicle pad source projection required");
                    Require(layout.TryProjectLocalToMap(service.CenterX,service.CenterZ,out float tx,out float tz),"Service pad source projection required");
                    foreach(var role in new[]{SkirmishRoleKind.Tank,SkirmishRoleKind.Siege,SkirmishRoleKind.ApcHeavy})
                    {
                        var prefab=FindNativePrefab(em,SkirmishRoleCatalogConfig.RuntimePrefabKey(role));Require(prefab!=Entity.Null,"Exact service route actor required");
                        var row=new JObject{["faction"]=faction,["role"]=role.ToString(),["fromPadId"]=spawn.PadId,["servicePadId"]=service.PadId};serviceQueries.Add(row);
                        QueryPath(em,gridEntity,grid,surface,prefab,GridUtils.WorldToCell(grid,new float3(sx,0,sz)),GridUtils.WorldToCell(grid,new float3(tx,0,tz)),row,faction,blockers);
                    }
                }
                RecordMaterializedIndependence(layout,grid,routes,result);
                result["status"] = "StaticNativeRoutesAndServiceJoinsCollected_WithAcceptedFacilityBlockers_LiveFitPending";
            }
        }

        static void RecordMaterializedIndependence(SkirmishMapLayoutConfig layout,GridConfig grid,JArray routes,JObject report)
        {
            var min=new int2[2];var max=new int2[2];var zones=new JArray();
            foreach(byte faction in new byte[]{1,2})
            {
                Require(layout.TryGetPad(faction,SkirmishLegalPadKind.GroundStaging,out var pad),"Actual typed compound entry required");
                Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z),"Actual compound entry projection required");
                var center=GridUtils.WorldToCell(grid,new float3(x,0,z));
                var half=math.min((int2)math.floor(new float2(pad.WidthMetres,pad.DepthMetres)/(2*grid.CellSize)),new int2(SharedEndpointRadiusCells));
                Require(math.all(half>0),"Positive actual bounded endpoint zone required");min[faction-1]=center-half;max[faction-1]=center+half;
                zones.Add(new JObject{["faction"]=faction,["typedPadId"]=pad.PadId,["minInclusive"]=new JArray(min[faction-1].x,min[faction-1].y),["maxInclusive"]=new JArray(max[faction-1].x,max[faction-1].y)});
            }
            var rows=new JArray();report["threeApproachIndependenceWithPlacedFacilities"]=rows;
            var ids=layout.Routes.Where(v=>v.AllowsVehicles).Select(v=>v.RouteId).ToArray();
            foreach(var role in new[]{SkirmishRoleKind.Tank,SkirmishRoleKind.Siege,SkirmishRoleKind.ApcHeavy})
            {
                var paths=new List<List<int2>>();int clearance=0;bool complete=ids.Length==3;var failures=new JArray();
                foreach(var id in ids)
                {
                    var legs=routes.OfType<JObject>().Where(v=>(string)v["role"]==role.ToString()&&(string)v["routeId"]==id).OrderBy(v=>(int)v["leg"]).ToArray();
                    if(!TryAssembleExactNativeLegs(legs,out var path,out int radius,out string error)){complete=false;failures.Add(new JObject{["routeId"]=id,["error"]=error});}
                    paths.Add(path);clearance=Math.Max(clearance,radius);
                }
                var row=new JObject{["role"]=role.ToString(),["completeExactNativePaths"]=complete,["routeIds"]=new JArray(ids),["assemblyFailures"]=failures,["actualAuthoredEndpointZones"]=zones.DeepClone(),["sourceFitCertified"]=false};rows.Add(row);
                if(!complete){row["independentUnderDeclaredProbePolicy"]=false;continue;}
                foreach(var property in ComparePilotApproaches(paths,clearance,min[0],max[0],min[1],max[1]).Properties())row[property.Name]=property.Value.DeepClone();
            }
        }

        static void RecordMeasuredPadRefinement(SkirmishMapLayoutConfig layout,GridConfig grid,JObject report)
        {
            var facilities=((JArray)report["normalFacilityRelocationProposals"]).OfType<JObject>().ToArray();
            var refinement=new JObject{["sourceFitCertified"]=false,["nativeRemeasureRequired"]=true};report["measuredPadRefinement"]=refinement;
            if(facilities.Length==0||facilities.Any(v=>(bool?)v["entireConfigurationFound"]!=true)||facilities.Select(v=>(string)v["size"]).Distinct().Count()!=3)
            {refinement["status"]="UnresolvedNoThreeCompleteNativeFacilityConfigurations";return;}
            var forceRows=((JArray)report["packingAfterNormalStaticFacilityRelocationWithTerrainFallback"]).OfType<JObject>().ToArray();
            if(forceRows.Length==0||forceRows.Any(v=>(int?)v["foundCount"]!=(int?)v["requestedCount"]||(int?)v["surfaceTraversableCount"]!=(int?)v["requestedCount"]))
            {refinement["status"]="UnresolvedActualFullArmyPackingFailed";return;}
            var refinements=new JArray();refinement["typedPadsFromActualPlacedFootprints"]=refinements;
            foreach(var pad in layout.Pads)
            {
                var rects=new List<RectInt>();
                foreach(var facility in facilities.Where(v=>(string)v["padId"]==pad.PadId&&(byte)v["faction"]==pad.FactionId))
                {
                    var rect=(JObject)facility["resolvedEffectiveRect"];rects.Add(new RectInt((int)rect["x"],(int)rect["z"],(int)rect["width"],(int)rect["depth"]));
                }
                foreach(var force in forceRows.Where(v=>(byte)v["faction"]==pad.FactionId))
                {
                    Require(SkirmishRoleIds.TryParse((string)force["roleId"],out var role),"Exact measured force role required");
                    var category=SkirmishRoleIds.Category(role);var kind=category==SkirmishPopulationCategory.Infantry?SkirmishLegalPadKind.InfantrySpawn:category==SkirmishPopulationCategory.Air?SkirmishLegalPadKind.AirReturn:SkirmishLegalPadKind.VehicleSpawn;
                    if(kind!=pad.Kind)continue;
                    var size=new int2((int)force["nativeFootprintX"],(int)force["nativeFootprintZ"]);
                    foreach(JObject actor in (JArray)force["candidateCells"])
                    {
                        if((bool?)actor["normalFallbackAccepted"]!=true)continue;
                        var minimum=UnitFootprintUtility.GetMinCell(new int2((int)actor["x"],(int)actor["z"]),size);rects.Add(new RectInt(minimum.x,minimum.y,size.x,size.y));
                    }
                }
                if(rects.Count==0)rects.Add(PilotExactAxisAlignedPadCells(layout,pad,grid));
                int loX=rects.Min(v=>v.xMin),loZ=rects.Min(v=>v.yMin),hiX=rects.Max(v=>v.xMax),hiZ=rects.Max(v=>v.yMax);
                float minX=grid.Origin.x+loX*grid.CellSize,minZ=grid.Origin.z+loZ*grid.CellSize,maxX=grid.Origin.x+hiX*grid.CellSize,maxZ=grid.Origin.z+hiZ*grid.CellSize;
                // Service point remains the actual runtime interaction center.
                // Tightening its region cannot silently move that repair owner.
                if(pad.Kind==SkirmishLegalPadKind.Service)
                {
                    var original=PilotExactAxisAlignedPadCells(layout,pad,grid);
                    minX=math.min(minX,grid.Origin.x+original.xMin*grid.CellSize);minZ=math.min(minZ,grid.Origin.z+original.yMin*grid.CellSize);
                    maxX=math.max(maxX,grid.Origin.x+original.xMax*grid.CellSize);maxZ=math.max(maxZ,grid.Origin.z+original.yMax*grid.CellSize);
                }
                refinements.Add(new JObject{["padId"]=pad.PadId,["kind"]=(int)pad.Kind,["faction"]=pad.FactionId,["worldRect"]=new JArray(minX,minZ,maxX,maxZ),["actualFootprintRows"]=rects.Count});
            }
            var overlaps=new JArray();var pads=refinements.OfType<JObject>().ToArray();
            for(int i=0;i<pads.Length;i++)for(int j=i+1;j<pads.Length;j++)
            {
                if((byte)pads[i]["faction"]!=(byte)pads[j]["faction"])continue;
                var a=(JArray)pads[i]["worldRect"];var b=(JArray)pads[j]["worldRect"];
                if((float)a[0]<(float)b[2]&&(float)b[0]<(float)a[2]&&(float)a[1]<(float)b[3]&&(float)b[1]<(float)a[3])
                    overlaps.Add(new JArray(pads[i]["padId"],pads[j]["padId"]));
            }
            refinement["remainingLogicalPadOverlapWitnesses"]=overlaps;
            refinement["status"]=overlaps.Count==0?"MeasuredNonoverlappingPadRefinement_NativeRecompileAndRemeasurePending":"MeasuredTightHullsStillOverlap_LogicalReauthoringRequired";
        }

        static string CanonicalBudgetHash(JToken token)
        {
            string Canonical(JToken value)
            {
                if (value is JObject obj) return "{" + string.Join(", ", obj.Properties().OrderBy(v => v.Name, StringComparer.Ordinal).Select(v => JsonConvert.ToString(v.Name) + ": " + Canonical(v.Value))) + "}";
                if (value is JArray array) return "[" + string.Join(", ", array.Select(Canonical)) + "]";
                Require(value.Type == JTokenType.String, "Canonical native budget facts must remain string fields");
                return JsonConvert.ToString((string)value);
            }
            using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(token)))).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
