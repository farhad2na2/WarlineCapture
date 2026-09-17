using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Composition;
using Game.Runtime;
using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    // Validation uses the displayed controls and screen hit testing. No match facts,
    // health, resources or transforms are edited to manufacture an outcome.
    public static class SkirmishGameplayProbe
    {
        public static void Squad(int index)
        {
            var tray=UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            var cards=(MatchHudSquadTrayView.Card[])typeof(MatchHudSquadTrayView).GetField("cards",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tray);
            Click(cards[index].Button);
        }
        public static void Command(string command)
        {
            var view=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            Click((Button)view.GetType().GetProperty(command+"Button").GetValue(view));
        }
        public static void Click(Button button)
        {
            if(button==null||!button.gameObject.activeInHierarchy||!button.IsInteractable())throw new InvalidOperationException("Control is unavailable.");
            var rect=(RectTransform)button.transform;
            var raycaster=button.GetComponentInParent<GraphicRaycaster>();
            var point=RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)
                throw new InvalidOperationException("Control is occluded: "+button.name);
            button.onClick.Invoke();
            Debug.Log("[SkirmishGameplay] click="+button.name+" screen="+point);
        }
        public static bool Move(float x,float z)=>WorldCommand(x,z,false);
        public static bool Attack(float x,float z)=>WorldCommand(x,z,true);
        private static bool WorldCommand(float x,float z,bool attack)
        {
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var commands=match.MatchBootstrap.SelectionUiCommand;
            var input=(RtsSelectionInputCompositionSystemHelper)typeof(SelectionUiCommandUiSystemHelper)
                .GetField("_inputSystem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(commands);
            var point=match.MatchBootstrap.WorldCamera.WorldToScreenPoint(new Vector3(x,0,z));
            if(point.z<=0||point.x<0||point.y<0||point.x>Screen.width||point.y>Screen.height)throw new InvalidOperationException("Target is outside the current view.");
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count>0)throw new InvalidOperationException("Target is covered by UI: "+hits[0].gameObject.name);
            bool accepted=attack?input.QueueAttackCommandRequest(point,true,Time.frameCount):input.QueueMoveCommandRequest(point,Time.frameCount);
            Debug.Log("[SkirmishGameplay] "+(attack?"Attack":"Move")+" screen="+point+" accepted="+accepted);
            return accepted;
        }
    }
}
