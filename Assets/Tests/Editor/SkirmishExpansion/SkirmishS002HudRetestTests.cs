using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Designer retest 2026-11-18 (S002 Command Siege): base-alert rows clipped their own text and
    // jumped when the top banner expired; the defender control did not say what it does; nothing
    // pointed at the reported enemy base after a successful defence; the result named neither the
    // attacker nor the library mission. HUD presentation only: no gameplay, stats, camera framing or
    // ARIA contract change.
    public sealed class SkirmishS002HudRetestTests
    {
        public const string Marker = "[S002HudRetest] result=Passed";

        public static void RunFocusedValidation()
        {
            var t = new SkirmishS002HudRetestTests();
            t.AlertRowAndPromptFitInEnglishAndFarsiAt1920And2400();
            t.FitShrinksOnlyWhenTheRowCannotFit();
            t.HeldRowDoesNotMoveUpWhenTheBannerExpires();
            t.DefenseHeldPromptFollowsOnlyAClearedAlertWithTheBaseStanding();
            t.BaseLossRecapNamesOnlyARecentVisibleAttacker();
            t.HitCueRunsOncePerNewHit();
            t.LibraryNameMatchesTheBattleLibraryCard();
            t.NewStringsAreInTheConfigAndTheBakedCatalogInBothLocales();
            t.AlertControlsNeverSelectOrOrderAndKeepTheAriaContract();
            Debug.Log(Marker + " controls=9");
        }

        static readonly (string key, string en)[] AlertKeys =
        {
            ("ui.skirmish.base_alert.under_fire", "BASE UNDER FIRE"),
            ("ui.skirmish.base_alert.enemy_at_base", "ENEMY AT BASE"),
            ("ui.skirmish.base_alert.attacker_in_sight", "ATTACKER IN SIGHT"),
            ("ui.skirmish.base_alert.source_unknown", "SOURCE UNKNOWN"),
            ("ui.skirmish.base_alert.show_home_guard", "SHOW HOME GUARD"),
            ("ui.skirmish.base_alert.show_nearest", "SHOW NEAREST SQUAD"),
        };

        static readonly string[] NewKeys =
        {
            "ui.skirmish.base_alert.under_fire", "ui.skirmish.base_alert.enemy_at_base",
            "ui.skirmish.base_alert.attacker_in_sight", "ui.skirmish.base_alert.source_unknown",
            "ui.skirmish.base_alert.show_home_guard", "ui.skirmish.base_alert.show_nearest",
            "ui.skirmish.defense_held.title", "ui.skirmish.defense_held.choice",
            "ui.skirmish.result.base_lost_to_squad", "ui.skirmish.result.base_lost_to_unit",
        };

        static readonly SkirmishRoleKind[] GroundRoles =
        {
            SkirmishRoleKind.Rifle, SkirmishRoleKind.Gunner, SkirmishRoleKind.Marksman, SkirmishRoleKind.Breacher,
            SkirmishRoleKind.Rocketeer, SkirmishRoleKind.Car, SkirmishRoleKind.ApcFast, SkirmishRoleKind.ApcArmored,
            SkirmishRoleKind.ApcHeavy, SkirmishRoleKind.Tank, SkirmishRoleKind.AntiAir, SkirmishRoleKind.Radar,
            SkirmishRoleKind.Siege, SkirmishRoleKind.LogisticsTruck, SkirmishRoleKind.Tanker,
        };

        static GameLocalizationCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            Assert.IsNotNull(catalog, "localization catalog");
            return catalog;
        }

        static string Value(GameLocaleTable table, string key)
        {
            foreach (var record in table.Entries)
                if (record != null && record.Key == key) return record.Value;
            return null;
        }

        static float Measure(TMP_FontAsset font, string text, bool farsi)
        {
            var go = new GameObject("S002HudMeasure", typeof(RectTransform));
            try
            {
                var label = go.AddComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSize = HudIconButtonView.LabelSize;
                label.enableAutoSizing = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                string shown = farsi ? V3LocalizedTextBindingView.ShapeForRendering(text) : text;
                return label.GetPreferredValues(shown, float.PositiveInfinity, float.PositiveInfinity).x;
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void AlertRowAndPromptFitInEnglishAndFarsiAt1920And2400()
        {
            var catalog = Catalog();
            string guid = AssetDatabase.FindAssets("Oxanium-Bold SDF t:TMP_FontAsset").FirstOrDefault();
            Assert.IsNotNull(guid, "HUD font");
            var latin = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            var persianLocale = catalog.FindLocale(GameLocalization.PersianLocaleCode);
            var persian = persianLocale.FontAsset as TMP_FontAsset;
            Assert.IsNotNull(latin); Assert.IsNotNull(persian, "Farsi font");
            foreach (bool fa in new[] { false, true })
            {
                var table = catalog.FindLocale(fa ? GameLocalization.PersianLocaleCode : GameLocalization.EnglishLocaleCode);
                var font = fa ? persian : latin;
                string S(string key) { string v = Value(table, key); Assert.IsFalse(string.IsNullOrEmpty(v), key); return v; }
                float focus = new[] { "under_fire", "enemy_at_base", "attacker_in_sight", "source_unknown" }
                    .Max(k => Measure(font, S("ui.skirmish.base_alert." + k), fa));
                float defender = new[] { "show_home_guard", "show_nearest" }.Max(k => Measure(font, S("ui.skirmish.base_alert." + k), fa));
                foreach (var role in GroundRoles)
                    defender = Mathf.Max(defender, Measure(font, SkirmishSetupBriefing.RoleLabel(role, fa) + " (12)", fa));
                float prompt = Mathf.Max(Measure(font, S("ui.skirmish.defense_held.title"), fa), Measure(font, S("ui.skirmish.defense_held.choice"), fa));
                foreach (float canvas in new[] { 1920f, 2400f })
                {
                    var widths = SkirmishBaseAlertRowLayout.Fit(focus, defender, true, canvas, out bool fits);
                    string at = (fa ? "FA" : "EN") + " @" + canvas;
                    Assert.IsTrue(fits, "alert row fits between the side panels " + at);
                    Assert.LessOrEqual(focus + SkirmishBaseAlertRowLayout.LabelInsets, widths.x, "focus text inside its control " + at);
                    Assert.LessOrEqual(defender + SkirmishBaseAlertRowLayout.LabelInsets, widths.y, "defender text inside its control " + at);
                    Assert.LessOrEqual(SkirmishBaseAlertRowLayout.ButtonWidth(prompt), SkirmishBaseAlertRowLayout.MaxRowWidth(canvas), "base-held prompt " + at);
                }
            }
        }

        [Test]
        public void FitShrinksOnlyWhenTheRowCannotFit()
        {
            var w = SkirmishBaseAlertRowLayout.Fit(200f, 150f, true, 1920f, out bool fits);
            Assert.IsTrue(fits);
            Assert.AreEqual(SkirmishBaseAlertRowLayout.ButtonWidth(200f), w.x);
            Assert.AreEqual(SkirmishBaseAlertRowLayout.ButtonWidth(150f), w.y);
            Assert.AreEqual(SkirmishBaseAlertRowLayout.MinButtonWidth, SkirmishBaseAlertRowLayout.ButtonWidth(10f), "short labels keep a thumb-sized control");
            var alone = SkirmishBaseAlertRowLayout.Fit(200f, 999f, false, 1920f, out fits);
            Assert.IsTrue(fits); Assert.AreEqual(0f, alone.y, "no defender, no width");
            Assert.AreEqual(alone.x, SkirmishBaseAlertRowLayout.RowWidth(alone, false));
            var wide = SkirmishBaseAlertRowLayout.Fit(800f, 800f, true, 1920f, out fits);
            Assert.IsFalse(fits);
            Assert.AreEqual(SkirmishBaseAlertRowLayout.MaxRowWidth(1920f), SkirmishBaseAlertRowLayout.RowWidth(wide, true), .01f);
            Assert.AreEqual(940f, SkirmishBaseAlertRowLayout.MaxRowWidth(1920f), "clear of the selection and ARIA panels at 1920x1080");
            Assert.AreEqual(1420f, SkirmishBaseAlertRowLayout.MaxRowWidth(2400f), "2400x1080");
        }

        [Test]
        public void HeldRowDoesNotMoveUpWhenTheBannerExpires()
        {
            float held = float.NaN;
            Assert.AreEqual(250f, SkirmishBaseAlertRowLayout.HoldDown(250f, ref held, true), "alert onset under the banner");
            Assert.AreEqual(250f, SkirmishBaseAlertRowLayout.HoldDown(160f, ref held, true), "banner expired: the row stays");
            Assert.AreEqual(300f, SkirmishBaseAlertRowLayout.HoldDown(300f, ref held, true), "something new above: it may move down");
            Assert.AreEqual(160f, SkirmishBaseAlertRowLayout.HoldDown(160f, ref held, false), "alert over: free again");
            Assert.IsTrue(float.IsNaN(held));
            Assert.AreEqual(170f, SkirmishBaseAlertRowLayout.HoldDown(170f, ref held, true), "a new alert starts from where it is");
        }

        [Test]
        public void DefenseHeldPromptFollowsOnlyAClearedAlertWithTheBaseStanding()
        {
            var p = default(SkirmishDefenseHeldPrompt);
            Assert.IsFalse(p.Observe(false, true, true, 0f), "no alert yet, no prompt");
            Assert.IsFalse(p.Observe(true, true, true, 1f), "alert up");
            Assert.IsTrue(p.Observe(false, true, true, 2f), "alert cleared with the base standing");
            Assert.IsTrue(p.Observe(false, true, true, 2f + SkirmishDefenseHeldPrompt.Seconds - .1f));
            Assert.IsFalse(p.Observe(false, true, true, 2f + SkirmishDefenseHeldPrompt.Seconds), "times out");

            p.Reset();
            p.Observe(true, true, true, 0f);
            Assert.IsTrue(p.Observe(false, true, true, 1f));
            p.Dismiss();
            Assert.IsFalse(p.Observe(false, true, true, 2f), "tapped: gone");

            p.Reset();
            p.Observe(true, true, true, 0f);
            Assert.IsTrue(p.Observe(false, true, true, 1f));
            Assert.IsFalse(p.Observe(true, true, true, 2f), "a new alert replaces the prompt");

            p.Reset();
            p.Observe(true, true, true, 0f);
            Assert.IsFalse(p.Observe(false, false, true, 1f), "base lost: no defence to praise");
            p.Reset();
            p.Observe(true, true, true, 0f);
            Assert.IsFalse(p.Observe(false, true, false, 1f), "enemy base in sight or not Base Assault: nothing to scout");
            Assert.IsFalse(p.Observe(false, true, true, 2f), "the cleared alert was consumed");
        }

        [Test]
        public void BaseLossRecapNamesOnlyARecentVisibleAttacker()
        {
            var r = default(SkirmishBaseLossRecap);
            r.Observe(SkirmishRoleKind.Rocketeer, attackerVisible: false, baseHit: true, now: 10f);
            Assert.AreEqual(SkirmishRoleKind.None, r.Resolve(true, 11f), "a hidden attacker is never named");
            r.Observe(SkirmishRoleKind.Rocketeer, true, false, 12f);
            Assert.AreEqual(SkirmishRoleKind.None, r.Resolve(true, 13f), "seen, but not hitting the base");
            r.Observe(SkirmishRoleKind.Rocketeer, true, true, 20f);
            Assert.AreEqual(SkirmishRoleKind.Rocketeer, r.Resolve(true, 25f));
            Assert.AreEqual(SkirmishRoleKind.None, r.Resolve(false, 25f), "base held: no loss recap");
            Assert.AreEqual(SkirmishRoleKind.None, r.Resolve(true, 20f + SkirmishBaseLossRecap.RecentSeconds + .5f), "stale sighting");
            r.Observe(SkirmishRoleKind.Tank, true, true, 30f);
            Assert.AreEqual(SkirmishRoleKind.Tank, r.Resolve(true, 31f), "the latest visible attacker");
            Assert.AreEqual("ui.skirmish.result.base_lost_to_squad", SkirmishBaseLossRecap.RecapKey(SkirmishRoleKind.Rocketeer));
            Assert.AreEqual("ui.skirmish.result.base_lost_to_unit", SkirmishBaseLossRecap.RecapKey(SkirmishRoleKind.Tank));
            Assert.AreEqual("Your main base was destroyed by an enemy ROCKETEER squad.",
                string.Format(SkirmishBaseLossRecap.RecapFallback(SkirmishRoleKind.Rocketeer), SkirmishSetupBriefing.RoleLabel(SkirmishRoleKind.Rocketeer, false)));
        }

        [Test]
        public void HitCueRunsOncePerNewHit()
        {
            var c = default(SkirmishBaseHitCue);
            Assert.Less(c.Observe(0, 0f), 0f, "no hit yet");
            Assert.AreEqual(0f, c.Observe(7, 1f), 1e-4f, "a new hit starts the cue");
            Assert.Greater(c.Observe(7, 1f + SkirmishBaseHitCue.Seconds * .5f), 0f);
            Assert.Less(c.Observe(7, 1f + SkirmishBaseHitCue.Seconds), 0f, "ends");
            Assert.Less(c.Observe(7, 5f), 0f, "the same hit does not replay");
            Assert.AreEqual(0f, c.Observe(8, 6f), 1e-4f, "the next hit does");
        }

        [Test]
        public void LibraryNameMatchesTheBattleLibraryCard()
        {
            Assert.IsTrue(SkirmishLibraryMissionNames.TryLocate("S002", out string objective, out int slot));
            Assert.AreEqual("BA", objective); Assert.AreEqual(1, slot);
            Assert.AreEqual("COMMAND SIEGE", SkirmishLibraryMissionNames.Fallback(objective, slot, false));
            Assert.AreEqual("محاصره ستاد", SkirmishLibraryMissionNames.Fallback(objective, slot, true));
            Assert.AreEqual("ui.atlas.mission.ba.1", SkirmishLibraryMissionNames.Key(objective, slot), "the key the atlas card reads");
            Assert.AreEqual(SkirmishLibraryMissionNames.Name("BA", 1), SkirmishLibraryMissionNames.ForCatalogId("S002"));
            Assert.IsTrue(SkirmishLibraryMissionNames.TryLocate("S007", out objective, out slot)); Assert.AreEqual(("FC", 0), (objective, slot));
            Assert.IsTrue(SkirmishLibraryMissionNames.TryLocate("S026", out objective, out slot)); Assert.AreEqual(("BA", 1), (objective, slot));
            Assert.IsTrue(SkirmishLibraryMissionNames.TryLocate("S120", out objective, out slot)); Assert.AreEqual(("CE", 5), (objective, slot));
            foreach (var id in new[] { null, "", "S000", "S121", "X002", "S02", "S0002", "S-01" })
                Assert.IsFalse(SkirmishLibraryMissionNames.TryLocate(id, out _, out _), "not a library id: " + id);
            Assert.AreEqual(string.Empty, SkirmishLibraryMissionNames.ForCatalogId("CUSTOM"));
        }

        [Test]
        public void NewStringsAreInTheConfigAndTheBakedCatalogInBothLocales()
        {
            var catalog = Catalog();
            var en = catalog.FindLocale(GameLocalization.EnglishLocaleCode);
            var fa = catalog.FindLocale(GameLocalization.PersianLocaleCode);
            var configs = V3UiLocalizationCatalogBuilder.ReadUiStringConfigs().ToDictionary(e => e.key);
            foreach (string key in NewKeys)
            {
                Assert.IsTrue(configs.TryGetValue(key, out var config), "config " + key);
                Assert.AreEqual(config.source, Value(en, key), "baked EN " + key);
                string farsi = config.translations.Single(t => t.locale == GameLocalization.PersianLocaleCode).value;
                Assert.AreEqual(farsi, Value(fa, key), "baked FA " + key);
                Assert.AreNotEqual(config.source, farsi, "FA draft present " + key);
            }
            foreach (var (key, english) in AlertKeys) Assert.AreEqual(english, configs[key].source, key);
        }

        [Test]
        public void AlertControlsNeverSelectOrOrderAndKeepTheAriaContract()
        {
            // The ARIA contract (ISkirmishBaseAlertPresentation) is unchanged and still implemented by the view.
            var members = typeof(ISkirmishBaseAlertPresentation).GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(new[] { "BaseAlertAttackerShown", "BaseAlertAttackerWorld", "BaseAlertDefenderButton",
                "BaseAlertDefenderKnown", "BaseAlertDefenderSlot", "BaseAlertPresented", "BaseAlertReason", "BaseAlertSequence" }, members);
            Assert.IsTrue(typeof(ISkirmishBaseAlertPresentation).IsAssignableFrom(typeof(SkirmishMatchView)));

            string root = Path.GetFullPath(Path.Combine(ThisFile(), "..", "..", "..", ".."));
            string Read(string relative) => File.ReadAllText(Path.Combine(root, relative));
            string Body(string source, string start, string end)
            {
                int a = source.IndexOf(start, System.StringComparison.Ordinal);
                int b = source.IndexOf(end, a + 1, System.StringComparison.Ordinal);
                Assert.IsTrue(a >= 0 && b > a, start);
                return source.Substring(a, b - a);
            }
            string view = Read("Game/Scripts/UI/Components/SkirmishMatchView.BaseUnderFire.cs");
            string reveal = Body(view, "private void RevealBaseDefender()", "private float GuidanceRowDown()");
            string tray = Read("Game/Scripts/UI/Components/MatchHudSquadTrayView.Defender.cs");
            string trayReveal = Body(tray, "public bool TryRevealGroup(", "public int VisibleSlotOf(");
            foreach (string body in new[] { reveal, trayReveal })
                foreach (string forbidden in new[] { "_selectGroup", "SelectGroup", "ClearSelection", "Deselect", "UiSkirmishAction", "TryIssue", "Order" })
                    Assert.IsFalse(body.Contains(forbidden), "the defender tap only pages and flashes: " + forbidden);
            string held = Read("Game/Scripts/UI/Components/SkirmishMatchView.DefenseHeld.cs");
            string scout = Body(held, "private void ScoutReportedEnemyBase()", "private void PulseEnemyReadout()");
            Assert.IsTrue(scout.Contains("FocusPresentedObjectiveOrBase(false)"), "the prompt reuses the enemy-base readout action");
            foreach (string forbidden in new[] { "SelectGroup", "ClearSelection", "TryIssue", "Order" })
                Assert.IsFalse(scout.Contains(forbidden), "the prompt does not command troops: " + forbidden);

            // Fog: the attacker's role reaches the HUD only while the attacker is visible to the player.
            string gateway = Read("Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.BaseThreat.cs");
            Assert.IsTrue(gateway.Contains("SkirmishFogService.IsVisibleToFaction(em, source, 1)"));
            Assert.IsTrue(gateway.Contains("if (sourceVisible) model.BaseAttackerRole = ReadGroupRole(em, session, source);"));
            Assert.IsTrue(view.Contains("baseHitCueHasSource = model.BaseAttackerVisible;"), "no shot line from a hidden attacker");
        }

        static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
    }
}
