using System;
using UnityEngine;

namespace Game.Editor
{
    public static class MissionEditorQaLogClassification
    {
        public static bool IsEditorCloudTokenFailure(string message,string stack,LogType type)
        {
            return type==LogType.Exception &&
                message=="UnityConnectWebRequestException: Token Exchange failed due a failure with the web request." &&
                stack!=null && stack.Contains("UnityEditor.Connect.TokenExchange.VerifyTokenExchangeResponse",StringComparison.Ordinal) &&
                stack.Contains("UnityEditor.Connect.UnityConnect.RequestNewServiceToken",StringComparison.Ordinal) &&
                !stack.Contains("Game.",StringComparison.Ordinal);
        }
    }
}
