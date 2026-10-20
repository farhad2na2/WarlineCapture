using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private string _sagaBriefingScrollScenario;
        private void FitSagaBriefingOverflow(RectTransform rules, RectTransform column, bool battlefield)
        {
            float bottom = _advancedOpen ? 196f : 8f;
            float available = Mathf.Max(1f,rules.rect.height - 6f - bottom);
            float needed = LayoutUtility.GetPreferredHeight(column);
            bool overflow = battlefield && needed > available + 1f;
            var scroll = rules.GetComponent<ScrollRect>();
            var clip = rules.GetComponent<RectMask2D>();
            var bar = rules.Find("BriefingScrollBar");
            if (!overflow)
            {
                if (scroll != null) { scroll.StopMovement(); scroll.enabled=false; }
                if (clip != null) clip.enabled=false;
                if (bar != null) bar.gameObject.SetActive(false);
                Stretch(column,24f,24f,6f,bottom);
                _sagaBriefingScrollScenario=null;
                return;
            }
            clip ??= rules.gameObject.AddComponent<RectMask2D>();
            clip.enabled=true;
            var surface = rules.GetComponent<Graphic>();
            if(surface == null) { var image=rules.gameObject.AddComponent<Image>(); image.color=Color.clear; surface=image; }
            surface.enabled=true;
            surface.raycastTarget=true;
            scroll ??= rules.gameObject.AddComponent<ScrollRect>();
            scroll.enabled=true;
            scroll.viewport=rules;
            scroll.content=column;
            scroll.horizontal=false;
            scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.inertia=true;
            scroll.scrollSensitivity=32f;
            column.anchorMin=new Vector2(0,1);
            column.anchorMax=new Vector2(1,1);
            column.pivot=new Vector2(.5f,1);
            column.sizeDelta=new Vector2(-48f,needed);
            if(_sagaBriefingScrollScenario!=SelectedScenarioId)
            {
                scroll.StopMovement();
                column.anchoredPosition=new Vector2(0,-6f);
                _sagaBriefingScrollScenario=SelectedScenarioId;
            }
            if(bar == null)
            {
                var root=SkirmishWidgetFactory.Rect("BriefingScrollBar",rules);
                var rail=root.gameObject.AddComponent<V3GradientGraphic>();
                rail.Configure(new Color(.04f,.07f,.08f,1),new Color(.02f,.04f,.05f,1),Color.clear,0);
                rail.raycastTarget=false;
                var handle=SkirmishWidgetFactory.Rect("Handle",root);
                Stretch(handle);
                var face=handle.gameObject.AddComponent<V3GradientGraphic>();
                face.Configure(new Color(.40f,.48f,.50f,1),new Color(.22f,.29f,.31f,1),Color.clear,0);
                face.raycastTarget=false;
                var scrollbar=root.gameObject.AddComponent<Scrollbar>();
                scrollbar.handleRect=handle;
                scrollbar.direction=Scrollbar.Direction.BottomToTop;
                scrollbar.transition=Selectable.Transition.None;
                bar=root;
            }
            bar.gameObject.SetActive(true);
            var barRect=(RectTransform)bar;
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            barRect.anchorMin=barRect.anchorMax=new Vector2(fa?0:1,1);
            barRect.pivot=new Vector2(fa?0:1,1);
            barRect.anchoredPosition=new Vector2(fa?6:-6,-8);
            barRect.sizeDelta=new Vector2(6,rules.rect.height-16);
            scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();
            bar.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }
    }
}
