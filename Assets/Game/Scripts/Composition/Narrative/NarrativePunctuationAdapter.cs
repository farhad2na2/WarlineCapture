using Game.Configs;
using Game.UI.Runtime;

namespace Game.Composition
{
    internal static class NarrativePunctuationAdapter
    {
        public static NarrativePunctuationPresentationModel From(NarrativePunctuationProfile profile)
        {
            return profile == null
                ? default
                : new NarrativePunctuationPresentationModel
                {
                    CharactersPerSecond = profile.CharactersPerSecond,
                    CommaPauseSeconds = profile.CommaPauseSeconds,
                    ClausePauseSeconds = profile.ClausePauseSeconds,
                    SentencePauseSeconds = profile.SentencePauseSeconds,
                    EllipsisPauseSeconds = profile.EllipsisPauseSeconds,
                    TailHoldSeconds = profile.TailHoldSeconds
                };
        }
    }
}
