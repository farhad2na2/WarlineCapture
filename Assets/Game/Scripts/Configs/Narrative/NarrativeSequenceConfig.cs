using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Narrative.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Configs
{
    public enum NarrativeMusicCue
    {
        Briefing = 0,
        Conflict = 1
    }

    public enum NarrativeAmbienceCue
    {
        CityConflict = 0,
        CityDay = 1,
        Battlefield = 2
    }

    public enum NarrativeVehicleCue
    {
        None = 0,
        Engine = 1
    }

    public enum NarrativeEventCue
    {
        None = 0,
        Attack = 1,
        SmallArms = 2,
        Radio = 3,
        Blackout = 4,
        AriaBoot = 5,
        Transition = 6
    }

    [Serializable]
    public sealed class NarrativeDialogueLineRecord
    {
        [SerializeField] private string lineId;
        [SerializeField] private string textKey;
        [SerializeField, TextArea] private string englishFallback;
        [SerializeField] private NarrativeSpeakerId speaker;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private AudioClip femaleVoiceClip;
        [SerializeField] private AudioClip neutralVoiceClip;
        [SerializeField, Min(0f)] private float startSeconds;
        [SerializeField, Min(0f)] private float deadlineSeconds;
        [SerializeField] private bool essentialCaption;

        public string LineId => lineId;
        public string TextKey => textKey;
        public string EnglishFallback => englishFallback;
        public NarrativeSpeakerId Speaker => speaker;
        public AudioClip VoiceClip => voiceClip;
        public AudioClip FemaleVoiceClip => femaleVoiceClip;
        public AudioClip NeutralVoiceClip => neutralVoiceClip;
        public float StartSeconds => startSeconds;
        public float DeadlineSeconds => deadlineSeconds;
        public bool EssentialCaption => essentialCaption;
    }

    [Serializable]
    public sealed class NarrativeStateRecord
    {
        [SerializeField] private string stateId;
        [SerializeField] private NarrativeStateKind kind;
        [SerializeField] private Sprite panel16x9;
        [SerializeField] private Sprite panel20x9;
        [SerializeField] private AssetReferenceSprite panel16x9Reference;
        [SerializeField] private AssetReferenceSprite panel20x9Reference;
        [SerializeField] private List<NarrativeDialogueLineRecord> lines = new();
        [SerializeField] private string continueStateId;
        [SerializeField] private string skipStateId;
        [SerializeField] private bool reducedMotionSupported = true;
        [SerializeField, Min(0f)] private float durationSeconds;
        [SerializeField] private NarrativeMotionPreset motionPreset;
        [SerializeField] private string locationTitleKey;
        [SerializeField] private string locationTitleFallback;
        [SerializeField] private string locationSubtitleKey;
        [SerializeField] private string locationSubtitleFallback;
        [SerializeField] private NarrativeMusicCue musicCue;
        [SerializeField] private NarrativeAmbienceCue ambienceCue;
        [SerializeField] private NarrativeVehicleCue vehicleCue;
        [SerializeField] private NarrativeEventCue eventCue;
        [SerializeField] private NarrativeRouteRole routeRole;
        [SerializeField] private string completionPayloadId;
        [SerializeField] private string[] evidenceIds = Array.Empty<string>();
        [SerializeField] private string[] missionContextFlags = Array.Empty<string>();

        public string StateId => stateId;
        public NarrativeStateKind Kind => kind;
        public Sprite Panel16x9 => panel16x9;
        public Sprite Panel20x9 => panel20x9;
        public AssetReferenceSprite Panel16x9Reference => panel16x9Reference;
        public AssetReferenceSprite Panel20x9Reference => panel20x9Reference;
        public bool HasPanelBinding => panel16x9 != null || panel20x9 != null ||
            panel16x9Reference != null && !string.IsNullOrEmpty(panel16x9Reference.AssetGUID) ||
            panel20x9Reference != null && !string.IsNullOrEmpty(panel20x9Reference.AssetGUID);
        public IReadOnlyList<NarrativeDialogueLineRecord> Lines => lines;
        public string ContinueStateId => continueStateId;
        public string SkipStateId => skipStateId;
        public bool ReducedMotionSupported => reducedMotionSupported;
        public float DurationSeconds => durationSeconds;
        public NarrativeMotionPreset MotionPreset => motionPreset;
        public string LocationTitleKey => locationTitleKey;
        public string LocationTitleFallback => locationTitleFallback;
        public string LocationSubtitleKey => locationSubtitleKey;
        public string LocationSubtitleFallback => locationSubtitleFallback;
        public NarrativeMusicCue MusicCue => musicCue;
        public NarrativeAmbienceCue AmbienceCue => ambienceCue;
        public NarrativeVehicleCue VehicleCue => vehicleCue;
        public NarrativeEventCue EventCue => eventCue;
        public NarrativeRouteRole RouteRole => routeRole;
        public string CompletionPayloadId => completionPayloadId;
        public string[] EvidenceIds => evidenceIds;
        public string[] MissionContextFlags => missionContextFlags;
    }

    [CreateAssetMenu(menuName = "Game/Narrative/Sequence Config", fileName = "NarrativeSequenceConfig")]
    public sealed class NarrativeSequenceConfig : ScriptableObject
    {
        [SerializeField] private string sequenceId;
        [SerializeField] private string entryStateId;
        [SerializeField] private string defaultSkipDestinationId;
        [SerializeField] private List<NarrativeStateRecord> states = new();

        public string SequenceId => sequenceId;
        public string EntryStateId => entryStateId;
        public string DefaultSkipDestinationId => defaultSkipDestinationId;
        public IReadOnlyList<NarrativeStateRecord> States => states;
    }
}
