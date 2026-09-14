using System;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TutorialAttentionAndContinueTests
{
    [Test]
    public void PointerRotatesInsideEveryEdgeAndAvoidsOtherControls()
    {
        foreach (var size in new[] { new Vector2(1334, 750), new Vector2(1920, 1080), new Vector2(2400, 1080) })
        foreach (float labelWidth in new[] { 150f, 300f })
        {
            Rect safe = new(20, 20, size.x - 40, size.y - 40);
            Rect bottom = new(safe.center.x - 80, safe.yMin, 160, 80);
            Rect top = new(safe.center.x - 80, safe.yMax - 80, 160, 80);
            Rect left = new(safe.xMin, safe.center.y - 40, 160, 80);
            Rect right = new(safe.xMax - 160, safe.center.y - 40, 160, 80);
            Check(bottom, safe, labelWidth, null, TutorialPointerSide.Above);
            Check(top, safe, labelWidth, null, TutorialPointerSide.Below);
            // A column of adjacent controls makes above/below unavailable.
            var leftObstacles = new[] { new Rect(left.x, left.yMax, 160, safe.height), new Rect(left.x, safe.yMin, 160, left.yMin - safe.yMin) };
            var rightObstacles = new[] { new Rect(right.x, right.yMax, 160, safe.height), new Rect(right.x, safe.yMin, 160, right.yMin - safe.yMin) };
            Check(left, safe, labelWidth, leftObstacles, TutorialPointerSide.Right);
            Check(right, safe, labelWidth, rightObstacles, TutorialPointerSide.Left);
            Assert.IsFalse(TutorialTapPointerLayout.TryPlace(top, safe, new Vector2(labelWidth, 30), 40, 8, 8,
                new[] { safe }, out _, out _, out _), "No arrow may cover surrounding controls when all sides are blocked.");
        }
    }

    private static void Check(Rect target, Rect safe, float width, Rect[] obstacles, TutorialPointerSide expected)
    {
        Assert.IsTrue(TutorialTapPointerLayout.TryPlace(target, safe, new Vector2(width, 30), 40, 8, 8,
            obstacles, out var arrow, out var caption, out var side));
        Assert.AreEqual(expected, side);
        foreach (float bounce in new[] { -8f, 0f, 8f })
        {
            var animated = arrow; animated.position += TutorialTapPointerLayout.Outward(side) * bounce;
            Assert.That(animated.xMin, Is.GreaterThanOrEqualTo(safe.xMin)); Assert.That(animated.xMax, Is.LessThanOrEqualTo(safe.xMax));
            Assert.That(animated.yMin, Is.GreaterThanOrEqualTo(safe.yMin)); Assert.That(animated.yMax, Is.LessThanOrEqualTo(safe.yMax));
            Assert.IsFalse(animated.Overlaps(target)); Assert.IsFalse(animated.Overlaps(caption));
        }
        Assert.IsFalse(caption.Overlaps(target));
    }

    [Test]
    public void ChevronHasRenderableGeometry()
    {
        var canvas = new GameObject("Pointer mesh QA", typeof(Canvas));
        var pointer = new GameObject("Chevron", typeof(RectTransform), typeof(TutorialChevronGraphic));
        pointer.transform.SetParent(canvas.transform, false);
        try
        {
            Assert.IsNotNull(pointer.GetComponent<CanvasRenderer>(), "A custom Graphic needs an explicit CanvasRenderer requirement.");
            ((RectTransform)pointer.transform).sizeDelta = new Vector2(50, 50);
            using var vertices = new UnityEngine.UI.VertexHelper();
            typeof(TutorialChevronGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Invoke(pointer.GetComponent<TutorialChevronGraphic>(), new object[] { vertices });
            Assert.That(vertices.currentVertCount, Is.GreaterThan(0));
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas); }
    }

    [Test]
    public void ContinueDisappearsBeforeCallbackAndCannotReappearForSameLesson()
    {
        var canvas = new GameObject("Continue regression", typeof(RectTransform), typeof(Canvas));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        var instance = UnityEngine.Object.Instantiate(prefab, canvas.transform);
        try
        {
            var aria = instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
            int clicks = 0;
            aria.BindActions(null, null, () => { Assert.IsFalse(aria.ContinueButton.gameObject.activeSelf); clicks++; });
            var model = Model("Materials lesson"); aria.Apply(model); aria.SetPresentationVisible(true); aria.SetContinueAvailable(true);
            Assert.IsTrue(aria.ContinueButton.IsActive()); aria.ContinueButton.onClick.Invoke();
            Assert.AreEqual(1, clicks); Assert.IsFalse(aria.ContinueButton.IsActive());
            aria.Apply(model); aria.SetContinueAvailable(true); aria.ContinueButton.onClick.Invoke();
            Assert.AreEqual(1, clicks); Assert.IsFalse(aria.ContinueButton.IsActive(), "A repeated read model must not resurrect an acknowledged action.");
            aria.Apply(Model("Next lesson")); aria.SetPresentationVisible(true); aria.SetContinueAvailable(true);
            Assert.IsTrue(aria.ContinueButton.IsActive()); aria.ContinueButton.onClick.Invoke(); Assert.AreEqual(2, clicks);
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas); }
    }

    private static UiAssistantPanelModel Model(string body) => new(1, "", "", "", false, false, true, "Lesson", body, "", "CONTINUE", false, true, false, false, "", "");

    public static void RunAndPlay()
    {
        Run();
        if (ValidationExit.LastExitCode == 0) M02BuildingPlacementEditorProbe.RunContinueEnglish();
    }

    public static void Run()
    {
        try
        {
            V3UiLocalizationCatalogBuilder.ApplyConfiguredUiStrings();
            var tests = new TutorialAttentionAndContinueTests();
            tests.PointerRotatesInsideEveryEdgeAndAvoidsOtherControls();
            tests.ChevronHasRenderableGeometry();
            tests.ContinueDisappearsBeforeCallbackAndCannotReappearForSameLesson();
            using (ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); OperationMapVehicleVisualOwnershipTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0) throw new Exception("Helicopter validation failed");
                ValidationExit.ClearLastExitCode(); M02EstablishBaseGuidanceTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0) throw new Exception("M2 guidance validation failed");
                HudRightColumnLayoutValidation.RunStackedButtons();
                ValidationExit.ClearLastExitCode(); MissionReadinessArchitectureValidation.Run();
                if (ValidationExit.LastExitCode != 0) throw new Exception("Architecture validation failed");
            }
            Debug.Log("[TutorialAttentionAndContinue] result=Passed edges=4 viewports=3 captions=2 immediateConsume,idempotence,nextLesson,helicopter,M2,ARIA");
            ValidationExit.Passed();
        }
        catch (Exception error) { Debug.LogException(error); Debug.Log("[TutorialAttentionAndContinue] result=Failed"); ValidationExit.Failed(); EditorApplication.Exit(1); }
    }
}
