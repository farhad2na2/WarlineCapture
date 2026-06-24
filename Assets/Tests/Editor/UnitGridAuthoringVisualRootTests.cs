#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class UnitGridAuthoringVisualRootTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitGridAuthoringVisualRootTests();
            tests.UnitGridAuthoringKeepsExplicitVisualRootsWithCompatibilityFallbacks();
            tests.UnitPrefabRenderAuditCanReportMissingExplicitVisualRoots();
            Debug.Log("[UnitGridAuthoringVisualRootValidation] result=Passed tests=2");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UnitGridAuthoringVisualRootValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void UnitGridAuthoringKeepsExplicitVisualRootsWithCompatibilityFallbacks()
    {
        string source = File.ReadAllText("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs");

        StringAssert.Contains("[SerializeField] private Transform modelRoot;", source);
        StringAssert.Contains("[SerializeField] private Transform destroyedRoot;", source);
        StringAssert.Contains("authoring.modelRoot != null ? authoring.modelRoot : authoring.transform.Find(\"Model\")", source);
        StringAssert.Contains("authoring.destroyedRoot != null ? authoring.destroyedRoot : authoring.transform.Find(\"Destroyed\")", source);
    }

    [Test]
    public void UnitPrefabRenderAuditCanReportMissingExplicitVisualRoots()
    {
        string source = File.ReadAllText("Assets/Game/Scripts/Editor/UnitPrefabRenderAudit.cs");

        StringAssert.Contains("Report Missing Unit Visual Root References", source);
        StringAssert.Contains("serialized.FindProperty(\"modelRoot\")", source);
        StringAssert.Contains("serialized.FindProperty(\"destroyedRoot\")", source);
        StringAssert.Contains("missingModelRoot", source);
        StringAssert.Contains("missingDestroyedRoot", source);
    }
}
#endif
