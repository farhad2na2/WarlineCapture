using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Presentation and visible input only. Mission rules live in the shared ECS world.</summary>
    public sealed class OperationsMissionScreenView : MonoBehaviour
    {
        private bool hud;
        private TMP_FontAsset font;
        private TMP_Text title, description, status, clock, result;
        private TMP_Text[] siteLabels;
        private Button deploy, resume, saveAndExit, conclude, continueButton, evidenceButton, recoverButton, ariaButton, guideButton;
        private Button[] scanButtons, focusButtons, advanceButtons;
        private bool evidenceVisible, interruptedAttempt;
        private Button withdrawInterrupted;
        private GameObject briefing, controls, resultPanel, confirmation;
        private bool guideOpen;
        private bool missionPlaying;
        private AriaTutorialBriefingView duplicateAria;
        private RectTransform[] markers;

        public static void Install(GameObject body)
        {
            if (body == null) return;
            var existing = body.GetComponentInChildren<TMP_Text>(true);
            var font = existing != null ? existing.font : TMP_Settings.defaultFontAsset;
            var dashboard = body.GetComponentInChildren<OperationsDashboardScreenView>(true);
            Transform parent = dashboard != null && dashboard.DailyBriefing != null
                ? dashboard.DailyBriefing : body.transform;
            if (dashboard != null && dashboard.DailyBriefing != null)
                foreach (Transform child in dashboard.DailyBriefing)
                    if (child.name is "AriaPortraitClip" or "Briefing") child.gameObject.SetActive(false);
            var root = new GameObject("StreetSignalsMissionCard", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = dashboard != null ? new Vector2(12, 8) : Vector2.zero;
            rect.offsetMax = dashboard != null ? new Vector2(-12, -102) : Vector2.zero;
            var view = root.AddComponent<OperationsMissionScreenView>(); view.font = font; view.Build(false);
        }

        public static OperationsMissionScreenView CreateHud(TMP_FontAsset font)
        {
            var root = new GameObject("OperationsMissionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32600;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var squad = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            var styledFont = squad != null ? squad.GetComponentInChildren<TMP_Text>(true)?.font : null;
            var view = root.AddComponent<OperationsMissionScreenView>(); view.font = styledFont != null ? styledFont : font;
            view.Build(true); return view;
        }

        private void Build(bool isHud)
        {
            hud = isHud;
            var card = Panel("MissionCard", transform);
            if (hud)
            { card.anchorMin = new Vector2(.77f, .66f); card.anchorMax = new Vector2(.985f, .91f); card.offsetMin = card.offsetMax = Vector2.zero; }
            else
            { card.anchorMin = Vector2.zero; card.anchorMax = Vector2.one; card.offsetMin = card.offsetMax = Vector2.zero; }
            card.GetComponent<Image>().color = new Color(.025f, .06f, .075f, .94f);
            Frame(card);
            Vertical(card, hud ? 5 : 6);
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(12,12,9,9);
            title = Label(card, "STREET SIGNALS", 24, 32);
            title.color = new Color(.08f, .76f, .89f);
            title.alignment = TextAlignmentOptions.Left;
            description = Label(card, "", hud ? 17 : 18, hud ? 69 : 60);
            description.alignment = TextAlignmentOptions.TopLeft;
            status = Label(card, "", hud ? 16 : 15, hud ? 46 : 42);
            status.alignment = TextAlignmentOptions.TopLeft;
            status.color = new Color(.94f, .74f, .36f);
            if (!hud)
            {
                var spacer = Panel("BriefingSpacer", card, false);
                var layout = spacer.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = 0; layout.flexibleHeight = 1;
            }
            clock = Label(card, "", 19, 28);
            clock.alignment = TextAlignmentOptions.Left;
            briefing = card.gameObject;
            if (!hud)
            {
                var menuActions = Row(card); menuActions.GetComponent<LayoutElement>().preferredHeight = 43;
                deploy = Button(menuActions, Text("operations.deploy", "DEPLOY"), () => Send(interruptedAttempt ? UiOperationsMissionAction.RestartAttempt : UiOperationsMissionAction.Deploy));
                resume = Button(menuActions, Text("operations.o001.resume_attempt", "RESUME ATTEMPT"), () => Send(UiOperationsMissionAction.ResumeAttempt));
                withdrawInterrupted = Button(menuActions, Text("operations.withdraw", "WITHDRAW"), () => confirmation.SetActive(true));
                var interruptedCard = Modal("ConfirmInterruptedWithdraw"); confirmation = interruptedCard.parent.gameObject;
                Label(interruptedCard, Text("operations.o001.interrupted_withdraw_confirm", "Withdraw from the interrupted attempt? This spends the reserved action point and applies withdrawal consequences."), 23, 135);
                Button(interruptedCard, Text("operations.withdraw", "WITHDRAW"), () => { confirmation.SetActive(false); Send(UiOperationsMissionAction.WithdrawInterrupted); });
                Button(interruptedCard, Text("ui.common.cancel", "CANCEL"), () => confirmation.SetActive(false));
                confirmation.SetActive(false);
                return;
            }
            var trackerActions = Row(card); trackerActions.GetComponent<LayoutElement>().preferredHeight = 44;
            saveAndExit = Button(trackerActions, Text("operations.o001.save_exit", "SAVE & EXIT"), () => Send(UiOperationsMissionAction.SaveAndExit));
            guideButton = Button(transform, Text("operations.o001.open_guide", "MISSION GUIDE"), () => guideOpen = !guideOpen);
            var guideRect = (RectTransform)guideButton.transform;
            guideRect.anchorMin = new Vector2(.77f, .925f); guideRect.anchorMax = new Vector2(.89f, .99f);
            guideRect.offsetMin = guideRect.offsetMax = Vector2.zero;
            Frame(guideRect);
            ariaButton = Button(transform, Text("operations.o001.aria_play", "ARIA PLAY"), () =>
            {
                if (UiShellRuntimeGateway.ReadAriaPlay().Active) UiShellRuntimeGateway.StopAriaPlay();
                else UiShellRuntimeGateway.TryStartAriaPlay();
            });
            var ariaRect = (RectTransform)ariaButton.transform;
            ariaRect.anchorMin = new Vector2(.895f, .925f); ariaRect.anchorMax = new Vector2(.985f, .99f);
            ariaRect.offsetMin = ariaRect.offsetMax = Vector2.zero;
            Frame(ariaRect);
            foreach (var button in new[] { guideButton, saveAndExit, ariaButton })
                button.GetComponentInChildren<TMP_Text>().fontSize = 14;
            var actionPanel = Panel("MissionGuideDrawer", transform); controls = actionPanel.gameObject;
            actionPanel.anchorMin = new Vector2(.77f, .20f); actionPanel.anchorMax = new Vector2(.985f, .65f);
            actionPanel.offsetMin = actionPanel.offsetMax = Vector2.zero;
            actionPanel.GetComponent<Image>().color = new Color(.025f, .075f, .095f, .96f);
            Frame(actionPanel);
            Vertical(actionPanel, 5);
            var guideHeader = Row(actionPanel); guideHeader.GetComponent<LayoutElement>().preferredHeight = 40;
            var guideTitle = Label(guideHeader, Text("operations.o001.guide_title", "MISSION GUIDE"), 22, 40);
            guideTitle.color = new Color(.08f, .76f, .89f);
            Button(guideHeader, Text("ui.common.close", "CLOSE"), () => guideOpen = false);
            var guideHint = Label(actionPanel, Text("operations.o001.guide_hint", "Select infantry, advance to a signal, then scan nearby. Recover the evidence and reach extraction."), 16, 58);
            guideHint.alignment = TextAlignmentOptions.TopLeft;
            var signalsTitle = Label(actionPanel, Text("operations.o001.signals_title", "SIGNAL SITES"), 17, 26);
            signalsTitle.alignment = TextAlignmentOptions.Left;
            signalsTitle.color = new Color(.08f, .76f, .89f);
            siteLabels = new TMP_Text[3];
            scanButtons = new Button[3];
            focusButtons = new Button[5];
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var signalRow = Row(actionPanel); signalRow.name = "Signal" + i;
                signalRow.GetComponent<LayoutElement>().preferredHeight = 52;
                var focus = Button(signalRow, string.Format(Text("operations.o001.signal", "SIGNAL {0}"), (char)('A' + i)), () => Send(UiOperationsMissionAction.FocusSite, index));
                focusButtons[i] = focus;
                siteLabels[i] = focus.GetComponentInChildren<TMP_Text>();
                scanButtons[i] = Button(signalRow, Text("operations.scan", "SCAN SELECTED"), () => Send(UiOperationsMissionAction.ScanSite, index));
            }
            var evidenceRow = Row(actionPanel); evidenceRow.GetComponent<LayoutElement>().preferredHeight = 50;
            evidenceButton = Button(evidenceRow, Text("operations.evidence", "EVIDENCE"), () => Send(UiOperationsMissionAction.FocusEvidence));
            focusButtons[3] = evidenceButton;
            recoverButton = Button(evidenceRow, Text("operations.recover", "RECOVER"), () => Send(UiOperationsMissionAction.RecoverEvidence));
            var exitRow = Row(actionPanel); exitRow.GetComponent<LayoutElement>().preferredHeight = 50;
            focusButtons[4] = Button(exitRow, Text("operations.exit", "EXIT"), () => Send(UiOperationsMissionAction.FocusExit));
            conclude = Button(exitRow, Text("operations.conclude", "CONCLUDE"), () => Send(UiOperationsMissionAction.Conclude));
            var withdrawRow = Row(actionPanel); withdrawRow.GetComponent<LayoutElement>().preferredHeight = 42;
            Button(withdrawRow, Text("operations.withdraw", "WITHDRAW"), () => confirmation.SetActive(true));

            var resultCard = Modal("MissionResult"); resultPanel = resultCard.parent.gameObject;
            result = Label(resultCard, "", 34, 220);
            continueButton = Button(resultCard, Text("ui.common.continue", "CONTINUE"), () => Send(UiOperationsMissionAction.Return));
            resultPanel.SetActive(false);
            var confirmCard = Modal("ConfirmWithdraw"); confirmation = confirmCard.parent.gameObject;
            Label(confirmCard, Text("operations.withdraw.confirm", "Withdraw from Street Signals? This records a withdrawal and applies its district consequences. The mission continues until you confirm."), 28, 180);
            Button(confirmCard, Text("operations.withdraw", "WITHDRAW"), () => { confirmation.SetActive(false); Send(UiOperationsMissionAction.Withdraw); });
            Button(confirmCard, Text("ui.common.cancel", "CANCEL"), () => confirmation.SetActive(false));
            confirmation.SetActive(false);
            markers = new RectTransform[5];
            advanceButtons = new Button[5];
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = Panel("WorldObjective" + i, transform);
                markers[i].SetAsFirstSibling(); markers[i].anchorMin = markers[i].anchorMax = new Vector2(.5f,.5f);
                markers[i].sizeDelta = new Vector2(240,48);
                string label = i < 3 ? string.Format(Text("operations.o001.signal", "SIGNAL {0}"), (char)('A' + i)) : i == 3 ? Text("operations.evidence", "EVIDENCE") : Text("operations.exit", "EXIT");
                int markerIndex = i;
                var advance = markers[i].gameObject.AddComponent<Button>();
                advanceButtons[i] = advance;
                advance.targetGraphic = markers[i].GetComponent<Image>();
                advance.onClick.AddListener(() => Send(markerIndex < 3 ? UiOperationsMissionAction.AdvanceSite :
                    markerIndex == 3 ? UiOperationsMissionAction.AdvanceEvidence : UiOperationsMissionAction.AdvanceExit, markerIndex));
                var text = Label(markers[i], string.Format(Text("operations.o001.advance_marker", "ADVANCE: {0}"), label), 18, 0); Stretch(text.rectTransform);
            }
        }

        private void Update()
        {
            if (hud)
            {
                if (duplicateAria == null) duplicateAria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                if (duplicateAria != null && duplicateAria.gameObject.activeSelf)
                    duplicateAria.gameObject.SetActive(false);
            }
            if (!UiShellRuntimeGateway.TryReadOperationsMission(out var model)) return;
            if (!hud)
            {
                UiLocalizedText.Set(title, model.Title); UiLocalizedText.Set(description, model.Description);
                UiLocalizedText.Set(status, model.Status); UiLocalizedText.Set(clock, model.Clock);
                status.gameObject.SetActive(!string.IsNullOrWhiteSpace(model.Status));
                interruptedAttempt = model.InterruptedAttempt;
                deploy.interactable = model.CanDeploy && !model.CanResume;
                UiLocalizedText.Set(deploy.GetComponentInChildren<TMP_Text>(), interruptedAttempt ? Text("operations.o001.restart_attempt", "RESTART ATTEMPT") : Text("operations.deploy", "DEPLOY"));
                resume.gameObject.SetActive(model.CanResume);
                withdrawInterrupted.gameObject.SetActive(interruptedAttempt);
                return;
            }
            bool ready = UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.CurrentMode == UiShellMode.MatchHud &&
                shell.Phase is UiShellTransitionPhase.MatchHudReady or UiShellTransitionPhase.Idle && !shell.IsTransitionRunning;
            missionPlaying = ready && model.InMission && !model.Finished;
            briefing.SetActive(missionPlaying && guideOpen);
            controls.SetActive(missionPlaying && guideOpen);
            guideButton.gameObject.SetActive(missionPlaying);
            UiLocalizedText.Set(guideButton.GetComponentInChildren<TMP_Text>(), guideOpen
                ? Text("operations.o001.close_guide", "CLOSE GUIDE") : Text("operations.o001.open_guide", "MISSION GUIDE"));
            ariaButton.gameObject.SetActive(missionPlaying &&
                UiShellRuntimeGateway.ReadAriaPlayCapability() != AriaPlayCapability.None);
            if (ariaButton.gameObject.activeSelf)
                UiLocalizedText.Set(ariaButton.GetComponentInChildren<TMP_Text>(), UiShellRuntimeGateway.ReadAriaPlay().Active
                    ? Text("operations.o001.aria_stop", "STOP ARIA") : Text("operations.o001.aria_play", "ARIA PLAY"));
            saveAndExit.interactable = model.InMission && !model.Finished;
            resultPanel.SetActive(ready && model.Finished);
            if (model.Finished) confirmation.SetActive(false);
            UiLocalizedText.Set(title, model.Title); UiLocalizedText.Set(description, model.Objective);
            UiLocalizedText.Set(status, model.Status); UiLocalizedText.Set(clock, model.Clock);
            UiLocalizedText.Set(result, model.Result); continueButton.interactable = model.Saved;
            conclude.interactable = model.CanConclude;
            evidenceVisible = model.EvidenceAvailable;
            evidenceButton.interactable = evidenceVisible;
            recoverButton.interactable = model.CanRecover;
            UiLocalizedText.Set(recoverButton.GetComponentInChildren<TMP_Text>(), model.EvidenceStatus);
            if (model.SiteStatus != null)
                for (int i = 0; i < siteLabels.Length && i < model.SiteStatus.Length; i++) UiLocalizedText.Set(siteLabels[i], model.SiteStatus[i]);
        }

        public Button ScanButton(int index) => scanButtons != null && index >= 0 && index < scanButtons.Length ? scanButtons[index] : null;
        public Button AdvanceButton(int index) => advanceButtons != null && index >= 0 && index < advanceButtons.Length ? advanceButtons[index] : null;
        public Button FocusButton(int index) => focusButtons != null && index >= 0 && index < focusButtons.Length ? focusButtons[index] : null;
        public Button RecoverButton => recoverButton;
        public Button GuideButton => guideButton;
        public Button AriaButton => ariaButton;
        public bool GuideOpen => guideOpen;

        public void PresentMarkers(Vector3[] screenPositions)
        {
            if (markers == null) return;
            for (int i = 0; i < markers.Length; i++)
            {
                Vector3 point = screenPositions[i];
                bool visible = missionPlaying && (i != 3 || evidenceVisible) && point.z > 0 && point.x >= 0 && point.x <= Screen.width && point.y >= 0 && point.y <= Screen.height;
                markers[i].gameObject.SetActive(visible);
                if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, point, null, out Vector2 local))
                    markers[i].anchoredPosition = local + new Vector2(0, 24);
            }
        }

        public void ShowWithdrawConfirmation()
        {
            if (hud && confirmation != null) confirmation.SetActive(true);
        }

        private static void Send(UiOperationsMissionAction action, int index = 0) => UiShellRuntimeGateway.TryRequestOperationsMission(action, index);
        private static string Text(string key, string fallback) => UiShellRuntimeGateway.Localization.Get(key, fallback);
        private RectTransform Modal(string name)
        {
            var parent = hud ? transform : GetComponentInParent<Canvas>().rootCanvas.transform;
            var shade = Panel(name, parent); Stretch(shade); shade.GetComponent<Image>().color = new Color(0,0,0,.8f);
            var card = Panel("Card", shade); card.anchorMin = card.anchorMax = new Vector2(.5f,.5f); card.sizeDelta = hud ? new Vector2(850,430) : new Vector2(670,350);
            Vertical(card, 18); return card;
        }
        private RectTransform Panel(string name, Transform parent, bool background = true)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false);
            if (background) root.AddComponent<Image>().color = new Color(.025f,.07f,.08f,.97f);
            return (RectTransform)root.transform;
        }
        private static void Frame(RectTransform rect)
        {
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.07f, .61f, .68f, .62f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var rule = new GameObject("CyanAccentRule", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rule.transform.SetParent(rect, false);
            rule.GetComponent<LayoutElement>().ignoreLayout = true;
            var line = (RectTransform)rule.transform;
            line.anchorMin = new Vector2(0, 1); line.anchorMax = Vector2.one;
            line.pivot = new Vector2(.5f, 1);
            line.sizeDelta = new Vector2(0, 3);
            rule.GetComponent<Image>().color = new Color(.06f, .72f, .82f, .88f);
            rule.GetComponent<Image>().raycastTarget = false;
        }
        private TMP_Text Label(Transform parent, string value, float size, float height)
        {
            var root = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement)); root.transform.SetParent(parent, false);
            var label = root.GetComponent<TMP_Text>(); label.font = font != null ? font : TMP_Settings.defaultFontAsset;
            label.fontSize = size; label.color = Color.white; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            root.GetComponent<LayoutElement>().preferredHeight = height; UiLocalizedText.Set(label, value); return label;
        }
        private Button Button(Transform parent, string label, Action clicked)
        {
            var root = Panel(label, parent); root.gameObject.AddComponent<LayoutElement>().preferredHeight = hud ? 44 : 58;
            root.GetComponent<Image>().color = new Color(.04f,.26f,.29f,1);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = root.GetComponent<Image>(); button.onClick.AddListener(() => clicked());
            var colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1);
            colors.pressedColor = new Color(.68f, .9f, .93f, 1);
            colors.disabledColor = new Color(.46f, .5f, .5f, .72f);
            button.colors = colors;
            var text = Label(root, label, hud ? 16 : 20, 0); Stretch(text.rectTransform); return button;
        }
        private RectTransform Row(Transform parent)
        {
            var row = Panel("Row", parent, false); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 88;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 5; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; return row;
        }
        private static void Vertical(RectTransform rect, int spacing)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(12,12,10,10); layout.spacing = spacing;
            layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
