using System;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class AudioPlaybackPresentationSceneBindingTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string EventCatalogPath = "Assets/Game/Audio/Events/AudioEventCatalogConfig.asset";
    private const string MixerBusConfigPath = "Assets/Game/Audio/Mixers/AudioMixerBusConfig.asset";

    public static void RunFocusedValidation()
    {
        try
        {
            AudioPlaybackPresentationSceneBindingTests tests = new();
            tests.MenuSceneBootstrap_HasAudioPlaybackRuntimeViewWithGeneratedAssets();
            Debug.Log("[AudioPlaybackPresentationSceneBindingValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[AudioPlaybackPresentationSceneBindingValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void MenuSceneBootstrap_HasAudioPlaybackRuntimeViewWithGeneratedAssets()
    {
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
        AudioPlaybackPresentationRuntimeView audioRuntime =
            UnityEngine.Object.FindAnyObjectByType<AudioPlaybackPresentationRuntimeView>(FindObjectsInactive.Include);
        AudioEventCatalogConfig expectedCatalog = AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>(EventCatalogPath);
        AudioMixerBusConfig expectedBusConfig = AssetDatabase.LoadAssetAtPath<AudioMixerBusConfig>(MixerBusConfigPath);

        Assert.NotNull(bootstrap, "Menu scene must contain MenuBootstrapView.");
        Assert.NotNull(audioRuntime, "Menu scene must contain AudioPlaybackPresentationRuntimeView.");
        Assert.AreSame(bootstrap.gameObject, audioRuntime.gameObject, "Audio playback runtime must live on the shell bootstrap object.");
        Assert.NotNull(expectedCatalog, $"Missing expected catalog asset: {EventCatalogPath}");
        Assert.NotNull(expectedBusConfig, $"Missing expected bus config asset: {MixerBusConfigPath}");

        SerializedObject serialized = new(audioRuntime);
        Assert.AreSame(
            expectedCatalog,
            serialized.FindProperty("eventCatalog").objectReferenceValue,
            "Audio playback runtime must reference the generated event catalog.");
        Assert.AreSame(
            expectedBusConfig,
            serialized.FindProperty("mixerBusConfig").objectReferenceValue,
            "Audio playback runtime must reference the generated mixer bus config.");
        Assert.GreaterOrEqual(serialized.FindProperty("initialPoolSize").intValue, 0);
        Assert.Greater(serialized.FindProperty("maxPoolSize").intValue, 0);
    }
}
