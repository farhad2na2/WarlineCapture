using System;
using System.Linq;
using System.Reflection;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class SelectionCommandWheelValidation
{
    public static void Run()
    {
        try
        {
            MatchHudV3PrefabBuilder.BuildSelectionWheel();
            new SelectionCommandWheelValidation().WheelFollowsSelectionAndDispatchesExactlyOneEnabledAction();
            var shell = new UIShellCurrentContentLoadTests();
            try { shell.InstalledMatchHudCommandWheelUsesV3StructureAndStateTransitions(); }
            finally { shell.TearDown(); }
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            MatchHudV3PrefabBuilder.CaptureSelectionWheel();
            using (ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); MissionReadinessArchitectureValidation.Run();
                if (ValidationExit.LastExitCode != 0) throw new Exception("Architecture checks failed.");
            }
            Debug.Log("[SelectionCommandWheelQa] result=Passed actions=4 portraits=all-selection-types disabled=guarded opener=separate");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [Test]
    public void WheelFollowsSelectionAndDispatchesExactlyOneEnabledAction()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        var root = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var selection = root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            var wheel = root.GetComponentInChildren<CommandWheelPanelView>(true);
            var counts = new int[4];
            selection.BindActions(()=>counts[1]++, ()=>counts[0]++, ()=>counts[3]++);
            selection.BindCameraAction(()=>counts[2]++);
            typeof(CommandWheelPanelView).GetMethod("Awake", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(wheel,null);
            // Match shell mounts these in separate sections. Bind the live opener explicitly.
            wheel.BindRuntimeSectionReferences(selection.CommandWheelOpenButton, null);
            var serialized = new SerializedObject(wheel);
            var portrait = (Image)serialized.FindProperty("selectionPortrait").objectReferenceValue;
            foreach (SelectionSummaryPortraitKind kind in new[]{SelectionSummaryPortraitKind.Soldiers,
                SelectionSummaryPortraitKind.Vehicles,SelectionSummaryPortraitKind.Aircraft,
                SelectionSummaryPortraitKind.Transports,SelectionSummaryPortraitKind.Buildings,SelectionSummaryPortraitKind.MixedForce})
            {
                var sprite = selection.ResolveFallbackPortraitSprite(kind);
                Assert.NotNull(sprite);
                selection.SetSelectionVisible(true, sprite);
                selection.CommandWheelOpenButton.onClick.Invoke();
                Assert.IsTrue(wheel.IsOpen);
                Assert.AreSame(sprite, portrait.sprite, kind.ToString());
                wheel.Close();
            }
            Assert.AreSame(selection.CommandWheelOpenButton, wheel.NextBoardButton, "Board tutorial first opens Commands.");
            wheel.NextBoardButton.onClick.Invoke();
            Assert.AreEqual("BoardSector", wheel.NextBoardButton.name, "Next tutorial click becomes Board.");
            wheel.Close();
            var actions = serialized.FindProperty("selectionActions");
            Assert.AreEqual(4, actions.arraySize);
            for (int i=0;i<4;i++)
            {
                var source = selection.ResolveWheelAction(i);
                Assert.IsFalse(source.gameObject.activeInHierarchy,"Retired side actions cannot be clicked.");
                source.interactable = true;
                var button = (Button)actions.GetArrayElementAtIndex(i).objectReferenceValue;
                wheel.Open(); button.onClick.Invoke();
                Assert.AreEqual(1,counts[i]); Assert.IsFalse(wheel.IsOpen);
                source.interactable = false;
                wheel.Open(); Assert.IsFalse(button.interactable);
                button.onClick.Invoke(); Assert.AreEqual(1,counts[i],"Unavailable actions cannot dispatch even through programmatic clicks.");
                wheel.Close();
            }
            Assert.AreEqual(4,counts.Sum());
            selection.ShowSelection(); wheel.Open(); selection.HideSelection();
            typeof(CommandWheelPanelView).GetMethod("RefreshOpenSelection", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(wheel,null);
            Assert.IsFalse(wheel.IsOpen,"Clear selection closes stale wheel.");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            Assert.IsFalse(transforms.Any(t=>t.name=="WheelUnitCard"));
            var frame=(RectTransform)transforms.Single(t=>t.name=="PortraitFrame");
            var opener=(RectTransform)selection.CommandWheelOpenButton.transform;
            Assert.IsNull(frame.GetComponent<Button>());
            Assert.AreSame(frame.parent,opener.parent);
            Assert.GreaterOrEqual(-opener.anchoredPosition.y, -frame.anchoredPosition.y+frame.rect.height+8);
            Assert.GreaterOrEqual(opener.rect.height,72);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
