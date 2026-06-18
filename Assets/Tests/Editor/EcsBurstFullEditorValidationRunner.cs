#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class EcsBurstFullEditorValidationRunner
{
    private const string TestAssemblyName = "Game.Tests.Editor";
    private static readonly HashSet<string> RequiresUnityTestRunnerLogScopeFixtures = new(StringComparer.Ordinal)
    {
        nameof(AIBuildPlannerValidationTests),
        nameof(AICombatOrderValidationTests),
        nameof(AIControlModeValidationTests),
        nameof(AIEconomyValidationTests),
        nameof(AIEndToEndValidationTests),
        nameof(AIProductionValidationTests),
        nameof(AISquadValidationTests),
        nameof(AITargetingValidationTests),
        nameof(MatchHudMinimapProjectionSystemTests),
    };

    public static void RunAllNonExplicitTests()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        int passed = 0;
        int skipped = 0;
        try
        {
            foreach (Type fixtureType in EnumerateTestFixtures())
            {
                if (RequiresUnityTestRunnerLogScopeFixtures.Contains(fixtureType.Name))
                {
                    int skippedFixtureTests = CountDeclaredTestMethods(fixtureType);
                    skipped += skippedFixtureTests;
                    Debug.Log($"[EcsBurstFullEditorValidation] fixture={fixtureType.FullName} skipped={skippedFixtureTests} reason=RequiresUnityTestRunnerLogScope");
                    continue;
                }

                FixtureResult result = RunFixture(fixtureType);
                passed += result.Passed;
                skipped += result.Skipped;
            }

            Debug.Log($"[EcsBurstFullEditorValidation] result=Passed tests={passed} skipped={skipped}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(Unwrap(exception));
            Debug.LogError($"[EcsBurstFullEditorValidation] result=Failed passed={passed} skipped={skipped}");
            ValidationExit.Exit(1);
        }
    }

    private static IEnumerable<Type> EnumerateTestFixtures()
    {
        Assembly editorTestAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, TestAssemblyName, StringComparison.Ordinal));

        if (editorTestAssembly == null)
            throw new InvalidOperationException($"Could not find loaded editor test assembly '{TestAssemblyName}'.");

        return editorTestAssembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && HasRunnableTestMethods(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasRunnableTestMethods(Type fixtureType)
    {
        return fixtureType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(IsRunnableTestMethod);
    }

    private static int CountDeclaredTestMethods(Type fixtureType)
    {
        return fixtureType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Count(method => method.GetCustomAttributes(typeof(TestAttribute), inherit: true).Length > 0);
    }

    private static FixtureResult RunFixture(Type fixtureType)
    {
        MethodInfo[] setupMethods = GetLifecycleMethods<SetUpAttribute>(fixtureType, baseToDerived: true);
        MethodInfo[] teardownMethods = GetLifecycleMethods<TearDownAttribute>(fixtureType, baseToDerived: false);
        MethodInfo[] testMethods = fixtureType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.GetCustomAttributes(typeof(TestAttribute), inherit: true).Length > 0)
            .OrderBy(method => method.MetadataToken)
            .ToArray();

        int passed = 0;
        int skipped = 0;
        foreach (MethodInfo testMethod in testMethods)
        {
            if (!IsRunnableTestMethod(testMethod))
            {
                skipped++;
                continue;
            }

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
                    $"{fixtureType.FullName}.{testMethod.Name} failed.",
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

        Debug.Log($"[EcsBurstFullEditorValidation] fixture={fixtureType.FullName} passed={passed} skipped={skipped}");
        return new FixtureResult(passed, skipped);
    }

    private static bool IsRunnableTestMethod(MethodInfo method)
    {
        return method.GetParameters().Length == 0 &&
               method.GetCustomAttributes(typeof(TestAttribute), inherit: true).Length > 0 &&
               method.GetCustomAttributes(typeof(ExplicitAttribute), inherit: true).Length == 0 &&
               method.DeclaringType?.GetCustomAttributes(typeof(ExplicitAttribute), inherit: true).Length == 0;
    }

    private static MethodInfo[] GetLifecycleMethods<TAttribute>(Type fixtureType, bool baseToDerived)
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

        if (baseToDerived)
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

    private readonly struct FixtureResult
    {
        public FixtureResult(int passed, int skipped)
        {
            Passed = passed;
            Skipped = skipped;
        }

        public int Passed { get; }
        public int Skipped { get; }
    }
}
#endif
