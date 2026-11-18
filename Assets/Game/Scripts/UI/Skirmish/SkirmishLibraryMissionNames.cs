using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Battle Library card names ("COMMAND SIEGE"). One table for the library atlas and the result
    /// screen, so a player recognises the mission they are retrying. A library card's name depends on
    /// its objective row and its slot in that row (catalog S001-S120: five maps x four objective rows
    /// x six slots, in SkirmishBattleLibraryArt order).
    /// </summary>
    public static class SkirmishLibraryMissionNames
    {
        public const int MissionsPerMap = 24, SlotsPerObjective = 6;

        private static readonly string[] BaEn = { "OUTPOST STRIKE", "COMMAND SIEGE", "AIR RAID", "AIRBASE STRIKE", "COMBINED ADVANCE", "COMBINED SIEGE" };
        private static readonly string[] BaFa = { "یورش به پاسگاه", "محاصره ستاد", "یورش هوایی", "حمله به پایگاه هوایی", "پیشروی ترکیبی", "محاصره ترکیبی" };
        private static readonly string[] FcEn = { "FORWARD LINE", "HOLD THE LINE", "SKYWATCH", "AIR CONTROL", "COMBINED FRONT", "STRONGPOINT" };
        private static readonly string[] FcFa = { "خط مقدم", "حفظ خط", "نگهبان آسمان", "کنترل هوایی", "جبهه ترکیبی", "نقطه استوار" };
        private static readonly string[] BtEn = { "BREAK THE LINE", "BASE BREAKER", "AIR BREAKTHROUGH", "AIRBASE BREACH", "COMBINED PUSH", "FULL ASSAULT" };
        private static readonly string[] BtFa = { "شکستن خط", "شکننده پایگاه", "پیشروی هوایی", "نفوذ هوایی", "پیشروی ترکیبی", "یورش کامل" };
        private static readonly string[] CeEn = { "ROAD ESCORT", "SUPPLY CONVOY", "AIR COVER", "BASE RESUPPLY", "COMBINED ESCORT", "RELIEF COLUMN" };
        private static readonly string[] CeFa = { "اسکورت جاده", "کاروان تدارکات", "پوشش هوایی", "تدارک پایگاه", "اسکورت ترکیبی", "ستون پشتیبانی" };

        /// <summary>Localization key for an objective row ("BA", "FC", "BT", "CE") and slot 0-5.</summary>
        public static string Key(string objective, int slot) =>
            "ui.atlas.mission." + objective?.ToLowerInvariant() + "." + Mathf.Clamp(slot, 0, SlotsPerObjective - 1);

        /// <summary>The untranslated card name for an objective row and slot (Base Assault for unknown rows).</summary>
        public static string Fallback(string objective, int slot, bool farsi)
        {
            int index = Mathf.Clamp(slot, 0, SlotsPerObjective - 1);
            switch (objective)
            {
                case "FC": return (farsi ? FcFa : FcEn)[index];
                case "BT": return (farsi ? BtFa : BtEn)[index];
                case "CE": return (farsi ? CeFa : CeEn)[index];
                default: return (farsi ? BaFa : BaEn)[index];
            }
        }

        public static string Name(string objective, int slot)
        {
            var text = UiShellRuntimeGateway.Localization;
            return text.Get(Key(objective, slot), Fallback(objective, slot, text.IsRightToLeft));
        }

        /// <summary>Objective row and slot of a library catalog id ("S002" -> BA, 1); false for any other id.</summary>
        public static bool TryLocate(string catalogId, out string objective, out int slot)
        {
            objective = null; slot = -1;
            if (string.IsNullOrEmpty(catalogId) || catalogId.Length != 4 || catalogId[0] != 'S' ||
                !int.TryParse(catalogId.Substring(1), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out int number))
                return false;
            int maps = SkirmishBattleLibraryArt.MapIds.Length;
            if (number < 1 || number > maps * MissionsPerMap) return false;
            int index = (number - 1) % MissionsPerMap;
            objective = SkirmishBattleLibraryArt.ObjectiveIds[index / SlotsPerObjective];
            slot = index % SlotsPerObjective;
            return true;
        }

        /// <summary>The library card name for a catalog id, or empty when the id is not a library mission.</summary>
        public static string ForCatalogId(string catalogId) =>
            TryLocate(catalogId, out string objective, out int slot) ? Name(objective, slot) : string.Empty;
    }
}
