using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>Read-only audit of every test in the nine production architecture fixtures.</summary>
public static class MissionReadinessArchitectureValidation
{
    public static void Run()
    {
        Type[] fixtures =
        {
            typeof(ProductionSourceGrowthArchitectureTests),
            typeof(ScriptArchitectureAlignmentContractTests),
            typeof(EcsBurstHotPathArchitectureTests),
            typeof(NonUiSystemBaseMigrationArchitectureTests),
            typeof(NonEcsSystemConversionArchitectureTests),
            typeof(ResourceExchangeArchitectureGuardrailTests),
            typeof(TacticalResourceOwnershipArchitectureTests),
            typeof(M01FirstContactBurstAotArchitectureTests),
            typeof(FirstLaunchArchitectureAlignmentTests),
        };
        int passed = 0;
        int failed = 0;
        foreach (Type fixture in fixtures)
        {
            MethodInfo[] methods = fixture.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
            // These source/ownership fixtures have no NUnit lifecycle or parameterized cases.
            // Fail closed if that changes instead of silently omitting setup or test cases.
            if (methods.Any(method => method.GetCustomAttributes(true).Any(attribute =>
                attribute is SetUpAttribute or TearDownAttribute or OneTimeSetUpAttribute or
                    OneTimeTearDownAttribute or TestCaseAttribute or TestCaseSourceAttribute)))
                throw new InvalidOperationException("Use the NUnit runner for changed fixture: " + fixture.Name);
            MethodInfo[] tests = methods.Where(method => method.IsDefined(typeof(TestAttribute), true))
                .OrderBy(method => method.MetadataToken).ToArray();
            if (tests.Length == 0)
                throw new InvalidOperationException("No tests discovered: " + fixture.Name);
            foreach (MethodInfo test in tests)
            {
                string name = fixture.Name + "." + test.Name;
                try
                {
                    if (test.ReturnType != typeof(void) || test.GetParameters().Length != 0 ||
                        test.IsDefined(typeof(IgnoreAttribute), true))
                        throw new InvalidOperationException("Unsupported test shape: " + name);
                    test.Invoke(test.IsStatic ? null : Activator.CreateInstance(fixture), null);
                    passed++;
                    Debug.Log("[MissionReadinessArchitecture] pass=" + name);
                }
                catch (Exception error)
                {
                    failed++;
                    Debug.LogError("[MissionReadinessArchitecture] fail=" + name + " " +
                        (error is TargetInvocationException invocation ? invocation.InnerException : error));
                }
            }
        }
        Debug.Log($"[MissionReadinessArchitecture] result={(failed == 0 ? "Passed" : "Failed")} fixtures={fixtures.Length} passed={passed} failed={failed}");
        ValidationExit.Exit(failed == 0 ? 0 : 1);
    }
}
