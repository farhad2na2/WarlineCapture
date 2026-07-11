using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Narrative/Punctuation Profile", fileName = "NarrativePunctuationProfile")]
    public sealed class NarrativePunctuationProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] private float charactersPerSecond = 28f;
        [SerializeField, Min(0f)] private float commaPauseSeconds = 0.11f;
        [SerializeField, Min(0f)] private float clausePauseSeconds = 0.16f;
        [SerializeField, Min(0f)] private float sentencePauseSeconds = 0.24f;
        [SerializeField, Min(0f)] private float ellipsisPauseSeconds = 0.32f;
        [SerializeField, Min(0f)] private float tailHoldSeconds = 0.25f;

        public float CharactersPerSecond => charactersPerSecond;
        public float CommaPauseSeconds => commaPauseSeconds;
        public float ClausePauseSeconds => clausePauseSeconds;
        public float SentencePauseSeconds => sentencePauseSeconds;
        public float EllipsisPauseSeconds => ellipsisPauseSeconds;
        public float TailHoldSeconds => tailHoldSeconds;
    }
}
