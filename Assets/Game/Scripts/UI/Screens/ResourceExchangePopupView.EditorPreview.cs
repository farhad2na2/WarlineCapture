#if UNITY_EDITOR
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class ResourceExchangePopupView
    {
        /// <summary>
        /// Deterministic visual-lock state used only by the two-ratio Play Mode capture
        /// harness. Normal gameplay continues to render the ECS projection through
        /// <see cref="ResourceExchangePopupRuntimeView"/>.
        /// </summary>
        public void ApplyV3TargetLockPreview()
        {
            ApplyHeader("3/3", "180", "620", "310", "7");
            ApplyTabs(true, 3, 3);

            string[] names =
            {
                "FUEL TO MATERIALS",
                "OIL TO MATERIALS",
                "FUEL TO OIL",
                "MATERIALS TO OIL"
            };
            string[] inputs = { "100 FUEL", "100 OIL", "100 FUEL", "100 MATERIALS" };
            string[] outputs = { "180 MATERIALS", "300 MATERIALS", "120 OIL", "15 OIL" };
            if (staticRecipeCards != null)
            {
                for (int i = 0; i < staticRecipeCards.Length; i++)
                {
                    ResourceExchangeRecipeCardView card = staticRecipeCards[i];
                    if (card == null)
                        continue;
                    bool visible = i < names.Length;
                    card.gameObject.SetActive(visible);
                    if (!visible)
                        continue;
                    card.Bind(
                        names[i],
                        inputs[i],
                        outputs[i],
                        i < 2 ? "00:45" : "01:30",
                        string.Empty,
                        ResolveRecipeThumbnail(i),
                        i == 0,
                        true,
                        false,
                        false,
                        defaultRecipeCardFrameSprite,
                        selectedRecipeCardFrameSprite,
                        lockedRecipeCardFrameSprite);
                }
            }

            if (staticQueueRows != null)
            {
                for (int i = 0; i < staticQueueRows.Length; i++)
                    if (staticQueueRows[i] != null)
                        staticQueueRows[i].gameObject.SetActive(i < 3);
                if (staticQueueRows.Length > 0 && staticQueueRows[0] != null)
                    staticQueueRows[0].Bind("1", "Fuel to Materials", "100 FUEL", "180 MATERIALS", "00:11", "65%", "IN PROGRESS", .65f, ResolveRecipeThumbnail(0), true, true, false, false);
                if (staticQueueRows.Length > 1 && staticQueueRows[1] != null)
                    staticQueueRows[1].Bind("2", "Oil to Materials", "100 OIL", "300 MATERIALS", "00:45", "0%", "QUEUED", 0f, ResolveRecipeThumbnail(1), false, true, false, false);
                if (staticQueueRows.Length > 2 && staticQueueRows[2] != null)
                    staticQueueRows[2].Bind("3", "Materials to Oil", "100 MATERIALS", "15 OIL", "DONE", "100%", "COMPLETE", 1f, ResolveRecipeThumbnail(3), false, false, true, false);
            }

            ApplyDetail(
                "Fuel to Materials",
                "CONVERSION ROUTE",
                "1 FUEL = 1.8 MATERIALS",
                "100",
                "100 FUEL",
                "180 MATERIALS",
                "01:30",
                "1 SLOT REQUIRED",
                "Adjust the amount, then convert to add this exchange to the logistics queue.",
                true,
                false,
                ResolveRecipeThumbnail(0));
            ApplyQueueControls(true, true);
        }
    }
}
#endif
