using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Disposable in-memory compiler probe. Fingerprints are computed values,
    // not evidence of review, and no view, geometry or asset is authored.
    internal static class S006PartialLogicalBindingProbe
    {
        private static JObject Snapshot(SkirmishMissionLogicalBindingConfig binding, SkirmishMissionLogicalVariant variant)
        {
            var view = new JObject();
            foreach (var field in typeof(SkirmishMissionViewProfile).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly))
            {
                object value = field.GetValue(variant.View);
                view[field.Name] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
            }
            return new JObject {
                ["schema_version"] = binding.SchemaVersion, ["content_version"] = binding.ContentVersion,
                ["definition_version"] = binding.DefinitionVersion, ["value_format"] = (int)binding.ValueFormat,
                ["catalog_id"] = binding.CatalogId, ["definition_id"] = binding.DefinitionId,
                ["source_operation_map_id"] = binding.SourceOperationMapId, ["source_scene_guid"] = binding.SourceSceneGuid,
                ["source_content_hash"] = binding.SourceContentHash, ["source_semantic_fingerprint"] = binding.SourceSemanticFingerprint,
                ["layout_id"] = binding.LayoutId, ["layout_semantic_fingerprint"] = binding.ReviewedLayoutSemanticFingerprint,
                ["objective_kind"] = (int)binding.ObjectiveKind,
                ["variant"] = new JObject { ["size"] = (int)variant.Size, ["difficulty"] = (int)variant.Difficulty,
                    ["opening_focus_role"] = (int)variant.OpeningFocusRole, ["opening_actor_id"] = variant.OpeningFocusStableActorId,
                    ["opening_smooth_seconds"] = variant.OpeningFocusSmoothSeconds, ["minimum_elevation"] = variant.SourceMinimumElevation,
                    ["maximum_elevation"] = variant.SourceMaximumElevation, ["framing"] = JArray.FromObject(variant.Framing),
                    ["view_fingerprint"] = variant.ReviewedViewFingerprint, ["view"] = view },
                ["reviewed"] = false, ["physical_fit_verified"] = false, ["runtime_admission"] = false, ["mission_ready"] = false };
        }

        internal static JObject Probe(SkirmishScenarioDefinitionConfig definition, SkirmishResolvedSetup setup,
            SkirmishMissionViewProfile existingView, bool includeCandidateView, JObject framingTemplate = null, string externalDirectory = null)
        {
            if (definition == null || setup == null || definition.CatalogId != "S006" ||
                definition.MissionLogicalBinding != null || definition.CampaignMap?.SourceSceneReference == null)
                throw new InvalidOperationException("Exact unbound S006 fixture required");
            var inputs = SkirmishMissionLogicalAuthoringInputs.Capture(definition, setup);
            var map = definition.CampaignMap;
            var partial = Activator.CreateInstance<SkirmishMissionLogicalBindingConfig>();
            partial.SchemaVersion = 1; partial.ContentVersion = 1; partial.DefinitionVersion = definition.ContentVersion;
            partial.ValueFormat = inputs.ValueFormat; partial.CatalogId = definition.CatalogId; partial.DefinitionId = definition.DefinitionId;
            partial.SourceOperationMapId = map.OperationMapId; partial.SourceSceneGuid = map.SourceSceneReference.AssetGUID;
            partial.SourceContentHash = map.ContentHash; partial.SourceSemanticFingerprint = inputs.SourceSemanticFingerprint;
            partial.LayoutId = definition.MapLayout.LayoutId; partial.ReviewedLayoutSemanticFingerprint = inputs.LayoutSemanticFingerprint;
            partial.ObjectiveKind = setup.ObjectiveKind;
            var variant = new SkirmishMissionLogicalVariant { Size = setup.SizeId, Difficulty = setup.DifficultyId,
                SourceMinimumElevation = inputs.SourceMinimumElevation, SourceMaximumElevation = inputs.SourceMaximumElevation };
            SkirmishMissionViewProfile proposed = null;
            if (includeCandidateView)
            {
                if (existingView == null || existingView.CatalogId != "S005" || !existingView.IsValid)
                    throw new InvalidOperationException("Exact valid S005 source view required");
                proposed = Activator.CreateInstance<SkirmishMissionViewProfile>();
                foreach (var field in typeof(SkirmishMissionViewProfile).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly))
                    field.SetValue(proposed, field.GetValue(existingView));
                proposed.CatalogId = definition.CatalogId; proposed.DefinitionId = definition.DefinitionId;
                proposed.DefinitionVersion = definition.ContentVersion; proposed.CampaignSourceContentHash = map.ContentHash;
                proposed.LayoutContentHash = definition.MapLayout.ContentHash; proposed.ObjectiveKind = setup.ObjectiveKind;
                proposed.AuthoredGenericBaseAssault = true; proposed.AuthoredNonBaseAssault = false;
                proposed.OpeningFocusPolicy = SkirmishMissionOpeningFocusPolicy.None; proposed.OpeningFocusStableActorId = null;
                proposed.UseSeparateAircraftYaw = false;
                variant.View = proposed;
            }
            if (framingTemplate != null)
            {
                if (proposed == null || (string)framingTemplate["kind"] != "UnreviewedS049FramingTemplate")
                    throw new InvalidOperationException("Exact unreviewed framing template and candidate view required");
                var rows = ((JArray)framingTemplate["rows"]).OfType<JObject>().ToDictionary(x => (int)x["Role"]);
                var fallback = new Dictionary<int, int> { [11] = 10, [15] = 14, [16] = 14, [18] = 17, [19] = 17, [20] = 17 };
                float templateMax = (float)framingTemplate["source_max_elevation"];
                var framing = new List<SkirmishRoleFramingRequirement>();
                foreach (var role in inputs.RequiredFramingRoles)
                {
                    int target = (int)role;
                    int sourceRole = rows.ContainsKey(target) ? target : fallback.TryGetValue(target, out int mapped) ? mapped : 0;
                    if (sourceRole == 0 || !rows.TryGetValue(sourceRole, out var row)) throw new InvalidOperationException("No explicit framing template for role " + target);
                    var item = row.ToObject<SkirmishRoleFramingRequirement>();
                    item.Role = role;
                    item.MinimumSubjectElevation = inputs.SourceMinimumElevation;
                    item.MaximumSubjectElevation = (item.Stages & SkirmishViewCaptureStage.FollowCruising) != 0
                        ? inputs.SourceMaximumElevation + (item.MaximumSubjectElevation - templateMax)
                        : inputs.SourceMaximumElevation;
                    framing.Add(item);
                }
                variant.Framing = framing.ToArray();
                variant.OpeningFocusRole = setup.Forces.First(x => x.FactionId == 1 && x.Quantity > 0).RoleKind;
                variant.OpeningFocusStableActorId = inputs.ActualOpeningPlayerActorIds.First();
                variant.OpeningFocusSmoothSeconds = (float)framingTemplate["opening_focus_smooth_seconds"];
                variant.ReviewedViewFingerprint = SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
            }
            partial.Variants = new[] { variant };
            try
            {
                definition.BindMissionLogicalBinding(partial);
                bool accepted = SkirmishMissionLogicalBindingCompiler.TryCompile(definition, setup, out var compiled, out string error);
                if (framingTemplate == null && (accepted || compiled != null)) throw new InvalidOperationException("Incomplete view candidate must not compile");
                var negatives = new JArray();
                JObject externalCandidate = null; bool roundTripAccepted = false; bool roundTripMatches = false;
                if (framingTemplate != null && accepted)
                {
                    externalCandidate = Snapshot(partial, variant);
                    if (!Path.IsPathFullyQualified(externalDirectory ?? "") || !Directory.Exists(externalDirectory))
                        throw new InvalidOperationException("Exact external candidate directory required");
                    string candidatePath = Path.Combine(externalDirectory, "binding-" + setup.DifficultyId + "-" + setup.SizeId + ".json");
                    using (var stream = new FileStream(candidatePath, FileMode.CreateNew, FileAccess.Write))
                    using (var writer = new StreamWriter(stream)) writer.Write(externalCandidate.ToString(Newtonsoft.Json.Formatting.Indented));
                    var restored = SourceUnqualifiedLogicalBindingCandidate.Load(candidatePath, AutonomousWorld.Hash(File.ReadAllBytes(candidatePath)), setup);
                    definition.BindMissionLogicalBinding(restored);
                    roundTripAccepted = SkirmishMissionLogicalBindingCompiler.TryCompile(definition, setup, out var roundTrip, out _);
                    roundTripMatches = roundTripAccepted && roundTrip != null && roundTrip.Fingerprint == compiled.Fingerprint;
                    definition.BindMissionLogicalBinding(partial);
                    if (!roundTripMatches) throw new InvalidOperationException("External candidate round trip changed the original compiler result");
                    string WriteNegative(string label, string content)
                    {
                        string path = Path.Combine(externalDirectory, "negative-" + label + "-" + setup.DifficultyId + "-" + setup.SizeId + ".json");
                        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                        using var writer = new StreamWriter(stream); writer.Write(content); return path;
                    }
                    void RequireExternalRejected(string label, string path, string expectedHash)
                    {
                        bool rejected = false; string rejection = null;
                        try { SourceUnqualifiedLogicalBindingCandidate.Load(path, expectedHash, setup); }
                        catch (InvalidDataException e) { rejected = true; rejection = e.Message; }
                        catch (Newtonsoft.Json.JsonException e) { rejected = true; rejection = e.Message; }
                        catch (InvalidOperationException e) { rejected = true; rejection = e.Message; }
                        if (!rejected) throw new InvalidOperationException("Invalid external candidate accepted: " + label);
                        negatives.Add(new JObject { ["case"] = label, ["rejected"] = true, ["reason"] = rejection });
                    }
                    var tampered = (JObject)externalCandidate.DeepClone(); tampered["catalog_id"] = "S005";
                    var negativePath = WriteNegative("identity", tampered.ToString(Newtonsoft.Json.Formatting.None));
                    RequireExternalRejected("external_wrong_identity", negativePath, AutonomousWorld.Hash(File.ReadAllBytes(negativePath)));
                    tampered = (JObject)externalCandidate.DeepClone(); tampered["mission_ready"] = true;
                    negativePath = WriteNegative("readiness", tampered.ToString(Newtonsoft.Json.Formatting.None));
                    RequireExternalRejected("external_readiness_claim", negativePath, AutonomousWorld.Hash(File.ReadAllBytes(negativePath)));
                    RequireExternalRejected("external_wrong_hash", candidatePath, new string('0', 64));
                    string candidateJson = externalCandidate.ToString(Newtonsoft.Json.Formatting.None);
                    negativePath = WriteNegative("duplicate", "{\"catalog_id\":\"S006\"," + candidateJson.Substring(1));
                    RequireExternalRejected("external_duplicate_key", negativePath, AutonomousWorld.Hash(File.ReadAllBytes(negativePath)));
                    tampered = (JObject)externalCandidate.DeepClone(); tampered["unexpected"] = 1;
                    negativePath = WriteNegative("unknown_field", tampered.ToString(Newtonsoft.Json.Formatting.None));
                    RequireExternalRejected("external_unknown_field", negativePath, AutonomousWorld.Hash(File.ReadAllBytes(negativePath)));
                    tampered = (JObject)externalCandidate.DeepClone();
                    tampered["variant"]["size"] = (int)setup.SizeId == 1 ? 2 : 1;
                    negativePath = WriteNegative("wrong_size", tampered.ToString(Newtonsoft.Json.Formatting.None));
                    RequireExternalRejected("external_wrong_size", negativePath, AutonomousWorld.Hash(File.ReadAllBytes(negativePath)));
                    tampered = (JObject)externalCandidate.DeepClone(); ((JObject)tampered["variant"]["framing"][0])["unexpected"] = 1;
                    negativePath = WriteNegative("unknown_framing", tampered.ToString(Newtonsoft.Json.Formatting.None));
                    RequireExternalRejected("external_unknown_framing_field", negativePath, AutonomousWorld.Hash(File.ReadAllBytes(negativePath)));
                    tampered = (JObject)externalCandidate.DeepClone(); tampered["mission_ready"] = "false";
                    negativePath = WriteNegative("flag_type", tampered.ToString(Newtonsoft.Json.Formatting.None));
                    RequireExternalRejected("external_invalid_flag_type", negativePath, AutonomousWorld.Hash(File.ReadAllBytes(negativePath)));
                    void RequireRejected(string label)
                    {
                        bool unexpectedlyAccepted = SkirmishMissionLogicalBindingCompiler.TryCompile(definition, setup, out var negativeResult, out string reason);
                        if (unexpectedlyAccepted || negativeResult != null) throw new InvalidOperationException("Invalid candidate accepted: " + label);
                        negatives.Add(new JObject { ["case"] = label, ["rejected"] = true, ["reason"] = reason });
                    }
                    var originalFraming = variant.Framing;
                    variant.Framing = originalFraming.Take(originalFraming.Length - 1).ToArray(); RequireRejected("missing_role"); variant.Framing = originalFraming;
                    string viewFingerprint = variant.ReviewedViewFingerprint;
                    variant.ReviewedViewFingerprint = new string('0', 64); RequireRejected("wrong_view_fingerprint"); variant.ReviewedViewFingerprint = viewFingerprint;
                    string catalog = partial.CatalogId;
                    partial.CatalogId = "S005"; RequireRejected("wrong_binding_identity"); partial.CatalogId = catalog;
                    string actor = variant.OpeningFocusStableActorId;
                    variant.OpeningFocusStableActorId = "nonexistent.fixture.actor"; RequireRejected("wrong_opening_actor"); variant.OpeningFocusStableActorId = actor;
                }
                bool metadataValid = map.TryValidateMetadata(out string metadataError);
                bool referencesValid = map.TryValidateLocalContentReferences(out string referencesError);
                return new JObject { ["compiler_accepted"] = accepted, ["compiler_error"] = error,
                    ["source_metadata_valid"] = metadataValid, ["source_metadata_error"] = metadataError,
                    ["source_references_valid"] = referencesValid, ["source_references_error"] = referencesError,
                    ["computed_fingerprints_are_review"] = false, ["view_supplied"] = proposed != null,
                    ["candidate_view_valid"] = proposed != null && proposed.IsValid,
                    ["candidate_view_matches"] = proposed != null && proposed.Matches(setup, definition),
                    ["candidate_view_source"] = proposed == null ? null : "UnreviewedIdentityAdaptationOfS005SourceValues",
                    ["candidate_framing_supplied"] = framingTemplate != null,
                    ["candidate_framing_role_count"] = variant.Framing?.Length ?? 0,
                    ["compiled_binding_returned"] = compiled != null,
                    ["candidate_binding_fingerprint"] = compiled?.Fingerprint,
                    ["external_candidate"] = externalCandidate,
                    ["external_roundtrip_accepted"] = roundTripAccepted,
                    ["external_roundtrip_matches"] = roundTripMatches,
                    ["negative_controls"] = negatives,
                    ["native_evidence_supplied"] = false, ["runtime_admission"] = false, ["mission_ready"] = false };
            }
            finally { definition.BindMissionLogicalBinding(null); }
        }
    }
}
