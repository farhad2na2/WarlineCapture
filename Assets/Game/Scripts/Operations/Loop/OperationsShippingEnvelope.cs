using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Game.Operations.Loop
{
    /// <summary>
    /// Packs the Operations profile directory into the shipping profile field
    /// <c>operationsEnvelope</c>. Campaign progress is not part of this blob.
    /// </summary>
    public static class OperationsShippingEnvelope
    {
        public const string ProfileField = "operationsEnvelope";

        public static string Normalize(string envelope) => envelope ?? string.Empty;

        public static string Pack(string directory)
        {
            if (!OperationsDurableProfile.Exists(directory))
                throw new InvalidOperationException("profile_missing");

            var builder = new StringBuilder();
            builder.Append("schema=1\n");
            AppendFile(builder, directory, "ready.txt");
            AppendFile(builder, directory, "strategic.json");
            AppendFile(builder, directory, "campaign.b64");
            AppendFile(builder, directory, "quick.b64");
            AppendFile(builder, directory, "loop.txt");
            string blobDirectory = Path.Combine(directory, "blobs");
            if (Directory.Exists(blobDirectory))
            {
                string[] blobs = Directory.GetFiles(blobDirectory);
                Array.Sort(blobs, StringComparer.Ordinal);
                for (int index = 0; index < blobs.Length; index++)
                    AppendFile(builder, blobDirectory, Path.GetFileName(blobs[index]), "blobs/" + Path.GetFileName(blobs[index]));
            }

            return builder.ToString();
        }

        public static void Unpack(string envelope, string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Profile directory is required.", nameof(directory));
            envelope = Normalize(envelope);
            if (envelope.Length == 0)
                throw new InvalidOperationException("envelope_empty");

            string[] lines = envelope.Split('\n');
            if (lines.Length == 0 || lines[0] != "schema=1")
                throw new InvalidOperationException("envelope_schema");

            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);

            for (int index = 1; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.Length == 0)
                    continue;
                if (!line.StartsWith("file ", StringComparison.Ordinal))
                    throw new InvalidOperationException("envelope_file");
                string header = line.Substring("file ".Length);
                int split = header.LastIndexOf(' ');
                if (split <= 0)
                    throw new InvalidOperationException("envelope_file");
                string name = header.Substring(0, split);
                if (!int.TryParse(header.Substring(split + 1), out int length) || length < 0)
                    throw new InvalidOperationException("envelope_length");
                if (name.Length == 0 || name.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf('\\') >= 0 || name.StartsWith("/", StringComparison.Ordinal))
                    throw new InvalidOperationException("envelope_name");
                index++;
                if (index >= lines.Length)
                    throw new InvalidOperationException("envelope_body");
                string body = lines[index];
                if (body.Length != length)
                    throw new InvalidOperationException("envelope_length");
                byte[] bytes = length == 0 ? Array.Empty<byte>() : Convert.FromBase64String(body);
                string relative = name.Replace('/', Path.DirectorySeparatorChar);
                string path = Path.Combine(directory, relative);
                string parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent))
                    Directory.CreateDirectory(parent);
                File.WriteAllBytes(path, bytes);
            }
        }

        static void AppendFile(StringBuilder builder, string directory, string name) =>
            AppendFile(builder, directory, name, name);

        static void AppendFile(StringBuilder builder, string directory, string fileName, string storedName)
        {
            string path = Path.Combine(directory, fileName);
            byte[] bytes = File.ReadAllBytes(path);
            string encoded = bytes.Length == 0 ? string.Empty : Convert.ToBase64String(bytes);
            builder.Append("file ").Append(storedName).Append(' ').Append(encoded.Length.ToString()).Append('\n');
            builder.Append(encoded).Append('\n');
        }
    }
}
