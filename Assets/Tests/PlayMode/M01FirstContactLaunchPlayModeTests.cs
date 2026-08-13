#if UNITY_INCLUDE_TESTS
using System.Collections;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine.TestTools;

public sealed class M01FirstContactLaunchPlayModeTests
{
    [UnityTest]
    public IEnumerator InvalidRequestRemainsFailClosedAcrossAFrame()
    {
        CampaignMissionLaunchRequestElement request = default;
        CampaignMissionCatalogComponent catalog = default;
        ActiveOperationMapComponent map = default;
        Assert.That(CampaignMissionLaunchSystem.TryValidate(in request, in catalog, in map, out _), Is.False);
        yield return null;
    }

    [UnityTest]
    public IEnumerator InvalidCatalogRemainsUnownedAcrossAFrame()
    {
        CampaignMissionCatalogComponent catalog = default;
        Assert.That(catalog.OwnsBlob, Is.Zero);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RuntimeOwnerRemainsUnmanagedAcrossAFrame()
    {
        Assert.That(UnsafeUtility.IsUnmanaged<CampaignMissionRuntimeComponent>(), Is.True);
        yield return null;
    }
}
#endif
