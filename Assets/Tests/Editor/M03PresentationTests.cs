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
            tests.AllTwelveM04AriaLessonsFitBothLanguagesAndTextSizes();
            tests.FinalComicCropsHaveStableMissionIdentityAndAspect();
            Debug.Log("[M03PresentationValidation] result=Passed ariaPresentations=192 finalComicCrops=14");
            ValidationExit.Passed();
        }
        catch(Exception exception)
        {Debug.LogException(exception); Debug.LogError("[M03PresentationValidation] result=Failed"); ValidationExit.Failed();}
    }

    [Test]
    public void AllTwelveAriaLessonsFitBothLanguagesAndTextSizes() => ValidateAriaLessons(false);

    [Test] public void AllTwelveM04AriaLessonsFitBothLanguagesAndTextSizes() => ValidateAriaLessons(true);

    private static void ValidateAriaLessons(bool extraction)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        Assert.NotNull(prefab);
        var canvasRoot=new GameObject("ARIA layout canvas",typeof(RectTransform),typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        ((RectTransform)canvasRoot.transform).sizeDelta=new Vector2(1920,1080);
        var instance=UnityEngine.Object.Instantiate(prefab,canvasRoot.transform);
        var previousLocale=GameLocalization.CurrentLocaleCode;
        GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath),previousLocale,false);
        try
        {
            var view=instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
            instance.GetComponentInChildren<MissionHudTouchLayoutView>(true).Apply(true);
            Assert.NotNull(view);
            Assert.IsTrue(view.TryBindHierarchy());
            var utility=(RectTransform)view.transform.Find(extraction ? "M04Navigation" : "M03Actions");
            Assert.NotNull(utility);
            view.transform.Find(extraction ? "M03Actions" : "M04Navigation").gameObject.SetActive(false);
            utility.gameObject.SetActive(true);
            foreach(int width in new[]{1920,2400})
            foreach(bool persian in new[]{false,true})
            foreach(bool large in new[]{false,true})
            for(byte step=1;step<=12;step++)
            {
                ((RectTransform)canvasRoot.transform).sizeDelta=new Vector2(width,1080);
                foreach(var layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true)) layout.RefreshLayout();
                var dock=instance.GetComponentInChildren<MissionHudTouchLayoutView>(true);dock.RefreshLayout();
                GameLocalization.SetLocale(persian ? "fa-IR" : "en", false);
                utility.gameObject.SetActive(true);
                if(extraction)
                {
                    utility.Find("Team").gameObject.SetActive(step<=5);
                    utility.Find("Landing").gameObject.SetActive(step>5 && step<=10);
                    utility.Find("Departure").gameObject.SetActive(step>10);
                }
                var copy=M03RadarWarningTutorialCopyCatalog.Steps[step-1];
                var rescue=M04AirliftCopyCatalog.Lessons[step-1];
                string title=extraction ? (persian ? rescue.PersianTitle : rescue.Title) : (persian ? copy.PersianTitle : copy.Title);
                string body=extraction ? (persian ? rescue.PersianBody : rescue.Body) : (persian ? copy.PersianBody : copy.Body);
                var model=new UiAssistantPanelModel(1,true,0,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,
                    UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,
                    UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantTargetLockModel.Empty,UiAssistantNarrationModel.Empty,true,title,
                    body,"",persian ? "ادامه" : "CONTINUE",true,true,false,false,"","",
                    largeTextEnabled:large,tutorialStep:step,tutorialStepCount:12,tutorialRightToLeft:persian);
                view.Apply(model);
                view.ApplyAccessibility(large,false);
                view.SetPresentationVisible(true);
                if(!persian) StringAssert.Contains("STEP "+step+"/12",view.ProgressText.text);
                Canvas.ForceUpdateCanvases();
                foreach(var text in new[]{view.TitleText,view.BodyText,view.ProgressText,view.ShowMeButton.GetComponentInChildren<TMP_Text>(),view.ContinueButton.GetComponentInChildren<TMP_Text>()})
                {
                    if(text==null || !text.gameObject.activeInHierarchy) continue;
                    text.ForceMeshUpdate(true,true);
                    Assert.IsFalse(text.isTextOverflowing || text.isTextTruncated,
                        $"ARIA M{(extraction ? 4 : 3)} {step} {(persian ? "fa-IR" : "en")} large={large}: {text.name} height={text.rectTransform.rect.height} preferred={text.preferredHeight}");
                    Assert.IsTrue(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c=>c.isVisible),$"Visible text missing: {text.name}, step={step}, fa={persian}, text={text.text}");
                    Assert.IsFalse(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c=>c.character=='\u25a1' || c.character=='\ufffd'));
                }
                view.RefreshContentLayout();
                Assert.IsFalse(view.DoItButton.gameObject.activeSelf,"Do It stays hidden; only reading acknowledgements retain Continue.");
                var actions=BoundsIn(view.transform,(RectTransform)view.ContinueButton.transform);
                var utilities=BoundsIn(view.transform,utility);
                var bodyBounds=BoundsIn(view.transform,(RectTransform)view.BodyText.transform.parent);
                var rail=((RectTransform)view.transform).rect;
                Assert.LessOrEqual(utilities.max.y,actions.min.y-11.99f,"Utility row must stay below primary actions.");
                Assert.LessOrEqual(actions.max.y,bodyBounds.min.y-43.99f,"Buttons must not cut instruction text.");
                Assert.GreaterOrEqual(utilities.min.y,rail.yMin+11.99f,"Utility row must fit inside ARIA.");
                Assert.GreaterOrEqual(utilities.min.x,rail.xMin+19.99f);
                Assert.LessOrEqual(utilities.max.x,rail.xMax-19.99f);
                Assert.GreaterOrEqual(rail.yMin,BoundsIn(view.transform,dock.Minimap).max.y+11.99f,"ARIA must clear the minimap.");
                float previousBottom=utilities.max.y+12;
                foreach(var button in utility.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    var rect=(RectTransform)button.transform;
                    Assert.GreaterOrEqual(rect.rect.height,72,"Keep utility touch targets large.");
                    var primary=view.ContinueButton;
                    var buttonBounds=BoundsIn(view.transform,rect);
                    var primaryBounds=BoundsIn(view.transform,(RectTransform)primary.transform);
                    Assert.That(buttonBounds.max.y,Is.LessThanOrEqualTo(previousBottom-11.99f),"Buttons must stack with a touch-safe gap.");
                    previousBottom=buttonBounds.min.y;
                    Assert.That(buttonBounds.min.x,Is.EqualTo(primaryBounds.min.x).Within(.01f),"All buttons must share the full-width left edge.");
                    Assert.That(buttonBounds.size.x,Is.EqualTo(primaryBounds.size.x).Within(.01f),"All buttons must span the same panel width.");
                }
                Assert.IsTrue(view.FirstStepGuideRoot==null || !view.FirstStepGuideRoot.gameObject.activeSelf,"M3 must not display M1's selection-only diagram.");
            }
            GameLocalization.SetLocale(previousLocale, false);
            view.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel());
            view.RefreshContentLayout();
            Assert.Greater(view.BriefingLayout.rect.height,0,"M1 instructions remain content-sized after M3.");
        }
        finally {UnityEngine.Object.DestroyImmediate(canvasRoot);GameLocalization.SetLocale(previousLocale,false);}
    }

    private static Bounds BoundsIn(Transform parent,RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        var bounds=new Bounds(parent.InverseTransformPoint(corners[0]),Vector3.zero);
        for(int i=1;i<4;i++) bounds.Encapsulate(parent.InverseTransformPoint(corners[i]));
        return bounds;
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
