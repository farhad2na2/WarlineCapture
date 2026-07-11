using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public enum NarrativeSpeakerTreatment
    {
        Radio = 0,
        HumanPortrait = 1,
        AriaIcon = 2,
        Commander = 3
    }

    [Serializable]
    public sealed class NarrativeSpeakerRecord
    {
        [SerializeField] private NarrativeSpeakerId speakerId;
        [SerializeField] private string nameKey;
        [SerializeField] private string nameFallback;
        [SerializeField] private string roleKey;
        [SerializeField] private string roleFallback;
        [SerializeField] private string accessibleLabelKey;
        [SerializeField] private string accessibleLabelFallback;
        [SerializeField] private NarrativeSpeakerTreatment treatment;
        [SerializeField] private Sprite identitySprite;
        [SerializeField] private Color accentColor = Color.white;

        public NarrativeSpeakerId SpeakerId => speakerId;
        public string NameKey => nameKey;
        public string NameFallback => nameFallback;
        public string RoleKey => roleKey;
        public string RoleFallback => roleFallback;
        public string AccessibleLabelKey => accessibleLabelKey;
        public string AccessibleLabelFallback => accessibleLabelFallback;
        public NarrativeSpeakerTreatment Treatment => treatment;
        public Sprite IdentitySprite => identitySprite;
        public Color AccentColor => accentColor;
    }

    [CreateAssetMenu(menuName = "Game/Narrative/Speaker Catalog", fileName = "NarrativeSpeakerCatalog")]
    public sealed class NarrativeSpeakerCatalog : ScriptableObject
    {
        [SerializeField] private List<NarrativeSpeakerRecord> speakers = new();

        public IReadOnlyList<NarrativeSpeakerRecord> Speakers => speakers;
    }
}
