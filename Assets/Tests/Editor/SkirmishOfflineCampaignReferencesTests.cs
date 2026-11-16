#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.IO;
using Game.Configs;
using Game.Editor.SkirmishOffline;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SkirmishOfflineCampaignReferencesTests
    {
        private const string Script = "11111111111111111111111111111111";
        private const string Missions = "22222222222222222222222222222222";
        private const string Maps = "33333333333333333333333333333333";
        private static string Component(long id = 17) => "--- !u!114 &" + id + "\nMonoBehaviour:\n  m_Enabled: 1\n  m_Script: {fileID: 11500000, guid: " + Script + ", type: 3}\n  campaignMissionCatalog: {fileID: 11400000, guid: " + Missions + ", type: 2}\n  campaignOperationMapCatalog: {fileID: 11400001, guid: " + Maps + ", type: 2}\n";
        public static int RunChecks()
        {
            var tests = new SkirmishOfflineCampaignReferencesTests();
            tests.ExactOwnerAndReferenceIdentity(); tests.RejectsAmbiguousOrMalformedAuthority();
            tests.CurrentMenuLoadsExactReferencedAssets(); tests.AssetLoadRejectsWrongTypeAndLocalId();
            Debug.Log("[SkirmishOfflineCampaignReferences] result=Passed checks=4 sourceMutation=0 missionReady=0"); return 4;
        }
        [Test] public void ExactOwnerAndReferenceIdentity()
        {
            string unrelated = Component(3).Replace(Script, "44444444444444444444444444444444");
            foreach (string text in new[] { unrelated + Component(), (unrelated + Component()).Replace("\n", "\r\n") })
            {
                var result = SkirmishOfflineCampaignReferences.Parse(text, Script);
                Assert.AreEqual(17, result.BootstrapFileId); Assert.AreEqual(Missions, result.MissionCatalogGuid);
                Assert.AreEqual(Maps, result.OperationMapCatalogGuid); Assert.AreEqual(11400000, result.MissionCatalogFileId);
                Assert.AreEqual(11400001, result.OperationMapCatalogFileId);
            }
        }
        [Test] public void RejectsAmbiguousOrMalformedAuthority()
        {
            string component = Component();
            string[] invalid = {
                component + Component(18), component.Replace("  m_Enabled: 1", "  m_Enabled: 0"),
                component.Replace(" &17", " &17 stripped"), component.Replace("guid: " + Missions, "guid: malformed"),
                component.Replace("campaignMissionCatalog: {fileID: 11400000", "campaignMissionCatalog: {fileID: 0"),
                component.Replace("campaignMissionCatalog:", "differentCatalog:"),
                component + "  campaignOperationMapCatalog: {fileID: 11400001, guid: " + Maps + ", type: 2}\n",
                component.Replace("guid: " + Maps + ", type: 2", "guid: " + Maps + ", type: 3"),
                component.Replace("guid: " + Script, "guid: 44444444444444444444444444444444"),
                "version https://git-lfs.github.com/spec/v1\noid sha256:0000\n"
            };
            foreach (string text in invalid) Assert.Throws<InvalidDataException>(() => SkirmishOfflineCampaignReferences.Parse(text, Script));
            Assert.Throws<InvalidDataException>(() => SkirmishOfflineCampaignReferences.Parse(component, ""));
            // Fields on another component may not complete a partial owner.
            string incomplete = component.Replace("  campaignOperationMapCatalog:", "  absent:");
            Assert.Throws<InvalidDataException>(() => SkirmishOfflineCampaignReferences.Parse(incomplete + Component(18).Replace(Script, Maps), Script));
        }
        [Test] public void CurrentMenuLoadsExactReferencedAssets()
        {
            string before = File.ReadAllText(SkirmishOfflineCampaignReferences.MenuScenePath);
            var result = SkirmishOfflineCampaignReferences.ReadCurrent();
            Assert.NotNull(result.MissionCatalog); Assert.NotNull(result.OperationMapCatalog);
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(result.MissionCatalog, out string missionGuid, out long missionId));
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(result.OperationMapCatalog, out string mapGuid, out long mapId));
            Assert.AreEqual(result.MissionCatalogGuid, missionGuid); Assert.AreEqual(result.MissionCatalogFileId, missionId);
            Assert.AreEqual(result.OperationMapCatalogGuid, mapGuid); Assert.AreEqual(result.OperationMapCatalogFileId, mapId);
            Assert.AreEqual(before, File.ReadAllText(SkirmishOfflineCampaignReferences.MenuScenePath));
        }
        [Test] public void AssetLoadRejectsWrongTypeAndLocalId()
        {
            var result = SkirmishOfflineCampaignReferences.ReadCurrent();
            Assert.Throws<InvalidDataException>(() => SkirmishOfflineCampaignReferences.LoadExact<OperationMapCatalogConfig>(result.MissionCatalogGuid, result.MissionCatalogFileId, out _));
            Assert.Throws<InvalidDataException>(() => SkirmishOfflineCampaignReferences.LoadExact<MissionDefinitionCatalogConfig>(result.MissionCatalogGuid, long.MaxValue, out _));
            Assert.Throws<InvalidDataException>(() => SkirmishOfflineCampaignReferences.LoadExact<MissionDefinitionCatalogConfig>("00000000000000000000000000000000", result.MissionCatalogFileId, out _));
        }
    }
}
#endif
