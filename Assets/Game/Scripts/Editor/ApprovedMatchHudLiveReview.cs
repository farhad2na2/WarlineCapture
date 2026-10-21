#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game.UI.Runtime;
using Game.Runtime;
using Game.UI.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace Game.Editor
{
 public static partial class CampaignTutorialEntryEditorProbe
 {
  static bool hudReviewBusy,hudReviewMenus,hudReviewDone,hudReviewPassengers,hudReviewBuild,hudReviewWheel;
  static string hudReviewOutput;
  static bool hudReviewSuite;
  static InputSettings hudReviewOriginalInput, hudReviewCaptureInput;
  static double hudReviewStart;
  public static void CaptureCurrentHudMenus(){hudReviewSuite=true;StartHudVisualReview(false);}
  public static void CaptureApprovedHudSuite(){hudReviewSuite=true;StartHudVisualReview(false);}
  public static void CaptureCurrentPassengers(){hudReviewSuite=true;StartHudVisualReview(true);}
  static void StartHudVisualReview(bool passengers)
  {
   hudReviewOriginalInput=InputSystem.settings;hudReviewCaptureInput=null;
   hudReviewWheel=false;hudReviewBuild=false;hudReviewPassengers=passengers;hudReviewMenus=hudReviewBusy=hudReviewDone=false;
   hudReviewOutput="Design/AgentReports/MatchHudReview20261021/Implementation/NativeAfter/"+(passengers?"passengers-":"menus-")+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
   Directory.CreateDirectory(hudReviewOutput);
   Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",Path.GetFullPath(hudReviewOutput+"/isolated-save"));
   Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");
   var saves=SaveService.CreateDefault();var profile=saves.LoadProfile();profile.firstLaunchStatus=FirstLaunchProfileState.Completed;profile.firstLaunchWatched=true;profile.firstLaunchLanguage="English";profile.firstLaunchCommanderDisplayName="Commander";saves.SaveProfile(profile);
   EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions|=EnterPlayModeOptions.DisableDomainReload;
   hudReviewStart=EditorApplication.timeSinceStartup;
   if(passengers)RunM04Journey();else RunM02Journey();
   EditorApplication.update+=HudVisualTick;
   Debug.Log("[MatchHudLiveVisualReview] output="+hudReviewOutput+" source=CurrentNativeGame implementationChanged=true");
  }
  static void HudVisualTick()
  {
   if(hudReviewDone||hudReviewBusy||!EditorApplication.isPlaying)return;
   if(EditorApplication.timeSinceStartup-hudReviewStart>360){EndHudVisualReview("TimedOut");return;}
   if(stage==2&&!hudReviewMenus)
   {hudReviewMenus=true;hudReviewBusy=true;waitUntil=EditorApplication.timeSinceStartup+600;CaptureHudMenus();return;}
   var picked=UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>();
   if(hudReviewMenus&&!hudReviewWheel&&picked!=null&&picked.HasWheelSelection&&Ready(picked.CommandWheelOpenButton)){hudReviewWheel=true;hudReviewBusy=true;waitUntil=EditorApplication.timeSinceStartup+600;CaptureWheelReview(picked);return;}
   var placement=UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>();
   var build=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
   if(!hudReviewBuild&&build!=null&&build.IsOpen&&HudVisible(build.DrawerRoot.transform)){hudReviewBuild=true;hudReviewBusy=true;waitUntil=EditorApplication.timeSinceStartup+600;CaptureBuildReview();return;}
   if(!hudReviewPassengers&&hudReviewMenus&&placement!=null&&placement.Root!=null&&HudVisible(placement.Root))
   {hudReviewBusy=true;waitUntil=EditorApplication.timeSinceStartup+600;CaptureHudTerminal("placement-confirmation");return;}
   var selection=UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>();
   if(hudReviewPassengers&&selection!=null&&selection.IsPassengerDrawerOpen)
   {hudReviewBusy=true;CaptureHudTerminal("passengers");}
  }
  static async void CaptureHudMenus()
  {
   try
   {
    Application.runInBackground=true;var input=UnityEngine.Object.Instantiate(InputSystem.settings);hudReviewCaptureInput=input;input.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;input.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=input;
    EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();await Task.Delay(700);await HudShot("01-hud");
    var selection=UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>();
    var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
    if(selection!=null&&!Ready(selection.CommandWheelOpenButton)&&aria!=null&&Ready(aria.SelectionButton)){aria.SelectionButton.onClick.Invoke();await Task.Delay(650);}
    if(selection!=null&&Ready(selection.CommandWheelOpenButton))
    {
     selection.CommandWheelOpenButton.onClick.Invoke();await Task.Delay(650);await HudShot("02-command-menu");
     var wheel=UnityEngine.Object.FindAnyObjectByType<CommandWheelPanelView>();wheel?.Close();await Task.Delay(300);
    }
    UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.OpenResourceExchange);await Task.Delay(900);await HudShot("03-exchange-request");
    UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.CloseResourceExchange);await Task.Delay(500);
    UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause);await Task.Delay(800);
    var pause=UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
    if(pause!=null&&Ready(pause.ResumeButton))
    {
     await HudShot("04-pause");
     if(Ready(pause.SettingsButton)){pause.SettingsButton.onClick.Invoke();await Task.Delay(800);await HudShot("05-settings");
      var settings=UnityEngine.Object.FindAnyObjectByType<SettingsPopupView>();if(settings!=null&&Ready(settings.CloseButton))settings.CloseButton.onClick.Invoke();await Task.Delay(600);}
     pause=UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();if(pause!=null&&Ready(pause.ResumeButton))pause.ResumeButton.onClick.Invoke();await Task.Delay(600);
    }
    if(hudReviewPassengers){approvedPreviousInput=InputSystem.settings;approvedMenuTouch=new AriaTouchInputUiSystemHelper();if(!approvedMenuTouch.Start())throw new InvalidOperationException("Capture normal-selection touch unavailable");SessionState.SetBool(ApprovedAtlasJourney,true);}
    EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();hudReviewBusy=false;waitUntil=EditorApplication.timeSinceStartup+.2;deadline=EditorApplication.timeSinceStartup+300;
   }
   catch(Exception e){Debug.LogException(e);EndHudVisualReview("Error");}
  }
  static async void CaptureWheelReview(MatchHudSelectionPanelView selection){try{await HudShot("selected-hud");var commands=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();if(commands!=null&&Ready(commands.MoveButton)){commands.MoveButton.onClick.Invoke();await Task.Delay(400);await HudShot("feedback-move");}selection.CommandWheelOpenButton.onClick.Invoke();await Task.Delay(650);await HudShot("command-menu");UnityEngine.Object.FindAnyObjectByType<CommandWheelPanelView>()?.Close();await Task.Delay(350);if(hudReviewPassengers&&selection.TryReadPresentedTransportControls(out var transport)&&Ready(transport.PassengerChip)){transport.PassengerChip.onClick.Invoke();await Task.Delay(600);await HudShot("passengers");EndHudVisualReview("Captured");return;}hudReviewBusy=false;waitUntil=EditorApplication.timeSinceStartup+.2;}catch(Exception e){Debug.LogException(e);EndHudVisualReview("Error");}}
  static bool HudVisible(Transform tr){if(tr==null||!tr.gameObject.activeInHierarchy)return false;for(var p=tr;p!=null;p=p.parent)foreach(var c in p.GetComponents<CanvasGroup>())if(c.alpha<.85f)return false;return true;}
  static async void CaptureBuildReview(){try{await Task.Delay(200);await HudShot("build-menu");hudReviewBusy=false;waitUntil=EditorApplication.timeSinceStartup+.2;deadline=EditorApplication.timeSinceStartup+300;}catch(Exception e){Debug.LogException(e);EndHudVisualReview("Error");}}
  static async void CaptureHudTerminal(string label)
  {try{await Task.Delay(500);await HudShot(label);EndHudVisualReview("Captured");}catch(Exception e){Debug.LogException(e);EndHudVisualReview("Error");}}
  static async Task HudShot(string label)
  {
   if (!hudReviewSuite) { await HudShotAtCurrentSize(label); return; }
   foreach (var sample in new[] { ("en",1920,1080), ("en",1280,720), ("fa-IR",1920,1080), ("fa-IR",2400,1080) })
   {
    Game.Configs.GameLocalization.SetLocale(sample.Item1,false);
    MainMenuV3PrefabBuilder.SetGameViewResolution(sample.Item2,sample.Item3);
    await Task.Delay(650);
    await HudShotAtCurrentSize(label+"-"+sample.Item1+"-"+sample.Item2);
   }
   Game.Configs.GameLocalization.SetLocale("en",false);
   MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);await Task.Delay(400);
  }
  static async Task HudShotAtCurrentSize(string label)
  {
   Canvas.ForceUpdateCanvases();
   var lines=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.gameObject.activeInHierarchy).Select(b=>b.name+" interactable="+b.IsInteractable()+" text="+string.Join("|",b.GetComponentsInChildren<TMPro.TMP_Text>().Select(t=>t.text)));
   File.WriteAllLines(hudReviewOutput+"/"+label+"-controls.txt",lines);
   ScreenCapture.CaptureScreenshot(hudReviewOutput+"/"+label+".png");await Task.Delay(650);
   Debug.Log("[MatchHudLiveVisualReview] captured="+label+" route="+(UiShellRuntimeGateway.TryReadShellState(out var state)?state.ActiveRoute.ToString():"unknown"));
  }
  static void EndHudVisualReview(string reason)
  {
   hudReviewDone=finished=true;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;EditorApplication.update-=HudVisualTick;
   Application.logMessageReceived-=Observe;Canvas.willRenderCanvases-=TickGameView;
   approvedMenuTouch?.Dispose();approvedMenuTouch=null;SessionState.EraseBool(ApprovedAtlasJourney);
   if(hudReviewOriginalInput!=null)InputSystem.settings=hudReviewOriginalInput;
   if(hudReviewCaptureInput!=null)UnityEngine.Object.DestroyImmediate(hudReviewCaptureInput);hudReviewCaptureInput=null;
   AssetDatabase.AllowAutoRefresh();
   Debug.Log("[MatchHudLiveVisualReview] result="+reason+" output="+hudReviewOutput+" missionReadiness=NotClaimed noOutcomesInjected=true");
   hudReviewSuite=false;
   MissionEditorValidationExit.Complete(reason=="Captured");
  }
 }
}
#endif
