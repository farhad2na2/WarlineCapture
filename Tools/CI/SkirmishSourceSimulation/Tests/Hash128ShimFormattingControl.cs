using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

internal static class Hash128ShimFormattingControl
{
    private const string PackageHash128Path = "Library/PackageCache/com.unity.entities@ef040875dea9/Unity.Entities/Types/Hash128.cs";
    private const string PackageHash128Sha256 = "fc9989feabee6de0f1acdd54564b1ab523e520934e8c63b52c94f2871b62a2f5";
    private const string Shim059Path = "Tools/CI/SkirmishSourceSimulation/Adapters/059_AutonomousEcs.cs";
    private const string ShimS026Path = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs";

    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(args.Length == 0 ? Environment.CurrentDirectory : args[0]);
        string packageSource = File.ReadAllText(Path.Combine(root, PackageHash128Path));
        string packageSha = Sha(File.ReadAllBytes(Path.Combine(root, PackageHash128Path)));
        Need(packageSha == PackageHash128Sha256, "installed Entities Hash128 source changed");

        string shim059 = ExtractType(File.ReadAllText(Path.Combine(root, Shim059Path)), "Hash128_059");
        string shimS026 = ExtractType(File.ReadAllText(Path.Combine(root, ShimS026Path)), "Hash128_S026");
        string literalTable = ExtractDeclaration(packageSource, "static readonly char[] k_HexToLiteral");
        string toString = ExtractMethod(packageSource, "public override unsafe string ToString()");

        string temporary = Path.Combine(Path.GetTempPath(), "hash128-shim-format-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        File.WriteAllText(Path.Combine(temporary, "OracleControl.csproj"), ProjectFile);
        File.WriteAllText(Path.Combine(temporary, "Program.cs"), Compose(shim059, shimS026, literalTable, toString));

        var start = new ProcessStartInfo("dotnet", "run --project \"" + Path.Combine(temporary, "OracleControl.csproj") + "\" --configuration Release -p:NuGetAudit=false")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var process = Process.Start(start))
        {
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Console.Write(stdout);
            if (!string.IsNullOrEmpty(stderr)) Console.Error.Write(stderr);
            Console.WriteLine("[Hash128ShimFormattingControl] generatedProject=" + temporary);
            Console.WriteLine("[Hash128ShimFormattingControl] packageSourceSha256=" + packageSha);
            return process.ExitCode;
        }
    }

    private static string Compose(string shim059, string shimS026, string literalTable, string toString)
    {
        return @"
using System;
namespace Unity.Mathematics
{
    public struct uint4
    {
        public uint x, y, z, w;
        public uint4(uint x, uint y, uint z, uint w) { this.x=x; this.y=y; this.z=z; this.w=w; }
        public uint this[int i] { get { switch(i) { case 0:return x; case 1:return y; case 2:return z; case 3:return w; default:throw new IndexOutOfRangeException(); } } set { switch(i) { case 0:x=value;break; case 1:y=value;break; case 2:z=value;break; case 3:w=value;break; default:throw new IndexOutOfRangeException(); } } }
    }
}
namespace Shim059 { " + shim059 + @" }
namespace ShimS026 { " + shimS026 + @" }
namespace PinnedEntities
{
    public struct Hash128
    {
        public Unity.Mathematics.uint4 Value;
        public Hash128(uint x,uint y,uint z,uint w) { Value=new Unity.Mathematics.uint4(x,y,z,w); }
        " + literalTable + @"
        " + toString + @"
    }
}
internal static class Program
{
    static int Main()
    {
        uint[][] vectors = new uint[][]
        {
            new uint[] {0,0,0,0},
            new uint[] {1,2,3,4},
            new uint[] {0xdeadbeef,0,0,0xbaadf00d},
            new uint[] {0x80000000,0x00000001,0x7fffffff,0xffffffff},
            new uint[] {0xffffffff,0xffffffff,0xffffffff,0xffffffff},
            new uint[] {0x01234567,0x89abcdef,0xfedcba98,0x76543210},
            new uint[] {0x0000000f,0x000000f0,0x00000f00,0x0000f000},
            new uint[] {0x01020304,0x11121314,0xa1a2a3a4,0xf1f2f3f4},
            new uint[] {0x13579bdf,0x2468ace0,0x0badc0de,0xc001d00d},
            new uint[] {0xaaaaaaaa,0x55555555,0x33333333,0xcccccccc}
        };
        for(int i=0;i<vectors.Length;i++)
        {
            uint[] v=vectors[i];
            string expected=new PinnedEntities.Hash128(v[0],v[1],v[2],v[3]).ToString();
            string actual059=new Shim059.Hash128_059(v[0],v[1],v[2],v[3]).ToString();
            string actualS026=new ShimS026.Hash128_S026(v[0],v[1],v[2],v[3]).ToString();
            if(i==1 && expected!=""10000000200000003000000040000000"") throw new Exception(""pinned package 1,2,3,4 vector changed: ""+expected);
            if(i==2 && expected!=""feebdaed0000000000000000d00fdaab"") throw new Exception(""pinned package deadbeef vector changed: ""+expected);
            if(actual059!=expected || actualS026!=expected)
            {
                Console.Error.WriteLine(""[Hash128ShimFormattingControl] result=Failed vector=""+i+"" expected=""+expected+"" shim059=""+actual059+"" shimS026=""+actualS026);
                return 1;
            }
        }
        Console.WriteLine(""[Hash128ShimFormattingControl] result=Passed compared=10 shims=2 packageMethod=ExactPinnedSource"");
        return 0;
    }
}
";
    }

    private static string ExtractType(string source, string replacementName)
    {
        const string startMarker = "public struct Hash128{";
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Need(start >= 0, "shim Hash128 declaration missing");
        int open = source.IndexOf('{', start);
        int end = FindClosingBrace(source, open);
        string declaration = source.Substring(start, end - start + 1);
        return declaration.Replace("Hash128", replacementName);
    }

    private static string ExtractDeclaration(string source, string marker)
    {
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        Need(start >= 0, "pinned package hex table missing");
        int end = source.IndexOf(';', start);
        Need(end >= 0, "pinned package hex table terminator missing");
        return source.Substring(start, end - start + 1);
    }

    private static string ExtractMethod(string source, string marker)
    {
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        Need(start >= 0, "pinned package ToString method missing");
        int open = source.IndexOf('{', start);
        int end = FindClosingBrace(source, open);
        return source.Substring(start, end - start + 1);
    }

    private static int FindClosingBrace(string source, int open)
    {
        Need(open >= 0, "opening brace missing");
        int depth = 0;
        for (int i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0) return i;
        }
        throw new InvalidOperationException("closing brace missing");
    }

    private static string Sha(byte[] bytes)
    {
        using (var hash = SHA256.Create())
            return string.Concat(hash.ComputeHash(bytes).Select(x => x.ToString("x2")));
    }

    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private const string ProjectFile = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><AllowUnsafeBlocks>true</AllowUnsafeBlocks><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup>
</Project>
""";
}
