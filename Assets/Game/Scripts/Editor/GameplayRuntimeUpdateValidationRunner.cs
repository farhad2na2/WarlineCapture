using Game.Runtime;
namespace Game.Editor
{
    #if UNITY_EDITOR
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public static class GameplayRuntimeUpdateValidationRunner
    {
        public static void Run()
        {
            try
            {
                ValidateHelperContract();
                Debug.Log("[GameplayRuntimeUpdateValidation] result=Passed tests=1");
                Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[GameplayRuntimeUpdateValidation] result=Failed");
                Exit(1);
            }
        }

        private static void ValidateHelperContract()
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                "Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs");
            string source = File.ReadAllText(sourcePath);
            if (!source.Contains("public sealed class GameplayRuntimeUpdateCompositionSystemHelper", StringComparison.Ordinal))
                throw new InvalidOperationException("GameplayRuntimeUpdateCompositionSystemHelper must be a plain direct-owned helper.");
            if (source.Contains("GameplayRuntimeUpdateCompositionSystemHelper : SystemBase", StringComparison.Ordinal))
                throw new InvalidOperationException("GameplayRuntimeUpdateCompositionSystemHelper must not derive from SystemBase.");
            if (source.Contains("protected override void OnCreate", StringComparison.Ordinal) ||
                source.Contains("protected override void OnUpdate", StringComparison.Ordinal))
                throw new InvalidOperationException("GameplayRuntimeUpdateCompositionSystemHelper must not keep disabled ECS lifecycle methods.");

            var helper = new GameplayRuntimeUpdateCompositionSystemHelper();
            var runtimeState = new RuntimeGameplayStateSystem();
            var performanceDiagnostics = new PerformanceDiagnosticsSystemHelper();
            bool gameplayStartPending = false;

            helper.Update(
                runtimeWorld: null,
                gameplayInitialized: false,
                runtimeGameplayStateSystem: runtimeState,
                performanceDiagnosticsSystem: performanceDiagnostics,
                roadBuildRuntimeUpdate: null,
                buildingRuntimeUpdate: null,
                buildingRuntimeUpdateContext: default,
                selectionRuntimeUpdate: null,
                worldCamera: null,
                runtimeCity: null,
                runtimeGridBlockers: null,
                runtimeDecorations: null,
                dayNight: null,
                citizenPopulationRuntimeUpdate: null,
                mainMenu: null,
                unitImpostors: null,
                ref gameplayStartPending);
            helper.LateUpdate(false, runtimeState, performanceDiagnostics, null, null);
            helper.OnGui(false, runtimeState, performanceDiagnostics, null, null);
            helper.Dispose();

            if (gameplayStartPending)
                throw new InvalidOperationException("Inactive direct-owned update path must not alter gameplay start pending state.");
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
    #endif
}
