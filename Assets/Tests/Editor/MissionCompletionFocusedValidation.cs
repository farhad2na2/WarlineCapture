using System;
using System.Linq;
using System.Reflection;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public static class MissionCompletionFocusedValidation
{
    public static void PrepareAssets()
    {
        MissionCameraBoundsAuthoring.UpdateCommittedMissionMaps();
        V3UiLocalizationCatalogBuilder.RepairMissionResultBindingsAndFont();
        Debug.Log("[MissionCompletionAssets] result=Passed");
    }

    public static void Run()
    {
        RunBehavior();
        MissionReadinessArchitectureValidation.Run();
    }

    public static void RunBehavior()
    {
        MissionCameraBoundsAuthoring.ValidateContentPacks();
        int passed = 0, failed = 0;
        foreach (Type fixture in new[] { typeof(MissionCameraOverviewTests), typeof(UnitAnimationIndexSystemTests), typeof(GameLocalizationCatalogTests) })
        {
            MethodInfo[] methods = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (methods.Any(m => m.GetCustomAttributes(true).Any(a => a is TestCaseAttribute or TestCaseSourceAttribute or OneTimeSetUpAttribute or OneTimeTearDownAttribute)))
                throw new InvalidOperationException("Use NUnit for changed fixture: " + fixture.Name);
            foreach (var test in methods.Where(m => m.IsDefined(typeof(TestAttribute), true)))
            {
                object target = Activator.CreateInstance(fixture);
                try
                {
                    if (test.ReturnType != typeof(void) || test.GetParameters().Length != 0 || test.IsDefined(typeof(IgnoreAttribute), true))
                        throw new InvalidOperationException("Unsupported test: " + test.Name);
                    foreach (var setup in methods.Where(m => m.IsDefined(typeof(SetUpAttribute), true))) setup.Invoke(target, null);
                    test.Invoke(test.IsStatic ? null : target, null);
                    passed++;
                    Debug.Log("[MissionCompletionFocused] pass=" + fixture.Name + "." + test.Name);
                }
                catch (Exception error)
                {
                    failed++;
                    Debug.LogError("[MissionCompletionFocused] fail=" + fixture.Name + "." + test.Name + " " +
                        (error is TargetInvocationException invocation ? invocation.InnerException : error));
                }
                finally
                {
                    foreach (var teardown in methods.Where(m => m.IsDefined(typeof(TearDownAttribute), true))) teardown.Invoke(target, null);
                }
            }
        }
        Debug.Log($"[MissionCompletionFocused] result={(failed == 0 ? "Passed" : "Failed")} passed={passed} failed={failed}");
        if (failed != 0) throw new InvalidOperationException("Mission completion focused regressions failed.");
    }
}
