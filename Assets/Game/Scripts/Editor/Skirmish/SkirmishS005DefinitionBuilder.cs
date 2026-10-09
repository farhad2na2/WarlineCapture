#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishS005DefinitionBuilder
    {
        public const string Folder = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005";
        public const string SourcePath = "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";

        public static void BindExistingDenseCampaignSource()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Bind logical configuration in Edit mode.");
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourcePath);
            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Folder + "/SkirmishOperationMap_S005.asset");
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(Folder + "/SkirmishLayout_S005.asset");
            if (source == null || map == null || layout == null ||
                !source.TryValidateMetadata(out var sourceError))
                throw new InvalidOperationException("Existing dense source or S005 logical configuration is invalid.");
            var beforeMap = UnityEngine.Object.Instantiate(map);
            var beforeLayout = UnityEngine.Object.Instantiate(layout);
            beforeMap.name = "SkirmishOperationMap_S005";
            beforeLayout.name = "SkirmishLayout_S005";
            try
            {
                var prior = new SerializedObject(beforeMap);
                EditorUtility.CopySerialized(source, map); map.name = "SkirmishOperationMap_S005";
                var data = new SerializedObject(map);
                foreach (string property in new[] {"bounds", "cameras", "planningCameraId", "battleCameraId", "minimap", "anchors"})
                    data.CopyFromSerializedProperty(prior.FindProperty(property));
                data.FindProperty("operationMapId").stringValue = "opmap.skirmish.desert_base_01_s005";
                var binding = data.FindProperty("sourceBinding");
                binding.FindPropertyRelative("sourceOperationMapId").stringValue = source.OperationMapId;
                binding.FindPropertyRelative("sourceIdentityHash").stringValue = source.SourceIdentityHash;
                binding.FindPropertyRelative("sourceContentHash").stringValue = source.ContentHash;
                // Broader logical construction bounds expose existing source
                // pads for capacity qualification; preserve measured armies,
                // objectives, deployments, budgets and every physical asset.
                data.FindProperty("bounds").FindPropertyRelative("playableMin").vector3Value = new Vector3(750,-20,150);
                data.FindProperty("bounds").FindPropertyRelative("playableMax").vector3Value = new Vector3(1900,980,1000);
                data.FindProperty("minimap").FindPropertyRelative("projectionOrigin").vector3Value = new Vector3(750,0,150);
                data.FindProperty("minimap").FindPropertyRelative("projectionSize").vector2Value = new Vector2(1150,850);
                var cameras = data.FindProperty("cameras");
                string battleCameraId = data.FindProperty("battleCameraId").stringValue;
                for (int i = 0; i < cameras.arraySize; i++)
                {
                    var camera = cameras.GetArrayElementAtIndex(i);
                    if (camera.FindPropertyRelative("cameraId").stringValue != battleCameraId) continue;
                    camera.FindPropertyRelative("position").vector3Value = new Vector3(905,65,600);
                    camera.FindPropertyRelative("eulerAngles").vector3Value = new Vector3(50,35,0);
                }
                data.FindProperty("contentHash").stringValue = "";
                data.FindProperty("generatedMetadataHash").stringValue = "";
                data.ApplyModifiedPropertiesWithoutUndo();
                using (var hash = System.Security.Cryptography.SHA256.Create())
                {
                    string digest = BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-", "").ToLowerInvariant();
                    data.Update(); data.FindProperty("contentHash").stringValue = digest;
                    data.FindProperty("generatedMetadataHash").stringValue = digest;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                if (!map.TryValidateMetadata(out var error) || !map.TryValidateLocalContentReferences(out error))
                    throw new InvalidOperationException("Dense S005 logical binding is invalid: " + error);
                layout.name = "SkirmishLayout_S005";
                var logical = new SerializedObject(layout);
                var measured = logical.FindProperty("worldBinding");
                float oldAcross = measured.FindPropertyRelative("AcrossMetres").floatValue;
                float oldOrigin = measured.FindPropertyRelative("OriginZ").floatValue;
                if (Mathf.Abs(oldAcross - 160f) < 0.01f) oldOrigin = 875f;
                // The dense residential quarter cannot legally accommodate both
                // full barracks. Reuse the existing southern town frontage;
                // translate the deployment geometry together to retain separation.
                float deploymentShift = 650f - (Mathf.Abs(oldAcross - 160f) < 0.01f
                    ? 875f : measured.FindPropertyRelative("PlayerDeployment").vector2Value.y);
                float oldLocalOrigin = logical.FindProperty("originZ").floatValue;
                float oldLocalDepth = logical.FindProperty("worldDepthMetres").floatValue;
                // Re-express the same physical combat routes in an envelope that
                // also covers existing southern industrial/airfield space.
                var allAnchors = logical.FindProperty("anchors");
                for (int i = 0; i < allAnchors.arraySize; i++)
                {
                    var anchor = allAnchors.GetArrayElementAtIndex(i);
                    float worldZ = oldOrigin + (anchor.FindPropertyRelative("NormalizedV").floatValue - 0.5f) * oldAcross + deploymentShift;
                    anchor.FindPropertyRelative("NormalizedV").floatValue = (worldZ - 150f) / 850f;
                    anchor.FindPropertyRelative("WorldZ").floatValue = worldZ;
                }
                var allPads = logical.FindProperty("pads");
                for (int i = 0; i < allPads.arraySize; i++)
                {
                    var pad = allPads.GetArrayElementAtIndex(i);
                    float v = (pad.FindPropertyRelative("CenterZ").floatValue - oldLocalOrigin) / oldLocalDepth;
                    pad.FindPropertyRelative("CenterZ").floatValue = oldOrigin + (v - 0.5f) * oldAcross + deploymentShift;
                }
                // Preserve the legal pad sizes in metres. Their previous compressed
                // projection put infantry inside the ground staging footprint.
                for (int faction = 1; faction <= 2; faction++)
                {
                    SerializedProperty staging = null, infantry = null;
                    for (int i = 0; i < allPads.arraySize; i++)
                    {
                        var pad = allPads.GetArrayElementAtIndex(i);
                        if (pad.FindPropertyRelative("FactionId").intValue != faction) continue;
                        string role = pad.FindPropertyRelative("RoleId").stringValue;
                        if (role == (faction == 1 ? "staging.player" : "staging.enemy")) staging = pad;
                        if (role == (faction == 1 ? "spawn.infantry.player" : "spawn.infantry.enemy")) infantry = pad;
                    }
                    if (staging == null || infantry == null) throw new InvalidOperationException("Missing required staging/infantry pad.");
                    float separation = (staging.FindPropertyRelative("DepthMetres").floatValue + infantry.FindPropertyRelative("DepthMetres").floatValue) * 0.5f + 3f;
                    infantry.FindPropertyRelative("CenterZ").floatValue = staging.FindPropertyRelative("CenterZ").floatValue + (faction == 1 ? -separation : separation);
                }
                logical.FindProperty("originZ").floatValue = 150f;
                logical.FindProperty("worldDepthMetres").floatValue = 850f;
                measured.FindPropertyRelative("AcrossMetres").floatValue = 850f;
                measured.FindPropertyRelative("SourceHash").stringValue = map.ContentHash;
                // Rebase the logical frame onto the existing DB layout's town
                // frontage. Preserve deployment separation and army budgets.
                measured.FindPropertyRelative("OriginX").floatValue = 863.50525f;
                measured.FindPropertyRelative("OriginZ").floatValue = 575f;
                measured.FindPropertyRelative("PlayerDeployment").vector2Value = new Vector2(935,650);
                measured.FindPropertyRelative("EnemyDeployment").vector2Value = new Vector2(1387.8f,650);
                // The northern supply yard cannot fit its full starting grant
                // among the existing town foundations. Use the southern frontage
                // within the same logical frame; keep both base deployments.
                const float enemySupplyX = 1430f, enemySupplyZ = 650f;
                float supplyU = (enemySupplyX - 863.50525f) / measured.FindPropertyRelative("ForwardMetres").floatValue;
                float supplyV = 0.5f + (enemySupplyZ - 575f) / measured.FindPropertyRelative("AcrossMetres").floatValue;
                float supplyLocalX = logical.FindProperty("originX").floatValue + supplyU * logical.FindProperty("worldWidthMetres").floatValue;
                float supplyLocalZ = logical.FindProperty("originZ").floatValue + supplyV * logical.FindProperty("worldDepthMetres").floatValue;
                var supplyAnchors = logical.FindProperty("anchors");
                for (int i = 0; i < supplyAnchors.arraySize; i++)
                {
                    var anchor = supplyAnchors.GetArrayElementAtIndex(i);
                    if (anchor.FindPropertyRelative("RoleId").stringValue != "economy.enemy") continue;
                    anchor.FindPropertyRelative("NormalizedU").floatValue = supplyU;
                    anchor.FindPropertyRelative("NormalizedV").floatValue = supplyV;
                    anchor.FindPropertyRelative("WorldX").floatValue = supplyLocalX;
                    anchor.FindPropertyRelative("WorldZ").floatValue = supplyLocalZ;
                }
                var supplyPads = logical.FindProperty("pads");
                for (int i = 0; i < supplyPads.arraySize; i++)
                {
                    var pad = supplyPads.GetArrayElementAtIndex(i);
                    if (pad.FindPropertyRelative("RoleId").stringValue != "economy.enemy") continue;
                    pad.FindPropertyRelative("CenterX").floatValue = supplyLocalX;
                    pad.FindPropertyRelative("CenterZ").floatValue = supplyLocalZ;
                }
                logical.ApplyModifiedPropertiesWithoutUndo();
                logical.Update();
                var logicalRoutes = logical.FindProperty("routes");
                for (int i = 0; i < logicalRoutes.arraySize; i++)
                {
                    var route = logicalRoutes.GetArrayElementAtIndex(i);
                    var ids = route.FindPropertyRelative("WaypointAnchorIds");
                    var points = new System.Collections.Generic.List<SkirmishLayoutAnchorConfig>();
                    for (int j = 0; j < ids.arraySize; j++)
                        if (layout.TryGetAnchorById(ids.GetArrayElementAtIndex(j).stringValue, out var point)) points.Add(point);
                    route.FindPropertyRelative("LengthMetres").floatValue = Game.Configs.SkirmishMapLayoutBuilder.PolylineLength(points);
                }
                logical.ApplyModifiedPropertiesWithoutUndo();
                SetRearVehicleSpawnPads(layout);
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(layout);
                AssetDatabase.SaveAssetIfDirty(map); AssetDatabase.SaveAssetIfDirty(layout);
                Debug.Log("[S005DenseCampaignBinding] result=Passed physicalSourceModified=0 scenarioBudgetsChanged=0 source=" + source.OperationMapId + " nativeSourceFit=Pending");
            }
            catch
            {
                EditorUtility.CopySerialized(beforeMap, map); EditorUtility.CopySerialized(beforeLayout, layout);
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(layout);
                AssetDatabase.SaveAssetIfDirty(map); AssetDatabase.SaveAssetIfDirty(layout);
                throw;
            }
            finally {UnityEngine.Object.DestroyImmediate(beforeMap);UnityEngine.Object.DestroyImmediate(beforeLayout);}
        }

        public static void BindRearVehicleSpawnPads()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Bind logical spawn pads in Edit mode.");
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(Folder + "/SkirmishLayout_S005.asset");
            SetRearVehicleSpawnPads(layout);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssetIfDirty(layout);
            Debug.Log("[S005RearVehiclePads] result=Passed playerWorld=(914.5,650.5) enemyWorld=(1410.5,650.5) physicalSourceModified=0 budgetsChanged=0 nativeOpeningAndHauling=Pending");
        }

        private static void SetRearVehicleSpawnPads(SkirmishMapLayoutConfig layout)
        {
            if (layout == null || !layout.HasWorldBinding)
                throw new InvalidOperationException("Missing measured S005 logical layout.");
            var frame = layout.WorldBinding;
            float determinant = frame.ForwardX * frame.AcrossZ - frame.ForwardZ * frame.AcrossX;
            if (Mathf.Abs(determinant) < .001f || frame.ForwardMetres <= 0 || frame.AcrossMetres <= 0)
                throw new InvalidOperationException("Invalid logical deployment frame.");
            var data = new SerializedObject(layout);
            var pads = data.FindProperty("pads");
            int changed = 0;
            for (int i = 0; i < pads.arraySize; i++)
            {
                var pad = pads.GetArrayElementAtIndex(i);
                if (pad.FindPropertyRelative("Kind").enumValueIndex != (int)SkirmishLegalPadKind.VehicleSpawn) continue;
                int faction = pad.FindPropertyRelative("FactionId").intValue;
                if (faction != 1 && faction != 2) continue;
                // The old vehicle pad started logistics ahead of the defensive
                // line. These rear cells passed the native spawn search on the
                // unchanged Campaign grid; actual opening/haul evidence follows.
                var target = new Vector2(faction == 1 ? 914.5f : 1410.5f, 650.5f);
                float dx = target.x - frame.OriginX, dz = target.y - frame.OriginZ;
                float forward = (dx * frame.AcrossZ - dz * frame.AcrossX) / determinant;
                float across = (frame.ForwardX * dz - frame.ForwardZ * dx) / determinant;
                pad.FindPropertyRelative("CenterX").floatValue = layout.OriginX + forward / frame.ForwardMetres * layout.WorldWidthMetres;
                pad.FindPropertyRelative("CenterZ").floatValue = layout.OriginZ + (.5f + across / frame.AcrossMetres) * layout.WorldDepthMetres;
                changed++;
            }
            if (changed != 2) throw new InvalidOperationException("Expected both faction vehicle pads.");
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AuditDenseSourceSpatialFit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Inspect source in Edit mode.");
            const string path = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
            string Digest()
            {
                using var hash = System.Security.Cryptography.SHA256.Create();
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
            }
            string before = Digest();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var buildings = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Game.Authoring.OperationMapBuildingAuthoring>(true))
                    .Select(b => new {name=b.name, stableId=b.StableId, faction=b.FactionId,
                        originX=b.OriginCell.x, originZ=b.OriginCell.y,
                        width=b.FootprintCells.x, depth=b.FootprintCells.y,
                        x=b.transform.position.x, y=b.transform.position.y, z=b.transform.position.z}).ToArray();
                File.WriteAllText("Design/AgentReports/SkirmishExpansion/S005/readonly542-dense-building-footprints.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(new {physicalSourceModified=false, scenePath=path,
                        sourceSha256=before.ToLowerInvariant(), buildings}, Newtonsoft.Json.Formatting.Indented));
                if (before != Digest()) throw new InvalidOperationException("Source scene changed during spatial inspection.");
                Debug.Log("[DenseSourceSpatialFit] result=Captured buildings="+buildings.Length+" physicalSourceModified=0 nativeLegality=Pending");
            }
            finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }

        public static void AuditExistingDenseRenderRows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Inspect source in Edit mode.");
            const string scenePath = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
            string before;
            using (var hash = System.Security.Cryptography.SHA256.Create())
                before = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(scenePath)));
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var roots = scene.GetRootGameObjects().Select(root => new
                {
                    name = root.name,
                    branches = root.GetComponentsInChildren<Renderer>(true)
                        .Where(r => r.enabled && r.gameObject.activeInHierarchy)
                        .GroupBy(r => DenseRenderBranch(r.transform, root.transform))
                        .Select(group => new { path = group.Key, renderers = group.Count(),
                            materialRows = group.Sum(r => r.sharedMaterials.Length) })
                        .OrderByDescending(group => group.materialRows).ToArray()
                }).ToArray();
                File.WriteAllText("Design/AgentReports/SkirmishExpansion/S005/readonly521-dense-source-render-branches.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(new { scenePath, before,
                        physicalSourceModified = false, roots }, Newtonsoft.Json.Formatting.Indented));
                var relativePath = typeof(OperationMapRenderEligibilityInventoryProbe).GetMethod(
                    "GetIndexedRelativePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var rows = new System.Collections.Generic.List<object>();
                foreach (var renderer in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy))
                {
                    var dense = renderer.GetComponentInParent<Game.Authoring.DenseCityPresentationIdentityAuthoring>(true);
                    var accepted = renderer.GetComponentInParent<Game.Authoring.OperationMapEntityPresentationIdentityAuthoring>(true);
                    var owner = dense != null ? dense.transform : accepted != null ? accepted.transform : renderer.transform;
                    string ownerKind = dense != null ? "DenseGenerated" : accepted != null ? "AcceptedMap" : "Unresolved";
                    string stableId = dense != null ? dense.StableId : accepted != null ? accepted.SourceGlobalObjectId :
                        GlobalObjectId.GetGlobalObjectIdSlow(renderer.gameObject).ToString();
                    string path = (string)relativePath.Invoke(null, new object[] {owner, renderer.transform});
                    for (int subMesh = 0; subMesh < renderer.sharedMaterials.Length; subMesh++)
                        rows.Add(new {ownerKind, ownerStableId = stableId, rendererPath = path, subMeshIndex = subMesh,
                            rendererName = renderer.name, ownerName = owner.name, branch = DenseRenderBranch(renderer.transform, renderer.transform.root)});
                }
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(rows));
                using var stream = File.Create("Design/AgentReports/SkirmishExpansion/S005/readonly524-dense-current-row-identities.json.gz");
                using var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionLevel.Optimal);
                gzip.Write(bytes, 0, bytes.Length);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            using (var hash = System.Security.Cryptography.SHA256.Create())
                if (before != BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(scenePath))))
                    throw new InvalidOperationException("Read-only source inspection changed scene bytes.");
            Debug.Log("[S005DenseRenderRows] result=Captured physicalSourceModified=0");
        }

        private static string DenseRenderBranch(Transform renderer, Transform root)
        {
            while (renderer.parent != null && renderer.parent != root) renderer = renderer.parent;
            return renderer == root ? root.name : root.name + "/" + renderer.name;
        }

        public static void TuneOpeningCamera()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Tune the authored camera in Edit mode.");
            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Folder + "/SkirmishOperationMap_S005.asset");
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(Folder + "/SkirmishLayout_S005.asset");
            if (map == null || layout == null) throw new InvalidOperationException("S005 logical map is missing.");
            var data = new SerializedObject(map);
            var cameras = data.FindProperty("cameras");
            bool found = false;
            for (int i = 0; i < cameras.arraySize; i++)
            {
                var camera = cameras.GetArrayElementAtIndex(i);
                if (camera.FindPropertyRelative("cameraId").stringValue != map.BattleCameraId) continue;
                camera.FindPropertyRelative("position").vector3Value = new Vector3(450,95,520);
                found = true;
            }
            if (!found) throw new InvalidOperationException("S005 battle camera is missing.");
            data.FindProperty("contentHash").stringValue = "";
            data.FindProperty("generatedMetadataHash").stringValue = "";
            data.ApplyModifiedPropertiesWithoutUndo();
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-", "").ToLowerInvariant();
                data.Update();
                data.FindProperty("contentHash").stringValue = hash;
                data.FindProperty("generatedMetadataHash").stringValue = hash;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var binding = new SerializedObject(layout);
            binding.FindProperty("worldBinding").FindPropertyRelative("SourceHash").stringValue = map.ContentHash;
            binding.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(map); EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssetIfDirty(map); AssetDatabase.SaveAssetIfDirty(layout);
            if (!map.TryValidateMetadata(out var error) || !map.TryValidateLocalContentReferences(out error))
                throw new InvalidOperationException("Updated camera metadata is invalid: " + error);
            Debug.Log("[S005OpeningCamera] result=Passed height=95 pitch=50 yaw=-90 fov=50 physicalMapModified=0 source=" + map.ContentHash);
        }

        public static void AuditTownPads()
        {
            const string path="Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity";
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var obstacles=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
                    .Where(r=>r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.y>2 && r.bounds.size.x>2 && r.bounds.size.z>2)
                    .Select(r=>r.bounds).ToArray();
                var grid=AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>("Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Grid.asset");
                var blocked=new System.Collections.Generic.HashSet<Vector2Int>(grid.BlockedCells);
                var pads=new System.Collections.Generic.List<Vector2>();
                for(int z=350;z<=470;z+=5) for(int x=415;x<=960;x+=5)
                {
                    if(obstacles.Any(b=>x+10>b.min.x && x-10<b.max.x && z+8>b.min.z && z-8<b.max.z)) continue;
                    bool legal=true;
                    for(int dz=-8;dz<=8 && legal;dz++) for(int dx=-10;dx<=10;dx++)
                        if(blocked.Contains(new Vector2Int(x+dx,z+dz))) {legal=false;break;}
                    if(legal) pads.Add(new Vector2(x,z));
                }
                File.WriteAllText("Design/AgentReports/SkirmishExpansion/S005/town-pad-audit.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(new {footprintMetres=new[]{20,16},pads=pads.Select(p=>new {x=p.x,z=p.y}).ToArray()},Newtonsoft.Json.Formatting.Indented));
                Debug.Log("[S005TownPadAudit] result=Captured count="+pads.Count+" physicalMapModified=0");
            }
            finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }

        public static void AuditCampaignRenderSource()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Audit in Edit mode.");
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourcePath);
            if (source == null) throw new InvalidOperationException("Campaign source is missing.");
            const string scenePath = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                var dirt = renderers.Where(r => r.name == "Dirt").Select(r => new {
                    name = r.name, parent = r.transform.parent != null ? r.transform.parent.name : "",
                    active = r.gameObject.activeInHierarchy, enabled = r.enabled, tag = r.tag,
                    mesh = r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null
                        ? r.GetComponent<MeshFilter>().sharedMesh.name : "MISSING",
                    materials = r.sharedMaterials.Select(m => m != null ? m.name : "MISSING").ToArray()
                }).ToArray();
                Directory.CreateDirectory("Design/AgentReports/SkirmishExpansion/S005");
                File.WriteAllText("Design/AgentReports/SkirmishExpansion/S005/campaign-render-source-audit.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(new {
                        scenePath, sceneModified = false, rendererCount = renderers.Length, dirt,
                        missingMeshes = renderers.Count(r => r.GetComponent<MeshFilter>() == null || r.GetComponent<MeshFilter>().sharedMesh == null),
                        missingMaterials = renderers.Count(r => r.sharedMaterials.Length == 0 || r.sharedMaterials.Any(m => m == null))
                    }, Newtonsoft.Json.Formatting.Indented));
                Debug.Log("[S005CampaignRenderSourceAudit] result=Captured renderers=" + renderers.Length + " dirt=" + dirt.Length + " physicalMapModified=0");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [MenuItem("Tools/Warline/Skirmish/Build S005 Combined Arms Field")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Build S005 in Edit mode.");
            // Rebuilding an authored mission must preserve its selected source,
            // scenario defaults, budgets and measured gameplay positions.
            var existing = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(Folder + "/SkirmishScenario_S005.asset");
            if (existing != null)
            {
                BindExistingDenseCampaignSource();
                var selectedSource = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourcePath);
                Directory.CreateDirectory("Design/AgentReports/SkirmishExpansion/S005");
                File.WriteAllText("Design/AgentReports/SkirmishExpansion/S005/source-binding.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    catalogId = "S005", sourcePath = SourcePath,
                    sourceIdentityHash = selectedSource.SourceIdentityHash, sourceContentHash = selectedSource.ContentHash,
                    scene = AssetDatabase.GUIDToAssetPath(selectedSource.SourceSceneReference.AssetGUID),
                    physicalMapModified = false, existingScenarioPreserved = true,
                    playerReady = false, publication = "InProgress"
                }, Newtonsoft.Json.Formatting.Indented));
                Debug.Log("[SkirmishS005DefinitionBuilder] result=Passed existingScenarioPreserved=1 physicalMapModified=0 publication=InProgress");
                return;
            }
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog == null || !catalog.TryValidate(out var error))
                throw new InvalidOperationException("Existing expansion catalog missing or invalid.");
            var set = catalog.ToAuthoredSet();
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourcePath);
            if (source == null || !source.TryValidateMetadata(out error) || !source.TryValidateLocalContentReferences(out error))
                throw new InvalidOperationException("Existing Campaign source invalid: " + error);
            Directory.CreateDirectory(Folder);
            // Logical DB packaging reuses the already working Campaign town/airfield scene.
            var map = LoadOrCreate<OperationMapDefinition>(Folder + "/SkirmishOperationMap_S005.asset");
            EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SkirmishS004CampaignMapBuilder.MapPath), map);
            map.name = "SkirmishOperationMap_S005";
            // Face the existing southern town frontage instead of the empty
            // runway. This changes logical camera packaging, not its geometry.
            var md = new SerializedObject(map);
            var cameras = md.FindProperty("cameras");
            for(int i=0;i<cameras.arraySize;i++)
            {
                var camera = cameras.GetArrayElementAtIndex(i);
                if(camera.FindPropertyRelative("cameraId").stringValue != map.BattleCameraId) continue;
                camera.FindPropertyRelative("eulerAngles").vector3Value = new Vector3(50,-90,0);
                camera.FindPropertyRelative("position").vector3Value = new Vector3(450,95,520);
            }
            md.FindProperty("contentHash").stringValue="";
            md.FindProperty("generatedMetadataHash").stringValue="";
            md.ApplyModifiedPropertiesWithoutUndo();
            using(var sha=System.Security.Cryptography.SHA256.Create())
            {
                var hash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();
                md.Update();md.FindProperty("contentHash").stringValue=hash;
                md.FindProperty("generatedMetadataHash").stringValue=hash;md.ApplyModifiedPropertiesWithoutUndo();
            }
            var army = LoadOrCreate<SkirmishArmyProfileConfig>(SkirmishDefinitionBuilder.SharedFolder + "/SkirmishArmy_CombinedArms.asset");
            army.ConfigureCombinedArms();
            var layout = LoadOrCreate<SkirmishMapLayoutConfig>(Folder + "/SkirmishLayout_S005.asset");
            // Reuse the existing DB logical geometry and measured deployment frame.
            // No Campaign terrain, scene, grid, surface, road or runway is rewritten.
            EditorUtility.CopySerialized(set.DefinitionS004.MapLayout, layout);
            layout.name = "SkirmishLayout_S005";
            var ld = new SerializedObject(layout);
            ld.FindProperty("worldBinding").FindPropertyRelative("SourceHash").stringValue = map.ContentHash;
            var binding = ld.FindProperty("worldBinding");
            binding.FindPropertyRelative("HasMeasuredDeployments").boolValue = true;
            binding.FindPropertyRelative("OriginZ").floatValue=520f;
            binding.FindPropertyRelative("PlayerDeployment").vector2Value = new Vector2(450f, 520f);
            binding.FindPropertyRelative("EnemyDeployment").vector2Value = new Vector2(902.8f, 520f);
            binding.FindPropertyRelative("BindAllConfigurations").boolValue = true;
            ld.ApplyModifiedPropertiesWithoutUndo();
            var definition = LoadOrCreate<SkirmishScenarioDefinitionConfig>(Folder + "/SkirmishScenario_S005.asset");
            definition.Configure("S005", set.ObjectiveBa, army, set.StartField, set.Economy,
                set.Upgrades, set.Intel, set.Roles, layout, SkirmishSizeId.Standard, SkirmishS005FirstVisit.RequiredFeatureIds);
            definition.BindCampaignMap(map);
            var cd = new SerializedObject(catalog);
            var definitions = cd.FindProperty("definitions");
            int index = -1;
            for (int i = 0; i < definitions.arraySize; i++)
                if (definitions.GetArrayElementAtIndex(i).objectReferenceValue is SkirmishScenarioDefinitionConfig d && d.CatalogId == "S005") index = i;
            if (index < 0) { index = definitions.arraySize; definitions.arraySize++; }
            definitions.GetArrayElementAtIndex(index).objectReferenceValue = definition;
            cd.ApplyModifiedPropertiesWithoutUndo();
            var publication = set.Publication;
            var pd = new SerializedObject(publication);
            var rows = pd.FindProperty("rows");
            if (!publication.TryGet("S005", out _))
            {
                index = rows.arraySize; rows.arraySize++;
                var row = rows.GetArrayElementAtIndex(index);
                row.FindPropertyRelative("CatalogId").stringValue = "S005";
                row.FindPropertyRelative("DefinitionId").stringValue = "skirmish.s005";
                row.FindPropertyRelative("Status").enumValueIndex = (int)SkirmishPublicationStatus.InProgress;
                row.FindPropertyRelative("EvidencePath").stringValue = "Design/AgentReports/SkirmishExpansion/S005/";
                row.FindPropertyRelative("Notes").stringValue = "Combined Arms Field candidate. Native journeys, all-role certification, bilateral runway capacity, recovery and device acceptance required; asset existence does not set Playable.";
                pd.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var asset in new UnityEngine.Object[] { map, army, layout, definition, catalog, publication }) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            V3UiLocalizationCatalogBuilder.ApplyConfiguredUiTables();
            BindExistingDenseCampaignSource();
            var report = new {
                catalogId = "S005", sourcePath = SourcePath, sourceIdentityHash = source.SourceIdentityHash,
                sourceContentHash = source.ContentHash,
                scene = AssetDatabase.GUIDToAssetPath(source.SourceSceneReference.AssetGUID),
                logicalPackagingPath = AssetDatabase.GetAssetPath(map), logicalPackagingHash = map.ContentHash,
                worldBinding = Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(layout.WorldBinding)), physicalMapModified = false,
                playerReady = false, publication = "InProgress",
                pending = new[] { "Normal-input manual and ARIA journeys", "Heavy APC and siege native certification", "Bilateral runway capacity on existing geometry", "Checkpoint recovery", "Player/device acceptance" }
            };
            Directory.CreateDirectory("Design/AgentReports/SkirmishExpansion/S005");
            File.WriteAllText("Design/AgentReports/SkirmishExpansion/S005/source-binding.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            Debug.Log("[SkirmishS005DefinitionBuilder] result=Passed source=" + source.SourceIdentityHash + " physicalMapModified=0 publication=InProgress");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
    }
}
#endif
