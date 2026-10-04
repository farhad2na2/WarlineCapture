#if UNITY_EDITOR
using System;
using System.Linq;

namespace Game.Editor
{
    public static class SkirmishS004EnvironmentBuilder
    {
        public static void BuildAndValidate()
        {
            V3UiLocalizationCatalogBuilder.ApplyConfiguredUiTables();
            Build();
            var validation=AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly=>assembly.GetType("Game.Tests.Editor.SkirmishSharedMaterialsTests",false))
                .FirstOrDefault(type=>type!=null) ?? throw new InvalidOperationException("S004 validation assembly missing.");
            validation.GetMethod("RunS004DeliveryValidation",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static)
                .Invoke(null,null);
            var results = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ModeResultStarRulesTests", false))
                .FirstOrDefault(type => type != null) ?? throw new InvalidOperationException("Result-card validation assembly missing.");
            results.GetMethod("RunFocusedValidation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Invoke(null, null);
            var roster = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Game.Tests.Editor.SkirmishNativeStartupRosterTests", false))
                .FirstOrDefault(type => type != null) ?? throw new InvalidOperationException("Native roster validation assembly missing.");
            roster.GetMethod("RunFocusedValidation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Invoke(null, null);
        }
        public static void BuildPlayerMap() => Build();
        public static void Build() => SkirmishS004CampaignMapBuilder.Build();
    }
}
#endif
