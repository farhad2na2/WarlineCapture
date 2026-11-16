using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Diagnostic data materialization only. The caller must invoke the original
    // binding compiler and must not interpret this object as reviewed or admitted.
    internal static class SourceUnqualifiedLogicalBindingCandidate
    {
        private static void Exact(JObject value, params string[] names)
        {
            if (value == null || !new HashSet<string>(value.Properties().Select(x => x.Name), StringComparer.Ordinal).SetEquals(names) || value.Count != names.Length)
                throw new InvalidDataException("Exact external candidate schema required");
        }

        private static void Finite(JToken token)
        {
            if (token.Type == JTokenType.Float)
            {
                double value = (double)token;
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Non-finite candidate number");
            }
            if (token is JContainer container) foreach (var child in container.Children()) Finite(child is JProperty p ? p.Value : child);
        }

        internal static SkirmishMissionLogicalBindingConfig Load(string path, string sha256, SkirmishResolvedSetup setup)
        {
            if (!Path.IsPathFullyQualified(path ?? "") || setup == null) throw new InvalidDataException("Absolute pinned candidate and actual setup required");
            byte[] bytes = AutonomousWorld.PinnedFile(path, sha256, 2 * 1024 * 1024);
            string json = new UTF8Encoding(false, true).GetString(bytes);
            var input = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            Finite(input);
            Exact(input, "schema_version", "content_version", "definition_version", "value_format", "catalog_id", "definition_id",
                "source_operation_map_id", "source_scene_guid", "source_content_hash", "source_semantic_fingerprint", "layout_id",
                "layout_semantic_fingerprint", "objective_kind", "variant", "reviewed", "physical_fit_verified", "runtime_admission", "mission_ready");
            if (new[] { "reviewed", "physical_fit_verified", "runtime_admission", "mission_ready" }.Any(k => input[k].Type != JTokenType.Boolean) ||
                (bool?)input["reviewed"] != false || (bool?)input["physical_fit_verified"] != false ||
                (bool?)input["runtime_admission"] != false || (bool?)input["mission_ready"] != false ||
                (string)input["catalog_id"] != setup.CatalogId || (string)input["definition_id"] != setup.DefinitionId)
                throw new InvalidDataException("Unqualified candidate scope and actual setup identity required");
            var data = input["variant"] as JObject;
            Exact(data, "size", "difficulty", "opening_focus_role", "opening_actor_id", "opening_smooth_seconds", "minimum_elevation", "maximum_elevation", "framing", "view_fingerprint", "view");
            if ((int?)data["size"] != (int)setup.SizeId || (int?)data["difficulty"] != (int)setup.DifficultyId)
                throw new InvalidDataException("Exact setup difficulty and size required");
            var result = Activator.CreateInstance<SkirmishMissionLogicalBindingConfig>();
            result.SchemaVersion=(int)input["schema_version"]; result.ContentVersion=(int)input["content_version"];
            result.DefinitionVersion=(int)input["definition_version"]; result.ValueFormat=(SkirmishBindingValueFormat)(int)input["value_format"];
            result.CatalogId=(string)input["catalog_id"]; result.DefinitionId=(string)input["definition_id"];
            result.SourceOperationMapId=(string)input["source_operation_map_id"]; result.SourceSceneGuid=(string)input["source_scene_guid"];
            result.SourceContentHash=(string)input["source_content_hash"]; result.SourceSemanticFingerprint=(string)input["source_semantic_fingerprint"];
            result.LayoutId=(string)input["layout_id"]; result.ReviewedLayoutSemanticFingerprint=(string)input["layout_semantic_fingerprint"];
            result.ObjectiveKind=(SkirmishObjectiveKind)(int)input["objective_kind"];
            var viewData=data["view"] as JObject;var view=Activator.CreateInstance<SkirmishMissionViewProfile>();
            var fields=typeof(SkirmishMissionViewProfile).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly);
            Exact(viewData, fields.Select(x=>x.Name).ToArray());
            foreach(var field in fields)
            {
                var token=viewData[field.Name];
                field.SetValue(view,token.Type==JTokenType.Null ? null : token.ToObject(field.FieldType));
            }
            var framing=data["framing"] as JArray;
            if (framing == null || framing.Count == 0 || framing.Count > 256) throw new InvalidDataException("Bounded framing rows required");
            var framingFields=typeof(SkirmishRoleFramingRequirement).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly).Select(x=>x.Name).ToArray();
            foreach(var row in framing) Exact(row as JObject, framingFields);
            result.Variants=new[] { new SkirmishMissionLogicalVariant { Size=(SkirmishSizeId)(int)data["size"],
                Difficulty=(SkirmishDifficultyId)(int)data["difficulty"], OpeningFocusRole=(SkirmishRoleKind)(int)data["opening_focus_role"],
                OpeningFocusStableActorId=(string)data["opening_actor_id"], OpeningFocusSmoothSeconds=(float)data["opening_smooth_seconds"],
                SourceMinimumElevation=(float)data["minimum_elevation"], SourceMaximumElevation=(float)data["maximum_elevation"],
                Framing=framing.ToObject<SkirmishRoleFramingRequirement[]>(), ReviewedViewFingerprint=(string)data["view_fingerprint"], View=view } };
            return result;
        }
    }
}
