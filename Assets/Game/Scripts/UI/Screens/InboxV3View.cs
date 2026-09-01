using System;
using System.Collections.Generic;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class InboxV3View : UIScreenView
    {
        [SerializeField] private TMP_Text creditsValue;
        [SerializeField] private TMP_Text commandValue;
        [SerializeField] private Button[] categoryButtons;
        [SerializeField] private V3GradientGraphic[] categoryGradients;
        [SerializeField] private TMP_Text[] categoryBadges;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button sortButton;
        [SerializeField] private TMP_Text sortLabel;
        [SerializeField] private Button filterButton;
        [SerializeField] private TMP_Text filterLabel;
        [SerializeField] private Button markAllReadButton;
        [SerializeField] private Button[] messageButtons;
        [SerializeField] private V3GradientGraphic[] messageGradients;
        [SerializeField] private TMP_Text[] messageTitles;
        [SerializeField] private TMP_Text[] messageSenders;
        [SerializeField] private TMP_Text[] messageTimes;
        [SerializeField] private GameObject[] messageUnreadBars;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailFrom;
        [SerializeField] private TMP_Text detailDate;
        [SerializeField] private RawImage detailArt;
        [SerializeField] private Texture[] detailArtTextures;
        [SerializeField] private TMP_Text detailBody;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Graphic favoriteStar;
        [SerializeField] private Button markReadButton;
        [SerializeField] private TMP_Text markReadLabel;
        [SerializeField] private Button[] attachmentButtons;
        [SerializeField] private TMP_Text[] attachmentTitles;
        [SerializeField] private TMP_Text[] attachmentFiles;
        [SerializeField] private TMP_Text[] attachmentStates;

        private readonly bool[] _unread = { true, true, true, true, true };
        private readonly bool[] _favorite = new bool[5];
        private readonly List<int> _visibleMessages = new(5);
        private UnityAction[] _categoryActions = Array.Empty<UnityAction>();
        private UnityAction[] _messageActions = Array.Empty<UnityAction>();
        private UnityAction[] _attachmentActions = Array.Empty<UnityAction>();
        private int _categoryIndex;
        private int _selectedMessageIndex;
        private bool _oldestFirst;
        private bool _unreadOnly;

        private static readonly string[] CategoryNames = { "ALL", "OPERATIONS", "ARIA", "REWARDS", "SYSTEM" };

        private static readonly MessageData[] Messages =
        {
            new("NORTH BRIDGE INTEL UPDATE", "Recon Command", "Today, 09:42", 1,
                "Recon assets confirm increased militia activity around North Bridge.\nMultiple supply convoys observed crossing into the East Ridge sector.\nRecommend strike window within the next 12 hours.",
                "INTEL REPORT", "NorthBridge_Intel.pdf", "1.4 MB", "SCOUT MAP", "NorthBridge_Map.png", "2.1 MB"),
            new("DAILY OPERATION REPORT", "Operations Command", "Today, 08:15", 1,
                "Daily readiness remains stable. Recon coverage has improved across the northern districts and two supply routes are available for tasking.",
                "DAILY REPORT", "Daily_Operation.pdf", "820 KB", "DISTRICT MAP", "Sahrin_Districts.png", "1.8 MB"),
            new("RANGER SQUAD UNLOCKED", "Field Command", "Today, 07:30", 3,
                "Ranger Squad is now available in the Armory. Review the unit profile, assign equipment, and add the squad to a valid loadout before deployment.",
                "UNIT PROFILE", "Ranger_Profile.pdf", "940 KB", "TRAINING FILE", "Ranger_Training.pdf", "1.1 MB"),
            new("ARIA TACTICAL REVIEW", "ARIA", "Today, 06:50", 2,
                "I completed the tactical review. Your response timing improved, but exposed supply vehicles remain the highest preventable risk in the current plan.",
                "TACTICAL REVIEW", "ARIA_Review.pdf", "680 KB", "ROUTE OVERLAY", "ARIA_Route.png", "1.6 MB"),
            new("COMMAND NETWORK NOTICE", "System", "Today, 06:10", 4,
                "Command Network synchronization completed successfully. Account services are healthy and all authored operation records are available offline.",
                "SERVICE LOG", "Command_Network.txt", "120 KB", "STATUS REPORT", "Network_Status.pdf", "420 KB")
        };

        public Button[] CategoryButtons => categoryButtons;
        public Button[] MessageButtons => messageButtons;
        public Button MarkAllReadButton => markAllReadButton;
        public Button MarkReadButton => markReadButton;
        public TMP_InputField SearchInput => searchInput;

        public void Configure(
            TMP_Text configuredCredits,
            TMP_Text configuredCommand,
            Button[] configuredCategoryButtons,
            V3GradientGraphic[] configuredCategoryGradients,
            TMP_Text[] configuredCategoryBadges,
            TMP_InputField configuredSearch,
            Button configuredSort,
            TMP_Text configuredSortLabel,
            Button configuredFilter,
            TMP_Text configuredFilterLabel,
            Button configuredMarkAllRead,
            Button[] configuredMessageButtons,
            V3GradientGraphic[] configuredMessageGradients,
            TMP_Text[] configuredMessageTitles,
            TMP_Text[] configuredMessageSenders,
            TMP_Text[] configuredMessageTimes,
            GameObject[] configuredUnreadBars,
            GameObject configuredEmptyState,
            TMP_Text configuredDetailTitle,
            TMP_Text configuredDetailFrom,
            TMP_Text configuredDetailDate,
            RawImage configuredDetailArt,
            Texture[] configuredDetailArtTextures,
            TMP_Text configuredDetailBody,
            Button configuredFavorite,
            Graphic configuredFavoriteStar,
            Button configuredMarkRead,
            TMP_Text configuredMarkReadLabel,
            Button[] configuredAttachmentButtons,
            TMP_Text[] configuredAttachmentTitles,
            TMP_Text[] configuredAttachmentFiles,
            TMP_Text[] configuredAttachmentStates)
        {
            creditsValue = configuredCredits;
            commandValue = configuredCommand;
            categoryButtons = configuredCategoryButtons;
            categoryGradients = configuredCategoryGradients;
            categoryBadges = configuredCategoryBadges;
            searchInput = configuredSearch;
            sortButton = configuredSort;
            sortLabel = configuredSortLabel;
            filterButton = configuredFilter;
            filterLabel = configuredFilterLabel;
            markAllReadButton = configuredMarkAllRead;
            messageButtons = configuredMessageButtons;
            messageGradients = configuredMessageGradients;
            messageTitles = configuredMessageTitles;
            messageSenders = configuredMessageSenders;
            messageTimes = configuredMessageTimes;
            messageUnreadBars = configuredUnreadBars;
            emptyState = configuredEmptyState;
            detailTitle = configuredDetailTitle;
            detailFrom = configuredDetailFrom;
            detailDate = configuredDetailDate;
            detailArt = configuredDetailArt;
            detailArtTextures = configuredDetailArtTextures;
            detailBody = configuredDetailBody;
            favoriteButton = configuredFavorite;
            favoriteStar = configuredFavoriteStar;
            markReadButton = configuredMarkRead;
            markReadLabel = configuredMarkReadLabel;
            attachmentButtons = configuredAttachmentButtons;
            attachmentTitles = configuredAttachmentTitles;
            attachmentFiles = configuredAttachmentFiles;
            attachmentStates = configuredAttachmentStates;
        }

        private void Awake()
        {
            SetRouteForTests(UIRoute.Inbox);
            WireButtons();
            RefreshResources();
            RefreshList(true);
        }

        private void OnEnable()
        {
            RefreshResources();
            RefreshList(false);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < Mathf.Min(categoryButtons?.Length ?? 0, _categoryActions.Length); i++)
                categoryButtons[i]?.onClick.RemoveListener(_categoryActions[i]);
            for (int i = 0; i < Mathf.Min(messageButtons?.Length ?? 0, _messageActions.Length); i++)
                messageButtons[i]?.onClick.RemoveListener(_messageActions[i]);
            for (int i = 0; i < Mathf.Min(attachmentButtons?.Length ?? 0, _attachmentActions.Length); i++)
                attachmentButtons[i]?.onClick.RemoveListener(_attachmentActions[i]);
            searchInput?.onValueChanged.RemoveListener(OnSearchChanged);
            sortButton?.onClick.RemoveListener(ToggleSort);
            filterButton?.onClick.RemoveListener(ToggleUnreadFilter);
            markAllReadButton?.onClick.RemoveListener(MarkAllRead);
            markReadButton?.onClick.RemoveListener(MarkSelectedRead);
            favoriteButton?.onClick.RemoveListener(ToggleFavorite);
        }

        private void WireButtons()
        {
            _categoryActions = new UnityAction[categoryButtons?.Length ?? 0];
            for (int i = 0; i < _categoryActions.Length; i++)
            {
                int index = i;
                _categoryActions[i] = () => SelectCategory(index);
                categoryButtons[i]?.onClick.AddListener(_categoryActions[i]);
            }
            _messageActions = new UnityAction[messageButtons?.Length ?? 0];
            for (int i = 0; i < _messageActions.Length; i++)
            {
                int slot = i;
                _messageActions[i] = () => SelectVisibleSlot(slot);
                messageButtons[i]?.onClick.AddListener(_messageActions[i]);
            }
            _attachmentActions = new UnityAction[attachmentButtons?.Length ?? 0];
            for (int i = 0; i < _attachmentActions.Length; i++)
            {
                int slot = i;
                _attachmentActions[i] = () => MarkAttachmentReady(slot);
                attachmentButtons[i]?.onClick.AddListener(_attachmentActions[i]);
            }
            searchInput?.onValueChanged.AddListener(OnSearchChanged);
            sortButton?.onClick.AddListener(ToggleSort);
            filterButton?.onClick.AddListener(ToggleUnreadFilter);
            markAllReadButton?.onClick.AddListener(MarkAllRead);
            markReadButton?.onClick.AddListener(MarkSelectedRead);
            favoriteButton?.onClick.AddListener(ToggleFavorite);
        }

        private void RefreshResources()
        {
            if (!UiShellRuntimeGateway.TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources))
                return;
            if (creditsValue != null && !string.IsNullOrWhiteSpace(resources.CreditsText))
                creditsValue.text = resources.CreditsText;
            if (commandValue != null && !string.IsNullOrWhiteSpace(resources.CommandText))
                commandValue.text = resources.CommandText;
        }

        private void SelectCategory(int index)
        {
            _categoryIndex = Mathf.Clamp(index, 0, CategoryNames.Length - 1);
            RefreshList(true);
        }

        private void SelectVisibleSlot(int slot)
        {
            if (slot < 0 || slot >= _visibleMessages.Count)
                return;
            _selectedMessageIndex = _visibleMessages[slot];
            RefreshList(false);
        }

        private void OnSearchChanged(string _)
        {
            RefreshList(true);
        }

        private void ToggleSort()
        {
            _oldestFirst = !_oldestFirst;
            RefreshList(false);
        }

        private void ToggleUnreadFilter()
        {
            _unreadOnly = !_unreadOnly;
            RefreshList(true);
        }

        private void MarkAllRead()
        {
            for (int i = 0; i < _unread.Length; i++)
                _unread[i] = false;
            RefreshList(false);
        }

        private void MarkSelectedRead()
        {
            _unread[_selectedMessageIndex] = false;
            RefreshList(false);
        }

        private void ToggleFavorite()
        {
            _favorite[_selectedMessageIndex] = !_favorite[_selectedMessageIndex];
            RefreshDetail();
        }

        private void MarkAttachmentReady(int slot)
        {
            if (attachmentStates == null || slot < 0 || slot >= attachmentStates.Length || attachmentStates[slot] == null)
                return;
            attachmentStates[slot].text = "OPEN VIA INTEL";
        }

        private void RefreshList(bool resetSelection)
        {
            _visibleMessages.Clear();
            string query = searchInput != null ? searchInput.text?.Trim() ?? string.Empty : string.Empty;
            for (int i = 0; i < Messages.Length; i++)
            {
                MessageData message = Messages[i];
                if (_categoryIndex > 0 && message.Category != _categoryIndex)
                    continue;
                if (_unreadOnly && !_unread[i])
                    continue;
                if (query.Length > 0 && message.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    message.From.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _visibleMessages.Add(i);
            }
            if (_oldestFirst)
                _visibleMessages.Reverse();
            if (resetSelection || !_visibleMessages.Contains(_selectedMessageIndex))
                _selectedMessageIndex = _visibleMessages.Count > 0 ? _visibleMessages[0] : 0;

            for (int i = 0; i < (messageButtons?.Length ?? 0); i++)
            {
                bool visible = i < _visibleMessages.Count;
                messageButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;
                int messageIndex = _visibleMessages[i];
                MessageData message = Messages[messageIndex];
                if (messageTitles != null && i < messageTitles.Length && messageTitles[i] != null) messageTitles[i].text = message.Title;
                if (messageSenders != null && i < messageSenders.Length && messageSenders[i] != null) messageSenders[i].text = "From: " + message.From;
                if (messageTimes != null && i < messageTimes.Length && messageTimes[i] != null) messageTimes[i].text = message.Time.Replace("Today, ", string.Empty);
                if (messageUnreadBars != null && i < messageUnreadBars.Length && messageUnreadBars[i] != null) messageUnreadBars[i].SetActive(_unread[messageIndex]);
                if (messageGradients != null && i < messageGradients.Length && messageGradients[i] != null)
                {
                    bool selected = messageIndex == _selectedMessageIndex;
                    messageGradients[i].ConfigureCorners(
                        selected ? new Color32(15, 70, 93, 255) : new Color32(27, 38, 42, 255),
                        selected ? new Color32(6, 48, 68, 255) : new Color32(16, 27, 30, 255),
                        selected ? new Color32(2, 25, 37, 255) : new Color32(4, 10, 13, 255),
                        selected ? new Color32(3, 37, 52, 255) : new Color32(8, 17, 19, 255),
                        selected ? new Color32(0, 190, 238, 255) : new Color32(56, 70, 74, 255),
                        3f);
                }
            }
            if (emptyState != null)
                emptyState.SetActive(_visibleMessages.Count == 0);
            if (sortLabel != null)
                sortLabel.text = _oldestFirst ? "OLDEST" : "NEWEST";
            if (filterLabel != null)
                filterLabel.text = _unreadOnly ? "UNREAD" : "FILTERS";
            RefreshCategories();
            RefreshDetail();
        }

        private void RefreshCategories()
        {
            for (int i = 0; i < (categoryButtons?.Length ?? 0); i++)
            {
                int count = 0;
                for (int messageIndex = 0; messageIndex < Messages.Length; messageIndex++)
                    if (_unread[messageIndex] && (i == 0 || Messages[messageIndex].Category == i)) count++;
                if (categoryBadges != null && i < categoryBadges.Length && categoryBadges[i] != null)
                    categoryBadges[i].text = count.ToString();
                if (categoryGradients != null && i < categoryGradients.Length && categoryGradients[i] != null)
                {
                    bool selected = i == _categoryIndex;
                    categoryGradients[i].ConfigureCorners(
                        selected ? new Color32(14, 125, 190, 255) : new Color32(30, 41, 45, 255),
                        selected ? new Color32(5, 72, 117, 255) : new Color32(18, 28, 31, 255),
                        selected ? new Color32(0, 38, 68, 255) : new Color32(4, 10, 13, 255),
                        selected ? new Color32(3, 55, 87, 255) : new Color32(8, 17, 19, 255),
                        selected ? new Color32(0, 190, 238, 255) : new Color32(56, 70, 74, 255),
                        3f);
                }
            }
        }

        private void RefreshDetail()
        {
            MessageData selected = Messages[Mathf.Clamp(_selectedMessageIndex, 0, Messages.Length - 1)];
            if (detailTitle != null) detailTitle.text = selected.Title;
            if (detailFrom != null) detailFrom.text = "From: <color=#77B936>" + selected.From + "</color>";
            if (detailDate != null) detailDate.text = selected.Time;
            if (detailBody != null) detailBody.text = selected.Body;
            if (detailArt != null && detailArtTextures != null && _selectedMessageIndex < detailArtTextures.Length)
            {
                Texture texture = detailArtTextures[_selectedMessageIndex];
                detailArt.texture = texture;
                AspectRatioFitter fitter = detailArt.GetComponent<AspectRatioFitter>();
                if (fitter != null && texture != null && texture.height > 0)
                    fitter.aspectRatio = texture.width / (float)texture.height;
            }
            if (favoriteStar != null)
                favoriteStar.color = _favorite[_selectedMessageIndex] ? new Color32(250, 177, 0, 255) : new Color32(160, 169, 170, 255);
            if (markReadLabel != null)
                markReadLabel.text = _unread[_selectedMessageIndex] ? "MARK READ" : "MARKED READ";
            if (markReadButton != null)
                markReadButton.interactable = _unread[_selectedMessageIndex];

            string[] titles = { selected.AttachmentTitleA, selected.AttachmentTitleB };
            string[] files = { selected.AttachmentFileA, selected.AttachmentFileB };
            string[] sizes = { selected.AttachmentSizeA, selected.AttachmentSizeB };
            for (int i = 0; i < 2; i++)
            {
                if (attachmentTitles != null && i < attachmentTitles.Length && attachmentTitles[i] != null) attachmentTitles[i].text = titles[i];
                if (attachmentFiles != null && i < attachmentFiles.Length && attachmentFiles[i] != null) attachmentFiles[i].text = files[i];
                if (attachmentStates != null && i < attachmentStates.Length && attachmentStates[i] != null) attachmentStates[i].text = sizes[i];
            }
        }

        private readonly struct MessageData
        {
            public readonly string Title;
            public readonly string From;
            public readonly string Time;
            public readonly int Category;
            public readonly string Body;
            public readonly string AttachmentTitleA;
            public readonly string AttachmentFileA;
            public readonly string AttachmentSizeA;
            public readonly string AttachmentTitleB;
            public readonly string AttachmentFileB;
            public readonly string AttachmentSizeB;

            public MessageData(string title, string from, string time, int category, string body,
                string attachmentTitleA, string attachmentFileA, string attachmentSizeA,
                string attachmentTitleB, string attachmentFileB, string attachmentSizeB)
            {
                Title = title;
                From = from;
                Time = time;
                Category = category;
                Body = body;
                AttachmentTitleA = attachmentTitleA;
                AttachmentFileA = attachmentFileA;
                AttachmentSizeA = attachmentSizeA;
                AttachmentTitleB = attachmentTitleB;
                AttachmentFileB = attachmentFileB;
                AttachmentSizeB = attachmentSizeB;
            }
        }
    }
}
