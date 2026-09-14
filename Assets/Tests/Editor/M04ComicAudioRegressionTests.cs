using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;
public sealed class M04ComicAudioRegressionTests
{
    [Test] public void ComicVoicesResolveInBothLanguagesAndFitTheirPanels()=>M04ComicAudioValidation.Validate();
    [Test] public void SharedRadioRemainsSpeechFreeAfterPrefabRegeneration()=>
        new FirstLaunchNarrativePresentationTests().Phase10RAudio_UsesIndependentSettingsAwareLayersAndCancelsCleanly();
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M04ComicAudioRegressionTests();
            tests.SharedRadioRemainsSpeechFreeAfterPrefabRegeneration();
            tests.ComicVoicesResolveInBothLanguagesAndFitTheirPanels();
            Debug.Log("[M04ComicAudioRegression] result=Passed tests=2");ValidationExit.Passed();
        }
        catch(Exception error){Debug.LogException(error);ValidationExit.Failed();}
    }
}
