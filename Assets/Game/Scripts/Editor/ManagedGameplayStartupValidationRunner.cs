namespace Game.Editor
{
    #if UNITY_EDITOR
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public static class ManagedGameplayStartupValidationRunner
    {
        public static void Run()
        {
            try
            {
                string sourcePath = Path.Combine(
                    Application.dataPath,
                    "Game/Scripts/Systems/ManagedGameplayStartupSystemHelper.cs");
                string source = File.ReadAllText(sourcePath);
                Require(source.Contains("internal sealed class ManagedGameplayStartupSystemHelper", StringComparison.Ordinal),
                    "ManagedGameplayStartupSystemHelper must be a plain direct-owned helper.");
                Require(!source.Contains("ManagedGameplayStartupSystemHelper : SystemBase", StringComparison.Ordinal),
                    "ManagedGameplayStartupSystemHelper must not derive from SystemBase.");
                Require(!source.Contains("protected override void OnCreate", StringComparison.Ordinal) &&
                        !source.Contains("protected override void OnUpdate", StringComparison.Ordinal),
                    "ManagedGameplayStartupSystemHelper must not keep disabled ECS lifecycle methods.");
                Require(source.Contains("public Result Initialize(", StringComparison.Ordinal),
                    "ManagedGameplayStartupSystemHelper must keep its direct Initialize API.");
                Require(source.Contains("private static DayNightSystem ResolveDayNightSystem()", StringComparison.Ordinal),
                    "ManagedGameplayStartupSystemHelper must keep its managed DayNight boundary resolver.");

                Debug.Log("[ManagedGameplayStartupValidation] result=Passed tests=1");
                Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[ManagedGameplayStartupValidation] result=Failed");
                Exit(1);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
    #endif
}
