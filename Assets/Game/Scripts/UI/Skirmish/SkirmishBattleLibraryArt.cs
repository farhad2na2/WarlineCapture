using System;
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
        /// Hit zones for the one painted collage. They follow the organic territories
        /// and must stay borderless so the plate is the only outline.
        /// </summary>
        public static readonly Rect[] TheaterRegions =
        {
            new Rect(0.00f, 0.00f, 0.43f, 0.54f),
            new Rect(0.38f, 0.00f, 0.34f, 0.50f),
            new Rect(0.66f, 0.00f, 0.34f, 0.48f),
            new Rect(0.00f, 0.46f, 0.48f, 0.54f),
            new Rect(0.42f, 0.44f, 0.58f, 0.56f)
        };

        /// <summary>Label centres on theater_collage.png, top-left normalised, clear of the shared edges.</summary>
        public static readonly Vector2[] TheaterLabelCentres =
        {
            new Vector2(0.18f, 0.26f),
            new Vector2(0.54f, 0.20f),
            new Vector2(0.84f, 0.22f),
            new Vector2(0.20f, 0.74f),
            new Vector2(0.70f, 0.74f)
        };

        public static string TheaterPlate => ResourceRoot + "theater_collage";
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
