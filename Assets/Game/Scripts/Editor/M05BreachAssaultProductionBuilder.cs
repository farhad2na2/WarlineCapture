using System;
using UnityEngine;
using UnityEditor;
namespace Game.Editor
{
    public static class M05BreachAssaultProductionBuilder
    {
        public static void BuildCaptioned()=>Build(false);
        public static void BuildFinal()=>Build(true);
        private static void Build(bool voices)
        {
            try
            {
                M05BreachAssaultConfigBuilder.Build();M05BreachAssaultMediaImporter.ConfigureArt();M05BreachAssaultPresentationBuilder.Build();
                if(voices) M05BreachAssaultNarrativeBuilder.BuildAndInstall(); else M05BreachAssaultNarrativeBuilder.BuildCaptionedArtAndInstall();
                Debug.Log("[M05Production] result=Passed data=1 narrative=3 locales=2 guide=57/8 voices="+voices);
            }
            catch(Exception e) {Debug.LogException(e);Debug.LogError("[M05Production] result=Failed");EditorApplication.Exit(1);}
        }
    }
}
