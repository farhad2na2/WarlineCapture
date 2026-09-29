using System;
using System.IO;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;
using UnityEngine.Scripting;

[assembly: TestRunCallback(typeof(MapPreparationNativeResultCallback))]

[Preserve]
public sealed class MapPreparationNativeResultCallback : ITestRunCallback
{
    private string rootId;
    private string root;
    public void RunStarted(ITest tests)
    {
        if(Application.isEditor) return;
        rootId=tests.Id;
        var directory=new DirectoryInfo(Application.dataPath);
        while(directory!=null && !File.Exists(Path.Combine(directory.FullName,"Design/MapVariants/HANDOFF_Map_Preparation.md"))) directory=directory.Parent;
        if(directory==null) throw new DirectoryNotFoundException("Preparation player must run beneath repository");
        root=directory.FullName;
        Application.runInBackground=true;
        UnityEngine.Profiling.Profiler.enabled=false;
        Debug.Log("[MapPreparationNativeNUnit] started tests="+tests.TestCaseCount);
    }
    public void TestStarted(ITest test) { if(!Application.isEditor) Debug.Log("[MapPreparationNativeNUnit] testStarted="+test.FullName); }
    public void TestFinished(ITestResult result) { if(!Application.isEditor) Debug.Log("[MapPreparationNativeNUnit] testFinished="+result.FullName+" result="+result.ResultState+" message="+result.Message); }
    public void RunFinished(ITestResult result)
    {
        if(Application.isEditor || root==null || result.Test.Id!=rootId) return;
        var document=new System.Xml.XmlDocument();
        var run=document.CreateElement("test-run");document.AppendChild(run);
        run.SetAttribute("result",result.ResultState.Status.ToString());
        run.SetAttribute("total",(result.PassCount+result.FailCount+result.SkipCount+result.InconclusiveCount).ToString());run.SetAttribute("passed",result.PassCount.ToString());run.SetAttribute("failed",result.FailCount.ToString());run.SetAttribute("skipped",result.SkipCount.ToString());
        var node=new System.Xml.XmlDocument();node.LoadXml(result.ToXml(true).OuterXml);run.AppendChild(document.ImportNode(node.DocumentElement,true));
        string output=Path.Combine(root,"Library/MapVariantNativeRun/result.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(output));document.Save(output+".tmp");File.Move(output+".tmp",output);
        Debug.Log("[MapPreparationNativeNUnit] result="+result.ResultState.Status+" tests="+(result.PassCount+result.FailCount+result.SkipCount+result.InconclusiveCount)+" passed="+result.PassCount+" failed="+result.FailCount);
        // The scoped macOS test player crashes in Unity's native
        // AssetBundleAnalytics shutdown after Application.Quit. NUnit has
        // already been written atomically; the Editor watcher checks that XML.
        // Exit this disposable test process without the crashing Unity quit path.
        Environment.Exit(result.FailCount==0 && result.PassCount==(result.PassCount+result.FailCount+result.SkipCount+result.InconclusiveCount) ? 0 : 2);
    }
}
