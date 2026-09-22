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
        private Button deploy, conclude, continueButton, evidenceButton, recoverButton;
        private bool evidenceVisible, interruptedAttempt;
        private Button withdrawInterrupted;
        private GameObject briefing, controls, resultPanel, confirmation;
        private RectTransform[] markers;

        public static void Install(GameObject body)
        {
            if (body == null) return;
            var existing = body.GetComponentInChildren<TMP_Text>(true);
            var font = existing != null ? existing.font : TMP_Settings.defaultFontAsset;
            foreach (Transform child in body.transform) child.gameObject.SetActive(false);
            var root = new GameObject("StreetSignalsBriefing", typeof(RectTransform));
            root.transform.SetParent(body.transform, false);
            root.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                new Vector2(1672, 941), MainMenuV3SectionAlignment.Center);
            var view = root.AddComponent<OperationsMissionScreenView>(); view.font = font; view.Build(false);
        }

        public static OperationsMissionScreenView CreateHud(TMP_FontAsset font)
        {
            var root = new GameObject("OperationsMissionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32600;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var view = root.AddComponent<OperationsMissionScreenView>(); view.font = font; view.Build(true); return view;
        }

        private void Build(bool isHud)
        {
            hud = isHud;
            var card = Panel("MissionCard", transform);
            if (hud)
            { card.anchorMin = new Vector2(.28f, .76f); card.anchorMax = new Vector2(.75f, .90f); card.offsetMin = card.offsetMax = Vector2.zero; }
            else
            { card.anchorMin = new Vector2(.15f, .13f); card.anchorMax = new Vector2(.85f, .86f); card.offsetMin = card.offsetMax = Vector2.zero; }
            Vertical(card, hud ? 1 : 14);
            if (hud) card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(6,6,4,4);
            title = Label(card, "STREET SIGNALS", hud ? 22 : 42, hud ? 26 : 60);
            description = Label(card, "", hud ? 22 : 28, hud ? 26 : 160);
            status = Label(card, "", hud ? 20 : 22, hud ? 48 : 100);
            clock = Label(card, "", hud ? 22 : 24, hud ? 26 : 30);
            briefing = card.gameObject;
            if (!hud)
            {
                deploy = Button(card, Text("operations.deploy", "DEPLOY"), () => Send(interruptedAttempt ? UiOperationsMissionAction.RestartAttempt : UiOperationsMissionAction.Deploy));
                withdrawInterrupted = Button(card, Text("operations.withdraw", "WITHDRAW"), () => confirmation.SetActive(true));
                var interruptedCard = Modal("ConfirmInterruptedWithdraw"); confirmation = interruptedCard.parent.gameObject;
                Label(interruptedCard, Text("operations.o001.interrupted_withdraw_confirm", "Withdraw from the interrupted attempt? This spends the reserved action point and applies withdrawal consequences."), 28, 180);
                Button(interruptedCard, Text("operations.withdraw", "WITHDRAW"), () => { confirmation.SetActive(false); Send(UiOperationsMissionAction.WithdrawInterrupted); });
                Button(interruptedCard, Text("ui.common.cancel", "CANCEL"), () => confirmation.SetActive(false));
                confirmation.SetActive(false);
                Button(card, Text("ui.common.back", "BACK"), () => UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.MainMenu, false));
                return;
            }
            var actionPanel = Panel("MissionActions", transform); controls = actionPanel.gameObject;
            actionPanel.anchorMin = new Vector2(.28f, .56f); actionPanel.anchorMax = new Vector2(.75f, .76f);
            actionPanel.offsetMin = actionPanel.offsetMax = Vector2.zero;
            Vertical(actionPanel, 4);
            var row = Row(actionPanel); row.GetComponent<LayoutElement>().preferredHeight = 124; siteLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var column = Panel("Signal" + i, row, false); Vertical(column, 2);
                var focus = Button(column, string.Format(Text("operations.o001.signal", "SIGNAL {0}"), (char)('A' + i)), () => Send(UiOperationsMissionAction.FocusSite, index));
                siteLabels[i] = focus.GetComponentInChildren<TMP_Text>();
                Button(column, Text("operations.scan", "SCAN SELECTED"), () => Send(UiOperationsMissionAction.ScanSite, index));
            }
            var secondRow = Row(actionPanel);
            secondRow.GetComponent<LayoutElement>().preferredHeight = 64;
            evidenceButton = Button(secondRow, Text("operations.evidence", "EVIDENCE"), () => Send(UiOperationsMissionAction.FocusEvidence));
            recoverButton = Button(secondRow, Text("operations.recover", "RECOVER"), () => Send(UiOperationsMissionAction.RecoverEvidence));
            Button(secondRow, Text("operations.exit", "EXIT"), () => Send(UiOperationsMissionAction.FocusExit));
            conclude = Button(secondRow, Text("operations.conclude", "CONCLUDE"), () => Send(UiOperationsMissionAction.Conclude));
            Button(secondRow, Text("operations.withdraw", "WITHDRAW"), () => confirmation.SetActive(true));

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
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = Panel("WorldObjective" + i, transform);
                markers[i].SetAsFirstSibling(); markers[i].anchorMin = markers[i].anchorMax = new Vector2(.5f,.5f);
                markers[i].sizeDelta = new Vector2(240,48);
                string label = i < 3 ? string.Format(Text("operations.o001.signal", "SIGNAL {0}"), (char)('A' + i)) : i == 3 ? Text("operations.evidence", "EVIDENCE") : Text("operations.exit", "EXIT");
                int markerIndex = i;
                var advance = markers[i].gameObject.AddComponent<Button>();
                advance.targetGraphic = markers[i].GetComponent<Image>();
                advance.onClick.AddListener(() => Send(markerIndex < 3 ? UiOperationsMissionAction.AdvanceSite :
                    markerIndex == 3 ? UiOperationsMissionAction.AdvanceEvidence : UiOperationsMissionAction.AdvanceExit, markerIndex));
                var text = Label(markers[i], string.Format(Text("operations.o001.advance_marker", "ADVANCE: {0}"), label), 18, 0); Stretch(text.rectTransform);
            }
        }

        private void Update()
        {
            if (!UiShellRuntimeGateway.TryReadOperationsMission(out var model)) return;
            if (!hud)
            {
                UiLocalizedText.Set(title, model.Title); UiLocalizedText.Set(description, model.Description);
                UiLocalizedText.Set(status, model.Status); UiLocalizedText.Set(clock, model.Clock);
                interruptedAttempt = model.InterruptedAttempt;
                deploy.interactable = model.CanDeploy;
                UiLocalizedText.Set(deploy.GetComponentInChildren<TMP_Text>(), interruptedAttempt ? Text("operations.o001.restart_attempt", "RESTART ATTEMPT") : Text("operations.deploy", "DEPLOY"));
                withdrawInterrupted.gameObject.SetActive(interruptedAttempt);
                // Keep the extra recovery choice inside the briefing card at 1080p.
                description.GetComponent<LayoutElement>().preferredHeight = interruptedAttempt ? 130 : 160;
                status.GetComponent<LayoutElement>().preferredHeight = interruptedAttempt ? 80 : 100;
                return;
            }
            bool ready = UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.CurrentMode == UiShellMode.MatchHud &&
                shell.Phase is UiShellTransitionPhase.MatchHudReady or UiShellTransitionPhase.Idle && !shell.IsTransitionRunning;
            briefing.SetActive(ready && model.InMission && !model.Finished);
            controls.SetActive(briefing.activeSelf);
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

        public void PresentMarkers(Vector3[] screenPositions)
        {
            if (markers == null) return;
            for (int i = 0; i < markers.Length; i++)
            {
                Vector3 point = screenPositions[i];
                bool visible = briefing.activeSelf && (i != 3 || evidenceVisible) && point.z > 0 && point.x >= 0 && point.x <= Screen.width && point.y >= 0 && point.y <= Screen.height;
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
            var shade = Panel(name, transform); Stretch(shade); shade.GetComponent<Image>().color = new Color(0,0,0,.8f);
            var card = Panel("Card", shade); card.anchorMin = card.anchorMax = new Vector2(.5f,.5f); card.sizeDelta = new Vector2(850,430);
            Vertical(card, 18); return card;
        }
        private RectTransform Panel(string name, Transform parent, bool background = true)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false);
            if (background) root.AddComponent<Image>().color = new Color(.025f,.07f,.08f,.97f);
            return (RectTransform)root.transform;
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
            var text = Label(root, label, hud ? 18 : 28, 0); Stretch(text.rectTransform); return button;
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
