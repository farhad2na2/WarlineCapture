#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class NonUiSystemBaseMigrationArchitectureTests
{
    private const string GameScriptsRoot = "Assets/Game/Scripts";
    private const string InventoryPath = "Design/Architecture/systembase_to_isystem_inventory.md";
    private const string MonoBehaviourLoopBaselinePath = "Design/Architecture/phase7_monobehaviour_loop_baseline.md";
    private const int ManagedExceptionPlanningCap = 30;
    private const int FinalProductionDeclarationCount = 211;
    private const int FinalProductionNonUiCount = 189;
    private const int FinalProductionUiCount = 22;
    private const int FinalProductionSystemBaseCount = 25;
    private const int FinalProductionISystemCount = 186;
    private const int FinalConvertedCount = 164;
    private const int FinalManagedExceptionCount = 25;
    private const int FinalUiOutOfScopeCount = 22;

    private static readonly Regex TypeDeclarationRegex = new(
        @"^[ \t]*(?:(?:\[[^\]\r\n]*(?:\r?\n[ \t]*\[[^\]\r\n]*)*\][ \t]*)\r?\n[ \t]*)*" +
        @"(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|unsafe)\s+)*" +
        @"(?<kind>class|struct)\s+" +
        @"(?<name>[A-Za-z_]\w*)" +
        @"(?:\s*<[^>{};\r\n]+>)?" +
        @"\s*(?<bases>:[^{;]+)?",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex CurrentBaseRegex = new(
        @"\b(ISystem|SystemBase|ComponentSystemBase|ComponentSystem|JobComponentSystem)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManagedBlockerRegex = new(
        @"\b(GameObject|Camera|UnityEngine\.Object|ScriptableObject|Material|Renderer|Light|ParticleSystem|LineRenderer|VisualEffect|MonoBehaviour|Coroutine)\b|" +
        @"\bResources\s*\.|\b(?:Object|UnityEngine\.Object)\.(?:Instantiate|Destroy)\s*\(|" +
        @"\b(?:GameObject|Object|UnityEngine\.Object)\.Find[A-Za-z0-9_]*\s*\(|\bFindObject[A-Za-z0-9_]*\s*\(|" +
        @"\bCamera\.main\b|\bStartCoroutine\s*\(|\bStopCoroutine\s*\(|" +
        @"\bList\s*<\s*GameObject\s*>|\bDictionary\s*<[^>\r\n]*GameObject",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManagedTransformTypeRegex = new(
        @"\bUnityEngine\.Transform\b|" +
        @"(?<![A-Za-z0-9_.])Transform\s*(?:\[\s*\])?\s+[A-Za-z_]\w*|" +
        @"(?:<|,)\s*Transform\s*(?=[>,])|" +
        @"\b(?:typeof|nameof)\s*\(\s*Transform\s*\)|" +
        @"\(\s*Transform\s*\)|" +
        @"\b(?:is|as)\s+Transform\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex UnmanagedEcsObjectReferenceRegex = new(
        @"\bUnityObjectRef\s*<\s*GameObject\s*>",
        RegexOptions.CultureInvariant);

    private static readonly Regex GameplayPolicyRegex = new(
        @"\b(Damage|Health|Attack|Combat|Path|MoveOrder|Selection|BuildingPlacement|Production|Economy|Resource|Validate|Command)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex MonoBehaviourLoopRegex = new(
        @"\b(?:void\s+(?:Update|LateUpdate|FixedUpdate)\s*\(|IEnumerator\s+[A-Za-z_]\w*\s*\()",
        RegexOptions.CultureInvariant);

    private static readonly Regex MonoBehaviourLoopMethodRegex = new(
        @"^[ \t]*(?:(?:public|internal|private|protected|static|virtual|override|sealed|async)\s+)*(?:(?:void\s+(?<update>Update|LateUpdate|FixedUpdate))|(?:IEnumerator\s+(?<coroutine>[A-Za-z_]\w*)))\s*\(",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex PublicMemberNameRegex = new(
        @"(?<name>[A-Za-z_]\w*)\s+\((?:method|property)\)",
        RegexOptions.CultureInvariant);

    private static readonly HashSet<string> AgentLanes = new(StringComparer.Ordinal)
    {
        "AgentB",
        "AgentC",
        "AgentD",
        "AgentE",
        "AgentF",
        "Integration"
    };

    private static readonly HashSet<string> ReviewedManagedPresentationPolicyMixRows = new(StringComparer.Ordinal)
    {
        // These rows consume ECS VFX request entities whose type names contain combat
        // vocabulary, but the systems only unwrap UnityObjectRef<GameObject> values and
        // play authored visual effects at the presentation boundary.
        "P7-0283",
        "P7-0284",
        // This row consumes selected-unit marker state whose names contain selection
        // vocabulary, but only creates ECS object-outline render entities/materials.
        "P7-0383"
    };

    private static readonly HashSet<string> ReviewedConvertedBakingBoundaryRows = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Rendering/Baking/OperationMapRenderMaterialBaseColorBakingSystem.cs|OperationMapRenderMaterialBaseColorBakingSystem|struct|ISystem",
        "Assets/Game/Scripts/Rendering/Baking/OperationMapRenderVirtualizationBakingSystem.cs|OperationMapRenderVirtualizationBakingSystem|struct|ISystem"
    };

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new NonUiSystemBaseMigrationArchitectureTests();
            tests.InventoryFileExistsAndRowsParse();
            tests.EveryProductionNonUiEcsDeclarationHasExactlyOneInventoryRow();
            tests.EveryProductionNonUiSystemBaseHasExactlyOneInventoryRow();
            tests.InventoryRowsPointToExistingFilesAndUniqueTypes();
            tests.ConvertedRowsCannotRegainSystemBase();
            tests.ConvertedRowsAvoidManagedUnityObjectBlockers();
            tests.ManagedPresentationExceptionsStayPresentationOnly();
            tests.NoNewMonoBehaviourRuntimeLoopsOutsideBaseline();
            tests.ViewReferenceOnlyMonoBehaviourRowsHaveNoRuntimeLoops();
            tests.BroadConvertedISystemRowsAreListedForReview();
            tests.ConvertedPublicHelperApisRemainTrackedAsDebt();
            tests.ProductionNonUiSystemBaseRowsStayOpenOrManagedExceptionDebt();
            tests.OwnerLaneNamesMatchAgentTrackers();
            tests.ManagedExceptionCountStaysUnderPlanningCap();
            tests.FinalShareCanBeComputedFromInventoryCounts();
            tests.DeliberateSystemBaseViolationIsDetectedByInventoryKeyGuard();
            tests.DeliberateMonoBehaviourLoopViolationIsDetected();
            tests.DeliberateManagedExceptionPolicyViolationIsDetected();
            tests.UnmanagedEcsReferenceShapesAreNotManagedBlockers();
            Debug.Log("[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=19");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[NonUiSystemBaseMigrationArchitectureValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void InventoryFileExistsAndRowsParse()
    {
        List<InventoryRow> rows = LoadInventoryRows();

        Assert.Greater(rows.Count, 0, "The Phase 7 SystemBase-to-ISystem inventory must contain rows.");
        Assert.IsEmpty(
            rows.Where(row => string.IsNullOrWhiteSpace(row.Id) ||
                              string.IsNullOrWhiteSpace(row.Type) ||
                              string.IsNullOrWhiteSpace(row.Path) ||
                              string.IsNullOrWhiteSpace(row.Scope) ||
                              string.IsNullOrWhiteSpace(row.OwnerLane) ||
                              string.IsNullOrWhiteSpace(row.Disposition) ||
                              string.IsNullOrWhiteSpace(row.Status))
                .Select(row => row.Raw)
                .ToArray(),
            "Inventory rows must populate id, type, path, scope, owner lane, disposition, and status.");

        string[] duplicateIds = rows
            .GroupBy(row => row.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.IsEmpty(duplicateIds, "Inventory ids must be unique.");
    }

    [Test]
    public void EveryProductionNonUiSystemBaseHasExactlyOneInventoryRow()
    {
        Dictionary<string, InventoryRow> inventory = LoadInventoryRows()
            .Where(row => row.Scope == "ProductionNonUI")
            .ToDictionary(row => row.Key, row => row, StringComparer.Ordinal);
        string[] missing = EnumerateCurrentDeclarations()
            .Where(declaration => declaration.Scope == "ProductionNonUI")
            .Where(declaration => IsSystemBaseLike(declaration.CurrentBase))
            .Select(declaration => declaration.Key)
            .Where(key => !inventory.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            missing,
            "Every production non-UI SystemBase/legacy ECS system must have exactly one Phase 7 inventory row. Missing:\n" +
            string.Join(Environment.NewLine, missing));
    }

    [Test]
    public void EveryProductionNonUiEcsDeclarationHasExactlyOneInventoryRow()
    {
        Dictionary<string, InventoryRow> inventory = LoadInventoryRows()
            .Where(row => row.Scope == "ProductionNonUI")
            .ToDictionary(row => row.Key, row => row, StringComparer.Ordinal);
        string[] missing = EnumerateCurrentDeclarations()
            .Where(declaration => declaration.Scope == "ProductionNonUI")
            .Select(declaration => declaration.Key)
            .Where(key => !inventory.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            missing,
            "Every production non-UI ECS declaration must have exactly one Phase 7 inventory row. Regenerate the inventory after adding ECS systems:\n" +
            string.Join(Environment.NewLine, missing));
    }

    [Test]
    public void InventoryRowsPointToExistingFilesAndUniqueTypes()
    {
        List<InventoryRow> rows = LoadInventoryRows();
        string[] stale = rows
            .Where(row => !File.Exists(row.Path))
            .Select(row => $"{row.Id} {row.Path} {row.Type}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] duplicateKeys = rows
            .GroupBy(row => row.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(stale, "Inventory rows must not point to deleted or renamed files:\n" + string.Join(Environment.NewLine, stale));
        Assert.IsEmpty(duplicateKeys, "Inventory rows must not duplicate the same type/path/base:\n" + string.Join(Environment.NewLine, duplicateKeys));
    }

    [Test]
    public void ConvertedRowsCannotRegainSystemBase()
    {
        Dictionary<string, CurrentDeclaration> current = EnumerateCurrentDeclarations()
            .ToDictionary(declaration => declaration.Key, declaration => declaration, StringComparer.Ordinal);
        string[] violations = LoadInventoryRows()
            .Where(row => row.Status == "Converted")
            .Where(row => !current.TryGetValue(row.Key, out CurrentDeclaration declaration) || declaration.CurrentBase != "ISystem")
            .Select(row => $"{row.Id} {row.Path} {row.Type} current={GetCurrentBaseOrMissing(current, row.Key)}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Rows marked Converted must remain ISystem and must not regress to SystemBase:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void ConvertedRowsAvoidManagedUnityObjectBlockers()
    {
        string[] invalidReviewedBakingBoundaries = LoadInventoryRows()
            .Where(row => ReviewedConvertedBakingBoundaryRows.Contains(row.Key))
            .Where(row => row.Status != "Converted" ||
                          !File.ReadAllText(row.Path).Contains(
                              "WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)",
                              StringComparison.Ordinal) ||
                          !File.ReadAllText(row.Path).Contains(
                              "UpdateInGroup(typeof(PostBakingSystemGroup))",
                              StringComparison.Ordinal))
            .Select(row => row.Key)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.AreEqual(
            ReviewedConvertedBakingBoundaryRows.Count,
            LoadInventoryRows().Count(row => ReviewedConvertedBakingBoundaryRows.Contains(row.Key)),
            "Every reviewed converted baking boundary must retain exactly one inventory row.");
        Assert.IsEmpty(
            invalidReviewedBakingBoundaries,
            "Reviewed converted baking boundaries must remain baking-world-only PostBaking ISystem owners.");

        string[] violations = LoadInventoryRows()
            .Where(row => row.Status == "Converted")
            .Where(row => row.ManagedBlockers != "None" || HasManagedBlocker(ReadDeclarationBody(row)))
            .Where(row => !ReviewedConvertedBakingBoundaryRows.Contains(row.Key))
            .Select(row => $"{row.Id} {row.Path} {row.Type} blockers={row.ManagedBlockers}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Converted ISystem rows must not carry managed Unity-object blockers. Mark the row ReviewRequired/SplitThenConvert instead:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void ManagedPresentationExceptionsStayPresentationOnly()
    {
        string[] violations = LoadInventoryRows()
            .Where(row => row.Disposition == "ManagedPresentationSystemBaseException")
            .Where(row => row.ManagedBlockers == "None" ||
                          (row.GameplayPolicyRisk.StartsWith("High", StringComparison.Ordinal) && !ReviewedManagedPresentationPolicyMixRows.Contains(row.Id)) ||
                          (HasHighRiskPolicyMix(ReadDeclarationBody(row)) && !ReviewedManagedPresentationPolicyMixRows.Contains(row.Id)))
            .Select(row => $"{row.Id} {row.Path} {row.Type} blockers={row.ManagedBlockers} risk={row.GameplayPolicyRisk}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Managed presentation/config/camera exceptions require concrete Unity-object blockers and must not own gameplay policy:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void ViewReferenceOnlyMonoBehaviourRowsHaveNoRuntimeLoops()
    {
        string[] violations = LoadInventoryRows()
            .Where(row => row.Disposition == "ViewReferenceOnlyMonoBehaviour")
            .Where(row => MonoBehaviourLoopRegex.IsMatch(File.ReadAllText(row.Path)))
            .Select(row => $"{row.Id} {row.Path} {row.Type}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "View/reference-only MonoBehaviour rows must not own Update/LateUpdate/FixedUpdate/coroutine loops:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void NoNewMonoBehaviourRuntimeLoopsOutsideBaseline()
    {
        HashSet<string> baseline = LoadMonoBehaviourLoopBaselineKeys();
        string[] violations = EnumerateCurrentMonoBehaviourLoops()
            .Where(entry => !baseline.Contains(entry.Key))
            .Select(entry => $"{entry.Path} {entry.Type}.{entry.Method} line={entry.Line} key={entry.Key}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Phase 7 must not introduce new MonoBehaviour Update/LateUpdate/FixedUpdate/coroutine loops. Remove the loop or update the baseline only with explicit architecture approval:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void BroadConvertedISystemRowsAreListedForReview()
    {
        HashSet<string> broadReviewIds = ReadInventorySectionLines("## Broad Converted ISystem Review Debt")
            .Where(line => line.StartsWith("| `P7-", StringComparison.Ordinal))
            .Select(line => UnwrapCode(SplitMarkdownRow(line)[0]))
            .ToHashSet(StringComparer.Ordinal);
        string[] violations = LoadInventoryRows()
            .Where(row => row.Status == "Converted")
            .Where(row => CountPublicMembers(row.PublicApiCallSites) > 8)
            .Where(row => !broadReviewIds.Contains(row.Id))
            .Select(row => $"{row.Id} {row.Path} {row.Type} publicMembers={CountPublicMembers(row.PublicApiCallSites)}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Converted ISystem rows over the broad-system threshold must be visible in the Broad Converted ISystem Review Debt section:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void ConvertedPublicHelperApisRemainTrackedAsDebt()
    {
        HashSet<string> helperReviewIds = ReadInventorySectionLines("## Converted Public Helper API Review Debt")
            .Where(line => line.StartsWith("| `P7-", StringComparison.Ordinal))
            .Select(line => UnwrapCode(SplitMarkdownRow(line)[0]))
            .ToHashSet(StringComparer.Ordinal);
        string[] violations = LoadInventoryRows()
            .Where(row => row.Status == "Converted")
            .Where(row => HasPublicHelperApi(row.PublicApiCallSites))
            .Where(row => !helperReviewIds.Contains(row.Id))
            .Select(row => $"{row.Id} {row.Path} {row.Type} helpers={row.PublicApiCallSites}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Converted rows with public/internal helper APIs must stay visible in the Converted Public Helper API Review Debt section until replaced by ECS request/result data or plain helpers:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void ProductionNonUiSystemBaseRowsStayOpenOrManagedExceptionDebt()
    {
        string[] violations = LoadInventoryRows()
            .Where(row => row.Scope == "ProductionNonUI")
            .Where(row => IsSystemBaseLike(row.CurrentBase))
            .Where(row => row.Status == "Converted" ||
                          row.Disposition == "Converted" ||
                          (row.Status == "ManagedException" && row.Disposition != "ManagedPresentationSystemBaseException"))
            .Select(row => $"{row.Id} {row.Path} {row.Type} disposition={row.Disposition} status={row.Status}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Production non-UI SystemBase rows must remain explicit open debt or counted managed exceptions until converted/retired:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void OwnerLaneNamesMatchAgentTrackers()
    {
        string[] invalidLanes = LoadInventoryRows()
            .Select(row => row.OwnerLane)
            .Where(lane => !AgentLanes.Contains(lane))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            invalidLanes,
            "Inventory owner lanes must match AgentB, AgentC, AgentD, AgentE, AgentF, or Integration:\n" +
            string.Join(Environment.NewLine, invalidLanes));
    }

    [Test]
    public void ManagedExceptionCountStaysUnderPlanningCap()
    {
        int count = LoadInventoryRows().Count(row => row.Disposition == "ManagedPresentationSystemBaseException");

        Assert.AreEqual(
            FinalManagedExceptionCount,
            count,
            "Final Phase 7 managed presentation/config/camera exception count drifted. Update the inventory, tracker, and rationale together if this is intentional.");
        Assert.LessOrEqual(
            count,
            ManagedExceptionPlanningCap,
            $"Managed presentation/config/camera SystemBase exceptions exceed the planning cap. Current={count}, cap={ManagedExceptionPlanningCap}.");
    }

    [Test]
    public void FinalShareCanBeComputedFromInventoryCounts()
    {
        List<InventoryRow> rows = LoadInventoryRows()
            .Where(row => row.Scope is "ProductionNonUI" or "ProductionUI")
            .ToList();
        int systemBase = rows.Count(row => IsSystemBaseLike(row.CurrentBase));
        int iSystem = rows.Count(row => row.CurrentBase == "ISystem");
        int productionNonUi = rows.Count(row => row.Scope == "ProductionNonUI");
        int productionUi = rows.Count(row => row.Scope == "ProductionUI");
        int converted = rows.Count(row => row.Disposition == "Converted");
        int managedExceptions = rows.Count(row => row.Disposition == "ManagedPresentationSystemBaseException");
        int reviewRequired = rows.Count(row => row.Disposition == "ReviewRequired" || row.Status == "ReviewRequired");
        int uiOutOfScope = rows.Count(row => row.Disposition == "UIOutOfScope");
        float share = iSystem / (float)Math.Max(1, iSystem + systemBase);

        Assert.AreEqual(FinalProductionDeclarationCount, rows.Count, "Final Phase 7 production declaration count drifted.");
        Assert.AreEqual(FinalProductionNonUiCount, productionNonUi, "Final Phase 7 production non-UI count drifted.");
        Assert.AreEqual(FinalProductionUiCount, productionUi, "Final Phase 7 production UI count drifted.");
        Assert.AreEqual(FinalProductionSystemBaseCount, systemBase, "Final Phase 7 production SystemBase/legacy count drifted.");
        Assert.AreEqual(FinalProductionISystemCount, iSystem, "Final Phase 7 production ISystem count drifted.");
        Assert.AreEqual(FinalConvertedCount, converted, "Final Phase 7 converted count drifted.");
        Assert.AreEqual(FinalManagedExceptionCount, managedExceptions, "Final Phase 7 managed exception count drifted.");
        Assert.AreEqual(FinalUiOutOfScopeCount, uiOutOfScope, "Final Phase 7 UI out-of-scope count drifted.");
        Assert.AreEqual(0, reviewRequired, "Final Phase 7 inventory must not retain ReviewRequired rows.");
        Assert.AreEqual(186f / 211f, share, 0.001f, "Final Phase 7 production ISystem share drifted.");
    }

    [Test]
    public void DeliberateSystemBaseViolationIsDetectedByInventoryKeyGuard()
    {
        var fakeDeclaration = new CurrentDeclaration(
            "Assets/Game/Scripts/Systems/FakePhase7ViolationSystem.cs",
            "FakePhase7ViolationSystem",
            "class",
            "SystemBase",
            "ProductionNonUI");
        HashSet<string> inventoryKeys = LoadInventoryRows().Select(row => row.Key).ToHashSet(StringComparer.Ordinal);

        Assert.IsFalse(
            inventoryKeys.Contains(fakeDeclaration.Key),
            "The guard must detect a new production non-UI SystemBase when it is absent from the inventory.");
    }

    [Test]
    public void DeliberateMonoBehaviourLoopViolationIsDetected()
    {
        const string source = "public sealed class FakePhase7Ticker : UnityEngine.MonoBehaviour { private void Update() {} }";

        Assert.IsTrue(
            MonoBehaviourLoopRegex.IsMatch(source),
            "The MonoBehaviour loop detector must catch Update/LateUpdate/FixedUpdate/coroutine loops.");
    }

    [Test]
    public void DeliberateManagedExceptionPolicyViolationIsDetected()
    {
        const string source = "GameObject Attack Command Validate";

        Assert.IsTrue(
            HasHighRiskPolicyMix(source),
            "The managed exception policy guard must catch gameplay command/validation policy in a managed exception.");
    }

    [Test]
    public void UnmanagedEcsReferenceShapesAreNotManagedBlockers()
    {
        const string unmanagedEcsData =
            "public LocalTransform Transform; UnityObjectRef<GameObject> Prefab; float3 Position => target.Transform.Position;";
        const string genuineManagedReferences = "private Transform _view; private GameObject _instance;";

        Assert.IsFalse(
            HasManagedBlocker(unmanagedEcsData),
            "LocalTransform member names and UnityObjectRef<GameObject> ECS data must not be classified as managed Unity-object blockers.");
        Assert.IsTrue(
            HasManagedBlocker(genuineManagedReferences),
            "Concrete UnityEngine Transform and GameObject references must remain managed blockers.");
    }

    private static List<InventoryRow> LoadInventoryRows()
    {
        Assert.IsTrue(File.Exists(InventoryPath), $"Missing Phase 7 inventory at `{InventoryPath}`.");

        return ReadInventorySectionLines()
            .Where(line => line.StartsWith("| `P7-", StringComparison.Ordinal))
            .Select(ParseInventoryRow)
            .ToList();
    }

    private static IEnumerable<string> ReadInventorySectionLines()
    {
        return ReadInventorySectionLines("## Inventory");
    }

    private static IEnumerable<string> ReadInventorySectionLines(string sectionHeading)
    {
        bool inInventory = false;
        foreach (string line in File.ReadLines(InventoryPath))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (line == sectionHeading)
                {
                    inInventory = true;
                    continue;
                }

                if (inInventory)
                    yield break;
            }

            if (inInventory)
                yield return line;
        }
    }

    private static InventoryRow ParseInventoryRow(string line)
    {
        string[] cells = SplitMarkdownRow(line);
        Assert.GreaterOrEqual(cells.Length, 16, $"Inventory row has too few columns:\n{line}");

        return new InventoryRow(
            UnwrapCode(cells[0]),
            UnwrapCode(cells[1]),
            UnwrapCode(cells[2]),
            UnwrapCode(cells[3]),
            UnwrapCode(cells[4]),
            UnwrapCode(cells[6]),
            UnwrapCode(cells[7]),
            UnwrapCode(cells[8]),
            cells[9],
            cells[10],
            cells[11],
            UnwrapCode(cells[15]),
            line);
    }

    private static string[] SplitMarkdownRow(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith("|", StringComparison.Ordinal))
            trimmed = trimmed.Substring(1);
        if (trimmed.EndsWith("|", StringComparison.Ordinal))
            trimmed = trimmed.Substring(0, trimmed.Length - 1);

        var cells = new List<string>();
        int cellStart = 0;
        for (int index = 0; index < trimmed.Length; index++)
        {
            if (trimmed[index] != '|')
                continue;

            bool escaped = index > 0 && trimmed[index - 1] == '\\';
            if (escaped)
                continue;

            cells.Add(trimmed.Substring(cellStart, index - cellStart).Trim().Replace("\\|", "|"));
            cellStart = index + 1;
        }

        cells.Add(trimmed.Substring(cellStart).Trim().Replace("\\|", "|"));
        return cells.ToArray();
    }

    private static string UnwrapCode(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '`' && trimmed[trimmed.Length - 1] == '`')
            return trimmed.Substring(1, trimmed.Length - 2);
        return trimmed;
    }

    private static IEnumerable<CurrentDeclaration> EnumerateCurrentDeclarations()
    {
        foreach (string path in Directory.EnumerateFiles(GameScriptsRoot, "*.cs", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.Ordinal))
        {
            string normalizedPath = path.Replace('\\', '/');
            string text = File.ReadAllText(normalizedPath);
            string stripped = StripCommentsAndStrings(text);
            foreach (Match match in TypeDeclarationRegex.Matches(stripped))
            {
                string bases = (match.Groups["bases"].Success ? match.Groups["bases"].Value : string.Empty).TrimStart(':').Trim();
                Match baseMatch = CurrentBaseRegex.Match(bases);
                if (!baseMatch.Success)
                    continue;

                yield return new CurrentDeclaration(
                    normalizedPath,
                    match.Groups["name"].Value,
                    match.Groups["kind"].Value,
                    baseMatch.Value,
                    ScopeFor(normalizedPath));
            }
        }
    }

    private static string ReadDeclarationBody(InventoryRow row)
    {
        string stripped = StripCommentsAndStrings(File.ReadAllText(row.Path));
        foreach (Match match in TypeDeclarationRegex.Matches(stripped))
        {
            string bases = (match.Groups["bases"].Success ? match.Groups["bases"].Value : string.Empty).TrimStart(':').Trim();
            Match baseMatch = CurrentBaseRegex.Match(bases);
            if (!baseMatch.Success ||
                !string.Equals(match.Groups["name"].Value, row.Type, StringComparison.Ordinal) ||
                !string.Equals(match.Groups["kind"].Value, row.Kind, StringComparison.Ordinal) ||
                !string.Equals(baseMatch.Value, row.CurrentBase, StringComparison.Ordinal))
            {
                continue;
            }

            int bodyEnd = FindBodyEnd(stripped, match);
            return stripped.Substring(match.Index, Math.Max(0, bodyEnd - match.Index));
        }

        Assert.Fail($"Inventory declaration was not found in source: {row.Id} {row.Path} {row.Type}");
        return string.Empty;
    }

    private static HashSet<string> LoadMonoBehaviourLoopBaselineKeys()
    {
        Assert.IsTrue(File.Exists(MonoBehaviourLoopBaselinePath), $"Missing Phase 7 MonoBehaviour loop baseline at `{MonoBehaviourLoopBaselinePath}`.");

        var keys = new HashSet<string>(StringComparer.Ordinal);
        bool inBaseline = false;
        foreach (string line in File.ReadLines(MonoBehaviourLoopBaselinePath))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inBaseline = line == "## Baseline";
                continue;
            }

            if (!inBaseline || !line.StartsWith("| `", StringComparison.Ordinal))
                continue;

            string[] cells = SplitMarkdownRow(line);
            if (cells.Length > 0)
                keys.Add(UnwrapCode(cells[0]));
        }

        Assert.Greater(keys.Count, 0, "Phase 7 MonoBehaviour loop baseline must list existing loop keys.");
        return keys;
    }

    private static IEnumerable<MonoBehaviourLoopEntry> EnumerateCurrentMonoBehaviourLoops()
    {
        foreach (string path in Directory.EnumerateFiles(GameScriptsRoot, "*.cs", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.Ordinal))
        {
            string normalizedPath = path.Replace('\\', '/');
            string stripped = StripCommentsAndStrings(File.ReadAllText(normalizedPath));
            foreach (Match match in TypeDeclarationRegex.Matches(stripped))
            {
                string bases = (match.Groups["bases"].Success ? match.Groups["bases"].Value : string.Empty).TrimStart(':').Trim();
                if (!Regex.IsMatch(bases, @"\b(?:MonoBehaviour|UnityEngine\.MonoBehaviour)\b", RegexOptions.CultureInvariant))
                    continue;

                int bodyEnd = FindBodyEnd(stripped, match);
                int declarationEnd = match.Index + match.Length;
                string body = stripped.Substring(declarationEnd, Math.Max(0, bodyEnd - declarationEnd));
                foreach (Match loop in MonoBehaviourLoopMethodRegex.Matches(body))
                {
                    string method = loop.Groups["update"].Success
                        ? loop.Groups["update"].Value
                        : $"Coroutine:{loop.Groups["coroutine"].Value}";
                    int line = CountCharBefore(stripped, '\n', declarationEnd + loop.Index) + 1;
                    yield return new MonoBehaviourLoopEntry(
                        normalizedPath,
                        match.Groups["name"].Value,
                        method,
                        line,
                        ScopeFor(normalizedPath));
                }
            }
        }
    }

    private static int FindBodyEnd(string text, Match declaration)
    {
        int declarationEnd = declaration.Index + declaration.Length;
        int openBrace = text.IndexOf('{', declarationEnd);
        if (openBrace < 0)
            return declarationEnd;

        int depth = 0;
        for (int index = openBrace; index < text.Length; index++)
        {
            if (text[index] == '{')
            {
                depth++;
            }
            else if (text[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return index + 1;
            }
        }

        return text.Length;
    }

    private static int CountCharBefore(string text, char value, int endExclusive)
    {
        int limit = Math.Min(text.Length, Math.Max(0, endExclusive));
        int count = 0;
        for (int index = 0; index < limit; index++)
        {
            if (text[index] == value)
                count++;
        }

        return count;
    }

    private static string StripCommentsAndStrings(string text)
    {
        string noBlockComments = Regex.Replace(text, @"/\*.*?\*/", match => new string('\n', match.Value.Count(ch => ch == '\n')), RegexOptions.Singleline);
        string noLineComments = Regex.Replace(noBlockComments, @"//.*", string.Empty);
        return Regex.Replace(noLineComments, @"""(?:\\""|[^""])*""|'(?:\\.|[^'])'", "\"\"", RegexOptions.Singleline);
    }

    private static string ScopeFor(string path)
    {
        if (path.Contains("/Editor/", StringComparison.Ordinal) || path.StartsWith("Assets/Game/Scripts/Editor/", StringComparison.Ordinal))
            return "Editor";
        if (path.Contains("/Tests/", StringComparison.Ordinal) || path.StartsWith("Assets/Tests/", StringComparison.Ordinal))
            return "Test";
        if (path.StartsWith("Assets/Game/Scripts/UI/", StringComparison.Ordinal))
            return "ProductionUI";
        return "ProductionNonUI";
    }

    private static bool IsSystemBaseLike(string currentBase)
    {
        return currentBase is "SystemBase" or "ComponentSystemBase" or "ComponentSystem" or "JobComponentSystem";
    }

    private static bool HasManagedBlocker(string text)
    {
        return ContainsManagedUnityObjectToken(StripCommentsAndStrings(text));
    }

    private static bool HasHighRiskPolicyMix(string text)
    {
        string stripped = StripCommentsAndStrings(text);
        return ContainsManagedUnityObjectToken(stripped) && GameplayPolicyRegex.IsMatch(stripped);
    }

    private static bool ContainsManagedUnityObjectToken(string stripped)
    {
        string blockerText = UnmanagedEcsObjectReferenceRegex.Replace(stripped, "UnityObjectRef");
        return ManagedBlockerRegex.IsMatch(blockerText) ||
               ManagedTransformTypeRegex.IsMatch(blockerText) ||
               HasExactIdentifier(blockerText, "GameObject") ||
               HasExactIdentifier(blockerText, "ScriptableObject") ||
               HasExactIdentifier(blockerText, "MonoBehaviour") ||
               HasExactIdentifier(blockerText, "ParticleSystem") ||
               HasExactIdentifier(blockerText, "LineRenderer") ||
               HasExactIdentifier(blockerText, "VisualEffect");
    }

    private static bool HasExactIdentifier(string text, string identifier)
    {
        return Regex.IsMatch(
            text,
            $@"(?<![A-Za-z0-9_]){Regex.Escape(identifier)}(?![A-Za-z0-9_])",
            RegexOptions.CultureInvariant);
    }

    private static int CountPublicMembers(string publicApiCallSites)
    {
        if (string.IsNullOrWhiteSpace(publicApiCallSites) || publicApiCallSites == "None")
            return 0;

        return Regex.Matches(publicApiCallSites, @"\((?:method|property)\)", RegexOptions.CultureInvariant).Count;
    }

    private static bool HasPublicHelperApi(string publicApiCallSites)
    {
        if (string.IsNullOrWhiteSpace(publicApiCallSites) || publicApiCallSites == "None")
            return false;

        var lifecycleOrRunner = new HashSet<string>(StringComparer.Ordinal)
        {
            "OnCreate",
            "OnStartRunning",
            "OnUpdate",
            "OnStopRunning",
            "OnDestroy",
            "Execute",
            "Dispose"
        };

        foreach (Match match in PublicMemberNameRegex.Matches(publicApiCallSites))
        {
            if (!lifecycleOrRunner.Contains(match.Groups["name"].Value))
                return true;
        }

        return false;
    }

    private static string GetCurrentBaseOrMissing(Dictionary<string, CurrentDeclaration> current, string key)
    {
        return current.TryGetValue(key, out CurrentDeclaration declaration)
            ? declaration.CurrentBase
            : "missing";
    }

    private readonly struct InventoryRow
    {
        public readonly string Id;
        public readonly string Type;
        public readonly string Kind;
        public readonly string CurrentBase;
        public readonly string Path;
        public readonly string Scope;
        public readonly string OwnerLane;
        public readonly string Disposition;
        public readonly string ManagedBlockers;
        public readonly string GameplayPolicyRisk;
        public readonly string PublicApiCallSites;
        public readonly string Status;
        public readonly string Raw;

        public InventoryRow(
            string id,
            string type,
            string kind,
            string currentBase,
            string path,
            string scope,
            string ownerLane,
            string disposition,
            string managedBlockers,
            string gameplayPolicyRisk,
            string publicApiCallSites,
            string status,
            string raw)
        {
            Id = id;
            Type = type;
            Kind = kind;
            CurrentBase = currentBase;
            Path = path;
            Scope = scope;
            OwnerLane = ownerLane;
            Disposition = disposition;
            ManagedBlockers = managedBlockers;
            GameplayPolicyRisk = gameplayPolicyRisk;
            PublicApiCallSites = publicApiCallSites;
            Status = status;
            Raw = raw;
        }

        public string Key => $"{Path}|{Type}|{Kind}|{CurrentBase}";
    }

    private readonly struct CurrentDeclaration
    {
        public readonly string Path;
        public readonly string Type;
        public readonly string Kind;
        public readonly string CurrentBase;
        public readonly string Scope;

        public CurrentDeclaration(string path, string type, string kind, string currentBase, string scope)
        {
            Path = path;
            Type = type;
            Kind = kind;
            CurrentBase = currentBase;
            Scope = scope;
        }

        public string Key => $"{Path}|{Type}|{Kind}|{CurrentBase}";
    }

    private readonly struct MonoBehaviourLoopEntry
    {
        public readonly string Path;
        public readonly string Type;
        public readonly string Method;
        public readonly int Line;
        public readonly string Scope;

        public MonoBehaviourLoopEntry(string path, string type, string method, int line, string scope)
        {
            Path = path;
            Type = type;
            Method = method;
            Line = line;
            Scope = scope;
        }

        public string Key => $"{Path}|{Type}|{Method}";
    }
}
#endif
