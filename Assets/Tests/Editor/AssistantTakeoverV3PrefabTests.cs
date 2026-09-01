#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class AssistantTakeoverV3PrefabTests
{
    [Test]
    public void Prefab_HasCenteredResponsiveTakeoverSurfaceWithV3PortraitAndBorders()
    {
        AriaCommandAssistantPopupView view = RequireView();
        RectTransform takeover = view.AssistantTakeoverSurface;
        Assert.NotNull(takeover);
        Assert.AreEqual(new Vector2(0f, 1f), takeover.anchorMin);
        Assert.That(takeover.anchoredPosition.x, Is.EqualTo(443f).Within(.1f));
        Assert.That(takeover.anchoredPosition.y, Is.EqualTo(-225f).Within(.1f));
        Assert.That(takeover.rect.width, Is.EqualTo(785f).Within(.1f));
        Assert.That(takeover.rect.height, Is.EqualTo(470f).Within(.1f));

        Image portrait = FindNamed(takeover, "TakeoverAriaPortraitV3").GetComponent<Image>();
        Assert.AreEqual(
            AriaCommandAssistantV3PrefabBuilder.PortraitPath,
            AssetDatabase.GetAssetPath(portrait.sprite));
        Assert.NotNull(portrait.GetComponent<AspectRatioFitter>());

        V3GradientGraphic[] gradients = takeover.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.That(gradients.Length, Is.GreaterThanOrEqualTo(6));
        for (int i = 0; i < gradients.Length; i++)
        {
            SerializedObject serialized = new(gradients[i]);
            float border = serialized.FindProperty("borderWidth").floatValue;
            Assert.That(border == 0f || Mathf.Approximately(border, 3f), Is.True,
                $"{gradients[i].name} uses a non-V3 border width of {border}.");
        }
    }

    [Test]
    public void TakeoverModel_ShowsLiveIntentRowsAndHidesStandardPanel()
    {
        GameObject instance = Object.Instantiate(RequirePrefab());
        try
        {
            AriaCommandAssistantPopupView view = instance.GetComponent<AriaCommandAssistantPopupView>();
            Assert.IsTrue(view.TryBindHierarchy());
            MatchHudV3PrefabBuilder.ApplyAriaCommandAssistantPreviewModel(
                view,
                AriaCommandAssistantV3PrefabBuilder.CreateAssistantTakeoverPreviewModel());

            Assert.IsTrue(view.AssistantTakeoverSurface.gameObject.activeSelf);
            Assert.IsFalse(view.CommandAssistantPanel.gameObject.activeSelf);
            Assert.AreEqual("ARIA CONTROLLING", Text(view.transform, "ControlStateText"));
            Assert.AreEqual("MOVE RIFLE SQUAD TO COVER", Text(view.transform, "TakeoverIntentTitle"));
            Assert.AreEqual("Move to cover", Text(view.transform, "Goal0Title"));
            Assert.AreEqual("IN PROGRESS", Text(view.transform, "Goal0StateText"));
            Assert.AreEqual("Hold position", Text(view.transform, "Goal1Title"));
            Assert.IsFalse(FindNamed(view.transform, "Goal1StateText").gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ResumeAndStop_BothReturnControlThroughTheBoundStopIntent()
    {
        GameObject instance = Object.Instantiate(RequirePrefab());
        try
        {
            AriaCommandAssistantPopupView view = instance.GetComponent<AriaCommandAssistantPopupView>();
            Assert.IsTrue(view.TryBindHierarchy());
            int stopRequests = 0;
            view.BindActions(() => { }, () => { }, () => { }, () => stopRequests++);
            MatchHudV3PrefabBuilder.ApplyAriaCommandAssistantPreviewModel(
                view,
                AriaCommandAssistantV3PrefabBuilder.CreateAssistantTakeoverPreviewModel());

            FindNamed(view.transform, "ResumeCommandButton").GetComponent<Button>().onClick.Invoke();
            FindNamed(view.transform, "StopButton").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(2, stopRequests);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    public static void RunFocusedValidation()
    {
        var tests = new AssistantTakeoverV3PrefabTests();
        int passed = 0;
        try
        {
            tests.Prefab_HasCenteredResponsiveTakeoverSurfaceWithV3PortraitAndBorders(); passed++;
            tests.TakeoverModel_ShowsLiveIntentRowsAndHidesStandardPanel(); passed++;
            tests.ResumeAndStop_BothReturnControlThroughTheBoundStopIntent(); passed++;
            Debug.Log($"[AssistantTakeoverV3Validation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[AssistantTakeoverV3Validation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AriaCommandAssistantV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        return prefab;
    }

    private static AriaCommandAssistantPopupView RequireView()
    {
        AriaCommandAssistantPopupView view = RequirePrefab().GetComponent<AriaCommandAssistantPopupView>();
        Assert.NotNull(view);
        Assert.IsTrue(view.TryBindHierarchy());
        return view;
    }

    private static string Text(Transform root, string name) =>
        FindNamed(root, name).GetComponent<TMP_Text>().text;

    private static Transform FindNamed(Transform root, string targetName)
    {
        if (root == null)
            return null;
        if (root.name == targetName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindNamed(root.GetChild(i), targetName);
            if (match != null)
                return match;
        }
        return null;
    }
}
#endif
