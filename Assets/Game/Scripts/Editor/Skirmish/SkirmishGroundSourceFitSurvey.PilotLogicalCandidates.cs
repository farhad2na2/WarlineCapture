#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        const string PilotFacts = "Design/AgentReports/SkirmishExpansion/GroundPilots/source-fit-next-logical-inputs-20261015-01.json";

        sealed class PilotCandidateOwner : IDisposable
        {
            public SkirmishMapLayoutConfig Layout;
            public void Dispose() { if (Layout != null) UnityEngine.Object.DestroyImmediate(Layout); }
        }

        // These are explicit engineering proposals on already measured rectangles,
        // never a declaration that a deployment, route or native picture is accepted.
        static SkirmishMapLayoutConfig CreatePilotLogicalCandidate(OperationMapDefinition source, string group,
            JObject report, PilotCandidateOwner owner)
        {
            Require(group == "MP" || group == "IB", "Only measured MP/IB candidate compounds are supported");
            Require(Hash(PilotFacts) == "854b8b7cd7dbc47d2ab4dc6a4ab6829eee7b5f1830aabcc31f00dfb3890bbc2b", "Measured pilot facts changed");
            var facts = JObject.Parse(File.ReadAllText(PilotFacts));
            var rows = ((JArray)facts["sources"]).OfType<JObject>().Where(r => (string)r["group"] == group).ToArray();
            string actualSourcePath = AssetDatabase.GetAssetPath(source);
            string actualSourceSha = string.IsNullOrEmpty(actualSourcePath) ? null : Hash(actualSourcePath);
            var sourceJoin = new JObject { ["group"] = group, ["measuredRows"] = rows.Length,
                ["actualSourcePath"] = actualSourcePath, ["callerSourcePath"] = report["sourcePath"],
                ["expectedAssetSha256"] = rows.Length == 1 ? rows[0]["source_sha256"] : null,
                ["callerAssetSha256"] = report["sourceAssetSha256"], ["actualAssetSha256"] = actualSourceSha,
                ["expectedContentHash"] = rows.Length == 1 ? rows[0]["source_content_hash"] : null,
                ["actualContentHash"] = source.ContentHash,
                ["expectedSourceIdentityHash"] = rows.Length == 1 ? rows[0]["source_identity_hash"] : null,
                ["actualSourceIdentityHash"] = source.SourceIdentityHash };
            Require(rows.Length == 1 && !string.IsNullOrEmpty(actualSourceSha) &&
                (string)rows[0]["source_sha256"] == (string)report["sourceAssetSha256"] &&
                (string)report["sourceAssetSha256"] == actualSourceSha &&
                (string)rows[0]["source_content_hash"] == source.ContentHash &&
                (string)rows[0]["source_identity_hash"] == source.SourceIdentityHash,
                "Exact measured source join required: " + sourceJoin.ToString(Newtonsoft.Json.Formatting.None));
            Require((string)facts["native_survey_sha256"] == "c8bfa82f6f76dfe35344fc6e00c00ce460baa700e395b150ac53c64644f36a84",
                "Actual native survey reference changed");
            var compounds = (JArray)rows[0]["measured_compounds"];
            Require(compounds.Count == 2, "Exact two measured compounds required");
            var centers = new Vector2[2];
            for (int i = 0; i < 2; i++)
            {
                var row = (JObject)compounds[i]; var proposal = (JObject)row["proposal"];
                Require((int)row["actualBakedBlockedCells"] == 0 && (int)row["surfaceSampledCells"] == 7680 &&
                    (int)proposal["sizeCellsXZ"][0] == 96 && (int)proposal["sizeCellsXZ"][1] == 80,
                    "Expected measured rectangle facts missing");
                centers[i] = new Vector2((float)proposal["centerWorldXZ"][0], (float)proposal["centerWorldXZ"][1]);
            }
            float west = centers[0].x, east = centers[1].x, z = centers[0].y;
            Require(east > west + 200 && centers[1].y == z, "Measured east-west compound frame required");
            var layout = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>(); owner.Layout = layout;
            layout.ConfigureDesertBaseAssault();
            // Author only the declared logical schema. EditorJsonUtility's native
            // object envelope did not expose pads in the checked native run;
            // copy the actual configured typed template instead of guessing keys.
            Require(layout.Pads != null && layout.Pads.Length > 0, "Actual configured typed template pads required");
            var json = new JObject();
            float originX = west - 48, originZ = z - 40, width = east - west + 96;
            json["layoutId"] = "layout.skirmish." + group.ToLowerInvariant() + ".ba.g.proposal.v1";
            json["operationMapId"] = source.OperationMapId;
            json["contentVersion"] = 1; json["contentHash"] = "unqualified.pilot.logical.v1";
            json["worldWidthMetres"] = width; json["worldDepthMetres"] = 80;
            json["originX"] = originX; json["originZ"] = originZ; json["cellSize"] = source.GridMetadata.CellSize;
            var anchors = new JArray(); var pads = new JArray(layout.Pads.Select(pad => JObject.FromObject(pad)));
            // Compounds have explicit separate building, infantry, vehicle, air,
            // supply and service slots. Their suitability is measured below.
            foreach (JObject pad in pads)
            {
                byte faction = (byte)pad["FactionId"]; int kind = (int)pad["Kind"];
                float x = faction == 1 ? west : east, dz = 0, dx = 0;
                switch (kind)
                {
                    case 1: dx = -28; dz = 0; break;
                    case 2: dx = 0; dz = 0; break;
                    case 3: dx = -22; dz = -24; break;
                    case 4: dx = 22; dz = -20; break;
                    case 5: dx = 28; dz = 12; break;
                    case 6: dx = -25; dz = 24; break;
                    case 7: dx = 12; dz = 24; break;
                    case 8: dx = 26; dz = 24; break;
                    case 9: x = west + (east - west) * (pad["RoleId"].ToString().EndsWith("a", StringComparison.Ordinal) ? .35f : .65f); dz = -28; break;
                    default: throw new InvalidOperationException("Unexpected template pad kind");
                }
                x += dx; float pz = z + dz;
                string id = "anchor.pilot." + pad["RoleId"].ToString();
                pad["AnchorId"] = id; pad["CenterX"] = x; pad["CenterZ"] = pz;
                anchors.Add(PilotAnchor(id, (string)pad["RoleId"], x, pz, originX, originZ, width));
            }
            var routes = new JArray();
            for (int route = 0; route < 3; route++)
            {
                float dz = route == 0 ? 0 : route == 1 ? 28 : -28;
                var points = new[] { new Vector2(west + 30, z), new Vector2(west + 54, z + dz),
                    new Vector2(east - 54, z + dz), new Vector2(east - 30, z) };
                var ids = new JArray(); float length = 0;
                for (int point = 0; point < points.Length; point++)
                {
                    string id = "anchor.pilot.approach." + route + "." + point;
                    ids.Add(id); anchors.Add(PilotAnchor(id, "approach." + route + "." + point,
                        points[point].x, points[point].y, originX, originZ, width));
                    if (point > 0) length += Vector2.Distance(points[point - 1], points[point]);
                }
                routes.Add(new JObject { ["RouteId"] = "route.pilot." + route, ["WaypointAnchorIds"] = ids,
                    ["Kind"] = route + 1, ["LengthMetres"] = length, ["WidthMetres"] = 8,
                    ["InfantryTransitSeconds"] = length / 4, ["GroundTransitSeconds"] = length / 8,
                    ["AirTransitSeconds"] = 0, ["InfantryFirstContactSeconds"] = length / 8,
                    ["GroundFirstContactSeconds"] = length / 16, ["ProvisionalTimes"] = true,
                    ["AllowsVehicles"] = true, ["AllowsAircraft"] = false });
            }
            json["anchors"] = anchors; json["routes"] = routes; json["pads"] = pads;
            json["worldBinding"] = new JObject { ["OriginX"] = originX, ["OriginZ"] = z,
                ["ForwardX"] = 1, ["ForwardZ"] = 0, ["AcrossX"] = 0, ["AcrossZ"] = 1,
                ["ForwardMetres"] = width, ["AcrossMetres"] = 80, ["SourceHash"] = source.ContentHash,
                ["PlayerAnchorId"] = "anchor.pilot.base.player", ["EnemyAnchorId"] = "anchor.pilot.base.enemy",
                ["HasMeasuredDeployments"] = false, ["BindAllConfigurations"] = false };
            JsonUtility.FromJsonOverwrite(json.ToString(Newtonsoft.Json.Formatting.None), layout);
            Require(layout.HasWorldBinding && !layout.WorldBinding.HasMeasuredDeployments && !layout.WorldBinding.BindAllConfigurations,
                "Probe candidate must remain unqualified");
            report["layoutStatus"] = "ExplicitUnqualifiedLogicalCompoundCandidate";
            report["candidateLayoutSerialized"] = json;
            report["candidateLayoutSemanticSha256"] = SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout);
            report["camera"] = new JObject { ["accepted"] = false, ["profileValues"] = JValue.CreateNull(),
                ["openingFocus"] = "Actual owned opening infantry and measured world elevation required",
                ["requirements"] = new JArray("Readable infantry native PNG and viewport bounds at default/min/max", "Owned helicopter/recon native clearance and framing", "Role-derived focus; no troop orders") };
            return layout;
        }

        static JObject PilotAnchor(string id, string role, float x, float z, float ox, float oz, float width) =>
            new JObject { ["AnchorId"] = id, ["RoleId"] = role, ["WorldX"] = x, ["WorldZ"] = z,
                ["NormalizedU"] = (x - ox) / width, ["NormalizedV"] = (z - oz) / 80 };

        public static void RunPilotLogicalAuthoringControls()
        {
            var input = JObject.Parse(File.ReadAllText(Inventory)); int controls = 0;
            foreach (string group in new[] { "MP", "IB" })
            {
                var pin = input["sources"][group];
                var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)pin["path"]);
                Require(source != null && !EditorUtility.IsDirty(source), "Actual immutable source required");
                var report = new JObject { ["sourceAssetSha256"] = Hash((string)pin["path"]) };
                using var owner = new PilotCandidateOwner();
                var layout = CreatePilotLogicalCandidate(source, group, report, owner);
                Require(layout.Routes.Length == 3 && layout.Routes.Select(r => r.Kind).Distinct().Count() == 3 &&
                    !layout.WorldBinding.HasMeasuredDeployments && !layout.WorldBinding.BindAllConfigurations,
                    "Unqualified candidate must retain three distinct typed approaches");
                foreach (var anchor in layout.Anchors)
                    Require(layout.TryProjectToMap(anchor.NormalizedU, anchor.NormalizedV, out float x, out float z) &&
                        Mathf.Abs(x - anchor.WorldX) < .001f && Mathf.Abs(z - anchor.WorldZ) < .001f,
                        "Actual projection must preserve candidate world coordinates");
                foreach (byte faction in new byte[] { 1, 2 })
                    foreach (var kind in new[] { SkirmishLegalPadKind.GroundStaging, SkirmishLegalPadKind.Service,
                        SkirmishLegalPadKind.InfantrySpawn, SkirmishLegalPadKind.VehicleSpawn, SkirmishLegalPadKind.AirReturn })
                        Require(layout.TryGetPad(faction, kind, out var pad) && pad.WidthMetres > 0 && pad.DepthMetres > 0,
                            "Exact faction/type pad missing");
                controls++;
                report["sourceAssetSha256"] = new string('0', 64); bool rejected = false;
                try { using var wrong = new PilotCandidateOwner(); CreatePilotLogicalCandidate(source, group, report, wrong); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "Foreign source pin must reject before candidate creation"); controls++;
            }
            var candidates = AssetDatabase.FindAssets("Building_GroundStaging t:Prefab", new[] { "Assets/Game/Prefabs/Skirmish" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(p => p != null && p.name == "Building_GroundStaging").ToArray();
            Require(candidates.Length == 1, "Exact normal Ground Staging prefab required");
            var identity = candidates[0].GetComponent<SkirmishGroundStagingIdentity>();
            Require(identity != null && !identity.IsExpertTent && identity.StructureId == "building.ground_staging" &&
                candidates[0].GetComponent<Game.Authoring.BuildingDefinitionAuthoring>() != null,
                "Normal configured Ground Staging required; Expert Tent cannot substitute");
            controls++;
            RunPilotRouteIndependenceControls(); controls += 3;
            Debug.Log("[SkirmishPilotLogicalAuthoringControls] result=Passed controls=" + controls + " fitCertified=0");
        }

        static void ProbePilotServiceAccess(EntityManager em, Entity gridEntity, GridConfig grid,
            MapSurfaceComponent surface, SkirmishMapLayoutConfig layout, JObject report)
        {
            if ((string)report["layoutStatus"] != "ExplicitUnqualifiedLogicalCompoundCandidate") return;
            var rows = new JArray(); report["nativeServiceAccessQueries"] = rows;
            foreach (byte faction in new byte[] { 1, 2 })
            {
                Require(layout.TryGetPad(faction, SkirmishLegalPadKind.Service, out var service), "Exact service pad required");
                Require(layout.TryGetPad(faction, SkirmishLegalPadKind.VehicleSpawn, out var spawn), "Exact vehicle pad required");
                Require(layout.TryProjectLocalToMap(service.CenterX, service.CenterZ, out float sx, out float sz), "Service access projection required");
                Require(layout.TryProjectLocalToMap(spawn.CenterX, spawn.CenterZ, out float vx, out float vz), "Vehicle spawn projection required");
                var from = GridUtils.WorldToCell(grid, new float3(vx, 0, vz));
                var to = GridUtils.WorldToCell(grid, new float3(sx, 0, sz));
                foreach (var role in new[] { Game.Skirmish.Contracts.SkirmishRoleKind.Tank, Game.Skirmish.Contracts.SkirmishRoleKind.Siege,
                    Game.Skirmish.Contracts.SkirmishRoleKind.ApcHeavy })
                {
                    var prefab = FindNativePrefab(em, SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                    Require(prefab != Entity.Null, "Exact native heavy service actor template required");
                    var row = new JObject { ["faction"] = faction, ["role"] = role.ToString(), ["servicePadId"] = service.PadId,
                        ["fromVehicleSpawnPadId"] = spawn.PadId, ["requestedStartCell"] = new JArray(from.x, from.y),
                        ["requestedGoalCell"] = new JArray(to.x, to.y), ["scope"] = "NativeStaticServiceApproach_NoFacilityOrLiveOccupancy",
                        ["normalPaidRepairCertified"] = false };
                    rows.Add(row); QueryPath(em, gridEntity, grid, surface, prefab, from, to, row, faction);
                }
            }
        }

        static void RecordPilotPlacementGates(JObject report)
        {
            if ((string)report["layoutStatus"] != "ExplicitUnqualifiedLogicalCompoundCandidate") return;
            var facilities = ((JArray)report["allSizeFacilities"]).OfType<JObject>().ToArray();
            var overlap = new JArray(); report["facilityMutualOverlaps"] = overlap;
            for (int i = 0; i < facilities.Length; i++) for (int j = i + 1; j < facilities.Length; j++)
            {
                var a = facilities[i]; var b = facilities[j];
                if ((string)a["catalogId"] != (string)b["catalogId"] || (string)a["size"] != (string)b["size"] ||
                    a["originX"] == null || b["originX"] == null) continue;
                if ((int)a["originX"] < (int)b["originX"] + (int)b["widthCells"] &&
                    (int)b["originX"] < (int)a["originX"] + (int)a["widthCells"] &&
                    (int)a["originZ"] < (int)b["originZ"] + (int)b["depthCells"] &&
                    (int)b["originZ"] < (int)a["originZ"] + (int)a["depthCells"])
                    overlap.Add(new JObject { ["catalogId"] = a["catalogId"], ["size"] = a["size"],
                        ["first"] = a.DeepClone(), ["second"] = b.DeepClone() });
            }
            int rejectedFacilities = facilities.Count(r => (string)r["status"] != "PreferredFootprintMeasured_ActualPlacementMayRelocate" ||
                (bool?)r["insideGrid"] != true || (bool?)r["foundationLevel"] != true ||
                (int?)r["blockedGridCells"] != 0 || (int?)r["waterCells"] != 0 || (int?)r["roadCells"] != 0 || (int?)r["sidewalkCells"] != 0);
            var forces = ((JArray)report["allSizeForcePacking"]).OfType<JObject>().ToArray();
            var routes = ((JArray)report["heavyRouteQueries"]).OfType<JObject>().ToArray();
            report["pilotPlacementGates"] = new JObject { ["facilityRows"] = facilities.Length,
                ["rejectedPreferredFacilityRows"] = rejectedFacilities, ["mutualFacilityOverlaps"] = overlap.Count,
                ["forceRows"] = forces.Length, ["rejectedForceRows"] = forces.Count(r => (string)r["status"] != "ProposedPackingFound_RuntimeFitPending"),
                ["nativeRouteLegRows"] = routes.Length, ["failedRouteLegRows"] = routes.Count(r => (string)r["status"] != "NativeQueryFoundExactGoal"),
                ["serviceApproachRows"] = ((JArray)report["nativeServiceAccessQueries"]).Count,
                ["failedServiceApproachRows"] = ((JArray)report["nativeServiceAccessQueries"]).OfType<JObject>().Count(r => (string)r["status"] != "NativeQueryFoundExactGoal"),
                ["threeDistinctApproachesProposed"] = true, ["allApproachesNativeCertified"] = false,
                ["sceneryAndClearance"] = "Pending actual model/packed geometry join",
                ["serviceAccess"] = "Pending actual Ground Staging and paid service lifecycle",
                ["fullProductionEnvelope"] = "Pending paid G16 capabilities and actual helicopter/recon delivery",
                ["readiness"] = "Pending", ["sourceFitCertified"] = false };
        }
    }
}
#endif
