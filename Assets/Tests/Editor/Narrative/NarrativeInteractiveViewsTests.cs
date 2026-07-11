using System;
using System.Reflection;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class NarrativeInteractiveViewsTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            NarrativeInteractiveViewsTests tests = new();
            tests.Identity_DefaultsEditsPortraitAndDebouncesCommit();
            tests.Identity_UnbindStopsEmissionAndRebindAllowsOneNewCommit();
            tests.Guidance_DefaultsSelectsAndDebouncesCommit();
            tests.Guidance_UnbindStopsEmissionAndAccessibilityLabelsAreApplied();
            Debug.Log("[NarrativeInteractiveViewsValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[NarrativeInteractiveViewsValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void Identity_DefaultsEditsPortraitAndDebouncesCommit()
    {
        GameObject root = new("IdentityView");
        root.SetActive(false);
        try
        {
            NarrativeCommanderIdentityView view = root.AddComponent<NarrativeCommanderIdentityView>();
            TMP_InputField callsign = AddChild<TMP_InputField>(root, "CallsignInput");
            TMP_InputField displayName = AddChild<TMP_InputField>(root, "DisplayNameInput");
            Button continueButton = AddButton(root, "ContinueButton");
            Button[] portraitButtons = { AddButton(root, "Portrait0"), AddButton(root, "Portrait1") };
            Image[] portraitImages = { AddImage(root, "PortraitImage0"), AddImage(root, "PortraitImage1") };
            Image[] selectionImages = { AddImage(root, "Selection0"), AddImage(root, "Selection1") };
            Image preview = AddImage(root, "SelectedPortrait");
            TMP_Text callsignLabel = AddChild<TextMeshProUGUI>(root, "CallsignLabel");
            TMP_Text displayNameLabel = AddChild<TextMeshProUGUI>(root, "DisplayNameLabel");
            TMP_Text continueLabel = AddChild<TextMeshProUGUI>(root, "ContinueLabel");

            SetField(view, "callsignInput", callsign);
            SetField(view, "displayNameInput", displayName);
            SetField(view, "continueButton", continueButton);
            SetField(view, "portraitButtons", portraitButtons);
            SetField(view, "portraitImages", portraitImages);
            SetField(view, "portraitSelectionImages", selectionImages);
            SetField(view, "selectedPortraitImage", preview);
            SetField(view, "callsignAccessibilityLabel", callsignLabel);
            SetField(view, "displayNameAccessibilityLabel", displayNameLabel);
            SetField(view, "continueAccessibilityLabel", continueLabel);

            root.SetActive(true);

            NarrativeUiAction emitted = default;
            int emissionCount = 0;
            view.SetActionContext("first_launch", "first_launch.commander_identity", "", 41UL);
            view.BindActions(action =>
            {
                emitted = action;
                emissionCount++;
            });

            Assert.IsNotEmpty(view.SelectedIdentity.Callsign);
            Assert.IsNotEmpty(view.SelectedIdentity.DisplayName);
            Assert.AreEqual(0, view.SelectedPortraitIndex);
            Assert.AreEqual("Commander callsign", callsignLabel.text);

            callsign.SetTextWithoutNotify("  RAVEN  ");
            displayName.SetTextWithoutNotify("  Alex Morgan  ");
            portraitButtons[1].onClick.Invoke();

            continueButton.onClick.Invoke();
            continueButton.onClick.Invoke();

            Assert.AreEqual(1, emissionCount);
            Assert.AreEqual(NarrativeUiActionKind.CommitCommanderIdentity, emitted.Kind);
            Assert.AreEqual("first_launch", emitted.SequenceId);
            Assert.AreEqual("first_launch.commander_identity", emitted.StateId);
            Assert.AreEqual(41UL, emitted.TransitionToken);
            Assert.AreEqual("RAVEN", view.SelectedIdentity.Callsign);
            Assert.AreEqual("Alex Morgan", view.SelectedIdentity.DisplayName);
            Assert.AreEqual(1, view.SelectedPortraitIndex);
            Assert.IsTrue(view.CommitRequested);
            Assert.IsFalse(continueButton.interactable);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Identity_UnbindStopsEmissionAndRebindAllowsOneNewCommit()
    {
        GameObject root = new("IdentityView");
        root.SetActive(false);
        try
        {
            NarrativeCommanderIdentityView view = root.AddComponent<NarrativeCommanderIdentityView>();
            TMP_InputField callsign = AddChild<TMP_InputField>(root, "CallsignInput");
            Button continueButton = AddButton(root, "ContinueButton");
            SetField(view, "callsignInput", callsign);
            SetField(view, "continueButton", continueButton);
            SetField(view, "selectedPortraitImage", AddImage(root, "SelectedPortrait"));
            SetField(view, "callsignAccessibilityLabel", AddChild<TextMeshProUGUI>(root, "CallsignLabel"));
            SetField(view, "displayNameAccessibilityLabel", AddChild<TextMeshProUGUI>(root, "DisplayNameLabel"));
            SetField(view, "continueAccessibilityLabel", AddChild<TextMeshProUGUI>(root, "ContinueLabel"));
            root.SetActive(true);

            int emissionCount = 0;
            view.BindActions(_ => emissionCount++);
            view.UnbindActions();
            continueButton.onClick.Invoke();
            Assert.AreEqual(0, emissionCount);

            view.BindActions(_ => emissionCount++);
            continueButton.onClick.Invoke();
            continueButton.onClick.Invoke();
            Assert.AreEqual(1, emissionCount);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Guidance_DefaultsSelectsAndDebouncesCommit()
    {
        GameObject root = new("GuidanceView");
        root.SetActive(false);
        try
        {
            NarrativeGuidanceChoiceView view = root.AddComponent<NarrativeGuidanceChoiceView>();
            Button fullButton = AddButton(root, "FullButton");
            Button contextualButton = AddButton(root, "ContextualButton");
            Button minimalButton = AddButton(root, "MinimalButton");
            Button continueButton = AddButton(root, "ContinueButton");
            SetField(view, "fullButton", fullButton);
            SetField(view, "contextualButton", contextualButton);
            SetField(view, "minimalButton", minimalButton);
            SetField(view, "continueButton", continueButton);
            SetField(view, "fullSelectionImage", AddImage(root, "FullSelection"));
            SetField(view, "contextualSelectionImage", AddImage(root, "ContextualSelection"));
            SetField(view, "minimalSelectionImage", AddImage(root, "MinimalSelection"));
            SetField(view, "fullAccessibilityLabel", AddChild<TextMeshProUGUI>(root, "FullLabel"));
            SetField(view, "contextualAccessibilityLabel", AddChild<TextMeshProUGUI>(root, "ContextualLabel"));
            SetField(view, "minimalAccessibilityLabel", AddChild<TextMeshProUGUI>(root, "MinimalLabel"));
            SetField(view, "continueAccessibilityLabel", AddChild<TextMeshProUGUI>(root, "ContinueLabel"));
            root.SetActive(true);

            NarrativeUiAction emitted = default;
            int emissionCount = 0;
            view.SetActionContext("first_launch", "first_launch.guidance_choice", null, 42UL);
            view.BindActions(action =>
            {
                emitted = action;
                emissionCount++;
            });

            Assert.AreEqual(NarrativeGuidanceMode.Full, view.SelectedGuidance);
            Assert.IsTrue(fullButton.interactable);

            contextualButton.onClick.Invoke();
            Assert.AreEqual(NarrativeGuidanceMode.Contextual, view.SelectedGuidance);

            continueButton.onClick.Invoke();
            continueButton.onClick.Invoke();

            Assert.AreEqual(1, emissionCount);
            Assert.AreEqual(NarrativeUiActionKind.CommitGuidance, emitted.Kind);
            Assert.AreEqual("first_launch.guidance_choice", emitted.StateId);
            Assert.AreEqual(42UL, emitted.TransitionToken);
            Assert.AreEqual(NarrativeGuidanceMode.Contextual, view.SelectedGuidance);
            Assert.IsTrue(view.CommitRequested);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Guidance_UnbindStopsEmissionAndAccessibilityLabelsAreApplied()
    {
        GameObject root = new("GuidanceView");
        root.SetActive(false);
        try
        {
            NarrativeGuidanceChoiceView view = root.AddComponent<NarrativeGuidanceChoiceView>();
            Button fullButton = AddButton(root, "FullButton");
            Button contextualButton = AddButton(root, "ContextualButton");
            Button minimalButton = AddButton(root, "MinimalButton");
            Button continueButton = AddButton(root, "ContinueButton");
            TMP_Text fullLabel = AddChild<TextMeshProUGUI>(root, "FullLabel");
            TMP_Text contextualLabel = AddChild<TextMeshProUGUI>(root, "ContextualLabel");
            TMP_Text minimalLabel = AddChild<TextMeshProUGUI>(root, "MinimalLabel");
            TMP_Text continueLabel = AddChild<TextMeshProUGUI>(root, "ContinueLabel");
            SetField(view, "fullButton", fullButton);
            SetField(view, "contextualButton", contextualButton);
            SetField(view, "minimalButton", minimalButton);
            SetField(view, "continueButton", continueButton);
            SetField(view, "fullSelectionImage", AddImage(root, "FullSelection"));
            SetField(view, "contextualSelectionImage", AddImage(root, "ContextualSelection"));
            SetField(view, "minimalSelectionImage", AddImage(root, "MinimalSelection"));
            SetField(view, "fullAccessibilityLabel", fullLabel);
            SetField(view, "contextualAccessibilityLabel", contextualLabel);
            SetField(view, "minimalAccessibilityLabel", minimalLabel);
            SetField(view, "continueAccessibilityLabel", continueLabel);
            root.SetActive(true);

            view.SetAccessibilityLabels("Guided", "Hints", "Veteran", "Confirm guidance");
            Assert.AreEqual("Guided", fullLabel.text);
            Assert.AreEqual("Hints", contextualLabel.text);
            Assert.AreEqual("Veteran", minimalLabel.text);
            Assert.AreEqual("Confirm guidance", continueLabel.text);

            int emissionCount = 0;
            view.BindActions(_ => emissionCount++);
            view.UnbindActions();
            continueButton.onClick.Invoke();
            Assert.AreEqual(0, emissionCount);

            view.SetSelectedGuidance((NarrativeGuidanceMode)99);
            Assert.AreEqual(NarrativeGuidanceMode.Full, view.SelectedGuidance);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static T AddChild<T>(GameObject parent, string name) where T : Component
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        return child.AddComponent<T>();
    }

    private static Button AddButton(GameObject parent, string name)
    {
        GameObject child = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        child.transform.SetParent(parent.transform, false);
        return child.GetComponent<Button>();
    }

    private static Image AddImage(GameObject parent, string name)
    {
        GameObject child = new(name, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(parent.transform, false);
        return child.GetComponent<Image>();
    }

    private static void SetField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, fieldName);
        field.SetValue(target, value);
    }
}
