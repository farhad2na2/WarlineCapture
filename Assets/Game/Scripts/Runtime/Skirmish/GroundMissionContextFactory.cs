using System;
using Game.Components;
using Game.Configs;
using Game.Simulation;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    // Compiled mission data only. This creates no World, native observation or publication authority.
    public static class GroundMissionContextFactory
    {
        public static bool TryCreateLogicalPublicData(
            SkirmishScenarioDefinitionConfig definition, SkirmishResolvedSetup setup,
            SkirmishContentManifest manifest, System.Collections.Generic.IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            SkirmishBattleCatalogConfig catalog, in SkirmishExpandedSessionComponent session,
            int playableScenarioIndex, int sourcePresetLoadIndex,
            out GroundMissionContext context, out string error)
        {
            context = default;
            error = "Actual compiled definition, catalog and session are required.";
            if (definition == null || setup == null || manifest == null || matrix == null || catalog == null ||
                catalog.Entries == null || session.IsLegacy != 0 || session.SessionId.IsEmpty ||
                playableScenarioIndex < -1 || sourcePresetLoadIndex < 0) return false;
            if (setup.CatalogId != definition.CatalogId || setup.DefinitionId != definition.DefinitionId ||
                setup.ContentVersion != definition.ContentVersion || setup.OperationMapId != definition.OperationMapId ||
                setup.LayoutId != definition.MapLayoutId || !SessionMatchesSetup(setup, session)) return false;
            if (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                setup.ArmyProfileId != SkirmishArmyProfileId.GroundManeuver ||
                !GroundMissionContext.SupportsOriginalGroundStart(setup.StartPackageId,setup.Readiness))
            { error = "Ground/Field-or-Established/BaseAssault compiled family required."; return false; }
            int matches = 0, indexMatches = 0;
            SkirmishBattleCatalogEntry selected = default;
            foreach (var row in catalog.Entries)
            {
                if (row.ScenarioId == setup.CatalogId) { selected = row; matches++; }
                if (row.IsPlayable && row.PlayableScenarioIndex == playableScenarioIndex) indexMatches++;
            }
            if (matches != 1 || selected.ObjectiveId != "BA" || selected.ArmyProfile != "G" || selected.StartProfile != SkirmishIdParsing.ToCode(setup.StartPackageId) ||
                selected.PlayableScenarioIndex != playableScenarioIndex ||
                playableScenarioIndex >= 0 && (!selected.IsPlayable || indexMatches != 1) ||
                !string.IsNullOrEmpty(selected.DefinitionId) && selected.DefinitionId != setup.DefinitionId ||
                selected.ContentVersion > 0 && selected.ContentVersion != setup.ContentVersion)
            { error = "Unique matching actual canonical catalog row required."; return false; }
            if (SkirmishMissionDispatch.TryGetLegacyIndex(setup.CatalogId, out int original) &&
                (playableScenarioIndex != original || sourcePresetLoadIndex != original))
            { error = "Original coded mission dispatch and source preset selection required."; return false; }
            if(!TryResolveApproachKind(selected.MapId,definition,setup,out var approach,out error))return false;
            // Re-run the original compiler: a caller-supplied hash alone is not compilation evidence.
            if (!SkirmishSetupCompiler.TryCompile(definition, setup.DifficultyId, setup.SizeId, setup.Seed,
                manifest, matrix, out var compiled, out var reasons))
            { error = string.Join(";", reasons); return false; }
            if (compiled.SetupHash != setup.SetupHash || setup.SetupHash != SkirmishSetupCompiler.ComputeHash(setup, setup.Seed))
            { error = "Actual setup differs from original compiled definition."; return false; }
            int presetIndex = SkirmishPresetConfig.SourcePresetIndex(sourcePresetLoadIndex);
            string presetResource = SkirmishPresetConfig.SourcePresetResourceName(sourcePresetLoadIndex);
            context = new GroundMissionContext(setup.CatalogId, setup.DefinitionId, setup.ContentVersion,
                setup.SetupHash, session.SessionId.ToString(), playableScenarioIndex, setup.ObjectiveKind,
                setup.ArmyProfileId, setup.StartPackageId, setup.Readiness, approach,
                "canonical:" + selected.MapId + ";source-preset:" + presetResource + (approach==AriaGroundApproachKind.AuthoredLogicalGround?";source:"+setup.OperationMapId+";logical-binding:"+setup.MissionLogicalBindingFingerprint:string.Empty),
                selected.MapId, sourcePresetLoadIndex, presetIndex, presetResource, AriaS002Scope.LogicalPublicData);
            error = null;
            return true;
        }

        public static bool TryResolveApproachKind(string canonicalMapId,SkirmishScenarioDefinitionConfig definition,SkirmishResolvedSetup setup,out AriaGroundApproachKind approach,out string error)
        {
            error=null;approach=default;
            switch(canonicalMapId){case "DB":approach=AriaGroundApproachKind.Ordinary;return true;case "CC":approach=AriaGroundApproachKind.CityCrossroads;return true;case "IB":approach=AriaGroundApproachKind.IndustrialBasin;return true;}
            if(string.IsNullOrEmpty(canonicalMapId)||definition==null||setup==null||definition.CampaignMap==null||definition.MissionLogicalBinding==null||setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault||setup.ArmyProfileId!=SkirmishArmyProfileId.GroundManeuver||!GroundMissionContext.SupportsOriginalGroundStart(setup.StartPackageId,setup.Readiness)||string.IsNullOrEmpty(setup.LayoutBindingFingerprint)||string.IsNullOrEmpty(setup.MissionLogicalBindingFingerprint)||definition.CatalogId!=setup.CatalogId||definition.DefinitionId!=setup.DefinitionId||definition.ContentVersion!=setup.ContentVersion||definition.OperationMapId!=setup.OperationMapId){error="Canonical map approach requires actual source-bound logical ground configuration.";return false;}
            approach=AriaGroundApproachKind.AuthoredLogicalGround;return true;
        }

        public static bool MatchesLogicalSession(in GroundMissionContext context, SkirmishResolvedSetup setup,
            in SkirmishExpandedSessionComponent session, int playableScenarioIndex) =>
            context.ObservationScope == AriaS002Scope.LogicalPublicData && setup != null &&
            context.SessionId == session.SessionId.ToString() && context.CatalogId == setup.CatalogId &&
            context.DefinitionId == setup.DefinitionId && context.ContentVersion == setup.ContentVersion &&
            context.SetupHash == setup.SetupHash && context.PlayableScenarioIndex == playableScenarioIndex &&
            context.SourcePresetResourceName == SkirmishPresetConfig.SourcePresetResourceName(context.SourcePresetLoadIndex) &&
            context.SourcePresetIndex == SkirmishPresetConfig.SourcePresetIndex(context.SourcePresetLoadIndex) &&
            SessionMatchesSetup(setup, session);

        private static bool SessionMatchesSetup(SkirmishResolvedSetup setup, in SkirmishExpandedSessionComponent session) =>
            session.IsLegacy == 0 && session.CatalogId.ToString() == setup.CatalogId &&
            session.DefinitionId.ToString() == setup.DefinitionId && session.ContentVersion == setup.ContentVersion &&
            session.SetupHash == setup.SetupHash && session.Seed == setup.Seed && session.SizeId == setup.SizeId &&
            session.DifficultyId == setup.DifficultyId && session.ObjectiveKind == setup.ObjectiveKind;
    }
}
