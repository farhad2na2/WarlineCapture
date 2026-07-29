#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Freezes the additive VRP-050 render-only pilot from accepted inventory evidence.
    /// It does not edit a map definition, scene, bake config, or presentation owner.
    /// </summary>
    internal static class OperationMapRenderVirtualizationPilotFreezeReport
    {
        private const string InventoryPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_eligibility_inventory.json";
        private const string PlacementsPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_logical_placements.json";
        private const string PrototypesPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_prototype_recipes.json";
        private const string ReportPath =
            "Design/AgentReports/2026-07-30_dense_city_render_virtualization_pilot_freeze.json";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void RunFocusedValidation()
        {
            string projectRoot =
                Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string inventoryJson = ReadAcceptedInput(projectRoot, InventoryPath);
            string placementsJson = ReadAcceptedInput(projectRoot, PlacementsPath);
            string prototypesJson = ReadAcceptedInput(projectRoot, PrototypesPath);
            InventoryDocument inventory =
                JsonUtility.FromJson<InventoryDocument>(inventoryJson);
            LogicalPlacementDocument placements =
                JsonUtility.FromJson<LogicalPlacementDocument>(placementsJson);
            PrototypeRecipeDocument prototypes =
                JsonUtility.FromJson<PrototypeRecipeDocument>(prototypesJson);

            PilotFreezeDocument report = Build(
                inventory,
                placements,
                prototypes,
                ComputeSha256(Utf8WithoutBom.GetBytes(placementsJson)),
                ComputeSha256(Utf8WithoutBom.GetBytes(prototypesJson)));
            string reportJson = JsonUtility.ToJson(report, true) + "\n";
            string repeatedJson = JsonUtility.ToJson(report, true) + "\n";
            Require(
                string.Equals(reportJson, repeatedJson, StringComparison.Ordinal),
                "Pilot freeze serialization is not deterministic.");

            string outputPath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            string temporaryPath = outputPath + ".tmp";
            File.WriteAllText(temporaryPath, reportJson, Utf8WithoutBom);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            File.Move(temporaryPath, outputPath);

            PilotFreezeDocument written = JsonUtility.FromJson<PilotFreezeDocument>(
                File.ReadAllText(outputPath, Utf8WithoutBom));
            Require(
                written != null &&
                written.result == "Passed" &&
                written.placementCount == 9721 &&
                written.renderRowCount == 11299 &&
                written.stableIdentities.Count == 9721,
                "Written pilot freeze report failed its closed acceptance gate.");
            Debug.Log(
                "[OperationMapRenderVirtualizationPilotFreezeValidation] " +
                $"result=Passed placements={report.placementCount} " +
                $"rows={report.renderRowCount} " +
                $"selectionSha256={report.stableIdentitySelectionSha256}");
        }

        private static PilotFreezeDocument Build(
            InventoryDocument inventory,
            LogicalPlacementDocument placements,
            PrototypeRecipeDocument prototypes,
            string placementsJsonSha256,
            string prototypesJsonSha256)
        {
            Require(inventory != null && inventory.result == "Passed",
                "Accepted eligibility inventory is missing or failed.");
            Require(placements != null && placements.result == "Passed",
                "Accepted logical placements are missing or failed.");
            Require(prototypes != null && prototypes.result == "Passed",
                "Accepted prototype recipes are missing or failed.");
            Require(
                inventory.operationMapId == placements.operationMapId &&
                inventory.operationMapId == prototypes.operationMapId,
                "Pilot evidence operation-map identities do not match.");
            Require(
                inventory.logicalPlacementsJsonSha256 == placementsJsonSha256 &&
                inventory.prototypeRecipesJsonSha256 == prototypesJsonSha256,
                "Pilot evidence JSON hashes do not match the accepted inventory.");
            Require(
                inventory.eligibleRenderRows == 11299 &&
                inventory.logicalPlacementCount == 9721 &&
                placements.placementCount == 9721 &&
                placements.placementPartRowCount == 11299 &&
                prototypes.logicalPlacementCount == 9721 &&
                prototypes.eligibleSourceRowCount == 11299,
                "Pilot evidence counts do not match the accepted inventory.");
            Require(
                placements.stateOwnerCount == 0 &&
                placements.stateLinkedPlacementCount == 0 &&
                placements.renderOnlyPlacementCount == placements.placementCount,
                "The Phase 5 pilot must remain entirely render-only.");
            Require(
                placements.placements != null &&
                placements.placements.Count == placements.placementCount &&
                prototypes.prototypes != null &&
                prototypes.prototypes.Count == prototypes.prototypeCount,
                "Pilot evidence arrays do not match their declared counts.");

            var prototypeByIndex = new PrototypeRecipe[prototypes.prototypeCount];
            var selectedPrototypes =
                new List<PilotPrototypeIdentity>(prototypes.prototypeCount);
            for (int index = 0; index < prototypes.prototypes.Count; index++)
            {
                PrototypeRecipe prototype = prototypes.prototypes[index];
                Require(
                    prototype != null &&
                    prototype.prototypeIndex == index &&
                    prototype.partCount > 0 &&
                    prototype.placementCount > 1 &&
                    IsPilotCategory(prototype.semanticCategory),
                    "Pilot prototype is missing, non-repeated, or outside Vegetation/Prop.");
                prototypeByIndex[index] = prototype;
                selectedPrototypes.Add(new PilotPrototypeIdentity
                {
                    prototypeIndex = prototype.prototypeIndex,
                    prototypeIdentityLow = prototype.prototypeIdentityLow,
                    prototypeIdentityHigh = prototype.prototypeIdentityHigh,
                    semanticCategory = prototype.semanticCategory,
                    placementCount = prototype.placementCount,
                    partCount = prototype.partCount
                });
            }

            int vegetationPlacements = 0;
            int propPlacements = 0;
            int vegetationRows = 0;
            int propRows = 0;
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var identityPairs = new HashSet<string>(StringComparer.Ordinal);
            var identities =
                new List<PilotStableIdentity>(placements.placementCount);
            var observedPrototypePlacements = new int[prototypes.prototypeCount];
            var canonical = new StringBuilder(placements.placementCount * 128);
            AppendCanonical(canonical, "vrp-050-pilot-freeze-v1");
            AppendCanonical(canonical, inventory.operationMapId);
            AppendCanonical(canonical, inventory.sourceRowsSha256);
            AppendCanonical(canonical, inventory.prototypeRecipesSha256);
            AppendCanonical(canonical, inventory.logicalPlacementsSha256);

            ulong previousLow = 0;
            ulong previousHigh = 0;
            for (int index = 0; index < placements.placements.Count; index++)
            {
                LogicalPlacement placement = placements.placements[index];
                Require(
                    placement != null &&
                    placement.placementIndex == index &&
                    !string.IsNullOrWhiteSpace(placement.stableOwnerId) &&
                    placement.prototypeIndex >= 0 &&
                    placement.prototypeIndex < prototypeByIndex.Length &&
                    placement.stateOwnerIndex == -1 &&
                    placement.requiredVisualState == "Any" &&
                    placement.priority == 0 &&
                    IsPilotCategory(placement.semanticCategory),
                    "Pilot placement violates the accepted render-only contract.");
                if (index > 0)
                {
                    Require(
                        placement.stableIdentityLow > previousLow ||
                        (placement.stableIdentityLow == previousLow &&
                         placement.stableIdentityHigh > previousHigh),
                        "Pilot stable identities are not strictly sorted.");
                }
                previousLow = placement.stableIdentityLow;
                previousHigh = placement.stableIdentityHigh;
                Require(stableIds.Add(placement.stableOwnerId),
                    "Pilot stable owner identity is duplicated.");
                Require(
                    identityPairs.Add(
                        placement.stableIdentityLow.ToString(
                            CultureInfo.InvariantCulture) +
                        ":" +
                        placement.stableIdentityHigh.ToString(
                            CultureInfo.InvariantCulture)),
                    "Pilot projected stable identity is duplicated.");

                PrototypeRecipe prototype =
                    prototypeByIndex[placement.prototypeIndex];
                Require(prototype.semanticCategory == placement.semanticCategory,
                    "Pilot placement/prototype semantic categories differ.");
                observedPrototypePlacements[placement.prototypeIndex]++;
                if (placement.semanticCategory == "Vegetation")
                {
                    vegetationPlacements++;
                    vegetationRows += prototype.partCount;
                }
                else
                {
                    propPlacements++;
                    propRows += prototype.partCount;
                }

                identities.Add(new PilotStableIdentity
                {
                    placementIndex = placement.placementIndex,
                    stableOwnerId = placement.stableOwnerId,
                    stableIdentityLow = placement.stableIdentityLow,
                    stableIdentityHigh = placement.stableIdentityHigh,
                    prototypeIndex = placement.prototypeIndex,
                    semanticCategory = placement.semanticCategory
                });
                AppendCanonical(canonical, placement.placementIndex);
                AppendCanonical(canonical, placement.stableOwnerId);
                AppendCanonical(canonical, placement.stableIdentityLow);
                AppendCanonical(canonical, placement.stableIdentityHigh);
                AppendCanonical(canonical, placement.prototypeIndex);
                AppendCanonical(canonical, placement.semanticCategory);
            }

            for (int index = 0; index < observedPrototypePlacements.Length; index++)
            {
                Require(
                    observedPrototypePlacements[index] ==
                    prototypeByIndex[index].placementCount,
                    "Pilot prototype placement count does not reconcile.");
            }
            Require(
                vegetationRows == GetEligibleRows(inventory, "Vegetation") &&
                propRows == GetEligibleRows(inventory, "Prop") &&
                vegetationRows + propRows == inventory.eligibleRenderRows,
                "Pilot category row counts do not reconcile to all eligible rows.");
            Require(
                GetTotalRows(inventory, "Prop") - propRows == 3 &&
                GetTotalRows(inventory, "Vegetation") - vegetationRows == 0,
                "Pilot does not retain exactly the three excluded Prop rows.");

            return new PilotFreezeDocument
            {
                schema =
                    "warline.operation-map.render-virtualization-pilot-freeze",
                schemaVersion = 1,
                operationMapId = inventory.operationMapId,
                result = "Passed",
                selectionPolicy =
                    "All accepted stable-owner-joined eligible repeated render-only Vegetation and Prop placements; excluded rows remain resident.",
                sourceInventoryPath = InventoryPath,
                sourceRowsSha256 = inventory.sourceRowsSha256,
                prototypeRecipesSha256 = inventory.prototypeRecipesSha256,
                prototypeRecipesJsonSha256 = prototypesJsonSha256,
                logicalPlacementsSha256 = inventory.logicalPlacementsSha256,
                logicalPlacementsJsonSha256 = placementsJsonSha256,
                placementCount = identities.Count,
                renderRowCount = vegetationRows + propRows,
                prototypeCount = selectedPrototypes.Count,
                prototypePartCount = prototypes.prototypePartCount,
                vegetationPlacementCount = vegetationPlacements,
                vegetationRenderRowCount = vegetationRows,
                propPlacementCount = propPlacements,
                propRenderRowCount = propRows,
                excludedPropRenderRowCount = 3,
                mutationAuthorized = false,
                selectionEnabled = false,
                stableIdentitySelectionSha256 =
                    ComputeSha256(Utf8WithoutBom.GetBytes(canonical.ToString())),
                prototypes = selectedPrototypes,
                stableIdentities = identities
            };
        }

        private static int GetEligibleRows(
            InventoryDocument inventory,
            string category)
        {
            Breakdown breakdown = GetBreakdown(inventory, category);
            return breakdown.eligible;
        }

        private static int GetTotalRows(
            InventoryDocument inventory,
            string category)
        {
            Breakdown breakdown = GetBreakdown(inventory, category);
            return breakdown.total;
        }

        private static Breakdown GetBreakdown(
            InventoryDocument inventory,
            string category)
        {
            Require(inventory.bySemanticCategory != null,
                "Inventory semantic-category breakdown is missing.");
            Breakdown match = null;
            for (int index = 0; index < inventory.bySemanticCategory.Count; index++)
            {
                Breakdown candidate = inventory.bySemanticCategory[index];
                if (candidate != null && candidate.key == category)
                {
                    Require(match == null,
                        "Inventory semantic category is duplicated.");
                    match = candidate;
                }
            }
            Require(match != null,
                $"Inventory semantic category '{category}' is missing.");
            return match;
        }

        private static bool IsPilotCategory(string category) =>
            category == "Vegetation" || category == "Prop";

        private static string ReadAcceptedInput(
            string projectRoot,
            string relativePath)
        {
            string path = Path.Combine(projectRoot, relativePath);
            Require(File.Exists(path), $"Accepted input is missing: {relativePath}");
            return File.ReadAllText(path, Utf8WithoutBom);
        }

        private static string ComputeSha256(byte[] source)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(source);
            var hex = new StringBuilder(digest.Length * 2);
            for (int index = 0; index < digest.Length; index++)
            {
                hex.Append(
                    digest[index].ToString("x2", CultureInfo.InvariantCulture));
            }
            return hex.ToString();
        }

        private static void AppendCanonical(StringBuilder builder, string value)
        {
            value ??= string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(value)
                .Append('\n');
        }

        private static void AppendCanonical(StringBuilder builder, int value) =>
            AppendCanonical(
                builder,
                value.ToString(CultureInfo.InvariantCulture));

        private static void AppendCanonical(StringBuilder builder, ulong value) =>
            AppendCanonical(
                builder,
                value.ToString(CultureInfo.InvariantCulture));

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        [Serializable]
        private sealed class PilotFreezeDocument
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public string selectionPolicy;
            public string sourceInventoryPath;
            public string sourceRowsSha256;
            public string prototypeRecipesSha256;
            public string prototypeRecipesJsonSha256;
            public string logicalPlacementsSha256;
            public string logicalPlacementsJsonSha256;
            public int placementCount;
            public int renderRowCount;
            public int prototypeCount;
            public int prototypePartCount;
            public int vegetationPlacementCount;
            public int vegetationRenderRowCount;
            public int propPlacementCount;
            public int propRenderRowCount;
            public int excludedPropRenderRowCount;
            public bool mutationAuthorized;
            public bool selectionEnabled;
            public string stableIdentitySelectionSha256;
            public List<PilotPrototypeIdentity> prototypes;
            public List<PilotStableIdentity> stableIdentities;
        }

        [Serializable]
        private sealed class PilotPrototypeIdentity
        {
            public int prototypeIndex;
            public ulong prototypeIdentityLow;
            public ulong prototypeIdentityHigh;
            public string semanticCategory;
            public int placementCount;
            public int partCount;
        }

        [Serializable]
        private sealed class PilotStableIdentity
        {
            public int placementIndex;
            public string stableOwnerId;
            public ulong stableIdentityLow;
            public ulong stableIdentityHigh;
            public int prototypeIndex;
            public string semanticCategory;
        }

        [Serializable]
        private sealed class InventoryDocument
        {
            public string result;
            public string operationMapId;
            public int eligibleRenderRows;
            public int logicalPlacementCount;
            public string sourceRowsSha256;
            public string prototypeRecipesSha256;
            public string prototypeRecipesJsonSha256;
            public string logicalPlacementsSha256;
            public string logicalPlacementsJsonSha256;
            public List<Breakdown> bySemanticCategory;
        }

        [Serializable]
        private sealed class Breakdown
        {
            public string key;
            public int eligible;
            public int total;
        }

        [Serializable]
        private sealed class LogicalPlacementDocument
        {
            public string result;
            public string operationMapId;
            public int placementCount;
            public int stateOwnerCount;
            public int stateLinkedPlacementCount;
            public int renderOnlyPlacementCount;
            public int placementPartRowCount;
            public List<LogicalPlacement> placements;
        }

        [Serializable]
        private sealed class LogicalPlacement
        {
            public int placementIndex;
            public string stableOwnerId;
            public ulong stableIdentityLow;
            public ulong stableIdentityHigh;
            public int prototypeIndex;
            public int stateOwnerIndex;
            public string requiredVisualState;
            public int priority;
            public string semanticCategory;
        }

        [Serializable]
        private sealed class PrototypeRecipeDocument
        {
            public string result;
            public string operationMapId;
            public int logicalPlacementCount;
            public int prototypeCount;
            public int prototypePartCount;
            public int eligibleSourceRowCount;
            public List<PrototypeRecipe> prototypes;
        }

        [Serializable]
        private sealed class PrototypeRecipe
        {
            public int prototypeIndex;
            public ulong prototypeIdentityLow;
            public ulong prototypeIdentityHigh;
            public string semanticCategory;
            public int placementCount;
            public int partCount;
        }
    }
}

#endif
