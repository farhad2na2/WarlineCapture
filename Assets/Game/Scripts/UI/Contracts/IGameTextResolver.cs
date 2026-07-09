namespace Game.UI.Contracts
{
    public interface IGameTextResolver
    {
        string Get(string key, string fallback = "");
        bool TryGet(string key, out string value);
        string Format(string key, string fallback, params object[] args);
    }
}
