using System.Collections;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MenuMatchMusicPlayModeTests
{
    private const string MenuSceneName = "Menu";
    private const string MatchSceneName = "Match";
    private const string MenuMusicClipName = "music_menu_loop_01";
    private const string MatchMusicClipName = "music_match_calm_loop_01";
    private const float TimeoutSeconds = 60f;

    private UISettingsModel previousSettings;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousSettings = SettingsService.Load();
        UISettingsModel enabledSettings = SettingsService.Defaults;
        enabledSettings.Audio.MusicEnabled = true;
        SettingsService.Save(enabledSettings);

        AsyncOperation load = SceneManager.LoadSceneAsync(MenuSceneName, LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SettingsService.Save(previousSettings);
        SettingsService.ApplyRuntime(previousSettings);
        Scene match = SceneManager.GetSceneByName(MatchSceneName);
        if (match.IsValid() && match.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(match);
            while (unload != null && !unload.isDone)
                yield return null;
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator MenuMusic_DefaultsOnAndSettingsMuteRestoreTheActiveLoop()
    {
        AudioPlaybackPresentationRuntimeView runtimeView = null;
        AudioSource musicSource = null;
        yield return WaitUntil(
            () =>
            {
                runtimeView = Object.FindFirstObjectByType<AudioPlaybackPresentationRuntimeView>();
                musicSource = FindMusicSource(runtimeView, MenuMusicClipName);
                return musicSource != null && musicSource.volume > 0.01f;
            },
            "Menu music did not start from the serialized runtime audio catalog.");

        Assert.That(musicSource.clip, Is.Not.Null);
        Assert.That(musicSource.clip.name, Is.EqualTo(MenuMusicClipName));
        Assert.That(musicSource.loop, Is.True);
        Assert.That(musicSource.spatialBlend, Is.Zero);

        UISettingsModel settings = SettingsService.Load();
        settings.Audio.MusicEnabled = false;
        SettingsService.Save(settings);
        SettingsService.ApplyRuntime(settings);

        yield return WaitUntil(
            () => musicSource != null && musicSource.volume <= 0.001f,
            "Disabling Music in settings did not fade the active menu loop to silence.");
        Assert.That(musicSource.clip, Is.Not.Null, "Muting Music should preserve the loop so it can resume.");

        settings.Audio.MusicEnabled = true;
        SettingsService.Save(settings);
        SettingsService.ApplyRuntime(settings);

        yield return WaitUntil(
            () => musicSource != null && musicSource.volume > 0.01f,
            "Re-enabling Music in settings did not restore the active menu loop.");
        Assert.That(musicSource.clip.name, Is.EqualTo(MenuMusicClipName));
    }

    [UnityTest]
    public IEnumerator EnterMatch_CrossfadesToMatchMusicAndSettingsCanMuteIt()
    {
        AudioPlaybackPresentationRuntimeView runtimeView = null;
        AudioSource menuSource = null;
        yield return WaitUntil(
            () =>
            {
                runtimeView = Object.FindFirstObjectByType<AudioPlaybackPresentationRuntimeView>();
                menuSource = FindMusicSource(runtimeView, MenuMusicClipName);
                return menuSource != null && menuSource.volume > 0.01f;
            },
            "Menu music did not start before the Match route transition.");

        Assert.That(
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.EnterMatch,
                UIRoute.Match,
                pushHistory: false),
            Is.True,
            "The production UI gateway rejected the Menu-to-Match route.");

        AudioSource matchSource = null;
        yield return WaitUntil(
            () =>
            {
                matchSource = FindMusicSource(runtimeView, MatchMusicClipName);
                menuSource = FindMusicSource(runtimeView, MenuMusicClipName);
                return matchSource != null && matchSource.volume > 0.01f && menuSource == null;
            },
            "Menu music did not crossfade cleanly into the Match music loop.");

        Assert.That(matchSource.loop, Is.True);
        Assert.That(matchSource.spatialBlend, Is.Zero);

        UISettingsModel settings = SettingsService.Load();
        settings.Audio.MusicEnabled = false;
        SettingsService.Save(settings);
        SettingsService.ApplyRuntime(settings);

        yield return WaitUntil(
            () => matchSource != null && matchSource.volume <= 0.001f,
            "Disabling Music in settings did not fade the active Match loop to silence.");
        Assert.That(matchSource.clip.name, Is.EqualTo(MatchMusicClipName));
    }

    private static AudioSource FindMusicSource(AudioPlaybackPresentationRuntimeView runtimeView, string clipName)
    {
        if (runtimeView == null)
            return null;

        AudioSource[] sources = runtimeView.GetComponentsInChildren<AudioSource>(includeInactive: true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioClip clip = sources[i].clip;
            if (clip != null && string.Equals(clip.name, clipName, System.StringComparison.Ordinal))
                return sources[i];
        }

        return null;
    }

    private static IEnumerator WaitUntil(System.Func<bool> predicate, string failureMessage)
    {
        float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup >= deadline)
                Assert.Fail(failureMessage);
            yield return null;
        }
    }
}
