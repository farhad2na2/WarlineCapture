using System.IO;
using System.Text;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.Editor
{
    // The Cargo Truck pins were written on a CRLF checkout; the repository keeps LF. Same content must verify on both,
    // a real content change must not.
    public sealed class SkirmishConvoyPinMatchTests
    {
        private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

        [Test] public void RawLfAndCrlfFormsOfTheSameContentMatchTheirPin()
        {
            byte[] lf = Bytes("a: 1\nb: 2\n"), crlf = Bytes("a: 1\r\nb: 2\r\n");
            Assert.AreEqual("raw", SkirmishConvoyPinMatch.Match(SkirmishConvoyPinMatch.Hash(lf), lf));
            Assert.AreEqual("lf", SkirmishConvoyPinMatch.Match(SkirmishConvoyPinMatch.Hash(lf), crlf));   // pinned on LF, checked on CRLF
            Assert.AreEqual("crlf", SkirmishConvoyPinMatch.Match(SkirmishConvoyPinMatch.Hash(crlf), lf));  // pinned on CRLF, checked on LF
        }

        [Test] public void ChangedContentMatchesNoForm()
        {
            byte[] pinned = Bytes("speed: 5\r\n"), edited = Bytes("speed: 6\n");
            Assert.IsNull(SkirmishConvoyPinMatch.Match(SkirmishConvoyPinMatch.Hash(pinned), edited));
            Assert.IsNull(SkirmishConvoyPinMatch.Match(null, edited));
            Assert.IsNull(SkirmishConvoyPinMatch.Match("not-a-hash", edited));
        }

        [Test] public void PinnedChassisAndConfigVerifyAgainstTheFilesOnDisk()
        {
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishConvoyTruckDefinition>(SkirmishConvoyTruckDefinition.AssetPath);
            Assert.IsNotNull(definition);
            Assert.IsNotNull(SkirmishConvoyPinMatch.Match(definition.ChassisPrefabSha256, File.ReadAllBytes("Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab")),
                "Cargo Truck prefab content differs from its pin");
            Assert.IsNotNull(SkirmishConvoyPinMatch.Match(definition.ChassisConfigSha256, File.ReadAllBytes(SkirmishConvoyTruckDefinition.ExistingConfigPath)),
                "Cargo Truck config content differs from its pin");
        }
    }
}
