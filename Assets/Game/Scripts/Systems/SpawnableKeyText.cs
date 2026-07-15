namespace Game.Runtime
{
    internal static class SpawnableKeyText
    {
        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            int last = value.Length - 1;
            bool hasOuterWhitespace = char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[last]);
            if (!hasOuterWhitespace)
            {
                bool alreadyNormalized = true;
                for (int i = 0; i < value.Length; i++)
                {
                    if (char.ToLowerInvariant(value[i]) == value[i])
                        continue;

                    alreadyNormalized = false;
                    break;
                }

                if (alreadyNormalized)
                    return value;
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}
