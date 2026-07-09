using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public sealed class UiAudioEventViewTests
{
    private World _world;
    private World _previousDefaultWorld;
    private GameObject _root;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.Gateway_ResolvesGeneratedEventIds());
            passed++;
            RunCase(test => test.Gateway_ResolvesSettingsSampleEvents());
            passed++;
            RunCase(test => test.SettingsPanelView_EmitsSamplesOnlyForEnableInteractions());
            passed++;
            RunCase(test => test.ButtonAudioEventView_EmitsConfiguredClickAndDisabledTap());
            passed++;
            RunCase(test => test.ToggleAudioEventView_EmitsOnAndOffEvents());
            passed++;
            RunCase(test => test.SliderAudioEventView_EmitsTickAfterMinimumDelta());
            passed++;
            RunCase(test => test.UiAudioEventBridge_EnqueuesRequestIntoDefaultWorld());
            passed++;

            Debug.Log($"[UiAudioEventViewValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[UiAudioEventViewValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<UiAudioEventViewTests> testCase)
    {
        var tests = new UiAudioEventViewTests();
        tests.SetUp();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UiAudioEventViewTests");
        World.DefaultGameObjectInjectionWorld = _world;
        _root = new GameObject("UiAudioEventViewTests");
    }

    [TearDown]
    public void TearDown()
    {
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousDefaultWorld;
        _world?.Dispose();

        if (_root != null)
            UnityEngine.Object.DestroyImmediate(_root);
    }

    [Test]
    public void Gateway_ResolvesGeneratedEventIds()
    {
        Assert.IsTrue(UIAudioEventGateway.TryCreateRequest(UIAudioEventKind.ButtonPrimaryClick, out UIAudioEventRequest request));
        Assert.AreEqual(UIAudioEventKind.ButtonPrimaryClick, request.Kind);
        Assert.AreEqual(AudioEventIds.UIButtonPrimaryClick, request.EventId);
        Assert.AreEqual(AudioEventIds.UIButtonPrimaryClickHash, request.EventHash);
        Assert.AreEqual("UI", request.BusId);
    }

    [Test]
    public void Gateway_ResolvesSettingsSampleEvents()
    {
        Assert.IsTrue(UIAudioEventGateway.TryCreateRequest(UIAudioEventKind.SettingsSoundConfirm, out UIAudioEventRequest soundRequest));
        Assert.AreEqual(UIAudioEventKind.SettingsSoundConfirm, soundRequest.Kind);
        Assert.AreEqual(AudioEventIds.UIFeedbackToastPositive, soundRequest.EventId);
        Assert.AreEqual(AudioEventIds.UIFeedbackToastPositiveHash, soundRequest.EventHash);
        Assert.AreEqual("UI", soundRequest.BusId);
        Assert.Greater(soundRequest.CooldownSeconds, 0f);

        Assert.IsTrue(UIAudioEventGateway.TryCreateRequest(UIAudioEventKind.SettingsVoiceSample, out UIAudioEventRequest voiceRequest));
        Assert.AreEqual(UIAudioEventKind.SettingsVoiceSample, voiceRequest.Kind);
        Assert.AreEqual(AudioEventIds.VOARIAMessageTacticalFeedbackRtsCameraRestored, voiceRequest.EventId);
        Assert.AreEqual(AudioEventIds.VOARIAMessageTacticalFeedbackRtsCameraRestoredHash, voiceRequest.EventHash);
        Assert.AreEqual("Voice", voiceRequest.BusId);
        Assert.GreaterOrEqual(voiceRequest.CooldownSeconds, 0.5f);
    }

    [Test]
    public void SettingsPanelView_EmitsSamplesOnlyForEnableInteractions()
    {
        var panelObject = new GameObject("SettingsPanel");
        panelObject.transform.SetParent(_root.transform, false);
        panelObject.SetActive(false);
        SettingsPanelView panel = panelObject.AddComponent<SettingsPanelView>();
        UIToggleRowView soundRow = CreateToggleRow("SoundEnabledRow", out Toggle soundToggle);
        UIToggleRowView voiceRow = CreateToggleRow("VoiceEnabledRow", out Toggle voiceToggle);
        SetPrivateField(panel, "soundEnabledRow", soundRow);
        SetPrivateField(panel, "voiceEnabledRow", voiceRow);
        InvokePrivate(panel, "Awake");

        int eventCount = 0;
        UIAudioEventKind firstKind = UIAudioEventKind.None;
        UIAudioEventKind secondKind = UIAudioEventKind.None;
        void Capture(UIAudioEventRequest request)
        {
            eventCount++;
            if (eventCount == 1)
                firstKind = request.Kind;
            else if (eventCount == 2)
                secondKind = request.Kind;
        }

        UIAudioEventGateway.AudioEventRequested += Capture;
        try
        {
            UISettingsModel model = default;
            model.Audio.SoundEnabled = false;
            model.Audio.VoiceEnabled = false;
            panel.Bind(model);
            Assert.AreEqual(0, eventCount, "Binding settings controls must not play samples.");

            soundToggle.isOn = true;
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(UIAudioEventKind.SettingsSoundConfirm, firstKind);

            soundToggle.isOn = false;
            Assert.AreEqual(1, eventCount, "Disabling Sound must not play the enable sample.");

            voiceToggle.isOn = true;
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(UIAudioEventKind.SettingsVoiceSample, secondKind);

            voiceToggle.isOn = false;
            Assert.AreEqual(2, eventCount, "Disabling Voice must not play the enable sample.");
        }
        finally
        {
            UIAudioEventGateway.AudioEventRequested -= Capture;
            InvokePrivate(panel, "OnDestroy");
        }
    }

    [Test]
    public void ButtonAudioEventView_EmitsConfiguredClickAndDisabledTap()
    {
        Button button = CreateComponent<Button>("Button");
        UIButtonAudioEventView view = button.gameObject.AddComponent<UIButtonAudioEventView>();
        view.Configure(UIAudioEventKind.TabSelect);

        int eventCount = 0;
        UIAudioEventKind lastKind = UIAudioEventKind.None;
        void Capture(UIAudioEventRequest request)
        {
            eventCount++;
            lastKind = request.Kind;
        }

        UIAudioEventGateway.AudioEventRequested += Capture;
        try
        {
            button.onClick.Invoke();
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(UIAudioEventKind.TabSelect, lastKind);

            button.interactable = false;
            view.OnPointerClick(null);
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(UIAudioEventKind.ButtonDisabledTap, lastKind);
        }
        finally
        {
            UIAudioEventGateway.AudioEventRequested -= Capture;
        }
    }

    [Test]
    public void ToggleAudioEventView_EmitsOnAndOffEvents()
    {
        Toggle toggle = CreateComponent<Toggle>("Toggle");
        UIToggleAudioEventView view = toggle.gameObject.AddComponent<UIToggleAudioEventView>();
        view.Configure(UIAudioEventKind.ToggleOn, UIAudioEventKind.ToggleOff);

        int eventCount = 0;
        UIAudioEventKind lastKind = UIAudioEventKind.None;
        void Capture(UIAudioEventRequest request)
        {
            eventCount++;
            lastKind = request.Kind;
        }

        UIAudioEventGateway.AudioEventRequested += Capture;
        try
        {
            toggle.isOn = true;
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(UIAudioEventKind.ToggleOn, lastKind);

            toggle.isOn = false;
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(UIAudioEventKind.ToggleOff, lastKind);
        }
        finally
        {
            UIAudioEventGateway.AudioEventRequested -= Capture;
        }
    }

    [Test]
    public void SliderAudioEventView_EmitsTickAfterMinimumDelta()
    {
        Slider slider = CreateComponent<Slider>("Slider");
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 0f;
        UISliderAudioEventView view = slider.gameObject.AddComponent<UISliderAudioEventView>();
        view.Configure(UIAudioEventKind.SliderTick, valueDelta: 5f);

        int eventCount = 0;
        UIAudioEventGateway.AudioEventRequested += Capture;
        try
        {
            slider.value = 2f;
            Assert.AreEqual(0, eventCount);

            slider.value = 6f;
            Assert.AreEqual(1, eventCount);
        }
        finally
        {
            UIAudioEventGateway.AudioEventRequested -= Capture;
        }

        void Capture(UIAudioEventRequest request)
        {
            if (request.Kind == UIAudioEventKind.SliderTick)
                eventCount++;
        }
    }

    [Test]
    public void UiAudioEventBridge_EnqueuesRequestIntoDefaultWorld()
    {
        _world.CreateSystem<UiAudioEventBridgeSystem>();

        Assert.IsTrue(UIAudioEventGateway.Raise(UIAudioEventKind.CardSelect));

        Entity audioEntity = Game.Runtime.AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests = _world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);

        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.UICardSelect, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.UICardSelectHash, requests[0].EventHash);
        Assert.AreEqual("UI", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
    }

    private T CreateComponent<T>(string name)
        where T : Component
    {
        var child = new GameObject(name);
        child.transform.SetParent(_root.transform, false);
        return child.AddComponent<T>();
    }

    private UIToggleRowView CreateToggleRow(string name, out Toggle toggle)
    {
        var rowObject = new GameObject(name);
        rowObject.transform.SetParent(_root.transform, false);
        var row = rowObject.AddComponent<UIToggleRowView>();

        var toggleObject = new GameObject("Toggle");
        toggleObject.transform.SetParent(rowObject.transform, false);
        toggle = toggleObject.AddComponent<Toggle>();
        SetPrivateField(row, "toggle", toggle);
        return row;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{target.GetType().Name} must contain private field {fieldName}.");
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, $"{target.GetType().Name} must contain private method {methodName}.");
        method.Invoke(target, null);
    }
}
