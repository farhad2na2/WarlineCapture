using System;
using System.IO;
using Game.Configs;
using Unity.Entities;

namespace Game.Runtime
{
    // Finite source-host boundaries. Unsupported native view, logical binding,
    // and checkpoint behavior fail closed; no native input is represented.
    internal static class SourceS004OuterBoundaries
    {
        [ThreadStatic] static string checkedRoot;
        internal static void RequireNoSourceProfile(string root)
        {
            string[] domains = { "Assets", "Packages", "Library/PackageCache" };
            foreach (string relative in domains)
            {
                string path = Path.Combine(root, relative);
                if (!Directory.Exists(path)) throw new InvalidDataException("Source Resources domain missing: " + relative);
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    if (String.Equals(Path.GetFileName(Path.GetDirectoryName(file)), "Resources", StringComparison.OrdinalIgnoreCase) &&
                        String.Equals(Path.GetFileNameWithoutExtension(file), "SkirmishMissionViewProfile_S004", StringComparison.OrdinalIgnoreCase) &&
                        !String.Equals(Path.GetExtension(file), ".meta", StringComparison.OrdinalIgnoreCase))
                        throw new NotSupportedException("S004 source view profile exists and needs the original profile owner");
                }
            }
        }
        internal static SkirmishMissionViewProfile LoadChecked(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            if (setup?.CatalogId != "S004") throw new NotSupportedException("S004 source view boundary required");
            string root = SourceS004OpeningContext.RequireSourceOpeningView(em, session, setup);
            RequireNoSourceProfile(root); checkedRoot = root;
            return null; // Exact source file-name absence, not a native AssetDatabase claim.
        }
        internal static bool RejectProfileResolution(EntityManager em, out SkirmishMissionViewProfile profile)
        { profile = null; throw new NotSupportedException("Nonempty S004 source view profile requires the original view resolver"); }
        internal static bool RejectValidatedView(EntityManager em, Entity session, SkirmishMissionViewProfile profile, out string error)
        { error = null; throw new NotSupportedException("Nonempty S004 source view profile requires the original validated view owner"); }
        // The source host has no presentation dispatcher. This is an explicit
        // logical-only boundary, not evidence that a native view was accepted.
        internal static bool DeferNativePresentation(EntityManager em, Entity session, out string error)
        {
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            SourceS004OpeningContext.RequireSourceOpeningView(em, session, setup);
            error = null;
            return true;
        }
        internal static bool RejectLogicalBinding(EntityManager em, Entity session, out string error)
        { error = null; throw new NotSupportedException("Typed source logical binding after spawn is not admitted by the S004 diagnostic"); }
        internal static void VerifySourceProfileAbsence()
        {
            if (checkedRoot == null) throw new InvalidOperationException("S004 source view lookup was not observed");
            string root = checkedRoot; checkedRoot = null; RequireNoSourceProfile(root);
        }
    }
}
