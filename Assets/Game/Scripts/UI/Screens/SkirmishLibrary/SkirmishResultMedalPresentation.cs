using System;

namespace Game.UI.Runtime
{
    public readonly struct SkirmishResultMedalLine
    {
        public SkirmishResultMedalLine(bool visible, string firstLocKey, string difficultyLocKey)
        {
            Visible = visible;
            FirstLocKey = firstLocKey ?? string.Empty;
            DifficultyLocKey = difficultyLocKey ?? string.Empty;
        }

        public bool Visible { get; }
        public string FirstLocKey { get; }
        public string DifficultyLocKey { get; }
    }

    public static class SkirmishResultMedalPresentation
    {
        public static SkirmishResultMedalLine Describe(bool isFirstAward, string scenarioId, byte difficulty)
        {
            if (!isFirstAward || string.IsNullOrWhiteSpace(scenarioId) || difficulty < 1 || difficulty > 4)
                return default;

            string difficultyKey = difficulty switch
            {
                1 => "ui.skirmish.medal.recruit",
                2 => "ui.skirmish.medal.regular",
                3 => "ui.skirmish.medal.veteran",
                _ => "ui.skirmish.medal.commander"
            };
            return new SkirmishResultMedalLine(true, "ui.skirmish.medal.first", difficultyKey);
        }
    }
}
