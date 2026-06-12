#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class EcsBurstSelectionCommandValidationRunner
{
    private static readonly Type[] FixtureTypes =
    {
        typeof(RtsSelectionInputSystemTests),
        typeof(UnitMoveOrderSystemTests),
        typeof(SelectionStateSystemTests),
        typeof(FocusableUnitLookupSystemTests),
        typeof(SelectionUiQuerySystemTests),
        typeof(SelectionOrderMarkerSystemTests),
        typeof(MatchHudCommandFeedbackPanelTests),
        typeof(MatchHudCommandControlsCurrentPrefabTests),
        typeof(ScanIntelCommandSystemTests),
    };

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            foreach (Type fixtureType in FixtureTypes)
                passed += RunFixture(fixtureType);

            Debug.Log($"[EcsBurstSelectionCommandValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(Unwrap(exception));
            Debug.LogError($"[EcsBurstSelectionCommandValidation] result=Failed passed={passed}");
            EditorApplication.Exit(1);
        }
    }

    private static int RunFixture(Type fixtureType)
    {
        MethodInfo[] setupMethods = GetLifecycleMethods<SetUpAttribute>(fixtureType);
        MethodInfo[] teardownMethods = GetLifecycleMethods<TearDownAttribute>(fixtureType);
        MethodInfo[] testMethods = fixtureType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.GetCustomAttributes(typeof(TestAttribute), inherit: true).Length > 0)
            .OrderBy(method => method.MetadataToken)
            .ToArray();

        int passed = 0;
        foreach (MethodInfo testMethod in testMethods)
        {
            object fixture = Activator.CreateInstance(fixtureType);
            try
            {
                InvokeAll(fixture, setupMethods);
                testMethod.Invoke(fixture, Array.Empty<object>());
                passed++;
            }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException(
                    $"{fixtureType.Name}.{testMethod.Name} failed.",
                    Unwrap(exception));
            }
            finally
            {
                try
                {
                    InvokeAll(fixture, teardownMethods);
                }
                catch (Exception teardownException)
                {
                    Debug.LogException(Unwrap(teardownException));
                }
            }
        }

        Debug.Log($"[EcsBurstSelectionCommandValidation] fixture={fixtureType.Name} tests={passed}");
        return passed;
    }

    private static MethodInfo[] GetLifecycleMethods<TAttribute>(Type fixtureType)
        where TAttribute : Attribute
    {
        List<MethodInfo> methods = new();
        for (Type current = fixtureType; current != null && current != typeof(object); current = current.BaseType)
        {
            methods.AddRange(
                current
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(method => method.GetCustomAttributes(typeof(TAttribute), inherit: true).Length > 0));
        }

        methods.Reverse();
        return methods.ToArray();
    }

    private static void InvokeAll(object fixture, IReadOnlyList<MethodInfo> methods)
    {
        for (int i = 0; i < methods.Count; i++)
            methods[i].Invoke(fixture, Array.Empty<object>());
    }

    private static Exception Unwrap(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: { } inner }
            ? Unwrap(inner)
            : exception;
    }
}
#endif
