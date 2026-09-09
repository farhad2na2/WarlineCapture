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
        var instance=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var view=instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
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
                Assert.IsTrue(view.FirstStepGuideRoot==null || !view.FirstStepGuideRoot.gameObject.activeSelf,"M3 must not display M1's selection-only diagram.");
            }
            view.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel());
            Assert.AreEqual(originalSize,view.BriefingLayout.sizeDelta,"M1 card size must restore after M3.");
            Assert.AreEqual(originalPosition,view.BriefingLayout.anchoredPosition,"M1 card position must restore after M3.");
        }
        finally {UnityEngine.Object.DestroyImmediate(instance);}
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
}
