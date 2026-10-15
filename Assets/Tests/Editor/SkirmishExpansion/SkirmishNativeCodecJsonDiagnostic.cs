#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Collects actual Unity serializer facts. A decode refusal is evidence, not a pass.
    public static class SkirmishNativeCodecJsonDiagnostic
    {
        [Serializable] private sealed class Report
        {
            public string Scope="ActualUnityJsonUtility_NoGameplayOrRecoveryAcceptance";
            public string RawPayload, ReserializedPayload, RawWire;
            public int CapturedVersion, DecodedVersion;
            public bool CapturedObjectiveNull, DecodedObjectiveNull;
            public string DecodedObjectiveJson;
            public bool CapturedSchema, DecodedSchema, CapturedValid, DecodedValid, CodecDecode;
        }
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("WARLINE_NATIVE_CODEC_DIAGNOSTIC_OUTPUT");
            if(string.IsNullOrWhiteSpace(output)||!Path.IsPathRooted(output))
                throw new ArgumentException("Fresh absolute persistent diagnostic output required.");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string full=Path.GetFullPath(output);
            if(!full.StartsWith(Path.Combine(root,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Diagnostic output must be under persistent Logs/SkirmishProduction.");
            using var world=new World("NativeCodecActualJsonDiagnostic");
            var captured=SkirmishNativeCheckpointCodecTests.Capture(world.EntityManager,out var setup,out _);
            string payload=JsonUtility.ToJson(captured);
            var decoded=JsonUtility.FromJson<SkirmishNativeCheckpointSnapshotService.Snapshot>(payload);
            var report=new Report {RawPayload=payload,ReserializedPayload=JsonUtility.ToJson(decoded),
                RawWire=SkirmishNativeCheckpointCodec.Encode(captured,setup),CapturedVersion=captured.Version,
                DecodedVersion=decoded.Version,CapturedObjectiveNull=captured.Objective==null,DecodedObjectiveNull=decoded.Objective==null,
                DecodedObjectiveJson=decoded.Objective==null?null:JsonUtility.ToJson(decoded.Objective),
                CapturedSchema=SkirmishNativeObjectiveCheckpoint.Schema(captured.Version,captured.Objective,setup.ObjectiveKind),
                DecodedSchema=SkirmishNativeObjectiveCheckpoint.Schema(decoded.Version,decoded.Objective,setup.ObjectiveKind),
                CapturedValid=SkirmishNativeCheckpointCodec.Validate(captured,setup),
                DecodedValid=SkirmishNativeCheckpointCodec.Validate(decoded,setup)};
            report.CodecDecode=SkirmishNativeCheckpointCodec.TryDecode(report.RawWire,setup,out _);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            using(var stream=new FileStream(full,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(report,true));
                stream.Write(bytes,0,bytes.Length);stream.Flush(true);
            }
            Debug.Log("[SkirmishNativeCodecJsonDiagnostic] result=Collected capturedValid="+report.CapturedValid+
                " decodedValid="+report.DecodedValid+" capturedObjectiveNull="+report.CapturedObjectiveNull+
                " decodedObjectiveNull="+report.DecodedObjectiveNull+" codecDecode="+report.CodecDecode+" gameplayCertified=0");
        }
    }
}
#endif
