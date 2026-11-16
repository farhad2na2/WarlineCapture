using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Exact, scoped replay of an already captured layout projection. It is not
    // a general JsonUtility implementation or a native-readiness claim.
    internal sealed class SourceValidatedLayoutJson : IDisposable
    {
        private static SourceValidatedLayoutJson current;
        private readonly string path, sha, json;
        private readonly object validated;
        private readonly Type projectionType;

        private SourceValidatedLayoutJson(string path, string sha, string json, object validated, Type projectionType)
        { this.path = path; this.sha = sha; this.json = json; this.validated = validated; this.projectionType = projectionType; }

        internal static SourceValidatedLayoutJson OpenDescriptor(SkirmishMapLayoutConfig layout, JToken descriptor)
        {
            var value = descriptor as JObject;
            if (value == null || value.Count != 2 || value["path"]?.Type != JTokenType.String ||
                value["sha256"]?.Type != JTokenType.String)
                throw new InvalidDataException("Exact layout replay path and hash required");
            return Open(layout, (string)value["path"], (string)value["sha256"]);
        }

        internal static SourceValidatedLayoutJson Open(SkirmishMapLayoutConfig layout, string path, string sha)
        {
            if (current != null || layout == null || !Path.IsPathFullyQualified(path ?? ""))
                throw new InvalidDataException("One exact pinned layout replay context required");
            string raw = new UTF8Encoding(false, true).GetString(AutonomousWorld.PinnedFile(path, sha, 4 * 1024 * 1024));
            var document = JObject.Parse(raw, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            Type type = typeof(SkirmishMeasuredLayoutBinder.LayoutValues);
            if (!SchemaMatches(type, document)) throw new InvalidDataException("Captured layout schema differs from original projection");
            object captured = JsonConvert.DeserializeObject(raw, type);
            object authored = SkirmishMeasuredLayoutBinder.LayoutValues.From(layout);
            if (!TypedEqual(authored, captured, type)) throw new InvalidDataException("Captured layout values differ from authored source");
            current = new SourceValidatedLayoutJson(path, sha, raw, captured, type);
            return current;
        }

        internal static string ToJson(object value)
        {
            var context = current;
            if (context == null || value == null || value.GetType() != context.projectionType ||
                !TypedEqual(value, context.validated, context.projectionType))
                throw new NotSupportedException("Generic Unity JsonUtility canonical encoding is unsupported in initial generated profile; use shared pure reviewed-value contract");
            return context.json;
        }

        public void Dispose()
        {
            try { AutonomousWorld.PinnedFile(path, sha, 4 * 1024 * 1024); }
            finally { if (ReferenceEquals(current, this)) current = null; }
        }

        private static bool SchemaMatches(Type type, JObject value)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(x => x.MetadataToken).ToArray();
            if (value == null || !value.Properties().Select(x => x.Name).SequenceEqual(fields.Select(x => x.Name))) return false;
            foreach (var field in fields)
            {
                JToken child = value[field.Name];
                if (child == null) return false;
                Type childType = field.FieldType;
                if (childType.IsArray && child.Type == JTokenType.Array && childType.GetElementType() != typeof(string))
                {
                    foreach (JToken item in (JArray)child)
                        if (!(item is JObject row) || !SchemaMatches(childType.GetElementType(), row)) return false;
                }
                else if (!childType.IsArray && childType.IsValueType && !childType.IsPrimitive && !childType.IsEnum && child.Type == JTokenType.Object &&
                    !SchemaMatches(childType, (JObject)child)) return false;
            }
            return true;
        }

        private static bool TypedEqual(object left, object right, Type type)
        {
            if (left == null || right == null) return left == right;
            if (type.IsArray)
            {
                var a = (Array)left; var b = (Array)right;
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++) if (!TypedEqual(a.GetValue(i), b.GetValue(i), type.GetElementType())) return false;
                return true;
            }
            if (type == typeof(float)) return BitConverter.ToInt32(BitConverter.GetBytes((float)left), 0) == BitConverter.ToInt32(BitConverter.GetBytes((float)right), 0);
            if (type.IsPrimitive || type.IsEnum || type == typeof(string)) return left.Equals(right);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(x => x.MetadataToken))
                if (!TypedEqual(field.GetValue(left), field.GetValue(right), field.FieldType)) return false;
            return true;
        }
    }
}
