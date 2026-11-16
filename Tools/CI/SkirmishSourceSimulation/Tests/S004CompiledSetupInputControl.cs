using System;
using System.IO;
using System.Security.Cryptography;
using Game.Runtime;
using Newtonsoft.Json.Linq;

internal static class S004CompiledSetupInputControl
{
    static string Hash(byte[] bytes) { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static int Main(string[] args)
    {
        try
        {
            string path = Path.GetFullPath(args[0]); byte[] bytes = File.ReadAllBytes(path);
            var setup = SourceS004CompiledSetupInput.Read(path, Hash(bytes));
            if (setup.PlayerAir != 1 || setup.EnemyAir != 1 || setup.MaterialsEach != 900 || setup.OilEach != 240 || setup.UsableFuelEach != 700)
                throw new InvalidOperationException("Compiled source budget or aircraft changed");
            SourceS004CompiledSetupInput.RequireExact(setup, path, Hash(bytes));
            setup.OilEach++;
            if (Game.Configs.SkirmishSetupCompiler.ComputeHash(setup, setup.Seed) != setup.SetupHash)
                throw new InvalidOperationException("Fixture must change a field outside the original setup hash");
            bool changedFieldRejected = false;
            try { SourceS004CompiledSetupInput.RequireExact(setup, path, Hash(bytes)); } catch (InvalidDataException) { changedFieldRejected = true; }
            if (!changedFieldRejected) throw new InvalidOperationException("Changed live source budget accepted");
            bool rejected = false;
            try { SourceS004CompiledSetupInput.Read(path, new string('0', 64)); } catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Wrong source hash accepted");
            Console.WriteLine("[S004CompiledSetupInputControl] result=Passed forces=18 structures=20 hash=Passed wrongHash=Rejected nonHashField=Rejected world=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
