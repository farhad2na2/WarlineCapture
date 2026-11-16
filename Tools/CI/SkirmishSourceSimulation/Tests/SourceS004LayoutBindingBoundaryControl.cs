using System;
using Game.Configs;
using Game.Skirmish.Contracts;

// Calls the real binding predicate from each isolated compiler variant.
internal static class SourceS004LayoutBindingBoundaryControl
{
    static void Main()
    {
        string[] missions = { "S002", "S003", "S004", "S005", "S026", "invalid" };
        foreach (string mission in missions)
        foreach (SkirmishSizeId size in new[] { SkirmishSizeId.None, SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
        foreach (SkirmishDifficultyId difficulty in new[] { SkirmishDifficultyId.None, SkirmishDifficultyId.Recruit,
            SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander, (SkirmishDifficultyId)255 })
        foreach (bool validMap in new[] { false, true })
        {
            var setup = new SkirmishResolvedSetup { CatalogId = mission, SizeId = size, DifficultyId = difficulty,
                OperationMapId = validMap ? SkirmishMapLayoutBuilder.DesertBaseMapId : "invalid",
                LayoutId = SkirmishMapLayoutBuilder.DesertBaseLayoutId };
            bool result = SkirmishMapLayoutBuilder.ShouldBindRegularStandard(setup);
            Console.WriteLine($"{mission}|{size}|{difficulty}|{validMap}|{result}");
        }
        if (SkirmishMapLayoutBuilder.ShouldBindRegularStandard(null)) throw new Exception("Null setup accepted");
    }
}
