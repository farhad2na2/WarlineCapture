using System;
namespace Game.UI.Runtime
{
    public enum MainMenuMode { Operations, Skirmish }
    public struct MainMenuModeProgress
    {
        public bool Available, HasNext, Resume, Replay;
        public string MissionId, TitleKey, Title, PreviewKey;
        public int Completed, Total, Published, Recruit, Regular, Veteran, Commander;
        public int MedalCount => Recruit + Regular + Veteran + Commander;
    }
    // Composition owns save/access reads; the native UI never changes a profile to recommend a mission.
    public static class MainMenuModeProgressGateway
    {
        public static Func<MainMenuMode, MainMenuModeProgress> Read;
        public static MainMenuModeProgress ReadMode(MainMenuMode mode) => Read != null ? Read(mode) : default;
    }
}
