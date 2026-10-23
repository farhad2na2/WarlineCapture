#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Game.Tests.Editor
{
    public static class SkirmishOfflineDataExportTests
    {
        public static int RunChecks()
        { CanonicalCoverage(); OutputIsolation(); CompleteAuthoredGrid(); NativeCoordinates(); PayloadAndDrift(); return 5; }
        [Test] public static void CanonicalCoverage()
        {
            Assert.IsTrue(SkirmishOfflineDataExporter.Canonical("S001"));Assert.IsTrue(SkirmishOfflineDataExporter.Canonical("S120"));
            foreach(string id in new[]{null,"S000","S121","S01","s001","S+01"})Assert.IsFalse(SkirmishOfflineDataExporter.Canonical(id));
        }
        [Test] public static void OutputIsolation()
        {
            Assert.Throws<ArgumentException>(()=>SkirmishOfflineDataExporter.ValidateOutputDirectory("Assets/UnsafeExport"));
            Assert.Throws<ArgumentException>(()=>SkirmishOfflineDataExporter.ValidateOutputDirectory("Logs/../Assets/UnsafeExport"));
            Assert.Throws<ArgumentException>(()=>SkirmishOfflineDataExporter.ValidateOutputDirectory("Logs"));
            Assert.DoesNotThrow(()=>SkirmishOfflineDataExporter.ValidateOutputDirectory(Path.Combine(Path.GetTempPath(),"skirmish-export-test-"+Guid.NewGuid().ToString("N"))));
        }
        [Test] public static void CompleteAuthoredGrid()
        {
            var grid=ScriptableObject.CreateInstance<GridAuthoringConfig>();
            try
            {
                var fields=new SerializedObject(grid);fields.FindProperty("width").intValue=71;fields.FindProperty("height").intValue=83;
                var blocked=fields.FindProperty("blockedCells");blocked.arraySize=4;
                blocked.GetArrayElementAtIndex(0).vector2IntValue=new Vector2Int(70,82);
                blocked.GetArrayElementAtIndex(1).vector2IntValue=new Vector2Int(0,0);
                blocked.GetArrayElementAtIndex(2).vector2IntValue=new Vector2Int(-1,5);
                blocked.GetArrayElementAtIndex(3).vector2IntValue=new Vector2Int(71,4);
                fields.ApplyModifiedPropertiesWithoutUndo();
                var bytes=SkirmishOfflineDataExporter.BuildAuthoredWalkable(grid);
                Assert.AreEqual(71*83,bytes.Length);Assert.AreEqual(0,bytes[0]);Assert.AreEqual(0,bytes[bytes.Length-1]);Assert.AreEqual(bytes.Length-2,bytes.Count(b=>b==1));
                fields.FindProperty("width").intValue=int.MaxValue;fields.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<ArgumentException>(()=>SkirmishOfflineDataExporter.BuildAuthoredWalkable(grid));
            }
            finally{UnityEngine.Object.DestroyImmediate(grid);}
        }
        [Test] public static void NativeCoordinates()
        {
            var map=new OfflineMap {authored_grid_available=true,width=71,height=83,origin_x=-4,origin_z=8,cell_size=2};
            var start=SkirmishOfflineDataExporter.Point(map,"a","unknown",-4,8,"test");Assert.IsTrue(start.in_bounds);Assert.AreEqual(0,start.cell_x);Assert.AreEqual(0,start.cell_z);
            var edge=SkirmishOfflineDataExporter.Point(map,"a","unknown",138,174,"test");Assert.IsFalse(edge.in_bounds);Assert.AreEqual(71,edge.cell_x);Assert.AreEqual(83,edge.cell_z);
            Assert.IsFalse(SkirmishOfflineDataExporter.Point(null,"a","unknown",0,0,"unavailable").has_cell);
        }
        [Test] public static void PayloadAndDrift()
        {
            string path=Path.Combine(Path.GetTempPath(),"skirmish-pin-"+Guid.NewGuid().ToString("N"));
            try
            {
                Assert.AreEqual("missing",SkirmishOfflineDataExporter.ReadPin(path).status);
                File.WriteAllText(path,"");Assert.AreEqual("empty",SkirmishOfflineDataExporter.ReadPin(path).status);
                File.WriteAllText(path,"version https://git-lfs.github.com/spec/v1\noid sha256:abc\nsize 8\n");Assert.AreEqual("lfs_pointer",SkirmishOfflineDataExporter.ReadPin(path).status);
                File.WriteAllText(path,"actual data");var before=SkirmishOfflineDataExporter.ReadPin(path);Assert.AreEqual("complete",before.status);Assert.IsTrue(SkirmishOfflineDataExporter.SamePin(before,SkirmishOfflineDataExporter.ReadPin(path)));
                File.WriteAllText(path,"changed data");Assert.IsFalse(SkirmishOfflineDataExporter.SamePin(before,SkirmishOfflineDataExporter.ReadPin(path)));
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }
        public static void RunFocusedValidation()
        {
            int checks=RunChecks()+SkirmishOfflineCampaignReferencesTests.RunChecks();
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-skirmishOfflineExport");int catalogAt=Array.IndexOf(args,"-skirmishOfflineCatalog");
            if(at<0 || at+1>=args.Length)throw new ArgumentException("Explicit new -skirmishOfflineExport directory required");
            string directory=args[at+1];var report=SkirmishOfflineDataExporter.Export(directory,catalogAt>=0&&catalogAt+1<args.Length?args[catalogAt+1]:null);
            var roundtrip=JsonUtility.FromJson<OfflineExportManifest>(File.ReadAllText(Path.Combine(directory,"manifest.json")));
            Assert.AreEqual(report.missions.Length,roundtrip.missions.Length);
            for(int m=0;m<report.missions.Length;m++) for(int n=0;n<report.missions[m].variants.Length;n++)
            {
                var before=report.missions[m].variants[n];var after=roundtrip.missions[m].variants[n];
                Assert.AreEqual(before.compile_success,after.compile_success);Assert.AreEqual(before.static_budget_available,after.static_budget_available);Assert.AreEqual(before.static_budget_exact,after.static_budget_exact);
                Assert.AreEqual(before.forces.Length,after.forces.Length);Assert.AreEqual(before.structures.Length,after.structures.Length);
                for(int i=0;i<before.forces.Length;i++){Assert.AreEqual(before.forces[i].Quantity,after.forces[i].Quantity);Assert.AreEqual(before.forces[i].RoleId,after.forces[i].RoleId);Assert.AreEqual(before.forces[i].SpawnWorldX,after.forces[i].SpawnWorldX);Assert.AreEqual(before.forces[i].SpawnWorldZ,after.forces[i].SpawnWorldZ);}
                if(before.setup!=null){Assert.NotNull(after.setup);Assert.AreEqual(before.setup.SetupHash,after.setup.SetupHash);Assert.AreEqual(before.setup.PlayerCombat,after.setup.PlayerCombat);Assert.AreEqual(before.setup.EnemyCombat,after.setup.EnemyCombat);Assert.AreEqual(before.setup.RoleOverlays.Length,after.setup.RoleOverlays.Length);Assert.AreEqual(before.setup.Forces.Length,after.setup.Forces.Length);}
            }
            Assert.AreEqual(120,report.missions.Length);Assert.AreEqual(120,report.missions.Select(m=>m.canonical_id).Distinct().Count());
            foreach(var mission in report.missions){Assert.AreEqual(12,mission.variants.Length);Assert.AreEqual(12,mission.variants.Select(v=>v.size+"/"+v.difficulty).Distinct().Count());foreach(var v in mission.variants)Assert.AreEqual(v.compile_success,v.setup!=null);}
            for(int i=0;i<report.maps.Length;i++){Assert.NotNull(roundtrip.maps[i].metadata);Assert.AreEqual(report.maps[i].metadata.bounds.WorldMin,roundtrip.maps[i].metadata.bounds.WorldMin);Assert.AreEqual(report.maps[i].metadata.bounds.WorldMax,roundtrip.maps[i].metadata.bounds.WorldMax);Assert.AreEqual(report.maps[i].metadata.grid.AssetGuid,roundtrip.maps[i].metadata.grid.AssetGuid);Assert.IsTrue(JsonUtility.ToJson(roundtrip.maps[i].metadata).Contains("worldMin"));}
            foreach(var artifact in report.artifacts){string path=Path.Combine(directory,artifact.path);Assert.AreEqual(artifact.bytes,new FileInfo(path).Length);Assert.AreEqual((long)artifact.count*artifact.stride,artifact.bytes);Assert.AreEqual(artifact.sha256,SkirmishOfflineDataExporter.HashFile(path));}
            foreach(var pin in report.source_pins)Assert.IsTrue(SkirmishOfflineDataExporter.SamePin(pin,SkirmishOfflineDataExporter.ReadPin(pin.path)),pin.path);
            var receipt=JsonUtility.FromJson<OfflineExportReceipt>(File.ReadAllText(Path.Combine(directory,"receipt.json")));Assert.IsTrue(receipt.source_pins_unchanged);Assert.AreEqual(receipt.manifest_sha256,SkirmishOfflineDataExporter.HashFile(Path.Combine(directory,"manifest.json")));
            var watch=System.Diagnostics.Stopwatch.StartNew();var repeat=SkirmishOfflineDataExporter.Export(directory+"-repeat",catalogAt>=0&&catalogAt+1<args.Length?args[catalogAt+1]:null);
            string firstHash=SkirmishOfflineDataExporter.HashFile(Path.Combine(directory,"manifest.json")),repeatHash=SkirmishOfflineDataExporter.HashFile(Path.Combine(directory+"-repeat","manifest.json"));
            Assert.AreEqual(firstHash,repeatHash,"Deterministic manifest bytes");Assert.AreEqual(SkirmishOfflineDataExporter.HashFile(Path.Combine(directory,"receipt.json")),SkirmishOfflineDataExporter.HashFile(Path.Combine(directory+"-repeat","receipt.json")),"Deterministic receipt bytes");
            Assert.AreEqual(report.artifacts.Length,repeat.artifacts.Length);for(int i=0;i<report.artifacts.Length;i++){Assert.AreEqual(report.artifacts[i].path,repeat.artifacts[i].path);Assert.AreEqual(report.artifacts[i].sha256,repeat.artifacts[i].sha256);}
            Debug.Log("[SkirmishOfflineDataExportTests] repeat_manifest_sha256="+repeatHash+" first_manifest_sha256="+firstHash+" repeat_seconds="+watch.Elapsed.TotalSeconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            Debug.Log("[SkirmishOfflineDataExportTests] result=Passed checks="+checks+" canonical=120 variants=1440 maps="+report.maps.Length);
            Debug.Log(SkirmishOfflineDataExporter.PassMarker);
        }
    }
}
#endif
