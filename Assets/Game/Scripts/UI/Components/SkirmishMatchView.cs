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
            if(UiShellRuntimeGateway.TryReadSkirmish(out var model)&&model.Finished)Send(UiSkirmishAction.MainMenu);
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
            var strip=Panel("BaseObjectives",transform);hud=strip.gameObject;
            strip.anchorMin=new Vector2(.30f,1);strip.anchorMax=new Vector2(.73f,1);strip.pivot=new Vector2(.5f,1);
            strip.offsetMin=new Vector2(0,-225);strip.offsetMax=new Vector2(0,-110);
            var row=strip.gameObject.AddComponent<HorizontalLayoutGroup>();row.padding=new RectOffset(8,8,8,8);row.spacing=8;row.childControlWidth=true;row.childControlHeight=true;row.childForceExpandWidth=true;
            row.childScaleWidth=false;
            player=Button(strip,"YOUR MAIN BASE",()=>Send(UiSkirmishAction.FocusPlayer));
            player.transform.parent.GetComponent<LayoutElement>().preferredWidth=280;player.fontSize=28;
            playerHealth=HealthLabel(player);
            HudIconButtonView.Apply(PlayerFocusButton, HudButtonIcon.YourBase, HudButtonRole.Play,
                "YOUR BASE", "پایگاه شما", player, true);
            clock=Label(strip,"TIME LEFT",30);clock.gameObject.AddComponent<LayoutElement>().preferredWidth=240;
            enemy=Button(strip,"ENEMY MAIN BASE",()=>Send(UiSkirmishAction.FocusEnemy));
            enemy.transform.parent.GetComponent<LayoutElement>().preferredWidth=280;enemy.fontSize=28;
            enemyHealth=HealthLabel(enemy);
            HudIconButtonView.Apply(EnemyFocusButton, HudButtonIcon.EnemyBase, HudButtonRole.Stop,
                "ENEMY BASE", "پایگاه دشمن", enemy, true);
            var shade=Panel("SkirmishResult",transform,false);modal=shade.gameObject;Stretch(shade);
            shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.72f);
            resultCard=CampaignStyleResultCard.Mount(shade, interfaceFont);
            resultCard.Bind(
                ()=>Send(UiSkirmishAction.Restart),
                ()=>Send(UiSkirmishAction.Replay),
                ()=>Send(UiSkirmishAction.MainMenu),
                ()=>Send(UiSkirmishAction.AdjustSetup));
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
        private void Update()
        {
            if(!UiShellRuntimeGateway.TryReadSkirmish(out var model)){hud.SetActive(false);modal.SetActive(false);return;}
            bool ready=UiShellRuntimeGateway.TryReadShellState(out var shell)&&shell.CurrentMode==UiShellMode.MatchHud&&
                shell.Phase is UiShellTransitionPhase.MatchHudReady or UiShellTransitionPhase.Idle && !shell.IsTransitionRunning;
            if(buildDrawer==null)buildDrawer=FindAnyObjectByType<BuildDrawerView>();
            ready &= buildDrawer==null||!buildDrawer.IsOpen;
            if (fullMap == null) fullMap = FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            ready &= fullMap == null || !fullMap.IsOpen;
            RefreshMapInformation(model);
            hud.SetActive(ready&&!model.Finished&&!model.StartupFailed&&!confirming);
            if (hud.activeSelf) PositionBelowWarning();
            SetBaseLabel(player,playerHealth,model.PlayerBase);SetBaseLabel(enemy,enemyHealth,model.EnemyBase);UiLocalizedText.Set(clock,model.Clock);
            AlignBaseHealth(playerHealth); AlignBaseHealth(enemyHealth);
            clock.color=(model.Clock??string.Empty).Contains("  0:")?new Color(1,.65f,.1f):Color.white;
            bool terminal=model.Finished||model.StartupFailed;
            modal.SetActive(terminal||confirming);
            if(!modal.activeSelf)return;
            resultCard.gameObject.SetActive(terminal);
            if(terminal) resultCard.Present(BuildResult(model));
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
            UiLocalizedText.Set(mapName, UiShellRuntimeGateway.Localization.Get(mapTitleKey, mapTitleFallback));
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
        private static ModeResultContent BuildResult(UiSkirmishModel model)
        {
            string mapKey = model.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex ? "ui.skirmish.industrial_basin_map"
                : model.ScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex ? "ui.skirmish.city_crossroads_map"
                : "ui.skirmish.base_assault_map";
            string mapFallback = model.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex ? "INDUSTRIAL BASIN"
                : model.ScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex ? "CITY CROSSROADS" : "DESERT BASE";
            string map = UiShellRuntimeGateway.Localization.Get(mapKey, mapFallback);
            string complete = "COMPLETE";
            string failed = "FAILED";
            bool victory = model.ResultStars > 0;
            return new ModeResultContent
            {
                Victory = victory,
                Title = string.IsNullOrEmpty(model.ResultTitle) ? (victory ? "VICTORY" : "DEFEAT") : model.ResultTitle,
                Identity = "SKIRMISH\n" + map,
                ObjectiveTitle = model.EnemyBaseDestroyed
                    ? "Enemy main base destroyed."
                    : model.PlayerBaseHeld ? "Your main base is still standing." : "Your main base was lost.",
                Status = model.StartupFailed ? "MATCH DID NOT START" : victory ? "MATCH COMPLETE" : model.ResultTitle,
                Elapsed = model.ResultElapsed,
                Stars = model.ResultStars,
                Objective1 = "Destroy the enemy main base",
                Objective2 = "Protect your main base",
                Objective3 = "Keep squad losses low",
                Objective1State = model.EnemyBaseDestroyed ? complete : failed,
                Objective2State = model.PlayerBaseHeld ? "HELD" : failed,
                Objective3State = victory && model.PlayerUnitsLost <= 2 ? complete : failed,
                Performance1 = "UNITS LOST",
                Performance1Value = model.PlayerUnitsLost.ToString(),
                Performance2 = "UNITS DEFEATED",
                Performance2Value = model.EnemyUnitsLost.ToString(),
                Performance3 = "BUILDINGS LOST / DESTROYED",
                Performance3Value = model.PlayerBuildingsLost + " / " + model.EnemyBuildingsLost,
                Summary = string.IsNullOrEmpty(model.ResultDetail) ? model.Objective : model.ResultDetail,
                LeaveLabel = "MAIN MENU",
                ShowAdjust = true,
                ActionsEnabled = true,
                Signature = model.ResultTitle + "|" + model.ResultStars + "|" + model.PlayerUnitsLost + "|" + model.ScenarioIndex + "|" +
                            UiShellRuntimeGateway.Localization.CurrentLocaleCode
            };
        }
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
            float top = 110;
            if (threatWarning != null && threatWarning.gameObject.activeInHierarchy)
            {
                var warningRect = (RectTransform)threatWarning.transform;
                var canvas = warningRect.GetComponentInParent<Canvas>();
                warningRect.GetWorldCorners(warningCorners);
                var screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, warningCorners[0]);
                var root = (RectTransform)transform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var bottom))
                    top = Mathf.Max(top, root.rect.yMax - bottom.y + 12);
            }
            var rect = (RectTransform)hud.transform;
            rect.offsetMin = new Vector2(rect.offsetMin.x, -top - 115);
            rect.offsetMax = new Vector2(rect.offsetMax.x, -top);
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
