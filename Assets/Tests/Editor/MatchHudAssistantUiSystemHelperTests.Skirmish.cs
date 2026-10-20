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



public sealed partial class MatchHudAssistantUiSystemHelperTests
{
    private sealed class ContextFallbackGateway : IUiSkirmishGateway
    {
        public bool Admitted, Expanded;
        public int FullReads;
        public bool TryReadSkirmish(out UiSkirmishModel model)
        {
            FullReads++;
            model = new UiSkirmishModel { Expanded = Expanded };
            return Admitted;
        }
        public bool TryRequestSkirmish(UiSkirmishAction action) => false;
    }

    [Test]
    public void SkirmishContextDefaultFallbackPreservesAdmissionAndExpanded()
    {
        foreach (bool admitted in new[] { false, true })
        foreach (bool expanded in new[] { false, true })
        {
            var fake = new ContextFallbackGateway { Admitted = admitted, Expanded = expanded };
            IUiSkirmishGateway gateway = fake;
            Assert.AreEqual(admitted, gateway.TryReadSkirmishContext(out bool actualExpanded));
            Assert.AreEqual(admitted && expanded, actualExpanded);
            Assert.AreEqual(1, fake.FullReads, "Other gateway implementations retain the full-read fallback.");
        }
    }

    [Test]
    public void SkirmishContextMatchesFullReadAcrossAdmissionAndSessionVariants()
    {
        var previous = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        var gateway = (IUiSkirmishGateway)Activator.CreateInstance(typeof(UiShellEcsGateway), true);
        using var world = new Unity.Entities.World("SkirmishContextEquivalence");
        try
        {
            Unity.Entities.World.DefaultGameObjectInjectionWorld = null;
            AssertContext(false, false);
            Unity.Entities.World.DefaultGameObjectInjectionWorld = world;
            AssertContext(false, false);
            var em = world.EntityManager;
            var session = em.CreateEntity(typeof(Game.Components.SkirmishMatchState));
            var duplicate = em.CreateEntity(typeof(Game.Components.SkirmishMatchState));
            AssertContext(false, false);
            em.DestroyEntity(duplicate);
            foreach (Game.Components.SkirmishPhase phase in Enum.GetValues(typeof(Game.Components.SkirmishPhase)))
            foreach (Game.Components.SkirmishStartupFailureCode failure in Enum.GetValues(typeof(Game.Components.SkirmishStartupFailureCode)))
            for (int variant = 0; variant < 3; variant++)
            {
                if (em.HasComponent<Game.Components.SkirmishExpandedSessionComponent>(session))
                    em.RemoveComponent<Game.Components.SkirmishExpandedSessionComponent>(session);
                if (variant != 0)
                    em.AddComponentData(session, new Game.Components.SkirmishExpandedSessionComponent { IsLegacy = (byte)(variant == 1 ? 1 : 0) });
                var match = new Game.Components.SkirmishMatchState { Phase = phase, StartupFailure = failure };
                em.SetComponentData(session, match);
                bool admitted = !(phase < Game.Components.SkirmishPhase.Playing && failure == Game.Components.SkirmishStartupFailureCode.None);
                AssertContext(admitted, admitted && failure == Game.Components.SkirmishStartupFailureCode.None && variant == 2);
                Assert.AreEqual(match, em.GetComponentData<Game.Components.SkirmishMatchState>(session));
            }
        }
        finally { Unity.Entities.World.DefaultGameObjectInjectionWorld = previous; }

        void AssertContext(bool admitted, bool expanded)
        {
            bool fullAdmitted = gateway.TryReadSkirmish(out var full);
            bool cheapAdmitted = gateway.TryReadSkirmishContext(out bool cheapExpanded);
            Assert.AreEqual(admitted, fullAdmitted);
            Assert.AreEqual(fullAdmitted, cheapAdmitted);
            Assert.AreEqual(fullAdmitted && full.Expanded, cheapExpanded);
            Assert.AreEqual(expanded, cheapExpanded);
        }
    }

