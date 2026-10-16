namespace Game.UI.Runtime
{
    public static class SkirmishLibraryPreviewAssets
    {
        public const int Width = 512;
        public const int Height = 288;
        public const string Root = "Assets/Game/Art/SkirmishLibrary";

        public static string PreviewPath(string scenarioId) =>
            Root + "/Previews/skirmish.preview." + scenarioId + ".png";

        public static string PlatePath(string mapId) =>
            Root + "/Plates/skirmish.plate." + mapId + ".png";

        public static string ResolvePreviewKey(string scenarioId, bool playable, string mapId)
        {
            if (playable && !string.IsNullOrEmpty(scenarioId))
            {
                string path = PreviewPath(scenarioId);
                if (HasAsset(path))
                    return path;
                return "skirmish.preview." + scenarioId;
            }

            return ResolvePlateKey(mapId);
        }

        public static string ResolvePlateKey(string mapId)
        {
            string id = mapId ?? string.Empty;
            string path = PlatePath(id);
            return HasAsset(path) ? path : "skirmish.plate." + id;
        }

        public static bool HasAsset(string assetPath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assetPath))
                return false;
            string root = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
            return !string.IsNullOrEmpty(root) && System.IO.File.Exists(System.IO.Path.Combine(root, assetPath));
#else
            return false;
#endif
        }
    }
}
