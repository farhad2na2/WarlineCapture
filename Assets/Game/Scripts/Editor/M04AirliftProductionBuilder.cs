using System;
using UnityEngine;
using UnityEditor;
namespace Game.Editor
{
    public static class M04AirliftProductionBuilder
    {
        public static void Build()
        {
            try{M04AirliftConfigBuilder.Build();M04AirliftMediaImporter.ConfigureArt();M04AirliftPresentationBuilder.Build();M04AirliftNarrativeBuilder.BuildAndInstall();Debug.Log("[M04AirliftProduction] result=Passed canonicalData=1 narrative=3 locales=2 guide=57/12");}
            catch(Exception e){Debug.LogException(e);Debug.LogError("[M04AirliftProduction] result=Failed");EditorApplication.Exit(1);}
        }
    }
}
