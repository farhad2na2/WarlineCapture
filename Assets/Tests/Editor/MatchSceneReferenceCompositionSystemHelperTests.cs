using Game.Composition;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class MatchSceneReferenceCompositionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(nameof(RegisterAndResolve_ReturnsExactView), test => test.RegisterAndResolve_ReturnsExactView(), ref passed);
            RunValidationStep(nameof(SeparateWorlds_DoNotShareView), test => test.SeparateWorlds_DoNotShareView(), ref passed);
            RunValidationStep(nameof(ReplacementOwner_CannotBeClearedByPreviousOwner), test => test.ReplacementOwner_CannotBeClearedByPreviousOwner(), ref passed);
            RunValidationStep(nameof(DisposedWorld_DoesNotReturnStaleView), test => test.DisposedWorld_DoesNotReturnStaleView(), ref passed);
            Debug.Log($"[MatchSceneReferenceFocusedValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MatchSceneReferenceFocusedValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void RegisterAndResolve_ReturnsExactView()
    {
        using World world = new("MatchSceneReference-Register");
        MatchSceneView view = CreateView("RegisteredView");
        try
        {
            var references = new MatchSceneReferenceCompositionSystemHelper();
            references.Register(world.EntityManager, view);

            Assert.IsTrue(references.TryGet(world.EntityManager, out MatchSceneView resolved));
            Assert.AreSame(view, resolved);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
    }

    [Test]
    public void SeparateWorlds_DoNotShareView()
    {
        using World firstWorld = new("MatchSceneReference-First");
        using World secondWorld = new("MatchSceneReference-Second");
        MatchSceneView firstView = CreateView("FirstView");
        MatchSceneView secondView = CreateView("SecondView");
        try
        {
            var firstReferences = new MatchSceneReferenceCompositionSystemHelper();
            var secondReferences = new MatchSceneReferenceCompositionSystemHelper();
            firstReferences.Register(firstWorld.EntityManager, firstView);
            secondReferences.Register(secondWorld.EntityManager, secondView);

            Assert.IsTrue(firstReferences.TryGet(firstWorld.EntityManager, out MatchSceneView resolvedFirst));
            Assert.IsTrue(secondReferences.TryGet(secondWorld.EntityManager, out MatchSceneView resolvedSecond));
            Assert.AreSame(firstView, resolvedFirst);
            Assert.AreSame(secondView, resolvedSecond);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstView.gameObject);
            UnityEngine.Object.DestroyImmediate(secondView.gameObject);
        }
    }

    [Test]
    public void ReplacementOwner_CannotBeClearedByPreviousOwner()
    {
        using World world = new("MatchSceneReference-Replacement");
        MatchSceneView firstView = CreateView("FirstOwner");
        MatchSceneView replacementView = CreateView("ReplacementOwner");
        try
        {
            var firstReferences = new MatchSceneReferenceCompositionSystemHelper();
            var replacementReferences = new MatchSceneReferenceCompositionSystemHelper();
            firstReferences.Register(world.EntityManager, firstView);
            replacementReferences.Register(world.EntityManager, replacementView);

            firstReferences.Clear(world.EntityManager, firstView);

            Assert.IsTrue(replacementReferences.TryGet(world.EntityManager, out MatchSceneView resolved));
            Assert.AreSame(replacementView, resolved);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstView.gameObject);
            UnityEngine.Object.DestroyImmediate(replacementView.gameObject);
        }
    }

    [Test]
    public void DisposedWorld_DoesNotReturnStaleView()
    {
        World firstWorld = new("MatchSceneReference-Disposed");
        MatchSceneView firstView = CreateView("DisposedView");
        MatchSceneView replacementView = CreateView("ReplacementView");
        var references = new MatchSceneReferenceCompositionSystemHelper();
        try
        {
            references.Register(firstWorld.EntityManager, firstView);
            firstWorld.Dispose();

            using World replacementWorld = new("MatchSceneReference-NewWorld");
            Assert.IsFalse(references.TryGet(replacementWorld.EntityManager, out _));
            references.Register(replacementWorld.EntityManager, replacementView);
            Assert.IsTrue(references.TryGet(replacementWorld.EntityManager, out MatchSceneView resolved));
            Assert.AreSame(replacementView, resolved);
        }
        finally
        {
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            UnityEngine.Object.DestroyImmediate(firstView.gameObject);
            UnityEngine.Object.DestroyImmediate(replacementView.gameObject);
        }
    }

    private static MatchSceneView CreateView(string name)
    {
        return new GameObject(name).AddComponent<MatchSceneView>();
    }

    private static void RunValidationStep(
        string name,
        Action<MatchSceneReferenceCompositionSystemHelperTests> action,
        ref int passed)
    {
        var tests = new MatchSceneReferenceCompositionSystemHelperTests();
        action(tests);
        passed++;
    }
}
#endif
