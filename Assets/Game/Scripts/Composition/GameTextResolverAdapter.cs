using Game.Configs;
using Game.UI.Contracts;

namespace Game.Composition
{
    internal sealed class GameTextResolverAdapter : IGameTextResolver
    {
        public string Get(string key, string fallback = "")
        {
            return GameText.Get(key, fallback);
        }

        public bool TryGet(string key, out string value)
        {
            return GameText.TryGet(key, out value);
        }

        public string Format(string key, string fallback, params object[] args)
        {
            return GameText.Format(key, fallback, args);
        }
    }
}
