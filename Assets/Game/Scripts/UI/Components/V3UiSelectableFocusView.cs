using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class V3UiSelectableFocusView : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Graphic focusGraphic;
        [SerializeField] private bool showOnPointerHover = true;

        private bool selected;
        private bool hovered;

        public void Configure(Graphic graphic, bool pointerHover = true)
        {
            focusGraphic = graphic;
            showOnPointerHover = pointerHover;
            Refresh();
        }

        private void Awake()
        {
            Refresh();
        }

        private void OnDisable()
        {
            selected = false;
            hovered = false;
            Refresh();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Refresh();
        }

        private void Refresh()
        {
            if (focusGraphic != null)
                focusGraphic.gameObject.SetActive(selected || (showOnPointerHover && hovered));
        }
    }
}
