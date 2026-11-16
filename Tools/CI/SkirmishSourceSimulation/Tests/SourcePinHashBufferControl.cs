using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

internal static class SourcePinHashBufferControl
{
    private static string Hex(byte[] value) => BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
    private static void Main(string[] args)
    {
        if (args.Length != 2) throw new InvalidDataException("Existing regular source file and expected SHA-256 required");
        var file = new FileInfo(args[0]);
        if (!file.Exists || file.Length < 1 || file.Length > 512L * 1024 * 1024 ||
            (file.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Bounded regular source file required");
        foreach (int bufferSize in new[] { 4096, 1024 * 1024 })
        {
            var watch = Stopwatch.StartNew(); string hash;
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
            using (var sha = SHA256.Create()) hash = Hex(sha.ComputeHash(stream));
            watch.Stop();
            if (hash != args[1]) throw new InvalidDataException("Source hash differs");
            Console.WriteLine("[SourcePinHashBufferControl] buffer=" + bufferSize + " bytes=" + file.Length +
                " elapsed_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " sha256=" + hash + " scope=OneFile_ExactHash_NotMissionReadiness");
        }
        Console.WriteLine("[SourcePinHashBufferControl] result=Passed");
    }
}
