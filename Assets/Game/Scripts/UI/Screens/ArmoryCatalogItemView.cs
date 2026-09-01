using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ArmoryCatalogItemView : MonoBehaviour
    {
        [SerializeField] private Button selectionButton;
        [SerializeField] private Image frameImage;
        [SerializeField] private Sprite defaultFrameSprite;
        [SerializeField] private Sprite selectedFrameSprite;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private ArmoryCatalogCategoryVisualSet[] categoryVisuals;

        public Button SelectionButton => selectionButton;
        public Image FrameImage => frameImage;

        public void Bind(ArmoryCatalogItem model)
        {
            if (titleText != null)
                titleText.text = model.DisplayName;

            if (typeText != null)
                typeText.text = ArmoryCatalogCategoryFormatter.Format(model.Category);

            BindCategoryVisuals(model.Category, model.CardPortrait);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            GetComponent<ArmoryV3CatalogItemVisual>()?.SetSelected(selected);

            if (frameImage == null)
                return;

            Sprite targetSprite = selected ? selectedFrameSprite : defaultFrameSprite;
            if (targetSprite != null)
                frameImage.sprite = targetSprite;
        }

        private void BindCategoryVisuals(ArmoryCatalogCategory selectedCategory, Sprite portrait)
        {
            if (categoryVisuals == null)
                return;

            for (int i = 0; i < categoryVisuals.Length; i++)
            {
                ArmoryCatalogCategoryVisualSet visualSet = categoryVisuals[i];
                visualSet?.Bind(visualSet.Category == selectedCategory, portrait);
            }
        }
    }
}
