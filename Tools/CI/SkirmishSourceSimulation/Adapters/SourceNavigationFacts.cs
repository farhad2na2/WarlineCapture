using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    // A finite authored-value format. It contains no allocated runtime entities,
    // current gameplay decisions, grid results, bank values or factory outputs.
    internal static class SourceNavigationFacts
    {
        internal const string Format = "source-navigation-facts-v1";
        internal static readonly Type[] Types = {
            typeof(UnitGrid), typeof(GridBlockerSize), typeof(Unity.Transforms.LocalTransform),
            typeof(StaticGridBlocker), typeof(OperationMapBuildingComponent),
            typeof(RuntimeBuildingCombatInfo), typeof(UnitHealth), typeof(Faction)
        };
        static readonly byte[] Magic = Encoding.ASCII.GetBytes("NAVFACT1\0");
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal static string InputAuditDirectory;
        static Dictionary<string, string> consumedPins;
        internal static string Schema => AutonomousWorld.Hash(Utf8.GetBytes(string.Join("\n", Types.Select(Describe))));
        static JObject Parse(byte[] bytes) => JObject.Parse(Utf8.GetString(bytes), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });

        internal static string StreamSha(string path)
        {
            path = AutonomousWorld.ResolvePinnedPath(path);
            Need(Path.IsPathRooted(path), "Absolute authored source path required");
            var file = new FileInfo(path);
            Need(file.Exists && file.Length > 0 && file.Length <= 128 * 1024 * 1024 &&
                (file.Attributes & FileAttributes.ReparsePoint) == 0, "Bounded regular authored source file required");
            using (var stream = File.OpenRead(path))
            using (var hasher = SHA256.Create())
                return BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static void ObserveInputs(Dictionary<string, string> pins)
        {
            consumedPins = new Dictionary<string, string>(pins, StringComparer.Ordinal);
            if (InputAuditDirectory == null) return;
            File.WriteAllText(Path.Combine(InputAuditDirectory, "navigation-inputs-before-materialization.json"),
                new JObject { ["kind"] = "VerifiedAuthoredNavigationInputs", ["phase"] = "AfterPinnedDecodeBeforeFirstEntityWrite", ["producer_sha256"] = SourceNavigationFactsIdentity.Producer, ["schema_sha256"] = Schema, ["pins"] = JObject.FromObject(consumedPins) }.ToString());
        }

        internal static void VerifyConsumedInputsAfterRun()
        {
            if (consumedPins == null) return;
            foreach (var pin in consumedPins) Need(StreamSha(pin.Key) == pin.Value, "Consumed navigation input changed during run: " + pin.Key);
            if (InputAuditDirectory != null)
                File.WriteAllText(Path.Combine(InputAuditDirectory, "navigation-inputs-after-run.json"),
                    new JObject { ["kind"] = "VerifiedAuthoredNavigationInputs", ["phase"] = "AfterRunRehashed", ["pins"] = JObject.FromObject(consumedPins) }.ToString());
        }

        static void Need(bool condition, string reason)
        {
            if (!condition) throw new InvalidDataException(reason);
        }

        static FieldInfo[] Fields(Type type) => type.GetFields(Instance).Where(f => !f.IsStatic).ToArray();

        static string Describe(Type type)
        {
            if (type.IsEnum)
                return "enum:" + type.FullName + ":" + Describe(Enum.GetUnderlyingType(type)) + ":" +
                    string.Join(",", Enum.GetNames(type).Select(n => n + "=" + Convert.ToString(Convert.ChangeType(Enum.Parse(type, n), Enum.GetUnderlyingType(type)), System.Globalization.CultureInfo.InvariantCulture)));
            if (type.IsPrimitive || type == typeof(string))
            {
                Need(type == typeof(byte) || type == typeof(int) || type == typeof(uint) ||
                    type == typeof(short) || type == typeof(ushort) || type == typeof(long) ||
                    type == typeof(ulong) || type == typeof(sbyte) || type == typeof(float) ||
                    type == typeof(double) || type == typeof(bool) || type == typeof(string), "Closed scalar schema required");
                return type.FullName;
            }
            Need(type.IsValueType && !type.IsGenericType && !type.IsExplicitLayout, "Closed authored value schema required");
            return type.FullName + "{" + string.Join(";", Fields(type).Select(f => f.Name + ":" + Describe(f.FieldType))) + "}";
        }

        static void WriteText(BinaryWriter writer, string value, int maximum)
        {
            if (value == null) { writer.Write(-1); return; }
            byte[] bytes = Utf8.GetBytes(value);
            Need(bytes.Length <= maximum, "Bounded authored string required");
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        static string ReadText(BinaryReader reader, int maximum)
        {
            int length = reader.ReadInt32();
            Need(length >= -1 && length <= maximum, "Authored string length out of bounds");
            if (length == -1) return null;
            var bytes = reader.ReadBytes(length);
            Need(bytes.Length == length, "Truncated authored string");
            return Utf8.GetString(bytes);
        }

        internal static void WriteValue(BinaryWriter writer, Type type, object value)
        {
            if (type.IsEnum) { WriteValue(writer, Enum.GetUnderlyingType(type), Convert.ChangeType(value, Enum.GetUnderlyingType(type))); return; }
            if (type == typeof(byte)) { writer.Write((byte)value); return; }
            if (type == typeof(sbyte)) { writer.Write((sbyte)value); return; }
            if (type == typeof(short)) { writer.Write((short)value); return; }
            if (type == typeof(ushort)) { writer.Write((ushort)value); return; }
            if (type == typeof(int)) { writer.Write((int)value); return; }
            if (type == typeof(uint)) { writer.Write((uint)value); return; }
            if (type == typeof(long)) { writer.Write((long)value); return; }
            if (type == typeof(ulong)) { writer.Write((ulong)value); return; }
            if (type == typeof(float)) { writer.Write(BitConverter.ToInt32(BitConverter.GetBytes((float)value), 0)); return; }
            if (type == typeof(double)) { writer.Write(BitConverter.DoubleToInt64Bits((double)value)); return; }
            if (type == typeof(bool)) { writer.Write((byte)((bool)value ? 1 : 0)); return; }
            if (type == typeof(string)) { WriteText(writer, (string)value, 125); return; }
            foreach (var field in Fields(type)) WriteValue(writer, field.FieldType, field.GetValue(value));
        }

        internal static object ReadValue(BinaryReader reader, Type type)
        {
            if (type.IsEnum) return Enum.ToObject(type, ReadValue(reader, Enum.GetUnderlyingType(type)));
            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(sbyte)) return reader.ReadSByte();
            if (type == typeof(short)) return reader.ReadInt16();
            if (type == typeof(ushort)) return reader.ReadUInt16();
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(uint)) return reader.ReadUInt32();
            if (type == typeof(long)) return reader.ReadInt64();
            if (type == typeof(ulong)) return reader.ReadUInt64();
            if (type == typeof(float)) return BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadInt32()), 0);
            if (type == typeof(double)) return BitConverter.Int64BitsToDouble(reader.ReadInt64());
            if (type == typeof(bool)) { byte value = reader.ReadByte(); Need(value <= 1, "Boolean byte out of range"); return value != 0; }
            if (type == typeof(string)) return ReadText(reader, 125);
            object result = Activator.CreateInstance(type);
            foreach (var field in Fields(type)) field.SetValue(result, ReadValue(reader, field.FieldType));
            return result;
        }

        static bool EqualValue(Type type, object left, object right)
        {
            if (type == typeof(float)) return BitConverter.ToInt32(BitConverter.GetBytes((float)left), 0) == BitConverter.ToInt32(BitConverter.GetBytes((float)right), 0);
            if (type == typeof(double)) return BitConverter.DoubleToInt64Bits((double)left) == BitConverter.DoubleToInt64Bits((double)right);
            if (type.IsPrimitive || type.IsEnum || type == typeof(string)) return object.Equals(left, right);
            return Fields(type).All(f => EqualValue(f.FieldType, f.GetValue(left), f.GetValue(right)));
        }

        internal static bool AllFactsEqual(List<Tuple<string, bool, Dictionary<Type, object>>> left, List<Tuple<string, bool, Dictionary<Type, object>>> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].Item1 != right[i].Item1 || left[i].Item2 != right[i].Item2 ||
                    !left[i].Item3.Keys.SequenceEqual(right[i].Item3.Keys)) return false;
                foreach (var cell in left[i].Item3)
                    if (!EqualValue(cell.Key, cell.Value, right[i].Item3[cell.Key])) return false;
            }
            return true;
        }

        static void ValidateRows(List<Tuple<string, bool, Dictionary<Type, object>>> rows)
        {
            Need(rows.Count > 0 && rows.Count <= 16384, "Complete bounded authored rows required");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                ExactInitialValueCodec.EntityId(row.Item1);
                Need(identities.Add(row.Item1), "Duplicate authored identity");
                Need(row.Item3.Count >= 4 && row.Item3.Count <= Types.Length, "Closed component count required");
                foreach (var cell in row.Item3)
                    Need(Array.IndexOf(Types, cell.Key) >= 0 && cell.Value != null && cell.Value.GetType() == cell.Key, "Closed component type required");
                foreach (int required in new[] { 0, 1, 2, 3 }) Need(row.Item3.ContainsKey(Types[required]), "Required authored component missing");
                Need(row.Item3.ContainsKey(Types[4]) == row.Item3.ContainsKey(Types[5]), "Original building origin closure required");
            }
        }

        internal static byte[] Encode(List<Tuple<string, bool, Dictionary<Type, object>>> rows, string rawSha)
        {
            ValidateRows(rows);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Utf8, true))
            {
                writer.Write(Magic);
                WriteText(writer, SourceNavigationFactsIdentity.Producer, 64);
                WriteText(writer, Schema, 64);
                WriteText(writer, rawSha, 64);
                writer.Write(rows.Count);
                foreach (var row in rows)
                {
                    WriteText(writer, row.Item1, 64);
                    writer.Write((byte)(row.Item2 ? 1 : 0));
                    writer.Write((byte)row.Item3.Count);
                    foreach (var cell in row.Item3)
                    {
                        writer.Write((byte)Array.IndexOf(Types, cell.Key));
                        WriteValue(writer, cell.Key, cell.Value);
                    }
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        internal static List<Tuple<string, bool, Dictionary<Type, object>>> Decode(byte[] bytes, string rawSha)
        {
            Need(bytes.Length > 0 && bytes.Length <= 8 * 1024 * 1024, "Bounded authored facts file required");
            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream, Utf8, true))
            {
                Need(reader.ReadBytes(Magic.Length).SequenceEqual(Magic), "Authored facts magic mismatch");
                Need(ReadText(reader, 64) == SourceNavigationFactsIdentity.Producer, "Stale authored producer/decoder identity");
                Need(ReadText(reader, 64) == Schema, "Stale authored type/field/enum schema");
                Need(ReadText(reader, 64) == rawSha, "Authored facts source identity mismatch");
                int count = reader.ReadInt32();
                Need(count > 0 && count <= 16384, "Authored facts row bound");
                var rows = new List<Tuple<string, bool, Dictionary<Type, object>>>(count);
                for (int i = 0; i < count; i++)
                {
                    string id = ReadText(reader, 64);
                    byte enabled = reader.ReadByte(), components = reader.ReadByte();
                    Need(enabled <= 1 && components >= 4 && components <= Types.Length, "Authored row shape invalid");
                    var values = new Dictionary<Type, object>();
                    for (int c = 0; c < components; c++)
                    {
                        byte tag = reader.ReadByte();
                        Need(tag < Types.Length && !values.ContainsKey(Types[tag]), "Unknown/duplicate authored component tag");
                        values.Add(Types[tag], ReadValue(reader, Types[tag]));
                    }
                    rows.Add(Tuple.Create(id, enabled != 0, values));
                }
                Need(stream.Position == stream.Length, "Extra/trailing authored facts bytes");
                ValidateRows(rows);
                return rows;
            }
        }

        internal static int Import(EntityManager em, JObject descriptor)
        {
            // Explicit raw fallback remains the original parser, decoder and materializer.
            if (descriptor["facts"] == null)
            {
                byte[] raw = AutonomousWorld.PinnedFile((string)descriptor["path"], (string)descriptor["sha256"], 128 * 1024 * 1024);
                var rows = GeneratedAuthoredNavigationImporter.Decode(Parse(raw), Path.GetDirectoryName((string)descriptor["path"]));
                ObserveInputs(new Dictionary<string, string> { [Path.GetFullPath((string)descriptor["path"])] = (string)descriptor["sha256"] });
                return GeneratedAuthoredNavigationImporter.Materialize(em, rows);
            }
            var facts = descriptor["facts"] as JObject;
            Need(facts != null && facts.Properties().Select(p => p.Name).OrderBy(n => n).SequenceEqual(new[] { "format", "manifest_path", "manifest_sha256", "path", "sha256" }), "Closed facts descriptor required");
            Need((string)facts["format"] == Format, "Unknown authored facts format");
            var manifest = Parse(AutonomousWorld.PinnedFile((string)facts["manifest_path"], (string)facts["manifest_sha256"], 32768));
            Need(manifest.Properties().Select(p => p.Name).OrderBy(n => n).SequenceEqual(new[] { "cache_sha256", "kind", "producer_sha256", "rows", "schema_sha256", "source_sha256" }), "Closed facts manifest required");
            string rawSha = (string)descriptor["sha256"];
            Need((string)manifest["kind"] == Format && (string)manifest["source_sha256"] == rawSha &&
                (string)manifest["producer_sha256"] == SourceNavigationFactsIdentity.Producer &&
                (string)manifest["schema_sha256"] == Schema && (string)manifest["cache_sha256"] == (string)facts["sha256"], "Authored facts receipt mismatch");
            string verifiedRawSha = StreamSha((string)descriptor["path"]);
            Need(verifiedRawSha == rawSha, "Authored raw source drift");
            var decoded = Decode(AutonomousWorld.PinnedFile((string)facts["path"], (string)facts["sha256"], 8 * 1024 * 1024), rawSha);
            Need((int?)manifest["rows"] == decoded.Count, "Authored facts receipt count mismatch");
            ObserveInputs(new Dictionary<string, string> {
                [Path.GetFullPath((string)descriptor["path"])] = verifiedRawSha,
                [Path.GetFullPath((string)facts["path"])] = (string)facts["sha256"],
                [Path.GetFullPath((string)facts["manifest_path"])] = (string)facts["manifest_sha256"]
            });
            // Every file, row, tag and value is decoded and validated before the first entity write.
            return GeneratedAuthoredNavigationImporter.Materialize(em, decoded);
        }
    }
}
