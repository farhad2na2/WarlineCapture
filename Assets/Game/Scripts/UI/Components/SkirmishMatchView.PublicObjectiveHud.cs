using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class SkirmishMatchView
    {
        private UiSkirmishPublicObjectiveModel objectiveModel;
        private readonly List<TMP_Text> objectiveMapLines=new();
        private readonly Dictionary<string,TMP_Text> objectiveMarkers=new(StringComparer.Ordinal);
        private readonly Dictionary<string,SkirmishPublicObjectiveOutlineGraphic> objectiveOutlines=new(StringComparer.Ordinal);
        private readonly HashSet<string> inspectedPublicAreas=new(StringComparer.Ordinal);
        private readonly Dictionary<string,float> bestPublicTravel=new(StringComparer.Ordinal);
        private uint publicTravelRevision;
        private TMP_Text thirdObjective,thirdObjectiveProgress;
        private string firstPublicGoal,secondPublicGoal,thirdPublicGoal,objectiveAttempt;
        private uint objectiveEpoch;
        private Matrix4x4 previousObjectiveCamera;
        private int objectiveCameraSettledFrames;
        private bool previousPublicApplicable;

        // Called by the existing HUD Update after ordinary public model/visibility.
        private void RefreshPublicObjectiveHud(in UiSkirmishModel match)
        {
            bool have=UiShellRuntimeGateway.TryReadSkirmishPublicObjective(out var model)&&model!=null&&model.Applicable;
            if(have!=previousPublicApplicable)
            {
                DeclarePublicObjectiveAria(have);previousPublicApplicable=have;
                if(!have)RestoreBaseObjectiveControls();
            }
            objectiveModel=have?model:null;
            if(!have){HideObjectiveMarkers();return;}
            if(objectiveAttempt!=model.AttemptId)
            {
                objectiveAttempt=model.AttemptId;objectiveEpoch++;if(objectiveEpoch==0)objectiveEpoch=1;
                DeclarePublicObjectiveAria(true);objectiveCameraSettledFrames=0;inspectedPublicAreas.Clear();bestPublicTravel.Clear();publicTravelRevision=0;
            }
            if(!model.Available||model.Goal==null)
            {
                RestoreClockCaption();
                UiLocalizedText.Set(player,model.UnavailableText);UiLocalizedText.Set(playerHealth,"");
                UiLocalizedText.Set(enemy,"");UiLocalizedText.Set(enemyHealth,"");
                PresentPublicObjectiveForAria(null);HideObjectiveMarkers();return;
            }
            var goal=model.Goal;
            var main=new List<SkirmishPublicGoalNode>();
            for(int i=0;i<goal.NodeCount;i++)if(goal.Node(i).Kind!=SkirmishPublicGoalNodeKind.OwnDesignatedSubject)main.Add(goal.Node(i));
            if(main.Count==0||main.Count>3){RestoreClockCaption();PresentPublicObjectiveForAria(null);return;}
            firstPublicGoal=main[0].PublicId;secondPublicGoal=main.Count>1?main[1].PublicId:null;thirdPublicGoal=main.Count>2?main[2].PublicId:null;
            if(thirdObjective==null)
            {
                thirdObjective=Button(hud.transform,"",()=>FocusPublicGoal(thirdPublicGoal));
                thirdObjectiveProgress=HealthLabel(thirdObjective);thirdObjective.transform.parent.SetSiblingIndex(clock.transform.parent.GetSiblingIndex());
                thirdObjective.transform.parent.GetComponent<LayoutElement>().preferredWidth=200;
            }
            thirdObjective.transform.parent.gameObject.SetActive(main.Count>2);
            PresentGoalControl(player,playerHealth,main[0],0);
            enemy.transform.parent.gameObject.SetActive(main.Count>1);
            if(main.Count>1)PresentGoalControl(enemy,enemyHealth,main[1],1);
            if(main.Count>2)PresentGoalControl(thirdObjective,thirdObjectiveProgress,main[2],2);
            if(!string.IsNullOrEmpty(model.PolicyUnavailableText))UiLocalizedText.Set(playerHealth,model.PolicyUnavailableText);
            if(!string.IsNullOrEmpty(model.UnloadStatus))UiLocalizedText.Set(playerHealth,model.UnloadStatus);
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            string score=goal.Score.TicketsKnown?(fa?"بلیت‌ها ":"TICKETS ")+goal.Score.OwnTickets+" / "+goal.Score.OpposingTickets:
                (fa?"تکمیل ":"COMPLETED ")+goal.Score.Completed+" / "+goal.Score.Required;
            if(goal.Score.ProgressKnown)score+="\n"+(fa?"زنده ":"ALIVE ")+(goal.Score.OriginalSubjects-goal.Score.Completed-goal.Score.SubjectsLost);
            PresentObjectiveClock(score);
            RefreshPublicGoalMap(goal,fa);
            PresentPublicConvoyService(model);
            // Rendering the goal HUD alone is not an executable ARIA capability.
            // Exact normal command feedback is mandatory and is never synthesized.
            PublishRenderedObjectiveFrame(model);
            RefreshPublicBoardingHud();
        }

        private void FocusPresentedObjectiveOrBase(bool own)
        {
            if(objectiveModel?.Applicable==true){FocusPublicGoal(own?firstPublicGoal:secondPublicGoal);return;}
            Send(own?UiSkirmishAction.FocusPlayer:UiSkirmishAction.FocusEnemy);
        }
        private static void FocusPublicGoal(string id)
        {if(!string.IsNullOrEmpty(id))UiShellRuntimeGateway.TryFocusSkirmishPublicObjective(id);}
        private void RestoreBaseObjectiveControls()
        {
            enemy.transform.parent.gameObject.SetActive(true);if(thirdObjective!=null)thirdObjective.transform.parent.gameObject.SetActive(false);
            HudIconButtonView.Apply(PlayerFocusButton,HudButtonIcon.YourBase,HudButtonRole.Play,"YOUR BASE","پایگاه شما",player,true);
            HudIconButtonView.Apply(EnemyFocusButton,HudButtonIcon.EnemyBase,HudButtonRole.Stop,"ENEMY BASE","پایگاه دشمن",enemy,true);
            RestoreClockCaption();player.transform.parent.GetComponent<LayoutElement>().preferredWidth=280;enemy.transform.parent.GetComponent<LayoutElement>().preferredWidth=280;
        }
        private void PresentGoalControl(TMP_Text heading,TMP_Text progress,SkirmishPublicGoalNode node,int index)
        {
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            string name=PublicGoalName(node,index,fa);var button=heading.GetComponentInParent<Button>();
            HudIconButtonView.Apply(button,HudButtonIcon.Camera,HudButtonRole.Navigation,name,name,heading,true);
            heading.transform.parent.GetComponent<LayoutElement>().preferredWidth=200;
            UiLocalizedText.Set(progress,PublicGoalProgress(node,fa));AlignBaseHealth(progress);
        }
        private static string PublicGoalName(SkirmishPublicGoalNode node,int index,bool fa)
        {
            string letter=((char)('A'+index)).ToString();
            return node.Kind==SkirmishPublicGoalNodeKind.CaptureZone?(fa?"منطقه ":"ZONE ")+letter:
                node.Kind==SkirmishPublicGoalNodeKind.ExitCorridor?(fa?"مسیر ":"CORRIDOR ")+letter:
                node.Kind==SkirmishPublicGoalNodeKind.EvacuationExit?(fa?"خروج":"EXIT"):
                node.Kind==SkirmishPublicGoalNodeKind.DeliveryDestination?(fa?"مقصد":"DESTINATION"):
                node.RevealedSubjectKind==SkirmishPublicSubjectKind.ObjectiveConvoyTruck?(fa?"کامیون ":"TRUCK ")+(index+1):
                (fa?"سرباز مشخص‌شده ":"DESIGNATED SOLDIER ")+(index+1);
        }
        private string PublicGoalProgress(SkirmishPublicGoalNode node,bool fa)
        {
            if(node.Kind==SkirmishPublicGoalNodeKind.EvacuationExit)return objectiveModel.Goal.Score.ExitOpen?(fa?"باز":"OPEN"):(fa?"بسته":"CLOSED");
            if(node.Kind==SkirmishPublicGoalNodeKind.DeliveryDestination)return (fa?"تحویل ":"DELIVER ")+objectiveModel.Goal.Score.Completed+" / "+objectiveModel.Goal.Score.Required;
            if(node.WorkProgressKnown)return Mathf.FloorToInt(node.WorkTicks/(float)TimeSpan.TicksPerSecond)+" / "+Mathf.CeilToInt(node.RequiredWorkTicks/(float)TimeSpan.TicksPerSecond)+(fa?" ثانیه":" s");
            if(node.Kind==SkirmishPublicGoalNodeKind.CaptureZone)
            {
                string owner=node.OwnerFaction==0?(fa?"بی‌طرف":"NEUTRAL"):node.OwnerFaction==objectiveModel.Goal.ObserverFaction?(fa?"خودی":"OURS"):(fa?"دشمن":"OPPONENT");
                if(node.Contested)return owner+" • "+(fa?"درگیری":"CONTESTED");
                if(node.CaptureProgressKnown)return owner+" • "+(node.CapturePhase==SkirmishPublicCapturePhase.Neutralizing?(fa?"خنثی‌سازی ":"NEUTRALIZE "):(fa?"تصرف ":"CAPTURE "))+Mathf.FloorToInt(node.CaptureFraction*100)+"%";
                return owner;
            }
            return node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live?
                Mathf.CeilToInt(node.Health)+" / "+Mathf.CeilToInt(node.MaximumHealth):
                node.SubjectDisposition==SkirmishPublicSubjectDisposition.Delivered?(fa?"تحویل شد":"DELIVERED"):
                node.SubjectDisposition==SkirmishPublicSubjectDisposition.Evacuated?(fa?"خارج شد":"EVACUATED"):(fa?"از دست رفت":"LOST");
        }
        private void HideObjectiveMarkers(){foreach(var marker in objectiveMarkers.Values)if(marker!=null)marker.gameObject.SetActive(false);foreach(var text in objectiveMapLines)if(text!=null)text.gameObject.SetActive(false);foreach(var outline in objectiveOutlines.Values)if(outline!=null)outline.gameObject.SetActive(false);}
        private void RefreshPublicGoalMap(SkirmishPublicGoalSnapshot goal,bool fa)
        {
            HideObjectiveMarkers();int lines=0,major=0,subjects=0;
            for(int i=0;i<goal.NodeCount;i++)
            {
                var node=goal.Node(i);bool subject=node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject;
                string name=PublicGoalName(node,subject?subjects++:major++,fa);
                if(mapInformation!=null&&fullMap!=null&&fullMap.IsOpen)
                {
                    if(lines==objectiveMapLines.Count)objectiveMapLines.Add(Label(mapInformation,"",HudIconButtonView.LabelSize,34));
                    var line=objectiveMapLines[lines++];line.gameObject.SetActive(true);UiLocalizedText.Set(line,name+" • "+PublicGoalProgress(node,fa));
                }
                if(!node.PositionKnown)continue;
                var position=Array.Find(objectiveModel.Positions,p=>p.PublicId==node.PublicId);
                if(!position.GroundAvailable)continue;
                Vector2 point;bool shown=false;
                var map=fullMap!=null&&fullMap.IsOpen?fullMap.GetComponentInChildren<MatchHudMinimapView>():null;
                if(map!=null)shown=map.TryGetPresentedMapPoint(position.World,null,out point);
                else {var camera=Camera.main;var screen=camera!=null?camera.WorldToScreenPoint(position.World):new Vector3(0,0,-1);point=screen;shown=hud.activeInHierarchy&&screen.z>0&&Screen.safeArea.Contains(point);}
                if(!shown)continue;
                if(!objectiveMarkers.TryGetValue(node.PublicId,out var label)){label=Label(transform,"",HudIconButtonView.LabelSize);label.rectTransform.sizeDelta=new Vector2(240,64);objectiveMarkers.Add(node.PublicId,label);}
                label.gameObject.SetActive(true);label.rectTransform.position=point;UiLocalizedText.Set(label,name+"\n"+PublicGoalProgress(node,fa));
            }
            if(mapInformation!=null&&fullMap!=null&&fullMap.IsOpen)
            {UiLocalizedText.Set(mapPlayer,goal.Score.TicketsKnown?(fa?"بلیت‌های ما":"OUR TICKETS"):(fa?"پیشرفت هدف":"OBJECTIVE PROGRESS"));UiLocalizedText.Set(mapPlayerHealth,goal.Score.TicketsKnown?goal.Score.OwnTickets.ToString():goal.Score.Completed+" / "+goal.Score.Required);UiLocalizedText.Set(mapEnemy,goal.Score.TicketsKnown?(fa?"بلیت‌های حریف":"OPPONENT TICKETS"):"");UiLocalizedText.Set(mapEnemyHealth,goal.Score.TicketsKnown?goal.Score.OpposingTickets.ToString():"");}
            foreach(var perimeter in objectiveModel.Perimeters)
            {
                var projected=new List<Vector2>();
                foreach(var world in perimeter.WorldPoints)
                {
                    Vector2 screen;bool visible;
                    var map=fullMap!=null&&fullMap.IsOpen?fullMap.GetComponentInChildren<MatchHudMinimapView>():null;
                    if(map!=null)visible=map.TryGetPresentedMapPoint(world,null,out screen);
                    else {var point=Camera.main!=null?Camera.main.WorldToScreenPoint(world):new Vector3(0,0,-1);screen=point;visible=hud.activeInHierarchy&&point.z>0&&Screen.safeArea.Contains(screen);}
                    if(!visible)break;
                    if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out var local))break;
                    projected.Add(local);
                }
                if(projected.Count!=perimeter.WorldPoints.Length)continue;
                if(!objectiveOutlines.TryGetValue(perimeter.PublicId,out var outline))
                {var rect=Panel("ObjectivePerimeter",transform,false);Stretch(rect);outline=rect.gameObject.AddComponent<SkirmishPublicObjectiveOutlineGraphic>();outline.color=Cyan;objectiveOutlines.Add(perimeter.PublicId,outline);}
                outline.gameObject.SetActive(true);outline.Present(projected.ToArray());inspectedPublicAreas.Add(perimeter.PublicId);
            }
        }
        private void PublishRenderedObjectiveFrame(UiSkirmishPublicObjectiveModel model)
        {
            bool mapOpen=fullMap!=null&&fullMap.IsOpen;
            if((!hud.activeInHierarchy&&!mapOpen)||!model.Available||!model.CommandFeedbackReady||model.GroundCellSize<=0){PresentPublicObjectiveForAria(null);return;}
            var camera=Camera.main;if(camera==null){PresentPublicObjectiveForAria(null);return;}
            if(camera.worldToCameraMatrix==previousObjectiveCamera)objectiveCameraSettledFrames++;else objectiveCameraSettledFrames=0;
            previousObjectiveCamera=camera.worldToCameraMatrix;
            var targets=new List<AriaPresentedObjectiveTarget>();var approaches=new List<SkirmishPublicPlayerApproach>();var groups=new List<SkirmishPublicPlayerGroup>();
            uint revision=2166136261,selected=0;int selectedCount=0;
            var tray=FindAnyObjectByType<MatchHudSquadTrayView>();
            foreach(var row in model.OwnGroups)
            {
                uint navigation=row.Policy.Dismounted?row.Policy.GroupId:row.UnloadCarrierGroupId;
                if(tray==null||navigation==0||!tray.TryGetExpandedCardTarget(navigation,out _,out _,out _))continue;
                var policy=row.Policy;groups.Add(policy);revision=(revision^policy.GroupId)*16777619;revision=(revision^(uint)row.AliveCount)*16777619;
                if(row.Selected){selected=policy.GroupId;selectedCount++;}
                revision=(revision^navigation)*16777619;foreach(char c in row.UnloadCarrierId??string.Empty)revision=(revision^c)*16777619;
                var transportPanel=FindAnyObjectByType<MatchHudSelectionPanelView>();
                MatchHudPresentedTransportControls transportControls=default;
                bool shownTransport=transportPanel!=null&&transportPanel.TryReadPresentedTransportControls(out transportControls)&&transportControls.Model.Transport==row.UnloadCarrier;
                policy.AcceptedOrderActive=false;policy.AcceptedGoalId=null;
                for(int i=0;i<model.Goal.NodeCount;i++)
                {
                    var node=model.Goal.Node(i);if(!node.PositionKnown)continue;
                    var position=Array.Find(model.Positions,p=>p.PublicId==node.PublicId);if(!position.GroundAvailable)continue;
                    var screen=camera.WorldToScreenPoint(position.World);bool visible=screen.z>0&&Screen.safeArea.Contains(screen);
                    bool inspected=inspectedPublicAreas.Contains(node.PublicId)||node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject;
                    bool inside=AllInsidePublicGoal(model,node,row.OwnMemberPositions);
                    bool policyTarget=model.Goal.Perspective==SkirmishPublicGoalPerspective.Capture?node.Kind==SkirmishPublicGoalNodeKind.CaptureZone:
                        model.Goal.Perspective==SkirmishPublicGoalPerspective.Evacuate?node.Kind==SkirmishPublicGoalNodeKind.ExitCorridor||node.Kind==SkirmishPublicGoalNodeKind.EvacuationExit:
                        policy.DesignatedSubjects.Length>0?node.Kind==SkirmishPublicGoalNodeKind.DeliveryDestination:
                        node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&node.RevealedSubjectKind==SkirmishPublicSubjectKind.ObjectiveConvoyTruck;
                    if(policyTarget&&(row.HoldActive&&inside||row.MoveActive&&AllInsidePublicGoal(model,node,row.CurrentMoveGoals)))
                    {policy.AcceptedOrderActive=true;policy.AcceptedGoalId=node.PublicId;}
                    var focus=node.PublicId==firstPublicGoal?PlayerFocusButton:node.PublicId==secondPublicGoal?EnemyFocusButton:
                        node.PublicId==thirdPublicGoal?thirdObjective?.GetComponentInParent<Button>():null;
                    if(visible&&!inspected)focus=null; // Inspect the actual public map footprint.
                    int id=GroundPublicId(node.PublicId);
                    targets.Add(new AriaPresentedObjectiveTarget {PublicId=node.PublicId,GroupId=policy.GroupId,PublicWorld=position.World,
                        FocusButton=focus,NavigationGroupId=navigation,OpenUnloadButton=shownTransport&&!transportControls.DrawerOpen?transportControls.PassengerChip:null,
                        UnloadButton=shownTransport&&transportControls.DrawerOpen&&transportControls.Model.ExitAllEnabled?transportControls.ExitAll:null,
                        InView=visible&&inspected,GroundFitsPublicGoal=true,
                        Ground=new AriaTouchTarget {Id=id,Position=screen,Available=visible&&!mapOpen} });
                    approaches.Add(new SkirmishPublicPlayerApproach {GroupId=policy.GroupId,GoalId=node.PublicId,Inspected=inspected,
                        GroundCompatible=policy.CanMove||policy.CanHold||policy.CanUnload,InsideGoalFootprint=inside,TravelSeconds=0,ObservedExposure=0});
                }
                if(policy.AcceptedOrderActive)
                    for(int i=0;i<model.Goal.NodeCount;i++)if(model.Goal.Node(i).PublicId==policy.AcceptedGoalId)
                    {
                        var node=model.Goal.Node(i);float distance=Vector2.Distance(new Vector2(policy.WorldX,policy.WorldZ),new Vector2(node.WorldX,node.WorldZ));
                        string key=policy.GroupId+"|"+node.PublicId;
                        if(!bestPublicTravel.TryGetValue(key,out float best))bestPublicTravel[key]=distance;
                        else if(best-distance>=model.GroundCellSize){bestPublicTravel[key]=distance;publicTravelRevision++;}
                    }
            }
            string acceptedGoal=ResolvePublicServiceOrder(model)??ResolvePublicOrderGoal(model);
            int contact=0;
            if(model.Order.Present&&model.Order.Accepted&&acceptedGoal!=null&&model.CompletedContactTargetId!=0&&
                Math.Abs(model.Order.Frame-model.CompletedContactFrame)<=2&&
                (model.Order.Screen-model.CompletedContactPosition).sqrMagnitude<=4)
                contact=model.CompletedContactTargetId;
            uint progress=PublicGoalRevision(model.Goal,false)^publicTravelRevision,significant=PublicGoalRevision(model.Goal,true);
            PresentPublicObjectiveForAria(new AriaPresentedObjectiveFrame {Goal=model.Goal,Groups=groups.ToArray(),Approaches=approaches.ToArray(),Targets=targets.ToArray(),
                Presented=true,OwnModel=model,ConvoyService=PresentedConvoyService(model,camera,mapOpen),Frame=Time.frameCount,PresentationEpoch=objectiveEpoch,RosterRevision=revision,Paused=model.Paused,
                SimulationSeconds=model.SimulationSeconds,SignificantPublicEventRevision=significant,CameraSettled=objectiveCameraSettledFrames>=2,
                ExclusiveSelectedGroup=selectedCount==1?selected:0,MoveMode=UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)&&command.ActiveCommandMode==Game.Tactical.Contracts.TacticalCommandMode.Move,
                AcceptedOrderRevision=model.Order.Present?model.Order.Revision:0,AcceptedGroup=model.Order.GroupId,AcceptedGoalId=acceptedGoal,
                AcceptedAction=model.Order.Action,AcceptedNavigationGroupId=model.Order.NavigationGroupId,AcceptedFrame=model.Order.Frame,AcceptedContactTargetId=contact,ProgressKnown=true,PublicProgressRevision=progress});
        }
        private static int GroundPublicId(string value){unchecked{uint hash=2166136261;foreach(char c in value)hash=(hash^c)*16777619;return (int)(0x30000000u|(hash&0x0fffffffu));}}
        private static bool AllInsidePublicGoal(UiSkirmishPublicObjectiveModel model,SkirmishPublicGoalNode node,Vector2[] points)
        {
            if(points==null||points.Length==0)return false;
            var perimeter=Array.Find(model.Perimeters,p=>p.PublicId==node.PublicId);
            if(perimeter.WorldPoints!=null)
            {foreach(var point in points)if(!model.PublicAreas.Contains(perimeter.Role,point.x,point.y))return false;return true;}
            if(node.Kind!=SkirmishPublicGoalNodeKind.OwnDesignatedSubject)return false;
            foreach(var point in points)if(Mathf.Abs(point.x-node.WorldX)>model.GroundCellSize*.75f||Mathf.Abs(point.y-node.WorldZ)>model.GroundCellSize*.75f)return false;
            return true;
        }
        private static string ResolvePublicOrderGoal(UiSkirmishPublicObjectiveModel model)
        {
            if(!model.Order.Present||!model.Order.Accepted)return null;
            var group=Array.Find(model.OwnGroups,g=>g.Policy.GroupId==model.Order.GroupId);if(group==null)return null;
            if(model.Order.Action==AriaObjectiveAction.Unload)
            {if(!model.Goal.Score.ExitOpen)return null;for(int n=0;n<model.Goal.NodeCount;n++)if(model.Goal.Node(n).Kind==SkirmishPublicGoalNodeKind.EvacuationExit)return model.Goal.Node(n).PublicId;return null;}
            string found=null;
            for(int i=0;i<model.Goal.NodeCount;i++)
            {
                var node=model.Goal.Node(i);if(!node.PositionKnown)continue;
                var points=model.Order.Action==AriaObjectiveAction.Move&&model.Order.WorldKnown?
                    new[]{new Vector2(model.Order.World.x,model.Order.World.z)}:model.Order.Action==AriaObjectiveAction.Hold?group.OwnMemberPositions:null;
                if(!AllInsidePublicGoal(model,node,points)||model.Order.Action==AriaObjectiveAction.Move&&!AllInsidePublicGoal(model,node,group.CurrentMoveGoals))continue;if(found!=null)return null;found=node.PublicId;
            }
            return found;
        }
        private static uint PublicGoalRevision(SkirmishPublicGoalSnapshot goal,bool significant)
        {
            unchecked
            {
                uint value=2166136261;value=(value^(uint)goal.Score.Completed)*16777619;value=(value^(uint)goal.Score.SubjectsLost)*16777619;value=(value^(goal.Score.ExitOpen?1u:0u))*16777619;
                if(!significant){value=(value^(uint)goal.Score.OwnTickets)*16777619;value=(value^(uint)goal.Score.OpposingTickets)*16777619;}
                for(int i=0;i<goal.NodeCount;i++){var node=goal.Node(i);value=(value^(uint)node.OwnerFaction)*16777619;value=(value^(uint)node.SubjectDisposition)*16777619;value=(value^(node.Mounted?1u:0u))*16777619;value=(value^(node.Contested?1u:0u))*16777619;if(!significant){value=(value^(uint)node.WorkTicks)*16777619;value=(value^(uint)(node.CaptureFraction*100))*16777619;}}
                return value;
            }
        }
        private ModeResultContent BuildPresentedSkirmishResult(UiSkirmishModel match)
        {
            var content=BuildResult(match);
            if(objectiveModel?.Applicable!=true)return content;
            content.HasObjectiveFacts=false;content.CompletedObjectives=0;content.IncompleteObjectives=0;content.NeutralObjectives=6;
            content.ObjectiveTitle=match.Objective;content.Objective1=content.Objective2=content.Objective3="";
            content.Objective1State=content.Objective2State=content.Objective3State="";
            if(!objectiveModel.Available||objectiveModel.Goal==null)return content;
            var goal=objectiveModel.Goal;bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            content.HasObjectiveFacts=true;
            if(goal.Score.TicketsKnown)
            {
                int zones=0;for(int i=0;i<goal.NodeCount;i++)if(goal.Node(i).Kind==SkirmishPublicGoalNodeKind.CaptureZone&&goal.Node(i).OwnerKnown&&goal.Node(i).OwnerFaction==goal.ObserverFaction)zones++;
                content.Objective1=fa?"کنترل مناطق":"CONTROL ZONES";content.Objective1State=zones+" / 3";
                content.Objective2=fa?"بلیت‌های ما":"OUR TICKETS";content.Objective2State=goal.Score.OwnTickets.ToString();
                content.Objective3=fa?"بلیت‌های حریف":"OPPONENT TICKETS";content.Objective3State=goal.Score.OpposingTickets.ToString();
                if(zones>=2)content.CompletedObjectives|=1;
            }
            else
            {
                content.Objective1=goal.Perspective==SkirmishPublicGoalPerspective.Evacuate?(fa?"خارج‌شده":"EVACUATED"):(fa?"تحویل‌شده":"DELIVERED");
                content.Objective1State=goal.Score.Completed+" / "+goal.Score.Required;
                content.Objective2=fa?"باقی‌مانده زنده":"REMAINING ALIVE";content.Objective2State=(goal.Score.OriginalSubjects-goal.Score.Completed-goal.Score.SubjectsLost).ToString();
                content.Objective3=fa?"از دست رفته":"LOST";content.Objective3State=goal.Score.SubjectsLost.ToString();
                if(goal.Score.Completed>=goal.Score.Required)content.CompletedObjectives|=1;
            }
            content.Signature+="|"+goal.ObjectiveKind+"|"+goal.Outcome+"|"+goal.Score.Completed+"|"+goal.Score.OwnTickets+"|"+goal.Score.OpposingTickets;
            return content;
        }
    }
}
