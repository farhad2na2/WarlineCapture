using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

// Load before any math-using method is JIT compiled. DLL stays in place.
static class StartupGroundingRuntime
{
    internal static void LoadInstalledMath(string path,string expectedSha256)
    {
        path=Path.GetFullPath(path);
        using var stream=File.OpenRead(path);
        if(Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()!=expectedSha256)
            throw new ArgumentException("Installed mathematics bytes differ");
        if(AssemblyName.GetAssemblyName(path).Name!="UnityEngine.MathematicsModule")
            throw new ArgumentException("Exact mathematics implementation required");
        AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }
}
