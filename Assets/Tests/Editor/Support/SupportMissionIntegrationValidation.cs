#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportMissionIntegrationValidation
    {
        public static void RunFocusedValidation()
        {
            try{var t=new SupportMissionIntegrationValidation();t.AuthoredContextProjectsAndMissingContextClears();t.AmbiguousOrWrongMissionFailsClosed();t.LessonScheduleIsOptionalAndOrdered();t.ContextRejectsUnknownOrNonfiniteData();Debug.Log("[SupportMissionIntegrationValidation] result=Passed tests=4 future-missions=pending");ValidationExit.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportMissionIntegrationValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        private static Entity Context(SupportValidationFixture f,string id="saga.ch04.m03.split_front")
        {
            var e=f.Em.CreateEntity(typeof(SupportMissionContextComponent));f.Em.SetComponentData(e,new SupportMissionContextComponent {MissionId=id,OperationMapId="map-test",MissionSourceVersion=7,Revision=2,GroundMin=new float2(-20),GroundMax=new float2(20),PopulationCeiling=20,Entry=new float3(-30,16,0),Release=new float3(0,16,0),Exit=new float3(30,16,0),Clearance=5,RouteAuthored=1});
            f.Em.AddBuffer<SupportAuthoredGroundRegionElement>(e).Add(new SupportAuthoredGroundRegionElement {Min=new float2(-10),Max=new float2(10),Visible=1});return e;
        }
        private static void Root(SupportValidationFixture f)
        {
            var s=f.Em.GetComponentData<SupportSessionComponent>(f.Root);s.MissionSourceVersion=7;s.TestEncounter=0;f.Em.SetComponentData(f.Root,s);f.Em.AddComponent<SupportMissionContextStampComponent>(f.Root);f.Em.AddComponent<SupportAirRouteComponent>(f.Root);
            f.Em.AddComponentData(f.Root,new CampaignMissionRuntimeComponent {MissionId="saga.ch04.m03.split_front",OperationMapId="map-test",SourceVersion=7,SessionToken=s.SessionToken,AttemptOrdinal=s.AttemptOrdinal});
        }
        [Test] public void AuthoredContextProjectsAndMissingContextClears()
        {
            using var f=new SupportValidationFixture();Root(f);var context=Context(f);SupportMissionContextProjectionSystem.Apply(f.Em,f.Root,context);
            Assert.AreEqual(20,f.Em.GetComponentData<SupportMissionPolicyComponent>(f.Root).PopulationCeiling);Assert.AreEqual(SupportRejectionReason.None,SupportTargetValidationUtilitySystemHelper.ValidateGround(f.Em,f.Root,float3.zero));Assert.AreEqual(1,f.Em.GetComponentData<SupportAirRouteComponent>(f.Root).Authored);
            SupportMissionContextProjectionSystem.Apply(f.Em,f.Root,Entity.Null);Assert.AreEqual(0,f.Em.GetBuffer<SupportGroundRegionElement>(f.Root).Length);Assert.AreEqual(0,f.Em.GetComponentData<SupportAirRouteComponent>(f.Root).Authored);Assert.AreEqual(0,f.Em.GetComponentData<SupportMissionPolicyComponent>(f.Root).PopulationCeiling);
            SupportMissionContextProjectionSystem.Apply(f.Em,f.Root,context);var s=f.Em.GetComponentData<SupportSessionComponent>(f.Root);s.AttemptOrdinal++;f.Em.SetComponentData(f.Root,s);SupportMissionContextProjectionSystem.Apply(f.Em,f.Root,context);Assert.AreEqual(s.AttemptOrdinal,f.Em.GetComponentData<SupportAirRouteComponent>(f.Root).AttemptOrdinal);
        }
        [Test] public void AmbiguousOrWrongMissionFailsClosed()
        {
            using var f=new SupportValidationFixture();Root(f);Context(f,"saga.ch04.m04.grounded_signal");var h=f.World.GetOrCreateSystem<SupportMissionContextProjectionSystem>();h.Update(f.World.Unmanaged);Assert.AreEqual(0,f.Em.GetBuffer<SupportGroundRegionElement>(f.Root).Length);
            Context(f);h.Update(f.World.Unmanaged);Assert.AreEqual(1,f.Em.GetBuffer<SupportGroundRegionElement>(f.Root).Length);Context(f);h.Update(f.World.Unmanaged);Assert.AreEqual(0,f.Em.GetBuffer<SupportGroundRegionElement>(f.Root).Length);
        }
        [Test] public void LessonScheduleIsOptionalAndOrdered()
        {
            var policies=UnityEditor.AssetDatabase.LoadAssetAtPath<SupportMissionPolicyConfig>(Game.Editor.SupportAbilityCatalogBuilder.PoliciesPath);
            string[] ids={"CH04-M03","CH04-M04","CH04-M05","CH05-M03"};for(int i=0;i<4;i++){Assert.IsTrue(policies.TryResolve(ids[i],out var policy));Assert.AreEqual((SupportAbilityKind)(i+1),policy.LessonKind);}
            Assert.IsTrue(policies.TryResolve("CH04-M02",out var hidden));Assert.AreEqual(SupportAbilityKind.None,hidden.LessonKind);Assert.AreEqual(0,hidden.AllowedMask);
        }
        [Test] public void ContextRejectsUnknownOrNonfiniteData()
        {
            var c=ScriptableObject.CreateInstance<SupportMissionContextConfig>();try{Assert.IsFalse(c.TryValidate(out _));c.MissionId="saga.ch04.m03.split_front";c.OperationMapId="map-test";c.MissionSourceVersion=7;c.GroundMin=new Vector2(-20,-20);c.GroundMax=new Vector2(20,20);c.Regions=new[]{new SupportAuthoredGroundRegion {Min=new Vector2(-10,-10),Max=new Vector2(10,10),Visible=true}};Assert.IsTrue(c.TryValidate(out _));c.GroundMax=new Vector2(float.NaN,20);Assert.IsFalse(c.TryValidate(out _));}finally{UnityEngine.Object.DestroyImmediate(c);}
        }
    }
}
#endif
