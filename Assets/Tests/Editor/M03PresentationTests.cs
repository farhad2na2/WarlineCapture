using System;
using System.Linq;
using Game.Configs;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class M03PresentationTests
{
    [Test]
    public void DeliveredHudBindsAriaWithOpeningInstructionsInstalled()
    {
        var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"));
        var popupLayer=new GameObject("Popup layer",typeof(RectTransform));
        var helperType=typeof(AriaTutorialBriefingView).Assembly.GetType("Game.UI.Runtime.MatchHudAssistantUiSystemHelper");
        var helper=Activator.CreateInstance(helperType,true);
        try
        {
            var aria=instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
            Assert.NotNull(aria.transform.Find("OpeningInstruction/OpeningHint"));
            var header=aria.transform.parent.gameObject;
            var popup=AssetDatabase.LoadAssetAtPath<GameObject>(AriaCommandAssistantV3PrefabBuilder.PrefabPath);
            helperType.GetMethod("Bind").Invoke(helper,new object[]{header,popupLayer.transform,popup,null,null,null});
            Assert.IsTrue((bool)helperType.GetProperty("IsBound").GetValue(helper),
                "A valid tutorial projection is useless if the delivered HUD cannot bind ARIA.");
        }
        finally
        {
            helperType.GetMethod("Unbind").Invoke(helper,null);
            UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(popupLayer);
        }
    }

    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M03PresentationTests();
            tests.AllTwelveAriaLessonsFitBothLanguagesAndTextSizes();
            tests.FinalComicCropsHaveStableMissionIdentityAndAspect();
            Debug.Log("[M03PresentationValidation] result=Passed ariaPresentations=48 finalComicCrops=14");
            ValidationExit.Passed();
        }
        catch(Exception exception)
        {Debug.LogException(exception); Debug.LogError("[M03PresentationValidation] result=Failed"); ValidationExit.Failed();}
    }

    [Test]
    public void AllTwelveAriaLessonsFitBothLanguagesAndTextSizes()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        Assert.NotNull(prefab);
        var canvasRoot=new GameObject("ARIA layout canvas",typeof(RectTransform),typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        ((RectTransform)canvasRoot.transform).sizeDelta=new Vector2(1920,1080);
        var instance=UnityEngine.Object.Instantiate(prefab,canvasRoot.transform);
        try
        {
            var view=instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
            instance.GetComponentInChildren<MissionHudTouchLayoutView>(true).Apply(true);
            Assert.NotNull(view);
            Assert.IsTrue(view.TryBindHierarchy());
            var originalSize=view.BriefingLayout.sizeDelta;
            var originalPosition=view.BriefingLayout.anchoredPosition;
            foreach(bool persian in new[]{false,true})
            foreach(bool large in new[]{false,true})
            for(byte step=1;step<=12;step++)
            {
                var copy=M03RadarWarningTutorialCopyCatalog.Steps[step-1];
                var model=new UiAssistantPanelModel(1,true,0,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,
                    UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,
                    UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantTargetLockModel.Empty,UiAssistantNarrationModel.Empty,true,persian ? copy.PersianTitle : copy.Title,
                    persian ? copy.PersianBody : copy.Body,"",persian ? "ادامه" : "CONTINUE",true,true,false,false,"","",
                    largeTextEnabled:large,tutorialStep:step,tutorialStepCount:12,tutorialRightToLeft:persian);
                view.Apply(model);
                view.ApplyAccessibility(large,false);
                view.SetPresentationVisible(true);
                if(!persian) StringAssert.Contains("STEP "+step+"/12",view.ProgressText.text);
                Canvas.ForceUpdateCanvases();
                foreach(var text in new[]{view.TitleText,view.BodyText,view.ProgressText,view.ShowMeButton.GetComponentInChildren<TMP_Text>(),view.DoItButton.GetComponentInChildren<TMP_Text>()})
                {
                    text.ForceMeshUpdate(true,true);
                    Assert.IsFalse(text.isTextOverflowing || text.isTextTruncated,
                        $"ARIA {step} {(persian ? "fa-IR" : "en")} large={large}: {text.name} height={text.rectTransform.rect.height} preferred={text.preferredHeight}");
                    Assert.IsTrue(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c=>c.isVisible));
                    Assert.IsFalse(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c=>c.character=='\u25a1' || c.character=='\ufffd'));
                }
                var utility=view.transform.Find("M03Actions") as RectTransform;
                Assert.NotNull(utility);
                utility.gameObject.SetActive(true);
                var corners=new Vector3[4];var utilityCorners=new Vector3[4];
                ((RectTransform)view.DoItButton.transform).GetWorldCorners(corners);utility.GetWorldCorners(utilityCorners);
                Assert.Less(utilityCorners[1].y,corners[0].y,"Guide controls must sit below ARIA's primary actions.");
                Assert.IsTrue(view.FirstStepGuideRoot==null || !view.FirstStepGuideRoot.gameObject.activeSelf,"M3 must not display M1's selection-only diagram.");
            }
            view.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel());
            Assert.AreEqual(originalSize,view.BriefingLayout.sizeDelta,"M1 card size must restore after M3.");
            Assert.AreEqual(originalPosition,view.BriefingLayout.anchoredPosition,"M1 card position must restore after M3.");
        }
        finally {UnityEngine.Object.DestroyImmediate(canvasRoot);}
    }

    [Test]
    public void RebuildingTheSharedDefenseGuidePreservesTheExtractionCatalog()
    {
        typeof(M03RadarWarningUiBuilder).GetMethod("BuildGuide",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
            .Invoke(null,null);
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(M03RadarWarningUiBuilder.GuidePath);
        var data=new SerializedObject(root.GetComponent<MissionFieldGuideView>());
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path),
            data.FindProperty("guide").objectReferenceValue);
        var extraction=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M04AirliftPresentationBuilder.GuidePath);
        Assert.NotNull(extraction);
        Assert.AreSame(extraction,data.FindProperty("extractionGuide").objectReferenceValue,
            "Rebuilding M3 must preserve M4's guide route in the shared prefab.");
    }

    [Test]
    public void WarningDetailsAndOptionalActionsFitTheHudAtBothAspects()
    {
        var canvasRoot=new GameObject("M3 HUD layout QA",typeof(RectTransform),typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        string oldLocale=GameLocalization.CurrentLocaleCode;
        try
        {
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"),canvasRoot.transform);
            var header=instance.transform.Find("V3Composition/HeaderContent");
            var actions=(RectTransform)header.Find("AriaAssistantButton/M03Actions");
            var aria=(RectTransform)header.Find("AriaAssistantButton");
            var title=header.Find("ThreatJumpPanel").GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Title");
            actions.gameObject.SetActive(true);
            foreach(int width in new[]{1920,2400})
            {
                ((RectTransform)canvasRoot.transform).sizeDelta=new Vector2(width,1080);
                foreach(var layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true)) layout.RefreshLayout();
                var actionCorners=new Vector3[4]; var ariaCorners=new Vector3[4];
                actions.GetWorldCorners(actionCorners); aria.GetWorldCorners(ariaCorners);
                Assert.GreaterOrEqual(actionCorners[0].x,ariaCorners[0].x,"Guide actions belong inside ARIA.");
                Assert.LessOrEqual(actionCorners[2].x,ariaCorners[2].x,"Guide actions belong inside ARIA.");
                Assert.IsNull(header.Find("M03Actions"),"No extra panel may cover the battlefield below the warning.");
                Assert.NotNull(header.Find("ThreatJumpPanel/ReadWarning").GetComponent<UnityEngine.UI.Button>(),"Read details through the alert itself.");
                Assert.NotNull(header.Find("ThreatJumpPanel").GetComponent<MatchHudThreatVisibilityView>());
                foreach(bool persian in new[]{false,true})
                {
                    GameLocalization.SetLocale(persian ? "fa-IR" : "en",false);
                    string copy=persian
                        ? "گزارش دیده‌بان · کاروان اصلی · جادهٔ غربی\nترکیب نیروها تأیید نشده · برآورد زمان تماس: 165 ثانیه"
                        : "Scout report · Main convoy · western road\nComposition unconfirmed · Contact estimate: 165s";
                    title.GetComponent<V3LocalizedTextBindingView>().SetLocalizedValue(copy);
                    title.ForceMeshUpdate(true,true);
                    Assert.IsFalse(title.isTextOverflowing || title.isTextTruncated,$"Contact details truncated: width={width} Persian={persian}");
                }
            }
        }
        finally {GameLocalization.SetLocale(oldLocale,false); UnityEngine.Object.DestroyImmediate(canvasRoot);}
    }

    [Test]
    public void ResultCompositionFitsWhenCanvasLogicalSizeChangesWithoutRemounting()
    {
        var root=new GameObject("Result resize QA",typeof(RectTransform),typeof(Canvas));
        root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var canvasRect=(RectTransform)root.transform;
        canvasRect.sizeDelta=new Vector2(1920,1080);
        try
        {
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab"),root.transform);
            var composition=(RectTransform)instance.GetComponentInChildren<MainMenuV3SectionLayoutView>().transform;
            var corners=new Vector3[4];
            // CanvasScaler changes logical height as well as width at a new aspect ratio.
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(2146.625f,965.981f),new Vector2(1920,1080)})
            {
                canvasRect.sizeDelta=size;
                Canvas.ForceUpdateCanvases();
                composition.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var local=canvasRect.InverseTransformPoint(corner);
                    Assert.That(local.x,Is.InRange(canvasRect.rect.xMin-1,canvasRect.rect.xMax+1),"Result horizontal bounds after resize");
                    Assert.That(local.y,Is.InRange(canvasRect.rect.yMin-1,canvasRect.rect.yMax+1),"Result vertical bounds after resize");
                }
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(root);}
    }

    [Test]
    public void FinalComicCropsHaveStableMissionIdentityAndAspect()
    {
        foreach(string panel in new[]{"B01","B02","B03","C01","D01","D02","D03"})
        foreach(bool wide in new[]{false,true})
        {
            var sprite=M03RadarWarningMediaImporter.Panel(panel,wide);
            Assert.NotNull(sprite,panel);
            StringAssert.StartsWith("M03-"+panel,sprite.name);
            Assert.AreEqual(wide ? 20f/9f : 16f/9f,sprite.rect.width/sprite.rect.height,.005f);
            Assert.GreaterOrEqual(sprite.rect.height,700);
        }
    }

    [TestCase("en",1920)]
    [TestCase("fa-IR",1920)]
    [TestCase("en",2400)]
    [TestCase("fa-IR",2400)]
    public void SaveFailureHeadlineFitsTheDeliveredResultHeader(string locale,int width)
    {
        string oldLocale=GameLocalization.CurrentLocaleCode;
        var root=new GameObject("Save result headline QA",typeof(RectTransform),typeof(Canvas));
        root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        ((RectTransform)root.transform).sizeDelta=new Vector2(width,1080);
        try
        {
            GameLocalization.SetLocale(locale,false);
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab"),root.transform);
            var view=instance.GetComponent<MissionResultPopupView>();
            var model=new UiMissionResultPopupModel(1,"saga.ch01.m03.radar_warning",UiMissionResultOutcome.Victory,
                GameText.Get("mission.m03.save.failed"),"Radar Warning",GameText.Get("mission.m03.save.body"),
                3,"03:07","0","7/7",GameText.Get("mission.m03.save.pending"),GameText.Get("mission.m03.save.retry"),true,false,
                defense:new UiMissionDefenseResultDetails(0,false,false,false,false),settlementFailed:true);
            view.Apply(in model);Canvas.ForceUpdateCanvases();
            var title=instance.transform.Find("V3Composition/OutcomeTitlePanel/TitleText").GetComponent<TMP_Text>();
            title.ForceMeshUpdate(true,true);
            Assert.IsFalse(title.isTextTruncated || title.isTextOverflowing,$"Save headline: {locale} {width}");
            Assert.IsTrue(title.textInfo.characterInfo.Take(title.textInfo.characterCount).Any(c=>c.isVisible));
        }
        finally {GameLocalization.SetLocale(oldLocale,false);UnityEngine.Object.DestroyImmediate(root);}
    }
}
