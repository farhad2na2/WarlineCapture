using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Configs
{
    public enum NarrativeSpeakerId
    {
        Radio = 0,
        Dalia = 1,
        Samira = 2,
        Aria = 3,
        Commander = 4
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

    [Serializable]
    public sealed class NarrativeDialogueLineRecord
    {
        [SerializeField] private string lineId;
        [SerializeField] private string textKey;
        [SerializeField, TextArea] private string englishFallback;
        [SerializeField] private NarrativeSpeakerId speaker;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField, Min(0f)] private float startSeconds;
        [SerializeField, Min(0f)] private float deadlineSeconds;
        [SerializeField] private bool essentialCaption;

        public string LineId => lineId;
        public string TextKey => textKey;
        public string EnglishFallback => englishFallback;
        public NarrativeSpeakerId Speaker => speaker;
        public AudioClip VoiceClip => voiceClip;
        public float StartSeconds => startSeconds;
        public float DeadlineSeconds => deadlineSeconds;
        public bool EssentialCaption => essentialCaption;
    }

    [Serializable]
    public sealed class NarrativeStateRecord
    {
        [SerializeField] private string stateId;
        [SerializeField] private Game.UI.Contracts.NarrativeStateKind kind;
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

        public string StateId => stateId;
        public Game.UI.Contracts.NarrativeStateKind Kind => kind;
        public Sprite Panel16x9 => panel16x9;
        public Sprite Panel20x9 => panel20x9;
        public AssetReferenceSprite Panel16x9Reference => panel16x9Reference;
        public AssetReferenceSprite Panel20x9Reference => panel20x9Reference;
        public IReadOnlyList<NarrativeDialogueLineRecord> Lines => lines;
        public string ContinueStateId => continueStateId;
        public string SkipStateId => skipStateId;
        public bool ReducedMotionSupported => reducedMotionSupported;
        public float DurationSeconds => durationSeconds;
        public NarrativeMotionPreset MotionPreset => motionPreset;
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
