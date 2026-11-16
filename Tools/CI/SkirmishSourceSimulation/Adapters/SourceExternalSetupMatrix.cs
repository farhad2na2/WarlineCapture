using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    // Source-only configuration input. Parsing a row does not bind a map,
    // allocate a world, admit a mission, or establish player readiness.
    internal static class SourceExternalSetupMatrix
    {
        internal static List<SkirmishSetupMatrixRow> ReadVerified(byte[] bytes, string expectedSha256)
        {
            if (bytes == null || expectedSha256 == null || expectedSha256.Length != 64 ||
                expectedSha256.Any(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
                throw new InvalidOperationException("Exact matrix bytes and lowercase SHA-256 required");
            string actual;
            using (var sha = SHA256.Create())
                actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if (actual != expectedSha256) throw new InvalidOperationException("Matrix bytes differ from the input pin");

            string text = new UTF8Encoding(false, true).GetString(bytes);
            if (!SkirmishSetupMatrixTable.TryLoadFromText(text, out var rows, out var error))
                throw new InvalidOperationException("Original setup matrix owner rejected input: " + error);
            var sizes = new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar };
            if (rows.Count != 360) throw new InvalidOperationException("All 360 authored mission-size rows required");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row == null || row.CatalogId == null || !sizes.Contains(row.SizeId) ||
                    !keys.Add(row.CatalogId + ":" + row.SizeId))
                    throw new InvalidOperationException("Invalid or duplicate mission-size identity");
            }
            for (int mission = 1; mission <= 120; mission++)
                foreach (var size in sizes)
                    if (!keys.Contains("S" + mission.ToString("D3", System.Globalization.CultureInfo.InvariantCulture) + ":" + size))
                        throw new InvalidOperationException("Complete authored mission-size identity set required");
            return rows;
        }

        internal static SkirmishSetupMatrixRow Select(
            IReadOnlyList<SkirmishSetupMatrixRow> rows, string mission, SkirmishSizeId size)
        {
            if (rows == null || string.IsNullOrEmpty(mission))
                throw new InvalidOperationException("Verified matrix and exact mission identity required");
            SkirmishSetupMatrixRow match = null;
            foreach (var row in rows)
                if (row != null && row.CatalogId == mission && row.SizeId == size)
                {
                    if (match != null) throw new InvalidOperationException("Duplicate selected mission-size identity");
                    match = row;
                }
            return match ?? throw new InvalidOperationException("Authored mission-size row is unavailable");
        }
    }
}
