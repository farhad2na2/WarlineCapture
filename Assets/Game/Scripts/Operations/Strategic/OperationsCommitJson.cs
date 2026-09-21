using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Game.Operations.Contracts;

namespace Game.Operations.Strategic
{
    public sealed class OperationsProfileCommitDocument
    {
        public string documentKind = "operations-profile-commit";
        public int profileRevision;
        public OperationsSaveData operations = new();
    }

    public static class OperationsCommitJson
    {
        public static readonly string[] ForbiddenAccountKeys =
        {
            "credits",
            "materials",
            "fuel",
            "intel",
            "commandAuthority",
            "starsEarned",
            "campaignMissionProgress",
            "commanderXp"
        };

        public static string Serialize(OperationsProfileCommitDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            StringBuilder builder = new();
            WriteValue(builder, document);
            return builder.ToString();
        }

        public static OperationsProfileCommitDocument Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Operations commit JSON is empty.", nameof(json));
            Parser parser = new(json);
            JsonNode node = parser.ParseValue();
            parser.Skip();
            if (!parser.End)
                throw new InvalidOperationException("Operations commit JSON has trailing data.");
            return (OperationsProfileCommitDocument)ConvertNode(typeof(OperationsProfileCommitDocument), node);
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            Type type = value.GetType();
            if (type == typeof(string))
            {
                WriteString(builder, (string)value);
                return;
            }

            if (type == typeof(bool))
            {
                builder.Append((bool)value ? "true" : "false");
                return;
            }

            if (type.IsEnum)
            {
                builder.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (type == typeof(int) || type == typeof(uint) || type == typeof(byte) || type == typeof(long) || type == typeof(short))
            {
                builder.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (type.IsArray)
            {
                Array array = (Array)value;
                builder.Append('[');
                for (int index = 0; index < array.Length; index++)
                {
                    if (index > 0)
                        builder.Append(',');
                    WriteValue(builder, array.GetValue(index));
                }

                builder.Append(']');
                return;
            }

            FieldInfo[] fields = InstanceFields(type);
            builder.Append('{');
            for (int index = 0; index < fields.Length; index++)
            {
                RejectForbiddenKey(fields[index].Name);
                if (index > 0)
                    builder.Append(',');
                WriteString(builder, fields[index].Name);
                builder.Append(':');
                WriteValue(builder, fields[index].GetValue(value));
            }

            builder.Append('}');
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(character); break;
                }
            }

            builder.Append('"');
        }

        private static void RejectForbiddenKey(string name)
        {
            for (int index = 0; index < ForbiddenAccountKeys.Length; index++)
            {
                if (name == ForbiddenAccountKeys[index])
                    throw new InvalidOperationException("Operations commit tried to write account wallet field '" + name + "'.");
            }
        }

        private static FieldInfo[] InstanceFields(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Array.Sort(fields, (left, right) => string.CompareOrdinal(left.Name, right.Name));
            return fields;
        }

        private static object ConvertNode(Type type, JsonNode node)
        {
            if (node is JsonNull)
                return null;
            if (type == typeof(string))
                return ((JsonString)node).Value;
            if (type == typeof(bool))
                return ((JsonBool)node).Value;
            if (type == typeof(int))
                return checked((int)((JsonNumber)node).Value);
            if (type == typeof(uint))
                return checked((uint)((JsonNumber)node).Value);
            if (type == typeof(byte))
                return checked((byte)((JsonNumber)node).Value);
            if (type == typeof(long))
                return ((JsonNumber)node).Value;
            if (type == typeof(short))
                return checked((short)((JsonNumber)node).Value);
            if (type.IsEnum)
                return Enum.ToObject(type, ((JsonNumber)node).Value);
            if (type.IsArray)
            {
                JsonArray array = (JsonArray)node;
                Type element = type.GetElementType();
                Array created = Array.CreateInstance(element, array.Items.Count);
                for (int index = 0; index < array.Items.Count; index++)
                    created.SetValue(ConvertNode(element, array.Items[index]), index);
                return created;
            }

