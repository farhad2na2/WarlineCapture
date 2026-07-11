namespace Game.Catalog.Contracts
{
    public enum NarrativeSpeakerId
    {
        Radio = 0,
        Dalia = 1,
        Samira = 2,
        Aria = 3,
        Commander = 4
    }

    public enum NarrativeSpeakerTreatment
    {
        Radio = 0,
        HumanPortrait = 1,
        AriaIcon = 2,
        Commander = 3
    }

    public enum NarrativeMotionPreset
    {
        Static = 0,
        PushIn = 1,
        PullBack = 2,
        DriftLeft = 3,
        DriftRight = 4,
        StaticImpact = 5,
        StaticInteractive = 6
    }
}
