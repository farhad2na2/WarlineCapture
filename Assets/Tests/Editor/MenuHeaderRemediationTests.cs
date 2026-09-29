using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class MenuHeaderRemediationTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
    public static void RunM05GuidedJourney()
    {
        SessionState.SetBool("Warline.CH03M05.EditorProbe",false);
        SessionState.SetBool("Warline.CH03M05.VisualOnly",false);
        SessionState.SetBool("Warline.M05.SkipComics",false);
        SessionState.SetBool("Warline.M05.EnglishCombat",true);
        M05BreachAssaultEditorProbe.RunGuidedEnglish();
    }
    public static void BuildAndValidateNativePresentation()
    {
        MainMenuV3PrefabBuilder.Build();
        V3UiLocalizationCatalogBuilder.RebuildMenuHeaderLocalization();
        ValidateAndCaptureNativePresentation();
    }
    public static void ValidateAndCaptureNativePresentation()
    {
        MenuAccountHeaderAuthoring.RemoveAccountBindingFromCombatPopup();
        MenuAccountHeaderAuthoring.PreserveNativeAriaImport();
        RunFocusedValidation();
        if(ValidationExit.LastExitCode != 0) return;
        CapturePresentationFixtures();
        CaptureCompletionFixtures();
    }
    public static void RunFocusedValidation()
    {
        string previousLocale=GameLocalization.CurrentLocaleCode;
        var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
        GameLocalization.Initialize(catalog,"en",persist:false);
        try
        {
            new MenuHeaderRemediationTests().AccountProjectionRefreshesAndSurvivesRecreation();
            new MenuHeaderRemediationTests().HomeArtAndContinueShareTargetAndMissingArtIsNeutral();
            new MenuHeaderRemediationTests().HomeTargetTracksClearReloadAndCompletion();
            new MenuHeaderRemediationTests().CompletionHomeRequiresTheWholeReadyCatalogAndKeepsScenePairsStable();
            MainMenuV3PrefabBuilder.Validate();
            Debug.Log("[MenuHeaderRemediation] result=Passed accountRefresh=True utf8PlayerName=True targetRouting=True missingArt=True clearReloadCompletion=True duplicateTap=True disclosure=True");
            ValidationExit.Passed();
        }
        catch(Exception error)
        { Debug.LogException(error); Debug.LogError("[MenuHeaderRemediation] result=Failed"); ValidationExit.Failed(); }
        finally { GameLocalization.Initialize(catalog,previousLocale,persist:false); }
    }

    [Test] public void AccountProjectionRefreshesAndSurvivesRecreation()
    {
        string directory = Path.Combine(Path.GetTempPath(), "warline-menu-profile-" + Guid.NewGuid().ToString("N"));
        string previous = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
        Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", directory);
        try
        {
            var saves=SaveService.CreateDefault();
            var profile=saves.LoadProfile(); profile.credits=0; profile.commandAuthority=987;
            profile.firstLaunchCommanderDisplayName="Test Commander"; profile.firstLaunchCommanderPortraitIndex=4;
            saves.SaveProfile(profile);
            using(var world=new World("menu-account-fixture"))
            {
                var em=world.EntityManager;
                var root=em.CreateEntity(typeof(UiShellMainMenuResourcesComponent),typeof(UiShellCommanderProfileComponent));
                var system=world.GetOrCreateSystemManaged<UiAccountProfileProjectionSystem>(); system.Update();
                Assert.AreEqual("0",em.GetComponentData<UiShellMainMenuResourcesComponent>(root).CreditsText.ToString());
                Assert.IsTrue(em.GetComponentData<UiShellMainMenuResourcesComponent>(root).CommandText.IsEmpty);
                Assert.AreEqual("4",em.GetComponentData<UiShellCommanderProfileComponent>(root).PortraitClass.ToString());
                profile=saves.LoadProfile(); profile.credits=123456789; saves.SaveProfile(profile); system.Update();
                Assert.AreEqual("123,456,789",em.GetComponentData<UiShellMainMenuResourcesComponent>(root).CreditsText.ToString());
                Assert.AreEqual(987,saves.LoadProfile().commandAuthority);
                profile=saves.LoadProfile(); profile.firstLaunchCommanderDisplayName=new string('\u0641',32); saves.SaveProfile(profile); system.Update();
                Assert.AreEqual(profile.firstLaunchCommanderDisplayName,em.GetComponentData<UiShellCommanderProfileComponent>(root).Name.ToString());
                profile=saves.LoadProfile(); profile.firstLaunchCommanderDisplayName=new string('\u0641',100); saves.SaveProfile(profile); system.Update();
                string displayed=em.GetComponentData<UiShellCommanderProfileComponent>(root).Name.ToString();
                Assert.IsNotEmpty(displayed); Assert.IsFalse(displayed.Contains("\uFFFD"));
                Assert.That(profile.firstLaunchCommanderDisplayName,Does.StartWith(displayed));
                Assert.AreEqual(new string('\u0641',100),saves.LoadProfile().firstLaunchCommanderDisplayName);
            }
            using(var world=new World("menu-account-recreated"))
            {
                var root=world.EntityManager.CreateEntity(typeof(UiShellMainMenuResourcesComponent));
                world.GetOrCreateSystemManaged<UiAccountProfileProjectionSystem>().Update();
                Assert.AreEqual("123,456,789",world.EntityManager.GetComponentData<UiShellMainMenuResourcesComponent>(root).CreditsText.ToString());
            }
            File.WriteAllText(Path.Combine(directory,SaveService.ProfileFileName),"invalid profile");
            Assert.IsFalse(saves.TryReadAccountProfile(out _));
        }
        finally
        { Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",previous); if(Directory.Exists(directory))Directory.Delete(directory,true); }
    }

    [Test] public void HomeArtAndContinueShareTargetAndMissingArtIsNeutral()
    {
        World previous=World.DefaultGameObjectInjectionWorld;
        using var world=new World("home-campaign-fixture");
        GameObject instance=null;
        try
        {
            World.DefaultGameObjectInjectionWorld=world; UiShellEcsGateway.RegisterAsRuntimeGateway();
            var root=CreatePresentation(world,CampaignMissionSequence.IdAt(1),false,1);
            instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var view=instance.GetComponentInChildren<MainMenuCampaignCardView>(true); view.Refresh();
            Assert.AreEqual(CampaignMissionSequence.IdAt(1),view.PresentedMissionId);
            var image=instance.transform.Find("LeftContent/Card_Campaign/CampaignArt").GetComponent<Image>();
            Assert.IsNotNull(image.sprite);
            Assert.That(AssetDatabase.GetAssetPath(image.sprite),Does.Contain("M02EstablishBase"));
            var requests=world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(root); requests.Clear();
            instance.transform.Find("LeftContent/Card_Campaign/ContinueButton").GetComponent<Button>().onClick.Invoke();
            // Explicit call in edit mode where OnEnable isn't guaranteed.
            if(requests.Length==0) typeof(MainMenuCampaignCardView).GetMethod("OpenCampaign",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
            Assert.AreEqual(UiCampaignMissionActionKind.Select,requests[requests.Length-1].Action);
            Assert.AreEqual(view.PresentedMissionId,requests[requests.Length-1].MissionId.ToString());
            var routes=world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(root);
            Assert.Greater(routes.Length,0); Assert.AreEqual(UIRoute.Campaign,routes[routes.Length-1].Route);
            int requestCount = requests.Length, routeCount = routes.Length;
            typeof(MainMenuCampaignCardView).GetMethod("OpenCampaign",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
            Assert.AreEqual(requestCount, requests.Length); Assert.AreEqual(routeCount, routes.Length);
            Assert.IsFalse(world.EntityManager.HasBuffer<CampaignMissionLaunchRequestElement>(root));
            foreach(var disclosure in instance.GetComponentsInChildren<MainMenuDisclosureView>(true))
            {
                disclosure.Refresh();
                Assert.IsFalse(disclosure.GetComponent<CanvasGroup>().interactable);
                Assert.IsFalse(disclosure.GetComponent<CanvasGroup>().blocksRaycasts);
                foreach(var button in disclosure.GetComponentsInChildren<Button>(true)) Assert.IsFalse(button.IsInteractable());
            }
            var model=world.EntityManager.GetComponentData<UiCampaignOperationsComponent>(root);
            model.SelectedMissionId=new FixedString64Bytes(CampaignMissionSequence.IdAt(14)); model.FirstClearCompleted=1;
            world.EntityManager.SetComponentData(root,model); view.Refresh();
            Assert.IsFalse(image.enabled); Assert.IsNull(image.sprite);
            Assert.That(instance.transform.Find("LeftContent/Card_Campaign/ContinueButton/Label").GetComponent<TMP_Text>().text,Does.Contain("CHOOSE"));
        }
        finally
        { if(instance!=null)UnityEngine.Object.DestroyImmediate(instance); World.DefaultGameObjectInjectionWorld=previous; UiShellEcsGateway.RegisterAsRuntimeGateway(); }
    }

    [Test] public void HomeTargetTracksClearReloadAndCompletion()
    {
        string directory = Path.Combine(Path.GetTempPath(), "warline-home-target-" + Guid.NewGuid().ToString("N"));
        var store = new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(directory)));
        try
        {
            Assert.AreEqual(CampaignMissionSequence.IdAt(0), ProjectHomeTarget(store));
            Assert.IsTrue(store.Settle(CampaignMissionSequence.IdAt(0), "home-first-clear", 0, true, 3, 60000, null));
            Assert.AreEqual(CampaignMissionSequence.IdAt(1), ProjectHomeTarget(store));
            // A new save owner and World reproduce the target after returning/reloading.
            var reopened = new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(directory)));
            Assert.AreEqual(CampaignMissionSequence.IdAt(1), ProjectHomeTarget(reopened));
            for(int i=1; i<CampaignMissionSequence.RegisteredMissionCount; i++)
                Assert.IsTrue(store.Settle(CampaignMissionSequence.IdAt(i), "home-all-clear-"+i, i, true, 3, 60000, null));
            string last = ProjectHomeTarget(store,true);
            Assert.AreEqual(CampaignMissionSequence.IdAt(CampaignMissionSequence.RegisteredMissionCount-1), last);
            Assert.IsTrue(System.Array.Exists(store.ReadAll(), entry => entry.missionId == last && entry.firstClearCompleted));
        }
        finally { if(Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static string ProjectHomeTarget(CampaignMissionProgressStore store,bool allCompleted=false)
    {
        using var world = new World("home-progression-projection");
        var em = world.EntityManager;
        var missions = AssetDatabase.LoadAssetAtPath<MissionDefinitionCatalogConfig>("Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset");
        var maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>("Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset");
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(em, missions, maps, 23, out var campaignRoot, out string error), error);
        em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(campaignRoot).Store = store;
        var ui = em.CreateEntity(typeof(UiShellRootComponent), typeof(UiShellStateComponent));
        em.SetComponentData(ui, new UiShellStateComponent { ActiveRoute=UIRoute.MainMenu });
        var handle = world.CreateSystem<UiCampaignMissionProjectionSystem>();
        try
        {
            ref var state = ref world.Unmanaged.ResolveSystemStateRef(handle);
            ref var system = ref world.Unmanaged.GetUnsafeSystemRef<UiCampaignMissionProjectionSystem>(handle);
            system.OnUpdate(ref state); state.Dependency.Complete(); em.CompleteAllTrackedJobs();
            var projection=em.GetComponentData<UiCampaignOperationsComponent>(ui);
            if(allCompleted)
            {
                uint required=(1u << CampaignMissionSequence.RegisteredMissionCount)-1;
                Assert.AreEqual(required,projection.RequiredMissionMask);
                Assert.AreEqual(required,projection.CompletedMissionMask);
                Assert.AreEqual(required,projection.ReadyMissionMask);
                Assert.AreEqual(0,projection.FullCampaignRegistered);
            }
            return projection.SelectedMissionId.ToString();
        }
        finally
        {
            world.DestroySystem(handle);
            var catalog = em.GetComponentData<CampaignMissionCatalogComponent>(campaignRoot);
            if(catalog.Blob.IsCreated) catalog.Blob.Dispose();
            catalog.Blob=default; catalog.OwnsBlob=0; em.SetComponentData(campaignRoot,catalog);
        }
    }

    [Test] public void CompletionHomeRequiresTheWholeReadyCatalogAndKeepsScenePairsStable()
    {
        World previous=World.DefaultGameObjectInjectionWorld;
        string locale=GameLocalization.CurrentLocaleCode;
        using var world=new World("completion-home-boundary");
        GameObject instance=null;
        try
        {
            World.DefaultGameObjectInjectionWorld=world;UiShellEcsGateway.RegisterAsRuntimeGateway();
            uint required=(1u << CampaignMissionSequence.RegisteredMissionCount)-1;
            var root=CreatePresentation(world,CampaignMissionSequence.SplitFront,true,required);
            var model=world.EntityManager.GetComponentData<UiCampaignOperationsComponent>(root);
            model.RequiredMissionMask=model.ReadyMissionMask=required;
            model.CompletedMissionMask=required & ~(1u << (CampaignMissionSequence.RegisteredMissionCount-1));
            world.EntityManager.SetComponentData(root,model);
            instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var view=instance.GetComponentInChildren<MainMenuCampaignCardView>();view.Refresh();Assert.IsFalse(view.CompletionVisible);
            model.CompletedMissionMask=required;world.EntityManager.SetComponentData(root,model);view.Refresh();Assert.IsTrue(view.CompletionVisible);
            var art=instance.transform.Find("LeftContent/Card_Campaign/CampaignArt").GetComponent<Image>();
            Assert.That(AssetDatabase.GetAssetPath(art.sprite),Does.Contain("CampaignCompletion"));
            var initial=art.sprite;int first=view.AftermathIndex;
            view.Refresh();view.Refresh();Assert.AreSame(initial,art.sprite);
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            GameLocalization.Initialize(catalog,"fa-IR",persist:false);view.Refresh();Assert.AreSame(initial,art.sprite);
            Assert.IsFalse(instance.transform.Find("LeftContent/Card_Campaign/Chapter").gameObject.activeSelf);
            typeof(MainMenuCampaignCardView).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
            typeof(MainMenuCampaignCardView).GetMethod("OnEnable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
            view.Refresh();Assert.AreEqual(first,view.AftermathIndex,"Overlay must not rotate a home visit.");
            world.EntityManager.SetComponentData(root,new UiShellStateComponent{ActiveRoute=UIRoute.Campaign});view.Refresh();
            world.EntityManager.SetComponentData(root,new UiShellStateComponent{ActiveRoute=UIRoute.MainMenu});view.Refresh();
            Assert.AreNotEqual(first,view.AftermathIndex);
            model.ReadyMissionMask=required & ~1u;world.EntityManager.SetComponentData(root,model);view.Refresh();Assert.IsFalse(view.CompletionVisible);
            model.ReadyMissionMask=required;model.PendingResume=1;world.EntityManager.SetComponentData(root,model);view.Refresh();Assert.IsFalse(view.CompletionVisible);
            model.PendingResume=0;model.RequiredMissionMask=model.ReadyMissionMask=model.CompletedMissionMask=(1u<<25)-1;model.FullCampaignRegistered=1;
            world.EntityManager.SetComponentData(root,model);view.Refresh();Assert.IsTrue(view.CompletionVisible);
            Assert.That(AssetDatabase.GetAssetPath(art.sprite),Does.EndWith("Campaign_Epilogue.png"));
            Assert.IsFalse(MainMenuStoryArchiveView.IsChapterEarned(required,4));Assert.IsTrue(MainMenuStoryArchiveView.IsChapterEarned(required,3));
            Assert.IsFalse(MainMenuStoryArchiveView.IsMissionEarned(required,18));
            var requests=world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(root);requests.Clear();
            typeof(MainMenuCampaignCardView).GetMethod("OpenCampaign",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
            foreach(var request in requests)Assert.AreNotEqual(UiCampaignMissionActionKind.Deploy,request.Action);
            Assert.AreEqual(UIRoute.Campaign,world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(root)[0].Route);
            Debug.Log("[CampaignCompletionHome] result=Passed wholeCatalog=True savedReload=True missingReady=True pendingResume=True stablePairs=True localeStable=True overlayStable=True noRepeat=True fullEndingDistinct=True archiveGates=True chooseDoesNotDeploy=True");
        }
        finally
        {
            if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);
            GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath),locale,persist:false);
            World.DefaultGameObjectInjectionWorld=previous;UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    public static void CaptureCompletionFixtures()
    {
        World previous=World.DefaultGameObjectInjectionWorld;string locale=GameLocalization.CurrentLocaleCode;
        using var world=new World("completion-home-native-fixtures");
        try
        {
            World.DefaultGameObjectInjectionWorld=world;UiShellEcsGateway.RegisterAsRuntimeGateway();
            uint required=(1u << CampaignMissionSequence.RegisteredMissionCount)-1;
            var root=CreatePresentation(world,CampaignMissionSequence.SplitFront,true,required);
            var model=world.EntityManager.GetComponentData<UiCampaignOperationsComponent>(root);model.RequiredMissionMask=model.ReadyMissionMask=required;
            world.EntityManager.SetComponentData(root,model);
            Directory.CreateDirectory("Design/AgentReports/MenuHeaderMonetization/After/completion");
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            foreach(string code in new[]{"en","fa-IR"})
            {
                GameLocalization.Initialize(catalog,code,persist:false);
                foreach(int width in new[]{1920,2400})for(int scene=0;scene<3;scene++)
                {
                    int target=scene;
                    MainMenuV3PrefabBuilder.CaptureConfigured($"Design/AgentReports/MenuHeaderMonetization/After/completion/home-{scene}-{code}-{width}.png",width,1080,instance=>
                    {
                        var view=instance.GetComponentInChildren<MainMenuCampaignCardView>();
                        for(int attempt=0;attempt<3 && view.AftermathIndex!=target;attempt++){view.BeginHomeVisit();view.Refresh();}
                        Assert.AreEqual(target,view.AftermathIndex);
                    });
                }
            }
            model.RequiredMissionMask=model.ReadyMissionMask=model.CompletedMissionMask=(1u<<25)-1;model.FullCampaignRegistered=1;world.EntityManager.SetComponentData(root,model);
            foreach(string code in new[]{"en","fa-IR"})
            {
                GameLocalization.Initialize(catalog,code,persist:false);
                foreach(int width in new[]{1920,2400})MainMenuV3PrefabBuilder.CaptureConfigured($"Design/AgentReports/MenuHeaderMonetization/After/completion/home-full-{code}-{width}.png",width,1080,null);
            }
            Debug.Log("[CampaignCompletionCaptures] result=Passed aftermathPairs=3 nativeCaptures=16 fullEndingIsFutureProjectionFixture=True gameplayAcceptance=False");
        }
        finally
        {
            GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath),locale,persist:false);
            World.DefaultGameObjectInjectionWorld=previous;UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    public static void CapturePresentationFixtures()
    {
        World previous=World.DefaultGameObjectInjectionWorld;
        string locale=GameLocalization.CurrentLocaleCode;
        using var world=new World("home-native-presentation-fixtures");
        try
        {
            World.DefaultGameObjectInjectionWorld=world; UiShellEcsGateway.RegisterAsRuntimeGateway();
            var root=CreatePresentation(world,CampaignMissionSequence.SteelPush,false,0x1ffff);
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var capture=typeof(MainMenuV3PrefabBuilder).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);
            foreach(string code in new[]{"en","fa-IR"})
            {
                GameLocalization.Initialize(catalog,code,persist:false);
                foreach(int width in new[]{1920,2400})
                    capture.Invoke(null,new object[]{"Design/AgentReports/MenuHeaderMonetization/After/home-"+code+"-"+width+".png",width,1080});
            }
            Debug.Log("[MenuHeaderFixtures] result=Passed captures=4 state=SteelPush presentationFixture=True gameplayAcceptance=False");
            world.EntityManager.SetComponentData(root,new UiShellMainMenuResourcesComponent{CreditsText=new FixedString32Bytes("2,147,483,647")});
            foreach(string code in new[]{"en","fa-IR"})
            {
                string playerName=code=="en"?"Wellington Alexandria Montgomery":new string('\u0641',32);
                world.EntityManager.SetComponentData(root,new UiShellCommanderProfileComponent{Name=new FixedString128Bytes(playerName),PortraitClass=new FixedString64Bytes("4")});
                GameLocalization.Initialize(catalog,code,persist:false);
                foreach(int width in new[]{1920,2400})
                    capture.Invoke(null,new object[]{"Design/AgentReports/MenuHeaderMonetization/After/home-long-name-"+code+"-"+width+".png",width,1080});
            }
            Debug.Log("[MenuHeaderStressFixtures] result=Passed captures=4 names=32Characters credits=Int32Max gameplayAcceptance=False");
            GameLocalization.Initialize(catalog,"en",persist:false);
            Directory.CreateDirectory("Design/AgentReports/MenuHeaderMonetization/After/commander-scenes");
            for (int index = 0; index < 6; index++)
            {
                world.EntityManager.SetComponentData(root,new UiShellCommanderProfileComponent{Name=new FixedString128Bytes("Commander"),PortraitClass=new FixedString64Bytes(index.ToString())});
                var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                try
                {
                    var view=instance.GetComponentInChildren<MainMenuCommanderVariantView>(true);
                    view.RefreshIdentity();
                    Assert.AreEqual(MainMenuV3PrefabBuilder.CommanderPanelPath(index),AssetDatabase.GetAssetPath(view.Target.sprite));
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
                capture.Invoke(null,new object[]{"Design/AgentReports/MenuHeaderMonetization/After/commander-scenes/home-commander-"+index+".png",1920,1080});
            }
            Debug.Log("[MenuHeaderCommanderScenes] result=Passed savedIndices=6 fullPanel=True nativeCaptures=6");
        }
        finally
        { GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath),locale,persist:false); World.DefaultGameObjectInjectionWorld=previous; UiShellEcsGateway.RegisterAsRuntimeGateway(); }
    }

    private static Entity CreatePresentation(World world,string mission,bool completed,uint completedMask)
    {
        var em=world.EntityManager;
        var root=em.CreateEntity(typeof(UiShellRootComponent),typeof(UiCampaignOperationsComponent),typeof(UiShellStateComponent),typeof(UiShellMainMenuResourcesComponent),typeof(UiShellCommanderProfileComponent));
        em.SetComponentData(root,new UiShellStateComponent{ActiveRoute=UIRoute.MainMenu,CurrentMode=UiShellMode.MainMenu});
        em.SetComponentData(root,new UiShellMainMenuResourcesComponent{CreditsText=new FixedString32Bytes("0")});
        em.SetComponentData(root,new UiShellCommanderProfileComponent{Name=new FixedString64Bytes("Commander"),PortraitClass=new FixedString64Bytes("0")});
        em.SetComponentData(root,new UiCampaignOperationsComponent{Version=1,SelectedMissionId=new FixedString64Bytes(mission),DisplayName=new FixedString64Bytes(mission==CampaignMissionSequence.SteelPush?"STEEL PUSH":"ESTABLISH THE BASE"),ContentReady=1,Available=1,FirstClearCompleted=completed?(byte)1:(byte)0,AvailableMissionMask=0x1ffff,CompletedMissionMask=completedMask});
        em.AddBuffer<UiCampaignMissionActionRequestElement>(root); em.AddBuffer<UiShellRouteRequestComponent>(root); em.AddBuffer<UiShellRouteHistoryComponent>(root);
        var briefing=em.CreateEntity(typeof(UiMissionBriefingComponent));
        var data=new UiMissionBriefingComponent{Version=1,MissionId=new FixedString64Bytes(mission),DisplayNameKey=new FixedString64Bytes(mission==CampaignMissionSequence.SteelPush?"mission.steel_push.name":"mission.m02.name"),DisplaySummaryKey=new FixedString64Bytes(mission==CampaignMissionSequence.SteelPush?"mission.steel_push.summary":"mission.m02.summary")};
        data.Objectives.Add(new UiMissionObjectiveProjectionData{RequiredCount=1});em.SetComponentData(briefing,data);
        return root;
    }
}
