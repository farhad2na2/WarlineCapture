namespace Game.UI.Runtime
{
    internal static class DiagnosticsFpsText
    {
        private const int MaxCachedFps = 999;
        private const string AboveCachedRange = "999+";
        private static readonly string[] Values = BuildValues();

        public static string Get(int fps)
        {
            if (fps <= 0)
                return Values[0];
            return fps <= MaxCachedFps ? Values[fps] : AboveCachedRange;
        }

        private static string[] BuildValues()
        {
            var values = new string[MaxCachedFps + 1];
            for (int i = 0; i < values.Length; i++)
                values[i] = i.ToString();
            return values;
        }
    }
}
