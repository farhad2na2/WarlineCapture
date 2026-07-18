using Game.Configs;
using NUnit.Framework;

public sealed class OperationMapContentAddressContractTests
{
    [Test]
    public void PresentationChunkAddress_MatchesApprovedLocalLayout()
    {
        Assert.That(OperationMapContentAddressContract.TryBuildPresentationChunkAddress(
            "opmap.skirmish.desert_base_01",
            "chunk_-12_34",
            out string address,
            out string error), Is.True, error);
        Assert.That(address, Is.EqualTo(
            "operation-map/opmap.skirmish.desert_base_01/presentation/chunk_-12_34"));
    }

    [TestCase(null, "chunk_0_0")]
    [TestCase("opmap.bad", "chunk_0_0")]
    [TestCase("opmap.skirmish.desert_base_01", null)]
    [TestCase("opmap.skirmish.desert_base_01", "chunk/0/0")]
    [TestCase("opmap.skirmish.desert_base_01", "chunk 0 0")]
    public void PresentationChunkAddress_RejectsInvalidIdentity(
        string operationMapId,
        string chunkId)
    {
        Assert.That(OperationMapContentAddressContract.TryBuildPresentationChunkAddress(
            operationMapId,
            chunkId,
            out string address,
            out string error), Is.False);
        Assert.That(address, Is.Null);
        Assert.That(error, Is.Not.Empty);
    }
}
