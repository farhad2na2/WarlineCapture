using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class AudioPlaybackPresentationSceneBindingTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string EventCatalogPath = "Assets/Game/Audio/Events/AudioEventCatalogConfig.asset";
    private const string MixerBusConfigPath = "Assets/Game/Audio/Mixers/AudioMixerBusConfig.asset";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            AudioPlaybackPresentationSceneBindingTests tests = new();
            tests.MenuSceneBootstrap_HasAudioPlaybackRuntimeViewWithGeneratedAssets();
            passed++;
            tests.MenuSceneAudioRuntime_DrainsUiRequestThroughPooledPlayback();
            passed++;
            tests.MenuAndMatchScenes_HaveExactlyOneEnabledAudioListener();
            passed++;

            Debug.Log($"[AudioPlaybackPresentationSceneBindingValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log($"[AudioPlaybackPresentationSceneBindingValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void MenuAndMatchScenes_HaveExactlyOneEnabledAudioListener()
    {
        AssertSceneHasExactlyOneEnabledAudioListener(MenuScenePath);
        AssertSceneHasExactlyOneEnabledAudioListener(MatchScenePath);
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

    [Test]
    public void MenuSceneAudioRuntime_DrainsUiRequestThroughPooledPlayback()
    {
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        AudioPlaybackPresentationRuntimeView audioRuntime =
            UnityEngine.Object.FindAnyObjectByType<AudioPlaybackPresentationRuntimeView>(FindObjectsInactive.Include);
        Assert.NotNull(audioRuntime, "Menu scene must contain AudioPlaybackPresentationRuntimeView.");

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("AudioPlaybackPresentationSceneSmokeTests");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            audioRuntime.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

            int uiRequestId = AudioEventRequestSystem.EnqueueOneShot(
                world.EntityManager,
                new FixedString64Bytes(AudioEventIds.UIButtonPrimaryClick),
                AudioEventIds.UIButtonPrimaryClickHash,
                new FixedString32Bytes("UI"),
                AudioPlaybackPriority.Medium,
                requestedAt: 1f,
                cooldownSeconds: 0f);
            int matchRequestId = AudioEventRequestSystem.EnqueueOneShot(
                world.EntityManager,
                new FixedString64Bytes(AudioEventIds.GameplayCommandMoveAccepted),
                AudioEventIds.GameplayCommandMoveAcceptedHash,
                new FixedString32Bytes("SFX"),
                AudioPlaybackPriority.Medium,
                requestedAt: 1f,
                cooldownSeconds: 0f);
            AudioCooldownSystem.ProcessPendingRequests(world.EntityManager, now: 1f);

            audioRuntime.SendMessage("Update", SendMessageOptions.DontRequireReceiver);

            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(world.EntityManager);
            DynamicBuffer<AudioPlaybackResultElement> results =
                world.EntityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);
            AudioPlaybackResultElement presentationResult = results[results.Length - 1];

            Assert.AreEqual(matchRequestId, audioRuntime.LastPresentedRequestId);
            Assert.Greater(audioRuntime.PoolSize, 0);
            Assert.GreaterOrEqual(audioRuntime.ActiveSourceCount, 2);
            Assert.AreEqual(matchRequestId, presentationResult.RequestId);
            Assert.AreEqual(AudioPlaybackRequestStatus.Accepted, presentationResult.Status);
            Assert.AreEqual("Played", presentationResult.Reason.ToString());
            Assert.IsTrue(HasPlayedResult(results, uiRequestId));
            Assert.IsTrue(HasPlayedResult(results, matchRequestId));
        }
        finally
        {
            audioRuntime.SendMessage("OnDestroy", SendMessageOptions.DontRequireReceiver);
            if (World.DefaultGameObjectInjectionWorld == world)
                World.DefaultGameObjectInjectionWorld = previousWorld;
            world.Dispose();
        }
    }

    private static bool HasPlayedResult(DynamicBuffer<AudioPlaybackResultElement> results, int requestId)
    {
        for (int i = 0; i < results.Length; i++)
        {
            AudioPlaybackResultElement result = results[i];
            if (result.RequestId == requestId && result.Reason.ToString() == "Played")
                return true;
        }

        return false;
    }

    private static void AssertSceneHasExactlyOneEnabledAudioListener(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int enabledCount = 0;
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                enabledCount++;
        }

        Assert.AreEqual(1, enabledCount, $"{scenePath} must contain exactly one enabled active AudioListener.");
    }
}
