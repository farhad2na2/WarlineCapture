using System;
using Game.Catalog.Contracts;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
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
            tests.DialogueAssets_UseSeparatePointerAriaPortraitAndNineSliceBorder();
            tests.PresentationPrefab_HasBoundViewsSkipAndDedicatedVoiceSource();
            tests.V3ComicChrome_ExpandsAcrossUltrawideCanvas();
            tests.InteractiveState_HidesComicChromeAndRestoresIt();
            tests.PresentationHelper_RespectsAutoAdvancePauseAndCancel();
            tests.Phase10RPresentation_UsesReadableTypeMobileTargetsAndCleanFrame();
            tests.Dialogue_LongTextExpandsFrameWithoutEllipsis();
            tests.Phase10RAudio_UsesIndependentSettingsAwareLayersAndCancelsCleanly();
            Debug.Log("[FirstLaunchNarrativePresentationValidation] result=Passed tests=11");
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
        Assert.IsTrue(NarrativeSubtitleStyleUtilitySystemHelper.Resolve(model).Visible);
        Assert.AreEqual(50f, NarrativeSubtitleStyleUtilitySystemHelper.Resolve(model).FontSize);
        Assert.AreEqual(0.75f, NarrativeSubtitleStyleUtilitySystemHelper.Resolve(model).BackgroundOpacity);

        model.Narrative.SubtitleSize = UISubtitleSize.ExtraLarge;
        model.Narrative.BackgroundOpacity = UISubtitleBackgroundOpacity.ZeroPercent;
        model.Narrative.InstantText = true;
        model.Narrative.SubtitlesEnabled = false;
        model.Accessibility.ReducedMotion = true;
        NarrativeSubtitleStyle style = NarrativeSubtitleStyleUtilitySystemHelper.Resolve(model);
        Assert.IsFalse(style.Visible);
        Assert.AreEqual(72f, style.FontSize);
        Assert.AreEqual(0f, style.BackgroundOpacity);
        Assert.IsTrue(style.InstantText);
        Assert.IsTrue(style.ReducedMotion);
    }

    [Test]
    public void RevealSchedule_IsMonotonicPunctuationAwareAndDeadlineBound()
    {
        NarrativeDialogueRevealPresentationSystemHelper plain = Build("Alpha beta gamma", 10f);
        NarrativeDialogueRevealPresentationSystemHelper punctuated = Build("Alpha, beta... gamma!", 10f);

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

        NarrativeDialogueRevealPresentationSystemHelper compressed = Build("One, two; three: four. Five? Six!", 0.25f);
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

        NarrativeDialogueRevealPresentationSystemHelper instant = Build("Immediate", 3f, true);
        Assert.AreEqual(0f, instant.Duration);
        Assert.AreEqual(instant.VisibleCharacterCount, instant.GetVisibleCharacterCount(0f));
    }

    [Test]
    public void DialogueAssets_UseSeparatePointerAriaPortraitAndNineSliceBorder()
    {
        FirstLaunchNarrativeDialogueAssetImporter.Configure();
        Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.FramePath);
        Sprite pointer = AssetDatabase.LoadAssetAtPath<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.PointerPath);
        Sprite aria = AssetDatabase.LoadAssetAtPath<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.AriaPortraitPath);

        Assert.NotNull(frame);
        Assert.NotNull(pointer);
        Assert.NotNull(aria);
        Assert.AreNotEqual(AssetDatabase.AssetPathToGUID(FirstLaunchNarrativeDialogueAssetImporter.FramePath),
            AssetDatabase.AssetPathToGUID(FirstLaunchNarrativeDialogueAssetImporter.PointerPath));
        Assert.AreEqual(FirstLaunchNarrativeDialogueAssetImporter.AriaPortraitPath,
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
        TMP_Text locationSubtitle = Array.Find(
            prefab.GetComponentsInChildren<TMP_Text>(true),
            text => text.name == "DistrictAndTime");
        Assert.NotNull(locationSubtitle);
        Assert.AreEqual("OLD MARKET / 10:00 LOCAL", locationSubtitle.text);
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
    public void V3ComicChrome_ExpandsAcrossUltrawideCanvas()
    {
        FirstLaunchNarrativeV3PrefabBuilder.Build();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);

        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.IsTrue(layout.ExpandToCanvasWidth, "The comic safe frame must expand beyond 16:9 on wide screens.");

        string[] rightTargets = Array.ConvertAll(layout.RightAnchoredTargets, target => target != null ? target.name : string.Empty);
        CollectionAssert.Contains(rightTargets, "PlaybackControls");
        CollectionAssert.Contains(rightTargets, "NextPanel");
        CollectionAssert.Contains(rightTargets, "Pointer");
        CollectionAssert.Contains(rightTargets, "NextLabel");

        SerializedObject serialized = new(layout);
        SerializedProperty expanded = serialized.FindProperty("widthExpandedTargets");
        Assert.NotNull(expanded);
        Assert.GreaterOrEqual(expanded.arraySize, 7);
        bool hasTimeline = false;
        bool hasDialogue = false;
        bool hasDialogueBody = false;
        for (int i = 0; i < expanded.arraySize; i++)
        {
            RectTransform target = expanded.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
            hasTimeline |= target != null && target.name == "ComicTimeline";
            hasDialogue |= target != null && target.name == "Dialogue";
            hasDialogueBody |= target != null && target.name == "DialogueBody";
        }

        Assert.IsTrue(hasTimeline);
        Assert.IsTrue(hasDialogue);
        Assert.IsTrue(hasDialogueBody);

        RectTransform dialogue = prefab.transform.Find("SafeArea/Dialogue") as RectTransform;
        Assert.NotNull(dialogue);
        Assert.GreaterOrEqual(dialogue.sizeDelta.y, 285f, "The dialogue tile must grow upward without opening a bottom gap.");
        TMP_Text speaker = dialogue.Find("SpeakerName")?.GetComponent<TMP_Text>();
        TMP_Text role = dialogue.Find("SpeakerRole")?.GetComponent<TMP_Text>();
        TMP_Text body = dialogue.Find("DialogueText")?.GetComponent<TMP_Text>();
        Assert.NotNull(speaker);
        Assert.NotNull(role);
        Assert.NotNull(body);
        Assert.GreaterOrEqual(speaker.rectTransform.sizeDelta.x, 455f);
        Assert.AreEqual(TextWrappingModes.NoWrap, speaker.textWrappingMode);
        float speakerBottom = -speaker.rectTransform.anchoredPosition.y + speaker.rectTransform.sizeDelta.y;
        float roleBottom = -role.rectTransform.anchoredPosition.y + role.rectTransform.sizeDelta.y;
        float bodyTop = -body.rectTransform.anchoredPosition.y;
        Assert.GreaterOrEqual(bodyTop - Mathf.Max(speakerBottom, roleBottom), 16f);

        Image sharedChevron = dialogue.Find("Pointer/SharedAdvanceIcon")?.GetComponent<Image>();
        Assert.NotNull(sharedChevron);
        Assert.NotNull(sharedChevron.sprite);
        Assert.AreEqual(
            V3UiFoundationBuilder.NavigationChevronIconPath,
            AssetDatabase.GetAssetPath(sharedChevron.sprite));
    }

    [Test]
    public void InteractiveState_HidesComicChromeAndRestoresIt()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            Assert.NotNull(view);

            view.SetInteractiveState(NarrativeInteractiveStateKind.CommanderIdentity);
            Assert.IsFalse(view.LocationIntroView.gameObject.activeSelf);
            Assert.IsFalse(view.PlaybackControlsView.gameObject.activeSelf);
            Assert.IsTrue(view.CommanderIdentityView.gameObject.activeSelf);
            Assert.IsFalse(view.GuidanceChoiceView.gameObject.activeSelf);

            view.SetInteractiveState(NarrativeInteractiveStateKind.None);
            Assert.IsTrue(view.LocationIntroView.gameObject.activeSelf);
            Assert.IsTrue(view.PlaybackControlsView.gameObject.activeSelf);
            Assert.IsFalse(view.CommanderIdentityView.gameObject.activeSelf);
            Assert.IsFalse(view.GuidanceChoiceView.gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
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
        NarrativeDialoguePresentationSystemHelper helper = new(view);
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

    [Test]
    public void Phase10RPresentation_UsesReadableTypeMobileTargetsAndCleanFrame()
    {
        FirstLaunchNarrativePresentationPrefabBuilder.Build();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);

        RectTransform dialogue = prefab.transform.Find("SafeArea/Dialogue") as RectTransform;
        Assert.NotNull(dialogue);
        Assert.AreEqual(292f, dialogue.sizeDelta.y, 0.01f);
        Assert.AreEqual(2.2f, dialogue.localScale.x, 0.01f);
        TMP_Text body = Array.Find(dialogue.GetComponentsInChildren<TMP_Text>(true), text => text.name == "DialogueText");
        TMP_Text speaker = Array.Find(dialogue.GetComponentsInChildren<TMP_Text>(true), text => text.name == "SpeakerName");
        TMP_Text role = Array.Find(dialogue.GetComponentsInChildren<TMP_Text>(true), text => text.name == "SpeakerRole");
        Assert.AreEqual(50f, body.fontSize);
        Assert.IsFalse(body.enableAutoSizing);
        Assert.AreEqual(54f, speaker.fontSize);
        Assert.AreEqual(30f, role.fontSize);
        Assert.IsFalse(dialogue.Find("Pointer").gameObject.activeSelf, "The artifact-producing pointer attachment must remain disabled.");

        RectTransform skip = prefab.transform.Find("SafeArea/PlaybackControls") as RectTransform;
        Assert.GreaterOrEqual(skip.sizeDelta.x, 88f);
        Assert.GreaterOrEqual(skip.sizeDelta.y, 88f);
        Assert.AreEqual(38f, skip.GetComponentInChildren<TMP_Text>(true).fontSize);

        Transform identity = prefab.transform.Find("SafeArea/CommanderIdentitySurface");
        Transform guidance = prefab.transform.Find("SafeArea/GuidanceChoiceSurface");
        Assert.NotNull(identity);
        Assert.NotNull(guidance);
        foreach (Button button in identity.GetComponentsInChildren<Button>(true))
            Assert.GreaterOrEqual(button.GetComponent<RectTransform>().rect.height, 88f, button.name);
        foreach (Button button in guidance.GetComponentsInChildren<Button>(true))
            Assert.GreaterOrEqual(button.GetComponent<RectTransform>().rect.height, 88f, button.name);

        RectTransform reviewer = prefab.transform.Find("SafeArea/DevelopmentReviewerControls") as RectTransform;
        Assert.AreEqual(1f, reviewer.pivot.y, 0.001f);
        NarrativeLocationIntroView location = prefab.GetComponent<NarrativeSequenceView>().LocationIntroView;
        Assert.NotNull(location);
        Assert.AreEqual(0f, location.GetComponent<RectTransform>().pivot.x, 0.001f);
    }

    [Test]
    public void Phase10RAudio_UsesIndependentSettingsAwareLayersAndCancelsCleanly()
    {
        FirstLaunchNarrativePresentationPrefabBuilder.Build();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        NarrativeSequenceAudioView audio = instance.GetComponent<NarrativeSequenceAudioView>();
        Assert.NotNull(audio);
        Assert.AreNotSame(audio.MusicSource, audio.AmbienceSource);
        Assert.AreNotSame(audio.AmbienceSource, audio.VehicleSource);
        Assert.AreNotSame(audio.VehicleSource, audio.EventSource);
        Assert.AreNotSame(prefab.GetComponent<NarrativeSequenceView>().VoiceSource, audio.MusicSource);
        Assert.AreNotSame(prefab.GetComponent<NarrativeSequenceView>().VoiceSource, audio.AmbienceSource);
        Assert.AreNotSame(prefab.GetComponent<NarrativeSequenceView>().VoiceSource, audio.VehicleSource);
        Assert.AreNotSame(prefab.GetComponent<NarrativeSequenceView>().VoiceSource, audio.EventSource);
        Assert.AreEqual(
            "first_launch_radio_emergency_event_01",
            audio.RadioCue.name,
            "The authored emergency dispatch cue must remain separate from narration voice playback.");

        audio.ApplyVolumes(0.2f, 0.3f, 0.1f, 0.4f);
        audio.ApplyClips(audio.BriefingMusic, audio.CityDayAmbience, null, null);
        Assert.AreEqual("first_launch_story_calm_loop_01", audio.MusicSource.clip.name);
        Assert.AreEqual("first_launch_city_market_loop_01", audio.AmbienceSource.clip.name);
        Assert.IsNull(audio.VehicleSource.clip);

        audio.ApplyClips(audio.ConflictMusic, audio.BattlefieldAmbience, audio.VehicleEngine, audio.AttackCue);
        Assert.AreEqual("first_launch_story_crisis_loop_01", audio.MusicSource.clip.name);
        Assert.AreEqual("first_launch_city_attack_loop_01", audio.AmbienceSource.clip.name);
        Assert.AreEqual("first_launch_convoy_interior_loop_01", audio.VehicleSource.clip.name);
        Assert.AreEqual("first_launch_distant_attack_event_01", audio.EventSource.clip.name);

        audio.ApplyClips(audio.ConflictMusic, audio.BattlefieldAmbience, null, null);
        Assert.IsNull(audio.EventSource.clip, "State changes without a cue must clear a previous one-shot.");

        audio.ApplyVolumes(0f, 0f, 0f, 0f);
        Assert.AreEqual(0f, audio.MusicSource.volume);
        Assert.AreEqual(0f, audio.AmbienceSource.volume);
        Assert.AreEqual(0f, audio.VehicleSource.volume);
        Assert.AreEqual(0f, audio.EventSource.volume);

        audio.StopAll();
        Assert.IsNull(audio.MusicSource.clip);
        Assert.IsNull(audio.AmbienceSource.clip);
        Assert.IsNull(audio.VehicleSource.clip);
        Assert.IsNull(audio.EventSource.clip);
        UnityEngine.Object.DestroyImmediate(instance);
    }

    [Test]
    public void Dialogue_LongTextExpandsFrameWithoutEllipsis()
    {
        FirstLaunchNarrativePresentationPrefabBuilder.Build();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        GameObject canvasObject = new("DialogueMeasurementCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(4800f, 2160f);
        scaler.matchWidthOrHeight = 0.5f;
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.SetParent(canvas.transform, false);
        Canvas.ForceUpdateCanvases();
        NarrativeDialogueView view = instance.GetComponentInChildren<NarrativeDialogueView>(true);
        TMP_Text body = Array.Find(view.GetComponentsInChildren<TMP_Text>(true), text => text.name == "DialogueText");
        RectTransform frame = view.GetComponent<RectTransform>();
        const string longLine = "Commander, the eastern clinic corridor remains blocked while civil crews, families, and the surviving response convoy wait beyond the damaged relay junction.";
        NarrativeSubtitleStyle style = NarrativeSubtitleStyleUtilitySystemHelper.Resolve(Game.UI.Runtime.SettingsService.Defaults);

        view.PrepareLine(longLine, style);
        Canvas.ForceUpdateCanvases();

        body.ForceMeshUpdate(true, true);
        TMP_TextInfo textInfo = body.textInfo;
        Assert.GreaterOrEqual(textInfo.lineCount, 3);
        float measuredHeight = textInfo.lineInfo[0].ascender - textInfo.lineInfo[textInfo.lineCount - 1].descender;
        Assert.Greater(frame.sizeDelta.y, 292f);
        Assert.AreEqual(TextOverflowModes.Overflow, body.overflowMode);
        Assert.GreaterOrEqual(body.rectTransform.rect.height + 0.5f, measuredHeight);
        Assert.GreaterOrEqual(frame.sizeDelta.y, 155f + 78f + measuredHeight + 10f);
        UnityEngine.Object.DestroyImmediate(instance);
        UnityEngine.Object.DestroyImmediate(canvasObject);
    }

    private static NarrativeDialogueRevealPresentationSystemHelper Build(string text, float deadline, bool instant = false)
    {
        NarrativeDialogueRevealPresentationSystemHelper helper = new();
        helper.Prepare(text, deadline, 28f, 0.11f, 0.16f, 0.24f, 0.32f, instant);
        return helper;
    }
}
