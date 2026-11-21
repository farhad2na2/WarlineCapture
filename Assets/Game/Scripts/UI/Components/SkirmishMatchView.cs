using System;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Skirmish presentation only; all match changes cross the UI gateway.</summary>
    public sealed partial class SkirmishMatchView : MonoBehaviour, IUiBackOverlayParticipant
    {
        private GameObject hud, modal;
        private BuildDrawerView buildDrawer;
        private MatchHudFullMapPopupView fullMap;
        private RectTransform mapInformation;
        private readonly System.Collections.Generic.List<GameObject> authoredMapInformation = new();
        private TMP_Text mapName, mapPlayer, mapPlayerHealth, mapEnemy, mapEnemyHealth, mapClockHeading, mapClock;
        private MatchHudThreatVisibilityView threatWarning;
        private readonly Vector3[] warningCorners = new Vector3[4];
        private static TMP_FontAsset interfaceFont;
        private TMP_Text player, enemy, playerHealth, enemyHealth, clock, confirmText;
        private GameObject confirmationActions;
        private CampaignStyleResultCard resultCard;
        private UiSkirmishAction pendingAction;
        private bool confirming;
        private static readonly Color Background=new(.025f,.07f,.08f,.97f);
        private static readonly Color Cyan=new(0,.73f,.85f,1);

        public bool IsOpen=>modal!=null&&modal.activeInHierarchy;
        private void OnEnable()=>UiBackOverlayRegistry.Register(this);
        private void OnDisable()=>UiBackOverlayRegistry.Unregister(this);
        public bool HandleBack()
        {
            if(!IsOpen)return false;
            if(confirming){confirming=false;modal.SetActive(false);return true;}
            if(UiShellRuntimeGateway.TryReadSkirmish(out var model)&&model.Finished)
            {
                if(!string.IsNullOrEmpty(model.CatalogId)) ReturnToLibrary(model);
                else Send(UiSkirmishAction.MainMenu);
            }
            return true;
        }
        public static SkirmishMatchView Create()
        {
            interfaceFont=null;
            foreach(var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if(font.name=="Oxanium-Bold SDF"){interfaceFont=font;break;}
            var root=new GameObject("SkirmishMatchCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32700;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=1;
            return root.AddComponent<SkirmishMatchView>();
        }
        private void Awake()
        {
            // W40 (Board 2): one compact objective card; the base controls keep their names and handlers.
            var strip=BuildObjectiveCard();hud=strip.gameObject;
            player=Button(objectiveStatus,"YOUR MAIN BASE",()=>FocusPresentedObjectiveOrBase(true));
            playerHealth=HealthLabel(player);
            HudIconButtonView.Apply(PlayerFocusButton, HudButtonIcon.Camera, HudButtonRole.Friendly,
                "YOUR BASE", "پایگاه شما", player, true, HudButtonLayout.TitleLineIcon);
            CreateObjectiveScore(objectiveStatus);
            enemy=Button(objectiveStatus,"ENEMY MAIN BASE",()=>FocusPresentedObjectiveOrBase(false));
            enemyHealth=HealthLabel(enemy);
            HudIconButtonView.Apply(EnemyFocusButton, HudButtonIcon.Camera, HudButtonRole.Enemy,
                "ENEMY BASE", "پایگاه دشمن", enemy, true, HudButtonLayout.TitleLineIcon);
            var shade=Panel("SkirmishResult",transform,false);modal=shade.gameObject;Stretch(shade);
            UiChromeStack.RaiseAboveTutorial(shade.gameObject, UiChromeStack.ConfirmationChrome);
            shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.72f);
            resultCard=CampaignStyleResultCard.Mount(shade, interfaceFont);
            resultCard.Bind(
                ()=>Send(UiSkirmishAction.Restart),
                ()=>Send(UiSkirmishAction.Replay),
                ()=>Send(UiSkirmishAction.MainMenu),
                ()=>Send(UiSkirmishAction.AdjustSetup));
            resultCard.BindBattlefield(()=>
            {
                if(UiShellRuntimeGateway.TryReadSkirmish(out var open)) ReturnToLibrary(open);
            });
            var confirmation=Panel("ConfirmationActions",shade,false);
            confirmation.anchorMin=confirmation.anchorMax=new Vector2(.5f,.5f);
            confirmation.sizeDelta=new Vector2(760,280);
            confirmationActions=confirmation.gameObject;Vertical(confirmation);
            confirmText=Label(confirmation,"",30,90);
            Button(confirmation,"CONFIRM",()=>{confirming=false;Send(pendingAction);});
            Button(confirmation,"CANCEL",()=>{confirming=false;modal.SetActive(false);UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);});
            modal.SetActive(false);
        }
        public void Confirm(UiSkirmishAction action)
        {
            pendingAction=action;confirming=true;
            UiLocalizedText.Set(confirmText,action==UiSkirmishAction.Surrender?"Surrender this match? This records a defeat.":"Restart this skirmish with the same setup?");
        }
        public Game.Skirmish.Contracts.SkirmishPublicSelectedApproach SelectedApproach { get; private set; }
        private void Update()
        {
            if(!UiShellRuntimeGateway.TryReadSkirmish(out var model)){hud.SetActive(false);modal.SetActive(false);return;}
            SelectedApproach = model.SelectedApproach;
            ObserveEnemyBaseKnowledge(model);
            bool ready=UiShellRuntimeGateway.TryReadShellState(out var shell)&&shell.CurrentMode==UiShellMode.MatchHud&&
                shell.Phase is UiShellTransitionPhase.MatchHudReady or UiShellTransitionPhase.Idle && !shell.IsTransitionRunning;
            if(buildDrawer==null)buildDrawer=FindAnyObjectByType<BuildDrawerView>();
            ready &= buildDrawer==null||!buildDrawer.IsOpen;
            if (fullMap == null) fullMap = FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            ready &= fullMap == null || !fullMap.IsOpen;
            RefreshMapInformation(model);
            hud.SetActive(ready&&!model.Finished&&!model.StartupFailed&&!confirming);
            if (hud.activeSelf) PositionBelowWarning();
            // The base controls' titles belong to HudIconButtonView; only their status line is set here.
            SetBaseDetail(playerHealth,model.PlayerBase);SetBaseDetail(enemyHealth,EnemyReadout(model));RefreshClock(model.Clock);
            RefreshPublicObjectiveHud(model);
            RefreshMissionGuidance(model);
            PresentObjectiveCard(model);
            RefreshBaseUnderFire(model);
            bool terminal=model.Finished||model.StartupFailed;
            modal.SetActive(terminal||confirming);
            if(!modal.activeSelf)return;
            resultCard.gameObject.SetActive(terminal);
            if(terminal) resultCard.Present(BuildPresentedSkirmishResult(model, BaseLostToRole));
            confirmText.gameObject.SetActive(confirming&&!terminal);
            confirmationActions.SetActive(confirming&&!terminal);
        }
        private void RefreshMapInformation(UiSkirmishModel model)
        {
            if (fullMap == null || !fullMap.IsOpen) return;
            if (mapInformation == null)
            {
                RectTransform source = null;
                foreach (var rect in fullMap.GetComponentsInChildren<RectTransform>(true))
                    if (rect.name == "MapInfoPanel") { source = rect; break; }
                if (source == null) return;
                foreach (Transform child in source)
                    if (child.gameObject.activeSelf)
                    {
                        authoredMapInformation.Add(child.gameObject);
                        child.gameObject.SetActive(false);
                    }
                mapInformation = Panel("SkirmishMapInformation", source, false);
                Stretch(mapInformation);
                var layout = mapInformation.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(10, 10, 12, 12);
                layout.spacing = 6;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                mapName = Label(mapInformation, "", 22, 42);
                mapName.color = Cyan;
                mapPlayer = Label(mapInformation, "", 18, 30);
                mapPlayerHealth = Label(mapInformation, "", 24, 32);
                mapEnemy = Label(mapInformation, "", 18, 30);
                mapEnemyHealth = Label(mapInformation, "", 24, 32);
                mapClockHeading = Label(mapInformation, "", 18, 30);
                mapClock = Label(mapInformation, "", 24, 32);
            }
            string mapTitleKey = model.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex
                ? "ui.skirmish.industrial_basin_map"
                : model.ScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex
                    ? "ui.skirmish.city_crossroads_map"
                    : "ui.skirmish.base_assault_map";
            string mapTitleFallback = model.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex
                ? "INDUSTRIAL BASIN"
                : model.ScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex
                    ? "CITY CROSSROADS"
                    : "DESERT BASE";
            UiLocalizedText.Set(mapName, string.IsNullOrEmpty(model.MissionTitle)
                ? UiShellRuntimeGateway.Localization.Get(mapTitleKey, mapTitleFallback) : model.MissionTitle);
            SetBaseLabel(mapPlayer, mapPlayerHealth, model.PlayerBase);
            SetBaseLabel(mapEnemy, mapEnemyHealth, model.EnemyBase);
            var time = (model.Clock ?? string.Empty).Split(new[] { "  " }, StringSplitOptions.None);
            UiLocalizedText.Set(mapClockHeading, time[0]);
            UiLocalizedText.Set(mapClock, time.Length > 1 ? time[1] : string.Empty);
        }

        private void OnDestroy()
        {
            // The map belongs to the shared HUD. Restore its authored content when
            // this skirmish ends, even if the shell retains the popup for another mode.
            foreach (var child in authoredMapInformation)
                if (child != null) child.SetActive(true);
            if (mapInformation != null) Destroy(mapInformation.gameObject);
        }

        private static void Send(UiSkirmishAction action)=>UiShellRuntimeGateway.TryRequestSkirmish(action);
        private static void ReturnToLibrary(UiSkirmishModel model)
        {
            string scenarioId = !string.IsNullOrEmpty(model.CatalogId) ? model.CatalogId : model.MedalScenarioId;
            SkirmishLibraryReturn.Queue(scenarioId);
            foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Include))
                setup.ReturnLibraryToScenario(scenarioId);
            Send(UiSkirmishAction.AdjustSetup);
        }
        // The enemy-base readout frames its reported or last-seen position when tapped. While the base
        // is out of sight a forward chevron marks it as a control (mirrored for right-to-left).
        private static string EnemyReadout(in UiSkirmishModel model)
        {
            string source = model.EnemyBase ?? string.Empty;
            if (!model.EnemyBaseKnowledgeKnown || model.EnemyBaseKnowledge == SkirmishEnemyBaseMarkerState.ReadoutVisible) return source;
            int line = source.IndexOf('\n');
            return line < 0 || line == source.Length - 1 ? source : source + " \u203A";
        }
        private static ModeResultContent BuildResult(UiSkirmishModel model, Game.Skirmish.Contracts.SkirmishRoleKind baseLostTo = Game.Skirmish.Contracts.SkirmishRoleKind.None)
        {
            string mapKey = model.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex ? "ui.skirmish.industrial_basin_map"
                : model.ScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex ? "ui.skirmish.city_crossroads_map"
                : "ui.skirmish.base_assault_map";
            string mapFallback = model.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex ? "INDUSTRIAL BASIN"
                : model.ScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex ? "CITY CROSSROADS" : "DESERT BASE";
            string map = string.IsNullOrEmpty(model.MissionTitle)
                ? UiShellRuntimeGateway.Localization.Get(mapKey, mapFallback) : model.MissionTitle;
            // The Battle Library card name ("COMMAND SIEGE") so the player recognises the retry.
            string libraryName = SkirmishLibraryMissionNames.ForCatalogId(model.CatalogId);
            string Text(string key, string fallback) => UiShellRuntimeGateway.Localization.Get("ui.skirmish.result." + key, fallback);
            string complete = Text("complete", "Complete");
            string failed = Text("failed", "Failed");
            bool victory = model.ResultStars > 0;
            SkirmishResultMedalLine medal = SkirmishResultMedalPresentation.Describe(
                model.MedalIsFirstAward, model.MedalScenarioId, model.MedalDifficulty);
            string medalText = string.Empty;
            if (medal.Visible)
            {
                string first = UiShellRuntimeGateway.Localization.Get(medal.FirstLocKey, "First medal");
                string difficulty = UiShellRuntimeGateway.Localization.Get(medal.DifficultyLocKey, "Medal");
                medalText = first + " · " + model.MedalScenarioId + " · " + difficulty;
            }
            // Each criterion is judged by its own condition (SkirmishStarCriteria), so a defeat
            // with no squad lost shows the loss criterion as met, not failed.
            var criteria = new SkirmishStarCriteria(victory, model.PlayerBuildingsLost, model.PlayerUnitsLost);
            string needsVictory = Text("met_needs_victory", "MET · NEEDS A WIN");
            return new ModeResultContent
            {
                Victory = victory,
                Title = string.IsNullOrEmpty(model.ResultTitle) ? (victory ? "VICTORY" : "DEFEAT") : model.ResultTitle,
                Identity = string.IsNullOrEmpty(libraryName)
                    ? Text("mode", "SKIRMISH") + "\n" + map
                    : Text("mode", "SKIRMISH") + " · " + model.CatalogId + "\n" + libraryName,
                ObjectiveTitle = model.EnemyBaseDestroyed
                    ? Text("enemy_destroyed", "Enemy main base destroyed.")
                    : model.PlayerBaseHeld ? Text("base_standing", "Your main base is still standing.")
                    : baseLostTo != Game.Skirmish.Contracts.SkirmishRoleKind.None ? BaseLossRecap(baseLostTo)
                    : Text("base_lost", "Your main base was lost."),
                Status = model.StartupFailed ? Text("startup_failed", "MATCH DID NOT START") : victory ? Text("match_complete", "MATCH COMPLETE") : model.ResultTitle,
                Elapsed = model.ResultElapsed,
                Stars = model.ResultStars,
                Objective1 = Text("destroy_base", "Destroy the enemy main base"),
                Objective2 = Text("protect_base", "Protect your main base"),
                Objective3 = UiShellRuntimeGateway.Localization.Format("ui.skirmish.result.low_losses_limit",
                    "Keep squad losses low ({0} or fewer)", SkirmishStarCriteria.LowLossLimit),
                Objective1State = model.EnemyBaseDestroyed ? complete : failed,
                Objective2State = CriterionText(criteria.ProtectBase, Text("held", "Held"), needsVictory,
                    model.PlayerBaseHeld
                        ? UiShellRuntimeGateway.Localization.Format("ui.skirmish.result.buildings_lost_count", "{0} BUILDINGS LOST", model.PlayerBuildingsLost)
                        : failed),
                Objective3State = CriterionText(criteria.LowLosses, complete, needsVictory,
                    UiShellRuntimeGateway.Localization.Format("ui.skirmish.result.units_lost_over_limit", "{0} LOST · LIMIT {1}",
                        model.PlayerUnitsLost, SkirmishStarCriteria.LowLossLimit)),
                HasObjectiveFacts = true,
                CompletedObjectives = (byte)((model.EnemyBaseDestroyed ? 1 : 0) |
                    (criteria.ProtectBase == SkirmishCriterionState.Earned ? 2 : 0) |
                    (criteria.LowLosses == SkirmishCriterionState.Earned ? 4 : 0)),
                MetNeedsVictoryObjectives = (byte)(
                    (criteria.ProtectBase == SkirmishCriterionState.MetNeedsVictory ? 2 : 0) |
                    (criteria.LowLosses == SkirmishCriterionState.MetNeedsVictory ? 4 : 0)),
                Performance1 = Text("units_lost", "UNITS LOST"),
                Performance1Value = model.PlayerUnitsLost.ToString(),
                Performance2 = Text("units_defeated", "UNITS DEFEATED"),
                Performance2Value = model.EnemyUnitsLost.ToString(),
                Performance3 = Text("buildings", "BUILDINGS LOST / DESTROYED"),
                Performance3Value = model.PlayerBuildingsLost + " / " + model.EnemyBuildingsLost,
                // Answers "why is a criterion I met not a star?": stars need a victory.
                Summary = (string.IsNullOrEmpty(model.ResultDetail) ? model.Objective : model.ResultDetail) +
                          (!victory && (criteria.MetMask & 6) != 0
                              ? "\n" + Text("star_rule", "Stars need a win. Gold criteria were met and count when you win.")
                              : string.Empty),
                LeaveLabel = "MAIN MENU",
                ShowAdjust = true,
                ActionsEnabled = true,
                ShowMedal = medal.Visible,
                MedalText = medalText,
                ShowBattlefieldBack = !string.IsNullOrEmpty(model.CatalogId),
                BattlefieldBackLabel = Text("battlefield", "BATTLEFIELD"),
                Signature = model.ResultTitle + "|" + model.ResultStars + "|" + model.PlayerUnitsLost + "|" + model.ScenarioIndex + "|" +
                            (medal.Visible ? model.MedalScenarioId : string.Empty) + "|" + model.CatalogId + "|" + (int)baseLostTo + "|" +
                            UiShellRuntimeGateway.Localization.CurrentLocaleCode
            };
        }
        // One recap line naming what destroyed the base, only when the player saw that attacker.
        private static string BaseLossRecap(Game.Skirmish.Contracts.SkirmishRoleKind role)
        {
            var text = UiShellRuntimeGateway.Localization;
            string label = SkirmishSetupBriefing.RoleLabel(role, text.IsRightToLeft);
            if (string.IsNullOrEmpty(label)) return text.Get("ui.skirmish.result.base_lost", "Your main base was lost.");
            return text.Format(SkirmishBaseLossRecap.RecapKey(role), SkirmishBaseLossRecap.RecapFallback(role), label);
        }
        private static string CriterionText(SkirmishCriterionState state, string earned, string metNeedsVictory, string failed) =>
            state == SkirmishCriterionState.Earned ? earned
            : state == SkirmishCriterionState.MetNeedsVictory ? metNeedsVictory
            : failed;
        // Keep numeric health out of the RTL heading so current/maximum retains its order.
        private static TMP_Text HealthLabel(TMP_Text heading)
        {
            heading.rectTransform.anchorMin=new Vector2(0,.5f);
            var health=Label(heading.transform.parent,"",HudIconButtonView.LabelSize);
            Stretch(health.rectTransform);
            health.rectTransform.anchorMax=new Vector2(1,.5f);
            health.rectTransform.offsetMin=new Vector2(8,5);
            health.rectTransform.offsetMax=new Vector2(-8,0);
            return health;
        }
        private static void SetBaseLabel(TMP_Text heading,TMP_Text health,string source)
        {
            var lines=(source??string.Empty).Split('\n');
            UiLocalizedText.Set(heading,lines[0]);
            UiLocalizedText.Set(health,lines.Length>1?lines[1]:string.Empty);
        }
        private static void SetBaseDetail(TMP_Text health,string source)
        {
            var lines=(source??string.Empty).Split('\n');
            UiLocalizedText.Set(health,lines.Length>1?lines[1]:string.Empty);
        }
        private static void AlignBaseHealth(TMP_Text health)
        {
            bool rtl=UiShellRuntimeGateway.Localization.IsRightToLeft;
            health.rectTransform.offsetMin=new Vector2(rtl?12:76,4);
            health.rectTransform.offsetMax=new Vector2(rtl?-76:-12,-4);
            health.enableAutoSizing=false;
            health.fontSize=HudIconButtonView.LabelSize;
        }
        private void PositionBelowWarning()
        {
            if (threatWarning == null) threatWarning = FindAnyObjectByType<MatchHudThreatVisibilityView>(FindObjectsInactive.Include);
            float warning = 0;
            if (threatWarning != null && threatWarning.gameObject.activeInHierarchy)
            {
                var warningRect = (RectTransform)threatWarning.transform;
                var canvas = warningRect.GetComponentInParent<Canvas>();
                warningRect.GetWorldCorners(warningCorners);
                var screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, warningCorners[0]);
                var root = (RectTransform)transform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var bottom))
                    warning = root.rect.yMax - bottom.y;
            }
            // W40: 12 px under the resource bar, or under a visible alert banner.
            float top = SkirmishObjectiveCardLayout.Top(ResourceBarBottom(), warning);
            // While a base alert is up the strip (and the alert row under it) keeps its place when the
            // top banner expires; it returns once the alert clears.
            top = SkirmishBaseAlertRowLayout.HoldDown(top, ref baseFireStripHeld, BaseUnderFirePresented);
            var rect = (RectTransform)hud.transform;
            rect.anchoredPosition = new Vector2(0, -top);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, SkirmishObjectiveCardLayout.Height);
        }
        private static RectTransform Panel(string name,Transform parent,bool visible=true)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            if(visible){var graphic=go.AddComponent<V3GradientGraphic>();graphic.Configure(new Color(.10f,.14f,.15f),Background,Cyan,2);}
            return rect;
        }
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static void Vertical(RectTransform rect)
        {var layout=rect.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;rect.gameObject.AddComponent<LayoutElement>().preferredHeight=210;}
        private static TMP_Text Label(Transform parent,string text,float size,float height=0)
        {
            var rect=Panel("Label",parent,false);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.fontSize=size;label.color=Color.white;
            label.font=interfaceFont!=null?interfaceFont:TMP_Settings.defaultFontAsset;label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.Normal;label.raycastTarget=false;
            if(height>0)rect.gameObject.AddComponent<LayoutElement>().preferredHeight=height;
            UiLocalizedText.Set(label,text);return label;
        }
        private static TMP_Text Button(Transform parent,string text,Action action)
        {
            var rect=Panel(text,parent);rect.gameObject.GetComponent<V3GradientGraphic>().Configure(new Color(.03f,.32f,.42f),new Color(.015f,.12f,.17f),Cyan,2);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight=62;
            var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>action());
            var label=Label(rect,text,30);Stretch(label.rectTransform);label.rectTransform.offsetMin=new Vector2(8,5);label.rectTransform.offsetMax=new Vector2(-8,-5);return label;
        }
    }
}
