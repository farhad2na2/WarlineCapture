using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        public Button ChapterFourButton => ResolveChapterCardButton(3);
        public Button ChapterFiveButton => ResolveChapterCardButton(4);

        public void EnableFutureComicChapters(int selectedChapter)
        {
            bool persian = UiShellRuntimeGateway.Localization.IsRightToLeft;
            EnableFutureChapterCard(ChapterFourButton, 3, selectedChapter == 4,
                persian ? "هوا و زره" : "AIR AND ARMOR",
                persian ? "مأموریت اول آماده است" : "M01 READY");
            EnableFutureChapterCard(ChapterFiveButton, 4, selectedChapter == 5,
                persian ? "فرماندهی شهر" : "CITYWIDE COMMAND",
                persian ? "پیش‌نمایش داستان" : "STORY PREVIEW");
            // Keep previously available story previews reachable beside playable M01.
            if(selectedChapter==4&&IsChapterFour)
                for(int index=1;index<5;index++)
                {
                    bool available=FutureMissionComicCatalog.Find(4,index+1)!=null;
                    if(missionNodeButtons!=null&&index<missionNodeButtons.Length&&missionNodeButtons[index]!=null)missionNodeButtons[index].interactable=available;
                    if(missionLockIcons!=null&&index<missionLockIcons.Length&&missionLockIcons[index]!=null)missionLockIcons[index].SetActive(!available);
                    ApplyNodeAppearance(index,available,false,false);
                }
        }

        public void EnableNetworkBreakNode(bool selected)
        {
            const int index = 4;
            if (!IsChapterThree || missionNodeButtons == null || missionNodeButtons.Length <= index)
                return;
            if (missionNodeButtons[index] != null)
                missionNodeButtons[index].interactable = true;
            if (missionLockIcons != null && missionLockIcons.Length > index && missionLockIcons[index] != null)
                missionLockIcons[index].SetActive(false);
            ApplyNodeAppearance(index, true, false, selected);
            if (chapterMissionNames != null && chapterMissionNames.Length > index)
                Set(chapterMissionNames[index], FutureMissionComicCatalog.Find(3, 5)?.Title(
                    UiShellRuntimeGateway.Localization.IsRightToLeft) ?? "NETWORK BREAK");
        }

        public void ApplyFutureComicMission(int chapter, int number)
        {
            FutureMissionComicMission mission = FutureMissionComicCatalog.Find(chapter, number);
            if (mission == null)
                return;
            bool persian = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (chapter >= 4)
            {
                string chapterName = chapter == 4
                    ? (persian ? "هوا و زره" : "AIR AND ARMOR")
                    : (persian ? "فرماندهی شهر" : "CITYWIDE COMMAND");
                Set(selectedChapterLabel, chapterName);
                for (int index = 0; index < 5; index++)
                {
                    FutureMissionComicMission nodeMission = FutureMissionComicCatalog.Find(chapter, index + 1);
                    if (chapterMissionNames != null && index < chapterMissionNames.Length)
                        Set(chapterMissionNames[index], nodeMission?.Title(persian) ?? string.Empty);
                    if (missionNodeButtons != null && index < missionNodeButtons.Length &&
                        missionNodeButtons[index] != null)
                        missionNodeButtons[index].interactable = nodeMission != null;
                    if (missionLockIcons != null && index < missionLockIcons.Length &&
                        missionLockIcons[index] != null)
                        missionLockIcons[index].SetActive(false);
                    ApplyNodeAppearance(index, nodeMission != null, false, index + 1 == number);
                }
            }
            else
                EnableNetworkBreakNode(true);

            Set(missionNumber, $"CH{chapter:00} · M{number:00}");
            Set(missionName, mission.Title(persian));
            Set(missionBriefingText, mission.Summary(persian));
            Set(primaryObjectiveText, mission.Objective(persian));
            Set(rewardSummaryText, persian ? "پیش‌نمایش داستان · بازی بعداً آماده می‌شود" :
                "STORY PREVIEW · GAMEPLAY COMING LATER");
            Set(launchMissionLabel, persian ? "نمایش کمیک" : "PLAY COMIC");
            if (missionPreviewImage != null)
                missionPreviewImage.texture = Resources.Load<Texture2D>(
                    "FutureMissionComics/" + mission.image);
            if (launchMissionButton != null)
                launchMissionButton.interactable = true;
            EnableFutureComicChapters(chapter);
        }

        private void EnableFutureChapterCard(
            Button button, int cardIndex, bool selected, string englishTitle, string englishSubtitle)
        {
            if (button == null)
                return;
            button.interactable = true;
            ApplyChapterTabAppearance(button, selected, true);
            if (chapterCards == null || cardIndex >= chapterCards.Length || chapterCards[cardIndex] == null)
                return;
            Transform card = chapterCards[cardIndex];
            Transform icon = card.Find("Icon");
            if (icon != null)
                icon.gameObject.SetActive(false);
            TMP_Text title = card.Find("Title")?.GetComponent<TMP_Text>();
            TMP_Text subtitle = card.Find("Subtitle")?.GetComponent<TMP_Text>();
            if (title != null)
                title.text = englishTitle;
            if (subtitle != null)
                subtitle.text = englishSubtitle;
        }
    }
}
