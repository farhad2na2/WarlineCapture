using System;
using System.IO;
using Game.Runtime;

// Disposable filesystem fixture for the source-only Resources name check.
internal static class SourceS004ViewProfileAbsenceControl
{
    static void Main(string[] args)
    {
        if (args.Length != 1 || !Path.IsPathRooted(args[0]) || Directory.Exists(args[0]))
            throw new ArgumentException("Fresh absolute fixture directory required");
        string root = args[0];
        foreach (string domain in new[] { "Assets", "Packages", "Library/PackageCache" })
            Directory.CreateDirectory(Path.Combine(root, domain));
        string resources = Path.Combine(root, "Assets", "Fixture", "Resources");
        Directory.CreateDirectory(resources);
        string target = Path.Combine(resources, "SkirmishMissionViewProfile_S004.asset");
        File.WriteAllText(target + ".meta", "fixture metadata");
        SourceS004OuterBoundaries.RequireNoSourceProfile(root);
        File.WriteAllText(target, "fixture asset");
        bool rejected = false;
        try { SourceS004OuterBoundaries.RequireNoSourceProfile(root); }
        catch (NotSupportedException) { rejected = true; }
        if (!rejected) throw new InvalidDataException("An actual source Resources profile must reject the absent-profile path");
        File.Delete(target);
        SourceS004OuterBoundaries.RequireNoSourceProfile(root);
        Console.WriteLine("[SourceS004ViewProfileAbsence] result=Passed absent=1 presentRejected=1 rescan=1 fixtureOnly=1 nativeParity=0 readiness=0");
    }
}
