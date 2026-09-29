using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Game.Editor
{
    public static partial class CH04M03SplitFrontInputProbe
    {
        private static AriaTouchInputUiSystemHelper smokeHand;
        private static GameObject smokeHost;
        private static int smokeStage;
        private static double nextSmokeAction;
        private static double committedAt;
        private static bool smokeCover,smokeExpiry;
        private sealed class SmokeInputHost:MonoBehaviour {private void Update()=>smokeHand?.Tick(Time.unscaledTime);}
        private static void ResetOptionalSmokeProbe(){smokeStage=cancelStage=0;observedCancelStage=-1;nextSmokeAction=0;smokeCover=smokeExpiry=false;}
        private static void FinishOptionalSmokeProbe(){smokeHand?.Dispose();smokeHand=null;if(smokeHost!=null)UnityEngine.Object.Destroy(smokeHost);smokeHost=null;}
        private static bool TickOptionalSmoke(EntityManager em,Entity root)
        {
            if(smokeStage==6)return true;
            if(!UiShellRuntimeGateway.TryReadSupport(out var model)||!model.Active)return false;
            if(em.GetComponentData<SupportSessionComponent>(root).TestEncounter!=0||em.GetComponentData<SupportMissionPolicyComponent>(root).TestGrantMask!=0)
                throw new InvalidOperationException("Split Front must use production Smoke ownership and context");
            if(smokeHand==null){smokeHand=new AriaTouchInputUiSystemHelper();if(!smokeHand.Start())throw new InvalidOperationException("Smoke touch actuator unavailable");smokeHost=new GameObject("Split Front Smoke Input",typeof(SmokeInputHost)){hideFlags=HideFlags.DontSave};}
            if(smokeHand.IsBusy||EditorApplication.timeSinceStartup<nextSmokeAction)return false;
            if(smokeStage==0)
            {
                if(!model.Smoke.Available)throw new InvalidOperationException("Authored optional Smoke unavailable: "+model.Smoke.ReasonKey);
                if(model.Fuel!=4||model.Smoke.Charges!=2)throw new InvalidOperationException("Mission Smoke must start with four usable Fuel and two charges");
                if(SmokeTapNamed("SupportCommand"))smokeStage=1;return false;
            }
            if(smokeStage==1){Shot("support-optional");if(SmokeTapUnder<SupportPopupView>("BeginTargeting"))smokeStage=2;return false;}
            if(smokeStage==2)
            {
                if(model.Phase!=UiSupportPhase.Targeting)return false;
                foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                {
                    if(member.FactionId!=1||!em.Exists(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;
                    var point=Camera.main.WorldToScreenPoint(em.GetComponentData<LocalTransform>(member.Entity).Position);
                    if(point.z<=0||!Screen.safeArea.Contains(point))continue;
                    var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                    if(hits.Count==0&&SmokeGesture(point)){smokeStage=3;break;}
                }
                return false;
            }
            if(smokeStage==3)
            {
                if(model.Phase!=UiSupportPhase.Preview||!model.Valid)return false;
                if(em.GetBuffer<SupportReceiptElement>(root).Length!=0||model.Fuel!=4)throw new InvalidOperationException("Smoke preview spent Fuel");
                Shot("support-safe-preview");if(SmokeTapUnder<SupportTargetingInputUiSystemHelper>("Confirm"))smokeStage=4;return false;
            }
            using var zones=em.CreateEntityQuery(typeof(SupportSmokeZoneComponent));
            if(smokeStage==4)
            {
                var receipts=em.GetBuffer<SupportReceiptElement>(root,true);if(receipts.Length==0)return false;
                if(receipts.Length!=1||receipts[0].Reason!=SupportRejectionReason.None||receipts[0].SpentFuel!=1||receipts[0].Source!=SupportRequestSource.Player||model.Smoke.Charges!=1)
                    throw new InvalidOperationException("Native Smoke must spend one Fuel and one charge exactly once");
                var reserve=em.GetComponentData<CampaignMissionSplitFrontFuelState>(root);var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve.FuelReserve);
                if(storage.StoredFuelBarrels!=43||storage.CivilianFuelReserveBarrels!=40)throw new InvalidOperationException("Smoke touched protected civilian Fuel");
                committedAt=em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds;Shot("support-smoke-live");smokeStage=5;
            }
            if(smokeStage==5)
            {
                using var covered=em.CreateEntityQuery(typeof(SupportRangedCoverComponent));using var values=covered.ToComponentDataArray<SupportRangedCoverComponent>(Allocator.Temp);
                foreach(var cover in values)smokeCover|=cover.DirectDamagePermille==650;
                if(em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds-committedAt<16)return false;
                smokeExpiry=zones.CalculateEntityCount()==0;if(!smokeCover||!smokeExpiry)throw new InvalidOperationException("Missing native Smoke coverage or expiry");
                Debug.Log("[SplitFrontOptionalSmoke] result=Passed productionContext=true testGrant=none input=touch previewSpend=0 Fuel=1 charges=1 civilianFloor=40 cover=650 expiry=observed");
                // Keep the same player touch device through the following launcher
                // lesson. Dispose only after its final observed cancellation.
                smokeStage=6;return true;
            }
            return false;
        }
        private static bool SmokeTapNamed(string name)=>SmokeTap(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(x=>x.name==name&&x.IsInteractable()));
        private static bool SmokeTapUnder<T>(string name)where T:Component
        {var owner=UnityEngine.Object.FindAnyObjectByType<T>();return owner!=null&&SmokeTap(owner.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name==name));}
        private static bool SmokeTap(Button button)
        {
            if(button==null||!button.IsActive()||!button.IsInteractable())return false;var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            return hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button&&SmokeGesture(point);
        }
        private static bool SmokeGesture(Vector2 point)
        {if(!smokeHand.TryGesture(point,point,.12f,0,Time.unscaledTime))return false;nextSmokeAction=EditorApplication.timeSinceStartup+.8;return true;}
    }
}
