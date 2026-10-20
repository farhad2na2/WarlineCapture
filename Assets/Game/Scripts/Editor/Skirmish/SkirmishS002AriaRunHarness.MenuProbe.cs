#if UNITY_EDITOR
using System;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static AriaTouchInputUiSystemHelper menuTouch;
        private static int menuStep;
        private static int menuLibraryBeat;
        private static string menuLibraryMap = "DB";
        private static string menuLibraryObjective = "BA";
        private static double menuDue;
        private static double menuDeadline;
        private static Button menuStableTarget;
        private static Vector2 menuStablePoint;
        private static double menuStableSince, menuLastTapAt;
        private static double menuDiagnosticDue;
        private static bool menuDiagnosticCaptured;
        private static SkirmishMenuLaunchPreparation menuLaunchPreparation;
        private static IDisposable candidateCatalogScope;
        private static SkirmishBattleCatalogConfig candidateCatalog;

        public static void RunS004EditorAriaEn() => RunEditorJourney("en", false);
        public static void RunS004EditorAriaStopEn() => RunEditorJourney("en", false, true);
        public static void RunS004EditorManualFa() => RunEditorJourney("fa-IR", true);
        public static void RunS005EditorAriaEn() => RunEditorJourney("en", false, false, "S005");
        public static void RunS005EditorAriaFa() => RunEditorJourney("fa-IR", false, false, "S005");
        public static void RunS005EditorAriaEnSeed2() => RunEditorJourney("en", false, false, "S005", 130368);
        public static void RunS005EditorAriaEnSeed3() => RunEditorJourney("en", false, false, "S005", 155926);
        public static void RunS005EditorAriaFaSeed2() => RunEditorJourney("fa-IR", false, false, "S005", 130368);
        public static void RunS005EditorAriaFaSeed3() => RunEditorJourney("fa-IR", false, false, "S005", 155926);
        public static void RunS005EditorAriaFaSeed3MainMenu()
        {
            RunEditorJourney("fa-IR", false, false, "S005", 155926);
            Environment.SetEnvironmentVariable("WARLINE_TERMINAL_ACTION", "MAIN MENU");
        }
        public static void RunS005EditorManualFa() => RunEditorJourney("fa-IR", true, false, "S005");
        public static void RunS005EditorPlacementClearanceProbe()
        {
            RunEditorJourney("en", false, false, "S005", 155926);
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_PLACEMENT_CLEARANCE", "1");
        }
        public static void RunS005EditorPaidBuildingClearanceProbe()
        {
            RunS005EditorStrikeCycleProbe();
            Environment.SetEnvironmentVariable("WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY", "1");
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_PLACEMENT_CLEARANCE", "1");
        }
        public static void RunS005EditorPaidFuelClickProbe()
        {
            RunS005EditorPaidBuildingClearanceProbe();
            Environment.SetEnvironmentVariable("WARLINE_S005_PAID_FUEL_CLICK_ONLY", "1");
            Environment.SetEnvironmentVariable("WARLINE_SELECTION_CLICK_DIAGNOSTICS", "1");
        }
        private static void RunEditorJourney(string language, bool manual, bool stopProbe=false, string selectedCatalog="S004", int selectedSeed=0)
        {
            // Pre-save failures in a reused Editor must not report the preceding
            // manual journey's counters before StartManualProbe runs.
            manualActions = manualAttackReceipts = 0;
            if (SkirmishMissionDispatch.RequiresSourceBoundDefinition(selectedCatalog))
            {
                if(candidateCatalogScope!=null)throw new InvalidOperationException("One owned catalog scope required.");
                var library=SkirmishBattleCatalogConfig.Load();
                if(library==null||!library.TryGet(selectedCatalog,out var row))
                    throw new InvalidOperationException("Exact existing generic library row required.");
                if(!row.IsPlayable)
                {
                    if(row.Status!=SkirmishBattleCatalogStatus.Planned||row.PlayableScenarioIndex>=0)
                        throw new InvalidOperationException("Unpublished generic draft row is malformed.");
                    var authored=SkirmishExpansionCatalogConfig.Load();
                    int index=1000+int.Parse(selectedCatalog.Substring(1),System.Globalization.CultureInfo.InvariantCulture);
                    candidateCatalogScope=SkirmishGenericMissionAvailabilityScope.Open(authored,selectedCatalog,index);
                }
            }
            else if (selectedCatalog == "S005" || codedSizeCameraActive)
            {
                if(candidateCatalogScope!=null)throw new InvalidOperationException("One owned catalog scope required.");
                var authored=SkirmishExpansionCatalogConfig.Load();
                if(authored==null||!authored.TryGetDefinition(selectedCatalog,out var exactDefinition))
                    throw new InvalidOperationException("Exact authored candidate definition required.");
                var source = SkirmishBattleCatalogConfig.Load();
                if (source == null) throw new InvalidOperationException("Packaged battle catalog is missing.");
                var rows = new SkirmishBattleCatalogEntry[source.Entries.Count];
                bool found = false;
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = source.Entries[i];
                    if (rows[i].ScenarioId != selectedCatalog) continue;
                    found = true;
                    rows[i].Status = SkirmishBattleCatalogStatus.Playable;
                    rows[i].PlayableScenarioIndex = selectedCatalog=="S002"?SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex:
                        selectedCatalog=="S003"?SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex:
                        selectedCatalog=="S004"?SkirmishPresetConfig.DesertBaseAirMobileEstablishedScenarioIndex:
                        SkirmishPresetConfig.DesertBaseCombinedArmsFieldScenarioIndex;
                    rows[i].DefinitionId = exactDefinition.DefinitionId;
                    rows[i].ContentVersion = exactDefinition.ContentVersion;
                    if(selectedCatalog=="S005")
                    {
                    rows[i].TitleKey = "skirmish.s005.title";
                    rows[i].DescriptionEnglish = "Advance with armor and infantry, or upgrade readiness to build a Helipad. Defend your original Barracks.";
                    rows[i].DescriptionFarsi = "با زرهی‌ها و پیاده‌ها پیشروی کن، یا آمادگی را ارتقا بده و هلی‌پد بساز. از سربازخانهٔ اصلی‌ات دفاع کن.";
                    }
                }
                if (!found) throw new InvalidOperationException("Exact candidate catalog entry is missing.");
                candidateCatalog = ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
                candidateCatalog.hideFlags = HideFlags.HideAndDontSave;
                candidateCatalog.Configure(rows);
                candidateCatalogScope = SkirmishBattleCatalogConfig.UseEditorValidationCatalog(candidateCatalog);
                Debug.Log("[S005CandidateAvailability] fixture=EditorMemoryOnly publication=InProgress shippedCatalogModified=0");
            }
            SessionState.SetBool("S004EditorJourney.OptionsSaved", true);
            SessionState.SetBool("S004EditorJourney.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("S004EditorJourney.Options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            // Match the shared native-probe completion contract: acquire one
            // refresh suspension and release it after the owned Play session ends.
            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", true);
            // A reused Editor retains process environment between validations.
            // A normal mission journey must not inherit a previous diagnostic.
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_PLACEMENT_CLEARANCE", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_PAID_FUEL_CLICK_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_STARTING_FUEL_CLICK_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_SINGLE_ACTOR_RETIREMENT_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_EMPTY_SELECTION_CAMERA_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_SELECTION_CLICK_DIAGNOSTICS", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_PLAYER_RESUME_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_NATIVE_RECOVERY_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_COMBAT_RESUME_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_SAVE_ROLE", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_BOARDED_RESUME_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_CAMERA_TOUCH_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_AIRPORT_PLACEMENT_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_FIXED_WING_CYCLE", null);
            Environment.SetEnvironmentVariable("WARLINE_S003_HELIPAD_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_AIRCRAFT_CYCLE", null);
            Environment.SetEnvironmentVariable("WARLINE_S003_NATIVE_REVIEW", null);
            Environment.SetEnvironmentVariable("WARLINE_MAP_REVIEW_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC", null);
            Environment.SetEnvironmentVariable("WARLINE_S003_PRODUCTION_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY", "1");
            Environment.SetEnvironmentVariable("WARLINE_S002_LOCALE", language);
            Environment.SetEnvironmentVariable("WARLINE_S002_SEED", selectedSeed > 0 ? selectedSeed.ToString(System.Globalization.CultureInfo.InvariantCulture) : selectedCatalog == "S005" ? "104734" : "104733");
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG", selectedCatalog);
            Environment.SetEnvironmentVariable("WARLINE_REVIEW_WIDTH", "2400");
            Environment.SetEnvironmentVariable("WARLINE_REVIEW_HEIGHT", "1080");
            Environment.SetEnvironmentVariable("WARLINE_TERMINAL_ACTION", manual ? "MAIN MENU" : "ADJUST SETUP");
            Environment.SetEnvironmentVariable("WARLINE_S004_STOP_PROBE", stopProbe ? "1" : null);
            Environment.SetEnvironmentVariable("WARLINE_S004_MANUAL_PROBE", manual ? "1" : null);
            menuStep = 0;
            menuLibraryBeat = 0;
            RunFocusedAriaAndExit();
        }

        private static void RestoreS004EditorJourneyOptions()
        {
            candidateCatalogScope?.Dispose();
            candidateCatalogScope = null;
            if (candidateCatalog != null) UnityEngine.Object.DestroyImmediate(candidateCatalog);
            candidateCatalog = null;
            if (!SessionState.GetBool("S004EditorJourney.OptionsSaved", false)) return;
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("S004EditorJourney.Options", 0);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("S004EditorJourney.OptionsEnabled", false);
            SessionState.EraseBool("S004EditorJourney.OptionsSaved");
        }

        private static bool EnterS004ThroughPresentedControls()
        {
            if (menuStep == 6) return true;
            if (menuTouch == null)
            {
                ConfigureBackgroundInput();
                menuStep = 0;
                menuLibraryBeat = 0;
                menuDeadline = EditorApplication.timeSinceStartup + 90d;
                menuStableTarget = null;
                menuStableSince = menuLastTapAt = 0;
                menuDiagnosticDue = 0;
                menuDiagnosticCaptured = false;
                menuLaunchPreparation = new SkirmishMenuLaunchPreparation();
                menuTouch = new AriaTouchInputUiSystemHelper();
                if (!menuTouch.Start()) throw new InvalidOperationException("Menu Touch unavailable.");
                var runner = new GameObject("S004MenuInputProbe");
                UnityEngine.Object.DontDestroyOnLoad(runner);
                runner.AddComponent<SkirmishS004MenuInputFrame>();
            }
            return false;
        }

        internal static void TickS004MenuPlayerFrame()
        {
            if (menuTouch == null) return;
            if (EditorApplication.timeSinceStartup > menuDeadline)
            {
                Debug.LogError("[S004MenuJourney] result=Failed reason=controlsTimeout step=" + menuStep);
                Finish(true, "menuControlsTimeout");
                return;
            }
            menuTouch.Tick(Time.unscaledTime);
            if (menuTouch.PlayerInterrupted || menuTouch.UnexpectedSamples != 0)
                throw new InvalidOperationException("Menu input interrupted.");
            if (menuTouch.IsBusy || EditorApplication.timeSinceStartup < menuDue) return;
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            if (menuStep == 1 && EditorApplication.timeSinceStartup - menuLastTapAt >= 2d &&
                EditorApplication.timeSinceStartup >= menuDiagnosticDue)
            {
                menuDiagnosticDue = EditorApplication.timeSinceStartup + 10d;
                ObservePresentedMenuControls(buttons);
            }
            if (menuStep == 4 && EditorApplication.timeSinceStartup >= menuDiagnosticDue)
            {
                menuDiagnosticDue = EditorApplication.timeSinceStartup + 10d;
                Debug.Log("[S005MenuLaunchObservation] screen=" + Screen.width + "x" + Screen.height +
                    " safeArea=" + Screen.safeArea + " step=4 readOnly=1 extraScreenshot=0");
                ObservePresentedMenuControls(buttons, false);
            }
            if (menuStep == 5)
            {
                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated &&
                    Game.Composition.SkirmishLaunchProjection.TryGet(world.EntityManager, out var observedSession, out var observedMatch))
                {
                    RecordProductionResolvedSnapshot(world.EntityManager, observedSession, in observedMatch,
                        Game.Composition.SkirmishLaunchProjection.IsSimulationActive(world.EntityManager),
                        Game.Composition.SkirmishLaunchProjection.ReadMatchElapsedSeconds(world.EntityManager, observedSession, in observedMatch),
                        "NormalMenuLaunchObserved");
                    if(codedSizeCameraActive) ValidateCodedSizeNativeIdentity(world.EntityManager,observedSession);
                    Debug.Log("[S004MenuJourney] result=Passed input=Touch controls=Skirmish,Scroll," +
                        catalogId + ",Launch seedFixture=" + seed + " nativeSessionObserved=1");
                    menuStep = 6;
                    StopS004MenuProbe();
                    return;
                }
                if (EditorApplication.timeSinceStartup - menuLastTapAt >= 2d)
                    foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude))
                        if (setup.IsBaseAssaultSetup && setup.SelectedScenarioId == catalogId)
                        {
                            menuStep = 4;
                            Debug.Log("[S004MenuJourney] phase=LaunchRetry input=Touch reason=SetupStillVisible");
                            break;
                        }
                return;
            }
            if (menuStep == 0)
            {
                foreach (var b in buttons)
                    if ((b.name == "Card_Skirmish" || b.transform.parent.name == "Card_Skirmish") &&
                        TryPresentedButtonPoint(b, out _))
                    { if (MenuTap(b)) menuStep++; return; }
                foreach (var b in buttons)
                    if ((b.name == "SkipButton" || b.name == "ConfirmButton") && TryPresentedButtonPoint(b, out _))
                    { MenuTap(b); return; }
            }
            else if (menuStep == 1)
            {
                if (AdvancePresentedLibraryNavigation(buttons))
                    return;
                if (!HasNewLibraryChrome(buttons) && AdvanceLibraryViewModel())
                    return;
                foreach (var b in buttons)
                    if (b.name == "ScenarioCard_" + catalogId && TryPresentedButtonPoint(b, out _))
                    { if (MenuTap(b)) menuStep = 4; return; }
                if (EditorApplication.timeSinceStartup - menuLastTapAt >= 2d)
                    foreach (var b in buttons)
                        if ((b.name == "Card_Skirmish" || b.transform.parent.name == "Card_Skirmish") &&
                            TryPresentedButtonPoint(b, out _))
                        {
                            if (MenuTap(b)) Debug.Log("[S004MenuJourney] phase=LibraryRetry input=Touch reason=MainMenuStillVisible");
                            return;
                        }
                foreach (var scroll in UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude))
                {
                    if (scroll.name != "BattleLibrary" || scroll.viewport == null) continue;
                    var rect = scroll.viewport;
                    var canvas = scroll.GetComponentInParent<Canvas>();
                    var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    // Follow the actual card position. A one-direction swipe
                    // can overshoot a short target row or start at retained scroll.
                    var from = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector2(rect.rect.center.x, rect.rect.yMin + rect.rect.height * .1f)));
                    var to = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector2(rect.rect.center.x, rect.rect.yMax - rect.rect.height * .1f)));
                    foreach (var card in buttons)
                        if (card.name == "ScenarioCard_" + catalogId)
                        {
                            var cardRect = (RectTransform)card.transform;
                            var point = RectTransformUtility.WorldToScreenPoint(camera, cardRect.TransformPoint(cardRect.rect.center));
                            if (point.y > (from.y + to.y) * .5f) (from, to) = (to, from);
                            break;
                        }
                    if (menuTouch.TryGesture(from, to, .15f, .75f, Time.unscaledTime))
                        menuDue = EditorApplication.timeSinceStartup + 1d;
                    return;
                }
            }
            else if (menuStep == 4)
            {
                var setup = SelectMenuSetupForLaunch(UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude), catalogId);
                if (setup == null)
                {
                    // The library persists inside a valid setup. Retry selection only
                    // when the actual exact selected setup is absent.
                    if (EditorApplication.timeSinceStartup - menuLastTapAt >= 2d)
                        foreach (var b in buttons)
                            if ((b.name == "ScenarioCard_" + catalogId ||
                                  b.name == SkirmishLibraryControlNames.BattleCard(catalogId)) &&
                                 TryPresentedButtonPoint(b, out _))
                            {
                                if (MenuTap(b)) Debug.Log("[S004MenuJourney] phase=ScenarioRetry input=Touch reason=SelectedSetupAbsent");
                                return;
                            }
                    return;
                }
                {
                    // Fixed seed is a test fixture; entry, selection and launch use native input.
                    if (menuLaunchPreparation.Read(setup, seed) == SkirmishMenuLaunchPreparation.Action.PrepareSetup)
                    {
                        setup.PrepareScenarioAdjustment(setup.ReadConfigFromControls().ScenarioIndex, seed);
                        menuLaunchPreparation.MarkPrepared(setup, seed);
                        menuStableTarget = null;
                        // Bind/SelectScenario rebuild the cards and presentation. Give
                        // the ordinary raycast/stability path a fresh subsequent frame.
                        return;
                    }
                    if(codedSizeCameraActive && !CodedSizeMenuReady(setup)) return;
                    foreach (var label in setup.GetComponentsInChildren<TMP_Text>())
                        if (label.name == "OperationName" || label.name == "SeedHelp")
                        {
                            label.ForceMeshUpdate();
                            Debug.Log("[S004SetupText] label=" + label.name + " rect=" + label.rectTransform.rect +
                                " fontSize=" + label.fontSize + " characters=" + label.textInfo.characterCount +
                                " visible=" + label.textInfo.characterCount + " overflow=" + label.isTextOverflowing);
                        }
                    foreach (var b in setup.GetComponentsInChildren<Button>())
                        if ((b.name.Contains("Launch") || b.name.Contains("Start")) && TryPresentedButtonPoint(b, out _))
                        {
                            if (MenuTap(b))
                            {
                                ScreenCapture.CaptureScreenshot(LiveTracePath() + ".setup.png");
                                Debug.Log("[S004MenuJourney] phase=LaunchTouch input=Touch controls=Skirmish,Scroll," + catalogId + ",Launch seedFixture=" + seed);
                                menuStep = 5;
                            }
                            return;
                        }
                }
            }
        }

        private static bool AdvancePresentedLibraryNavigation(Button[] buttons)
        {
            if (!HasNewLibraryChrome(buttons))
                return false;
            ResolveLibraryTarget();
            if (menuLibraryBeat == 0)
            {
                Button pin = FindNamed(buttons, SkirmishLibraryControlNames.TheaterPin(menuLibraryMap));
                if (pin == null)
                    menuLibraryBeat = 1;
                else if (MenuTap(pin))
                {
                    Debug.Log("[S004MenuJourney] phase=TheaterPin input=Touch map=" + menuLibraryMap +
                        " navigation=TheaterZoom");
                    menuLibraryBeat = 1;
                }

                return true;
            }

            if (menuLibraryBeat == 1)
            {
                Button zoom = FindNamed(buttons, SkirmishLibraryControlNames.ZoomIn);
                if (zoom != null && MenuTap(zoom))
                {
                    Debug.Log("[S004MenuJourney] phase=ZoomIn input=Touch navigation=TheaterZoom map=" + menuLibraryMap);
                    menuLibraryBeat = 2;
                }

                return true;
            }

            if (menuLibraryBeat == 2)
            {
                Button tab = FindNamed(buttons, SkirmishLibraryControlNames.ObjectiveTab(menuLibraryObjective));
                if (tab == null)
                    menuLibraryBeat = 3;
                else if (MenuTap(tab))
                {
                    Debug.Log("[S004MenuJourney] phase=ObjectiveTab input=Touch objective=" + menuLibraryObjective);
                    menuLibraryBeat = 3;
                }

                return true;
            }

            Button card = FindNamed(buttons, SkirmishLibraryControlNames.BattleCard(catalogId)) ??
                          FindNamed(buttons, "ScenarioCard_" + catalogId);
            if (card != null && MenuTap(card))
            {
                Debug.Log("[S004MenuJourney] phase=BattleCard input=Touch scenario=" + catalogId);
                menuStep = 4;
            }

            return true;
        }

        private static bool AdvanceLibraryViewModel()
        {
            if (menuLibraryBeat == 4)
                return false;
            ResolveLibraryTarget();
            QuickCustomScreenView setup = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude))
            {
                if (!candidate.IsBaseAssaultSetup)
                    continue;
                setup = candidate;
                break;
            }

            if (setup == null)
                return false;

            ISkirmishLibraryViewModel library;
            try
            {
                library = setup.Library;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[S004MenuJourney] phase=LibraryViewModel skipped=" + exception.Message);
                menuLibraryBeat = 4;
                return false;
            }

            if (!SkirmishLibraryNavigation.OpenTheaterBattlefieldCard(library, menuLibraryMap, catalogId))
            {
                menuLibraryBeat = 4;
                return false;
            }

            setup.SelectScenarioId(catalogId);
            if (!string.Equals(setup.SelectedScenarioId, catalogId, StringComparison.Ordinal) ||
                !string.Equals(library.Briefing.ScenarioId, catalogId, StringComparison.OrdinalIgnoreCase))
            {
                menuLibraryBeat = 4;
                return false;
            }

            menuLastTapAt = EditorApplication.timeSinceStartup;
            menuStep = 4;
            Debug.Log("[S004MenuJourney] phase=LibraryViewModel input=ViewModel navigation=Theater,Battlefield,Card map=" +
                      menuLibraryMap + " scenario=" + catalogId + " briefing=" + library.Briefing.ScenarioId);
            return true;
        }

        private static bool HasNewLibraryChrome(Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                string name = buttons[i] != null ? buttons[i].name : string.Empty;
                if (name == SkirmishLibraryControlNames.ZoomIn ||
                    name == SkirmishLibraryControlNames.QuickBattle ||
                    name.StartsWith("TheaterPin_", StringComparison.Ordinal) ||
                    name.StartsWith("ObjectiveTab_", StringComparison.Ordinal) ||
                    name.StartsWith("BattleCard_", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void ResolveLibraryTarget()
        {
            SkirmishBattleCatalogConfig catalog = SkirmishBattleCatalogConfig.Load();
            if (catalog != null && catalog.TryGet(catalogId, out SkirmishBattleCatalogEntry entry))
            {
                if (!string.IsNullOrEmpty(entry.MapId))
                    menuLibraryMap = entry.MapId;
                if (!string.IsNullOrEmpty(entry.ObjectiveId))
                    menuLibraryObjective = entry.ObjectiveId;
            }
        }

        private static Button FindNamed(Button[] buttons, string name)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == name && TryPresentedButtonPoint(buttons[i], out _))
                    return buttons[i];
            }

            return null;
        }

        public static QuickCustomScreenView SelectMenuSetupForLaunch(QuickCustomScreenView[] setups, string requestedId)
        {
            QuickCustomScreenView selected = null;
            foreach (var setup in setups)
            {
                if (setup == null || !setup.IsBaseAssaultSetup || setup.SelectedScenarioId != requestedId) continue;
                if (selected != null) throw new InvalidOperationException("Ambiguous exact selected menu setup.");
                selected = setup;
            }
            return selected;
        }

        private static bool MenuTap(Button b)
        {
            if (!TryPresentedButtonPoint(b, out var p)) return false;
            double now = EditorApplication.timeSinceStartup;
            if (menuStableTarget != b || Vector2.Distance(menuStablePoint, p) > 1f)
            {
                menuStableTarget = b;
                menuStablePoint = p;
                menuStableSince = now;
                return false;
            }
            if (now - menuStableSince < .5d) return false;
            bool accepted = menuTouch.TryGesture(p, p, .15f, 0f, Time.unscaledTime);
            if (accepted)
            {
                menuLastTapAt = now;
                menuDue = now + 1d;
                menuStableTarget = null;
                Debug.Log("[S004MenuJourney] phase=Touch step=" + menuStep + " button=" + b.name +
                    " point=" + p + " stableBeforeTouch=1 transitionAcceptance=Pending");
            }
            return accepted;
        }

        private static void ObservePresentedMenuControls(Button[] buttons, bool allowScreenshot = true)
        {
            if (allowScreenshot && !menuDiagnosticCaptured)
            {
                ScreenCapture.CaptureScreenshot(LiveTracePath() + ".menu-selection-diagnostic.png");
                menuDiagnosticCaptured = true;
            }
            foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude))
                Debug.Log("[S005MenuObservation] setup=" + setup.name + " baseAssault=" + setup.IsBaseAssaultSetup +
                    " selected=" + setup.SelectedScenarioId + " readOnly=1");
            foreach (var scroll in UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude))
                Debug.Log("[S005MenuObservation] scroll=" + scroll.name + " viewport=" +
                    (scroll.viewport != null ? scroll.viewport.rect.ToString() : "None") + " content=" +
                    (scroll.content != null ? scroll.content.rect.ToString() : "None") +
                    " normalized=" + scroll.normalizedPosition + " readOnly=1");
            foreach (var button in buttons)
            {
                if (button.name != "ScenarioCard_" + catalogId && button.name != "LaunchMissionButton") continue;
                var rect = (RectTransform)button.transform;
                var canvas = button.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(
                        new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = point }, hits);
                Debug.Log("[S005MenuObservation] button=" + button.name + " active=" + button.IsActive() +
                    " interactable=" + button.IsInteractable() + " point=" + point + " safe=" + Screen.safeArea.Contains(point) +
                    " presented=" + TryPresentedButtonPoint(button, out _) + " topHit=" +
                    (hits.Count > 0 ? hits[0].gameObject.name : "None") + " readOnly=1");
            }
        }

        private static void StopS004MenuProbe()
        {
            menuTouch?.Dispose();
            menuTouch = null;
        }
    }

    public sealed class SkirmishMenuLaunchPreparation
    {
        public enum Action { SelectCard, PrepareSetup, TouchLaunch }
        private object preparedInstance;
        private int preparedSeed;
        public Action Read(object selectedSetup, int seed) => selectedSetup == null ? Action.SelectCard :
            ReferenceEquals(preparedInstance, selectedSetup) && preparedSeed == seed ? Action.TouchLaunch : Action.PrepareSetup;
        public void MarkPrepared(object actualSetup, int seed)
        {
            if (actualSetup == null) throw new ArgumentNullException(nameof(actualSetup));
            preparedInstance = actualSetup; preparedSeed = seed;
        }
    }

    public sealed class SkirmishS004MenuInputFrame : MonoBehaviour
    {
        private void Update() => SkirmishS002AriaRunHarness.TickS004MenuPlayerFrame();
    }
}
#endif
