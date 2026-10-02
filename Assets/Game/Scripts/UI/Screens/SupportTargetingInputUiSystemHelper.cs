using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    // Pointer/raycast presentation edge; the ECS request owner remains authoritative.
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(Canvas),typeof(GraphicRaycaster))]
    public sealed class SupportTargetingInputUiSystemHelper : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<RaycastResult> releaseUiHits=new();
        [SerializeField] private GameObject bar;
        [SerializeField] private TMP_Text status,executionStatus;
        private bool executionRendered;private byte lastExecutionKind,lastExecutionPhase;private int lastReserved,lastSeconds;private string lastExecutionReason,lastCollectionReason;
        [SerializeField] private Button confirm,cancel,propose,approve,decline,showTarget;
        [SerializeField] private LineRenderer footprint;
        private Vector2 down;private bool pressed,armed,touchGesture;private UiSupportPhase previousPhase;
        private UiSupportModel renderedModel;private bool rendered;private TMP_Text confirmLabel;
        public void Configure(GameObject toolbar,TMP_Text text,Button commit,Button back,Button ask,Button yes,Button no,Button focus,LineRenderer circle,TMP_Text flightStatus=null)
        {bar=toolbar;status=text;confirm=commit;cancel=back;propose=ask;approve=yes;decline=no;showTarget=focus;footprint=circle;executionStatus=flightStatus;}
        private void OnEnable()
        {UiChromeStack.RaiseAboveTutorial(gameObject);
         confirm.onClick.AddListener(Confirm);cancel.onClick.AddListener(Cancel);propose.onClick.AddListener(Propose);
         approve.onClick.AddListener(Approve);decline.onClick.AddListener(Decline);showTarget.onClick.AddListener(Show);
         confirmLabel=confirm.GetComponentInChildren<TMP_Text>();rendered=false;UiShellRuntimeGateway.Localization.LocaleChanged+=LocaleChanged;}
        private void LocaleChanged(){rendered=false;executionRendered=false;}
        private void OnDisable()
        {confirm.onClick.RemoveListener(Confirm);cancel.onClick.RemoveListener(Cancel);propose.onClick.RemoveListener(Propose);
         approve.onClick.RemoveListener(Approve);decline.onClick.RemoveListener(Decline);showTarget.onClick.RemoveListener(Show);
         UiShellRuntimeGateway.Localization.LocaleChanged-=LocaleChanged;
         if(bar!=null)bar.SetActive(false);if(footprint!=null)footprint.enabled=false;UiShellRuntimeGateway.CancelSupport();}
        private void Confirm()=>UiShellRuntimeGateway.ConfirmSupport();
        private void Cancel()=>UiShellRuntimeGateway.CancelSupport();
        private void Propose()=>UiShellRuntimeGateway.ProposeSupport();
        private void Approve()=>UiShellRuntimeGateway.ApproveSupport();
        private void Decline()=>UiShellRuntimeGateway.DeclineSupport();
        private void Show()=>UiShellRuntimeGateway.ShowSupportTarget();
        private void Update()
        {
            bool has=UiShellRuntimeGateway.TryReadSupport(out var model);
            if(executionStatus!=null)
            {
                bool collectionFailed=has&&!string.IsNullOrEmpty(model.CollectionReasonKey)&&model.CollectionReasonKey!="support.reason.0";
                bool executing=has&&model.Active&&model.Phase==UiSupportPhase.Closed&&(model.ExecutionKind!=0||collectionFailed);
                executionStatus.transform.parent.gameObject.SetActive(executing);
                if(executing&&(!executionRendered||lastExecutionKind!=model.ExecutionKind||lastExecutionPhase!=model.ExecutionPhase||lastReserved!=model.ReservedFuel||lastSeconds!=model.ExecutionSeconds||lastExecutionReason!=model.ExecutionReasonKey||lastCollectionReason!=model.CollectionReasonKey))
                {
                    string name=UiShellRuntimeGateway.Localization.Get("support.name."+model.ExecutionKind,"");
                    string text=model.ExecutionPhase==1?UiShellRuntimeGateway.Localization.Format("support.flight.approaching","",name,model.ReservedFuel,model.ExecutionSeconds):
                        model.ExecutionPhase==2?UiShellRuntimeGateway.Localization.Format("support.flight.deploying","",name):
                        model.ExecutionPhase==5?UiShellRuntimeGateway.Localization.Format("support.flight.partial","",name,UiShellRuntimeGateway.Localization.Get(model.ExecutionReasonKey,"")):
                        model.ExecutionPhase==4?UiShellRuntimeGateway.Localization.Format("support.flight.aborted","",name,UiShellRuntimeGateway.Localization.Get(model.ExecutionReasonKey,"")):
                        UiShellRuntimeGateway.Localization.Format("support.flight.resolved","",name);
                    if(collectionFailed)text=UiShellRuntimeGateway.Localization.Get(model.CollectionReasonKey,"");
                    UiLocalizedText.Set(executionStatus,text);lastCollectionReason=model.CollectionReasonKey;lastExecutionKind=model.ExecutionKind;lastExecutionPhase=model.ExecutionPhase;
                    lastReserved=model.ReservedFuel;lastSeconds=model.ExecutionSeconds;lastExecutionReason=model.ExecutionReasonKey;executionRendered=true;
                }
            }
            bool visible=has && model.Phase is UiSupportPhase.Targeting or UiSupportPhase.Preview or UiSupportPhase.Pending or UiSupportPhase.Collecting;
            bar.SetActive(visible);if(footprint!=null)footprint.enabled=visible && model.Phase==UiSupportPhase.Preview;
            if(!visible){armed=false;pressed=false;previousPhase=model.Phase;return;}
            // Wait for the modal's hide sequence to finish before accepting a fresh world gesture.
            if(UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.CurrentMode==UiShellMode.PopupOnly)
            {armed=false;pressed=false;return;}
            confirm.interactable=model.Phase==UiSupportPhase.Preview && model.Valid && !model.HasProposal;
            propose.interactable=confirm.interactable;approve.gameObject.SetActive(model.HasProposal);decline.gameObject.SetActive(model.HasProposal);
            showTarget.interactable=model.Phase is UiSupportPhase.Preview or UiSupportPhase.Collecting;
            if(!rendered || renderedModel.Version!=model.Version || renderedModel.Phase!=model.Phase || renderedModel.HasProposal!=model.HasProposal || renderedModel.ReasonKey!=model.ReasonKey || renderedModel.CollectionReasonKey!=model.CollectionReasonKey)
            {
                string instruction=model.Phase==UiSupportPhase.Collecting?"support.supply.target":model.Selected==1?"support.target.instruction":model.Selected==2?"support.target.strike":"support.target.landing";
                string message=model.HasProposal?UiShellRuntimeGateway.Localization.Format("support.consent","",UiShellRuntimeGateway.Localization.Get("support.name."+model.Selected,""),model.Ability(model.Selected).FuelCost):
                    UiShellRuntimeGateway.Localization.Get(model.Phase is UiSupportPhase.Targeting or UiSupportPhase.Collecting?instruction:model.ReasonKey,"");
                if(model.Phase==UiSupportPhase.Collecting&&model.CollectionReasonKey!="support.reason.0"&&!string.IsNullOrEmpty(model.CollectionReasonKey))message=UiShellRuntimeGateway.Localization.Get(model.CollectionReasonKey,"")+" · "+message;
                if(!string.IsNullOrEmpty(model.TargetName))message=model.TargetName+" · "+message;
                UiLocalizedText.Set(status,model.Phase==UiSupportPhase.Preview && !model.HasProposal?
                    UiShellRuntimeGateway.Localization.Format("support.preview.cost","Cost: 1 charge + {0} Fuel · {1}",model.Ability(model.Selected).FuelCost,message):message);
                if(confirmLabel!=null)UiLocalizedText.Set(confirmLabel,UiShellRuntimeGateway.Localization.Get(model.Selected==1?"support.confirm.smoke":"support.confirm",""));
                renderedModel=model;rendered=true;
            }
            if(footprint!=null && footprint.enabled)
            {
                footprint.startColor=footprint.endColor=model.Valid?Color.green:Color.red;
                for(int i=0;i<65;i++){float angle=i*Mathf.PI*2/64;footprint.SetPosition(i,model.Target+new Vector3(Mathf.Cos(angle)*model.Radius,.15f,Mathf.Sin(angle)*model.Radius));}
            }
            bool held=false;Vector2 position=default;
            if(Touchscreen.current!=null && (Touchscreen.current.primaryTouch.press.isPressed || touchGesture))
            {held=Touchscreen.current.primaryTouch.press.isPressed;position=Touchscreen.current.primaryTouch.position.ReadValue();touchGesture=held;}
            else if(Mouse.current!=null){held=Mouse.current.leftButton.isPressed;position=Mouse.current.position.ReadValue();}
            if(previousPhase!=model.Phase && model.Phase==UiSupportPhase.Targeting){armed=false;pressed=false;}
            previousPhase=model.Phase;
            if(!armed){if(!held)armed=true;return;}
            if(held && !pressed){down=position;pressed=true;}
            if(!held && pressed)
            {
                pressed=false;
                // This helper runs before EventSystem.Update. Its pointer-over
                // cache still describes the previous frame (and pointer IDs vary
                // between mouse and touch). Check this release position instead.
                bool over=false;
                if(EventSystem.current!=null)
                {
                    releaseUiHits.Clear();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},releaseUiHits);
                    foreach(var uiHit in releaseUiHits)if(uiHit.module is GraphicRaycaster){over=true;break;}
                }
                if((down-position).sqrMagnitude>144 || over || model.Phase==UiSupportPhase.Pending)return;
                Camera camera=Camera.main;if(camera==null)return;
                Ray ray=camera.ScreenPointToRay(position);Vector3 target;
                if(Physics.Raycast(ray,out var hit,10000))target=hit.point;
                else {var plane=new Plane(Vector3.up,Vector3.zero);if(!plane.Raycast(ray,out float distance))return;target=ray.GetPoint(distance);}
                UiShellRuntimeGateway.PreviewSupportPointer(position,target);
            }
        }
    }
}
