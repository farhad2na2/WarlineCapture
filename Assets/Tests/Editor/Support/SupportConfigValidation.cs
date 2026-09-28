#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Runtime;
namespace Game.Tests.Editor
{
    public sealed class SupportConfigValidation
    {
        public static void RunFocusedValidation()
        {
            try{SupportPopupPrefabBuilder.Build();var tests=new SupportConfigValidation();tests.CatalogRejectsMalformedDataAndPoliciesRetainUnlockOrder();tests.NativeTargetingLeavesBattlefieldUnobscured();
                Debug.Log("[SupportConfigValidation] result=Passed tests=2 catalog=4 policies=25");ValidationExit.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportConfigValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        [Test] public void CatalogRejectsMalformedDataAndPoliciesRetainUnlockOrder()
        {
            var source=AssetDatabase.LoadAssetAtPath<SupportAbilityCatalogConfig>(SupportAbilityCatalogBuilder.CatalogPath);Assert.IsTrue(source.TryValidate(out var error),error);
            var clone=UnityEngine.Object.Instantiate(source);
            try
            {
                var entries=(SupportAbilityConfig[])clone.Abilities.Clone();clone.Abilities=entries;
                foreach(var a in source.Abilities)Assert.IsFalse(a.ProductionReady,"Acceptance cannot be inferred from import.");
                clone.Abilities[0].Radius=float.NaN;Assert.IsFalse(clone.TryValidate(out _));clone.Abilities[0]=source.Abilities[0];
                clone.Abilities[1].Kind=SupportAbilityKind.Smoke;Assert.IsFalse(clone.TryValidate(out _));clone.Abilities[1]=source.Abilities[1];
                clone.Abilities[0].Icon=null;Assert.IsFalse(clone.TryValidate(out _));
                var policies=AssetDatabase.LoadAssetAtPath<SupportMissionPolicyConfig>(SupportAbilityCatalogBuilder.PoliciesPath);
                Assert.AreEqual(25,policies.Missions.Length);Assert.IsTrue(policies.TryResolve("saga.ch04.m02.steel_push",out var before));Assert.AreEqual(0,before.AllowedMask);
                Assert.IsTrue(policies.TryResolve("saga.ch04.m03.split_front",out var smoke));Assert.AreEqual(1,smoke.AllowedMask);
                Assert.IsTrue(policies.TryResolve("CH04-M04",out var strike));Assert.AreEqual(3,strike.AllowedMask);
                Assert.IsTrue(policies.TryResolve("CH04-M05",out var para));Assert.AreEqual(7,para.AllowedMask);
                Assert.IsTrue(policies.TryResolve("CH05-M03",out var supply));Assert.AreEqual(15,supply.AllowedMask);
                Assert.IsFalse(policies.TryResolve("unknown",out _));
                using var sha=System.Security.Cryptography.SHA256.Create();var hash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText("Design/BalanceConfigs/Combat_Balance_Config_v0_1.json")))).Replace("-","").ToLowerInvariant();Assert.AreEqual(hash,source.SourceHash);
            }
            finally{UnityEngine.Object.DestroyImmediate(clone);}
        }
        [Test] public void NativeTargetingLeavesBattlefieldUnobscured()
        {
            var popup=AssetDatabase.LoadAssetAtPath<GameObject>(SupportPopupPrefabBuilder.PopupPath);
            Assert.IsNotNull(popup.GetComponent<Canvas>());Assert.IsNotNull(popup.GetComponent<GraphicRaycaster>());
            var parent=new GameObject("SupportLayoutCheck",typeof(RectTransform),typeof(Canvas));GameObject instance=null;
            try
            {
                parent.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
                foreach(var resolution in new[]{new Vector2(1920,1080),new Vector2(2400,1080)})
                {
                    parent.GetComponent<RectTransform>().sizeDelta=resolution;
                    instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SupportPopupPrefabBuilder.TargetingPath),parent.transform,false);
                    Assert.IsNotNull(instance.GetComponent<GraphicRaycaster>());
                    foreach(var layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>())layout.RefreshLayout();
                    Canvas.ForceUpdateCanvases();var bar=instance.transform.Find("PreviewBar") as RectTransform;var corners=new Vector3[4];bar.GetWorldCorners(corners);
                    float height=corners[1].y-corners[0].y;
                    Assert.LessOrEqual(height,resolution.y*.3f,"Targeting toolbar must leave the battlefield available to pointer input.");
                    Assert.LessOrEqual(corners[1].y,-resolution.y*.2f,"Toolbar stays in the bottom strip.");
                    UnityEngine.Object.DestroyImmediate(instance);instance=null;
                }
            }
            finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(parent);}
        }
    }
}
#endif
