using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class NarrativeDialogueView
    {
        [SerializeField] private RectTransform[] authoredHeightTargets = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform[] authoredFooterTargets = Array.Empty<RectTransform>();
        private float[] authoredBaseHeights, authoredFooterPositions;
        private float authoredRootPosition, authoredExtraHeight;

        public void ConfigureAuthoredCaptionLayout(RectTransform[] heightTargets, RectTransform[] footerTargets)
        {
            authoredHeightTargets = heightTargets ?? Array.Empty<RectTransform>();
            authoredFooterTargets = footerTargets ?? Array.Empty<RectTransform>();
            authoredBaseHeights = null;
        }

        private void CacheAuthoredCaptionLayout()
        {
            if (!useAuthoredHeight || dialogueRect == null || authoredBaseHeights != null) return;
            authoredRootPosition = dialogueRect.anchoredPosition.y;
            authoredBaseHeights = new float[authoredHeightTargets.Length];
            authoredFooterPositions = new float[authoredFooterTargets.Length];
            for (int i = 0; i < authoredHeightTargets.Length; i++)
                authoredBaseHeights[i] = authoredHeightTargets[i] != null ? authoredHeightTargets[i].sizeDelta.y : 0;
            for (int i = 0; i < authoredFooterTargets.Length; i++)
                authoredFooterPositions[i] = authoredFooterTargets[i] != null ? authoredFooterTargets[i].anchoredPosition.y : 0;
        }

        private void PrepareAuthoredCaptionLayout(float fontSize)
        {
            CacheAuthoredCaptionLayout();
            authoredExtraHeight = fontSize >= 72f ? 220f : fontSize >= 60f ? 130f : 0f;
            ApplyAuthoredCaptionLayout();
        }

        // The V3 layout owns horizontal expansion. Apply only vertical geometry after
        // it has reacted to aspect/mount changes, keeping the card's bottom edge fixed.
        private void LateUpdate()
        {
            ApplyAuthoredCaptionLayout();
            ApplyIdentityReadingOrder(UiShellRuntimeGateway.Localization.IsRightToLeft);
        }

        private RectTransform[] readingOrderTargets;
        private float[] readingOrderBaseX;
        private MainMenuV3SectionLayoutView readingOrderLayout;
        private void CacheReadingOrderLayout()
        {
            if(!useAuthoredHeight||dialogueRect==null||readingOrderTargets!=null)return;
            var targets=new System.Collections.Generic.List<RectTransform>();
            foreach(Transform child in dialogueRect)
                if(child is RectTransform rect&&child.name!="SpeakerName"&&child.name!="SpeakerRole")targets.Add(rect);
            readingOrderTargets=targets.ToArray();readingOrderBaseX=new float[targets.Count];
            readingOrderLayout=GetComponentInParent<MainMenuV3SectionLayoutView>();
            float appliedWidth=readingOrderLayout!=null?readingOrderLayout.LastAppliedExtraWidth:0;
            for(int i=0;i<targets.Count;i++)
            {
                // Parent Awake may already have expanded these right-anchored controls.
                // Cache reference coordinates so expansion is applied exactly once.
                readingOrderBaseX[i]=targets[i].anchoredPosition.x;
                if(targets[i].name is "NextPanel" or "Pointer" or "NextLabel")readingOrderBaseX[i]-=appliedWidth;
            }
            if(readingOrderLayout!=null)readingOrderLayout.LayoutApplied+=RefreshReadingOrder;
        }
        private void RefreshReadingOrder()=>ApplyIdentityReadingOrder(UiShellRuntimeGateway.Localization.IsRightToLeft);
        private void ApplyDialogueReadingOrder(bool rightToLeft)
        {
            CacheReadingOrderLayout();
            if(readingOrderTargets==null)return;
            float extraWidth=readingOrderLayout!=null?readingOrderLayout.LastAppliedExtraWidth:0;
            for(int i=0;i<readingOrderTargets.Length;i++)
            {
                var rect=readingOrderTargets[i];if(rect==null)continue;
                float x=readingOrderBaseX[i];
                if(rect.name is "NextPanel" or "Pointer" or "NextLabel")x+=extraWidth;
                var position=rect.anchoredPosition;
                position.x=rightToLeft?dialogueRect.rect.width-x-rect.rect.width:x;
                rect.anchoredPosition=position;
                if(rect.name=="Pointer"&&rect.Find("SharedAdvanceIcon") is Transform icon)
                {
                    var scale=icon.localScale;scale.x=Mathf.Abs(scale.x)*(rightToLeft?-1:1);icon.localScale=scale;
                }
            }
        }

        private void ApplyAuthoredCaptionLayout()
        {
            if (!useAuthoredHeight || dialogueRect == null || authoredBaseHeights == null || authoredHeightTargets.Length == 0) return;
            var position = dialogueRect.anchoredPosition;
            position.y = authoredRootPosition + authoredExtraHeight;
            if (dialogueRect.anchoredPosition != position) dialogueRect.anchoredPosition = position;
            for (int i = 0; i < authoredHeightTargets.Length; i++)
            {
                var target = authoredHeightTargets[i]; if (target == null) continue;
                var size = target.sizeDelta; size.y = authoredBaseHeights[i] + authoredExtraHeight;
                if (target.sizeDelta != size) target.sizeDelta = size;
            }
            for (int i = 0; i < authoredFooterTargets.Length; i++)
            {
                var target = authoredFooterTargets[i]; if (target == null) continue;
                var footer = target.anchoredPosition; footer.y = authoredFooterPositions[i] - authoredExtraHeight;
                if (target.anchoredPosition != footer) target.anchoredPosition = footer;
            }
        }
    }
}
