using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public sealed partial class MatchHudAssistantUiSystemHelperTests
{
    [TestCase("en")]
    [TestCase("fa-IR")]
    public void SkirmishBriefingKeepsItsLayoutAcrossAllHudUpdates(string locale)
    {
        GameLocalization.SetLocale(locale, false);
        Assert.AreEqual(locale, GameLocalization.CurrentLocaleCode);
        CreateHudHarness(true, out var overlay, out var header, out _);
        var model = new UiAssistantPanelModel(1, "", "", "", false, false, true,
            GameText.Get("ui.skirmish.base_assault", "BASE ASSAULT"),
            GameText.Get("ui.skirmish.match_help", "Destroy the enemy Main Base."),
            "", "", false, false, false, false, "", "");
        var gateway = new FakeAssistantPanelGateway(model, UiAssistantHighlightModel.Empty) { Skirmish = true };
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        try
        {
            ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
            var helper = GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
            var view = header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
            helper.ApplyReadModel(model);
            view.ApplyAccessibility(false, false);
            view.RefreshContentLayout();
            var expected = ((RectTransform)view.transform).rect;
            for (int frame = 0; frame < 20; frame++)
            {
                helper.ApplyReadModel(model);
                Check();
                helper.TickHighlight(frame);
                Check();
                view.ApplyAccessibility(false, false);
                view.RefreshContentLayout();
                Check();
            }
            void Check()
            {
                Assert.IsTrue(view.IsPresentationVisible);
                Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf, "No campaign action may reappear between updates.");
                Assert.IsFalse(view.ContinueButton.gameObject.activeSelf);
                Assert.AreEqual(expected, ((RectTransform)view.transform).rect, "The briefing must not alternate height within a frame.");
                Assert.AreEqual(model.RecommendationBody, view.CurrentInstructionBody);
            }
        }
        finally { ui.Dispose(); }
    }

    private sealed partial class FakeAssistantPanelGateway : IUiSkirmishGateway
    {
        public bool Skirmish;
        public bool TryReadSkirmish(out UiSkirmishModel model) { model = default; return Skirmish; }
        public int SkirmishRequestCount;
        public bool TryRequestSkirmish(UiSkirmishAction action) { SkirmishRequestCount++; return false; }
    }
}


public sealed partial class MatchHudAssistantUiSystemHelperTests
{
    // Native screen-space control; invoked after a Play Mode frame by the entry below.
    private IEnumerator SkirmishOffscreenAdvanceMapGesturesOnlyRequestCamera(bool insideViewport)
    {
        using var f = new SkirmishAdvanceMapFixture();
        yield return null;
        var view = f.View();
        Assert.IsFalse(Screen.safeArea.Contains(f.BattleCamera.WorldToScreenPoint(f.Ground)));
        f.Observe(ref view);
        Assert.IsFalse(view.AdvanceGround.Available);
        Assert.IsTrue(view.FocusAdvance.Available);
        Assert.AreEqual(-20010, view.FocusAdvance.Id);
        Assert.AreEqual(0, f.OpenRequests);
        Assert.AreEqual(0, f.FocusRequests);
        f.Gesture(view);
        Assert.AreEqual(1, f.OpenRequests);
        Assert.AreEqual(0, f.FocusRequests, "Opening the map is not a focus or troop order.");
        f.SetExpanded(true, insideViewport);
        yield return null;
        view = f.View(); f.Observe(ref view);
        Assert.IsTrue(view.FocusAdvance.Available);
        Assert.AreEqual(-20011, view.FocusAdvance.Id);
        Assert.AreEqual(insideViewport, view.FocusAdvanceDrag);
        if (insideViewport)
            Assert.Greater((view.FocusAdvanceDragEnd-view.FocusAdvance.Position).sqrMagnitude, 25);
        else
        {
            Assert.IsTrue(f.Map.TryGetPresentedMapPoint(f.Ground, null, out var expected));
            Assert.Less(Vector2.Distance(expected, view.FocusAdvance.Position), .01f);
        }
        f.Gesture(view);
        Assert.AreEqual(1, f.FocusRequests, "A drag's following click must not focus twice.");
        Assert.AreEqual(insideViewport, f.Map.HasManualViewportOverride);
        Assert.AreEqual(0, f.Gateway.SkirmishRequestCount);
        Assert.AreEqual(0, f.Gateway.AssistantIntentRequestCount);
        f.AssertReadableCamera();
    }

