using System;
using System.Globalization;
using System.Text;

namespace Game.Runtime
{
    [Serializable]
    public sealed class TransportBoardingScenarioMetrics
    {
        public string ScenarioId;
        public string VariantId;
        public string TransportSourceKey;
        public string[] PassengerSourceKeys = Array.Empty<string>();
        public bool BoardCommandAccepted;
        public bool BoardingStarted;
        public bool BoardingCompleted;
        public float BoardTimeSeconds = -1f;
        public bool PassengerHiddenAfterBoard;
        public int TransportPassengerCount;
        public bool ExitCommandAccepted;
        public bool ExitStarted;
        public bool ExitCompleted;
        public float ExitTimeSeconds = -1f;
        public bool PassengerVisibleAfterExit;
        public bool HasPassengerFinalCell;
        public int PassengerFinalCellX;
        public int PassengerFinalCellY;
        public bool DropVisualEntityCreated;
        public bool DropVisualCleaned;
        public int ReasonCode;
        public BattleScenarioFailureReason FailureReason;
        public string VisualProofPath;
    }

    [Serializable]
    public sealed class TransportBoardingScenarioReport
    {
        public string GeneratedAtUtc;
        public TransportBoardingScenarioMetrics[] Metrics = Array.Empty<TransportBoardingScenarioMetrics>();
        public bool Passed;
    }

    public static class TransportBoardingScenarioReportJson
    {
        public static string ToJson(TransportBoardingScenarioReport report, bool prettyPrint = true)
        {
            if (report == null)
                return "{}";

            var builder = new StringBuilder(4096);
            JsonWriter writer = new(builder, prettyPrint);
            writer.BeginObject();
            writer.WriteString("GeneratedAtUtc", report.GeneratedAtUtc);
            writer.WritePropertyName("Metrics");
            writer.BeginArray();
            TransportBoardingScenarioMetrics[] metrics = report.Metrics ?? Array.Empty<TransportBoardingScenarioMetrics>();
            for (int i = 0; i < metrics.Length; i++)
            {
                if (i > 0)
                    writer.WriteArraySeparator();
                WriteMetrics(writer, metrics[i]);
            }
            writer.EndArray();
            writer.WriteBool("Passed", report.Passed);
            writer.EndObject();
            return builder.ToString();
        }

        public static string ToJson(TransportBoardingScenarioMetrics metrics, bool prettyPrint = true)
        {
            if (metrics == null)
                return "{}";

            var builder = new StringBuilder(2048);
            JsonWriter writer = new(builder, prettyPrint);
            WriteMetrics(writer, metrics);
            return builder.ToString();
        }

        private static void WriteMetrics(JsonWriter writer, TransportBoardingScenarioMetrics metrics)
        {
            metrics ??= new TransportBoardingScenarioMetrics();
            writer.BeginObject();
            writer.WriteString("ScenarioId", metrics.ScenarioId);
            writer.WriteString("VariantId", metrics.VariantId);
            writer.WriteString("TransportSourceKey", metrics.TransportSourceKey);
            writer.WritePropertyName("PassengerSourceKeys");
            writer.BeginArray();
            string[] passengerSourceKeys = metrics.PassengerSourceKeys ?? Array.Empty<string>();
            for (int i = 0; i < passengerSourceKeys.Length; i++)
            {
                if (i > 0)
                    writer.WriteArraySeparator();
                writer.WriteStringValue(passengerSourceKeys[i]);
            }
            writer.EndArray();
            writer.WriteBool("BoardCommandAccepted", metrics.BoardCommandAccepted);
            writer.WriteBool("BoardingStarted", metrics.BoardingStarted);
            writer.WriteBool("BoardingCompleted", metrics.BoardingCompleted);
            writer.WriteNumber("BoardTimeSeconds", metrics.BoardTimeSeconds);
            writer.WriteBool("PassengerHiddenAfterBoard", metrics.PassengerHiddenAfterBoard);
            writer.WriteNumber("TransportPassengerCount", metrics.TransportPassengerCount);
            writer.WriteBool("ExitCommandAccepted", metrics.ExitCommandAccepted);
            writer.WriteBool("ExitStarted", metrics.ExitStarted);
            writer.WriteBool("ExitCompleted", metrics.ExitCompleted);
            writer.WriteNumber("ExitTimeSeconds", metrics.ExitTimeSeconds);
            writer.WriteBool("PassengerVisibleAfterExit", metrics.PassengerVisibleAfterExit);
            writer.WriteBool("HasPassengerFinalCell", metrics.HasPassengerFinalCell);
            writer.WriteNumber("PassengerFinalCellX", metrics.PassengerFinalCellX);
            writer.WriteNumber("PassengerFinalCellY", metrics.PassengerFinalCellY);
            writer.WriteBool("DropVisualEntityCreated", metrics.DropVisualEntityCreated);
            writer.WriteBool("DropVisualCleaned", metrics.DropVisualCleaned);
            writer.WriteNumber("ReasonCode", metrics.ReasonCode);
            writer.WriteString("FailureReason", metrics.FailureReason.ToString());
            writer.WriteString("VisualProofPath", metrics.VisualProofPath);
            writer.EndObject();
        }

