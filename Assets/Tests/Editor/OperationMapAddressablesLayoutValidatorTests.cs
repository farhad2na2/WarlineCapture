using Game.Editor;
using NUnit.Framework;

public sealed class OperationMapAddressablesLayoutValidatorTests
{
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
