#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool sagaMenuProof;
        private static bool sagaScaleRecorded;
        private static bool CaptureNativeSagaMenuScale(QuickCustomScreenView setup)
        {
            if(setup.ApprovedAtlas!=null)return CaptureApprovedAtlasScale(setup);
            foreach(var text in setup.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if(text.name=="SeedHelp")
                {
                    text.ForceMeshUpdate();
                    float available=text.rectTransform.rect.height-text.margin.y-text.margin.w;
                    if(text.renderedHeight>available+1f)
                    {
                        ScreenCapture.CaptureScreenshot(LiveTracePath()+".briefing-failed.png");
                        Debug.LogError("[SagaNativeBriefingReadability] result=Failed font="+text.fontSize+" rendered="+text.renderedHeight+" available="+available+" root="+setup.name);
                        Finish(true,"nativeBriefingOverflow");
                        return false;
                    }
                    if(!sagaScaleRecorded) Debug.Log("[SagaNativeBriefingReadability] result=Passed font="+text.fontSize+" rendered="+text.renderedHeight+" available="+available);
                }
            if (sagaScaleRecorded) return true;
            var corners = new Vector3[4];
            float Height(RectTransform rect)
            {
                var canvas = rect.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                rect.GetWorldCorners(corners);
                return Mathf.Abs(RectTransformUtility.WorldToScreenPoint(camera,corners[1]).y - RectTransformUtility.WorldToScreenPoint(camera,corners[0]).y);
            }
            float footer = float.MaxValue, stop = 0;
            int count = 0;
            foreach(var button in setup.GetComponentsInChildren<UnityEngine.UI.Button>(false))
            {
                if(button.name is "BackButton" or "RandomizeSeedButton" or "LaunchMissionButton" or "LibraryAdvancedButton")
                { footer = Mathf.Min(footer,Height((RectTransform)button.transform)); count++; }
                if(button.name == SkirmishLibraryControlNames.BattleCard(catalogId)) stop = Height((RectTransform)button.transform);
            }
            if(count != 4 || footer / Screen.height < .083f || stop / Screen.height < .065f)
            {
                Debug.LogError("[SagaNativeMenuScale] result=Failed footer="+footer+" stop="+stop+" screenHeight="+Screen.height+" controls="+count);
                Finish(true,"nativeMenuScale");
                return false;
            }
            string prefix=LiveTracePath()+".menu-scale";
            ScreenCapture.CaptureScreenshot(prefix+".png");
            File.WriteAllText(prefix+".json",new JObject { ["screenWidth"]=Screen.width,["screenHeight"]=Screen.height,["footerHeightPixels"]=footer,["stopDiameterPixels"]=stop,["footerScreenHeightRatio"]=footer/Screen.height,["stopScreenHeightRatio"]=stop/Screen.height,["actualPlayMode"]=true,["selectedScenario"]=catalogId }.ToString());
            Debug.Log("[SagaNativeMenuScale] result=Passed footer="+footer+" stop="+stop+" screen="+Screen.width+"x"+Screen.height+" nativePlayMode=1");
            sagaScaleRecorded=true;
            return true;
        }

        private static bool CaptureApprovedAtlasScale(QuickCustomScreenView setup)
        {
            if(sagaScaleRecorded)return true;
            var atlas=setup.ApprovedAtlas;var canvas=atlas.GetComponentInParent<Canvas>().rootCanvas;var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            float Height(RectTransform r){var corners=new Vector3[4];r.GetWorldCorners(corners);return Mathf.Abs(RectTransformUtility.WorldToScreenPoint(camera,corners[1]).y-RectTransformUtility.WorldToScreenPoint(camera,corners[0]).y);}
            float command=Mathf.Min(Height(atlas.BackButton.transform as RectTransform),Height(atlas.PrimaryButton.transform as RectTransform));
            float card=0;foreach(var region in atlas.Regions)foreach(var item in region.Cards)if(item.Rect.name==SkirmishLibraryControlNames.BattleCard(catalogId))card=Height(item.Rect);
            if(command/Screen.height<.08f || card/Screen.height<.12f)throw new InvalidOperationException("Native atlas targets are too small");
            string prefix=LiveTracePath()+".menu-scale";ScreenCapture.CaptureScreenshot(prefix+".png");
            File.WriteAllText(prefix+".json",new JObject{["presentation"]="approved-map-cards",["actualPlayMode"]=true,["selectedScenario"]=catalogId,["commandHeightPixels"]=command,["missionCardHeightPixels"]=card,["screenWidth"]=Screen.width,["screenHeight"]=Screen.height}.ToString());
            sagaScaleRecorded=true;Debug.Log("[ApprovedAtlasNativeScale] result=Passed command="+command+" card="+card+" input=Touch");return true;
        }

        private static SkirmishDifficultyId terminalSagaDifficulty;
        private static bool ObserveSagaResultMedal(EntityManager em, Entity session, in SkirmishMatchState match)
        {
            if (match.Outcome != SkirmishOutcome.Victory)
                throw new InvalidOperationException("Saga medal journey requires an actual ARIA victory; observed " + match.Outcome);
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                throw new InvalidOperationException("Actual expanded session required");
            terminalSagaDifficulty = em.GetComponentData<SkirmishExpandedSessionComponent>(session).DifficultyId;
            return UiShellRuntimeGateway.TryReadSkirmish(out var result) && result.MedalScenarioId == catalogId && result.MedalDifficulty == (byte)terminalSagaDifficulty && result.MedalIsFirstAward;
        }

        private static void VerifySagaReturnedMedal(string prefix)
        {
            var catalog = SkirmishBattleCatalogConfig.Load();
            var reloaded = SaveService.CreateDefault().LoadProfile();
            var ledger = new SkirmishMedalLedger(reloaded.skirmishMedals, catalog.Entries);
            if (!ledger.Has(catalogId, terminalSagaDifficulty))
                throw new InvalidOperationException("Settled medal missing from a fresh disk reload");
            QuickCustomScreenView selected = null;
            foreach (var view in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude))
                if (view.IsBaseAssaultSetup && view.SelectedScenarioId == catalogId)
                {
                    selected = view;
                    break;
                }

            if (selected == null || selected.Library.Level != SkirmishLibraryLevel.Battlefield)
                throw new InvalidOperationException("Return did not restore the native battlefield and selected battle");
            var mask = terminalSagaDifficulty switch
            {
                SkirmishDifficultyId.Recruit => SkirmishMedalMask.Recruit,
                SkirmishDifficultyId.Regular => SkirmishMedalMask.Regular,
                SkirmishDifficultyId.Veteran => SkirmishMedalMask.Veteran,
                _ => SkirmishMedalMask.Commander
            };
            if ((selected.Library.Briefing.Medals & mask) == 0)
                throw new InvalidOperationException("Returned briefing did not refresh its earned medal");
            bool ribbon = false;
            foreach (var node in selected.GetComponentsInChildren<Transform>(false))
                if (node.name.StartsWith("EarnedRibbon_", StringComparison.Ordinal) && node.parent.name == SkirmishLibraryControlNames.BattleCard(catalogId))
                    ribbon = true;
            bool atlasCardMedal=false;
            if(selected.ApprovedAtlas!=null)
            {
                foreach(var region in selected.ApprovedAtlas.Regions)foreach(var card in region.Cards)
                    if(card.Rect.name==SkirmishLibraryControlNames.BattleCard(catalogId))
                    {
                        atlasCardMedal=card.HasEarnedMedalVisual(terminalSagaDifficulty);
                    }
                ribbon=atlasCardMedal;
            }
            if (!ribbon || selected.Library.Theater.OverallWonCount < 1)
                throw new InvalidOperationException("Returned saga stop/theatre totals did not show the earned medal");
            File.WriteAllText(prefix + "-saga-medal-proof.json", new JObject { ["candidateSourceHash"] = UnityEditor.SessionState.GetString(CandidateHashKey, ""), ["producerPid"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["locale"] = locale, ["scenario"] = catalogId, ["difficulty"] = terminalSagaDifficulty.ToString(), ["outcome"] = "Victory", ["resultFirstMedalShown"] = true, ["returnedBattlefield"] = selected.Library.Battlefield.MapId, ["returnedScenario"] = selected.SelectedScenarioId, ["nativeStopRibbonVisible"] = selected.ApprovedAtlas==null, ["nativeMissionCardMedalVisible"] = atlasCardMedal, ["presentation"] = selected.ApprovedAtlas!=null?"approved-map-cards":"saga", ["nativeBriefingMedalRefreshed"] = true, ["diskReloadHasMedal"] = true, ["profileRoot"] = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"), ["processRestartAcceptance"] = "Pending" }.ToString());
            Debug.Log("[SagaNativeMedalJourney] result=Passed nativeResult=1 returnSelection=1 earnedCardMedal=1 diskReload=1 processRestart=Pending");
        }
    }
}
#endif

