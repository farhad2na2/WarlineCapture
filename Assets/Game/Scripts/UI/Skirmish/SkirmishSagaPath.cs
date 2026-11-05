using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>Authored mission stops over the existing Campaign plate. Coordinates use a top-left origin.
    /// The path is a suggested tour; access is always projected from the catalog and ownership.</summary>
    public static class SkirmishSagaPath
    {
        public const int StopCount = 24;
        public const float MinimumWidth = 920f;
        public const float MinimumHeight = 760f;
        public const float TouchDiameter = 80f;
        private static readonly Dictionary<string, Vector2[]> Paths = new()
        {
            ["DB"] = new[]
            {
                P(.20f, .18f),
                P(.32f, .14f),
                P(.43f, .16f),
                P(.58f, .14f),
                P(.68f, .16f),
                P(.80f, .22f),
                P(.80f, .40f),
                P(.68f, .36f),
                P(.57f, .38f),
                P(.42f, .36f),
                P(.32f, .38f),
                P(.20f, .44f),
                P(.20f, .62f),
                P(.32f, .58f),
                P(.43f, .60f),
                P(.58f, .58f),
                P(.68f, .60f),
                P(.80f, .66f),
                P(.80f, .84f),
                P(.68f, .80f),
                P(.57f, .82f),
                P(.42f, .80f),
                P(.32f, .82f),
                P(.20f, .88f)
            },
            ["CC"] = new[]
            {
                P(.20f, .16f),
                P(.32f, .18f),
                P(.43f, .14f),
                P(.58f, .18f),
                P(.68f, .16f),
                P(.80f, .22f),
                P(.80f, .40f),
                P(.68f, .38f),
                P(.57f, .34f),
                P(.42f, .38f),
                P(.32f, .36f),
                P(.20f, .44f),
                P(.20f, .62f),
                P(.32f, .60f),
                P(.43f, .56f),
                P(.58f, .60f),
                P(.68f, .58f),
                P(.80f, .66f),
                P(.80f, .84f),
                P(.68f, .82f),
                P(.57f, .78f),
                P(.42f, .82f),
                P(.32f, .80f),
                P(.20f, .88f)
            },
            ["MP"] = new[]
            {
                P(.20f, .18f),
                P(.32f, .14f),
                P(.43f, .18f),
                P(.58f, .14f),
                P(.68f, .18f),
                P(.80f, .22f),
                P(.80f, .40f),
                P(.68f, .36f),
                P(.57f, .40f),
                P(.42f, .36f),
                P(.32f, .40f),
                P(.20f, .44f),
                P(.20f, .62f),
                P(.32f, .58f),
                P(.43f, .62f),
                P(.58f, .58f),
                P(.68f, .62f),
                P(.80f, .66f),
                P(.80f, .84f),
                P(.68f, .80f),
                P(.57f, .84f),
                P(.42f, .80f),
                P(.32f, .84f),
                P(.20f, .88f)
            },
            ["IB"] = new[]
            {
                P(.20f, .16f),
                P(.32f, .14f),
                P(.43f, .16f),
                P(.58f, .18f),
                P(.68f, .16f),
                P(.80f, .22f),
                P(.80f, .40f),
                P(.68f, .38f),
                P(.57f, .36f),
                P(.42f, .34f),
                P(.32f, .36f),
                P(.20f, .44f),
                P(.20f, .62f),
                P(.32f, .60f),
                P(.43f, .58f),
                P(.58f, .56f),
                P(.68f, .58f),
                P(.80f, .66f),
                P(.80f, .84f),
                P(.68f, .82f),
                P(.57f, .80f),
                P(.42f, .78f),
                P(.32f, .80f),
                P(.20f, .88f)
            },
            ["AP"] = new[]
            {
                P(.20f, .16f),
                P(.32f, .18f),
                P(.43f, .16f),
                P(.58f, .14f),
                P(.68f, .16f),
                P(.80f, .22f),
                P(.80f, .40f),
                P(.68f, .38f),
                P(.57f, .36f),
                P(.42f, .38f),
                P(.32f, .40f),
                P(.20f, .44f),
                P(.20f, .62f),
                P(.32f, .60f),
                P(.43f, .58f),
                P(.58f, .60f),
                P(.68f, .62f),
                P(.80f, .66f),
                P(.80f, .84f),
                P(.68f, .82f),
                P(.57f, .80f),
                P(.42f, .82f),
                P(.32f, .84f),
                P(.20f, .88f)
            }
        };
        public static IReadOnlyList<Vector2> ForMap(string mapId) => Paths.TryGetValue(mapId ?? "", out var points) ? points : Paths["DB"];
        private static Vector2 P(float x, float y) => new(x, y);
    }
}
