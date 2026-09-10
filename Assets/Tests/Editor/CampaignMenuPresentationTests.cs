using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CampaignMenuPresentationTests
{
    [TestCase(1920,1080)] [TestCase(2400,1080)] [TestCase(2560,1080)]
    public void BriefingClearsHeaderAndFooterAtSupportedLandscapeSizes(int width, int height)
    {
        var canvasRoot = new GameObject("Campaign layout", typeof(RectTransform), typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvasRoot.transform).sizeDelta = new Vector2(width,height);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignMenuPresentationRepair.PrefabPath);
        var instance = Object.Instantiate(prefab, canvasRoot.transform, false);
        try
        {
            var view = instance.GetComponentInChildren<CampaignOperationsScreenView>(true);
            view.GetComponent<MainMenuV3SectionLayoutView>().RefreshLayout(); Canvas.ForceUpdateCanvases();
            var panel = new Vector3[4]; var header = new Vector3[4]; var footer = new Vector3[4];
            view.MissionBriefing.GetWorldCorners(panel);
            ((RectTransform)view.transform.Find("CommandChip")).GetWorldCorners(header);
            view.LaunchMissionButton.GetComponent<RectTransform>().GetWorldCorners(footer);
            Assert.Less(panel[1].y, header[0].y, "Briefing must be entirely below header.");
            Assert.Greater(panel[0].y, footer[1].y, "Briefing must be above footer.");
        }
        finally { Object.DestroyImmediate(canvasRoot); }
    }

    [Test]
    public void AvailableM3ShowsItsNumberWhileCompletedM2ShowsACheck()
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CampaignMenuPresentationRepair.PrefabPath));
        try
        {
            var view = root.GetComponentInChildren<CampaignOperationsScreenView>(true);
            var selected = new UiCampaignMissionModel("saga.ch01.m03.radar_warning", "", "", "M3", true, false, false, 0, 0, 0, UiCampaignMissionPrimaryActionKind.Start, "START");
            view.Apply(new UiCampaignOperationsModel(1,1,1,selected,"saga.ch01.m04.airlift",false,7,3));
            Assert.IsTrue(view.MissionNodeButtons[2].interactable);
            Assert.IsFalse(view.MissionNodes[2].Find("StateIcon").gameObject.activeSelf);
            Assert.IsTrue(view.MissionNodes[2].Find("Number").gameObject.activeSelf);
            Assert.IsTrue(view.MissionNodes[1].Find("StateIcon").gameObject.activeSelf);
            Assert.IsFalse(view.MissionNodeButtons[3].interactable);
            Assert.IsTrue(view.MissionNodes[3].Find("StateIcon").gameObject.activeSelf);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
