using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class AriaPresentedConvoyService
    {
        public UiSkirmishPublicObjectiveModel Model;
        public Button FocusButton;
        public AriaTouchTarget[] Ground=Array.Empty<AriaTouchTarget>();
        public bool[] GroundVisible=Array.Empty<bool>();
    }
    public static class SkirmishPublicServiceAcceptance
    {
        public static bool AllInside(UiPublicConvoyService service,Vector2[] points)
        {
            if(service==null||!service.Available||service.EligibilityRadiusMetres!=12||points==null||points.Length==0)return false;
            foreach(var point in points)
                if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||(point-new Vector2(service.World.x,service.World.z)).sqrMagnitude>144)return false;
            return true;
        }
        public static bool Move(UiSkirmishPublicObjectiveModel model,UiSkirmishPublicGroupRow group,int[] presentedGroundIds)
        {
            if(model==null||group==null||group.AliveCount!=1||model.Service==null||!model.Order.Present||!model.Order.Accepted||
                model.Order.Action!=AriaObjectiveAction.Move||model.Order.GroupId!=group.Policy.GroupId||!model.Order.WorldKnown||
                model.CompletedContactTargetId==0||presentedGroundIds==null||Array.IndexOf(presentedGroundIds,model.CompletedContactTargetId)<0||
                Math.Abs(model.Order.Frame-model.CompletedContactFrame)>2||(model.Order.Screen-model.CompletedContactPosition).sqrMagnitude>4)return false;
            return AllInside(model.Service,new[]{new Vector2(model.Order.World.x,model.Order.World.z)})&&AllInside(model.Service,group.CurrentMoveGoals);
        }
    }
    public sealed partial class SkirmishMatchView
    {
        uint serviceMoveAcceptedGroup,serviceOrderRevision;
        string serviceOrderAttempt;
        string serviceJourneyDiagnosis;
        public void PresentConvoyServiceDiagnosis(string message){serviceJourneyDiagnosis=message;}
        void PresentPublicConvoyService(UiSkirmishPublicObjectiveModel model)
        {
            if(model.Goal.Perspective!=SkirmishPublicGoalPerspective.Escort)return;
            if(serviceOrderAttempt!=model.AttemptId){serviceOrderAttempt=model.AttemptId;serviceMoveAcceptedGroup=serviceOrderRevision=0;serviceJourneyDiagnosis=null;}
            if(model.Service==null||!model.Service.Available){if(!string.IsNullOrEmpty(serviceJourneyDiagnosis))UiLocalizedText.Set(playerHealth,serviceJourneyDiagnosis);return;}
            // Reuse the existing second Campaign objective/focus control. This
            // callback still only requests a camera focus, never selection/order.
            secondPublicGoal=model.Service.PublicId;enemy.transform.parent.gameObject.SetActive(true);
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;string title=fa?"محل تعمیر":"SERVICE PAD";
            HudIconButtonView.Apply(EnemyFocusButton,HudButtonIcon.Camera,HudButtonRole.Navigation,title,title,enemy,true);
            UiLocalizedText.Set(enemyHealth,(fa?"شعاع ۱۲ متر · مصالح ":"WITHIN 12 m · MATERIALS ")+model.Service.Materials);
            if(!string.IsNullOrEmpty(serviceJourneyDiagnosis))UiLocalizedText.Set(enemyHealth,serviceJourneyDiagnosis);
            enemy.transform.parent.GetComponent<LayoutElement>().preferredWidth=200;
        }
        AriaPresentedConvoyService PresentedConvoyService(UiSkirmishPublicObjectiveModel model,Camera camera,bool mapOpen)
        {
            if(model.Goal.Perspective!=SkirmishPublicGoalPerspective.Escort||model.Service==null||!model.Service.Available||
                secondPublicGoal!=model.Service.PublicId||EnemyFocusButton==null||!EnemyFocusButton.gameObject.activeInHierarchy)return null;
            var targets=new List<AriaTouchTarget>();var visible=new List<bool>();
            for(int i=0;i<model.Service.Destinations.Length;i++)
            {
                var destination=model.Service.Destinations[i];var screen=camera.WorldToScreenPoint(destination.World);
                destination.PresentedGroundId=GroundPublicId(model.Service.PublicId+"/"+i);model.Service.Destinations[i]=destination;
                bool shown=destination.GroundAvailable&&screen.z>camera.nearClipPlane&&screen.z<camera.farClipPlane&&Screen.safeArea.Contains(screen);
                visible.Add(shown);targets.Add(new AriaTouchTarget{Id=destination.PresentedGroundId,Position=screen,Available=shown&&!mapOpen});
            }
            return new AriaPresentedConvoyService{Model=model,FocusButton=EnemyFocusButton,Ground=targets.ToArray(),GroundVisible=visible.ToArray()};
        }
        string ResolvePublicServiceOrder(UiSkirmishPublicObjectiveModel model)
        {
            if(model.Service==null||!model.Service.Available||!model.Order.Present||!model.Order.Accepted)return null;
            var group=Array.Find(model.OwnGroups,row=>row.Policy.GroupId==model.Order.GroupId);
            if(group==null||group.AliveCount!=1)return null;
            var presentedIds=new int[model.Service.Destinations.Length];
            for(int i=0;i<model.Service.Destinations.Length;i++)
                presentedIds[i]=GroundPublicId(model.Service.PublicId+"/"+i);
            if(model.Order.Action==AriaObjectiveAction.Move)
            {
                bool exact=SkirmishPublicServiceAcceptance.Move(model,group,presentedIds);
                if(model.Order.Revision!=serviceOrderRevision)
                {serviceOrderRevision=model.Order.Revision;serviceMoveAcceptedGroup=exact?group.Policy.GroupId:0;}
                if(exact)return model.Service.PublicId;
            }
            bool shownServiceIntent=UiShellRuntimeGateway.ReadAriaPlay().Active&&UiShellRuntimeGateway.ReadAriaSkirmishIntent()==AriaSkirmishIntent.ServiceOriginalTruck;
            if(model.Order.Action==AriaObjectiveAction.Hold&&(serviceMoveAcceptedGroup==group.Policy.GroupId||shownServiceIntent)&&SkirmishPublicServiceAcceptance.AllInside(model.Service,group.OwnMemberPositions))return model.Service.PublicId;
            return null;
        }
    }
}
