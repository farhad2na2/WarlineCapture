namespace Game.Configs
{
    // Original shared time-limit policy value. SkirmishPresetConfig forwards its
    // public constant to this owner so native and source-host outcome evaluation
    // use the same authored policy.
    public static class SkirmishLegacyMatchPolicy
    {
        public const float MatchDurationSeconds = 900f;
    }
}