            JsonObject obj = (JsonObject)node;
            object instance = Activator.CreateInstance(type);
            FieldInfo[] fields = InstanceFields(type);
            for (int index = 0; index < fields.Length; index++)
            {
                if (!obj.Properties.TryGetValue(fields[index].Name, out JsonNode child))
                    continue;
                fields[index].SetValue(instance, ConvertNode(fields[index].FieldType, child));
            }

            return instance;
        }

        private abstract class JsonNode
        {
        }

        private sealed class JsonNull : JsonNode
        {
            public static readonly JsonNull Instance = new();
        }

        private sealed class JsonBool : JsonNode
        {
            public JsonBool(bool value) => Value = value;
            public bool Value { get; }
        }

        private sealed class JsonNumber : JsonNode
        {
            public JsonNumber(long value) => Value = value;
            public long Value { get; }
        }

        private sealed class JsonString : JsonNode
        {
            public JsonString(string value) => Value = value;
            public string Value { get; }
        }

        private sealed class JsonArray : JsonNode
        {
            public List<JsonNode> Items { get; } = new();
        }

        private sealed class JsonObject : JsonNode
        {
            public Dictionary<string, JsonNode> Properties { get; } = new(StringComparer.Ordinal);
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _index;

            public Parser(string text) => _text = text;
            public bool End => _index >= _text.Length;

            public JsonNode ParseValue()
            {
                Skip();
                if (End)
                    throw new InvalidOperationException("Unexpected end of Operations commit JSON.");
                char character = _text[_index];
                if (character == '{')
                    return ParseObject();
                if (character == '[')
                    return ParseArray();
                if (character == '"')
                    return new JsonString(ParseString());
                if (character == 't')
                {
                    Expect("true");
                    return new JsonBool(true);
                }

                if (character == 'f')
                {
                    Expect("false");
                    return new JsonBool(false);
                }

                if (character == 'n')
                {
                    Expect("null");
                    return JsonNull.Instance;
                }

                return new JsonNumber(ParseNumber());
            }

            public void Skip()
            {
                while (!End && char.IsWhiteSpace(_text[_index]))
                    _index++;
            }

            private JsonObject ParseObject()
            {
                Expect("{");
                JsonObject obj = new();
                Skip();
                if (Peek('}'))
                {
                    _index++;
                    return obj;
                }

                while (true)
                {
                    Skip();
                    string name = ParseString();
                    Skip();
                    Expect(":");
                    obj.Properties[name] = ParseValue();
                    Skip();
                    if (Peek('}'))
                    {
                        _index++;
                        return obj;
                    }

                    Expect(",");
                }
            }

            private JsonArray ParseArray()
            {
                Expect("[");
                JsonArray array = new();
                Skip();
                if (Peek(']'))
                {
                    _index++;
                    return array;
                }

                while (true)
                {
                    array.Items.Add(ParseValue());
                    Skip();
                    if (Peek(']'))
                    {
                        _index++;
                        return array;
                    }

                    Expect(",");
                }
            }

            private string ParseString()
            {
                Expect("\"");
                StringBuilder builder = new();
                while (!End)
                {
                    char character = _text[_index++];
                    if (character == '"')
                        return builder.ToString();
                    if (character != '\\')
                    {
                        builder.Append(character);
                        continue;
                    }

                    if (End)
                        break;
                    char escaped = _text[_index++];
                    switch (escaped)
                    {
                        case '\\': builder.Append('\\'); break;
                        case '"': builder.Append('"'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        default: builder.Append(escaped); break;
                    }
                }

                throw new InvalidOperationException("Unterminated Operations commit string.");
            }

            private long ParseNumber()
            {
                int start = _index;
                if (Peek('-'))
                    _index++;
                if (End || !char.IsDigit(_text[_index]))
                    throw new InvalidOperationException("Invalid Operations commit number.");
                while (!End && char.IsDigit(_text[_index]))
                    _index++;
                return long.Parse(_text.Substring(start, _index - start), CultureInfo.InvariantCulture);
            }

            private void Expect(string token)
            {
                Skip();
                for (int index = 0; index < token.Length; index++)
                {
                    if (End || _text[_index] != token[index])
                        throw new InvalidOperationException("Expected '" + token + "' in Operations commit JSON.");
                    _index++;
                }
            }

            private bool Peek(char character)
            {
                return !End && _text[_index] == character;
            }
        }
    }

