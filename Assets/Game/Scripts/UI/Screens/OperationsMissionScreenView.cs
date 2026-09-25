using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Native presentation. Checklist and markers never issue troop orders.</summary>
    public sealed partial class OperationsMissionScreenView : MonoBehaviour
    {
        private bool hud, interruptedAttempt;
        private TMP_FontAsset font;
        private TMP_Text title, description, status, clock, result;
        private Button deploy, resume, withdrawInterrupted, continueButton;
        private GameObject confirmation, resultPanel;
        private RectTransform safeRoot, tracker, introduction, tour;
        private readonly TMP_Text[] objectiveRows = new TMP_Text[3];
        private readonly V3GradientGraphic[] objectiveFrames = new V3GradientGraphic[3];
        private TMP_Text guidance, progress, tourCaption, introObjective, introFacts, introActionText, focusText, recoverText;
        private Button focusButton, introButton, skipButton, recoverButton, ariaPlayButton;
        private TMP_Text ariaPlayText;
        private bool ariaWasPlaying;
        private readonly RectTransform[] markers = new RectTransform[5];
        private readonly TMP_Text[] markerLabels = new TMP_Text[5];
        private readonly Vector3[] markerPositions = new Vector3[5];
        private UiOperationsMissionModel current;
        private AriaTutorialBriefingView sharedAria;
        private Sprite portrait;
        private bool sharedAriaWasActive;
        private bool startAriaWhenResumed;
        private MatchOverlayCommandControlsView commandControls;
        private OperationsScanAreaGraphic scanAreas;
        private Camera worldCamera;
        private static readonly Color Gold = new Color32(255,192,43,255), Cyan = new Color32(0,190,230,255),
            Green = new Color32(74,188,77,255), Muted = new Color32(175,186,188,255);
        public bool IsHud => hud;
        public Button IntroductionButton => introButton;
        public Button SkipButton => skipButton;
        public Button ObjectiveButton => focusButton;
        public Button RecoverButton => recoverButton;
        public Button ScanButton(int index) => commandControls != null ? commandControls.ScanButton : null;
        public Button GuideButton => focusButton;
        public bool GuideOpen => tracker != null && tracker.gameObject.activeInHierarchy;
        public Button AriaButton => ariaPlayButton;
        public void StartAriaWhenResumed() => startAriaWhenResumed = true;

        public static void Install(GameObject body)
        {
            if (body == null) return;
            var dashboard = body.GetComponentInChildren<OperationsDashboardScreenView>(true);
            Transform parent = dashboard != null && dashboard.DailyBriefing != null ? dashboard.DailyBriefing : body.transform;
            if (dashboard != null && dashboard.DailyBriefing != null)
                foreach (Transform child in dashboard.DailyBriefing)
                    if (child.name is "AriaPortraitClip" or "Briefing") child.gameObject.SetActive(false);
            var root = Node("StreetSignalsMissionCard", parent); Stretch(root);
            root.offsetMin = new Vector2(12,8); root.offsetMax = new Vector2(-12,-102);
            var view = root.gameObject.AddComponent<OperationsMissionScreenView>();
            view.font = body.GetComponentInChildren<TMP_Text>(true)?.font ?? TMP_Settings.defaultFontAsset;
            view.BuildMenu();
        }
        public static OperationsMissionScreenView CreateHud(TMP_FontAsset font)
        {
            var root = new GameObject("OperationsMissionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32600;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 1;
            var view = root.AddComponent<OperationsMissionScreenView>(); view.hud = true;
            view.commandControls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (UiShellRuntimeGateway.TryReadMatchHudSelection(out var selection) && !selection.Visible)
                UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>()?.ClearActiveSlot();
            view.font = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>()?.GetComponentInChildren<TMP_Text>(true)?.font ?? font;
            view.sharedAria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>(FindObjectsInactive.Include);
            if (view.sharedAria != null)
            { view.portrait = view.sharedAria.PortraitImage?.sprite; view.sharedAriaWasActive = view.sharedAria.gameObject.activeSelf; }
            view.BuildHud(); return view;
        }
        private void BuildMenu()
        {
            // DailyBriefing leaves 252 points below its existing theater/day header.
            // Keep every action inside that space, above the warnings panel.
            var card = Surface("MissionCard", transform); Stretch(card); Vertical(card,6,8);
            title = Label(card,"",24,48); description = Label(card,"",20,76);
            status = Label(card,"",18,40);
            var actions = Row(card,54);
            actions.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            deploy = Button(actions,Text("operations.deploy","DEPLOY"),() => Send(interruptedAttempt ? UiOperationsMissionAction.RestartAttempt : UiOperationsMissionAction.Deploy),Green);
            resume = Button(actions,T("resume_attempt","RESUME ATTEMPT"),() => Send(UiOperationsMissionAction.ResumeAttempt),Green);
            withdrawInterrupted = Button(actions,Text("operations.withdraw","WITHDRAW"),() => confirmation.SetActive(true),Muted);
            BuildConfirmation();
        }
        private void BuildHud()
        {
            safeRoot = Node("SafeArea",transform); Stretch(safeRoot);
            var rings = Node("SignalScanAreas", safeRoot); Stretch(rings);
            scanAreas = rings.gameObject.AddComponent<OperationsScanAreaGraphic>(); scanAreas.raycastTarget = false;
            tracker = Surface("ObjectiveTracker",safeRoot); TopRight(tracker,500,800,20,20); Vertical(tracker,8,20);
            tracker.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            title = Label(tracker,T("short_title","STREET SIGNALS"),38,42); title.color = Gold;
            Label(tracker,T("district","OLD QUARTER"),27,30); clock = Label(tracker,"",27,36); Rule(tracker);
            for (int i=0;i<3;i++)
            {
                var row = Surface("Objective"+i,tracker); Height(row,i==2 ? 108 : 64);
                objectiveFrames[i] = row.GetComponent<V3GradientGraphic>();
                objectiveRows[i] = Label(row,"",28,0); Stretch(objectiveRows[i].rectTransform,16);
            }
            Rule(tracker); progress = Label(tracker,"",25,34); progress.color = Cyan;
            var aria = Row(tracker,172); Portrait(aria,112,128);
            var copy = Node("AriaGuidance",aria); copy.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1; Vertical(copy,6,0);
            Label(copy,"ARIA",29,38).color = Cyan; guidance = Label(copy,"",25,128);
            focusButton = Button(tracker,T("show_objective","SHOW OBJECTIVE"),() => Send(current.ViewingObjective ? UiOperationsMissionAction.FocusSquad : UiOperationsMissionAction.FocusObjective),Cyan);
            focusText = focusButton.GetComponentInChildren<TMP_Text>();
            ariaPlayButton = Button(tracker,T("aria_play","ARIA PLAY"),() =>
            {
                if (UiShellRuntimeGateway.ReadAriaPlay().Active)
                { startAriaWhenResumed = false; UiShellRuntimeGateway.StopAriaPlay(); }
                else startAriaWhenResumed = true;
            },Green);
            ariaPlayText = ariaPlayButton.GetComponentInChildren<TMP_Text>();

            introduction = Surface("DeploymentBriefing",safeRoot,new Color(0,0,0,.65f)); Stretch(introduction);
            var card = Surface("BriefingCard",introduction); Center(card,1460,890); Vertical(card,20,36);
            Label(card,T("short_title","STREET SIGNALS"),54,70).color = Gold;
            Label(card,T("intro_subtitle","OLD QUARTER  •  OPERATION 01  •  MISSION PAUSED"),28,46);
            var content = Row(card,455);
            var portraitColumn = Node("Aria",content); Height(portraitColumn,455); Width(portraitColumn,310); Vertical(portraitColumn,12,8);
            Portrait(portraitColumn,270,290);
            Label(portraitColumn,T("purpose","Find the active relay. Recover its evidence and bring your squad home."),29,138);
            var objectives = Node("Plan",content); objectives.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1; Vertical(objectives,18,16);
            Label(objectives,T("your_objectives","YOUR OBJECTIVES"),34,50); introObjective = Label(objectives,"",33,280);
            introFacts = Label(objectives,T("force_deadline","16 INFANTRY  •  12 MINUTES"),30,44);
            Label(card,T("optional_recon","Optional: finish all scans without losing recon infantry."),27,46);
            introButton = Button(card,T("show_plan","SHOW THE PLAN"),() => Send(UiOperationsMissionAction.StartIntroduction),Green);
            Height((RectTransform)introButton.transform,90); introActionText = introButton.GetComponentInChildren<TMP_Text>(); introduction.gameObject.SetActive(false);

            tour = Surface("CameraIntroduction",safeRoot); TopRight(tour,500,620,20,20); Vertical(tour,16,26);
            Label(tour,"ARIA",36,48).color = Cyan; Portrait(tour,150,125); tourCaption = Label(tour,"",29,150);
            skipButton = Button(tour,T("skip_tour","RETURN TO SQUAD"),() => Send(UiOperationsMissionAction.SkipIntroduction),Green); tour.gameObject.SetActive(false);
            for (int i=0;i<5;i++)
            {
                markers[i] = Surface("ObjectiveMarker"+i,safeRoot); markers[i].SetAsFirstSibling(); markers[i].sizeDelta = new Vector2(270,56);
                markers[i].GetComponent<V3GradientGraphic>().raycastTarget = false;
                markerLabels[i] = Label(markers[i],"",26,0,TextAlignmentOptions.Center); Stretch(markerLabels[i].rectTransform,8);
                markerLabels[i].alignment = TextAlignmentOptions.Center; markerLabels[i].color = Gold;
            }
            recoverButton = Button(safeRoot,T("recover_action","RECOVER EVIDENCE"),() => Send(UiOperationsMissionAction.RecoverEvidence),Green);
            ((RectTransform)recoverButton.transform).sizeDelta = new Vector2(340,78); recoverText = recoverButton.GetComponentInChildren<TMP_Text>(); recoverButton.gameObject.SetActive(false);
            var resultCard = Modal("MissionResult",out resultPanel); result = Label(resultCard,"",38,280);
            continueButton = Button(resultCard,Text("ui.common.continue","CONTINUE"),() => Send(UiOperationsMissionAction.Return),Green); resultPanel.SetActive(false);
            BuildConfirmation();
        }
        private void BuildConfirmation()
        {
            var card = Modal("ConfirmWithdraw",out confirmation);
            Label(card,Text("operations.withdraw.confirm","Withdraw from Street Signals? This ends the attempt and applies its district consequences."),30,185);
            Button(card,Text("operations.withdraw","WITHDRAW"),() =>
            {
                confirmation.SetActive(false);
                Send(hud ? UiOperationsMissionAction.Withdraw : UiOperationsMissionAction.WithdrawInterrupted);
                if (hud) UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);
            },Gold);
            Button(card,Text("ui.common.cancel","CANCEL"),() => confirmation.SetActive(false),Muted); confirmation.SetActive(false);
        }
        private void Update()
        {
            RefreshLocaleTypography();
            if (!UiShellRuntimeGateway.TryReadOperationsMission(out current)) return;
            if (!hud)
            {
                Set(title,current.Title); Set(description,current.Description);
                Set(status,string.IsNullOrEmpty(current.Status) ? current.Clock : current.Status);
                interruptedAttempt = current.InterruptedAttempt; deploy.interactable = current.CanDeploy && !current.CanResume;
                deploy.gameObject.SetActive(!current.CanResume);
                Set(deploy.GetComponentInChildren<TMP_Text>(),interruptedAttempt ? T("restart_attempt","RESTART ATTEMPT") : Text("operations.deploy","DEPLOY"));
                resume.gameObject.SetActive(current.CanResume); withdrawInterrupted.gameObject.SetActive(interruptedAttempt); return;
            }
            var safe = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height); safeRoot.anchorMax = new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            bool ready = UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.CurrentMode == UiShellMode.MatchHud && !shell.IsTransitionRunning;
            bool live = ready && current.InMission && !current.Finished;
            bool touringSite = current.Touring && current.IntroductionStage is >= 1 and <= 3;
            scanAreas.gameObject.SetActive(live && (touringSite || !current.Introduction && !current.Paused));
            if (startAriaWhenResumed && live && !current.Paused && Time.timeScale > 0)
                startAriaWhenResumed = !UiShellRuntimeGateway.TryStartAriaPlay();
            if (sharedAria != null && live) sharedAria.gameObject.SetActive(false);
            tracker.gameObject.SetActive(live && !current.Introduction && !current.Paused);
            introduction.gameObject.SetActive(live && current.Introduction && !current.Touring); tour.gameObject.SetActive(live && current.Touring);
            resultPanel.SetActive(ready && current.Finished); if (current.Finished) confirmation.SetActive(false);
            bool ariaPlaying = UiShellRuntimeGateway.ReadAriaPlay().Active;
            Set(ariaPlayText,ariaPlaying ? T("aria_stop","STOP ARIA") : T("aria_play","ARIA PLAY"));
            if (ariaPlaying != ariaWasPlaying)
            {
                var accent = ariaPlaying ? new Color(1,.22f,.08f) : Green;
                Color upper = accent*.65f, lower = accent*.25f; upper.a = lower.a = 1;
                ariaPlayButton.GetComponent<V3GradientGraphic>().Configure(upper,lower,accent,3);
                ariaWasPlaying = ariaPlaying;
            }
            Set(clock,current.Clock);
            Set(objectiveRows[0],string.Format(T("check_scans","1   Scan signal sites   {0}/3"),current.CompletedScans));
            Set(objectiveRows[1],T("check_evidence","2   Recover relay evidence"));
            Set(objectiveRows[2],T("check_extract","3   Extract with 2+ infantry\nBring the evidence carrier."));
            int active = current.CompletedScans < 3 ? 0 : current.EvidenceCarried ? 2 : 1;
            for (int i=0;i<3;i++)
            {
                objectiveRows[i].color = i > active ? Muted : Color.white;
                objectiveFrames[i].Configure(new Color(.1f,.14f,.16f),new Color(.03f,.06f,.075f),i<active ? Green : i==active ? Gold : Muted,i==active ? 3 : 1);
            }
            Set(progress,current.Progress); progress.gameObject.SetActive(!string.IsNullOrEmpty(current.Progress)); Set(guidance,current.Guidance);
            Set(focusText,current.ViewingObjective ? T("return_squad","RETURN TO SQUAD") : T("show_objective","SHOW OBJECTIVE")); Set(tourCaption,current.Guidance);
            Set(introObjective,current.Resumed ? current.Objective : T("intro_objectives","1   Scan all 3 signal sites\n\n2   Recover the relay evidence\n\n3   Extract with evidence and 2+ infantry"));
            Set(introFacts,current.Resumed ? current.Clock : T("force_deadline","16 INFANTRY  •  12 MINUTES"));
            Set(introActionText,current.Resumed ? T("resume_mission","RESUME MISSION") : T("show_plan","SHOW THE PLAN"));
            Set(result,current.Result); continueButton.interactable = current.Saved;
            for (int i=0;i<5;i++)
            {
                bool complete = i<3 && current.SiteCompleted != null && current.SiteCompleted[i];
                Set(markerLabels[i],i<3 ? complete ? (i+1)+"  •  "+T("done","DONE") : string.Format(T("site_marker","{0}  •  SIGNAL SITE"),i+1) : i==3 ? Text("operations.evidence","EVIDENCE") : T("extraction_marker","EXTRACTION"));
                bool channeling = i < 3 && !complete && current.SiteProgress != null && current.SiteProgress[i] > 0;
                markers[i].sizeDelta = new Vector2(270,channeling ? 84 : 56);
                if (channeling)
                    Set(markerLabels[i], string.Format(T("scanning_site", "Scanning site {0}   {1}/15 s"), i+1, Mathf.FloorToInt(current.SiteProgress[i])));
                markerLabels[i].color = complete ? Green : Gold;
                bool tourTarget = current.Touring && (i < 3 && current.IntroductionStage == i + 1 || i == 4 && current.IntroductionStage == 4);
                bool gameplayTarget = !current.Introduction && !current.Paused && (i!=4 || current.EvidenceCarried) && (i!=3 || current.EvidenceAvailable && !current.EvidenceCarried);
                bool visible = live && (tourTarget || gameplayTarget) && InWorldViewport(markerPositions[i]);
                markers[i].gameObject.SetActive(visible); if (visible) PlaceMarker(markers[i],markerPositions[i],new Vector2(0,42));
            }
            bool recover = live && !current.Introduction && !current.Paused && current.CanRecoverHere && InWorldViewport(markerPositions[3]);
            recoverButton.gameObject.SetActive(recover);
            if (recover)
            {
                recoverButton.interactable = current.EvidenceProgress <= 0;
                Set(recoverText,current.EvidenceProgress > 0 ? current.EvidenceStatus : T("recover_action","RECOVER EVIDENCE"));
                PlaceMarker((RectTransform)recoverButton.transform,markerPositions[3],new Vector2(0,-44));
            }
        }
        public void PresentMarkers(Vector3[] positions) => Array.Copy(positions,markerPositions,Math.Min(5,positions.Length));
        public void PresentScanArea(int index, Camera camera, Vector3 center, float radius, bool complete)
        { worldCamera = camera; if (scanAreas != null) scanAreas.Present(index, camera, center, radius, complete); }
        public void ShowWithdrawConfirmation() { if (confirmation != null) confirmation.SetActive(true); }
        public bool TryObserveObjective(out Vector2 point)
        {
            int i = current.NextSite >= 0 ? current.NextSite : current.EvidenceCarried ? 4 : 3;
            Vector3 projected = markerPositions[i];
            if (worldCamera != null && !current.EvidenceCarried)
            {
                // A signal is attached to a building. The public range ring also
                // includes its approach; tap that ground instead of attacking the facade.
                Vector3 approach = worldCamera.transform.position - current.ObjectivePosition; approach.y = 0;
                float radius = current.NextSite >= 0 ? 8f : 6f;
                projected = worldCamera.WorldToScreenPoint(current.ObjectivePosition + approach.normalized * (radius * .75f));
            }
            point = projected; return InWorldViewport(projected);
        }
        private bool InWorldViewport(Vector3 p) => p.z>0 && Screen.safeArea.Contains(p) && p.y>Screen.height*.25f && p.y<Screen.height*.88f && !RectTransformUtility.RectangleContainsScreenPoint(tracker,p,null);
        private void PlaceMarker(RectTransform rect,Vector3 screen,Vector2 offset)
        { if (RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot,screen,null,out var local)) rect.anchoredPosition = local+offset; }
        private void OnDestroy() { if (sharedAria != null) sharedAria.gameObject.SetActive(sharedAriaWasActive); }
        private static void Send(UiOperationsMissionAction action,int index=0) => UiShellRuntimeGateway.TryRequestOperationsMission(action,index);
        private static string Text(string key,string fallback) => UiShellRuntimeGateway.Localization.Get(key,fallback);
        private static string T(string key,string fallback) => Text("operations.o001."+key,fallback);
        private static void Set(TMP_Text label,string value) { if (label != null) UiLocalizedText.Set(label,value); }
    }
}
