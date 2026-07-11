using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Configs
{
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

        public string StateId => stateId;
        public NarrativeStateKind Kind => kind;
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