        private readonly struct JsonWriter
        {
            private readonly StringBuilder _builder;
            private readonly bool _prettyPrint;
            private readonly string _indent;
            private readonly int _depth;

            public JsonWriter(StringBuilder builder, bool prettyPrint, string indent = "    ", int depth = 0)
            {
                _builder = builder;
                _prettyPrint = prettyPrint;
                _indent = indent;
                _depth = depth;
            }

            public void BeginObject()
            {
                _builder.Append('{');
                NewLine();
            }

            public void EndObject()
            {
                NewLineBeforeClose();
                _builder.Append('}');
            }

            public void BeginArray()
            {
                _builder.Append('[');
                NewLine();
            }

            public void EndArray()
            {
                NewLineBeforeClose();
                _builder.Append(']');
            }

            public void WriteArraySeparator()
            {
                _builder.Append(',');
                NewLine();
            }

            public void WritePropertyName(string name)
            {
                WriteCommaIfNeeded();
                WriteIndent();
                WriteEscaped(name);
                _builder.Append(_prettyPrint ? ": " : ":");
            }

            public void WriteString(string name, string value)
            {
                WritePropertyName(name);
                WriteEscaped(value ?? string.Empty);
            }

            public void WriteStringValue(string value)
            {
                WriteIndent();
                WriteEscaped(value ?? string.Empty);
            }

            public void WriteNumber(string name, int value)
            {
                WritePropertyName(name);
                _builder.Append(value.ToString(CultureInfo.InvariantCulture));
            }

            public void WriteNumber(string name, float value)
            {
                WritePropertyName(name);
                _builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
            }

            public void WriteBool(string name, bool value)
            {
                WritePropertyName(name);
                _builder.Append(value ? "true" : "false");
            }

            private void WriteCommaIfNeeded()
            {
                if (_builder.Length == 0)
                    return;

                char last = _builder[_builder.Length - 1];
                if (last != '{' && last != '[' && last != '\n' && last != ',')
                {
                    _builder.Append(',');
                    NewLine();
                }
            }

            private void NewLine()
            {
                if (_prettyPrint)
                    _builder.Append('\n');
            }

            private void NewLineBeforeClose()
            {
                if (!_prettyPrint)
                    return;

                if (_builder.Length > 0 && _builder[_builder.Length - 1] == '\n')
                {
                    for (int i = 0; i < _depth; i++)
                        _builder.Append(_indent);
                }
                else
                {
                    _builder.Append('\n');
                    for (int i = 0; i < _depth; i++)
                        _builder.Append(_indent);
                }
            }

            private void WriteIndent()
            {
                if (!_prettyPrint)
                    return;

                for (int i = 0; i < _depth + 1; i++)
                    _builder.Append(_indent);
            }

            private void WriteEscaped(string value)
            {
                _builder.Append('"');
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    switch (c)
                    {
                        case '\\':
                            _builder.Append("\\\\");
                            break;
                        case '"':
                            _builder.Append("\\\"");
                            break;
                        case '\n':
                            _builder.Append("\\n");
                            break;
                        case '\r':
                            _builder.Append("\\r");
                            break;
                        case '\t':
                            _builder.Append("\\t");
                            break;
                        default:
                            _builder.Append(c);
                            break;
                    }
                }
                _builder.Append('"');
            }
        }
    }
}
