using System;
using System.IO;
using Game.Runtime;

internal static class S004TransformBoundaryControl
{
    static void Reject(Action action, string label)
    {
        try { action(); }
        catch (Exception error) when (error is InvalidOperationException || error is NotSupportedException || error is InvalidDataException) { return; }
        throw new Exception("Unexpected acceptance: " + label);
    }

    static int Main(string[] args)
    {
        try
        {
            string readset = Path.GetFullPath(args[0]);
            string setupPath = Path.GetFullPath(args[1]);
            string setupHash = args[2];
            string altered = Path.GetFullPath(args[3]);
            var source = SourceTransformConsumedReadset.VerifyAtBoundary(readset);
            source.RequireCurrent();
            byte[] bytes = File.ReadAllBytes(readset);
            if (bytes.Length == 0) throw new InvalidDataException("Empty source readset");
            bytes[bytes.Length - 1] ^= 1;
            File.WriteAllBytes(altered, bytes);
            Reject(() => SourceTransformConsumedReadset.VerifyAtBoundary(altered), "changed readset");
            var setup = SourceS004CompiledSetupInput.Read(setupPath, setupHash);
            SourceS004CompiledSetupInput.RequireExact(setup, setupPath, setupHash);
            setup.Seed++;
            Reject(() => SourceS004CompiledSetupInput.RequireExact(setup, setupPath, setupHash), "changed setup");
            Reject(() => SourceS004CompiledSetupInput.Read(setupPath, new string('0', 64)), "wrong setup hash");
            Console.WriteLine("[S004TransformBoundaryControl] result=Passed readset=1 setup=1 negatives=3 world=0 native=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
