using System;
using Game.Catalog.Contracts;
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class FirstLaunchNarrativePresentationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativePresentationTests tests = new();
            tests.RevealSchedule_IsMonotonicPunctuationAwareAndDeadlineBound();
            tests.RevealSchedule_HandlesRichTextMalformedAndInstantInputs();
            tests.SubtitleStyleResolver_MapsAllAccessibilityPresets();
            tests.DialogueAssets_UseSeparatePointerProductionAriaIconAndNineSliceBorder();
            tests.PresentationPrefab_HasBoundViewsSkipAndDedicatedVoiceSource();
            tests.PresentationHelper_RespectsAutoAdvancePauseAndCancel();
            Debug.Log("[FirstLaunchNarrativePresentationValidation] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativePresentationValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void SubtitleStyleResolver_MapsAllAccessibilityPresets()
    {
        UISettingsModel model = Game.UI.Runtime.SettingsService.Defaults;
        Assert.IsTrue(NarrativeSubtitleStyleResolver.Resolve(model).Visible);
        Assert.AreEqual(30f, NarrativeSubtitleStyleResolver.Resolve(model).FontSize);
        Assert.AreEqual(0.75f, NarrativeSubtitleStyleResolver.Resolve(model).BackgroundOpacity);

        model.Narrative.SubtitleSize = UISubtitleSize.ExtraLarge;
        model.Narrative.BackgroundOpacity = UISubtitleBackgroundOpacity.ZeroPercent;
        model.Narrative.InstantText = true;
        model.Narrative.SubtitlesEnabled = false;
        model.Accessibility.ReducedMotion = true;
        NarrativeSubtitleStyle style = NarrativeSubtitleStyleResolver.Resolve(model);
        Assert.IsFalse(style.Visible);
        Assert.AreEqual(44f, style.FontSize);
        Assert.AreEqual(0f, style.BackgroundOpacity);
        Assert.IsTrue(style.InstantText);
        Assert.IsTrue(style.ReducedMotion);
    }

    [Test]
    public void RevealSchedule_IsMonotonicPunctuationAwareAndDeadlineBound()
    {
        NarrativeDialogueReveal plain = Build("Alpha beta gamma", 10f);
        NarrativeDialogueReveal punctuated = Build("Alpha, beta... gamma!", 10f);

        Assert.Greater(punctuated.Duration, plain.Duration);
        Assert.LessOrEqual(punctuated.Duration, 10f);
        int previous = 0;
        for (float time = 0f; time <= punctuated.Duration + 0.1f; time += 0.025f)
        {
            int visible = punctuated.GetVisibleCharacterCount(time);
            Assert.GreaterOrEqual(visible, previous);
            Assert.LessOrEqual(visible, punctuated.VisibleCharacterCount);
            previous = visible;
        }

        NarrativeDialogueReveal compressed = Build("One, two; three: four. Five? Six!", 0.25f);
        Assert.AreEqual(0.25f, compressed.Duration, 0.0001f);
        Assert.AreEqual(compressed.VisibleCharacterCount, compressed.GetVisibleCharacterCount(0.25f));
    }

    [Test]
    public void RevealSchedule_HandlesRichTextMalformedAndInstantInputs()
    {
        Assert.DoesNotThrow(() => Build("<b>ARIA</b>: relay ready.", 3f));
        Assert.DoesNotThrow(() => Build("Broken <tag remains visible", 3f));
        Assert.DoesNotThrow(() => Build("e\u0301", 1f));
        Assert.DoesNotThrow(() => Build(string.Empty, 1f));
        Assert.DoesNotThrow(() => Build("   ", 1f));

        NarrativeDialogueReveal instant = Build("Immediate", 3f, true);
        Assert.AreEqual(0f, instant.Duration);
        Assert.AreEqual(instant.VisibleCharacterCount, instant.GetVisibleCharacterCount(0f));
    }

    [Test]
    public void DialogueAssets_UseSeparatePointerProductionAriaIconAndNineSliceBorder()
    {
        FirstLaunchNarrativeDialogueAssetImporter.Configure();
        Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.FramePath);
        Sprite pointer = AssetDatabase.LoadAssetAtPath<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.PointerPath);
        Sprite aria = AssetDatabase.LoadAssetAtPath<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.AriaIconPath);

        Assert.NotNull(frame);
        Assert.NotNull(pointer);
        Assert.NotNull(aria);
        Assert.AreNotEqual(AssetDatabase.AssetPathToGUID(FirstLaunchNarrativeDialogueAssetImporter.FramePath),
            AssetDatabase.AssetPathToGUID(FirstLaunchNarrativeDialogueAssetImporter.PointerPath));
        Assert.AreEqual("Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_focus_reticle.png",
            AssetDatabase.GetAssetPath(aria));
        Assert.Greater(frame.border.x, 0f);
        Assert.Greater(frame.border.y, 0f);
        Assert.Greater(frame.border.z, 0f);
        Assert.Greater(frame.border.w, 0f);
    }

    [Test]
    public void PresentationPrefab_HasBoundViewsSkipAndDedicatedVoiceSource()
    {
        FirstLaunchNarrativePresentationPrefabBuilder.Build();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        Assert.NotNull(prefab.GetComponent<NarrativeSequenceView>());
        Assert.NotNull(prefab.GetComponent<AudioSource>());
        Assert.IsFalse(prefab.GetComponent<AudioSource>().playOnAwake);
        Assert.NotNull(prefab.GetComponentInChildren<NarrativeDialogueView>(true));
        Assert.NotNull(prefab.GetComponentInChildren<NarrativePlaybackControlsView>(true));
        Assert.NotNull(prefab.GetComponentInChildren<NarrativeReviewerControlsView>(true));
        Assert.NotNull(prefab.GetComponent<NarrativeSequenceView>().PanelMotionRoot);
        AspectRatioFitter panelFitter = prefab.transform.Find("Panel")?.GetComponent<AspectRatioFitter>();
        Assert.NotNull(panelFitter, "Full-screen narrative panels require aspect-envelope fitting on non-16:9 devices.");
        Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, panelFitter.aspectMode);
        Image frame = Array.Find(prefab.GetComponentsInChildren<Image>(true), image => image.name == "Frame");
        Image pointer = Array.Find(prefab.GetComponentsInChildren<Image>(true), image => image.name == "Pointer");
        Assert.NotNull(frame);
        Assert.NotNull(pointer);
        Assert.AreEqual(Image.Type.Sliced, frame.type);
        Assert.AreNotSame(frame.sprite, pointer.sprite);
    }

    [Test]
    public void PresentationHelper_RespectsAutoAdvancePauseAndCancel()
    {
        FirstLaunchNarrativePresentationPrefabBuilder.Build();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
        NarrativePunctuationPresentationModel punctuation = new()
        {
            CharactersPerSecond = 28f,
            CommaPauseSeconds = 0.11f,
            ClausePauseSeconds = 0.16f,
            SentencePauseSeconds = 0.24f,
            EllipsisPauseSeconds = 0.32f,
            TailHoldSeconds = 0.25f
        };
        NarrativeSequencePresentation helper = new(view);
        UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
        NarrativeSpeakerPresentationModel speaker = new()
        {
            DisplayName = "ARIA",
            Role = "CIVIC RELAY ASSISTANT",
            Treatment = NarrativeSpeakerTreatment.AriaIcon,
            AccentColor = Color.cyan
        };

        helper.StartDialogue("Relay ready.", speaker, null, 1f, punctuation, settings);
        helper.Pause();
        helper.Tick(10f);
        Assert.IsFalse(helper.IsAdvanceReady);
        helper.Resume();
        helper.Tick(2f);
        Assert.IsTrue(helper.IsAdvanceReady);
        helper.Tick(1f);
        Assert.IsTrue(helper.ConsumeAutoAdvanceRequest());
        Assert.IsFalse(helper.ConsumeAutoAdvanceRequest());

        settings.Narrative.AutoAdvance = false;
        helper.StartDialogue("Hold for input.", speaker, null, 1f, punctuation, settings);
        helper.Tick(10f);
        Assert.IsTrue(helper.IsAdvanceReady);
        Assert.IsFalse(helper.ConsumeAutoAdvanceRequest());
        helper.Cancel();
        Assert.AreEqual(NarrativeDialoguePhase.Hidden, view.DialogueView.Phase);

        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static NarrativeDialogueReveal Build(string text, float deadline, bool instant = false)
    {
        NarrativeDialogueReveal helper = new();
        helper.Prepare(text, deadline, 28f, 0.11f, 0.16f, 0.24f, 0.32f, instant);
        return helper;
    }
}
