using System.Collections.Generic;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.Editor.SkirmishLibrary
{
    public sealed class SkirmishLibraryWidgetTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
                if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        private RectTransform Root(float width = 1920f, float height = 1080f)
        {
            var go = new GameObject("WidgetTestRoot", typeof(RectTransform));
            _created.Add(go);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static SkirmishWidgetContext Context(bool rtl, float width = 1920f, float height = 1080f)
        {
            SkirmishWidgetContext context = SkirmishWidgetContext.ForFrame(new Vector2(width, height), new Rect(28f, 24f, width - 56f, height - 48f), rtl);
            context.LoadSprite = _ => null;
            context.LoadPreview = _ => null;
            context.PlaySound = _ => { };
            return context;
        }

        private static SkirmishBattleCardState Card(string id, SkirmishMedalMask medals, SkirmishCardAccess access = SkirmishCardAccess.Playable, bool selected = false)
        {
            return new SkirmishBattleCardState
            {
                ScenarioId = id, MapId = "DB", ObjectiveId = "BA", ArmyProfile = "G", StartProfile = "F",
                Medals = medals, Access = access, IsSelected = selected
            };
        }

        [TestCase(1920f, 1080f, SkirmishWidgetAspect.Standard)]
        [TestCase(2400f, 1080f, SkirmishWidgetAspect.Wide)]
        [TestCase(1440f, 1080f, SkirmishWidgetAspect.Narrow)]
        public void AspectIsClassifiedPerFrame(float w, float h, SkirmishWidgetAspect expected)
        {
            Assert.AreEqual(expected, SkirmishWidgetContext.ClassifyAspect(w, h));
        }

        [Test]
        public void NarrowFrameScalesDownByWidth()
        {
            Assert.AreEqual(1f, SkirmishWidgetContext.ResolveScale(1920f, 1080f), 1e-4f);
            Assert.AreEqual(1f, SkirmishWidgetContext.ResolveScale(2400f, 1080f), 1e-4f);
            Assert.Less(SkirmishWidgetContext.ResolveScale(1440f, 1080f), 1f);
        }

        [Test]
        public void MedalSlotsMirrorInRightToLeft()
        {
            const float c = 28f, g = 10f;
            float row = SkirmishMedalRowWidget.RowWidth(c, g);
            for (int i = 0; i < 4; i++)
            {
                float ltr = SkirmishMedalRowWidget.SlotX(i, c, g, false);
                float rtl = SkirmishMedalRowWidget.SlotX(i, c, g, true);
                Assert.AreEqual(row - ltr - c, rtl, 1e-3f, "tier " + i);
            }
            Assert.AreEqual(0f, SkirmishMedalRowWidget.SlotX(0, c, g, false));
            Assert.AreEqual(row - c, SkirmishMedalRowWidget.SlotX(0, c, g, true), 1e-3f);
        }

        [Test]
        public void MedalRowBuildsFourCirclesWithEmptyRings()
        {
            RectTransform row = SkirmishMedalRowWidget.Build(Root(), SkirmishMedalMask.Recruit | SkirmishMedalMask.Veteran, Context(false));
            Assert.AreEqual(4, row.childCount);
            for (int i = 0; i < 4; i++)
                Assert.IsNotNull(row.Find(SkirmishMedalRowWidget.SlotName(i)));
            Assert.AreEqual(SkirmishDifficultyId.Veteran, SkirmishMedalRowWidget.HighestEarned(SkirmishMedalMask.Recruit | SkirmishMedalMask.Veteran));
            Assert.IsNull(SkirmishMedalRowWidget.HighestEarned(SkirmishMedalMask.None));
            StringAssert.EndsWith("medal_empty", SkirmishBattleLibraryArt.Medal(false, SkirmishDifficultyId.Commander));
        }

        [Test]
        public void MapPinHasHeaderArtMedalsAndControlName()
        {
            RectTransform pin = SkirmishBattleCardWidget.BuildMapPin(Root(), Card("S001", SkirmishMedalMask.Regular), Context(false));
            Assert.AreEqual(SkirmishLibraryControlNames.BattleCard("S001"), pin.name);
            Assert.IsNotNull(pin.Find(SkirmishBattleCardWidget.HeaderName));
            Assert.IsNotNull(pin.Find(SkirmishBattleCardWidget.ArtName));
            Assert.IsNotNull(pin.Find("MedalBand/" + SkirmishMedalRowWidget.RowName));
            Assert.IsNull(pin.Find(SkirmishBattleCardWidget.LockName));
            Assert.IsNotNull(pin.GetComponent<Button>());
            Assert.AreEqual(SkirmishBattleCardWidget.PinReferenceSize.x * Context(false).Scale, pin.sizeDelta.x, 1e-3f);
        }

        [Test]
        public void LockedPinShowsShadeAndHidesMedals()
        {
            RectTransform pin = SkirmishBattleCardWidget.BuildMapPin(Root(), Card("S006", SkirmishMedalMask.Commander, SkirmishCardAccess.Planned), Context(false));
            Assert.IsNotNull(pin.Find(SkirmishBattleCardWidget.LockName));
            Assert.IsTrue(SkirmishBattleCardWidget.IsLocked(Card("x", 0, SkirmishCardAccess.NotOwned)));
        }

        [Test]
        public void SelectedPinGetsGlowFrame()
        {
            RectTransform pin = SkirmishBattleCardWidget.BuildMapPin(Root(), Card("S005", SkirmishMedalMask.None, SkirmishCardAccess.Playable, true), Context(false));
            Transform frame = pin.Find(SkirmishGlowFrameWidget.FrameName);
            Assert.IsNotNull(frame);
            Assert.AreEqual(pin.childCount - 1, frame.GetSiblingIndex());
            foreach (string edge in SkirmishGlowFrameWidget.EdgeNames)
                Assert.IsNotNull(frame.Find("Edges/" + edge));
        }

        [Test]
        public void PinHeaderMirrorsInRightToLeft()
        {
            SkirmishWidgetContext rtl = Context(true);
            RectTransform pin = SkirmishBattleCardWidget.BuildMapPin(Root(), Card("S001", 0), rtl);
            var id = (RectTransform)pin.Find(SkirmishBattleCardWidget.HeaderName + "/" + SkirmishBattleCardWidget.IdName);
            var profile = (RectTransform)pin.Find(SkirmishBattleCardWidget.HeaderName + "/Profile");
            Assert.Greater(id.anchoredPosition.x, profile.anchoredPosition.x, "RTL puts the id on the right.");
            Assert.IsTrue(profile.GetComponent<TMP_Text>().isRightToLeftText);
            Assert.IsFalse(id.GetComponent<TMP_Text>().isRightToLeftText);
        }

        [Test]
        public void GridCellBigMedalSitsInTrailingCornerAndMirrors()
        {
            var size = new Vector2(150f, 100f);
            RectTransform ltr = SkirmishBattleCardWidget.BuildGridCell(Root(), Card("S001", SkirmishMedalMask.Recruit), size, Context(false));
            RectTransform rtl = SkirmishBattleCardWidget.BuildGridCell(Root(), Card("S001", SkirmishMedalMask.Recruit), size, Context(true));
            var a = (RectTransform)ltr.Find(SkirmishBattleCardWidget.BigMedalName);
            var b = (RectTransform)rtl.Find(SkirmishBattleCardWidget.BigMedalName);
            Assert.Greater(a.anchoredPosition.x, size.x * 0.5f);
            Assert.Less(b.anchoredPosition.x, size.x * 0.5f);
            Assert.LessOrEqual(a.anchoredPosition.x + a.sizeDelta.x, size.x);
        }

        [Test]
        public void GridCellNewRibbonOnlyForUnwonPlayable()
        {
            var size = new Vector2(150f, 100f);
            Assert.IsNotNull(SkirmishBattleCardWidget.BuildGridCell(Root(), Card("S002", 0), size, Context(false)).Find(SkirmishBattleCardWidget.NewRibbonName));
            Assert.IsNull(SkirmishBattleCardWidget.BuildGridCell(Root(), Card("S003", SkirmishMedalMask.Recruit), size, Context(false)).Find(SkirmishBattleCardWidget.NewRibbonName));
            Assert.IsNull(SkirmishBattleCardWidget.BuildGridCell(Root(), Card("S004", 0, SkirmishCardAccess.Planned), size, Context(false)).Find(SkirmishBattleCardWidget.NewRibbonName));
            RectTransform empty = SkirmishBattleCardWidget.BuildGridCell(Root(), Card(null, 0), size, Context(false));
            Assert.IsNull(empty.GetComponent<Button>());
            Assert.IsNull(empty.Find(SkirmishBattleCardWidget.NewRibbonName));
            Assert.AreEqual(SkirmishBattleCardWidget.EmptyCellName, empty.name);
        }

        [Test]
        public void GridCellArtIsFullBleedAndPendingUsesPlateCrop()
        {
            var size = new Vector2(150f, 100f);
            SkirmishWidgetContext context = Context(false);
            var texture = new Texture2D(16, 9);
            context.LoadPreview = _ => texture;
            RectTransform cell = SkirmishBattleCardWidget.BuildGridCell(Root(), Card("S001", SkirmishMedalMask.Regular), size, context);
            var art = (RectTransform)cell.Find(SkirmishBattleCardWidget.ArtName);
            Assert.IsNotNull(art);
            Assert.AreEqual(Color.white, art.GetComponent<RawImage>().color);
            Assert.IsNull(cell.Find(SkirmishBattleCardWidget.PendingName));
            RectTransform pending = SkirmishBattleCardWidget.BuildGridCell(Root(), Card("S007", 0), size, context, null, true);
            RawImage plate = pending.Find(SkirmishBattleCardWidget.ArtName).GetComponent<RawImage>();
            Assert.IsNull(pending.Find(SkirmishBattleCardWidget.PendingName), "A scenario without its own art shows the plate, not a placeholder.");
            Assert.AreEqual(Color.white, plate.color);
            Assert.Less(Mathf.Abs(plate.uvRect.width), art.GetComponent<RawImage>().uvRect.width, "Plate art is a crop.");
            var empty = new SkirmishBattleCardState { Access = SkirmishCardAccess.Unavailable };
            RectTransform emptyCell = SkirmishBattleCardWidget.BuildGridCell(Root(), empty, size, context);
            Assert.IsNotNull(emptyCell.Find(SkirmishBattleCardWidget.PendingName));
            Assert.Less(emptyCell.Find(SkirmishBattleCardWidget.ArtName).GetComponent<RawImage>().color.r, 0.9f);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PlateCropsDifferPerScenarioAndAreDeterministic()
        {
            var cover = new Rect(0.1f, 0f, 0.8f, 1f);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 5; i <= 10; i++)
            {
                string id = "S" + i.ToString("000");
                Rect crop = SkirmishBattleCardWidget.PlateCrop(cover, id);
                Assert.AreEqual(crop, SkirmishBattleCardWidget.PlateCrop(cover, id));
                seen.Add(crop.ToString("F3"));
            }

            Assert.GreaterOrEqual(seen.Count, 5, "Neighbouring Coming soon cells should show different windows of the plate.");
        }

        [Test]
        public void GridCellSizeFillsGrid()
        {
            Vector2 cell = SkirmishBattleCardWidget.GridCellSize(606f, 400f, 6, 4, 6f);
            Assert.AreEqual(606f, cell.x * 6f + 6f * 5f, 1e-3f);
            Assert.AreEqual(400f, cell.y * 4f + 6f * 3f, 1e-3f);
        }

        [Test]
        public void ZoneTabsReverseInRightToLeftAndShowProgress()
        {
            Rect first = SkirmishZoneTabCardWidget.TabRect(0, 5, 1000f, 120f, 10f, false);
            Rect firstRtl = SkirmishZoneTabCardWidget.TabRect(0, 5, 1000f, 120f, 10f, true);
            Assert.AreEqual(0f, first.x);
            Assert.AreEqual(1000f, firstRtl.xMax, 1e-3f);
            var pin = new SkirmishTheaterPinState { MapId = "DB", WonCount = 9, PublishedCount = 24, ZoneScenarioCount = 24 };
            Assert.AreEqual("9/24", SkirmishZoneTabCardWidget.ProgressText(pin, Context(false)));
            StringAssert.Contains("۹/۲۴", SkirmishZoneTabCardWidget.ProgressText(pin, Context(true)));
            var empty = new SkirmishTheaterPinState { MapId = "MP" };
            Assert.AreEqual(SkirmishBattleLibraryArt.ComingSoon(false), SkirmishZoneTabCardWidget.ProgressText(empty, Context(false)));
            Assert.AreEqual(SkirmishBattleLibraryArt.ComingSoon(true), SkirmishZoneTabCardWidget.ProgressText(empty, Context(true)));
            RectTransform tab = SkirmishZoneTabCardWidget.Build(Root(), pin, true, new Vector2(190f, 120f), Context(false));
            Assert.AreEqual(SkirmishZoneTabCardWidget.TabName("DB"), tab.name);
            Assert.IsNotNull(tab.Find(SkirmishGlowFrameWidget.FrameName));
            Assert.IsNull(SkirmishZoneTabCardWidget.Build(Root(), pin, false, new Vector2(190f, 120f), Context(false)).Find(SkirmishGlowFrameWidget.FrameName));
        }

        [Test]
        public void FooterButtonsPerLevel()
        {
            CollectionAssert.AreEqual(
                new[] { SkirmishFooterBarWidget.BackName, SkirmishLibraryControlNames.Filters, SkirmishLibraryControlNames.ZoomIn, SkirmishLibraryControlNames.QuickBattle },
                Names(SkirmishLibraryLevel.Theater));
            CollectionAssert.AreEqual(
                new[] { SkirmishFooterBarWidget.BackName, SkirmishLibraryControlNames.TheaterMap, SkirmishLibraryControlNames.StartBattle, SkirmishFooterBarWidget.AdvancedName },
                Names(SkirmishLibraryLevel.Battlefield));
        }

        private static string[] Names(SkirmishLibraryLevel level)
        {
            var specs = SkirmishFooterBarWidget.Buttons(level);
            var names = new string[specs.Count];
            for (int i = 0; i < specs.Count; i++) names[i] = specs[i].Name;
            return names;
        }

        [TestCase(1864f)]
        [TestCase(2344f)]
        [TestCase(1384f)]
        public void FooterFitsWidthAndMirrors(float width)
        {
            foreach (SkirmishLibraryLevel level in new[] { SkirmishLibraryLevel.Theater, SkirmishLibraryLevel.Battlefield })
            {
                Rect[] ltr = SkirmishFooterBarWidget.Layout(level, width, 64f, 8f, false);
                Rect[] rtl = SkirmishFooterBarWidget.Layout(level, width, 64f, 8f, true);
                Assert.AreEqual(0f, ltr[0].x, 1e-3f);
                Assert.AreEqual(width, ltr[ltr.Length - 1].xMax, 1e-2f);
                Assert.AreEqual(width, rtl[0].xMax, 1e-2f, "RTL puts Back on the right.");
                for (int i = 0; i < ltr.Length; i++)
                    Assert.AreEqual(width - ltr[i].xMax, rtl[i].x, 1e-2f);
            }
        }

        [Test]
        public void FooterClickRaisesSound()
        {
            SkirmishWidgetContext context = Context(false);
            var sounds = new List<UIAudioEventKind>();
            context.PlaySound = sounds.Add;
            string clicked = null;
            RectTransform bar = SkirmishFooterBarWidget.Build(Root(), SkirmishLibraryLevel.Battlefield, context, n => clicked = n);
            bar.Find(SkirmishLibraryControlNames.StartBattle).GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(SkirmishLibraryControlNames.StartBattle, clicked);
            CollectionAssert.AreEqual(new[] { UIAudioEventKind.ButtonPrimaryClick }, sounds);
            Assert.AreEqual(context.SafeFrame.xMin, bar.anchoredPosition.x, 1e-3f);
            Assert.AreEqual(context.SafeFrame.width, bar.sizeDelta.x, 1e-3f);
        }

        [Test]
        public void LockedCardClickPlaysLockedSound()
        {
            SkirmishWidgetContext context = Context(false);
            var sounds = new List<UIAudioEventKind>();
            context.PlaySound = sounds.Add;
            RectTransform pin = SkirmishBattleCardWidget.BuildMapPin(Root(), Card("S006", 0, SkirmishCardAccess.NotOwned), context);
            pin.GetComponent<Button>().onClick.Invoke();
            CollectionAssert.AreEqual(new[] { UIAudioEventKind.CardLocked }, sounds);
        }

        [Test]
        public void PersianDigits()
        {
            Assert.AreEqual("۱۲/۲۴", SkirmishWidgetContext.ToPersianDigits("12/24"));
        }
    }
}
