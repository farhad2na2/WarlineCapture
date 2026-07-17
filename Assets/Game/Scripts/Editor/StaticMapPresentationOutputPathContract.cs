using System;

namespace Game.Editor
{
    internal static class StaticMapPresentationOutputPathContract
    {
        internal const string GeneratedRoot =
            "Assets/Game/GeneratedStaticMapPresentation";
        internal const string OperationMapsRoot =
            GeneratedRoot + "/OperationMaps";

        internal static bool TryResolveOutputRoot(
            string operationMapId,
            out string outputRoot,
            out string error)
        {
            outputRoot = null;
            if (string.IsNullOrWhiteSpace(operationMapId) || operationMapId.Length > 64)
                return Fail("Operation-map id must contain 1 to 64 characters.", out error);

            string[] segments = operationMapId.Split('.');
            if (segments.Length != 3 || !string.Equals(segments[0], "opmap", StringComparison.Ordinal))
                return Fail("Operation-map id must use opmap.<mode-or-chapter>.<slug>.", out error);

            for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                string segment = segments[segmentIndex];
                if (segment.Length == 0)
                    return Fail("Operation-map id segments cannot be empty.", out error);

                for (int characterIndex = 0; characterIndex < segment.Length; characterIndex++)
                {
                    char character = segment[characterIndex];
                    bool valid = character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-';
                    if (!valid)
                        return Fail("Operation-map id contains an invalid path character.", out error);
                }
            }

            outputRoot = $"{OperationMapsRoot}/{segments[0]}/{segments[1]}/{segments[2]}";
            error = null;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