    // Native screen-space control; invoked after a Play Mode frame by the entry below.
    private IEnumerator ExpandedSkirmishAdvanceClosesMapBeforeSeparateWorldTarget(bool insideViewport)
    {
        using var f = new SkirmishAdvanceMapFixture();
        yield return null;
        var view = f.View(); f.Observe(ref view);
        var plan = new AriaSkirmishPlanComponent();
        var touch = new AriaPlaySessionComponent { Phase=AriaPlayPhase.Observing };
        var output = new AriaPlayObservationComponent();
        AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
        AssertCameraControl(output, -20010);
        f.Gesture(view); Complete(ref touch, output);
        f.SetExpanded(true, insideViewport);
        yield return null;
        view=f.View();view.Time=2;view.Frame=2;f.Observe(ref view);
        AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
        AssertCameraControl(output, -20011);
        Assert.AreEqual(insideViewport ? 1 : 0, output.Drag);
        if (insideViewport) Assert.AreEqual(view.FocusAdvanceDragEnd, output.DragEnd);
        f.Gesture(view); Complete(ref touch, output);
        // Settle the fixture camera from the actual public map event. The observer
        // itself is read-only; production camera settlement remains a native gate.
        f.AimAt(new Vector3(f.LastFocus.x*200, 0, f.LastFocus.y*200));
        view.Time=3;view.Frame=3;
        AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
        AssertCameraControl(output, view.CloseMap.Id);
        f.CloseButton.onClick.Invoke(); Complete(ref touch, output);
        yield return null;
        Assert.AreEqual(1, f.CloseRequests);
        bool worldTarget=false;
        for (int frame=4;frame<=12;frame++)
        {
            view=f.View();view.Time=frame;view.Frame=frame;f.Observe(ref view);
            Assert.IsTrue(view.AdvanceGround.Available, "The settled approach must be a real reachable screen point. "+f.DescribeGround());
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            if (output.Kind==AriaPlayObservationKind.WorldTarget)
            {
                Assert.AreEqual(-20006, output.TargetId);
                Assert.AreEqual(view.AdvanceGround.Position, output.Position);
                worldTarget=true;break;
            }
            Assert.AreNotEqual(-20010, output.TargetId, "Do not reopen the map after its one focus gesture.");
        }
        Assert.IsTrue(worldTarget, "Only after camera focus and map closure may the planner offer Attack Move.");
        Assert.AreEqual(1, f.OpenRequests);Assert.AreEqual(1, f.FocusRequests);
        Assert.AreEqual(0, f.Gateway.SkirmishRequestCount);
        Assert.AreEqual(0, f.Gateway.AssistantIntentRequestCount);
        f.AssertReadableCamera();
    }

    private static void AssertCameraControl(AriaPlayObservationComponent output, int id)
    {
        Assert.AreEqual(AriaPlayObservationKind.Control, output.Kind,
            "Camera guidance must never be a WorldTarget/order.");
        Assert.AreEqual(id, output.TargetId);
    }
    private static void Complete(ref AriaPlaySessionComponent touch, AriaPlayObservationComponent output)
    {
        touch.Actions++;touch.TargetId=output.TargetId;
        touch.LastCompletedTargetId=output.TargetId;touch.LastCompletedFrame=output.Frame;
        touch.LastCompletedPosition=output.Position;
    }

    private static IEnumerator advanceNativeCases;
    private static int advanceNativeFrame=-1;
    private const string AdvanceNativeKey = "Warline.AdvanceMapNative.20261016";

