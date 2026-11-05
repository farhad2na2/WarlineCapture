using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Authored pin centres for the six army/start cards on each objective.
    /// Coordinates are normalised from the top-left of the battlefield plate.
    /// They are not painted into the plate.
    /// </summary>
    public static class SkirmishBattlefieldPinLayout
    {
        private static readonly string[] Maps = { "DB", "CC", "MP", "IB", "AP" };
        private static readonly string[] Objectives = { "BA", "FC", "BT", "CE" };
        private static readonly string[] Armies = { "G", "A", "C" };
        private static readonly string[] Starts = { "F", "E" };

        /// <summary>Stable 3×2. Nudges used to stack the six cards on top of each other.</summary>
        private static readonly Vector2[] Arc =
        {
            new Vector2(0.20f, 0.30f),
            new Vector2(0.50f, 0.30f),
            new Vector2(0.80f, 0.30f),
            new Vector2(0.20f, 0.70f),
            new Vector2(0.50f, 0.70f),
            new Vector2(0.80f, 0.70f)
        };

        public static bool TryGet(string pinId, out Vector2 centreFromTopLeft)
        {
            centreFromTopLeft = new Vector2(0.5f, 0.5f);
            if (string.IsNullOrEmpty(pinId))
                return false;
            int first = pinId.IndexOf('.');
            int second = first >= 0 ? pinId.IndexOf('.', first + 1) : -1;
            if (first <= 0 || second <= first + 1)
                return false;
            string map = pinId.Substring(0, first);
            string objective = pinId.Substring(first + 1, second - first - 1);
            if (!int.TryParse(pinId.Substring(second + 1), out int slot) || slot < 0 || slot >= Arc.Length)
                return false;
            int mapIndex = Index(Maps, map);
            int objectiveIndex = Index(Objectives, objective);
            if (mapIndex < 0 || objectiveIndex < 0)
                return false;
            centreFromTopLeft = Arc[slot];
            return true;
        }

        public static bool TryGet(string mapId, string objectiveId, string army, string start, out Vector2 centreFromTopLeft)
        {
            int map = Index(Maps, mapId);
            int objective = Index(Objectives, objectiveId);
            int armyIndex = Index(Armies, army);
            int startIndex = Index(Starts, start);
            if (map < 0 || objective < 0 || armyIndex < 0 || startIndex < 0)
            {
                centreFromTopLeft = new Vector2(0.5f, 0.5f);
                return false;
            }

            centreFromTopLeft = Arc[armyIndex * 2 + startIndex];
            return true;
        }

        private static int Index(string[] values, string value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], value, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }
    }
}
