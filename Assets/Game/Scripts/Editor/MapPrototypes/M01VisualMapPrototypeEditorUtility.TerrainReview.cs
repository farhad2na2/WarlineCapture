namespace Game.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static partial class M01VisualMapPrototypeEditorUtility
    {
        private const int TerrainContactReviewPageSize = 8;

        private readonly struct TerrainContactReviewRecord
        {
            public TerrainContactReviewRecord(Transform module, Transform structure, Bounds reviewBounds, int contactCount)
            {
                Module = module;
                Structure = structure;
                ReviewBounds = reviewBounds;
                ContactCount = contactCount;
            }

            public Transform Module { get; }
            public Transform Structure { get; }
            public Bounds ReviewBounds { get; }
            public int ContactCount { get; }
        }

        public static void CaptureTerrainContactReviewBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            List<TerrainContactReviewRecord> records = CollectTerrainContactReviewRecords(sceneRoot);
            records.Sort(CompareTerrainContactReviewRecords);
            int captureCount = records.Count;
            if (captureCount == 0)
                throw new InvalidOperationException("M01 terrain-contact review found no primary-structure contacts.");

            string outputDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Logs", "M01_R73_TerrainContactReview"));
            Directory.CreateDirectory(outputDirectory);
            var capturePaths = new List<string>(captureCount);
            var contactSheetPaths = new List<string>((captureCount + TerrainContactReviewPageSize - 1) / TerrainContactReviewPageSize);
            GameObject cameraObject = new("M01_TerrainContactReviewCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ConfigureTerrainContactReviewCamera(cameraObject, camera);

            try
            {
                for (int index = 0; index < captureCount; index++)
                {
                    TerrainContactReviewRecord record = records[index];
                    FrameTerrainContactReview(camera, record.ReviewBounds, index);
                    string path = Path.Combine(outputDirectory, $"contact_{index + 1:00}.png");
                    CaptureCamera(camera, path);
                    capturePaths.Add(path);
                    Debug.Log(
                        $"[M01TerrainContactReview] rank={index + 1} module={record.Module.name} " +
                        $"structure={record.Structure.name} contacts={record.ContactCount} " +
                        $"center={record.ReviewBounds.center} size={record.ReviewBounds.size}");

                    bool pageComplete = capturePaths.Count % TerrainContactReviewPageSize == 0 ||
                                        capturePaths.Count == captureCount;
                    if (pageComplete)
                    {
                        int pageIndex = (capturePaths.Count - 1) / TerrainContactReviewPageSize;
                        int pageStart = pageIndex * TerrainContactReviewPageSize;
                        int pageCount = capturePaths.Count - pageStart;
                        string contactSheetPath = Path.Combine(
                            outputDirectory,
                            $"m01_r73_terrain_contact_review_page_{pageIndex + 1:00}.png");
                        CreateContactSheet(contactSheetPath, capturePaths.GetRange(pageStart, pageCount));
                        contactSheetPaths.Add(contactSheetPath);
                    }
                }

                WriteTerrainContactReviewManifest(outputDirectory, records, contactSheetPaths);
                Debug.Log(
                    $"[M01TerrainContactReview] result=Passed candidates={records.Count} " +
                    $"captured={captureCount} pages={contactSheetPaths.Count} output={outputDirectory}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static List<TerrainContactReviewRecord> CollectTerrainContactReviewRecords(GameObject sceneRoot)
        {
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                throw new InvalidOperationException("M01 district module root is missing.");

            var records = new List<TerrainContactReviewRecord>();
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform compositionRoot = FindDistrictCompositionRoot(module);
                var structures = new List<CompositionBoundsRecord>();
                var terrain = new List<CompositionBoundsRecord>();
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy)
                        continue;

                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                        continue;

                    if (IsPrimaryStructureOwner(owner))
                        structures.Add(new CompositionBoundsRecord(owner, bounds));
                    else if (ContainsPenetratingTerrainName(owner) && Mathf.Max(bounds.size.x, bounds.size.z) >= 3f)
                        terrain.Add(new CompositionBoundsRecord(owner, bounds));
                }

                for (int structureIndex = 0; structureIndex < structures.Count; structureIndex++)
                {
                    CompositionBoundsRecord structure = structures[structureIndex];
                    Bounds reviewBounds = structure.Bounds;
                    int contactCount = 0;
                    for (int terrainIndex = 0; terrainIndex < terrain.Count; terrainIndex++)
                    {
                        CompositionBoundsRecord terrainRecord = terrain[terrainIndex];
                        if (!HasMeaningfulTerrainStructureOverlap(structure.Bounds, terrainRecord.Bounds))
                            continue;

                        reviewBounds.Encapsulate(terrainRecord.Bounds);
                        contactCount++;
                    }

                    if (contactCount > 0)
                    {
                        records.Add(new TerrainContactReviewRecord(
                            module,
                            structure.Owner,
                            reviewBounds,
                            contactCount));
                    }
                }
            }

            return records;
        }

        private static int CompareTerrainContactReviewRecords(
            TerrainContactReviewRecord first,
            TerrainContactReviewRecord second)
        {
            int contactComparison = second.ContactCount.CompareTo(first.ContactCount);
            if (contactComparison != 0)
                return contactComparison;

            int moduleComparison = string.Compare(first.Module.name, second.Module.name, StringComparison.Ordinal);
            return moduleComparison != 0
                ? moduleComparison
                : string.Compare(first.Structure.name, second.Structure.name, StringComparison.Ordinal);
        }

        private static void ConfigureTerrainContactReviewCamera(GameObject cameraObject, Camera camera)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1200f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            EnableUrpPostProcessing(cameraObject);
        }

        private static void FrameTerrainContactReview(Camera camera, Bounds bounds, int captureIndex)
        {
            Vector3 viewDirection = captureIndex % 2 == 0
                ? new Vector3(-1f, 0.72f, -1f).normalized
                : new Vector3(1f, 0.72f, -1f).normalized;
            float halfFieldOfView = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = Mathf.Max(12f, bounds.extents.magnitude / Mathf.Tan(halfFieldOfView) * 1.25f);
            Vector3 target = bounds.center + Vector3.up * Mathf.Min(0.6f, bounds.extents.y * 0.15f);
            camera.transform.position = target + viewDirection * distance;
            camera.transform.rotation = Quaternion.LookRotation((target - camera.transform.position).normalized, Vector3.up);
        }

        private static void WriteTerrainContactReviewManifest(
            string outputDirectory,
            IReadOnlyList<TerrainContactReviewRecord> records,
            IReadOnlyList<string> contactSheetPaths)
        {
            string manifestPath = Path.Combine(outputDirectory, "m01_r73_terrain_contact_review.md");
            var lines = new List<string>(records.Count + contactSheetPaths.Count + 9)
            {
                "# M01 R73 Terrain Contact Review",
                string.Empty,
                $"- Source generator: `{GeneratorVersion}`",
                $"- Source scene: `{ScenePath}`",
                $"- Ranked primary structures: `{records.Count}`",
                $"- Captured: `{records.Count}`",
                $"- Contact-sheet pages: `{contactSheetPaths.Count}`",
                string.Empty
            };

            for (int pageIndex = 0; pageIndex < contactSheetPaths.Count; pageIndex++)
                lines.Add($"- Page {pageIndex + 1}: `{Path.GetFileName(contactSheetPaths[pageIndex])}`");

            lines.Add(string.Empty);
            for (int index = 0; index < records.Count; index++)
            {
                TerrainContactReviewRecord record = records[index];
                lines.Add(
                    $"- `contact_{index + 1:00}.png`: `{record.Module.name}` / " +
                    $"`{record.Structure.name}` / contacts `{record.ContactCount}`");
            }

            File.WriteAllLines(manifestPath, lines);
        }
    }
#endif
}
