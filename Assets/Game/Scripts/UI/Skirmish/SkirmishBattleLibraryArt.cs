using System;
using System.Text;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Resource keys and authored theatre regions. Battlefield plates and scenario
    /// previews are Programmer 1 sprites addressed by PlatePreviewKey and PreviewSpriteKey.
    /// No player text is baked into the art.
    /// </summary>
    public static class SkirmishBattleLibraryArt
    {
        public const string ResourceRoot = "SkirmishBattleLibrary/";
        public static readonly string[] MapIds = { "DB", "CC", "MP", "IB", "AP" };
        public static readonly string[] ObjectiveIds = { "BA", "FC", "BT", "CE" };

        /// <summary>
        /// Hit zones on the painted 16:9 theatre base, top-left normalised.
        /// Order matches MapIds: Desert Base upper-left, City Crossroads upper-right,
        /// Mountain Pass mid-left, Industrial Basin mid-right, Airfield Plains lower-centre.
        /// Runtime draws the outlines; the painting has none.
        /// </summary>
        public static readonly Rect[] TheaterRegions =
        {
            new Rect(0.00f, 0.00f, 0.50f, 0.34f),
            new Rect(0.50f, 0.00f, 0.50f, 0.34f),
            new Rect(0.00f, 0.34f, 0.48f, 0.32f),
            new Rect(0.48f, 0.34f, 0.52f, 0.32f),
            new Rect(0.00f, 0.66f, 1.00f, 0.34f)
        };

        /// <summary>Pin centres on the painted theatre base, top-left normalised. The pin stays on the landmark.</summary>
        public static readonly Vector2[] TheaterLabelCentres =
        {
            new Vector2(0.25f, 0.15f),
            new Vector2(0.75f, 0.15f),
            new Vector2(0.24f, 0.48f),
            new Vector2(0.74f, 0.48f),
            new Vector2(0.52f, 0.78f)
        };

        /// <summary>
        /// Where each zone card prefers to sit, top-left normalised on the painting.
        /// Industrial Basin prefers the open ground beside the refinery. Mountain Pass stays on the left.
        /// Airfield Plains prefers lower-centre sand near the runway. The runtime seat rejects any spot that covers a hex.
        /// </summary>
        public static readonly Vector2[] TheaterCardAnchors =
        {
            new Vector2(0.22f, 0.12f),
            new Vector2(0.80f, 0.12f),
            new Vector2(0.16f, 0.62f),
            new Vector2(0.84f, 0.66f),
            new Vector2(0.50f, 0.88f)
        };

        public const string ComingSoonEn = "Coming soon";
        public const string ComingSoonFa = "به زودی";

        public static string ComingSoon(bool fa) => fa ? ComingSoonFa : ComingSoonEn;

        public static bool IsComingSoon(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            string cleaned = value
                .Replace("\u200e", string.Empty)
                .Replace("\u200f", string.Empty)
                .Replace("\u202a", string.Empty)
                .Replace("\u202b", string.Empty)
                .Replace("\u202c", string.Empty)
                .Replace("\u061c", string.Empty)
                .Trim();
            return string.Equals(cleaned, ComingSoonEn, StringComparison.Ordinal) ||
                string.Equals(cleaned, ComingSoonFa, StringComparison.Ordinal);
        }

        /// <summary>
        /// True when the shown label is the logical phrase or the exact string the RTL shaper stores on TMP.
        /// Ordinal after the same format-mark strip, or NFKC of those two forms. A different phrase does not match.
        /// </summary>
        public static bool SameRenderedText(string shown, string expected)
        {
            string left = StripFormatMarks(shown).Trim();
            string right = StripFormatMarks(expected).Trim();
            if (string.Equals(left, right, StringComparison.Ordinal))
                return true;
            string shaped = StripFormatMarks(V3LocalizedTextBindingView.ShapeForRendering(right)).Trim();
            if (string.Equals(left, shaped, StringComparison.Ordinal))
                return true;
            string normLeft = left.Normalize(NormalizationForm.FormKC);
            string normRight = right.Normalize(NormalizationForm.FormKC);
            if (string.Equals(normLeft, normRight, StringComparison.Ordinal))
                return true;
            return string.Equals(normLeft, shaped.Normalize(NormalizationForm.FormKC), StringComparison.Ordinal);
        }

        public static bool IsRenderedComingSoon(string value)
        {
            return SameRenderedText(value, ComingSoonEn) || SameRenderedText(value, ComingSoonFa);
        }

        private static string StripFormatMarks(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\u200C' || c == '\u200D' || c == '\u200B' || c == '\uFEFF' || c == '\u2060' || c == '\u061C'
                    || c == '\u200E' || c == '\u200F' || c == '\u202A' || c == '\u202B' || c == '\u202C'
                    || c == '\u202D' || c == '\u202E' || c == '\u2066' || c == '\u2067' || c == '\u2068' || c == '\u2069')
                    continue;
                builder.Append(c);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Legacy painting-space outlines. The theatre runtime builds screen-space hexagons
        /// instead, so these are no longer the drawn district edges.
        /// </summary>
        public static readonly Vector2[][] TheaterDistrictPolygons =
        {
            new[]
            {
                new Vector2(0.072f, 0.206f), new Vector2(0.074f, 0.185f), new Vector2(0.090f, 0.156f), new Vector2(0.108f, 0.141f),
                new Vector2(0.234f, 0.050f), new Vector2(0.254f, 0.037f), new Vector2(0.295f, 0.026f), new Vector2(0.326f, 0.024f),
                new Vector2(0.528f, 0.024f), new Vector2(0.556f, 0.026f), new Vector2(0.581f, 0.044f), new Vector2(0.584f, 0.063f),
                new Vector2(0.584f, 0.201f), new Vector2(0.582f, 0.222f), new Vector2(0.564f, 0.251f), new Vector2(0.544f, 0.266f),
                new Vector2(0.401f, 0.359f), new Vector2(0.378f, 0.372f), new Vector2(0.337f, 0.384f), new Vector2(0.309f, 0.385f),
                new Vector2(0.124f, 0.385f), new Vector2(0.098f, 0.383f), new Vector2(0.075f, 0.365f), new Vector2(0.072f, 0.346f)
            },
            new[]
            {
                new Vector2(0.619f, 0.221f), new Vector2(0.620f, 0.201f), new Vector2(0.623f, 0.183f), new Vector2(0.629f, 0.165f),
                new Vector2(0.674f, 0.045f), new Vector2(0.686f, 0.029f), new Vector2(0.734f, 0.021f), new Vector2(0.748f, 0.020f),
                new Vector2(0.812f, 0.020f), new Vector2(0.831f, 0.023f), new Vector2(0.919f, 0.047f), new Vector2(0.932f, 0.064f),
                new Vector2(0.946f, 0.172f), new Vector2(0.948f, 0.203f), new Vector2(0.948f, 0.341f), new Vector2(0.948f, 0.360f),
                new Vector2(0.947f, 0.379f), new Vector2(0.946f, 0.383f), new Vector2(0.939f, 0.395f), new Vector2(0.934f, 0.397f),
                new Vector2(0.908f, 0.398f), new Vector2(0.880f, 0.398f), new Vector2(0.676f, 0.398f), new Vector2(0.648f, 0.396f),
                new Vector2(0.622f, 0.379f), new Vector2(0.619f, 0.359f)
            },
            new[]
            {
                new Vector2(0.013f, 0.550f), new Vector2(0.015f, 0.530f), new Vector2(0.032f, 0.508f), new Vector2(0.052f, 0.502f),
                new Vector2(0.191f, 0.469f), new Vector2(0.212f, 0.465f), new Vector2(0.235f, 0.466f), new Vector2(0.244f, 0.470f),
                new Vector2(0.293f, 0.503f), new Vector2(0.300f, 0.510f), new Vector2(0.306f, 0.532f), new Vector2(0.307f, 0.552f),
                new Vector2(0.307f, 0.692f), new Vector2(0.304f, 0.711f), new Vector2(0.281f, 0.729f), new Vector2(0.254f, 0.731f),
                new Vector2(0.066f, 0.731f), new Vector2(0.039f, 0.729f), new Vector2(0.016f, 0.711f), new Vector2(0.013f, 0.691f)
            },
            new[]
            {
                new Vector2(0.653f, 0.594f), new Vector2(0.653f, 0.574f), new Vector2(0.653f, 0.549f), new Vector2(0.656f, 0.538f),
                new Vector2(0.711f, 0.477f), new Vector2(0.721f, 0.469f), new Vector2(0.749f, 0.469f), new Vector2(0.774f, 0.476f),
                new Vector2(0.940f, 0.535f), new Vector2(0.964f, 0.545f), new Vector2(0.984f, 0.570f), new Vector2(0.987f, 0.591f),
                new Vector2(0.987f, 0.731f), new Vector2(0.984f, 0.751f), new Vector2(0.961f, 0.769f), new Vector2(0.936f, 0.771f),
                new Vector2(0.752f, 0.771f), new Vector2(0.726f, 0.771f), new Vector2(0.698f, 0.771f), new Vector2(0.690f, 0.771f),
                new Vector2(0.653f, 0.770f), new Vector2(0.653f, 0.767f), new Vector2(0.653f, 0.750f), new Vector2(0.653f, 0.731f)
            },
            new[]
            {
                new Vector2(0.336f, 0.566f), new Vector2(0.339f, 0.547f), new Vector2(0.365f, 0.530f), new Vector2(0.394f, 0.527f),
                new Vector2(0.599f, 0.527f), new Vector2(0.628f, 0.530f), new Vector2(0.649f, 0.547f), new Vector2(0.649f, 0.566f),
                new Vector2(0.649f, 0.702f), new Vector2(0.649f, 0.722f), new Vector2(0.649f, 0.741f), new Vector2(0.649f, 0.756f),
                new Vector2(0.606f, 0.857f), new Vector2(0.592f, 0.873f), new Vector2(0.528f, 0.897f), new Vector2(0.510f, 0.898f),
                new Vector2(0.430f, 0.882f), new Vector2(0.412f, 0.866f), new Vector2(0.346f, 0.757f), new Vector2(0.338f, 0.741f),
                new Vector2(0.336f, 0.722f), new Vector2(0.336f, 0.703f)
            }
        };

        public static Vector2[] CopyDistrictPolygon(int index)
        {
            Vector2[] source = TheaterDistrictPolygons[index];
            var copy = new Vector2[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        /// <summary>The painted theatre base. There is no collage plate.</summary>
        public static string TheaterPlate => SkirmishLibraryPreviewAssets.TheatreBaseKey;
        public static string Backdrop => ResourceRoot + "theater_backdrop";
        public static string ProgressRing => ResourceRoot + "progress_ring";

        public static string Medal(bool earned, Game.Skirmish.Contracts.SkirmishDifficultyId difficulty)
        {
            if (!earned)
                return ResourceRoot + "medal_empty";
            switch (difficulty)
            {
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Recruit: return ResourceRoot + "medal_recruit";
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Regular: return ResourceRoot + "medal_regular";
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Veteran: return ResourceRoot + "medal_veteran";
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Commander: return ResourceRoot + "medal_commander";
                default: return ResourceRoot + "medal_empty";
            }
        }

        public static string ObjectiveIcon(string objectiveId)
        {
            switch ((objectiveId ?? string.Empty).ToUpperInvariant())
            {
                case "FC": return ResourceRoot + "icon_objective_fc";
                case "BT": return ResourceRoot + "icon_objective_bt";
                case "CE": return ResourceRoot + "icon_objective_ce";
                default: return ResourceRoot + "icon_objective_ba";
            }
        }

        public static string ArmyIcon(string army)
        {
            switch ((army ?? string.Empty).ToUpperInvariant())
            {
                case "A": return ResourceRoot + "icon_army_a";
                case "C": return ResourceRoot + "icon_army_c";
                default: return ResourceRoot + "icon_army_g";
            }
        }

        public static string NewBadge => ResourceRoot + "icon_new";
        public static string Crest => ResourceRoot + "icon_crest";
        public static string Corner => ResourceRoot + "icon_corner";

        public static void Copy(string mapId, bool fa, out string title, out string geography)
        {
            switch (mapId)
            {
                case "CC":
                    title = fa ? "چهارراه شهر" : "CITY CROSSROADS";
                    geography = fa
                        ? "خیابان های باز، مسیر خدماتی و کوچه های پوشیده."
                        : "Open streets, a service lane and covered blocks.";
                    return;
                case "MP":
                    title = fa ? "گذرگاه کوهستان" : "MOUNTAIN PASS";
                    geography = fa
                        ? "رویکرد شکست زره و یک مسیر کناری، روی زمین کمپین موجود."
                        : "The Armor Break approach and a side route, on the existing Campaign ground.";
                    return;
                case "IB":
                    title = fa ? "حوضه صنعتی" : "INDUSTRIAL BASIN";
                    geography = fa
                        ? "حیاط های صنعتی، مسیر باربری و پالایشگاه."
                        : "Industrial yards, a freight route and the refinery.";
                    return;
                case "AP":
                    title = fa ? "دشت فرودگاه" : "AIRFIELD PLAINS";
                    geography = fa
                        ? "یک باند، پیشخان و جاده های دور فرودگاه."
                        : "One runway, the apron and the perimeter roads.";
                    return;
                default:
                    title = fa ? "پایگاه کویر" : "DESERT BASE";
                    geography = fa
                        ? "جاده های باز کویر و لبه شهر."
                        : "Open desert roads and the edge of the city.";
                    return;
            }
        }

        public static void ShortTitle(string mapId, bool fa, out string label)
        {
            switch (mapId)
            {
                case "CC": label = fa ? "شهر" : "CITY"; return;
                case "MP": label = fa ? "کوهستان" : "PASS"; return;
                case "IB": label = fa ? "حوضه" : "BASIN"; return;
                case "AP": label = fa ? "فرودگاه" : "AIRFIELD"; return;
                default: label = fa ? "کویر" : "DESERT"; return;
            }
        }

        public static void ObjectiveCopy(string objectiveId, bool fa, out string title, out string shortLabel)
        {
            switch (objectiveId)
            {
                case "FC":
                    title = fa ? "کنترل خط" : "FRONTLINE CONTROL";
                    shortLabel = fa ? "خط" : "FRONT";
                    return;
                case "BT":
                    title = fa ? "شکستن خط" : "BREAKTHROUGH";
                    shortLabel = fa ? "شکست" : "BREAK";
                    return;
                case "CE":
                    title = fa ? "اسکورت کاروان" : "CONVOY ESCORT";
                    shortLabel = fa ? "کاروان" : "CONVOY";
                    return;
                default:
                    title = fa ? "حمله به پایگاه" : "BASE ASSAULT";
                    shortLabel = fa ? "پایگاه" : "BASE";
                    return;
            }
        }

        public static void ProfileCopy(string code, bool fa, out string label)
        {
            switch (code)
            {
                case "A": label = fa ? "هوایی" : "AIR"; return;
                case "C": label = fa ? "ترکیبی" : "COMBINED"; return;
                case "E": label = fa ? "آماده" : "ESTABLISHED"; return;
                case "F": label = fa ? "میدانی" : "FIELD"; return;
                case "G": label = fa ? "زمینی" : "GROUND"; return;
                default: label = code ?? string.Empty; return;
            }
        }

        public static void DifficultyCopy(Game.Skirmish.Contracts.SkirmishDifficultyId difficulty, bool fa, out string label)
        {
            switch (difficulty)
            {
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Recruit:
                    label = fa ? "تازه‌کار" : "RECRUIT";
                    return;
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Veteran:
                    label = fa ? "کهنه‌کار" : "VETERAN";
                    return;
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Commander:
                    label = fa ? "فرمانده" : "COMMANDER";
                    return;
                default:
                    label = fa ? "عادی" : "REGULAR";
                    return;
            }
        }

        public static string Fallback(string key, bool fa)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith("ui.skirmish.library.", StringComparison.Ordinal))
                return string.Empty;
            string rest = key.Substring("ui.skirmish.library.".Length);
            if (rest.StartsWith("map.", StringComparison.Ordinal))
            {
                string body = rest.Substring(4);
                bool geography = body.EndsWith(".geo", StringComparison.Ordinal);
                string mapId = (geography ? body.Substring(0, body.Length - 4) : body).ToUpperInvariant();
                Copy(mapId, fa, out string title, out string place);
                return geography ? place : title;
            }

            if (rest.StartsWith("objective.", StringComparison.Ordinal))
            {
                ObjectiveCopy(rest.Substring(10).ToUpperInvariant(), fa, out _, out string shortLabel);
                return shortLabel;
            }

            if (rest.StartsWith("army.", StringComparison.Ordinal) || rest.StartsWith("start.", StringComparison.Ordinal))
            {
                ProfileCopy(rest.Substring(rest.IndexOf('.') + 1).ToUpperInvariant(), fa, out string label);
                return label;
            }

            if (rest.StartsWith("difficulty.", StringComparison.Ordinal))
            {
                DifficultyCopy(ParseDifficulty(rest.Substring(11)), fa, out string label);
                return label;
            }

            switch (rest)
            {
                case "locked.not_owned":
                    return fa
                        ? "این نبرد در بسته اصلی است. برای شروع، آن را از فروشگاه بگیر."
                        : "This battle is in the core package. Open the store to add it.";
                case "locked.unavailable":
                    return fa ? "این نبرد الان شروع نمی شود." : "This battle cannot be started.";
                case "locked.planned":
                    return string.Empty;
                case "battles_won":
                    return fa ? "نبردهای برده" : "Battles won";
                case "zone_progress":
                    return fa ? "پیشرفت منطقه" : "ZONE PROGRESS";
                default:
                    return string.Empty;
            }
        }

        private static Game.Skirmish.Contracts.SkirmishDifficultyId ParseDifficulty(string name)
        {
            switch ((name ?? string.Empty).ToLowerInvariant())
            {
                case "recruit": return Game.Skirmish.Contracts.SkirmishDifficultyId.Recruit;
                case "veteran": return Game.Skirmish.Contracts.SkirmishDifficultyId.Veteran;
                case "commander": return Game.Skirmish.Contracts.SkirmishDifficultyId.Commander;
                default: return Game.Skirmish.Contracts.SkirmishDifficultyId.Regular;
            }
        }
    }
}
