#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
namespace Game.Tests.Editor
{
    public static class SupportSmokeSliceValidation
    {
        public static void RunSupportOnlyValidation()
        {
            using var scope=ValidationExit.SuppressProcessExit();
            try
            {
                Run(SupportConfigValidation.RunFocusedValidation);Run(SupportRuntimeValidation.RunFocusedValidation);
                Run(SupportSmokeValidation.RunFocusedValidation);Run(SupportUiAriaValidation.RunFocusedValidation);
                Debug.Log("[SupportAutomatedValidation] result=Passed readiness=automated-only repositoryArchitecture=separate");ValidationExit.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportAutomatedValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        public static void RunFocusedValidation()
        {
            using var scope=ValidationExit.SuppressProcessExit();
            try
            {
                Run(SupportConfigValidation.RunFocusedValidation);Run(SupportRuntimeValidation.RunFocusedValidation);
                Run(SupportSmokeValidation.RunFocusedValidation);Run(SupportUiAriaValidation.RunFocusedValidation);
                Run(ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation);
                Run(EcsBurstHotPathArchitectureTests.RunFocusedValidation);Run(VehicleFuelConsumptionSystemTests.RunFocusedValidation);
                Debug.Log("[SupportSmokeSliceValidation] result=Passed readiness=automated-only");ValidationExit.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportSmokeSliceValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        private static void Run(Action action)
        {ValidationExit.ClearLastExitCode();action();Assert.AreEqual(0,ValidationExit.LastExitCode,action.Method.DeclaringType?.Name);}
    }
}
#endif
