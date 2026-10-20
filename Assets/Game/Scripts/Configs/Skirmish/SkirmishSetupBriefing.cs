using System;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    /// <summary>Player-facing facts from the same resolved setup used to launch.</summary>
    public readonly struct SkirmishSetupBriefing
    {
        public readonly string Summary, Objective, Roster, Economy, Intel;

        public SkirmishSetupBriefing(SkirmishResolvedSetup setup, bool fa)
        {
            int minutes = (int)Math.Ceiling(setup.DeadlineSeconds / 60d);
            Summary = fa ? $"۱ حریف · محدودیت {minutes} دقیقه" : $"1 opponent · {minutes}-minute limit";
            Objective = fa
                ? "سربازخانهٔ اصلی دشمن را نابود کن و از سربازخانهٔ خودت دفاع کن. اگر هر دو تا پایان زمان باقی بمانند، نبرد مساوی می‌شود."
                : "Destroy the original enemy Barracks and protect yours. If both survive until time runs out, the match is a draw.";
            Roster = fa
                ? $"نیروی آغازین: {setup.PlayerInfantry} پیاده · {setup.PlayerGround} خودروی رزمی · {setup.PlayerAir} هواگرد\nسقف: {setup.InfantryCapEach} پیاده · {setup.GroundCapEach} خودرو · {setup.TacticalAirCapEach} هواگرد"
                : $"Start: {setup.PlayerInfantry} infantry · {setup.PlayerGround} ground vehicles · {setup.PlayerAir} aircraft\nCaps: {setup.InfantryCapEach} infantry · {setup.GroundCapEach} vehicles · {setup.TacticalAirCapEach} aircraft";
            Roster += DefenderAdvantage(setup.ObjectiveKind,fa);
            Economy = fa
                ? $"آغاز: {setup.MaterialsEach} مصالح · {setup.OilEach} نفت · {setup.UsableFuelEach} سوخت\nبرای ساخت و جذب نیرو مصالح خرج کن. از مسیر تدارکات محافظت کن."
                : $"Start: {setup.MaterialsEach} Materials · {setup.OilEach} Oil · {setup.UsableFuelEach} Fuel\nSpend Materials to build and recruit. Protect your supply route.";
            Intel = setup.DevelopmentFullVision
                ? (fa ? "دید کامل نقشه · بدون پاداش کارزار" : "Full map visibility · No campaign rewards")
                : (fa ? "اهداف را پیش از حمله شناسایی کن" : "Scout targets before attacking");
        }

        public static string DefenderAdvantage(SkirmishObjectiveKind kind,bool fa)
        {
            if(kind!=SkirmishObjectiveKind.Breakthrough&&kind!=SkirmishObjectiveKind.ConvoyEscort)return string.Empty;
            return fa ? "\nبرتری مدافع: دشمن دو برج نگهبانی اضافه در مسیرهای نزدیک‌شدن دارد."
                : "\nDefender advantage: the enemy has two additional watchtowers protecting the approaches.";
        }

        public static string MatchHelp(SkirmishResolvedSetup setup, bool fa)
        {
            // Constant combinations keep this frequently read HUD copy allocation-free.
            const string en = "Destroy the original enemy Barracks and protect yours.\nSelect a squad. Tap Attack, then tap the target.";
            const string faHelp = "سربازخانهٔ اصلی دشمن را نابود کن و پایگاهت را حفظ کن.\nگروه را انتخاب کن؛ حمله و سپس هدف را لمس کن.";
            var army = setup != null ? SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId) : null;
            if (army != null && army.Kind == SkirmishArmyProfileId.CombinedArms)
                return fa
                    ? faHelp + "\nبا زرهی‌ها و پیاده‌ها پیشروی کن؛ برای هلی‌پد، آمادگی را ارتقا بده."
                    : en + "\nAdvance with armor and infantry, or upgrade readiness for a Helipad.";
            if (army?.AllowsOffensiveAir == true)
                return setup.StartPackageId == SkirmishStartPackageId.EstablishedBase
                    ? (fa ? faHelp + "\nهلی‌پد آماده است؛ بالگرد رزمی جذب کن یا با پشتیبانی پیاده‌ها پیشروی کن."
                        : en + "\nYour Helipad is ready. Recruit attack helicopters or advance with infantry support.")
                    : (fa ? faHelp + "\nاز راکت‌اندازها با پیاده‌ها پشتیبانی کن. با حفظ تدارکات و ارتقای آمادگی، هلی‌پد بساز."
                        : en + "\nSupport Rocketeers with infantry, or protect supply and upgrade readiness for a Helipad.");
            return fa ? faHelp : en;
        }

        public static bool TryLoad(string catalogId, int seed, out SkirmishResolvedSetup setup)
            => TryLoad(catalogId, seed, SkirmishSizeId.Standard, out setup);

        public static bool TryLoad(string catalogId, int seed, SkirmishSizeId size, out SkirmishResolvedSetup setup)
        {
            setup = null;
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog == null || !catalog.TryGetDefinition(catalogId, out var definition) || definition == null)
                return false;
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out _)) return false;
            var manifest = new SkirmishContentManifest
            { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
            return SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                size, seed, manifest, matrix, out setup, out _);
        }

        public static string RoleLabel(SkirmishRoleKind role, bool fa) => role switch
        {
            SkirmishRoleKind.Rifle => fa ? "تفنگدار" : "RIFLE",
            SkirmishRoleKind.Gunner => fa ? "تیربارچی" : "GUNNER",
            SkirmishRoleKind.Marksman => fa ? "تک‌تیرانداز" : "MARKSMAN",
            SkirmishRoleKind.Breacher => fa ? "رخنه‌گر" : "BREACHER",
            SkirmishRoleKind.Rocketeer => fa ? "راکت‌انداز" : "ROCKETEER",
            SkirmishRoleKind.Car => fa ? "شناسایی" : "SCOUT CAR",
            SkirmishRoleKind.ApcFast => fa ? "نفربر سریع" : "FAST APC",
            SkirmishRoleKind.ApcArmored => fa ? "نفربر زرهی" : "ARMORED APC",
            SkirmishRoleKind.ApcHeavy => fa ? "نفربر سنگین" : "HEAVY APC",
            SkirmishRoleKind.Tank => fa ? "تانک" : "TANK",
            SkirmishRoleKind.AntiAir => fa ? "پدافند" : "ANTI-AIR",
            SkirmishRoleKind.Radar => fa ? "رادار" : "RADAR",
            SkirmishRoleKind.Siege => fa ? "توپخانه" : "ARTILLERY",
            SkirmishRoleKind.TransportHeli => fa ? "بالگرد ترابری" : "AIRLIFT",
            SkirmishRoleKind.AttackHeliLight => fa ? "بالگرد سبک" : "LIGHT HELI",
            SkirmishRoleKind.AttackHeli => fa ? "بالگرد رزمی" : "ATTACK HELI",
            SkirmishRoleKind.Drone => fa ? "پهپاد" : "DRONE",
            SkirmishRoleKind.Fighter => fa ? "جنگنده" : "FIGHTER",
            SkirmishRoleKind.Strike => fa ? "هواپیمای ضربتی" : "STRIKE",
            SkirmishRoleKind.TransportPlane => fa ? "هواپیمای ترابری" : "TRANSPORT",
            SkirmishRoleKind.LogisticsTruck => fa ? "تدارکات" : "LOGISTICS",
            SkirmishRoleKind.Tanker => fa ? "سوخت‌رسان" : "TANKER",
            SkirmishRoleKind.ConvoyLogic => fa ? "کاروان" : "CONVOY",
            _ => string.Empty
        };
    }
}
