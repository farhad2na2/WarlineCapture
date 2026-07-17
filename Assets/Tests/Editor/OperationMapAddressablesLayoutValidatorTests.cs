using Game.Editor;
using NUnit.Framework;

public sealed class OperationMapAddressablesLayoutValidatorTests
{
    [Test]
    public void CurrentCompatibilityLayout_ValidatesWithoutInventingMinimapRaster()
    {
        Assert.That(OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(
            false,
            out string error), Is.True, error);
    }

    [Test]
    public void StrictLayout_FailsClosedOnMissingMinimapRaster()
    {
        Assert.That(OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(
            true,
            out string error), Is.False);
        StringAssert.Contains(OperationMapAddressablesLayoutValidator.MinimapRasterAddress, error);
    }
}
