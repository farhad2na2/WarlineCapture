using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class S003CurrentQualificationSourceHostControls
{
    static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static string Sha(byte[] bytes) { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static string Sha(string path) => Sha(File.ReadAllBytes(path));
    static JObject Read(string path) => JObject.Parse(File.ReadAllText(path), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
    static JObject Descriptor(string path) => new JObject { ["path"] = Path.GetFullPath(path), ["sha256"] = Sha(path) };
    static Game.Configs.SkirmishResolvedSetup CloneSetup(Game.Configs.SkirmishResolvedSetup setup)
        => (Game.Configs.SkirmishResolvedSetup)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(setup, null);
    static bool Reject(Action action)
    {
        try { action(); return false; }
        catch (InvalidDataException) { return true; }
        catch (NotSupportedException) { return true; }
    }
    static bool HistoricalAuditFixtureRejects(JObject context)
    {
        var audit = Read((string)context["factory_capture_historical_compiler_audit"]["path"]);
        var receipt = Read((string)context["factory_capture_historical_compiler_receipt"]["path"]);
        var method = typeof(SourceConfigurationQualification).GetMethod("RequireS003HistoricalAudit", BindingFlags.Static | BindingFlags.NonPublic);
        Need(method != null, "Historical audit validator is unavailable");
        method.Invoke(null, new object[] { audit, receipt["source_graph"], receipt });
        var malformed = (JObject)audit.DeepClone();
        malformed["source_graph_pin_mismatches"] = 0;
        try { method.Invoke(null, new object[] { malformed, context["compiler_graph"], receipt }); return false; }
        catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { return true; }
    }
    static bool CapturePathFixtures(string output)
    {
        string root = Path.Combine(output, "capture-path-fixture");
        string evidence = Path.Combine(output, "capture-evidence-fixture");
        Directory.CreateDirectory(Path.Combine(root, "Assets", "Game"));
        Directory.CreateDirectory(Path.Combine(root, "Packages"));
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(root, "Assets", "Game", "fixture.asset"), "asset");
        File.WriteAllText(Path.Combine(root, "Packages", "manifest.json"), "{}");
        File.WriteAllText(Path.Combine(evidence, "preserved.cs"), "evidence");
        var saved = new System.Collections.Generic.Dictionary<string, string> { ["Assets/Game/Editor/preserved.cs"] = "preserved.cs" };
        string relative = SourceMetadataCompatibility.ResolveS003CapturePin("Assets/Game/fixture.asset", root, evidence, saved);
        string absolute = SourceMetadataCompatibility.ResolveS003CapturePin(Path.GetFullPath(Path.Combine(root, "Packages", "manifest.json")), root, evidence, saved);
        string preserved = SourceMetadataCompatibility.ResolveS003CapturePin("Assets/Game/Editor/preserved.cs", root, evidence, saved);
        bool traversalRejected = Reject(() => SourceMetadataCompatibility.ResolveS003CapturePin("Assets/../outside", root, evidence, saved));
        bool outsideAbsoluteRejected = Reject(() => SourceMetadataCompatibility.ResolveS003CapturePin(Path.GetFullPath(Path.Combine(output, "outside")), root, evidence, saved));
        string assetSha = Sha(Path.Combine(root, "Assets", "Game", "fixture.asset"));
        var normalizedPins = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
        SourceMetadataCompatibility.AdmitS003CapturePin(normalizedPins, "Assets/Game/fixture.asset", root, evidence, saved, assetSha);
        SourceMetadataCompatibility.AdmitS003CapturePin(normalizedPins, relative, root, evidence, saved, assetSha);
        bool conflictingNormalizedPinRejected = Reject(() => SourceMetadataCompatibility.AdmitS003CapturePin(normalizedPins, relative, root, evidence, saved, new string('0', 64)));
        string currentRelative = SourceConfigurationQualification.ResolveCurrentS003CapturePin("Assets/Game/fixture.asset", root);
        string currentAbsolute = SourceConfigurationQualification.ResolveCurrentS003CapturePin(Path.GetFullPath(Path.Combine(root, "Packages", "manifest.json")), root);
        bool currentTraversalRejected = Reject(() => SourceConfigurationQualification.ResolveCurrentS003CapturePin("Assets/../outside", root));
        bool currentOutsideAbsoluteRejected = Reject(() => SourceConfigurationQualification.ResolveCurrentS003CapturePin(Path.GetFullPath(Path.Combine(output, "outside")), root));
        var currentPins = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        SourceConfigurationQualification.AdmitCurrentS003CapturePin(currentPins, "Assets/Game/fixture.asset", root, assetSha);
        SourceConfigurationQualification.AdmitCurrentS003CapturePin(currentPins, currentRelative, root, assetSha);
        bool currentConflictingDuplicateRejected = Reject(() => SourceConfigurationQualification.AdmitCurrentS003CapturePin(currentPins, currentRelative, root, new string('0', 64)));
        return relative == Path.GetFullPath(Path.Combine(root, "Assets", "Game", "fixture.asset")) &&
            absolute == Path.GetFullPath(Path.Combine(root, "Packages", "manifest.json")) &&
            preserved == Path.GetFullPath(Path.Combine(evidence, "preserved.cs")) && traversalRejected && outsideAbsoluteRejected && conflictingNormalizedPinRejected &&
            currentRelative == relative && currentAbsolute == absolute && currentTraversalRejected && currentOutsideAbsoluteRejected && currentConflictingDuplicateRejected;
    }
    static string WriteJson(string path, JObject value) { File.WriteAllText(path, value.ToString(Formatting.Indented) + Environment.NewLine); return path; }
    static JObject Request(string root, JObject qualification, string qualificationPath)
    {
        var compiler = (JObject)qualification["compiler_context"];
        var context = Read((string)compiler["path"]);
        var graphDescriptor = (JObject)context["compiler_graph"];
        var graph = Read((string)graphDescriptor["path"]);
        string catalogPath = Path.GetFullPath(Path.Combine(root, "Assets/Game/Resources/SkirmishBattleCatalog.asset"));
        string matrixPath = (string)graph["setup_matrix"]["path"];
        var graphPins = (JObject)graph["source_pins"];
        return new JObject {
            ["schema_version"] = 1,
            ["kind"] = "GeneratedMissionInitialization",
            ["authority"] = "AuthoredConfigAndBakedCampaignMap",
            ["mission_id"] = "S003",
            ["selection"] = new JObject { ["seed"] = 104732, ["size_id"] = 1, ["difficulty_id"] = 2 },
            ["metadata_compiler_context"] = compiler.DeepClone(),
            ["configuration_qualification"] = Descriptor(qualificationPath),
            ["source_config_graph"] = graphDescriptor.DeepClone(),
            ["canonical_battle_catalog"] = new JObject { ["path"] = catalogPath, ["sha256"] = (string)graphPins[catalogPath] },
            ["setup_matrix"] = new JObject { ["path"] = matrixPath, ["sha256"] = (string)graph["setup_matrix"]["sha256"] }
        };
    }
    static bool ContextRejects(JObject request, string output, string label, string mutate)
    {
        var q = (JObject)request["configuration_qualification"].DeepClone();
        var qualification = Read((string)q["path"]);
        var contextDescriptor = (JObject)request["metadata_compiler_context"].DeepClone();
        var context = Read((string)contextDescriptor["path"]);
        string contextPath = Path.Combine(output, label + "-context.json");
        if (mutate == "target") context["target_context"]["seed"] = 104733;
        else if (mutate == "pin") context["source_graph"]["sha256"] = new string('0', 64);
        else if (mutate == "capture-pin") context["native_factory_capture"]["sha256"] = new string('0', 64);
        else throw new ArgumentOutOfRangeException(nameof(mutate));
        WriteJson(contextPath, context);
        contextDescriptor = Descriptor(contextPath);
        qualification["compiler_context"] = contextDescriptor.DeepClone();
        string qualificationPath = Path.Combine(output, label + "-qualification.json");
        WriteJson(qualificationPath, qualification);
        request["metadata_compiler_context"] = contextDescriptor;
        request["configuration_qualification"] = Descriptor(qualificationPath);
        return Reject(() => SourceConfigurationQualification.Context(request));
    }
    static bool RuntimeGraphRejects(JObject request, string mutate)
    {
        var bad = (JObject)request.DeepClone();
        if (mutate == "capture-graph")
        {
            var context = Read((string)bad["metadata_compiler_context"]["path"]);
            bad["source_config_graph"] = context["source_graph"].DeepClone();
        }
        else if (mutate == "catalog-path") bad["canonical_battle_catalog"]["path"] = Path.Combine(Path.GetDirectoryName((string)bad["canonical_battle_catalog"]["path"]), "OtherCatalog.asset");
        else if (mutate == "catalog-hash") bad["canonical_battle_catalog"]["sha256"] = new string('0', 64);
        else if (mutate == "setup-matrix") bad["setup_matrix"]["sha256"] = new string('0', 64);
        else throw new ArgumentOutOfRangeException(nameof(mutate));
        return Reject(() => SourceConfigurationQualification.Context(bad));
    }
    static bool CompilerGraphMutationRejects(JObject context, string mutate)
    {
        var graphDescriptor = (JObject)context["compiler_graph"];
        var graph = Read((string)graphDescriptor["path"]);
        string root = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (mutate == "catalog-node")
        {
            var catalogNode = ((JArray)graph["nodes"]).OfType<JObject>().FirstOrDefault(n => (string)n["source_type"] == "SkirmishBattleCatalogConfig");
            catalogNode?.Remove();
        }
        else if (mutate == "catalog-meta-pin") ((JObject)graph["source_pins"]).Remove(Path.Combine(root, "Assets/Game/Resources/SkirmishBattleCatalog.asset.meta"));
        else if (mutate == "catalog-augmentation") graph["canonical_catalog_augmentation"] = null;
        else if (mutate == "catalog-row") graph["canonical_catalog_augmentation"]["catalog_membership_audit"]["row"]["DefinitionId"] = "skirmish.other";
        else if (mutate == "catalog-origin") graph["canonical_catalog_augmentation"]["origin"] = "self-reported source";
        else if (mutate == "selection-node") ((JArray)graph["nodes"]).OfType<JObject>().First(n => (string)n["source_path"] == "Assets/Game/Resources/SkirmishBaseAssault.asset").Remove();
        else if (mutate == "selection-pin") graph["source_pins"][Path.Combine(root, "Assets/Game/Resources/SkirmishBaseAssault.asset")] = new string('0', 64);
        else if (mutate == "selection-receipt-pin") { }
        else if (mutate == "grid-surface-node") ((JArray)graph["nodes"]).OfType<JObject>().First(n => (string)n["source_path"] == "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset").Remove();
        else if (mutate == "grid-surface-script-pin") graph["source_pins"][Path.Combine(root, "Assets/Game/Scripts/Configs/Scene/GridAuthoringSceneConfigAsset.cs")] = new string('0', 64);
        else if (mutate == "grid-surface-receipt-pin") { }
        else throw new ArgumentOutOfRangeException(nameof(mutate));
        var extensionDescriptor = (JObject)context["source_selection_extension_receipt"].DeepClone();
        if (mutate == "selection-receipt-pin") extensionDescriptor["sha256"] = new string('0', 64);
        var gridSurfaceDescriptor = (JObject)context["source_grid_surface_extension_receipt"].DeepClone();
        if (mutate == "grid-surface-receipt-pin") gridSurfaceDescriptor["sha256"] = new string('0', 64);
        var method = typeof(SourceConfigurationQualification).GetMethod("RequireS003CompilerGraph", BindingFlags.Static | BindingFlags.NonPublic);
        Need(method != null, "Current compiler graph validator is unavailable");
        try { method.Invoke(null, new object[] { graph, context["source_graph"], extensionDescriptor, gridSurfaceDescriptor }); return false; }
        catch (TargetInvocationException e) when (e.InnerException is InvalidDataException || e.InnerException is NotSupportedException) { return true; }
    }
    static bool SelectionExtensionReceiptMutationRejects(JObject context, string mutate)
    {
        string root = Path.GetFullPath(Directory.GetCurrentDirectory());
        var selectionReceipt = Read((string)context["source_selection_extension_receipt"]["path"]);
        var graphPath = SourceConfigurationQualification.ResolveCurrentS003CapturePin((string)selectionReceipt["output_graph"]["path"], root);
        var graph = Read(graphPath);
        var receipt = selectionReceipt;
        var basePath = SourceConfigurationQualification.ResolveCurrentS003CapturePin((string)receipt["base_graph"]["path"], root);
        var baseGraph = Read(basePath);
        if (mutate == "added-pin") ((JObject)receipt["added_pins"]).Properties().First().Value = new JValue(new string('0', 64));
        else if (mutate == "added-node") receipt["added_nodes"][0]["source_path"] = "Assets/Game/Resources/Fabricated.asset";
        else if (mutate == "required-asset") receipt["generated_owner"]["required_assets"]["Assets/Game/Resources/SkirmishBaseAssault.asset"] = new string('0', 64);
        else if (mutate == "direct-reference-guid") receipt["direct_reference_audit"][0]["guid"] = new string('0', 32);
        else throw new ArgumentOutOfRangeException(nameof(mutate));
        var method = typeof(SourceConfigurationQualification).GetMethod("RequireS003SelectionGraphExtension", BindingFlags.Static | BindingFlags.NonPublic);
        Need(method != null, "Current source-selection extension validator is unavailable");
        try { method.Invoke(null, new object[] { baseGraph, graph, receipt, root }); return false; }
        catch (TargetInvocationException e) when (e.InnerException is InvalidDataException || e.InnerException is NotSupportedException) { return true; }
    }
    static bool GridSurfaceExtensionReceiptMutationRejects(JObject context, string mutate)
    {
        string root = Path.GetFullPath(Directory.GetCurrentDirectory());
        var graph = Read((string)context["compiler_graph"]["path"]);
        var receipt = Read((string)context["source_grid_surface_extension_receipt"]["path"]);
        var basePath = SourceConfigurationQualification.ResolveCurrentS003CapturePin((string)receipt["base_graph"]["path"], root);
        var baseGraph = Read(basePath);
        if (mutate == "added-script-pin") ((JObject)receipt["added_source_pins"]).Properties().First().Value = new JValue(new string('0', 64));
        else if (mutate == "grid-node-guid") receipt["added_nodes"][0]["asset_guid"] = new string('0', 32);
        else if (mutate == "surface-payload-length") receipt["added_nodes"].OfType<JObject>().First(n => (string)n["source_type"] == "MapSurfaceDataAsset")["surface_payload_hex_chars"] = 1;
        else if (mutate == "parser-pin") receipt["parser_provenance"]["strict_yaml_reader"]["sha256"] = new string('0', 64);
        else throw new ArgumentOutOfRangeException(nameof(mutate));
        var method = typeof(SourceConfigurationQualification).GetMethod("RequireS003GridSurfaceGraphExtension", BindingFlags.Static | BindingFlags.NonPublic);
        Need(method != null, "Current grid/surface extension validator is unavailable");
        try { method.Invoke(null, new object[] { baseGraph, graph, receipt, root }); return false; }
        catch (TargetInvocationException e) when (e.InnerException is InvalidDataException || e.InnerException is NotSupportedException) { return true; }
    }
    static bool S003StorageGraphEpochControls(JObject context)
    {
        var sourceGraph = (JObject)context["source_graph"].DeepClone();
        var compilerGraph = (JObject)context["compiler_graph"].DeepClone();
        var currentContext = new JObject {
            ["kind"] = "OriginalS003CurrentSourceOnlyFactoryContext",
            ["source_graph"] = sourceGraph.DeepClone(),
            ["compiler_graph"] = compilerGraph.DeepClone()
        };
        bool currentUsesCompiler = JToken.DeepEquals(
            FreshSourceStorageLifetimeAuthority.ResolveS003StorageGraphDescriptor(
                new JObject { ["source_config_graph"] = compilerGraph.DeepClone() }, currentContext), compilerGraph);
        bool currentRejectsCaptureGraph = Reject(() => FreshSourceStorageLifetimeAuthority.ResolveS003StorageGraphDescriptor(
            new JObject { ["source_config_graph"] = sourceGraph.DeepClone() }, currentContext));
        var historicalContext = new JObject {
            ["kind"] = "OriginalS003SourceOnlyFactoryContext",
            ["source_graph"] = sourceGraph.DeepClone()
        };
        bool historicalUsesSourceGraph = JToken.DeepEquals(
            FreshSourceStorageLifetimeAuthority.ResolveS003StorageGraphDescriptor(
                new JObject { ["source_config_graph"] = sourceGraph.DeepClone() }, historicalContext), sourceGraph);
        bool historicalRejectsCompilerGraph = Reject(() => FreshSourceStorageLifetimeAuthority.ResolveS003StorageGraphDescriptor(
            new JObject { ["source_config_graph"] = compilerGraph.DeepClone() }, historicalContext));
        bool unknownEpochRejected = Reject(() => FreshSourceStorageLifetimeAuthority.ResolveS003StorageGraphDescriptor(
            new JObject { ["source_config_graph"] = compilerGraph.DeepClone() },
            new JObject { ["kind"] = "UnrecognizedS003Context" }));
        return currentUsesCompiler && currentRejectsCaptureGraph && historicalUsesSourceGraph &&
            historicalRejectsCompilerGraph && unknownEpochRejected;
    }
    static int Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("repository root, T4 qualification descriptor, output directory required");
        string root = Path.GetFullPath(args[0]), qualificationPath = Path.GetFullPath(args[1]), output = Path.GetFullPath(args[2]);
        var qualification = Read(qualificationPath);
        var request = Request(root, qualification, qualificationPath);
        var qualified = SourceConfigurationQualification.Context(request);
        Need((string)qualified["kind"] == "OriginalS003CurrentSourceOnlyFactoryContext" &&
            (int?)qualified["target_context"]["seed"] == 104732 && (uint?)qualified["target_context"]["setup_hash"] == 2394754492u,
            "Exact T4 current S003 context was not returned");
        bool historicalAuditSchemaFixtureRejected = HistoricalAuditFixtureRejects(qualified);
        Need(historicalAuditSchemaFixtureRejected, "Historical audit schema fixture did not reject scalar mismatch data");
        bool capturePathFixturesPassed = CapturePathFixtures(output);
        Need(capturePathFixturesPassed, "S003 capture relative/absolute path controls failed");
        bool runtimeGraphSwapRejected = RuntimeGraphRejects(request, "capture-graph");
        bool wrongCanonicalCatalogPathRejected = RuntimeGraphRejects(request, "catalog-path");
        bool wrongCanonicalCatalogHashRejected = RuntimeGraphRejects(request, "catalog-hash");
        bool wrongRuntimeSetupMatrixRejected = RuntimeGraphRejects(request, "setup-matrix");
        bool missingCanonicalCatalogNodeRejected = CompilerGraphMutationRejects(qualified, "catalog-node");
        bool missingCanonicalCatalogMetaPinRejected = CompilerGraphMutationRejects(qualified, "catalog-meta-pin");
        bool missingCanonicalCatalogAugmentationRejected = CompilerGraphMutationRejects(qualified, "catalog-augmentation");
        bool alteredCanonicalCatalogRowRejected = CompilerGraphMutationRejects(qualified, "catalog-row");
        bool unverifiedCanonicalCatalogOriginRejected = CompilerGraphMutationRejects(qualified, "catalog-origin");
        bool missingSelectionGraphNodeRejected = CompilerGraphMutationRejects(qualified, "selection-node");
        bool tamperedSelectionGraphPinRejected = CompilerGraphMutationRejects(qualified, "selection-pin");
        bool wrongSelectionExtensionReceiptPinRejected = CompilerGraphMutationRejects(qualified, "selection-receipt-pin");
        bool alteredSelectionExtensionAddedPinRejected = SelectionExtensionReceiptMutationRejects(qualified, "added-pin");
        bool alteredSelectionExtensionAddedNodeRejected = SelectionExtensionReceiptMutationRejects(qualified, "added-node");
        bool alteredSelectionExtensionRequiredAssetRejected = SelectionExtensionReceiptMutationRejects(qualified, "required-asset");
        bool alteredSelectionExtensionReferenceGuidRejected = SelectionExtensionReceiptMutationRejects(qualified, "direct-reference-guid");
        bool missingGridSurfaceNodeRejected = CompilerGraphMutationRejects(qualified, "grid-surface-node");
        bool tamperedGridSurfaceScriptPinRejected = CompilerGraphMutationRejects(qualified, "grid-surface-script-pin");
        bool wrongGridSurfaceExtensionReceiptPinRejected = CompilerGraphMutationRejects(qualified, "grid-surface-receipt-pin");
        bool alteredGridSurfaceAddedScriptPinRejected = GridSurfaceExtensionReceiptMutationRejects(qualified, "added-script-pin");
        bool alteredGridSurfaceNodeGuidRejected = GridSurfaceExtensionReceiptMutationRejects(qualified, "grid-node-guid");
        bool alteredSurfacePayloadLengthRejected = GridSurfaceExtensionReceiptMutationRejects(qualified, "surface-payload-length");
        bool alteredGridSurfaceParserPinRejected = GridSurfaceExtensionReceiptMutationRejects(qualified, "parser-pin");
        bool storageGraphEpochControlsPassed = S003StorageGraphEpochControls(qualified);
        Need(runtimeGraphSwapRejected && wrongCanonicalCatalogPathRejected && wrongCanonicalCatalogHashRejected &&
            wrongRuntimeSetupMatrixRejected && missingCanonicalCatalogNodeRejected && missingCanonicalCatalogMetaPinRejected &&
            missingCanonicalCatalogAugmentationRejected && alteredCanonicalCatalogRowRejected && unverifiedCanonicalCatalogOriginRejected &&
            missingSelectionGraphNodeRejected && tamperedSelectionGraphPinRejected && wrongSelectionExtensionReceiptPinRejected &&
            alteredSelectionExtensionAddedPinRejected && alteredSelectionExtensionAddedNodeRejected &&
            alteredSelectionExtensionRequiredAssetRejected && alteredSelectionExtensionReferenceGuidRejected &&
            missingGridSurfaceNodeRejected && tamperedGridSurfaceScriptPinRejected && wrongGridSurfaceExtensionReceiptPinRejected &&
            alteredGridSurfaceAddedScriptPinRejected && alteredGridSurfaceNodeGuidRejected && alteredSurfacePayloadLengthRejected &&
            alteredGridSurfaceParserPinRejected,
            "At least one dual-graph/catalog/matrix rejection control failed");
        Need(storageGraphEpochControlsPassed, "S003 storage graph epoch routing controls failed");

        // Use the actual authored source compiler and catalog graph to construct the exact setup.
        var compiled = AutonomousWorld.ReadCompiledInitialization(request);
        Need(compiled.setup.CatalogId == "S003" && compiled.setup.Seed == 104732 && compiled.setup.SetupHash == 2394754492u,
            "Actual authored S003 setup compiler differs from T4 target");
        var metadata = SourceMetadataCompatibility.Load(request, compiled.setup, root, root);
        Need(metadata.S003SourceOnly && metadata.FactoryDomainQualified && !metadata.NativeQualified &&
            metadata.TargetSeed == 104732 && metadata.TargetSetupHash == 2394754492u &&
            metadata.BuildingCount == 10 && metadata.UnitCount == 19,
            "Current S003 metadata compatibility did not stay in its source-only factory domain");
        metadata.VerifyUnchanged();

        var wrongSeed = CloneSetup(compiled.setup);
        wrongSeed.Seed++;
        bool wrongSetupSeedRejected = Reject(() => SourceMetadataCompatibility.Load(request, wrongSeed, root, root));
        var wrongHash = CloneSetup(compiled.setup);
        wrongHash.SetupHash ^= 1u;
        bool wrongSetupHashRejected = Reject(() => SourceMetadataCompatibility.Load(request, wrongHash, root, root));

        // Exact current-context mutants, each with recomputed descriptor hashes.
        var targetRequest = (JObject)request.DeepClone();
        bool wrongContextTargetRejected = ContextRejects(targetRequest, output, "wrong-target", "target");
        var pinRequest = (JObject)request.DeepClone();
        bool wrongGraphPinRejected = ContextRejects(pinRequest, output, "wrong-graph-pin", "pin");
        var capturePinRequest = (JObject)request.DeepClone();
        bool wrongCapturePinRejected = ContextRejects(capturePinRequest, output, "wrong-capture-pin", "capture-pin");
        var flagRequest = (JObject)request.DeepClone();
        var badQualification = Read(qualificationPath);
        badQualification["mission_ready"] = true;
        string badQualificationPath = Path.Combine(output, "wrong-readiness-flag.json");
        WriteJson(badQualificationPath, badQualification);
        flagRequest["configuration_qualification"] = Descriptor(badQualificationPath);
        bool wrongQualificationFlagRejected = Reject(() => SourceConfigurationQualification.Context(flagRequest));

        // No historical fixture is present in this epoch. Confirm the old path remains distinct
        // and its original identity checks remain in source; this is structural, not a runtime test.
        string compatibilitySource = Path.Combine(root, "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceMetadataCompatibility.s003.cs");
        string sourceText = File.ReadAllText(compatibilitySource);
        bool historicalBranchPreservedStructurally = typeof(SourceMetadataCompatibility).GetMethod("LoadS003SourceOnly", BindingFlags.Static | BindingFlags.NonPublic) != null &&
            sourceText.Contains("OriginalS003SourceOnlyFactoryContext") && sourceText.Contains("335284549b2ebb6b48c00acaa31cd796c056a9960db975396cb6628997690927");

        Need(wrongSetupSeedRejected && wrongSetupHashRejected && wrongContextTargetRejected && wrongGraphPinRejected &&
            wrongCapturePinRejected && wrongQualificationFlagRejected && historicalBranchPreservedStructurally,
            "At least one target/pin/flag rejection or historical structural control failed");
        var report = new JObject {
            ["kind"] = "S003CurrentQualificationSourceHostControls",
            ["status"] = "Passed_CurrentDualGraphS003QualificationAndMetadataSourceOnly",
            ["capture_source_graph_sha256"] = (string)qualified["source_graph"]["sha256"],
            ["runtime_compiler_graph_sha256"] = (string)qualified["compiler_graph"]["sha256"],
            ["capture_graph_is_runtime_graph"] = JToken.DeepEquals(qualified["source_graph"], qualified["compiler_graph"]),
            ["actual_setup_compiled"] = true,
            ["metadata_factory_domain_qualified"] = metadata.FactoryDomainQualified,
            ["native_context_qualified"] = metadata.NativeQualified,
            ["mission_world_created"] = false,
            ["runtime_state_transferred"] = false,
            ["source_fit_certified"] = false,
            ["mission_ready"] = false,
            ["wrong_setup_seed_rejected"] = wrongSetupSeedRejected,
            ["wrong_setup_hash_rejected"] = wrongSetupHashRejected,
            ["wrong_context_target_rejected"] = wrongContextTargetRejected,
            ["wrong_graph_pin_rejected"] = wrongGraphPinRejected,
            ["wrong_capture_pin_rejected"] = wrongCapturePinRejected,
            ["wrong_qualification_flag_rejected"] = wrongQualificationFlagRejected,
            ["historical_audit_schema_fixture_rejected"] = historicalAuditSchemaFixtureRejected,
            ["capture_path_fixtures_passed"] = capturePathFixturesPassed,
            ["runtime_graph_swap_rejected"] = runtimeGraphSwapRejected,
            ["wrong_canonical_catalog_path_rejected"] = wrongCanonicalCatalogPathRejected,
            ["wrong_canonical_catalog_hash_rejected"] = wrongCanonicalCatalogHashRejected,
            ["wrong_runtime_setup_matrix_rejected"] = wrongRuntimeSetupMatrixRejected,
            ["missing_canonical_catalog_node_rejected"] = missingCanonicalCatalogNodeRejected,
            ["missing_canonical_catalog_meta_pin_rejected"] = missingCanonicalCatalogMetaPinRejected,
            ["missing_canonical_catalog_augmentation_rejected"] = missingCanonicalCatalogAugmentationRejected,
            ["altered_canonical_catalog_row_rejected"] = alteredCanonicalCatalogRowRejected,
            ["unverified_canonical_catalog_origin_rejected"] = unverifiedCanonicalCatalogOriginRejected,
            ["missing_selection_graph_node_rejected"] = missingSelectionGraphNodeRejected,
            ["tampered_selection_graph_pin_rejected"] = tamperedSelectionGraphPinRejected,
            ["wrong_selection_extension_receipt_pin_rejected"] = wrongSelectionExtensionReceiptPinRejected,
            ["altered_selection_extension_added_pin_rejected"] = alteredSelectionExtensionAddedPinRejected,
            ["altered_selection_extension_added_node_rejected"] = alteredSelectionExtensionAddedNodeRejected,
            ["altered_selection_extension_required_asset_rejected"] = alteredSelectionExtensionRequiredAssetRejected,
            ["altered_selection_extension_reference_guid_rejected"] = alteredSelectionExtensionReferenceGuidRejected,
            ["missing_grid_surface_node_rejected"] = missingGridSurfaceNodeRejected,
            ["tampered_grid_surface_script_pin_rejected"] = tamperedGridSurfaceScriptPinRejected,
            ["wrong_grid_surface_extension_receipt_pin_rejected"] = wrongGridSurfaceExtensionReceiptPinRejected,
            ["altered_grid_surface_added_script_pin_rejected"] = alteredGridSurfaceAddedScriptPinRejected,
            ["altered_grid_surface_node_guid_rejected"] = alteredGridSurfaceNodeGuidRejected,
            ["altered_surface_payload_length_rejected"] = alteredSurfacePayloadLengthRejected,
            ["altered_grid_surface_parser_pin_rejected"] = alteredGridSurfaceParserPinRejected,
            ["current_storage_graph_uses_compiler_graph"] = storageGraphEpochControlsPassed,
            ["historical_branch_preserved_structurally"] = historicalBranchPreservedStructurally,
            ["historical_branch_runtime_tested"] = false,
            ["building_count"] = metadata.BuildingCount,
            ["unit_count"] = metadata.UnitCount,
            ["receipt"] = metadata.Receipt(),
            ["scope"] = "Actual S003 authored setup compiler and current qualification/metadata gates; no mission world or readiness authority. Historical path check is structural only because the old fixture is absent."
        };
        File.WriteAllText(Path.Combine(output, "control.json"), report.ToString(Formatting.Indented) + Environment.NewLine);
        Console.WriteLine("[S003CurrentQualificationSourceHostControls] result=Passed context=ActualCurrentDualGraph metadata=SourceOnly mutants=12 historicalAudit=SchemaFixture capturePaths=PortableFixture historical=Structural world=0 readiness=0");
        return 0;
    }
}
