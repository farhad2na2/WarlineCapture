using System;
using System.Collections.Generic;
using System.Reflection;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class NarrativeReviewerControlsViewTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.Bind_EmitsEveryReviewerActionOnce());
            passed++;
            RunCase(test => test.RepeatedBind_ReplacesDelegateWithoutDuplicatingListeners());
            passed++;
            RunCase(test => test.Unbind_StopsActionEmission());
            passed++;
            RunCase(test => test.Setters_ProjectStateWithoutEmittingActions());
            passed++;
            RunCase(test => test.StateAndProgressSetters_ClampInvalidInput());
            passed++;

            Debug.Log($"[NarrativeReviewerControlsViewValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[NarrativeReviewerControlsViewValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void Bind_EmitsEveryReviewerActionOnce()
    {
        using ViewFixture fixture = new();
        List<NarrativeReviewerAction> actions = new();
        fixture.View.Bind(actions.Add);

        fixture.PlayPauseButton.onClick.Invoke();
        fixture.RestartButton.onClick.Invoke();
        fixture.PreviousButton.onClick.Invoke();
        fixture.NextButton.onClick.Invoke();
        fixture.TimelineSlider.value = 0.35f;
        fixture.SkipToGameButton.onClick.Invoke();
        fixture.JumpToDebriefButton.onClick.Invoke();
        fixture.ReducedMotionToggle.isOn = true;
        fixture.SubtitlesToggle.isOn = true;
        fixture.SafeAreaToggle.isOn = true;
        fixture.CaptureButton.onClick.Invoke();

        Assert.AreEqual(11, actions.Count);
        CollectionAssert.AreEqual(
            new[]
            {
                NarrativeReviewerActionKind.TogglePlayPause,
                NarrativeReviewerActionKind.Restart,
                NarrativeReviewerActionKind.Previous,
                NarrativeReviewerActionKind.Next,
                NarrativeReviewerActionKind.Seek,
                NarrativeReviewerActionKind.SkipToGame,
                NarrativeReviewerActionKind.JumpToDebrief,
                NarrativeReviewerActionKind.SetReducedMotion,
                NarrativeReviewerActionKind.SetSubtitles,
                NarrativeReviewerActionKind.SetSafeArea,
                NarrativeReviewerActionKind.Capture
            },
            actions.ConvertAll(action => action.Kind));
        Assert.AreEqual(0.35f, actions[4].Position, 0.0001f);
        Assert.IsTrue(actions[7].ReducedMotion);
        Assert.IsTrue(actions[8].Enabled);
        Assert.IsTrue(actions[9].Enabled);
    }

    [Test]
    public void RepeatedBind_ReplacesDelegateWithoutDuplicatingListeners()
    {
        using ViewFixture fixture = new();
        int firstHandlerCalls = 0;
        int secondHandlerCalls = 0;

        fixture.View.Bind(_ => firstHandlerCalls++);
        fixture.View.Bind(_ => secondHandlerCalls++);
        fixture.View.Bind(_ => secondHandlerCalls++);
        fixture.NextButton.onClick.Invoke();

        Assert.AreEqual(0, firstHandlerCalls);
        Assert.AreEqual(1, secondHandlerCalls);
    }

    [Test]
    public void Unbind_StopsActionEmission()
    {
        using ViewFixture fixture = new();
        int calls = 0;
        fixture.View.Bind(_ => calls++);
        fixture.View.Unbind();

        fixture.PlayPauseButton.onClick.Invoke();
        fixture.TimelineSlider.value = 0.5f;
        fixture.ReducedMotionToggle.isOn = true;

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Setters_ProjectStateWithoutEmittingActions()
    {
        using ViewFixture fixture = new();
        int calls = 0;
        fixture.View.Bind(_ => calls++);

        fixture.View.SetPlayingState(true);
        fixture.View.SetPosition(0.625f, "05 / 08");
        fixture.View.SetStateId("first_launch.debrief.p03");
        fixture.View.SetReducedMotion(true);
        fixture.View.SetSubtitles(true);
        fixture.View.SetSafeArea(true);
        fixture.View.SetDevelopmentVisibility(false);

        Assert.AreEqual("PAUSE", fixture.PlayPauseLabel.text);
        Assert.AreEqual(0.625f, fixture.TimelineSlider.value, 0.0001f);
        Assert.AreEqual("05 / 08", fixture.PositionLabel.text);
        Assert.AreEqual("first_launch.debrief.p03", fixture.StateIdLabel.text);
        Assert.IsTrue(fixture.ReducedMotionToggle.isOn);
        Assert.IsTrue(fixture.SubtitlesToggle.isOn);
        Assert.IsTrue(fixture.SafeAreaToggle.isOn);
        Assert.AreEqual(0f, fixture.VisibilityGroup.alpha);
        Assert.IsFalse(fixture.VisibilityGroup.interactable);
        Assert.IsFalse(fixture.VisibilityGroup.blocksRaycasts);
        Assert.AreEqual(0, calls);

        fixture.View.SetPlayingState(false);
        fixture.View.SetPosition(0.25f);
        fixture.View.SetDevelopmentVisibility(true);

        Assert.AreEqual("PLAY", fixture.PlayPauseLabel.text);
        Assert.AreEqual(0.25f, fixture.TimelineSlider.value, 0.0001f);
        Assert.AreEqual("05 / 08", fixture.PositionLabel.text);
        Assert.AreEqual(1f, fixture.VisibilityGroup.alpha);
        Assert.IsTrue(fixture.VisibilityGroup.interactable);
        Assert.IsTrue(fixture.VisibilityGroup.blocksRaycasts);
        Assert.AreEqual(0, calls);
    }

    [Test]
    public void StateAndProgressSetters_ClampInvalidInput()
    {
        using ViewFixture fixture = new();

        fixture.View.SetProgress(2f);
        Assert.AreEqual(1f, fixture.TimelineSlider.value, 0.0001f);

        fixture.View.SetProgress(-1f);
        Assert.AreEqual(0f, fixture.TimelineSlider.value, 0.0001f);

        fixture.View.SetState("state.high", 12, 8);
        Assert.AreEqual("state.high", fixture.StateIdLabel.text);
        Assert.AreEqual("8 / 8", fixture.PositionLabel.text);

        fixture.View.SetState(null, -3, -2);
        Assert.AreEqual(string.Empty, fixture.StateIdLabel.text);
        Assert.AreEqual("0 / 0", fixture.PositionLabel.text);
    }

    private static void RunCase(Action<NarrativeReviewerControlsViewTests> testCase)
    {
        testCase(new NarrativeReviewerControlsViewTests());
    }

    private sealed class ViewFixture : IDisposable
    {
        private readonly GameObject root;

        public ViewFixture()
        {
            root = new GameObject("NarrativeReviewerControls");
            root.SetActive(false);

            View = root.AddComponent<NarrativeReviewerControlsView>();
            PlayPauseButton = AddButton("PlayPause");
            RestartButton = AddButton("Restart");
            PreviousButton = AddButton("Previous");
            NextButton = AddButton("Next");
            SkipToGameButton = AddButton("SkipToGame");
            JumpToDebriefButton = AddButton("JumpToDebrief");
            CaptureButton = AddButton("Capture");
            TimelineSlider = AddChild<Slider>("Timeline");
            PlayPauseLabel = AddChild<TextMeshProUGUI>("PlayPauseLabel");
            StateIdLabel = AddChild<TextMeshProUGUI>("StateIdLabel");
            PositionLabel = AddChild<TextMeshProUGUI>("PositionLabel");
            ReducedMotionToggle = AddChild<Toggle>("ReducedMotion");
            SubtitlesToggle = AddChild<Toggle>("Subtitles");
            SafeAreaToggle = AddChild<Toggle>("SafeArea");
            VisibilityGroup = root.AddComponent<CanvasGroup>();

            SetField("playPauseButton", PlayPauseButton);
            SetField("restartButton", RestartButton);
            SetField("previousButton", PreviousButton);
            SetField("nextButton", NextButton);
            SetField("skipToGameButton", SkipToGameButton);
            SetField("jumpToDebriefButton", JumpToDebriefButton);
            SetField("captureButton", CaptureButton);
            SetField("timelineSlider", TimelineSlider);
            SetField("playPauseLabel", PlayPauseLabel);
            SetField("stateIdLabel", StateIdLabel);
            SetField("positionLabel", PositionLabel);
            SetField("reducedMotionToggle", ReducedMotionToggle);
            SetField("subtitlesToggle", SubtitlesToggle);
            SetField("safeAreaToggle", SafeAreaToggle);
            SetField("visibilityGroup", VisibilityGroup);

            root.SetActive(true);
        }

        public NarrativeReviewerControlsView View { get; }
        public Button PlayPauseButton { get; }
        public Button RestartButton { get; }
        public Button PreviousButton { get; }
        public Button NextButton { get; }
        public Button SkipToGameButton { get; }
        public Button JumpToDebriefButton { get; }
        public Button CaptureButton { get; }
        public Slider TimelineSlider { get; }
        public TMP_Text PlayPauseLabel { get; }
        public TMP_Text StateIdLabel { get; }
        public TMP_Text PositionLabel { get; }
        public Toggle ReducedMotionToggle { get; }
        public Toggle SubtitlesToggle { get; }
        public Toggle SafeAreaToggle { get; }
        public CanvasGroup VisibilityGroup { get; }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        private Button AddButton(string name)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(root.transform, false);
            return child.GetComponent<Button>();
        }

        private T AddChild<T>(string name) where T : Component
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<T>();
        }

        private void SetField<T>(string fieldName, T value)
        {
            FieldInfo field = typeof(NarrativeReviewerControlsView).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            field.SetValue(View, value);
        }
    }
}
