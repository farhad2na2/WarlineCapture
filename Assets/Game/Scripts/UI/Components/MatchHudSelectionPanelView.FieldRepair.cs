using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        Button fieldRepairAction;
        TMP_Text fieldRepairLabel;
        UiVehicleRepairModel presentedFieldRepair;
        int fieldRepairPresentedFrame;
        public bool TryReadPresentedFieldRepair(int frame,out UiVehicleRepairModel model,out Button action)
        {
            model=presentedFieldRepair;action=fieldRepairAction;
            return fieldRepairPresentedFrame==frame&&model!=null&&model.Visible&&HasWheelSelection&&
                fieldRepairLabel!=null&&fieldRepairLabel.gameObject.activeInHierarchy;
        }
        // Called after the ordinary current-order label is set. The existing
        // Campaign text, font and selection layout remain the presentation owner.
        void RefreshFieldRepairContext(string currentOrder)
        {
            presentedFieldRepair=null;fieldRepairPresentedFrame=0;
            if(currentOrderText==null)return;
            if(!HasWheelSelection||!UiShellRuntimeGateway.TryReadVehicleRepair(out var model)||model==null||!model.Applicable||!model.Visible)
            {
                if(fieldRepairAction!=null)fieldRepairAction.gameObject.SetActive(false);
                return;
            }
            if(fieldRepairAction==null)
            {
                if(returnAction==null||returnAction.targetGraphic==null)return;
                var frame=_commandButtonsRoot!=null?_commandButtonsRoot.transform.parent:currentOrderText.transform.parent;
                var root=new GameObject("FieldRepairAction",typeof(RectTransform),typeof(Image),typeof(Button));root.transform.SetParent(frame,false);
                fieldRepairAction=root.GetComponent<Button>();var image=root.GetComponent<Image>();
                if(returnAction.targetGraphic is V3GradientGraphic template)
                {
                    image.color=Color.clear;image.raycastTarget=false;
                    var chrome=new GameObject("Chrome",typeof(RectTransform));chrome.transform.SetParent(root.transform,false);
                    var gradient=chrome.AddComponent<V3GradientGraphic>();gradient.CopyAppearanceFrom(template);gradient.raycastTarget=true;
                    var plate=(RectTransform)gradient.transform;plate.anchorMin=Vector2.zero;plate.anchorMax=Vector2.one;plate.offsetMin=plate.offsetMax=Vector2.zero;
                    fieldRepairAction.targetGraphic=gradient;
                }
                else if(returnAction.targetGraphic is Image source)
                {image.sprite=source.sprite;image.type=source.type;image.color=source.color;fieldRepairAction.targetGraphic=image;}
                else{Object.Destroy(root);fieldRepairAction=null;return;}
                var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(root.transform,false);
                fieldRepairLabel=label.GetComponent<TextMeshProUGUI>();fieldRepairLabel.font=currentOrderText.font;fieldRepairLabel.fontSharedMaterial=currentOrderText.fontSharedMaterial;
                fieldRepairLabel.fontSize=currentOrderText.fontSize;fieldRepairLabel.enableAutoSizing=true;fieldRepairLabel.fontSizeMax=currentOrderText.fontSize;
                fieldRepairLabel.fontSizeMin=Mathf.Min(currentOrderText.fontSize,18);fieldRepairLabel.color=currentOrderText.color;
                fieldRepairLabel.alignment=TextAlignmentOptions.Center;fieldRepairLabel.raycastTarget=false;
                var rect=(RectTransform)label.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,4);rect.offsetMax=new Vector2(-8,-4);
                if(returnAction!=null)fieldRepairAction.colors=returnAction.colors;
                var navigation=fieldRepairAction.navigation;navigation.mode=Navigation.Mode.None;fieldRepairAction.navigation=navigation;
                fieldRepairAction.onClick.AddListener(HandleQuotedFieldRepair);
            }
            presentedFieldRepair=model;fieldRepairPresentedFrame=Time.frameCount;
            bool actionable=model.SingleOwnVehicle&&!model.Paused&&!model.RequestPending&&(model.Eligible||model.Working);
            fieldRepairAction.gameObject.SetActive(true);fieldRepairAction.interactable=actionable;
            UiLocalizedText.Set(fieldRepairLabel,model.ActionLabel);
        }
        float FieldRepairContextHeight=>fieldRepairAction!=null&&fieldRepairAction.gameObject.activeSelf?76:0;
        void LayoutFieldRepairContext(ref float y)
        {if(FieldRepairContextHeight<=0)return;Place(fieldRepairAction.transform as RectTransform,17,y,-34,68);y+=76;}
        void HandleQuotedFieldRepair()
        {
            var shown=presentedFieldRepair;
            if(shown==null||!shown.SingleOwnVehicle||shown.RequestPending||!fieldRepairAction.interactable)return;
            UIAudioEventGateway.Raise(UIAudioEventKind.ButtonPrimaryClick);
            UiShellRuntimeGateway.TryRequestQuotedVehicleRepair(shown,shown.Working);
        }
    }
}
