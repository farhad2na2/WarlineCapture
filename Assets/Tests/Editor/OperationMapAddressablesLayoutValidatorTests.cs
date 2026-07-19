using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapAddressablesLayoutValidatorTests
{
    public static void RunFocusedValidation()
    {
        var layout = new OperationMapAddressablesLayoutBuilderTests();
        var validator = new OperationMapAddressablesLayoutValidatorTests();
        int passed = 0;
        try
        {
            layout.SharedDependencyThresholdCoversEveryCrossBundleDependency(); passed++;
            layout.CurrentLayout_UsesExactLocalOneMapGroupTopology(); passed++;
            layout.SharedShardLabel_IsDeterministicAndBounded(); passed++;
            layout.CurrentDefinition_ReferencesConfiguredHeavyAssetsByGuid(); passed++;
            validator.CurrentCompatibilityLayout_ValidatesWithMapOwnedMinimapRaster(); passed++;
            validator.StrictLayout_ValidatesCompleteLocalPackage(); passed++;
            Debug.Log($"[OperationMapAddressablesLayoutValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[OperationMapAddressablesLayoutValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void CurrentCompatibilityLayout_ValidatesWithMapOwnedMinimapRaster()
    {
        Assert.That(OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(
            false,
            out string error), Is.True, error);
    }

    [Test]
    public void StrictLayout_ValidatesCompleteLocalPackage()
    {
        Assert.That(OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(
            true,
            out string error), Is.True, error);
    }
}
