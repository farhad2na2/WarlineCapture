#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Game.Composition;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class FirstLaunchNarrativePlayModeTests
{
    [UnityTest]
    public IEnumerator ReviewerBoot_AddressablesPlaybackAndDebriefSkipCompleteInLiveMenu()
    {
        FirstLaunchNarrativeReviewSession.Request();
        AsyncOperation load = SceneManager.LoadSceneAsync("Assets/Game/Scenes/Menu.unity", LoadSceneMode.Single);
        Assert.NotNull(load);
        while (!load.isDone)
            yield return null;
        yield return null;

        MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
        Assert.NotNull(bootstrap);
        NarrativeSequenceView narrative = bootstrap.FirstLaunchNarrativeView;
        Assert.NotNull(narrative);
        CanvasScaler scaler = narrative.GetComponentInParent<CanvasScaler>();
        Assert.NotNull(scaler);
        Assert.AreEqual(new Vector2(4800f, 2160f), scaler.referenceResolution);
        RectTransform dialogue = narrative.transform.Find("SafeArea/Dialogue") as RectTransform;
        Assert.NotNull(dialogue);
        Assert.AreEqual(2.2f, dialogue.localScale.x, 0.01f);
        Assert.AreEqual(0f, narrative.LocationIntroView.GetComponent<RectTransform>().pivot.x, 0.001f);
        Assert.Greater(narrative.GetComponent<CanvasGroup>().alpha, 0.99f);
        Assert.NotNull(narrative.CurrentPanelSprite, "FL-P01 must load through Addressables before live presentation.");
        Assert.AreEqual(1f, narrative.ReviewerControlsView.GetComponent<CanvasGroup>().alpha);

        TMP_Text stateLabel = Array.Find(narrative.GetComponentsInChildren<TMP_Text>(true), value => value.name == "StateIdLabel");
        Button next = Array.Find(narrative.GetComponentsInChildren<Button>(true), value => value.name == "NextButton");
        Button restart = Array.Find(narrative.GetComponentsInChildren<Button>(true), value => value.name == "RestartButton");
        Button debrief = Array.Find(narrative.GetComponentsInChildren<Button>(true), value => value.name == "JumpToDebriefButton");
        Button skip = Array.Find(narrative.GetComponentsInChildren<Button>(true), value => value.name == "SkipButton");
        Toggle reducedMotion = Array.Find(narrative.GetComponentsInChildren<Toggle>(true), value => value.name == "ReducedMotionToggle");
        Toggle subtitles = Array.Find(narrative.GetComponentsInChildren<Toggle>(true), value => value.name == "SubtitlesToggle");
        Toggle safeArea = Array.Find(narrative.GetComponentsInChildren<Toggle>(true), value => value.name == "SafeAreaToggle");
        Assert.NotNull(stateLabel);
        Assert.NotNull(next);
        Assert.NotNull(restart);
        Assert.NotNull(debrief);
        Assert.NotNull(skip);
        Assert.NotNull(reducedMotion);
        Assert.NotNull(subtitles);
        Assert.NotNull(safeArea);
        Assert.AreEqual("FL-P01", stateLabel.text);

        next.onClick.Invoke();
        yield return null;
        Assert.AreEqual("FL-P02", stateLabel.text);
        Assert.NotNull(narrative.CurrentPanelSprite);

        reducedMotion.isOn = true;
        yield return null;
        Assert.AreEqual(Vector2.zero, narrative.PanelMotionRoot.anchoredPosition);
        Assert.AreEqual(Vector3.one, narrative.PanelMotionRoot.localScale);

        subtitles.isOn = false;
        safeArea.isOn = true;
        yield return null;
        Assert.AreEqual(0f, narrative.DialogueView.GetComponent<CanvasGroup>().alpha);
        Transform safeAreaPreview = narrative.transform.Find("SafeArea/SafeAreaPreview");
        Assert.NotNull(safeAreaPreview);
        Assert.IsTrue(safeAreaPreview.gameObject.activeSelf);
        subtitles.isOn = true;

        debrief.onClick.Invoke();
        yield return null;
        Assert.AreEqual("FL-P19", stateLabel.text);
        skip.onClick.Invoke();
        yield return null;
        Assert.AreEqual("first_launch.command_base_reveal", stateLabel.text);

        restart.onClick.Invoke();
        yield return null;
        HashSet<string> visited = new(StringComparer.Ordinal);
        bool identitySurfaceValidated = false;
        for (int step = 0; step < 32; step++)
        {
            visited.Add(stateLabel.text);
            if (stateLabel.text == "first_launch.commander_identity")
            {
                Transform identity = narrative.transform.Find("SafeArea/CommanderIdentitySurface");
                Assert.NotNull(identity);
                Assert.IsTrue(identity.gameObject.activeSelf);
                Button[] portraits = Array.FindAll(
                    identity.GetComponentsInChildren<Button>(true),
                    value => value.name.StartsWith("PortraitButton_", StringComparison.Ordinal));
                Assert.AreEqual(7, portraits.Length);
                identitySurfaceValidated = true;

                Button identityContinue = Array.Find(
                    identity.GetComponentsInChildren<Button>(true),
                    value => value.name == "ContinueButton");
                Assert.NotNull(identityContinue);
                identityContinue.onClick.Invoke();
                yield return null;
                Assert.AreEqual("first_launch.guidance_choice", stateLabel.text);
                visited.Add(stateLabel.text);

                Transform guidance = narrative.transform.Find("SafeArea/GuidanceChoiceSurface");
                Assert.NotNull(guidance);
                Button guidanceContinue = Array.Find(
                    guidance.GetComponentsInChildren<Button>(true),
                    value => value.name == "ContinueButton");
                Assert.NotNull(guidanceContinue);
                guidanceContinue.onClick.Invoke();
                yield return null;
                Assert.AreEqual("FL-P09", stateLabel.text);
                visited.Add(stateLabel.text);
                float voiceDeadline = Time.realtimeSinceStartup + 2f;
                while (narrative.VoiceSource.clip == null && Time.realtimeSinceStartup < voiceDeadline)
                    yield return null;
                Assert.NotNull(narrative.VoiceSource.clip);
                Assert.AreEqual("p09_aria", narrative.VoiceSource.clip.name);
                AudioClip assignedVoiceClip = narrative.VoiceSource.clip;
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.AreSame(assignedVoiceClip, narrative.VoiceSource.clip);
                Assert.IsNull(narrative.SequenceAudioView.EventSource.clip);
                int assignedSpeechClips = 0;
                foreach (AudioSource source in narrative.GetComponentsInChildren<AudioSource>(true))
                {
                    if (source.clip != null && source.clip.name.StartsWith("p", StringComparison.Ordinal))
                        assignedSpeechClips++;
                }
                Assert.AreEqual(1, assignedSpeechClips, "Only the dedicated narration source may hold a dialogue clip.");
            }
            if (stateLabel.text == "first_launch.command_base_reveal")
                break;
            next.onClick.Invoke();
            yield return null;
        }
        CollectionAssert.Contains(visited, "first_launch.commander_identity");
        CollectionAssert.Contains(visited, "first_launch.guidance_choice");
        CollectionAssert.Contains(visited, "first_launch.gameplay_placeholder");
        CollectionAssert.Contains(visited, "FL-P22");
        CollectionAssert.Contains(visited, "first_launch.command_base_reveal");
        Assert.IsTrue(identitySurfaceValidated);
    }
}
#endif