    public static void RunSkirmishAdvanceMapValidation()
    {
        string token = Environment.GetEnvironmentVariable("WARLINE_ADVANCE_MAP_NATIVE_RUN");
        if (string.IsNullOrEmpty(token) || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Fresh explicit native UI validation required.");
        var scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
        SessionState.SetInt(AdvanceNativeKey+".sceneCount",scenes.Length);
        for(int i=0;i<scenes.Length;i++)
        {
            SessionState.SetString(AdvanceNativeKey+".scenePath"+i,scenes[i].path);
            SessionState.SetBool(AdvanceNativeKey+".sceneLoaded"+i,scenes[i].isLoaded);
            SessionState.SetBool(AdvanceNativeKey+".sceneActive"+i,scenes[i].isActive);
        }
        var startScene=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
        SessionState.SetString(AdvanceNativeKey+".startScene", startScene==null ? "" : AssetDatabase.GetAssetPath(startScene));
        SessionState.SetString(AdvanceNativeKey+".token", token);
        SessionState.SetBool(AdvanceNativeKey, true);
        SessionState.SetInt(AdvanceNativeKey+".stage", 0);
        SessionState.SetInt(AdvanceNativeKey+".exit", 1);
        SessionState.SetFloat(AdvanceNativeKey+".deadline", (float)EditorApplication.timeSinceStartup+90);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=null;
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        // A live fixture camera keeps the normal camera-less-preview redirect
        // from loading the full Menu scene during this isolated UI control.
        new GameObject("AssistantUiTestStartupCamera",typeof(Camera));
        EditorUtility.UnloadUnusedAssetsImmediate();
        // Existing ledger regressions are Edit Mode tests; Play Mode requires
        // baked runtime prefabs and correctly rejects their isolated worlds.
        Assert.IsFalse(Application.isPlaying);
        Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation();
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        SessionState.SetInt(AdvanceNativeKey+".size",(int)gameView.GetType().GetProperty("selectedSizeIndex",flags).GetValue(gameView));
        SessionState.SetBool(AdvanceNativeKey+".gizmos",(bool)gameView.GetType().GetProperty("drawGizmos",flags).GetValue(gameView));
        typeof(Game.Editor.MainMenuV3PrefabBuilder).GetMethod("SetGameViewResolution",BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{2400,1080});
        gameView.Show();gameView.Focus();
        ResumeAdvanceMapNativeValidation();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void ResumeAdvanceMapNativeValidation()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        string token=Environment.GetEnvironmentVariable("WARLINE_ADVANCE_MAP_NATIVE_RUN");
        if (!SessionState.GetBool(AdvanceNativeKey,false) || string.IsNullOrEmpty(token) ||
            token!=SessionState.GetString(AdvanceNativeKey+".token","")) return;
        EditorApplication.update-=TickAdvanceMapNativeValidation;
        EditorApplication.update+=TickAdvanceMapNativeValidation;
    }

    private static void TickAdvanceMapNativeValidation()
    {
        int stage=SessionState.GetInt(AdvanceNativeKey+".stage",0);
        if (stage==0 && EditorApplication.isPlaying)
        {
            if (Time.frameCount<2 || Screen.width!=2400 || Screen.height!=1080)
            {
                if (EditorApplication.timeSinceStartup<SessionState.GetFloat(AdvanceNativeKey+".deadline",0)) return;
                SessionState.SetInt(AdvanceNativeKey+".stage",2);
                SessionState.SetInt(AdvanceNativeKey+".exit",1);
                Debug.LogError("Native GameView never produced a screen surface.");
                EditorApplication.ExitPlaymode();
                return;
            }
            advanceNativeCases=RunSkirmishAdvanceMapCases();
            advanceNativeFrame=-1;
            SessionState.SetInt(AdvanceNativeKey+".stage",1);
            return;
        }
        if(stage==1 && EditorApplication.isPlaying)
        {
            if(advanceNativeFrame==Time.frameCount)return;
            advanceNativeFrame=Time.frameCount;
            try
            {
                if(advanceNativeCases.MoveNext())return;
                SessionState.SetInt(AdvanceNativeKey+".exit",0);
            }
            catch(Exception error)
            {
                SessionState.SetInt(AdvanceNativeKey+".exit",1);
                Debug.LogException(error);
            }
            (advanceNativeCases as IDisposable)?.Dispose();
            advanceNativeCases=null;
            SessionState.SetInt(AdvanceNativeKey+".stage",2);
            EditorApplication.ExitPlaymode();
            return;
        }
        if (stage!=2 || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update-=TickAdvanceMapNativeValidation;
        SessionState.SetBool(AdvanceNativeKey,false);
        int exit=SessionState.GetInt(AdvanceNativeKey+".exit",1);
        bool restored=false;
        try
        {
            int count=SessionState.GetInt(AdvanceNativeKey+".sceneCount",0);
            var scenes=new UnityEditor.SceneManagement.SceneSetup[count];
            for(int i=0;i<count;i++)
                scenes[i]=new UnityEditor.SceneManagement.SceneSetup
                {path=SessionState.GetString(AdvanceNativeKey+".scenePath"+i,""),
                 isLoaded=SessionState.GetBool(AdvanceNativeKey+".sceneLoaded"+i,false),
                 isActive=SessionState.GetBool(AdvanceNativeKey+".sceneActive"+i,false)};
            if(scenes.Length>0) UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(scenes);
            string start=SessionState.GetString(AdvanceNativeKey+".startScene","");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=
                string.IsNullOrEmpty(start) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(start);
            var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            gameView.GetType().GetProperty("selectedSizeIndex",flags).SetValue(gameView,SessionState.GetInt(AdvanceNativeKey+".size",0));
            gameView.GetType().GetProperty("drawGizmos",flags).SetValue(gameView,SessionState.GetBool(AdvanceNativeKey+".gizmos",false));
            gameView.Repaint();
            restored=true;
        }
        catch(Exception error) { exit=1;Debug.LogException(error); }
        Debug.Log("[SkirmishAdvanceMapNativeExit] result="+(exit==0 ? "Passed" : "Failed")+" restored="+(restored ? "1" : "0"));
        EditorApplication.Exit(exit);
    }

    private static IEnumerator RunSkirmishAdvanceMapCases()
    {
        var cases=new Func<MatchHudAssistantUiSystemHelperTests,IEnumerator>[]
        {
            t=>t.SkirmishOffscreenAdvanceMapGesturesOnlyRequestCamera(false),
            t=>t.SkirmishOffscreenAdvanceMapGesturesOnlyRequestCamera(true),
            t=>t.ExpandedSkirmishAdvanceClosesMapBeforeSeparateWorldTarget(false),
            t=>t.ExpandedSkirmishAdvanceClosesMapBeforeSeparateWorldTarget(true)
        };
        foreach(var control in cases)
        {
            var tests=new MatchHudAssistantUiSystemHelperTests();
            string locale=GameLocalization.CurrentLocaleCode;
            IEnumerator steps=null;
            try
            {
                GameLocalization.SetLocale(GameLocalization.EnglishLocaleCode,persist:false);
                steps=control(tests);
                while(steps.MoveNext())yield return null;
            }
            finally
            {
                (steps as IDisposable)?.Dispose();tests.TearDown();
                GameLocalization.SetLocale(locale,persist:false);
            }
        }
        Debug.Log("[SkirmishAdvanceMapControls] result=Passed controls=4 surface=2400x1080 cameraOrders=0");
        Debug.Log("[SkirmishAdvanceMapValidation] result=Passed tests=4 expanded=1 cameraOrders=0 nativeMissionAcceptance=Pending");
    }

    private sealed class SkirmishAdvanceMapFixture : IDisposable
    {
        readonly List<Camera> previousMainCameras=new();
        readonly List<GraphicRaycaster> previousRaycasters=new();
        readonly EventSystem previousEvents;
        readonly GameObject root;
        readonly MatchHudAssistantUiSystemHelper helper=new();
        readonly SkirmishMatchView skirmish;
        readonly Vector3 objective=new(100,0,100);
        readonly Vector3 player;
        readonly bool hadPreviousEvents;
        bool expanded;
        public Camera BattleCamera {get;private set;}
        public MatchHudMinimapView Map {get;private set;}
        public Button CloseButton {get;private set;}
        public FakeAssistantPanelGateway Gateway {get;private set;}
        public int OpenRequests,FocusRequests,CloseRequests;
        public Vector2 LastFocus;
        public Vector3 Ground=>objective+(player-objective).normalized*25;
        public SkirmishAdvanceMapFixture()
        {
            previousEvents=EventSystem.current;hadPreviousEvents=previousEvents!=null;
            foreach(var raycaster in UnityEngine.Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None))
                if(raycaster.enabled){previousRaycasters.Add(raycaster);raycaster.enabled=false;}
            root=new GameObject("AssistantUiTestAdvanceMap",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            player=objective-Quaternion.Euler(0,35,0)*Vector3.forward*100;
            try
            {
                Assert.Greater(Screen.width,0);Assert.Greater(Screen.height,0);
                foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if(camera.CompareTag("MainCamera")){previousMainCameras.Add(camera);camera.gameObject.tag="Untagged";}
                var cameraObject=new GameObject("AssistantUiTestAdvanceCamera",typeof(Camera));
                BattleCamera=cameraObject.GetComponent<Camera>();cameraObject.tag="MainCamera";
                BattleCamera.fieldOfView=46;BattleCamera.aspect=(float)Screen.width/Screen.height;
                AimAt(objective);Assert.AreSame(BattleCamera,Camera.main);
                var eventObject=new GameObject("Events",typeof(EventSystem));eventObject.transform.SetParent(root.transform,false);
                EventSystem.current=eventObject.GetComponent<EventSystem>();
                root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder=short.MaxValue;
                Canvas.ForceUpdateCanvases();
                var mapObject=new GameObject("Map",typeof(RectTransform),typeof(Image));mapObject.transform.SetParent(root.transform,false);
                var rect=mapObject.GetComponent<RectTransform>();rect.sizeDelta=Vector2.one*Mathf.Min(Screen.width,Screen.height)*.3f;
                rect.position=new Vector3(Screen.width*.2f,Screen.height*.2f,0);
                var viewport=new GameObject("Viewport",typeof(RectTransform));viewport.transform.SetParent(rect,false);
                Map=mapObject.AddComponent<MatchHudMinimapView>();
                Map.Configure(mapObject.GetComponent<Image>(),rect,viewport.GetComponent<RectTransform>(),null,null,rect);
                Map.PresentedProjection=new MatchHudMinimapProjectionGrid(Vector3.zero,200,200);
                Map.FullMapOpenRequested+=()=>OpenRequests++;
                Map.FocusRequested+=point=>{FocusRequests++;LastFocus=point;};
                CloseButton=CreateCommandButton("CloseMap",(RectTransform)root.transform,new Vector2(Screen.width*.35f,Screen.height*.35f));
                CloseButton.onClick.AddListener(()=>{CloseRequests++;SetExpanded(false,false);});
                var objectiveObject=new GameObject("PublicObjectives",typeof(RectTransform));
                objectiveObject.transform.SetParent(root.transform,false);objectiveObject.SetActive(false);
                skirmish=objectiveObject.AddComponent<SkirmishMatchView>();
                skirmish.PresentEnemyBaseMarker(new Vector3(-1,-1,-1),true,objective,player,false);
                SetPrivateField(helper,"watchSkirmish",skirmish);SetPrivateField(helper,"watchMap",Map);
                SetPrivateField(helper,"watchScenarioIndex",4);
                SetPrivateField(helper,"watchRaycast",new PointerEventData(EventSystem.current));
                SetPrivateField(helper,"watchEventSystem",EventSystem.current);
                Gateway=new FakeAssistantPanelGateway(CreateStructuredModel(1),UiAssistantHighlightModel.Empty){Skirmish=true};
                UiShellRuntimeGateway.Register(Gateway);SetExpanded(false,false);
            }
            catch{Dispose();throw;}
        }
        public void AimAt(Vector3 point)
        {
            var rotation=Quaternion.Euler(65,35,0);
            BattleCamera.transform.SetPositionAndRotation(point-rotation*Vector3.forward*(18/Mathf.Sin(65*Mathf.Deg2Rad)),rotation);
        }
        public void SetExpanded(bool value,bool insideViewport)
        {
            expanded=value;
            Map.ApplyInteractionOptions(value,true,true,true,false,!value);
            Vector2 center=insideViewport?new Vector2(Ground.x/200,Ground.z/200):new Vector2(.85f,.85f);
            Map.SetViewportNormalizedRect(new Rect(center-Vector2.one*.1f,Vector2.one*.2f));
            Canvas.ForceUpdateCanvases();
        }
        public AriaSkirmishObservation View()=>new()
        {
            Active=true,ExpandedSession=true,CombinedArmsField=true,Frame=1,Time=1,
            AttackMode=true,SelectionVisible=true,SelectedCount=4,Infantry=24,
            PlayerHealth=800,EnemyHealth=800,MapOpen=expanded,
            FocusEnemy=new AriaTouchTarget{Id=-24296,Available=true},
            CloseMap=new AriaTouchTarget{Id=CloseButton.GetEntityId().GetHashCode(),Available=true,
                Position=CenterScreenPoint((RectTransform)CloseButton.transform)}
        };
        public void Observe(ref AriaSkirmishObservation view)
        {
            var position=BattleCamera.transform.position;var rotation=BattleCamera.transform.rotation;float fov=BattleCamera.fieldOfView;
            object[] args={view};
            var method=typeof(MatchHudAssistantUiSystemHelper).GetMethod("ObserveAdvanceGround",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(method);method.Invoke(helper,args);view=(AriaSkirmishObservation)args[0];
            Assert.AreEqual(position,BattleCamera.transform.position);
            Assert.AreEqual(rotation,BattleCamera.transform.rotation);
            Assert.AreEqual(fov,BattleCamera.fieldOfView);
        }
        public void Gesture(AriaSkirmishObservation view)
        {
            var pointer=new PointerEventData(EventSystem.current){position=view.FocusAdvance.Position,button=PointerEventData.InputButton.Left};
            Map.OnPointerDown(pointer);
            if(view.FocusAdvanceDrag){Map.OnInitializePotentialDrag(pointer);pointer.position=view.FocusAdvanceDragEnd;Map.OnDrag(pointer);}
            Map.OnPointerUp(pointer);Map.OnPointerClick(pointer);
        }
        public string DescribeGround()
        {
            var point=BattleCamera.WorldToScreenPoint(Ground);
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            var names=new List<string>();
            for(int i=0;i<Mathf.Min(8,hits.Count);i++)names.Add(hits[i].gameObject.name);
            return $"screen={Screen.width}x{Screen.height} safe={Screen.safeArea} ground={Ground} point={point} focus={LastFocus} hits={string.Join(",",names)}";
        }
        public void AssertReadableCamera()
        {
            Assert.That(BattleCamera.transform.position.y,Is.EqualTo(18).Within(.001));
            Assert.That(BattleCamera.transform.eulerAngles.x,Is.EqualTo(65).Within(.001));
            Assert.That(BattleCamera.transform.eulerAngles.y,Is.EqualTo(35).Within(.001));
            Assert.AreEqual(46,BattleCamera.fieldOfView);
        }
        public void Dispose()
        {
            if(BattleCamera!=null)UnityEngine.Object.DestroyImmediate(BattleCamera.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
            foreach(var camera in previousMainCameras)if(camera!=null)camera.gameObject.tag="MainCamera";
            if(hadPreviousEvents&&previousEvents!=null)EventSystem.current=previousEvents;
            foreach(var raycaster in previousRaycasters)if(raycaster!=null)raycaster.enabled=true;
            UiShellRuntimeGateway.Register(null);
        }
    }
}
