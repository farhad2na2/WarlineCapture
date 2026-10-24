using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

// Optional body-stage host only. Uses the exact installed managed math module in
// place; never launches an Editor, copies runtime binaries or chooses a fallback.
internal static class RuntimeMathBootstrap
{
    internal static string ProjectRoot { get; private set; }
    internal static void Initialize(string profilePath)
    {
        string path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(profilePath)),"math-module.json");
        var file=new FileInfo(path);
        if(!file.Exists||file.Length<=0||file.Length>16384||(file.Attributes&(FileAttributes.ReparsePoint|FileAttributes.Directory|FileAttributes.Device))!=0)
            throw new ArgumentException("Pinned regular math-module config required");
        byte[] raw;
        using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
        {
            WorldJson.Need(stream.Length==file.Length&&stream.Length<=16384,"Math-module config length changed");
            raw=new byte[checked((int)stream.Length)];stream.ReadExactly(raw);WorldJson.Need(stream.ReadByte()==-1,"Math-module config grew");
        }
        using var doc=JsonDocument.Parse(raw);var root=doc.RootElement;
        WorldJson.UniqueKeys(root);WorldJson.Shape(root,new[]{"schema_version","project_root","module_path","module_sha256"});
        WorldJson.Need(root.GetProperty("schema_version").GetInt32()==1,"Unsupported math-module schema");
        ProjectRoot=root.GetProperty("project_root").GetString();
        string module=root.GetProperty("module_path").GetString(),digest=root.GetProperty("module_sha256").GetString();
        WorldJson.Need(!string.IsNullOrWhiteSpace(ProjectRoot)&&Path.IsPathRooted(ProjectRoot)&&!string.IsNullOrWhiteSpace(module)&&Path.IsPathRooted(module),"Absolute source/math paths required");
        var info=new FileInfo(module);
        WorldJson.Need(info.Exists&&info.Length>0&&info.Length<=67108864&&(info.Attributes&(FileAttributes.ReparsePoint|FileAttributes.Directory|FileAttributes.Device))==0,"Pinned installed math module required");
        using(var stream=File.OpenRead(module))
            WorldJson.Need(Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()==digest,"Installed math module changed");
        var assembly=Assembly.LoadFrom(module);
        WorldJson.Need(assembly.GetType("Unity.Mathematics.math",false)!=null,"Unsupported installed math module");
    }
}
