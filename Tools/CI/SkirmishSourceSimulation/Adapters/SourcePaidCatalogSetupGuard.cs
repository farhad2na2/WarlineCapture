using System;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    // Shared source-backed validation for the narrowly scoped S003 paid catalog.
    // This does not grant S003 normal catalog or mission admission.
    internal static class SourcePaidCatalogSetupGuard
    {
#if WARLINE_S003_SHARED_SELECTION
        private static bool AdmittedIdentity(SkirmishResolvedSetup setup)
        {
            if (setup.SetupHash == 2394754492u) return true;
            return SourceS003DiagnosticConfiguration.Current?.IsSelected(setup) == true;
        }
        internal static bool IsSelectedSourceDiagnostic(SkirmishResolvedSetup setup)
        {
            if (SourceS003DiagnosticConfiguration.Current?.IsSelected(setup) != true) return false;
            try { RequireS003(setup); return true; } catch (NotSupportedException) { return false; }
        }
#else
#endif
        internal static void RequireS003(SkirmishResolvedSetup setup)
        {
            if (setup?.CatalogId != "S003" || setup.DefinitionId != "skirmish.s003" ||
#if WARLINE_S003_SHARED_SELECTION
                setup.Seed != 104732 || !AdmittedIdentity(setup) ||
#else
                setup.Seed != 104732 || setup.SetupHash != 2394754492u ||
#endif
                SkirmishSetupCompiler.ComputeHash(setup, setup.Seed) != setup.SetupHash ||
                setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                setup.ArmyProfileId != SkirmishArmyProfileId.AirMobile || setup.EnemyPolicy != null ||
                setup.LayoutBindingFingerprint != "" || setup.MissionLogicalBindingFingerprint != "")
                throw new NotSupportedException("Exact original S003 AirMobile paid catalog required");
        }
    }
}
