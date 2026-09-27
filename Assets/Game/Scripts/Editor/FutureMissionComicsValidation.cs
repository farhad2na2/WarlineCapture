using System;
using System.Reflection;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class FutureMissionComicsValidation
    {
        private const string CampaignPrefab =
            "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";

        [MenuItem("Game/Campaign/Validate Future Mission Comics")]
        public static void Run()
        {
            AssetDatabase.Refresh();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignPrefab);
            CampaignOperationsScreenView view =
                prefab != null ? prefab.GetComponentInChildren<CampaignOperationsScreenView>(true) : null;
            if (view == null || view.ChapterCards == null || view.ChapterCards.Length < 5 ||
                view.MissionNodeButtons == null || view.MissionNodeButtons.Length < 5)
                throw new InvalidOperationException("Campaign UI is missing future chapter cards or mission nodes.");

            int count = 0;
            for (int chapter = 3; chapter <= 5; chapter++)
            for (int number = chapter == 3 ? 5 : 1; number <= 5; number++)
            {
                FutureMissionComicMission mission = FutureMissionComicCatalog.Find(chapter, number)
                    ?? throw new InvalidOperationException($"Missing CH{chapter:00} M{number:00} comic.");
                foreach (string image in new[] { mission.image, mission.commsImage, mission.debriefImage })
                {
                    Sprite art = Resources.Load<Sprite>("FutureMissionComics/" + image)
                        ?? throw new InvalidOperationException($"Missing sprite {image} for {mission.id}.");
                    if (art.rect.width != 1672 || art.rect.height != 941)
                        throw new InvalidOperationException(
                            $"Wrong image size for {mission.id}/{image}: {art.rect.size}.");
                }
                if (mission.lines == null || mission.lines.Length < 4)
                    throw new InvalidOperationException($"Missing story text for {mission.id}.");
                count++;
            }

            string[] bookendIds =
            {
                "seq.ch03.open.hidden_network", "seq.ch04.open.air_and_armor",
                "seq.ch05.open.citywide_command",
                "seq.ch01.close.protocol_fragment_01", "seq.ch02.close.protocol_fragment_02",
                "seq.ch03.close.protocol_fragment_03", "seq.ch04.close.protocol_fragment_04",
                "seq.ch05.close.protocol_fragment_05", "seq.campaign.epilogue.canonical",
                "seq.campaign.epilogue.trust_emphasis", "seq.campaign.epilogue.evidence_emphasis",
                "seq.campaign.epilogue.infrastructure_emphasis",
                "seq.campaign.postscript.recovery_watch"
            };
            foreach (string id in bookendIds)
            {
                BookendComicSequence sequence = BookendComicCatalog.Find(id)
                    ?? throw new InvalidOperationException($"Missing bookend {id}.");
                if (sequence.pages == null || sequence.pages.Length == 0)
                    throw new InvalidOperationException($"Empty bookend {id}.");
                foreach (BookendComicPage panel in sequence.pages)
                {
                    Sprite art = Resources.Load<Sprite>("FutureMissionComics/" + panel.image)
                        ?? throw new InvalidOperationException($"Missing bookend sprite {panel.image}.");
                    if (art.rect.width != 1672 || art.rect.height != 941)
                        throw new InvalidOperationException($"Wrong bookend image size: {panel.image}.");
                    if (string.IsNullOrWhiteSpace(panel.en) || string.IsNullOrWhiteSpace(panel.fa))
                        throw new InvalidOperationException($"Missing localized caption in {id}.");
                    if (id.StartsWith("seq.campaign.epilogue.") &&
                        id != "seq.campaign.epilogue.canonical" &&
                        (string.IsNullOrWhiteSpace(panel.lowEn) || string.IsNullOrWhiteSpace(panel.lowFa)))
                        throw new InvalidOperationException($"Missing low-state caption in {id}.");
                }
            }

            Debug.Log($"[FutureMissionComics] result=Passed missions={count} images={count * 3} bookends={bookendIds.Length} canvas=existing");
        }

        [MenuItem("Game/Campaign/Smoke Future Mission Comic Click")]
        public static void RunPreviewSmoke()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
            NarrativeSequenceView narrative =
                UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException("Menu narrative canvas missing.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignPrefab)
                ?? throw new InvalidOperationException("Campaign prefab missing.");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                CampaignOperationsScreenView campaign =
                    instance.GetComponentInChildren<CampaignOperationsScreenView>(true)
                    ?? throw new InvalidOperationException("Campaign view missing.");
                CampaignMissionScreenBinderView binder =
                    instance.GetComponentInChildren<CampaignMissionScreenBinderView>(true)
                    ?? throw new InvalidOperationException("Campaign binder missing.");
                binder.Configure(campaign, "saga.ch01.m01.first_contact");
                typeof(CampaignMissionScreenBinderView)
                    .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(binder, null);

                campaign.ChapterFourButton.onClick.Invoke();
                campaign.MissionNodeButtons[1].onClick.Invoke();
                FutureMissionComicPreviewController preview =
                    binder.GetComponent<FutureMissionComicPreviewController>();
                if (preview == null || !preview.IsOpen || !narrative.IsVisible)
                    throw new InvalidOperationException("Chapter 4 Mission 2 click did not open its comic.");
                FutureMissionComicMission mission = FutureMissionComicCatalog.Find(4, 2);
                if (narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/" + mission.image))
                    throw new InvalidOperationException("Briefing image did not appear.");
                MethodInfo advance = typeof(FutureMissionComicPreviewController)
                    .GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic);
                advance?.Invoke(preview, new object[] { NarrativeDialoguePhase.AdvanceReady });
                advance?.Invoke(preview, new object[] { NarrativeDialoguePhase.AdvanceReady });
                if (narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/" + mission.commsImage))
                    throw new InvalidOperationException("Comms image did not appear.");
                advance?.Invoke(preview, new object[] { NarrativeDialoguePhase.AdvanceReady });
                if (narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/" + mission.debriefImage))
                    throw new InvalidOperationException("Debrief image did not appear.");
                preview.Close();

                campaign.MissionNodeButtons[4].onClick.Invoke();
                bool sawChapterFourClose = false;
                Sprite chapterFourClose = Resources.Load<Sprite>("FutureMissionComics/Bookends/CH04_Close");
                for (int step = 0; step < 20 && preview.IsOpen; step++)
                {
                    sawChapterFourClose |= narrative.CurrentPanelSprite == chapterFourClose;
                    advance?.Invoke(preview, new object[] { NarrativeDialoguePhase.AdvanceReady });
                }
                if (!sawChapterFourClose || preview.IsOpen)
                    throw new InvalidOperationException("Chapter 4 final comic did not reach its close.");
                preview.Close();

                campaign.ChapterFiveButton.onClick.Invoke();
                campaign.MissionNodeButtons[4].onClick.Invoke();
                if (!preview.IsOpen || narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/" + FutureMissionComicCatalog.Find(5, 5).image))
                    throw new InvalidOperationException("Chapter 5 final comic did not open.");
                bool sawClose = false;
                bool sawEpilogue = false;
                Sprite closeArt = Resources.Load<Sprite>("FutureMissionComics/Bookends/CH05_Close");
                Sprite endingArt = Resources.Load<Sprite>("FutureMissionComics/Bookends/Campaign_Epilogue");
                for (int step = 0; step < 40 && preview.IsOpen; step++)
                {
                    sawClose |= narrative.CurrentPanelSprite == closeArt;
                    sawEpilogue |= narrative.CurrentPanelSprite == endingArt;
                    advance?.Invoke(preview, new object[] { NarrativeDialoguePhase.AdvanceReady });
                }
                if (!sawClose || !sawEpilogue || preview.IsOpen)
                    throw new InvalidOperationException("Final mission did not reach chapter close and epilogue.");
                preview.Close();

                campaign.ChapterThreeButton.onClick.Invoke();
                if (!preview.IsOpen || narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/Bookends/CH03_Open"))
                    throw new InvalidOperationException("Chapter 3 opening did not appear on chapter selection.");
                preview.Close();
                typeof(CampaignOperationsScreenView).GetProperty("IsChapterThree")
                    ?.SetValue(campaign, true);
                campaign.MissionNodeButtons[4].onClick.Invoke();
                if (!preview.IsOpen || narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/" + FutureMissionComicCatalog.Find(3, 5).image))
                    throw new InvalidOperationException("Chapter 3 Mission 5 comic did not open.");
                bool sawChapterThreeClose = false;
                Sprite chapterThreeClose = Resources.Load<Sprite>("FutureMissionComics/Bookends/CH03_Close");
                for (int step = 0; step < 20 && preview.IsOpen; step++)
                {
                    sawChapterThreeClose |= narrative.CurrentPanelSprite == chapterThreeClose;
                    advance?.Invoke(preview, new object[] { NarrativeDialoguePhase.AdvanceReady });
                }
                if (!sawChapterThreeClose || preview.IsOpen)
                    throw new InvalidOperationException("Chapter 3 final comic did not reach its close.");
                preview.Close();

                foreach (var resultCase in new[]
                {
                    (missionId: "saga.ch01.m05.breach_assault", image: "CH01_Close"),
                    (missionId: "saga.ch02.m05.route_reopened", image: "CH02_Close")
                })
                {
                    GameObject resultProbe = new GameObject("BookendResultProbe");
                    try
                    {
                        CampaignMissionHudResultBinderView resultBinder =
                            resultProbe.AddComponent<CampaignMissionHudResultBinderView>();
                        UiMissionResultPopupModel resultModel = new(
                            1, resultCase.missionId, UiMissionResultOutcome.Victory,
                            "", "", "", 0, "", "", "", "", "", true, false);
                        typeof(CampaignMissionHudResultBinderView)
                            .GetField("activeModel", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.SetValue(resultBinder, resultModel);
                        typeof(CampaignMissionHudResultBinderView)
                            .GetMethod("OnPrimaryRequested", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.Invoke(resultBinder, null);
                        FutureMissionComicPreviewController closePreview =
                            resultProbe.GetComponent<FutureMissionComicPreviewController>();
                        if (closePreview == null || !closePreview.IsOpen ||
                            narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                                "FutureMissionComics/Bookends/" + resultCase.image))
                            throw new InvalidOperationException(
                                $"Playable final mission result did not open {resultCase.image}.");
                        closePreview.Close();
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(resultProbe);
                    }
                }
                Debug.Log("[FutureMissionComicsClick] result=Passed missions=4 stageTransitions=3 chapterOpenings=1 chapterCloses=5 epilogue=1");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