    public sealed class OperationsProfileStore
    {
        private OperationsProfileCommitDocument _committed;
        private string _committedJson;
        private OperationsProfileCommitDocument _pending;
        private string _pendingJson;
        private bool _readOnly;

        private OperationsProfileStore(
            OperationsProfileCommitDocument committed,
            string json,
            bool readOnly,
            byte[] campaignEnvelope,
            byte[] quickGameEnvelope)
        {
            _committed = committed;
            _committedJson = json;
            _readOnly = readOnly;
            CampaignEnvelope = campaignEnvelope ?? new byte[] { 1, 2, 3 };
            QuickGameEnvelope = quickGameEnvelope ?? new byte[] { 4, 5, 6 };
        }

        public byte[] CampaignEnvelope { get; }
        public byte[] QuickGameEnvelope { get; }
        public bool HasPending => _pending != null;
        public bool IsReadOnly => _readOnly;
        public int ProfileRevision => _committed.profileRevision;
        public OperationsSaveData Committed => _committed.operations;
        public string CommittedJson => _committedJson;

        public static OperationsProfileStore CreateNew(byte[] campaignEnvelope, byte[] quickGameEnvelope)
        {
            OperationsSaveMigrationResult migration = OperationsSaveMigration.Migrate(null);
            return FromMigration(migration, campaignEnvelope, quickGameEnvelope);
        }

        public static OperationsProfileStore FromMigration(
            OperationsSaveMigrationResult migration,
            byte[] campaignEnvelope,
            byte[] quickGameEnvelope)
        {
            OperationsProfileCommitDocument document = new()
            {
                profileRevision = migration.Data.profileRevision,
                operations = migration.Data
            };
            document.operations.profileRevision = document.profileRevision;
            string json = OperationsCommitJson.Serialize(document);
            OperationsProfileCommitDocument detached = OperationsCommitJson.Deserialize(json);
            return new OperationsProfileStore(detached, json, !migration.CanWrite, campaignEnvelope, quickGameEnvelope);
        }

        public static OperationsProfileStore FromCommittedJson(
            string json,
            byte[] campaignEnvelope,
            byte[] quickGameEnvelope)
        {
            OperationsProfileCommitDocument document = OperationsCommitJson.Deserialize(json);
            string canonical = OperationsCommitJson.Serialize(document);
            return new OperationsProfileStore(document, canonical, false, campaignEnvelope, quickGameEnvelope);
        }

        public OperationsSaveData CloneCommitted()
        {
            OperationsProfileCommitDocument clone = OperationsCommitJson.Deserialize(_committedJson);
            return clone.operations;
        }

        public bool TryBeginCommit(int expectedRevision, OperationsSaveData next, out OperationsReasonCode reason)
        {
            reason = OperationsReasonCode.None;
            if (_readOnly)
            {
                reason = OperationsReasonCode.SchemaUnknown;
                return false;
            }

            if (HasPending)
            {
                reason = OperationsReasonCode.TechnicalFailure;
                return false;
            }

            if (expectedRevision != _committed.profileRevision)
            {
                reason = OperationsReasonCode.InvalidRevision;
                return false;
            }

            _pending = new OperationsProfileCommitDocument
            {
                profileRevision = next.profileRevision,
                operations = next
            };
            _pendingJson = OperationsCommitJson.Serialize(_pending);
            _pending = OperationsCommitJson.Deserialize(_pendingJson);
            return true;
        }

        public void CompleteCommit()
        {
            if (!HasPending)
                throw new InvalidOperationException("No Operations commit is pending.");
            _committed = _pending;
            _committedJson = _pendingJson;
            _pending = null;
            _pendingJson = null;
        }

        public void AbandonPendingAsCrash()
        {
            _pending = null;
            _pendingJson = null;
        }
    }
}
