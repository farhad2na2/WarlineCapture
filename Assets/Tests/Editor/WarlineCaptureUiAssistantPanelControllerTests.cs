using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WarlineCaptureUiAssistantPanelControllerTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab";
    private const string ControllerPath = "Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs";

    [Test]
    public void AssistantPanelController_InstantiatesShowsHidesAndBindsRecommendation()
    {
        GameObject host = new("AssistantPanelControllerHost");
        GameObject root = new("AssistantPanelRoot");
        try
        {
            AssistantPanelController controller = host.AddComponent<AssistantPanelController>();
            controller.SetPanelRootForTests(root.transform);
            controller.SetPanelPrefabForTests(LoadPrefab().GetComponent<AssistantPanelView>());
            InvokeAwake(controller);

            Assert.IsFalse(controller.IsOpen);

            AssistantPanelPresentationData recommendation = new(
                "placeholder.ui.test.select_squad",
                "SELECT RIFLE SQUAD",
                "Selection recommendations are placeholder-bound until the assistant runtime service owns state.",
                new[] { "Select squad", "Use move" },
                canShow: true,
                canExecute: false,
                canStop: true);

            AssistantPanelView view = controller.ShowRecommendation(recommendation);

            Assert.NotNull(view);
            Assert.AreEqual(root.transform, view.transform.parent);
            Assert.IsTrue(controller.IsOpen);
            Assert.AreEqual("placeholder.ui.test.select_squad", controller.ActiveRecommendationId);
            Assert.AreEqual("SELECT RIFLE SQUAD", view.RecommendationTitleText.text);
            Assert.AreEqual("Selection recommendations are placeholder-bound until the assistant runtime service owns state.", view.RecommendationBodyText.text);
            Assert.AreEqual("Select squad", view.ChipLabels[0].text);
            Assert.AreEqual("Use move", view.ChipLabels[1].text);
            Assert.IsTrue(view.ChipLabels[0].transform.parent.gameObject.activeSelf);
            Assert.IsTrue(view.ChipLabels[1].transform.parent.gameObject.activeSelf);
            Assert.IsFalse(view.ChipLabels[2].transform.parent.gameObject.activeSelf);
            Assert.IsTrue(view.ShowMeButton.interactable);
            Assert.IsFalse(view.DoItButton.interactable);
            Assert.IsTrue(view.StopButton.interactable);

            controller.Hide();
            Assert.IsFalse(controller.IsOpen);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AssistantPanelController_UsesViewReferencesNotChildNamesForBinding()
    {
        GameObject host = new("AssistantPanelControllerHost");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab());
        try
        {
            AssistantPanelView view = instance.GetComponent<AssistantPanelView>();
            view.RecommendationTitleText.name = "RenamedTitleText";
            view.RecommendationBodyText.name = "RenamedBodyText";
            view.ChipLabels[0].name = "RenamedPrimaryChip";
            view.ShowMeButton.name = "RenamedShowMeButton";
            view.DoItButton.name = "RenamedDoItButton";
            view.StopButton.name = "RenamedStopButton";

            AssistantPanelController controller = host.AddComponent<AssistantPanelController>();
            controller.SetPanelViewForTests(view);
            InvokeAwake(controller);

            controller.ShowRecommendation(new AssistantPanelPresentationData(
                "placeholder.ui.test.renamed_children",
                "RENAMED CHILDREN STILL BIND",
                "The controller must bind through AssistantPanelView fields, not transform paths.",
                new[] { "View API" },
                canShow: true,
                canExecute: true,
                canStop: false));

            Assert.AreEqual("RENAMED CHILDREN STILL BIND", view.RecommendationTitleText.text);
            Assert.AreEqual("The controller must bind through AssistantPanelView fields, not transform paths.", view.RecommendationBodyText.text);
            Assert.AreEqual("View API", view.ChipLabels[0].text);
            Assert.IsTrue(view.ShowMeButton.interactable);
            Assert.IsTrue(view.DoItButton.interactable);
            Assert.IsFalse(view.StopButton.interactable);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void AssistantPanelController_ExposesFutureSafeButtonCallbackSeams()
    {
        GameObject host = new("AssistantPanelControllerHost");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab());
        try
        {
            AssistantPanelView view = instance.GetComponent<AssistantPanelView>();
            AssistantPanelController controller = host.AddComponent<AssistantPanelController>();
            controller.SetPanelViewForTests(view);
            InvokeAwake(controller);

            string showMeId = string.Empty;
            string doItId = string.Empty;
            string stopId = string.Empty;
            controller.ShowMeRequested += id => showMeId = id;
            controller.DoItRequested += id => doItId = id;
            controller.StopRequested += id => stopId = id;

            controller.ShowRecommendation(new AssistantPanelPresentationData(
                "placeholder.ui.test.callbacks",
                "CALLBACK SEAMS",
                "Buttons expose ids for the future assistant runtime without dispatching gameplay here.",
                new[] { "Show", "Do", "Stop" },
                canShow: true,
                canExecute: true,
                canStop: true));

            view.ShowMeButton.onClick.Invoke();
            view.DoItButton.onClick.Invoke();
            view.StopButton.onClick.Invoke();

            Assert.AreEqual("placeholder.ui.test.callbacks", showMeId);
            Assert.AreEqual("placeholder.ui.test.callbacks", doItId);
            Assert.AreEqual("placeholder.ui.test.callbacks", stopId);
            Assert.IsTrue(controller.IsOpen, "Stop callback is only a seam in this UI pass; runtime ownership will decide whether to hide the panel.");
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void AssistantPanelController_DoesNotUseScreenCoordinatesOrChildPathLookups()
    {
        string source = File.ReadAllText(ResolveRepoFilePath(ControllerPath));

        StringAssert.DoesNotContain(".Find(", source);
        StringAssert.DoesNotContain("FindObject", source);
        StringAssert.DoesNotContain("GetComponentInChildren", source);
        StringAssert.DoesNotContain("Input.mousePosition", source);
        StringAssert.DoesNotContain("Screen.", source);
        StringAssert.DoesNotContain("anchoredPosition", source);
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, PrefabPath);
        return prefab;
    }

    private static void InvokeAwake(MonoBehaviour component)
    {
        MethodInfo awake = component.GetType().GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(awake);
        awake.Invoke(component, null);
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrEmpty(projectRoot)
            ? Path.GetFullPath(relativePath)
            : Path.Combine(projectRoot, relativePath);
    }
}
