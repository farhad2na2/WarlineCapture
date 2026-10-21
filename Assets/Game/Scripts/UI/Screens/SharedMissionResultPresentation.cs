using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum ResultScreenMode { Campaign, Skirmish, Operations }
    public enum ResultObjectiveState { Incomplete, Complete, Failed, Information }
    public enum ResultActionTone { Neutral, Navigation, Positive }
    [Serializable] public struct SharedResultObjective
    {
        public string Label, Value;
        public ResultObjectiveState State;
        public SharedResultObjective(string label, string value, ResultObjectiveState state)
        { Label = label; Value = value; State = state; }
    }
    public sealed class SharedMissionResultPresentation
    {
        public ResultScreenMode Mode;
        public bool Victory, UseRetry, ActionsEnabled;
        public string MissionId, ModeLabel, MissionTitle, Context, OutcomeTitle, Reason, Elapsed, Summary;
        public SharedResultObjective[] Objectives = Array.Empty<SharedResultObjective>();
        public string[] PerformanceLabels = Array.Empty<string>(), PerformanceValues = Array.Empty<string>();
        public string InfoHeading, InfoBody, InfoCaption, SaveStatus, PrimaryLabel, SecondaryLabel, TertiaryLabel;
        public ResultActionTone PrimaryTone, SecondaryTone, TertiaryTone;
        public int Stars, MedalTier;
        public bool ShowMedal, MedalEarned, ShowRewardIcons;
        public string Signature;
        public Texture Artwork;
    }
}
