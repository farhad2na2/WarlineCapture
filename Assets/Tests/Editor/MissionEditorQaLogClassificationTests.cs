using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class MissionEditorQaLogClassificationTests
{
    [Test]
    public void OnlyTheObservedEditorCloudTokenStackIsSeparateFromMissionErrors()
    {
        const string message="UnityConnectWebRequestException: Token Exchange failed due a failure with the web request.";
        const string stack="UnityEditor.Connect.TokenExchange.VerifyTokenExchangeResponse\nUnityEditor.Connect.UnityConnect.RequestNewServiceToken";
        Assert.IsTrue(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,LogType.Exception));
        Assert.IsFalse(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack+"\nGame.Runtime.MissionSystem.OnUpdate",LogType.Exception));
        Assert.IsFalse(MissionEditorQaLogClassification.IsEditorCloudTokenFailure("NullReferenceException",stack,LogType.Exception));
        Assert.IsFalse(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,null,LogType.Exception));
        Assert.IsFalse(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,LogType.Assert));
    }
}