    public static void RunSkirmishContextValidation()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode context controls required.");
        var tests = new MatchHudAssistantUiSystemHelperTests();
        tests.SkirmishContextDefaultFallbackPreservesAdmissionAndExpanded();
        tests.SkirmishContextMatchesFullReadAcrossAdmissionAndSessionVariants();
        Debug.Log("[SkirmishContextValidation] result=Passed fallback=4 admission=3 matrix=48 nativeMissionAcceptance=Pending");
    }
    public sealed class PadWitnessRaycaster : BaseRaycaster
    {
        public int Calls; public bool Block;
        public void EnsureRegistered(){if(!RaycasterManager.GetRaycasters().Contains(this))base.OnEnable();}
        public void ReleaseRegistered(){if(RaycasterManager.GetRaycasters().Contains(this))base.OnDisable();}
        public override Camera eventCamera => null;
        public override void Raycast(PointerEventData data, List<RaycastResult> results)
        {
            Calls++;
            if(Block)results.Add(new RaycastResult{gameObject=gameObject,module=this,
                screenPosition=data.position,distance=0,index=results.Count});
        }
    }
    public static void RunHomePadWitnessControls()
    {
        Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
        Assert.IsTrue(SkirmishMatchView.NativeCombatEvidenceEnabled,
            "Use WARLINE_S005_COMBAT_EVIDENCE=1 before Editor startup.");
        PadWitnessFixture(true);
        Debug.Log("[HomePadRefusalWitnessControls] result=Passed controls=11 normalMissionReady=0");
    }
    public static void RunHomePadWitnessDefaultOffControl()
    {
        Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
        Assert.IsFalse(SkirmishMatchView.NativeCombatEvidenceEnabled,
            "Separate Editor invocation: omit WARLINE_S005_COMBAT_EVIDENCE.");
        PadWitnessFixture(false);
        Debug.Log("[HomePadRefusalWitnessDefaultOff] result=Passed controls=1 normalMissionReady=0");
    }
    private static void PadWitnessFixture(bool enabled)
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        var helper=new MatchHudAssistantUiSystemHelper();
        var type=typeof(MatchHudAssistantUiSystemHelper);
        var method=type.GetMethod("ObserveHomePadSite",flags);
        Assert.IsNotNull(method);
        void Set(string name,object value)=>type.GetField(name,flags).SetValue(helper,value);
        var saved=new[]{SkirmishMatchView.NativeHomePadSite0,SkirmishMatchView.NativeHomePadSite1,
            SkirmishMatchView.NativeHomePadSite2,SkirmishMatchView.NativeHomePadSite3};
        var previousEventSystem=EventSystem.current;
        var systems=(System.Collections.IList)typeof(EventSystem).GetField("m_EventSystems",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var previousSystems=new List<EventSystem>();foreach(EventSystem system in systems)previousSystems.Add(system);
        var previousRays=new List<BaseRaycaster>(RaycasterManager.GetRaycasters());
        EventSystem ownedEventSystem=null;PadWitnessRaycaster ownedRaycaster=null;
        var tagged=new List<GameObject>();
        var disabled=new List<BaseRaycaster>();
        var root=new GameObject("OwnedPadWitnessFixture");
        var fontField=typeof(SkirmishMatchView).GetField("interfaceFont",BindingFlags.Static|BindingFlags.NonPublic);
        object previousFont=fontField.GetValue(null);
        try
        {
            foreach(var existing in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(existing.CompareTag("MainCamera")){tagged.Add(existing.gameObject);existing.tag="Untagged";}
            var cameraGo=new GameObject("OwnedPadCamera",typeof(Camera));cameraGo.transform.SetParent(root.transform);
            var camera=cameraGo.GetComponent<Camera>();cameraGo.tag="MainCamera";
            camera.orthographic=true;camera.orthographicSize=60;camera.cullingMask=0;
            camera.pixelRect=Screen.safeArea;camera.nearClipPlane=.1f;camera.farClipPlane=1000;
            var site=new Vector3(-34,0,18);
            camera.transform.SetPositionAndRotation(site+Vector3.up*100,Quaternion.Euler(90,0,0));
            var hud=SkirmishMatchView.Create();hud.gameObject.name="OwnedPadHud";
            hud.transform.SetParent(root.transform);
            void Bases(Vector3 home,Vector3 enemy)
            {
                typeof(SkirmishMatchView).GetField("<PlayerBaseObjectivePosition>k__BackingField",flags).SetValue(hud,home);
                typeof(SkirmishMatchView).GetField("<EnemyBaseObjectivePosition>k__BackingField",flags).SetValue(hud,enemy);
            }
            Bases(Vector3.zero,new Vector3(0,0,500));
            var mapGo=new GameObject("OwnedPadMap",typeof(RectTransform));mapGo.transform.SetParent(root.transform);
            var map=mapGo.AddComponent<MatchHudMinimapView>();
            var esGo=new GameObject("OwnedPadEventSystem",typeof(EventSystem));esGo.transform.SetParent(root.transform);
            ownedEventSystem=esGo.GetComponent<EventSystem>();
            if(!systems.Contains(ownedEventSystem))typeof(EventSystem).GetMethod("OnEnable",flags).Invoke(ownedEventSystem,null);
            Assert.IsTrue(systems.Contains(ownedEventSystem));EventSystem.current=ownedEventSystem;
            Assert.AreSame(ownedEventSystem,EventSystem.current);
            foreach(var ray in UnityEngine.Object.FindObjectsByType<BaseRaycaster>(FindObjectsSortMode.None))
                if(ray.enabled){disabled.Add(ray);ray.enabled=false;}
            var rayGo=new GameObject("OwnedPadRaycaster");rayGo.transform.SetParent(root.transform);
            var raycaster=rayGo.AddComponent<PadWitnessRaycaster>();ownedRaycaster=raycaster;
            raycaster.EnsureRegistered();Assert.IsTrue(RaycasterManager.GetRaycasters().Contains(raycaster));
            Set("watchSkirmish",hud);Set("watchMap",map);
            AriaTouchTarget Invoke(string refusal,int rayCalls,bool available=false)
            {
                int calls=raycaster.Calls,contacts=map.PresentedContacts.Count;
                Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
                var target=(AriaTouchTarget)method.Invoke(helper,new object[]{0});
                Assert.AreEqual(available,target.Available);
                Assert.AreEqual(rayCalls,raycaster.Calls-calls,"Unexpected extra/missing real RaycastAll");
                Assert.AreEqual(contacts,map.PresentedContacts.Count);
                Assert.AreEqual(position,camera.transform.position);Assert.AreEqual(rotation,camera.transform.rotation);
                if(enabled)
                {
                    var evidence=SkirmishMatchView.NativeHomePadSite0;
                    Assert.IsTrue(evidence.collected);Assert.AreEqual(Time.frameCount,evidence.frame);
                    Assert.AreEqual(refusal,evidence.refusal);Assert.AreEqual(available,evidence.reachable);
                    Assert.AreEqual(rayCalls==0?-1:(available?0:1),evidence.uiHitCount);
                }
                else Assert.AreEqual(saved[0],SkirmishMatchView.NativeHomePadSite0);
                return target;
            }
            if(!enabled){Assert.AreEqual(-20200,Invoke("",1,true).Id);return;}
            Set("watchSkirmish",null);Assert.AreEqual(0,Invoke("MissingViewMapCamera",0).Id);Set("watchSkirmish",hud);
            Set("watchMap",null);Invoke("MissingViewMapCamera",0);Set("watchMap",map);
            cameraGo.tag="Untagged";Invoke("MissingViewMapCamera",0);cameraGo.tag="MainCamera";
            Bases(Vector3.zero,Vector3.zero);Invoke("DegenerateHomeEnemyDirection",0);
            Bases(Vector3.zero,new Vector3(0,0,30));Invoke("EnemyBaseClearance",0);
            Assert.AreEqual(75,SkirmishMatchView.NativeHomePadSite0.requiredClearance);
            Assert.AreEqual((site-new Vector3(0,0,30)).sqrMagnitude,SkirmishMatchView.NativeHomePadSite0.rejectedDistanceSquared);
            Bases(Vector3.zero,new Vector3(0,0,500));
            map.RecordPresentedContact(new MatchHudMinimapMarkerModel(site,MatchHudMinimapMarkerAllegiance.Player),null);
            Invoke("PublicFriendlyContactClearance",0);Assert.AreEqual(16,SkirmishMatchView.NativeHomePadSite0.requiredClearance);
            map.PresentedContacts.Clear();
            map.RecordPresentedContact(new MatchHudMinimapMarkerModel(site,MatchHudMinimapMarkerAllegiance.Enemy),null);
            Invoke("PublicEnemyContactClearance",0);Assert.AreEqual(36,SkirmishMatchView.NativeHomePadSite0.requiredClearance);
            map.PresentedContacts.Clear();
            camera.transform.position=site-Vector3.up*100;Invoke("BehindCamera",0);
            Assert.IsFalse(SkirmishMatchView.NativeHomePadSite0.screenValid);
            camera.transform.position=site+Vector3.up*100+Vector3.right*100000;Invoke("OutsideSafeArea",0);
            camera.transform.position=site+Vector3.up*100;
            var projected=camera.WorldToScreenPoint(site);Assert.Greater(projected.z,0);Assert.IsTrue(Screen.safeArea.Contains(projected));
            var accepted=Invoke("None",1,true);Assert.AreEqual(-20200,accepted.Id);Assert.AreEqual((Vector2)projected,accepted.Position);
            raycaster.Block=true;var blocked=Invoke("ReachabilityRejected",1);
            Assert.AreEqual(accepted.Id,blocked.Id);Assert.AreEqual(accepted.Position,blocked.Position);
        }
        finally
        {
            SkirmishMatchView.NativeHomePadSite0=saved[0];SkirmishMatchView.NativeHomePadSite1=saved[1];
            SkirmishMatchView.NativeHomePadSite2=saved[2];SkirmishMatchView.NativeHomePadSite3=saved[3];
            if(ownedRaycaster!=null){ownedRaycaster.enabled=false;ownedRaycaster.ReleaseRegistered();}
            if(ownedEventSystem!=null){ownedEventSystem.enabled=false;
                if(systems.Contains(ownedEventSystem))typeof(EventSystem).GetMethod("OnDisable",flags).Invoke(ownedEventSystem,null);}
            UnityEngine.Object.DestroyImmediate(root);
            fontField.SetValue(null,previousFont);
            foreach(var ray in disabled)if(ray!=null)ray.enabled=true;
            foreach(var go in tagged)if(go!=null)go.tag="MainCamera";
            if(previousEventSystem!=null&&systems.Contains(previousEventSystem))EventSystem.current=previousEventSystem;
            var restoredRays=RaycasterManager.GetRaycasters();restoredRays.Clear();restoredRays.AddRange(previousRays);
            CollectionAssert.AreEqual(previousSystems,systems);
            CollectionAssert.AreEqual(previousRays,restoredRays);
            Assert.AreSame(previousEventSystem,EventSystem.current);
        }
    }

}
