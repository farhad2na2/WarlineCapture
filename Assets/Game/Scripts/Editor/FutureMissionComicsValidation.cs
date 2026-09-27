using System;
using System.Reflection;
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

            Debug.Log($"[FutureMissionComics] result=Passed missions={count} images={count * 3} canvas=existing");
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

                campaign.ChapterFiveButton.onClick.Invoke();
                campaign.MissionNodeButtons[4].onClick.Invoke();
                if (!preview.IsOpen || narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/" + FutureMissionComicCatalog.Find(5, 5).image))
                    throw new InvalidOperationException("Chapter 5 final comic did not open.");
                preview.Close();

                campaign.ChapterThreeButton.onClick.Invoke();
                typeof(CampaignOperationsScreenView).GetProperty("IsChapterThree")
                    ?.SetValue(campaign, true);
                campaign.MissionNodeButtons[4].onClick.Invoke();
                if (!preview.IsOpen || narrative.CurrentPanelSprite != Resources.Load<Sprite>(
                        "FutureMissionComics/" + FutureMissionComicCatalog.Find(3, 5).image))
                    throw new InvalidOperationException("Chapter 3 Mission 5 comic did not open.");
                preview.Close();
                Debug.Log("[FutureMissionComicsClick] result=Passed missions=3 stageTransitions=3");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
