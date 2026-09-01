using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class EventsV3View : UIScreenView
    {
        [SerializeField] private TMP_Text creditsValue;
        [SerializeField] private TMP_Text commandValue;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private V3GradientGraphic[] tabGradients;
        [SerializeField] private Button[] eventButtons;
        [SerializeField] private V3GradientGraphic[] eventGradients;
        [SerializeField] private TMP_Text[] eventTitles;
        [SerializeField] private TMP_Text[] eventTimers;
        [SerializeField] private TMP_Text[] eventDescriptions;
        [SerializeField] private TMP_Text[] eventProgressTexts;
        [SerializeField] private RectTransform[] eventProgressFills;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailTimer;
        [SerializeField] private TMP_Text detailDescription;
        [SerializeField] private TMP_Text[] detailObjectives;
        [SerializeField] private TMP_Text[] detailObjectiveStates;
        [SerializeField] private TMP_Text[] detailModifiers;
        [SerializeField] private TMP_Text[] detailRewards;

        private UnityAction[] _tabActions = Array.Empty<UnityAction>();
        private UnityAction[] _eventActions = Array.Empty<UnityAction>();
        private int _tabIndex;
        private int _eventIndex;

        private static readonly EventData[][] Catalog =
        {
            new[]
            {
                new EventData("HOLD THE OLD MARKET", "02D 14H", "Hold the market district and repel enemy waves.", 7, 10,
                    "Enemy forces are pushing into the old market district. Hold key positions, protect civilians, and survive the counterattacks.",
                    "Establish perimeter", "COMPLETED", "Secure central plaza", "COMPLETED", "Hold 10 waves", "7/10", "NO AIR SUPPORT", "FOG OF WAR", "2,500", "250", "10"),
                new EventData("CONVOY BREAKER", "05D 08H", "Destroy supply convoys and deny their advance.", 4, 6,
                    "Intercept armored supply convoys before they reach the East Ridge depots. Preserve enough force for the final extraction.",
                    "Locate convoy route", "COMPLETED", "Disable escort vehicles", "2/3", "Destroy 6 convoys", "4/6", "ARMORED UNITS", "LIMITED REPAIRS", "4,000", "400", "20"),
                new EventData("ARIA FIELD TRIALS", "07D", "Complete ARIA's field trials and gather operation data.", 2, 5,
                    "ARIA is evaluating coordinated command responses under pressure. Complete the authored trials and preserve the telemetry package.",
                    "Run movement trial", "COMPLETED", "Run support trial", "1/2", "Complete 5 trials", "2/5", "ARIA ASSIST", "ENHANCED ENEMIES", "3,000", "300", "15")
            },
            new[]
            {
                new EventData("EAST RIDGE DEFENSE", "STARTS 1D", "Prepare defenses across the East Ridge approach.", 0, 8, "This operation becomes available in the next event rotation.", "Review approach", "READY", "Prepare loadout", "READY", "Hold 8 waves", "0/8", "FORTIFIED", "NIGHT OP", "3,000", "300", "12"),
                new EventData("AIRLIFT CORRIDOR", "STARTS 3D", "Secure a corridor for civilian airlift.", 0, 5, "This operation becomes available later in the current event cycle.", "Review corridor", "READY", "Prepare support", "READY", "Secure 5 zones", "0/5", "AIR SUPPORT", "HIGH RISK", "3,500", "350", "15"),
                new EventData("SIGNAL BLACKOUT", "STARTS 5D", "Restore the district command network.", 0, 4, "This authored event is scheduled for the final rotation window.", "Review network", "READY", "Prepare scouts", "READY", "Restore 4 relays", "0/4", "NO MINIMAP", "ARIA ASSIST", "4,000", "400", "18")
            },
            new[]
            {
                new EventData("TACTICAL MASTERY", "WEEKLY", "Complete command challenges without losing a squad.", 3, 6, "A deterministic weekly challenge set for experienced commanders.", "Win one operation", "COMPLETED", "Protect all squads", "1/3", "Complete 6 goals", "3/6", "VETERAN AI", "NO RETRIES", "2,000", "500", "20"),
                new EventData("RESOURCE DISCIPLINE", "WEEKLY", "Finish operations below the resource cap.", 1, 4, "Demonstrate efficient use of authored operation resources.", "Limit support use", "COMPLETED", "Preserve reserves", "0/2", "Complete 4 goals", "1/4", "LOW SUPPLY", "FIXED LOADOUT", "2,500", "450", "18"),
                new EventData("CIVILIAN SHIELD", "WEEKLY", "Protect civilians in high-risk districts.", 2, 5, "Maintain civilian safety while completing the main objective.", "Secure shelters", "COMPLETED", "Escort convoy", "1/2", "Complete 5 goals", "2/5", "HIGH PRESSURE", "LIMITED TIME", "3,000", "550", "22")
            },
            new[]
            {
                new EventData("EVENT REWARD TRACK", "SEASON", "Review earned event milestones and fixed rewards.", 7, 10, "Rewards are granted only for completed authored milestones.", "Tier 1 reward", "CLAIMED", "Tier 2 reward", "CLAIMED", "Reach tier 10", "7/10", "FIXED REWARDS", "NO RANDOM LOOT", "5,000", "750", "30"),
                new EventData("COMMANDER MILESTONES", "SEASON", "Review commander XP earned from events.", 4, 8, "Milestone rewards remain deterministic and visible before completion.", "Earn 250 XP", "CLAIMED", "Earn 500 XP", "2/4", "Reach milestone 8", "4/8", "ACCOUNT WIDE", "FIXED REWARDS", "4,000", "600", "25"),
                new EventData("UNIT PARTS TRACK", "SEASON", "Review unit parts earned from event play.", 2, 6, "Unit parts support existing Armory unlock progression.", "Earn Rifle Parts", "CLAIMED", "Earn Vehicle Parts", "1/2", "Reach milestone 6", "2/6", "ARMORY PARTS", "EARN PATH", "3,000", "500", "35")
            }
        };

        public Button[] TabButtons => tabButtons;
        public Button[] EventButtons => eventButtons;

        public void Configure(TMP_Text credits, TMP_Text command, Button[] tabs, V3GradientGraphic[] tabChrome,
            Button[] events, V3GradientGraphic[] eventChrome, TMP_Text[] titles, TMP_Text[] timers,
            TMP_Text[] descriptions, TMP_Text[] progressTexts, RectTransform[] progressFills,
            TMP_Text selectedTitle, TMP_Text selectedTimer, TMP_Text selectedDescription,
            TMP_Text[] objectives, TMP_Text[] objectiveStates, TMP_Text[] modifiers, TMP_Text[] rewards)
        {
            creditsValue = credits;
            commandValue = command;
            tabButtons = tabs;
            tabGradients = tabChrome;
            eventButtons = events;
            eventGradients = eventChrome;
            eventTitles = titles;
            eventTimers = timers;
            eventDescriptions = descriptions;
            eventProgressTexts = progressTexts;
            eventProgressFills = progressFills;
            detailTitle = selectedTitle;
            detailTimer = selectedTimer;
            detailDescription = selectedDescription;
            detailObjectives = objectives;
            detailObjectiveStates = objectiveStates;
            detailModifiers = modifiers;
            detailRewards = rewards;
        }

        private void Awake()
        {
            SetRouteForTests(UIRoute.Events);
            Wire();
            RefreshResources();
            Refresh();
        }

        private void OnEnable()
        {
            RefreshResources();
            Refresh();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < Mathf.Min(tabButtons?.Length ?? 0, _tabActions.Length); i++) tabButtons[i]?.onClick.RemoveListener(_tabActions[i]);
            for (int i = 0; i < Mathf.Min(eventButtons?.Length ?? 0, _eventActions.Length); i++) eventButtons[i]?.onClick.RemoveListener(_eventActions[i]);
        }

        private void Wire()
        {
            _tabActions = new UnityAction[tabButtons?.Length ?? 0];
            for (int i = 0; i < _tabActions.Length; i++)
            {
                int index = i;
                _tabActions[i] = () => { _tabIndex = index; _eventIndex = 0; Refresh(); };
                tabButtons[i]?.onClick.AddListener(_tabActions[i]);
            }
            _eventActions = new UnityAction[eventButtons?.Length ?? 0];
            for (int i = 0; i < _eventActions.Length; i++)
            {
                int index = i;
                _eventActions[i] = () => { _eventIndex = index; Refresh(); };
                eventButtons[i]?.onClick.AddListener(_eventActions[i]);
            }
        }

        private void RefreshResources()
        {
            if (!UiShellRuntimeGateway.TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)) return;
            if (creditsValue != null && !string.IsNullOrWhiteSpace(resources.CreditsText)) creditsValue.text = resources.CreditsText;
            if (commandValue != null && !string.IsNullOrWhiteSpace(resources.CommandText)) commandValue.text = resources.CommandText;
        }

        private void Refresh()
        {
            EventData[] events = Catalog[Mathf.Clamp(_tabIndex, 0, Catalog.Length - 1)];
            for (int i = 0; i < events.Length; i++)
            {
                EventData data = events[i];
                if (eventTitles != null && i < eventTitles.Length) eventTitles[i].text = data.Title;
                if (eventTimers != null && i < eventTimers.Length) eventTimers[i].text = data.Timer;
                if (eventDescriptions != null && i < eventDescriptions.Length) eventDescriptions[i].text = data.CardDescription;
                if (eventProgressTexts != null && i < eventProgressTexts.Length) eventProgressTexts[i].text = data.Progress + "/" + data.Total;
                if (eventProgressFills != null && i < eventProgressFills.Length)
                {
                    Vector2 size = eventProgressFills[i].sizeDelta;
                    size.x = 276f * data.Progress / Mathf.Max(1f, data.Total);
                    eventProgressFills[i].sizeDelta = size;
                }
                eventGradients?[i]?.SetBorder(i == _eventIndex ? new Color32(255, 180, 0, 255) : ResolveAccent(i), 3f);
            }
            for (int i = 0; i < (tabGradients?.Length ?? 0); i++)
            {
                bool selected = i == _tabIndex;
                tabGradients[i]?.ConfigureCorners(
                    selected ? new Color32(55, 143, 55, 255) : new Color32(30, 41, 45, 255),
                    selected ? new Color32(27, 105, 36, 255) : new Color32(18, 28, 31, 255),
                    selected ? new Color32(9, 62, 25, 255) : new Color32(4, 10, 13, 255),
                    selected ? new Color32(16, 78, 30, 255) : new Color32(8, 17, 19, 255),
                    selected ? new Color32(92, 194, 83, 255) : new Color32(56, 70, 74, 255), 3f);
            }
            EventData selectedEvent = events[Mathf.Clamp(_eventIndex, 0, events.Length - 1)];
            if (detailTitle != null) detailTitle.text = selectedEvent.Title;
            if (detailTimer != null) detailTimer.text = selectedEvent.Timer + " REMAINING";
            if (detailDescription != null) detailDescription.text = selectedEvent.DetailDescription;
            string[] objectives = { selectedEvent.ObjectiveA, selectedEvent.ObjectiveB, selectedEvent.ObjectiveC };
            string[] states = { selectedEvent.StateA, selectedEvent.StateB, selectedEvent.StateC };
            for (int i = 0; i < 3; i++)
            {
                if (detailObjectives != null && i < detailObjectives.Length) detailObjectives[i].text = objectives[i];
                if (detailObjectiveStates != null && i < detailObjectiveStates.Length) detailObjectiveStates[i].text = states[i];
            }
            if (detailModifiers != null && detailModifiers.Length >= 2) { detailModifiers[0].text = selectedEvent.ModifierA; detailModifiers[1].text = selectedEvent.ModifierB; }
            if (detailRewards != null && detailRewards.Length >= 3) { detailRewards[0].text = selectedEvent.RewardA; detailRewards[1].text = selectedEvent.RewardB; detailRewards[2].text = selectedEvent.RewardC; }
        }

        private static Color ResolveAccent(int index) => index switch
        {
            0 => new Color32(250, 177, 0, 255),
            1 => new Color32(239, 62, 20, 255),
            _ => new Color32(0, 174, 220, 255)
        };

        private readonly struct EventData
        {
            public readonly string Title, Timer, CardDescription, DetailDescription;
            public readonly int Progress, Total;
            public readonly string ObjectiveA, StateA, ObjectiveB, StateB, ObjectiveC, StateC;
            public readonly string ModifierA, ModifierB, RewardA, RewardB, RewardC;
            public EventData(string title, string timer, string cardDescription, int progress, int total, string detailDescription,
                string objectiveA, string stateA, string objectiveB, string stateB, string objectiveC, string stateC,
                string modifierA, string modifierB, string rewardA, string rewardB, string rewardC)
            {
                Title = title; Timer = timer; CardDescription = cardDescription; Progress = progress; Total = total; DetailDescription = detailDescription;
                ObjectiveA = objectiveA; StateA = stateA; ObjectiveB = objectiveB; StateB = stateB; ObjectiveC = objectiveC; StateC = stateC;
                ModifierA = modifierA; ModifierB = modifierB; RewardA = rewardA; RewardB = rewardB; RewardC = rewardC;
            }
        }
    }
}
