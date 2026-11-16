using System;
using System.IO;
using System.Security.Cryptography;
using Game.Runtime;

internal static class SourceS004GuardCacheOwnerControl
{
    static string Hash(byte[] bytes) { using var h=SHA256.Create(); return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static void Main(string[] args)
    {
        if(args.Length!=4 || Directory.Exists(args[3]))throw new InvalidDataException("Exact inputs and fresh output directory required");
        Directory.CreateDirectory(args[3]);
        string negatives=Path.Combine(args[3],"negative-fixtures");
        SourceS004UnboundSetupAdmissionControl.Main(new[]{args[0],args[1],args[2],negatives});
        var baseline=SourceS004CompiledSetupInput.Read(args[0],args[1]);
        string cacheDir=Path.Combine(args[3],"cache");Directory.CreateDirectory(cacheDir);
        SourceS004SetupGuardCacheControls.Run(baseline,args[0],args[1],cacheDir);
        string invalid=Path.Combine(negatives,"same-identity-unbound.json");
        string invalidHash=Hash(File.ReadAllBytes(invalid));
        bool rejected=false;
        try { SourceS004CompiledSetupInput.RequireExact(baseline,invalid,invalidHash); }
        catch(InvalidDataException) { rejected=true; }
        if(!rejected)throw new InvalidOperationException("Cached setup guard accepted unbound reference");
        Console.WriteLine("[SourceS004GuardCacheOwnerControl] result=Passed baselineCacheParity=Passed sameHashUnbound=Rejected world=0 readiness=0");
    }
}
