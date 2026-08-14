#if UNITY_INCLUDE_TESTS
using System.Collections;
using Game.Components;
using Game.Narrative.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.TestTools;

public sealed class M01FirstContactFirstLaunchPlayModeTests
{
    [UnityTest] public IEnumerator EnqueuesOneTypedRequestAcrossFrames()
    {
        using World world = new(nameof(EnqueuesOneTypedRequestAcrossFrames)); EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity(typeof(CampaignMissionRootComponent)); em.AddBuffer<CampaignMissionLaunchRequestElement>(root); em.AddBuffer<CampaignMissionLaunchResultElement>(root);
        PlayerProfileSaveData profile = new(); var payload = FirstLaunchMissionHandoffOperation.Prepare(profile, 41, NarrativeGuidanceMode.Full); bool published = false; byte rejections = 0;
        FirstLaunchMissionHandoffOperation.Advance(em, payload, ref published, ref rejections); yield return null;
        FirstLaunchMissionHandoffOperation.Advance(em, payload, ref published, ref rejections);
        Assert.That(em.GetBuffer<CampaignMissionLaunchRequestElement>(root).Length, Is.EqualTo(1));
    }
}
#endif
