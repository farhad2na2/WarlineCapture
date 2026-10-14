using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Difficulty")]
    public sealed class SkirmishDifficultyConfig : ScriptableObject
    {
        [SerializeField] private string difficultyId = "Regular";
        [SerializeField] private SkirmishDifficultyId kind = SkirmishDifficultyId.Regular;
        [SerializeField] private float responseMinSeconds = 3f;
        [SerializeField] private float responseMaxSeconds = 5f;
        [SerializeField] private float scoutMinSeconds = 25f;
        [SerializeField] private float scoutMaxSeconds = 40f;
        [SerializeField] private int establishedOpeningRestraintSeconds = 40;
        [SerializeField] private int contentVersion = 1;

        public string DifficultyId => difficultyId;
        public SkirmishDifficultyId Kind => kind;
        public float ResponseMinSeconds => responseMinSeconds;
        public float ResponseMaxSeconds => responseMaxSeconds;
        public float ScoutMinSeconds => scoutMinSeconds;
        public float ScoutMaxSeconds => scoutMaxSeconds;
        public int EstablishedOpeningRestraintSeconds => establishedOpeningRestraintSeconds;
        public int ContentVersion => contentVersion;

        public static bool TryGetPolicyValues(SkirmishDifficultyId difficulty, out float responseMin,
            out float responseMax, out float scoutMin, out float scoutMax, out int fieldOpening, out int establishedOpening)
        {
            responseMin = responseMax = scoutMin = scoutMax = 0; fieldOpening = establishedOpening = 0;
            switch (difficulty)
            {
                case SkirmishDifficultyId.Recruit: responseMin = 5; responseMax = 8; scoutMin = 45; scoutMax = 60; fieldOpening = 90; establishedOpening = 60; return true;
                case SkirmishDifficultyId.Regular: responseMin = 3; responseMax = 5; scoutMin = 25; scoutMax = 40; fieldOpening = 60; establishedOpening = 40; return true;
                case SkirmishDifficultyId.Veteran: responseMin = 1.5f; responseMax = 3; scoutMin = 15; scoutMax = 25; fieldOpening = 45; establishedOpening = 25; return true;
                case SkirmishDifficultyId.Commander: responseMin = .8f; responseMax = 2; scoutMin = 10; scoutMax = 20; fieldOpening = 30; establishedOpening = 15; return true;
                default: return false;
            }
        }

        public void Configure(SkirmishDifficultyId configured)
        {
            kind = configured;
            switch (configured)
            {
                case SkirmishDifficultyId.Recruit:
                    difficultyId = "Recruit";
                    responseMinSeconds = 5f;
                    responseMaxSeconds = 8f;
                    scoutMinSeconds = 45f;
                    scoutMaxSeconds = 60f;
                    establishedOpeningRestraintSeconds = 60;
                    break;
                case SkirmishDifficultyId.Veteran:
                    difficultyId = "Veteran";
                    responseMinSeconds = 1.5f;
                    responseMaxSeconds = 3f;
                    scoutMinSeconds = 15f;
                    scoutMaxSeconds = 25f;
                    establishedOpeningRestraintSeconds = 25;
                    break;
                case SkirmishDifficultyId.Commander:
                    difficultyId = "Commander";
                    responseMinSeconds = 0.8f;
                    responseMaxSeconds = 2f;
                    scoutMinSeconds = 10f;
                    scoutMaxSeconds = 20f;
                    establishedOpeningRestraintSeconds = 15;
                    break;
                default:
                    difficultyId = "Regular";
                    kind = SkirmishDifficultyId.Regular;
                    responseMinSeconds = 3f;
                    responseMaxSeconds = 5f;
                    scoutMinSeconds = 25f;
                    scoutMaxSeconds = 40f;
                    establishedOpeningRestraintSeconds = 40;
                    break;
            }

            contentVersion = 1;
        }
    }
}
