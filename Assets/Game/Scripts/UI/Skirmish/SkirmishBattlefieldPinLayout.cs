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

        private static readonly Vector2[] Arc =
        {
            new Vector2(0.22f, 0.30f),
            new Vector2(0.40f, 0.22f),
            new Vector2(0.60f, 0.28f),
            new Vector2(0.30f, 0.58f),
            new Vector2(0.50f, 0.66f),
            new Vector2(0.72f, 0.50f)
        };

        private static readonly Vector2[] MapNudge =
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(0.04f, 0.03f),
            new Vector2(-0.03f, 0.04f),
            new Vector2(0.03f, -0.03f),
            new Vector2(-0.02f, 0.02f)
        };

        private static readonly Vector2[] ObjectiveNudge =
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(0.05f, 0.04f),
            new Vector2(-0.04f, 0.05f),
            new Vector2(0.03f, -0.04f)
        };

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

            Vector2 point = Arc[armyIndex * 2 + startIndex] + MapNudge[map] + ObjectiveNudge[objective];
            point.x = Mathf.Clamp(point.x, 0.18f, 0.82f);
            point.y = Mathf.Clamp(point.y, 0.18f, 0.76f);
            centreFromTopLeft = point;
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
