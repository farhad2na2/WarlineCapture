namespace Game.UI.Contracts
{
    public readonly struct UiMatchHudResourceValuesModel
    {
        public readonly int Oil;
        public readonly int Fuel;
        public readonly bool ShowOil;
        public readonly bool IsValid;
        public readonly bool RequiresTextFallback;

        private UiMatchHudResourceValuesModel(
            int oil,
            int fuel,
            bool showOil,
            bool isValid,
            bool requiresTextFallback)
        {
            Oil = oil < 0 ? 0 : oil;
            Fuel = fuel < 0 ? 0 : fuel;
            ShowOil = showOil;
            IsValid = isValid;
            RequiresTextFallback = requiresTextFallback;
        }

        public static UiMatchHudResourceValuesModel FromValues(int oil, int fuel, bool showOil) =>
            new(oil, fuel, showOil, true, false);

        public static UiMatchHudResourceValuesModel TextFallback(bool showOil) =>
            new(0, 0, showOil, true, true);

        public static UiMatchHudResourceValuesModel Invalid => default;
    }
}
