using System;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;

// Compares actual compiler outputs only. The caller must separately validate
// the source graph, captured Regular inputs and diagnostic-only admission.
internal static class SourceS003DifficultySetupCompatibility
{
    internal static void Require(SkirmishResolvedSetup regular, SkirmishResolvedSetup selected)
    {
        if (regular == null || selected == null || regular.CatalogId != "S003" ||
            selected.CatalogId != "S003" || regular.DefinitionId != "skirmish.s003" ||
            selected.DefinitionId != regular.DefinitionId || regular.Seed != selected.Seed ||
            regular.SizeId != selected.SizeId || regular.DifficultyId != SkirmishDifficultyId.Regular ||
            (selected.DifficultyId != SkirmishDifficultyId.Recruit &&
             selected.DifficultyId != SkirmishDifficultyId.Veteran &&
             selected.DifficultyId != SkirmishDifficultyId.Commander) ||
            regular.SetupHash == selected.SetupHash ||
            regular.SetupHash != SkirmishSetupCompiler.ComputeHash(regular, regular.Seed) ||
            selected.SetupHash != SkirmishSetupCompiler.ComputeHash(selected, selected.Seed))
            throw new InvalidOperationException("Distinct actual S003 compiler identities required");

        var original = JObject.FromObject(regular);
        var candidate = JObject.FromObject(selected);
        foreach (string identity in new[] { "DifficultyId", "SetupHash", "Report" })
        {
            if (!original.Remove(identity) || !candidate.Remove(identity))
                throw new InvalidOperationException("Complete serialized setup identity required");
        }
        if (!JToken.DeepEquals(original, candidate))
            throw new InvalidOperationException("S003 compiler setup differs beyond difficulty identity");
    }
}
