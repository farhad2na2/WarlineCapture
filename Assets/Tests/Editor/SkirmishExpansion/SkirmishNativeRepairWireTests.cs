#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeRepairWireTests
    {
        // Independent captured wire1 shape, preserving original field order.
        [Serializable] private sealed class OldWire {public int Version;public string Payload,Checksum;}
        [Serializable] private sealed class NewWire {public int Version;public string Payload,RepairPayload,Checksum;}
        // Independent historical INNER DTO: 81569142d fields and order.
        [Serializable] private sealed class OldInnerV7
        {
            public int Version = 7;
            public string SessionId;
            public string CatalogId;
            public uint SetupHash;
            public string ContentHash;
            public string OperationMapId;
            public string LayoutId;
            public string MapMetadataHash;
            public double WorldTime;
            public float UnityTime;
            public int Frame;
            public SkirmishNativeCheckpointTransientState.Visual[] MissileVisuals = Array.Empty<SkirmishNativeCheckpointTransientState.Visual>();
            public SkirmishNativeCheckpointTransientState.Transient[] MissileStates = Array.Empty<SkirmishNativeCheckpointTransientState.Transient>();
            public SkirmishNativeCheckpointTransientState.DropVisual[] DropVisuals = Array.Empty<SkirmishNativeCheckpointTransientState.DropVisual>();
            public SkirmishNativeCheckpointSupportState.Record[] Support = Array.Empty<SkirmishNativeCheckpointSupportState.Record>();
            public SkirmishNativeCheckpointSnapshotService.Actor[] Actors;
            public SkirmishNativeCheckpointSnapshotService.MapReference[] MapReferences;
            public SkirmishNativeCheckpointSnapshotService.Bank[] Banks;
            public SkirmishNativeCheckpointParts.Part[] SessionParts;
        }
        private static string Sha(string value)
        {using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}

        [Test] public void ActualNoRepairCaptureKeepsOriginalWireOneBytesAndRoundTrip()
        {
            using var world=new World("RepairWireLegacyByteControl");
            var saved=SkirmishNativeCheckpointCodecTests.Capture(world.EntityManager,out var setup,out _);
            Assert.IsNull(saved.Repairs);
            string payload=JsonUtility.ToJson(JsonUtility.FromJson<OldInnerV7>(JsonUtility.ToJson(saved)));
            StringAssert.DoesNotContain("\"Objective\":",payload);
            string expected=JsonUtility.ToJson(new OldWire {Version=1,Payload=payload,Checksum=Sha(payload)});
            string actual=SkirmishNativeCheckpointCodec.Encode(saved,setup);
            Assert.AreEqual(expected,actual);
            Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(actual,setup,out var decoded));Assert.IsNull(decoded.Repairs);
            Assert.IsNull(decoded.Objective);Assert.AreEqual(7,decoded.Version);
            foreach(string injected in new[]{"\"Objective\":null", "\"Obj\\u0065ctive\":{}", "\"Version\":7"})
            {
                string changed="{"+injected+","+payload.Substring(1);
                string wire=JsonUtility.ToJson(new OldWire {Version=1,Payload=changed,Checksum=Sha(changed)});
                Assert.IsFalse(SkirmishNativeCheckpointCodec.TryDecode(wire,setup,out _),"BA7 rejects root objective slots/duplicate keys.");
            }
            string future=payload.Replace("\"Version\":7","\"Version\":8");
            string futureWire=JsonUtility.ToJson(new OldWire {Version=1,Payload=future,Checksum=Sha(future)});
            Assert.IsFalse(SkirmishNativeCheckpointCodec.TryDecode(futureWire,setup,out _),"Missing nonBA objective remains invalid.");
            var em=world.EntityManager;
            using var sessions=em.CreateEntityQuery(typeof(Game.Components.SkirmishExpandedSessionComponent));
            var session=sessions.GetSingletonEntity();var native=em.GetComponentData<Game.Components.SkirmishExpandedSessionComponent>(session);
            native.ObjectiveKind=Game.Skirmish.Contracts.SkirmishObjectiveKind.ConvoyEscort;em.SetComponentData(session,native);
            saved.SessionParts=SkirmishNativeCheckpointParts.Capture(em,session,entity=>entity==session?"$session":null);
            Assert.IsNull(SkirmishNativeCheckpointCodec.Encode(saved,setup),"BA header cannot conceal a nonBA native session.");
        }
        [Test] public void ManagedRepairCarrierDoesNotChangeEitherLegacyInnerSchemaBytes()
        {
            foreach(int version in new[]{7,8})
            {
                var saved=new SkirmishNativeCheckpointSnapshotService.Snapshot {Version=version};
                string original=JsonUtility.ToJson(saved);
                saved.Repairs=new SkirmishNativeRepairCheckpoint.Payload();
                Assert.AreEqual(original,JsonUtility.ToJson(saved));StringAssert.DoesNotContain("Repairs",original);
            }
        }
        [Test] public void NewWireChecksumCoversBothRawJsonValuesAndVersion()
        {
            string valid=SkirmishNativeRepairWireCodec.Encode("{\"Version\":7}",new SkirmishNativeRepairCheckpoint.Payload());
            Assert.IsTrue(SkirmishNativeRepairWireCodec.TryDecode(valid,out string original,out var repairs));Assert.IsNotNull(repairs);
            Assert.AreEqual("{\"Version\":7}",original);
            for(int mutation=0;mutation<4;mutation++)
            {
                var wire=JsonUtility.FromJson<NewWire>(valid);
                if(mutation==0)wire.Payload+=" ";
                if(mutation==1)wire.RepairPayload+=" ";
                if(mutation==2)wire.Checksum=new string('0',64);
                if(mutation==3)wire.Version=1;
                Assert.IsFalse(SkirmishNativeRepairWireCodec.TryDecode(JsonUtility.ToJson(wire),out _,out _));
            }
        }
        [Test] public void OriginalReaderExplicitlyRejectsWireTwo()
        {
            string json=SkirmishNativeRepairWireCodec.Encode("{\"Version\":8}",new SkirmishNativeRepairCheckpoint.Payload());
            var old=JsonUtility.FromJson<OldWire>(json);
            Assert.AreEqual(2,old.Version);Assert.IsFalse(old.Version==1&&!string.IsNullOrEmpty(old.Payload)&&old.Checksum==Sha(old.Payload));
        }
        [Test] public void DescriptorPreservesRepairCarrierButCannotAuthorizeAnInvalidPayload()
        {
            using var world=new World("RepairDescriptorRecognitionOnly");
            var saved=SkirmishNativeCheckpointCodecTests.Capture(world.EntityManager,out var setup,out _);
            string json=SkirmishNativeRepairWireCodec.Encode(SkirmishNativeLegacyBa7Json.Encode(saved),new SkirmishNativeRepairCheckpoint.Payload());
            string path=Path.Combine(Path.GetTempPath(),"repair-descriptor-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                File.WriteAllText(path,json,new UTF8Encoding(false));
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryReadDescriptor(path,out var descriptor));Assert.IsNotNull(descriptor.Repairs);
                Assert.IsFalse(SkirmishNativeCheckpointCodec.TryRead(path,setup,out _));
                Assert.IsFalse(SkirmishNativeCheckpointCodec.TryDecode(json,setup,out _));
            }
            finally{if(File.Exists(path))File.Delete(path);}
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishNativeRepairWireTests();t.ActualNoRepairCaptureKeepsOriginalWireOneBytesAndRoundTrip();
            t.ManagedRepairCarrierDoesNotChangeEitherLegacyInnerSchemaBytes();t.NewWireChecksumCoversBothRawJsonValuesAndVersion();
            t.OriginalReaderExplicitlyRejectsWireTwo();t.DescriptorPreservesRepairCarrierButCannotAuthorizeAnInvalidPayload();
            Debug.Log("[SkirmishNativeRepairWireTests] result=Passed tests=5 normalMissionAcceptance=0");
        }
    }
}
#endif
