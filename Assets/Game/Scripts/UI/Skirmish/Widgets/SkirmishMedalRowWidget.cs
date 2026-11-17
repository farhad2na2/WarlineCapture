using Game.Skirmish.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Four medal circles (Recruit, Regular, Veteran, Commander). Unearned tiers show an empty ring.
    /// In RTL locales the row is mirrored so Recruit reads first from the right.
    /// </summary>
    public static class SkirmishMedalRowWidget
    {
        public const string RowName = "MedalRow";
        public static readonly SkirmishDifficultyId[] Tiers =
        {
            SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular,
            SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander
        };

        public static string SlotName(int tierIndex) => "Medal_" + Tiers[tierIndex];

        public static bool Earned(SkirmishMedalMask mask, SkirmishDifficultyId tier)
        {
            switch (tier)
            {
                case SkirmishDifficultyId.Recruit: return (mask & SkirmishMedalMask.Recruit) != 0;
                case SkirmishDifficultyId.Regular: return (mask & SkirmishMedalMask.Regular) != 0;
                case SkirmishDifficultyId.Veteran: return (mask & SkirmishMedalMask.Veteran) != 0;
                case SkirmishDifficultyId.Commander: return (mask & SkirmishMedalMask.Commander) != 0;
                default: return false;
            }
        }

        /// <summary>Highest earned tier, or null when no medal is earned.</summary>
        public static SkirmishDifficultyId? HighestEarned(SkirmishMedalMask mask)
        {
            for (int i = Tiers.Length - 1; i >= 0; i--)
                if (Earned(mask, Tiers[i])) return Tiers[i];
            return null;
        }

        public static float RowWidth(float circle, float gap) => circle * Tiers.Length + gap * (Tiers.Length - 1);

        /// <summary>Left x of a tier's circle inside a row of <see cref="RowWidth"/>; mirrored for RTL.</summary>
        public static float SlotX(int tierIndex, float circle, float gap, bool rightToLeft)
        {
            int visual = rightToLeft ? Tiers.Length - 1 - tierIndex : tierIndex;
            return visual * (circle + gap);
        }

        /// <summary>Builds the row centred in <paramref name="parent"/>; sizes are reference pixels scaled by the context.</summary>
        public static RectTransform Build(Transform parent, SkirmishMedalMask medals, SkirmishWidgetContext context,
            float circle = 28f, float gap = 10f)
        {
            float c = context.Px(circle);
            float g = context.Px(gap);
            RectTransform row = SkirmishWidgetFactory.Rect(RowName, parent);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(RowWidth(c, g), c);
            for (int i = 0; i < Tiers.Length; i++)
            {
                bool earned = Earned(medals, Tiers[i]);
                Image medal = SkirmishWidgetFactory.Image(SlotName(i), row,
                    earned ? Color.white : new Color(0.85f, 0.87f, 0.88f, 0.9f),
                    context.LoadSprite(SkirmishBattleLibraryArt.Medal(earned, Tiers[i])));
                medal.preserveAspect = true;
                SkirmishWidgetFactory.TopLeft(medal.rectTransform, SlotX(i, c, g, context.RightToLeft), 0f, c, c);
                if (medal.sprite == null)
                    medal.color = earned ? TierColor(Tiers[i]) : new Color(1f, 1f, 1f, 0.25f);
            }
            return row;
        }

        public static Color TierColor(SkirmishDifficultyId tier)
        {
            switch (tier)
            {
                case SkirmishDifficultyId.Recruit: return new Color(0.80f, 0.45f, 0.20f, 1f);
                case SkirmishDifficultyId.Regular: return new Color(0.78f, 0.80f, 0.82f, 1f);
                case SkirmishDifficultyId.Veteran: return new Color(0.95f, 0.72f, 0.20f, 1f);
                case SkirmishDifficultyId.Commander: return new Color(0.85f, 0.15f, 0.12f, 1f);
                default: return Color.white;
            }
        }
    }
}
