using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSharedMaterialsTests
    {
        [Test]
        public void S004UsesFixedDaylightAndRestoresTheEnvironment()
        {
            using var world=new World(nameof(S004UsesFixedDaylightAndRestoresTheEnvironment));
            var em=world.EntityManager;
            Assert.IsTrue(SkirmishSetupBriefing.TryLoad("S004",104733,out var setup));
            var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord {Setup=setup});
            Assert.IsFalse(MissionDayNightPolicyUtility.ShouldEnableDayNightVisuals(em));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent {IsLegacy=1});
            Assert.IsTrue(MissionDayNightPolicyUtility.ShouldEnableDayNightVisuals(em));
            var root=new GameObject("S004DaylightTest");
            var sky=RenderSettings.ambientSkyColor;
            try
            {
                var light=root.AddComponent<Light>(); light.type=LightType.Directional;
                light.intensity=.6f; light.color=Color.blue;
                var direction=root.AddComponent<Game.Composition.SkirmishS004Daylight>();
                direction.Configure(light);
                Assert.Greater(light.intensity,1f);
                Assert.AreEqual(LightShadows.Soft,light.shadows);
                direction.SendMessage("OnDestroy");
                Assert.AreEqual(.6f,light.intensity);
                Assert.AreEqual(Color.blue,light.color);
                Assert.AreEqual(sky,RenderSettings.ambientSkyColor);
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
            Debug.Log("[S004FixedDaylightValidation] result=Passed restored=1 legacyPolicyPreserved=1");
        }

        [Test]
        public void S004SourceSceneryIsNeutralWithoutChangingAuthoredPlacements()
        {
            var source=ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
            var additions=ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
            MapBuildingPlacementConfig overlay=null;
            try
            {
                var entry=new MapBuildingPlacementConfigEntry("Source/House","house",null,1,
                    new Vector3(12,0,15),new Vector3(10,0,13),new Vector3(0,90,0),Vector3.one,90,true,false,true);
                source.EditorSetPlacements(new System.Collections.Generic.List<MapBuildingPlacementConfigEntry>{entry});
                overlay=MapBuildingPlacementConfig.CreateRuntimeOverlay(source,additions,true);
                Assert.AreEqual(1,source.Placements[0].FactionId);
                Assert.AreEqual(0,overlay.Placements[0].FactionId);
                Assert.AreEqual(entry.WorldPosition,overlay.Placements[0].WorldPosition);
                Assert.AreEqual(entry.WorldCenter,overlay.Placements[0].WorldCenter);
                Assert.AreEqual(entry.WorldEulerAngles,overlay.Placements[0].WorldEulerAngles);
                Assert.IsTrue(overlay.Placements[0].UseGeometricFootprintCenter);
                Assert.IsTrue(overlay.Placements[0].RotateVertical);
            }
            finally
            {
                if(overlay!=null) UnityEngine.Object.DestroyImmediate(overlay);
                UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(additions);
            }
        }
        [Test]
        public void S004SceneryInstancingRestoresRenderersAndPreservesSharedMaterials()
        {
            var root = new GameObject("Map");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = false };
            var manifest = ScriptableObject.CreateInstance<Game.Rendering.StaticMapPresentationManifest>();
            try
            {
                var sources = new System.Collections.Generic.List<Game.Rendering.StaticMapPresentationSourceEntry>();
                var renderers = new System.Collections.Generic.List<MeshRenderer>();
                for (int i = 0; i < 3; i++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "Court" + i; cube.transform.SetParent(root.transform, false);
                    cube.transform.localPosition = new Vector3(2 + i * 3, 0, 2);
                    var renderer = cube.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = material; renderer.enabled = i != 2;
                    renderers.Add(renderer);
                    sources.Add(new Game.Rendering.StaticMapPresentationSourceEntry("source" + i,
                        "Map[" + root.transform.GetSiblingIndex() + "]/Court" + i + "[" + i + "]",
                        "test", "chunk", cube.name, renderer.bounds, cube.GetComponent<MeshFilter>().sharedMesh, "", 0,
                        new System.Collections.Generic.List<Game.Rendering.StaticMapPresentationMaterialEntry>(), false));
                }
                manifest.EditorSetData("opmap.skirmish.desert_base_01", "source-guid", "source.unity", "hash", 64, "hash",
                    new System.Collections.Generic.List<Game.Rendering.StaticMapPresentationChunkEntry>(), sources);
                var instances = root.AddComponent<Game.Composition.SkirmishS004SceneryInstancing>();
                instances.Configure(null, root.transform, manifest, 0, true);
                Assert.AreEqual(2, instances.SuppressedRendererCount);
                Assert.AreEqual(1, instances.BatchCount);
                Assert.IsFalse(material.enableInstancing);
                Assert.IsFalse(renderers[0].enabled);
                instances.enabled = false;
                instances.SendMessage("OnDisable"); // Edit-mode tests do not dispatch play-mode lifecycle callbacks.
                Assert.IsTrue(renderers[0].enabled);
                Assert.IsTrue(renderers[1].enabled);
                Assert.IsFalse(renderers[2].enabled);
                Assert.AreSame(material, renderers[0].sharedMaterial);
                instances.SendMessage("OnDestroy");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(manifest);
            }
        }
        [Test]
        public void AriaButtonBindingIgnoresOtherHeaderCanvasButtons()
        {
            var header = new GameObject("Header");
            try
            {
                var resource = new GameObject("Resource", typeof(RectTransform), typeof(Canvas),
                    typeof(UnityEngine.UI.GraphicRaycaster), typeof(UnityEngine.UI.Button));
                resource.transform.SetParent(header.transform);
                var aria = new GameObject("Aria", typeof(RectTransform), typeof(Canvas),
                    typeof(UnityEngine.UI.GraphicRaycaster), typeof(UnityEngine.UI.Button),
                    typeof(Game.UI.Runtime.AriaTutorialBriefingView));
                aria.transform.SetParent(header.transform);
                var root = Game.UI.Runtime.MatchHudAssistantReferenceUiSystemHelper.ResolveButton(header, out var button);
                Assert.AreSame(aria.transform, root);
                Assert.AreSame(aria.GetComponent<UnityEngine.UI.Button>(), button);
                var state = new GameObject("State", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                state.transform.SetParent(aria.transform);
                var cue = new GameObject("AlertCue", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                cue.transform.SetParent(aria.transform);
                var explanation = new GameObject("AriaPlayExplanation", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                explanation.transform.SetParent(aria.transform);
                Assert.IsTrue(Game.UI.Runtime.MatchHudAssistantReferenceUiSystemHelper.TryResolveButtonText(
                    root, out var stateText, out var cueText));
                Assert.AreSame(state.GetComponent<TMPro.TMP_Text>(), stateText);
                Assert.AreSame(cue.GetComponent<TMPro.TMP_Text>(), cueText);
            }
            finally { UnityEngine.Object.DestroyImmediate(header); }
        }

        [Test]
        public void ConstructionAndExpandedOrdersShareOneBankWithoutRepeatedGrants()
        {
            using var world = new World(nameof(ConstructionAndExpandedOrdersShareOneBankWithoutRepeatedGrants));
            var em = world.EntityManager;
            Entity session = em.CreateEntity();
            var setup = new SkirmishResolvedSetup { MaterialsEach = 450, MaterialsCapacityEach = 800 };
            em.AddComponentData(session, new SkirmishEconomyStockComponent { Materials = 9999 });
            SkirmishMaterialsService.Initialize(em, session, setup);
            Entity bank = SkirmishMaterialsService.FindEconomy(em, 1);
            var economy = em.GetComponentData<FactionEconomy>(bank);
            var materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 0, 40));
            em.SetComponentData(bank, economy);
            em.SetComponentData(bank, materials);
            Assert.AreEqual(410, SkirmishMaterialsService.Read(em, session, 1));
            SkirmishMaterialsService.Initialize(em, session, setup);
            Assert.AreEqual(410, SkirmishMaterialsService.Read(em, session, 1));
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 330, FactionTacticalMaterialsSpendKind.Production);
            Assert.AreEqual(330, em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current);
            Assert.AreEqual(450, SkirmishMaterialsService.Read(em, session, 2));
            Assert.AreEqual(9999, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials,
                "The compatibility stock must not be mirrored or used as a second bank.");
            materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            Assert.AreEqual(FactionConstructionResourceMutationResult.InsufficientMaterials,
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 0, 400));
            Assert.AreEqual(330, materials.Current);
            Assert.AreEqual(120, materials.LifetimeSpent);
            Assert.AreEqual(80, materials.LifetimeProductionSpent);
            Assert.AreEqual(40, materials.LifetimeConstructionSpent);
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 90, FactionTacticalMaterialsSpendKind.Upgrade);
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 150, FactionTacticalMaterialsSpendKind.Production);
            materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            Assert.AreEqual(300, materials.LifetimeSpent, "Construction, production and research share net spending.");
            Assert.AreEqual(20, materials.LifetimeProductionSpent, "The production refund only changes its category.");
            Assert.AreEqual(240, materials.LifetimeUpgradeSpent);
            uint version = materials.Version;
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 150, FactionTacticalMaterialsSpendKind.Production);
            Assert.AreEqual(version, em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Version);
            Assert.Throws<InvalidOperationException>(() =>
                SkirmishMaterialsService.WriteTransaction(em, session, 1, 180, FactionTacticalMaterialsSpendKind.Production));
            Assert.AreEqual(150, em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current,
                "An unsupported extra refund cannot mutate the bank.");
        }

        [Test]
        public void CleanedExpandedSessionDoesNotSuppressTheNextMissionsMaterialsGrant()
        {
            using var world = new World(nameof(CleanedExpandedSessionDoesNotSuppressTheNextMissionsMaterialsGrant));
            var em = world.EntityManager;
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            SkirmishMaterialsService.Initialize(em, session, new SkirmishResolvedSetup { MaterialsEach = 450, MaterialsCapacityEach = 800 });
            em.SetComponentData(session, new SkirmishExpandedSessionComponent
            { Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Playing });
            SkirmishMaterialsService.Write(em, session, 1, 100);
            var nextMission = new InitialUnitsSpawnConfig
            { InitialMaterials = 600, MaterialsCapacity = 900, InitialAiMaterials = 500, AiMaterialsCapacity = 900 };
            FactionTacticalMaterialsStartupSystemHelper.ApplyInitialResourceTotals(em, nextMission);
            Assert.AreEqual(100, SkirmishMaterialsService.Read(em, session, 1));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent
            { Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Cleaning });
            FactionTacticalMaterialsStartupSystemHelper.ApplyInitialResourceTotals(em, nextMission);
            Assert.AreEqual(600, em.GetComponentData<FactionTacticalMaterialsComponent>(SkirmishMaterialsService.FindEconomy(em, 1)).Current);
        }

        [Test]
        public void AirMobileBriefingUsesPackagedLaunchFacts()
        {
            Assert.IsTrue(SkirmishSetupBriefing.TryLoad("S003", 104732, out var setup));
            var en = new SkirmishSetupBriefing(setup, false);
            StringAssert.Contains("18-minute", en.Summary);
            StringAssert.Contains("12 infantry", en.Roster);
            StringAssert.Contains("450 Materials", en.Economy);
            StringAssert.Contains("350 Fuel", en.Economy);
            var fa = new SkirmishSetupBriefing(setup, true);
            StringAssert.Contains("18", fa.Summary);
            Assert.AreNotEqual(en.Objective, fa.Objective);
        }

        [Test]
        public void EstablishedAirMobileBriefingUsesStartingPadAndGrants()
        {
            Assert.IsTrue(SkirmishSetupBriefing.TryLoad("S004", 104733, out var setup));
            var en = new SkirmishSetupBriefing(setup, false);
            StringAssert.Contains("20 infantry", en.Roster);
            StringAssert.Contains("1 aircraft", en.Roster);
            StringAssert.Contains("900 Materials", en.Economy);
            StringAssert.Contains("700 Fuel", en.Economy);
            StringAssert.Contains("Your Helipad is ready", SkirmishSetupBriefing.MatchHelp(setup, false));
            StringAssert.DoesNotContain("upgrade readiness", SkirmishSetupBriefing.MatchHelp(setup, false));
            StringAssert.Contains("پد بالگردت آماده است", SkirmishSetupBriefing.MatchHelp(setup, true));
        }

        [Test]
        public void S004LibraryCopyHonorsRequestedLocaleWhileEditorUsesFarsi()
        {
            string previous = GameLocalization.CurrentLocaleCode;
            try
            {
                GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false);
                var entry = new SkirmishBattleCatalogEntry { ScenarioId = "S004" };
                SkirmishExpandedCopyProjection.ApplyLibraryCopyS004(ref entry);
                StringAssert.Contains("Scout with infantry", entry.DescriptionEnglish);
                StringAssert.Contains("با پیاده", entry.DescriptionFarsi);
                Assert.AreNotEqual(entry.DescriptionEnglish, entry.DescriptionFarsi);
                Assert.AreEqual(2, entry.ContentVersion);
                Assert.AreEqual(GameLocalization.PersianLocaleCode, GameLocalization.CurrentLocaleCode);
            }
            finally { GameLocalization.SetLocale(previous, false); }
        }

        [Test]
        public void InputReceiptsRejectDirectStaleAndReusedCommands()
        {
            try
            {
                AriaCommandEvidence.Begin();
                Assert.IsFalse(AriaCommandEvidence.TryReadAudit(0, 0, out int unobserved));
                Assert.AreEqual(-1, unobserved);
                AriaCommandEvidence.Accepted("unsupported direct order", 1);
                Assert.AreEqual(1, AriaCommandEvidence.Violations);
                AriaCommandEvidence.ObserveRelease(10, new Vector2(30, 40));
                Assert.AreEqual(0u, AriaCommandEvidence.ClaimRelease(10, new Vector2(99, 40)));
                uint receipt = AriaCommandEvidence.ClaimRelease(11, new Vector2(30, 40));
                Assert.AreNotEqual(0u, receipt);
                Assert.AreEqual(0u, AriaCommandEvidence.ClaimRelease(11));
                using (AriaCommandEvidence.Enter(receipt))
                {
                    AriaCommandEvidence.Accepted("queued world order", 1);
                    AriaCommandEvidence.Accepted("second member of same group", 1);
                }
                Assert.AreEqual(1, AriaCommandEvidence.Violations);
                using (AriaCommandEvidence.Enter(receipt)) AriaCommandEvidence.Accepted("reused receipt", 1);
                Assert.AreEqual(2, AriaCommandEvidence.Violations);
                AriaCommandEvidence.ObserveRelease(20, Vector2.zero);
                Assert.AreEqual(0u, AriaCommandEvidence.ClaimRelease(23));
                AriaCommandEvidence.Accepted("enemy counter", 2);
                Assert.AreEqual(4, AriaCommandEvidence.AcceptedCommands);
                Assert.AreEqual(2, AriaCommandEvidence.Violations);
                AriaCommandEvidence.ObserveRelease(30, Vector2.zero);
                uint previousAttempt = AriaCommandEvidence.ClaimRelease(30);
                AriaCommandEvidence.Begin();
                AriaCommandEvidence.ObserveRelease(31, Vector2.zero);
                uint nextAttempt = AriaCommandEvidence.ClaimRelease(31);
                Assert.AreNotEqual(previousAttempt, nextAttempt);
                using (AriaCommandEvidence.Enter(previousAttempt)) AriaCommandEvidence.Accepted("previous attempt", 1);
                using (AriaCommandEvidence.Enter(nextAttempt)) AriaCommandEvidence.Accepted("fresh attempt", 1);
                Assert.AreEqual(1, AriaCommandEvidence.Violations);
                Assert.IsTrue(AriaCommandEvidence.TryReadAudit(1, 2, out int observed));
                Assert.AreEqual(3, observed, "Direct commands and unexpected samples are both violations.");
                Assert.IsFalse(AriaCommandEvidence.TryReadAudit(2, 0, out _), "Missing release observation is unknown.");
                AriaCommandEvidence.End();
                Assert.IsFalse(AriaCommandEvidence.TryReadAudit(1, 0, out _));
                AriaCommandEvidence.Begin();
                AriaCommandEvidence.ObserveRelease(40, Vector2.zero);
                using (AriaCommandEvidence.Enter(AriaCommandEvidence.ClaimRelease(40)))
                    AriaCommandEvidence.Accepted("verified request", 1);
                Assert.IsTrue(AriaCommandEvidence.TryReadAudit(1, 0, out int clean));
                Assert.AreEqual(0, clean);
            }
            finally { AriaCommandEvidence.End(); }
        }

        public static void RunBroaderRegressionValidation()
        {
            var failures = new System.Collections.Generic.List<string>();
            void Run(Action action, string name)
            {
                try { action(); }
                catch (Exception exception) { failures.Add(name); Debug.LogError(name + " failed\n" + exception); }
            }
            Run(RunFocusedValidation, "SharedGameplay");
            Run(global::FactionTacticalMaterialsUtilitySystemHelperTests.RunFocusedValidation, "SharedMaterialsUtility");
            Run(SkirmishExpandedDefinitionTests.RunFocusedValidation, "Definitions");
            Run(SkirmishExpandedCatalogTests.RunFocusedValidation, "Catalog");
            Run(SkirmishExpandedObjectiveTests.RunFocusedValidation, "Objectives");
            Run(SkirmishExpandedVisualTests.RunFocusedValidation, "VisualBindings");
            Run(SkirmishExpandedAcceptanceTests.RunFocusedValidation, "EvidenceGates");
            Run(() => Debug.Log(global::SkirmishScenarioSourceBindingTests.RunFocusedValidation()), "SourceBindings");
            Run(() => new SkirmishSharedMaterialsTests().S004CampaignLightingUsesAuthoredSunAndRestoresMatchLights(), "S004PresentationEnvironment");
            if (failures.Count > 0)
                throw new InvalidOperationException("[SkirmishPlayerReadyRegressions] result=Failed suites=" + string.Join(",", failures));
            Debug.Log("[SkirmishPlayerReadyRegressions] result=Passed");
        }

        public static void RunBroaderAndCampaignValidation()
        {
            string previousLocale=GameLocalization.CurrentLocaleCode;
            try
            {
                RunBroaderRegressionValidation();
                // Opening Menu in the environment fixture restores the saved
                // locale. This Campaign fixture explicitly asserts English copy.
                GameLocalization.SetLocale("en",false);
                global::M01FirstContactCampaignUiTests.RunFocusedValidation();
            }
            finally { GameLocalization.SetLocale(previousLocale,false); }
        }

        public static void RunS004DeliveryValidation()
        {
            string previousLocale = GameLocalization.CurrentLocaleCode;
            try
            {
                GameLocalization.SetLocale("en", false);
                Game.Editor.SkirmishDefinitionBuilder.RefreshS004RoleBindings();
                Debug.Log(Game.Editor.SkirmishBattleCatalogBuilder.RebuildLibraryAndPrefab());
                RunBroaderRegressionValidation();
                new global::MatchHudSquadTrayQuickSelectTests().RtlPaginationKeepsBothArrowHitAreasInsideTheNavigationRow();
                ValidateS004Environment();
                new SkirmishSharedMaterialsTests().S004CampaignLightingUsesAuthoredSunAndRestoresMatchLights();
                new SkirmishSharedMaterialsTests().S004UsesFixedDaylightAndRestoresTheEnvironment();
                new SkirmishSharedMaterialsTests().S004SourceSceneryIsNeutralWithoutChangingAuthoredPlacements();
                new SkirmishSharedMaterialsTests().S004SceneryInstancingRestoresRenderersAndPreservesSharedMaterials();
                foreach (bool preserveGuidance in new[] { false, true })
                {
                    var drawer = new global::BuildDrawerCatalogQueryUiSystemHelperTests();
                    drawer.SetUp();
                    try { drawer.BuildDrawerPopup_HidesDuplicateMatchHudChromeAndRestoresItOnClose(preserveGuidance); }
                    finally { drawer.TearDown(); }
                }
                Debug.Log("[SkirmishDrawerGuidanceValidation] result=Passed cases=2");
                ValidationExit.ClearLastExitCode();
                global::M01FirstContactCampaignUiTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0)
                    throw new InvalidOperationException("Campaign UI regression validation failed.");
                SkirmishExpandedEconomyTests.RunFocusedValidation();
                SkirmishExpandedAriaTests.RunFocusedValidation();
                Debug.Log("[SkirmishS004DeliveryValidation] result=Passed");
            }
            finally
            {
                GameLocalization.SetLocale(previousLocale, false);
            }
        }

        [Test]
        public void S004CampaignLightingUsesAuthoredSunAndRestoresMatchLights()
        {
            SharedRuntimeRenderPipelineLease.ShutdownSession();
            var match=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
            var source=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
            try
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(match);
                var hostLight=new GameObject("MatchSun").AddComponent<Light>();hostLight.type=LightType.Directional;hostLight.enabled=true;
                var root=new GameObject("LightingOwner");
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=Color.blue;RenderSettings.sun=hostLight;
                RenderSettings.fog=true;
                var originalPipeline=QualitySettings.renderPipeline;
                var activePipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                float originalShadowDistance=activePipeline!=null ? activePipeline.shadowDistance : 0;
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(source);
                var sun=new GameObject("CampaignSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;
                sun.transform.rotation=Quaternion.Euler(36,-32,0);
                var rotation=sun.transform.rotation;
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=Color.yellow;RenderSettings.sun=sun;
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(match);
                var lighting=root.AddComponent<Game.Composition.SkirmishS004CampaignLighting>();lighting.Configure(match,source);
                Assert.AreEqual(match,UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Assert.IsFalse(hostLight.enabled);Assert.IsTrue(sun.enabled);Assert.AreEqual(1.3f,sun.intensity);
                Assert.AreEqual(rotation,sun.transform.rotation);Assert.AreEqual(new Color(.79f,.86f,.96f),RenderSettings.ambientSkyColor);Assert.AreSame(sun,RenderSettings.sun);
                Assert.IsFalse(RenderSettings.fog);
                if(activePipeline!=null)
                {
                    var runtimePipeline=QualitySettings.renderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                    Assert.NotNull(runtimePipeline);
                    Assert.AreNotSame(activePipeline,runtimePipeline);
                    Assert.AreSame(activePipeline,SharedRuntimeRenderPipelineLease.SourceFor(runtimePipeline));
                    Assert.GreaterOrEqual(runtimePipeline.shadowDistance,Application.isMobilePlatform?160f:260f);
                    Assert.AreEqual(originalShadowDistance,activePipeline.shadowDistance);
                    QualitySettings.renderPipeline=originalPipeline;
                    lighting.SendMessage("LateUpdate");
                    Assert.AreSame(runtimePipeline,QualitySettings.renderPipeline,
                        "An external reset should reselect the cached asset, not create another clone.");
                    Assert.AreEqual(1,SharedRuntimeRenderPipelineLease.RuntimeAssetCount);
                    lighting.SendMessage("OnDestroy");
                    Assert.AreSame(runtimePipeline,QualitySettings.renderPipeline,
                        "The selected quality clone must survive match teardown for Replay.");
                    Assert.AreEqual(originalShadowDistance,runtimePipeline.shadowDistance);
                    UnityEngine.Object.DestroyImmediate(lighting);
                    var replayLighting=root.AddComponent<Game.Composition.SkirmishS004CampaignLighting>();
                    replayLighting.Configure(match,source);
                    Assert.AreSame(runtimePipeline,QualitySettings.renderPipeline);
                    Assert.GreaterOrEqual(runtimePipeline.shadowDistance,Application.isMobilePlatform?160f:260f);
                    replayLighting.SendMessage("OnDestroy");
                    UnityEngine.Object.DestroyImmediate(replayLighting);
                    Assert.AreEqual(originalShadowDistance,runtimePipeline.shadowDistance);
                }
                else
                {
                    lighting.SendMessage("OnDestroy");
                    UnityEngine.Object.DestroyImmediate(lighting);
                }
                Assert.IsTrue(hostLight.enabled);Assert.AreEqual(Color.blue,RenderSettings.ambientSkyColor);Assert.AreSame(hostLight,RenderSettings.sun);
                Assert.IsTrue(RenderSettings.fog);
                SharedRuntimeRenderPipelineLease.ShutdownSession();
                Assert.AreSame(originalPipeline,QualitySettings.renderPipeline);
            }
            finally
            {
                SharedRuntimeRenderPipelineLease.ShutdownSession();
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(match);
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(source,true);
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
            }
            Debug.Log("[S004CampaignLightingValidation] result=Passed authoredSunPreserved=1 matchLightsRestored=1");
        }

        [Test]
        public void SharedRuntimePipelineReusesQualityAssetsAcrossCampaignReplays()
        {
            SharedRuntimeRenderPipelineLease.ShutdownSession();
            var originalPipeline=QualitySettings.renderPipeline;
            var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<VisualQualityProfileAsset>(
                "Assets/Game/Rendering/VisualQualityConfig.asset");
            Assert.NotNull(profile);
            var lowSource=profile.LowRenderPipelineAsset;
            var highSource=profile.HighRenderPipelineAsset;
            Assert.NotNull(lowSource);Assert.NotNull(highSource);
            float lowSourceScale=lowSource.renderScale,highSourceScale=highSource.renderScale;
            float lowSourceShadow=lowSource.shadowDistance,highSourceShadow=highSource.shadowDistance;
            int lowSourceMsaa=lowSource.msaaSampleCount,highSourceMsaa=highSource.msaaSampleCount;
            var campaignOwner=new object();
            try
            {
                var lowRuntime=SharedRuntimeRenderPipelineLease.SelectQuality(lowSource,.5f);
                Assert.AreSame(lowRuntime,QualitySettings.renderPipeline);
                Assert.AreSame(lowSource,SharedRuntimeRenderPipelineLease.SourceFor(lowRuntime));
                Assert.AreEqual(lowSourceMsaa,lowRuntime.msaaSampleCount);
                Assert.IsTrue(SharedRuntimeRenderPipelineLease.AcquireCampaignShadows(campaignOwner));
                Assert.GreaterOrEqual(lowRuntime.shadowDistance,Application.isMobilePlatform?160f:260f);

                var highRuntime=SharedRuntimeRenderPipelineLease.SelectQuality(highSource,.75f);
                Assert.AreNotSame(lowRuntime,highRuntime);
                Assert.AreEqual(lowSourceShadow,lowRuntime.shadowDistance);
                Assert.GreaterOrEqual(highRuntime.shadowDistance,Application.isMobilePlatform?160f:260f);
                Assert.AreEqual(highSourceMsaa,highRuntime.msaaSampleCount);
                SharedRuntimeRenderPipelineLease.ReleaseCampaignShadows(new object());
                Assert.IsTrue(SharedRuntimeRenderPipelineLease.HasCampaignShadowOverride);
                SharedRuntimeRenderPipelineLease.ReleaseCampaignShadows(campaignOwner);
                Assert.AreEqual(highSourceShadow,highRuntime.shadowDistance);

                var replayRuntime=SharedRuntimeRenderPipelineLease.SelectQuality(lowSource,.5f);
                Assert.AreSame(lowRuntime,replayRuntime);
                Assert.AreEqual(2,SharedRuntimeRenderPipelineLease.RuntimeAssetCount);
                Assert.AreEqual(lowSourceScale,lowSource.renderScale);
                Assert.AreEqual(highSourceScale,highSource.renderScale);
                Assert.AreEqual(lowSourceShadow,lowSource.shadowDistance);
                Assert.AreEqual(highSourceShadow,highSource.shadowDistance);
            }
            finally
            {
                SharedRuntimeRenderPipelineLease.ReleaseCampaignShadows(campaignOwner);
                SharedRuntimeRenderPipelineLease.ShutdownSession();
            }
            Assert.AreSame(originalPipeline,QualitySettings.renderPipeline);
        }

        public static void RunSharedRuntimePipelineLeaseValidation()
        {
            try
            {
                var tests=new SkirmishSharedMaterialsTests();
                tests.SharedRuntimePipelineReusesQualityAssetsAcrossCampaignReplays();
                tests.S004CampaignLightingUsesAuthoredSunAndRestoresMatchLights();
                global::AndroidVisualQualityValidationTests.RuntimeQualityTierMappingsAreComplete();
                Debug.Log("[SharedRuntimePipelineLeaseValidation] result=Passed cases=3");
                ValidationExit.Passed();
            }
            catch(Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[SharedRuntimePipelineLeaseValidation] result=Failed");
                ValidationExit.Failed();
            }
        }

        private static void ValidateS004Environment()
        {
            var map=Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap");
            var source=UnityEditor.AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Game.Editor.SkirmishS004CampaignMapBuilder.SourcePath);
            Assert.NotNull(map);Assert.NotNull(source);
            Assert.IsTrue(map.TryValidateMetadata(out var error),error);
            Assert.AreEqual(source.OperationMapId,map.SourceBinding.SourceOperationMapId);
            Assert.AreEqual(source.SourceIdentityHash,map.SourceBinding.SourceIdentityHash);
            Assert.AreEqual(source.ContentHash,map.SourceBinding.SourceContentHash);
            Assert.AreEqual(source.SourceSceneReference.AssetGUID,map.SourceSceneReference.AssetGUID);
            Assert.AreEqual(source.NavigationMetadata.AuthoredSubSceneGuid,map.NavigationMetadata.AuthoredSubSceneGuid);
            Assert.AreEqual(source.MapSurfaceDataReference.AssetGUID,map.MapSurfaceDataReference.AssetGUID);
            Assert.AreEqual(source.RenderResidencyMode,map.RenderResidencyMode);
            Assert.IsNull(map.AdditionalBuildingPlacements);
            var layout=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(Game.Editor.SkirmishDefinitionBuilder.ScenarioFolderS004+"/SkirmishLayout_S004.asset");
            Assert.AreEqual("campaign.cityedgeairfield.s004.townedge.v2",layout.WorldBinding.SourceHash);
            foreach(var anchor in layout.Anchors)
            {
                Assert.IsTrue(layout.TryProjectToMap(anchor.NormalizedU,anchor.NormalizedV,out float x,out float z));
                Assert.That(x,Is.InRange(400f,1000f));Assert.That(z,Is.InRange(300f,700f));
            }
            Debug.Log("[SkirmishS004EnvironmentValidation] result=Passed campaignReuse=CityEdgeAirfield additionalTown=0");
        }

        public static void RunFocusedValidation()
        {
            try
            {
                new SkirmishSharedMaterialsTests().InputReceiptsRejectDirectStaleAndReusedCommands();
                new SkirmishStartingBuildingsTests().StartingGrantsWaitForSharedResultsAndBindTheActualBaseOnce();
                new SkirmishStartingBuildingsTests().PhysicalSupplyGrantIsAtomicAndDoesNotRefillOnRepeatedStartup();
                new SkirmishStartingBuildingsTests().OilGrantStartsFabricationAndTheHaulChainBeforeRefineryOverflow();
                new SkirmishSharedMaterialsTests().AirMobileBriefingUsesPackagedLaunchFacts();
                new SkirmishSharedMaterialsTests().EstablishedAirMobileBriefingUsesStartingPadAndGrants();
                new SkirmishSharedMaterialsTests().S004LibraryCopyHonorsRequestedLocaleWhileEditorUsesFarsi();
                new SkirmishSharedMaterialsTests().AriaButtonBindingIgnoresOtherHeaderCanvasButtons();
                new SkirmishSharedMaterialsTests().ConstructionAndExpandedOrdersShareOneBankWithoutRepeatedGrants();
                new SkirmishSharedMaterialsTests().CleanedExpandedSessionDoesNotSuppressTheNextMissionsMaterialsGrant();
                new SkirmishS002AriaHarnessTests().UnknownInputEvidenceCannotCountAsAriaWin();
                new UnitCombatFocusedEditModeTests().StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
                new UnitCombatFocusedEditModeTests().StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();
                new AutomaticCombatFactionTargetingTests().AcquisitionAndRetaliationRespectAuthoredDomainsAndVisibility();
                new AutomaticCombatFactionTargetingTests().AttackMove_InterruptsCommandedPathForHostileButPlainMoveDoesNot();
                new AutomaticCombatFactionTargetingTests().AttackMove_AcquiresHostileBuildingButIgnoresNeutralDeadAndSuppressedBuildings();
                var towers = new global::BuildingDefenseAttackSystemTests();
                towers.ConstructedStructuresUseAttemptPolicyAndCleanupWithoutReplacingTheObjective();
                towers.GuardTowerDefense_MissionOverlayBindsBothFactionsAndPreservesLegacy();
                towers.GuardTowerDefense_PolicyAppliesAtAcquisitionAndBeforeCachedShot();
                towers.GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget();
                towers.GuardTowerDefense_IgnoresNeutralTargetsAndFiresAtHostileTarget();
                UnitMoveOrderSystemTests.RunFocusedValidation();
                AIFactionControlStartupSystemValidationTests.RunFocusedValidation();
                new SkirmishSharedCombatTests().RuntimeActorAndLinkedVisualSurviveSourceSceneUnload();
                new SkirmishSharedCombatTests().RegistryActorsNeverReceiveSupplementalStructureDamage();
                new SkirmishSharedCombatTests().CommandedSiegeUsesNativeOrdersAndRejectsIllegalTargets();
                new SkirmishSharedCombatTests().AntiAirOverlayBindsTheActualMissileWeapon();
                SkirmishExpandedAriaTests.RunFocusedValidation();
                new SkirmishExpandedArmyTests().AcceptedArmyOrdersRequireInputEvidence();
                SkirmishExpandedArmyTests.RunFocusedValidation();
                SkirmishExpandedEconomyTests.RunFocusedValidation();
                SkirmishExpandedCheckpointTests.RunFocusedValidation();
                SkirmishS002AriaHarnessTests.RunFocusedValidation();
                Game.Editor.AriaTouchInputValidation.Run();
                var fuel = new global::VehicleFuelConsumptionSystemTests();
                fuel.VehicleFuelConsumption_DrainsDeliveredFuelStorageAfterGridMovement();
                fuel.VehicleFuelConsumption_DoesNotDrainRefineryOutput();
                fuel.VehicleFuelConsumption_UsesAirFuelCostForAirUnits();
                fuel.AircraftFuelSafetyReturn_ZeroFuelClearsOrdersAndReturnsHome();
                fuel.AircraftFuelSafetyReturn_WithUsableFuelKeepsActiveOrders();
                fuel.AircraftFuelSafetyReturn_AlreadyReturningPreservesManeuverAndLandingProgress();
                fuel.GroundVehicleFuelHold_ZeroFuelClearsMovementAndStopsKinematics();
                fuel.GroundVehicleFuelHold_WithUsableFuelKeepsMovement();
                var recipes = new SkirmishSharedProductionRecipeTests();
                recipes.RuntimeRecipeChangesProducerPacketAndPriceWithoutMutatingAuthoredDefaults();
                recipes.CatalogOverlayLeavesOriginalBuildingListAndRegistryUntouched();
                recipes.AuthoredAirMobileCatalogResolvesEveryMissionRecipe();
                var header = new global::UiShellEcsGatewayResourceHeaderTests();
                header.ExpandedResourceHeaderShowsAllOilAndOnlyUnreservedUsableFuel();
                header.MatchHudResourceValues_UsesVersionedUsableFuelSummaryWithoutTextFallback();
                header.MatchHudResourceValues_WarmedVersionChangesDoNotAllocate();
                Debug.Log("[SkirmishSharedMaterialsTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishSharedMaterialsTests] result=Failed\n" + exception);
                throw;
            }
        }
    }
}
