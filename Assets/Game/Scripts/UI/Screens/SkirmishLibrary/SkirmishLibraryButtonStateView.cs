using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Pressed, hover, selected and disabled tints for library buttons.
    /// The graphic stays a <see cref="V3GradientGraphic"/> so the existing chrome is reused.
    /// </summary>
    public sealed class SkirmishLibraryButtonStateView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        private V3GradientGraphic _graphic;
        private bool _selected;
        private bool _enabledVisual = true;
        private bool _opaque = true;
        private bool _hover;
        private bool _pressed;
        private bool _custom;
        private Color _top = new Color(0.07f, 0.12f, 0.14f, 1f);
        private Color _bottom = new Color(0.02f, 0.05f, 0.06f, 1f);
        private Color _border = new Color(0.23f, 0.29f, 0.31f, 1f);

        public void Bind(V3GradientGraphic graphic)
        {
            _graphic = graphic;
        }

        public void Apply(bool selected, bool enabledVisual, bool opaque)
        {
            _custom = false;
            _selected = selected;
            _enabledVisual = enabledVisual;
            _opaque = opaque;
            Button button = GetComponent<Button>();
            if (button != null)
                button.interactable = enabledVisual;
            Paint();
        }

        public void ApplyPalette(Color top, Color bottom, Color border, bool enabledVisual, bool selected)
        {
            _custom = true;
            _top = top;
            _bottom = bottom;
            _border = border;
            _selected = selected;
            _enabledVisual = enabledVisual;
            Paint();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hover = true;
            Paint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            _pressed = false;
            Paint();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            Paint();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            Paint();
        }

        private void Paint()
        {
            if (_graphic == null)
                _graphic = GetComponent<V3GradientGraphic>();
            if (_graphic == null)
                return;

            if (!_enabledVisual)
            {
                _graphic.Configure(
                    new Color(0.16f, 0.16f, 0.17f, 1f),
                    new Color(0.08f, 0.08f, 0.09f, 1f),
                    new Color(0.42f, 0.42f, 0.44f, 1f),
                    2f);
                return;
            }

            Color top;
            Color bottom;
            Color border;
            float width;
            if (_custom)
            {
                top = _top;
                bottom = _bottom;
                border = _selected ? new Color(0.22f, 0.48f, 0.58f, 1f) : _border;
                width = _selected ? 4f : 3f;
            }
            else if (_selected)
            {
                top = new Color(0.07f, 0.18f, 0.23f, 1f);
                bottom = new Color(0.02f, 0.06f, 0.08f, 1f);
                border = new Color(0.22f, 0.48f, 0.58f, 1f);
                width = 3f;
            }
            else
            {
                float body = _opaque ? 1f : 0.94f;
                top = new Color(0.07f, 0.12f, 0.14f, body);
                bottom = new Color(0.02f, 0.05f, 0.06f, body);
                border = new Color(0.23f, 0.29f, 0.31f, 1f);
                width = 2f;
            }

            if (_hover && !_pressed)
                border = Color.Lerp(border, Color.white, 0.35f);
            if (_pressed)
            {
                top = Color.Lerp(top, Color.black, 0.28f);
                bottom = Color.Lerp(bottom, Color.black, 0.28f);
                width += 1f;
            }

            _graphic.Configure(top, bottom, border, width);
        }
    }
}
