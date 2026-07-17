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
        private const string QualityAuditDirectory = "Logs/M01_QualityAudit";

        private readonly struct QualityCaptureDefinition
        {
            public QualityCaptureDefinition(string name, Vector3 position, Vector3 target, float fieldOfView)
            {
                Name = name;
                Position = position;
                Target = target;
                FieldOfView = fieldOfView;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
            public float FieldOfView { get; }
        }

        private readonly struct QualityOwnerRecord
        {
            public QualityOwnerRecord(Transform owner, Bounds bounds, bool authored)
            {
                Owner = owner;
                Bounds = bounds;
                Authored = authored;
            }

            public Transform Owner { get; }
            public Bounds Bounds { get; }
            public bool Authored { get; }
        }

        private readonly struct QualityAuditResult
        {
            public QualityAuditResult(
                int structureCount,
                int authoredImportedOverlaps,
                int authoredAuthoredOverlaps,
                int roadStructureOverlaps,
                int buriedStructures,
                int floatingAuthoredStructures,
                int unsupportedPrimaryAssemblies)
            {
                StructureCount = structureCount;
                AuthoredImportedOverlaps = authoredImportedOverlaps;
                AuthoredAuthoredOverlaps = authoredAuthoredOverlaps;
                RoadStructureOverlaps = roadStructureOverlaps;
                BuriedStructures = buriedStructures;
                FloatingAuthoredStructures = floatingAuthoredStructures;
                UnsupportedPrimaryAssemblies = unsupportedPrimaryAssemblies;
            }

            public int StructureCount { get; }
            public int AuthoredImportedOverlaps { get; }
            public int AuthoredAuthoredOverlaps { get; }
            public int RoadStructureOverlaps { get; }
            public int BuriedStructures { get; }
            public int FloatingAuthoredStructures { get; }
            public int UnsupportedPrimaryAssemblies { get; }
            public int ObviousViolationCount =>
                AuthoredImportedOverlaps +
                AuthoredAuthoredOverlaps +
                RoadStructureOverlaps +
                BuriedStructures +
                FloatingAuthoredStructures +
                UnsupportedPrimaryAssemblies;
        }

        private static readonly QualityCaptureDefinition[] QualityCaptureDefinitions =
        {
            new("compound_south_gate", new Vector3(14f, 9f, 2f), new Vector3(42f, 2.2f, 28f), 42f),
            new("compound_northwest_interior", new Vector3(76f, 12f, 3f), new Vector3(45f, 2.2f, 27f), 44f),
            new("market_street_ground", new Vector3(0f, 12f, 45f), new Vector3(5f, 1.2f, 18f), 44f),
            new("old_market_interior", new Vector3(-98f, 14f, 54f), new Vector3(-58f, 3f, 34f), 45f),
            new("residential_north", new Vector3(36f, 11f, -28f), new Vector3(0f, 2.2f, -65f), 48f),
            new("residential_south", new Vector3(-44f, 11f, -112f), new Vector3(-5f, 2.2f, -70f), 48f),
            new("civilian_frontage", new Vector3(-5f, 9f, -35f), new Vector3(-28f, 1.8f, -15f), 48f),
            new("aftermath_road", new Vector3(31f, 8f, -24f), new Vector3(4f, 1.5f, -3f), 40f)
        };

        public static int GetObviousQualityViolationCount()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            return AuditObviousQuality(sceneRoot, false, null).ObviousViolationCount;
        }

        public static void CaptureQualityAuditBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            string outputDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", QualityAuditDirectory));
            Directory.CreateDirectory(outputDirectory);
            string reportPath = Path.Combine(outputDirectory, "m01_quality_audit.md");
            var report = new List<string>(128)
            {
                "# M01 Close Quality Audit",
                string.Empty,
                $"- Generator: `{GeneratorVersion}`",
                $"- Scene: `{ScenePath}`",
                string.Empty,
                "## Automated Findings",
                string.Empty
            };

            QualityAuditResult result = AuditObviousQuality(sceneRoot, true, report);
            report.Add(string.Empty);
            report.Add("## Close Captures");
            report.Add(string.Empty);

            GameObject cameraObject = new("M01_QualityAuditCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1200f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            EnableUrpPostProcessing(cameraObject);

            try
            {
                for (int index = 0; index < QualityCaptureDefinitions.Length; index++)
                {
                    QualityCaptureDefinition definition = QualityCaptureDefinitions[index];
                    camera.fieldOfView = definition.FieldOfView;
                    camera.transform.position = definition.Position;
                    camera.transform.rotation = Quaternion.LookRotation(
                        (definition.Target - definition.Position).normalized,
                        Vector3.up);
                    string fileName = $"{index + 1:00}_{definition.Name}.png";
                    CaptureCamera(camera, Path.Combine(outputDirectory, fileName));
                    report.Add($"- `{fileName}`");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            report.Insert(8, $"- Structures inspected: `{result.StructureCount}`");
            report.Insert(9, $"- Authored/imported overlaps: `{result.AuthoredImportedOverlaps}`");
            report.Insert(10, $"- Authored/authored overlaps: `{result.AuthoredAuthoredOverlaps}`");
            report.Insert(11, $"- Local-road/structure overlaps: `{result.RoadStructureOverlaps}`");
            report.Insert(12, $"- Fully buried structures: `{result.BuriedStructures}`");
            report.Insert(13, $"- Floating authored structures: `{result.FloatingAuthoredStructures}`");
            report.Insert(14, $"- Unsupported primary assemblies: `{result.UnsupportedPrimaryAssemblies}`");
            report.Insert(15, $"- Obvious violations: `{result.ObviousViolationCount}`");
            File.WriteAllLines(reportPath, report);
            Debug.Log(
                $"[M01QualityAudit] result={(result.ObviousViolationCount == 0 ? "Passed" : "Failed")} " +
                $"structures={result.StructureCount} authoredImported={result.AuthoredImportedOverlaps} " +
                $"authoredAuthored={result.AuthoredAuthoredOverlaps} roads={result.RoadStructureOverlaps} " +
                $"buried={result.BuriedStructures} floating={result.FloatingAuthoredStructures} " +
                $"unsupportedAssemblies={result.UnsupportedPrimaryAssemblies} " +
                $"captures={QualityCaptureDefinitions.Length} output={outputDirectory}");
        }

        private static QualityAuditResult AuditObviousQuality(
            GameObject sceneRoot,
            bool logDetails,
            List<string> report)
        {
            List<QualityOwnerRecord> structures = CollectQualityStructures(sceneRoot);
            int authoredImported = 0;
            int authoredAuthored = 0;
            for (int firstIndex = 0; firstIndex < structures.Count; firstIndex++)
            {
                QualityOwnerRecord first = structures[firstIndex];
                for (int secondIndex = firstIndex + 1; secondIndex < structures.Count; secondIndex++)
                {
                    QualityOwnerRecord second = structures[secondIndex];
                    if (!first.Authored && !second.Authored)
                        continue;
                    if (!HasMeaningfulStructureOverlap(first.Bounds, second.Bounds))
                        continue;

                    if (first.Authored && second.Authored)
                        authoredAuthored++;
                    else
                        authoredImported++;
                    RecordQualityFinding(
                        logDetails,
                        report,
                        "structure-overlap",
                        $"first={GetTransformPath(first.Owner, sceneRoot.transform)} " +
                        $"second={GetTransformPath(second.Owner, sceneRoot.transform)}");
                }
            }

            int roadOverlaps = CountAllLocalRoadStructureOverlaps(sceneRoot, structures, logDetails, report);
            int buried = 0;
            int floating = 0;
            for (int index = 0; index < structures.Count; index++)
            {
                QualityOwnerRecord structure = structures[index];
                if (structure.Bounds.max.y <= 0.15f)
                {
                    buried++;
                    RecordQualityFinding(
                        logDetails,
                        report,
                        "buried-structure",
                        $"owner={GetTransformPath(structure.Owner, sceneRoot.transform)} bounds={structure.Bounds}");
                }
                else if (structure.Authored && structure.Bounds.min.y >= 0.5f)
                {
                    floating++;
                    RecordQualityFinding(
                        logDetails,
                        report,
                        "floating-authored-structure",
                        $"owner={GetTransformPath(structure.Owner, sceneRoot.transform)} bounds={structure.Bounds}");
                }
            }

            int unsupportedAssemblies = CountUnsupportedPrimaryAssemblies(sceneRoot, logDetails, report);
            return new QualityAuditResult(
                structures.Count,
                authoredImported,
                authoredAuthored,
                roadOverlaps,
                buried,
                floating,
                unsupportedAssemblies);
        }

        private static List<QualityOwnerRecord> CollectQualityStructures(GameObject sceneRoot)
        {
            var records = new List<QualityOwnerRecord>(96);
            Transform generated = sceneRoot.transform.Find("_M01VisualGenerated");
            Transform modulesRoot = generated?.Find("02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                return records;

            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform compositionRoot = FindDistrictCompositionRoot(modulesRoot.GetChild(moduleIndex));
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy || !ContainsDescendantName(owner, "_Bld_"))
                        continue;
                    if (TryGetCombinedBounds(owner.GetComponentsInChildren<Renderer>(true), out Bounds bounds))
                        records.Add(new QualityOwnerRecord(owner, bounds, false));
                }
            }

            for (int layerIndex = 0; layerIndex < generated.childCount; layerIndex++)
            {
                Transform layer = generated.GetChild(layerIndex);
                if (!IsAuthoredQualityLayer(layer.name))
                    continue;
                for (int childIndex = 0; childIndex < layer.childCount; childIndex++)
                {
                    Transform owner = layer.GetChild(childIndex);
                    if (!owner.gameObject.activeInHierarchy || !IsAuthoredStructure(owner))
                        continue;
                    if (TryGetCombinedBounds(owner.GetComponentsInChildren<Renderer>(true), out Bounds bounds))
                        records.Add(new QualityOwnerRecord(owner, bounds, true));
                }
            }

            return records;
        }

        private static int CountAllLocalRoadStructureOverlaps(
            GameObject sceneRoot,
            IReadOnlyList<QualityOwnerRecord> structures,
            bool logDetails,
            List<string> report)
        {
            int overlapCount = 0;
            for (int structureIndex = 0; structureIndex < structures.Count; structureIndex++)
            {
                QualityOwnerRecord structure = structures[structureIndex];
                for (int roadIndex = 0; roadIndex < LocalRoadSegments.Length; roadIndex++)
                {
                    LocalRoadSegmentDefinition road = LocalRoadSegments[roadIndex];
                    if (!IntersectsRoadCorridor(
                            structure.Bounds,
                            road,
                            (LocalRoadWidth + LocalRoadShoulderAllowance) * 0.5f))
                    {
                        continue;
                    }

                    overlapCount++;
                    RecordQualityFinding(
                        logDetails,
                        report,
                        "road-structure-overlap",
                        $"road={road.Name} owner={GetTransformPath(structure.Owner, sceneRoot.transform)}");
                }
            }

            return overlapCount;
        }

        private static int CountUnsupportedPrimaryAssemblies(
            GameObject sceneRoot,
            bool logDetails,
            List<string> report)
        {
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                return 1;

            int unsupportedCount = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform compositionRoot = FindDistrictCompositionRoot(module);
                var supportSurfaces = new List<Bounds>(96);
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy)
                        continue;

                    string category = ClassifyDistrictCompositionOwner(owner);
                    bool supportsStructure = ContainsImportedGroundSurfaceName(owner) ||
                                             string.Equals(category, "road", StringComparison.Ordinal) ||
                                             string.Equals(category, "major-road", StringComparison.Ordinal) ||
                                             ContainsPenetratingTerrainName(owner);
                    if (supportsStructure &&
                        TryGetCombinedBounds(owner.GetComponentsInChildren<Renderer>(true), out Bounds supportBounds))
                    {
                        supportSurfaces.Add(supportBounds);
                    }
                }

                Transform[] transforms = module.GetComponentsInChildren<Transform>(true);
                for (int candidateIndex = 0; candidateIndex < transforms.Length; candidateIndex++)
                {
                    Transform candidate = transforms[candidateIndex];
                    if (!candidate.gameObject.activeInHierarchy ||
                        !IsDistrictBuildingAssemblyCandidate(candidate, module) ||
                        !TryGetCombinedBounds(candidate.GetComponentsInChildren<Renderer>(true), out Bounds bounds) ||
                        bounds.min.y <= 0.4f)
                    {
                        continue;
                    }

                    bool supported = false;
                    for (int supportIndex = 0; supportIndex < supportSurfaces.Count; supportIndex++)
                    {
                        Bounds support = supportSurfaces[supportIndex];
                        float overlapX = Mathf.Min(bounds.max.x, support.max.x) - Mathf.Max(bounds.min.x, support.min.x);
                        float overlapZ = Mathf.Min(bounds.max.z, support.max.z) - Mathf.Max(bounds.min.z, support.min.z);
                        float verticalGap = bounds.min.y - support.max.y;
                        if (overlapX >= 0.5f && overlapZ >= 0.5f && verticalGap >= -0.35f && verticalGap <= 0.45f)
                        {
                            supported = true;
                            break;
                        }
                    }

                    if (supported)
                        continue;

                    unsupportedCount++;
                    RecordQualityFinding(
                        logDetails,
                        report,
                        "unsupported-primary-assembly",
                        $"owner={GetTransformPath(candidate, sceneRoot.transform)} bottom={bounds.min.y:0.00}");
                }
            }

            return unsupportedCount;
        }

        private static bool IsAuthoredQualityLayer(string layerName)
        {
            return layerName.StartsWith("03_", StringComparison.Ordinal) ||
                   layerName.StartsWith("04_", StringComparison.Ordinal) ||
                   layerName.StartsWith("05_", StringComparison.Ordinal) ||
                   layerName.StartsWith("06_", StringComparison.Ordinal);
        }

        private static bool IsAuthoredStructure(Transform owner)
        {
            string prefabPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(owner.gameObject);
            return ContainsName(prefabPath, "/Buildings/") ||
                   ContainsName(owner.name, "House") ||
                   ContainsName(owner.name, "Hall") ||
                   ContainsName(owner.name, "Building") ||
                   ContainsName(owner.name, "Tower") ||
                   ContainsName(owner.name, "Ruin") ||
                   ContainsName(owner.name, "Tent") ||
                   ContainsName(owner.name, "Archway") ||
                   ContainsName(owner.name, "Wall");
        }

        private static void RecordQualityFinding(
            bool logDetails,
            List<string> report,
            string kind,
            string details)
        {
            if (logDetails)
                Debug.LogWarning($"[M01QualityFinding] kind={kind} {details}");
            report?.Add($"- `{kind}`: `{details}`");
        }
    }
#endif
}
