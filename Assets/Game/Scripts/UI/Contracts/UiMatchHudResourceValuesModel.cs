namespace Game.UI.Contracts
{
    public readonly struct UiMatchHudResourceValuesModel
    {
        public readonly int Oil;
        public readonly int Fuel;
        public readonly bool ShowOil;
        public readonly int Credits;
        public readonly bool IsValid;
        public readonly bool RequiresTextFallback;

        private UiMatchHudResourceValuesModel(
            int oil,
            int fuel,
            bool showOil,
            int credits,
            bool isValid,
            bool requiresTextFallback)
        {
            Oil = oil < 0 ? 0 : oil;
            Fuel = fuel < 0 ? 0 : fuel;
            ShowOil = showOil;
            Credits = credits < 0 ? 0 : credits;
            IsValid = isValid;
            RequiresTextFallback = requiresTextFallback;
        }

        public static UiMatchHudResourceValuesModel FromValues(
            int oil,
            int fuel,
            bool showOil,
            int credits = 0) =>
            new(oil, fuel, showOil, credits, true, false);

        public static UiMatchHudResourceValuesModel TextFallback(bool showOil, int credits = 0) =>
            new(0, 0, showOil, credits, true, true);

        public static UiMatchHudResourceValuesModel Invalid => default;
    }
}
