#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool codedSizeCameraActive, codedSizeCameraFinishing;
        private static SkirmishResolvedSetup codedSizeExpected;
        private static string codedSizeLocale, codedSizeDirectory;
        private static int codedSizeTapFrame=-1, codedSizeCaptureFrame=-1;
        private static string codedSizePng;
        private static bool codedSizeEnvironmentCaptured;
        private static string codedSizePreviousSaveRoot, codedSizePreviousBackground;

        private static void RestoreCodedSizeEnvironment()
        {
            if(!codedSizeEnvironmentCaptured)return;
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",codedSizePreviousSaveRoot);
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",codedSizePreviousBackground);
            codedSizeEnvironmentCaptured=false;
        }

        // Separate observational entry. Never creates a production job/claim.
        public static void RunCodedBaSizeCameraObservation()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || productionCanary!=null || codedSizeCameraActive)
                throw new InvalidOperationException("Idle exclusive camera observation required.");
            string id=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_CATALOG");
            string text=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_SIZE");
            if(!new[]{"S002","S003","S004"}.Contains(id) ||
                !Enum.TryParse(text,false,out SkirmishSizeId size) ||
                !new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar}.Contains(size))
                throw new InvalidOperationException("Exact coded catalog and canonical size required.");
            int chosenSeed=id=="S002"?104731:id=="S003"?104732:155925;
            codedSizeLocale=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_LOCALE")??"en";
            if(codedSizeLocale!="en"&&codedSizeLocale!="fa-IR")throw new InvalidOperationException("Supported locale required.");
            var catalog=SkirmishExpansionCatalogConfig.Load();
            if(catalog==null||!catalog.TryGetDefinition(id,out var definition)||!AssetDatabase.Contains(definition)||
                EditorUtility.IsDirty(definition)||!SkirmishSetupBriefing.TryLoad(id,chosenSeed,size,out codedSizeExpected))
                throw new InvalidOperationException("Exact clean existing coded definition/compiler required.");
            codedSizeDirectory=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_NATIVE_DIRECTORY");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            if(string.IsNullOrEmpty(codedSizeDirectory)||!Path.IsPathRooted(codedSizeDirectory)||
                !Path.GetFullPath(codedSizeDirectory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||
                Directory.Exists(codedSizeDirectory)||File.Exists(codedSizeDirectory))
                throw new InvalidOperationException("Fresh owned camera output directory required.");
            Directory.CreateDirectory(codedSizeDirectory);
            codedSizeTapFrame=codedSizeCaptureFrame=-1;
            codedSizePng=Path.Combine(codedSizeDirectory,"normal-opening.png");
            codedSizePreviousSaveRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            codedSizePreviousBackground=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            codedSizeEnvironmentCaptured=true;
            codedSizeCameraFinishing=false;
            codedSizeCameraActive=true;
            try { RunEditorJourney(codedSizeLocale,false,false,id,chosenSeed); }
            catch(Exception error) { FinishCodedSizeCamera(false,"LaunchException:"+error);throw; }
        }

        // Uses only the actual selected control and ordinary compiled briefing.
        private static bool CodedSizeMenuReady(QuickCustomScreenView setup)
        {
            var size=codedSizeExpected.SizeId;
            var expected=size==SkirmishSizeId.War?UiSkirmishMissionSize.War:
                size==SkirmishSizeId.LargeWar?UiSkirmishMissionSize.LargeWar:UiSkirmishMissionSize.Standard;
            var control=setup.MissionSizeControl;
            if(control==null||!control.gameObject.activeInHierarchy||control.SegmentButtons==null||control.SegmentButtons.Length!=3)
                throw new InvalidOperationException("Visible normal army-size control absent.");
            if(setup.SelectedMissionSize!=expected)
            {
                int index=size==SkirmishSizeId.War?1:size==SkirmishSizeId.LargeWar?2:0;
                if(MenuTap(control.SegmentButtons[index])) codedSizeTapFrame=Time.frameCount;
                return false;
            }
            if(codedSizeTapFrame>=0 && Time.frameCount<=codedSizeTapFrame+1)return false;
            var presented=setup.PresentedMissionSizeSetup;
            if(presented==null||presented.CatalogId!=codedSizeExpected.CatalogId||
                presented.DefinitionId!=codedSizeExpected.DefinitionId||presented.ContentVersion!=codedSizeExpected.ContentVersion||
                presented.SizeId!=size||presented.DifficultyId!=SkirmishDifficultyId.Regular||
                presented.Seed!=codedSizeExpected.Seed||presented.SetupHash!=codedSizeExpected.SetupHash)
                throw new InvalidOperationException("Presented selected-size briefing differs from compiled canonical identity.");
            return true;
        }

        private static void ValidateCodedSizeNativeIdentity(EntityManager em,Entity session)
        {
            if(!em.HasComponent<SkirmishResolvedSetupRecord>(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                throw new InvalidOperationException("Actual expanded setup missing.");
            var actual=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(actual==null||actual.CatalogId!=codedSizeExpected.CatalogId||actual.DefinitionId!=codedSizeExpected.DefinitionId||
                actual.ContentVersion!=codedSizeExpected.ContentVersion||actual.SizeId!=codedSizeExpected.SizeId||
                actual.DifficultyId!=SkirmishDifficultyId.Regular||actual.Seed!=codedSizeExpected.Seed||
                actual.SetupHash!=codedSizeExpected.SetupHash||
                SkirmishAcceptanceCensusCapture.ComputeConfigHash(actual)!=SkirmishAcceptanceCensusCapture.ComputeConfigHash(codedSizeExpected)||
                native.IsLegacy!=0||native.CatalogId.ToString()!=actual.CatalogId||native.DefinitionId.ToString()!=actual.DefinitionId||
                native.ContentVersion!=actual.ContentVersion||native.SizeId!=actual.SizeId||native.DifficultyId!=actual.DifficultyId||
                native.Seed!=actual.Seed||native.SetupHash!=actual.SetupHash)
                throw new InvalidOperationException("Actual native selected-size identity mismatch.");
        }

        private static void TickCodedSizeCamera(EntityManager em,Entity session,in SkirmishMatchState match,bool active,float elapsed)
        {
            ValidateCodedSizeNativeIdentity(em,session);
            if(match.Phase!=SkirmishPhase.Playing||!active||elapsed<=0)return;
            if(codedSizeCaptureFrame<0)
            {
                SkirmishGroundSourceFitSurvey.CaptureCodedBaCameraObservation(em,session,in match,active,elapsed);
                int censusReceipts=Directory.GetFiles(codedSizeDirectory,"*.json").Length;
                if(!CodedSizeCensusReceiptReady(censusReceipts))return;
                ScreenCapture.CaptureScreenshot(codedSizePng);
                codedSizeCaptureFrame=Time.frameCount;
                return;
            }
            if(Time.frameCount<=codedSizeCaptureFrame+2)return;
            if(!File.Exists(codedSizePng)||new FileInfo(codedSizePng).Length==0)return;
            FinishCodedSizeCamera(true,"ObservedNormalOpeningOnly");
        }

        private static bool CodedSizeCensusReceiptReady(int count)
        {
            if(count<0||count>1)throw new InvalidOperationException("Duplicate/invalid passive source/camera receipts refused.");
            return count==1;
        }

        // Uses the actual admission/restoration helpers without scheduling Play/quit or touching the ledger.
        public static int RunCodedSizeReceiptControls()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||codedSizeCameraActive||productionCanary!=null)
                throw new InvalidOperationException("Idle receipt controls required.");
            int count=0;
            void Require(bool value,string label)
            {if(!value)throw new InvalidOperationException("Coded size control failed: "+label);count++;}
            Require(!CodedSizeCensusReceiptReady(0),"zero receipts must poll");
            Require(CodedSizeCensusReceiptReady(1),"one receipt permits screenshot");
            bool rejected=false;try{CodedSizeCensusReceiptReady(2);}catch(InvalidOperationException){rejected=true;}
            Require(rejected,"duplicate receipts refused");
            string save=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            string background=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            string priorSave=codedSizePreviousSaveRoot,priorBackground=codedSizePreviousBackground;
            bool captured=codedSizeEnvironmentCaptured;
            try
            {
                codedSizePreviousSaveRoot=save;codedSizePreviousBackground=background;codedSizeEnvironmentCaptured=true;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT","size-control-temporary");
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","size-control-temporary");
                RestoreCodedSizeEnvironment();
                Require(Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT")==save&&
                    Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION")==background,"exact previous environment restored");
                RestoreCodedSizeEnvironment();
                Require(!codedSizeEnvironmentCaptured&&Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT")==save&&
                    Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION")==background,"restoration idempotent");
                codedSizeCameraActive=true;codedSizeCameraFinishing=true;
                CompleteCodedSizeCameraAfterOwnedPlay();
                Require(!codedSizeCameraActive&&!codedSizeCameraFinishing,"owned post-Play cleanup releases finishing latch");
            }
            finally
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",save);
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",background);
                codedSizePreviousSaveRoot=priorSave;codedSizePreviousBackground=priorBackground;
                codedSizeEnvironmentCaptured=captured;codedSizeCameraActive=false;codedSizeCameraFinishing=false;
            }
            return count;
        }

        private static void CompleteCodedSizeCameraAfterOwnedPlay()
        {
            if(!codedSizeCameraActive || !codedSizeCameraFinishing)return;
            RestoreCodedSizeEnvironment();
            codedSizeCameraActive=false;codedSizeCameraFinishing=false;
        }

        private static void FinishCodedSizeCamera(bool observed,string reason)
        {
            if(!codedSizeCameraActive || codedSizeCameraFinishing)return;
            // Ownership remains latched until the shared post-Play cleanup.
            // An artifact exception must never fall through to the normal ledger.
            codedSizeCameraFinishing=true;
            bool receiptWritten=false;
            try
            {
                EditorApplication.update-=Tick;
                StopS004MenuProbe();RestoreBackgroundInput();
                RestoreCodedSizeEnvironment();
                SessionState.SetBool(Key,false);SessionState.SetBool(NormalKey,false);
                var row=new JObject{["scope"]="NormalMenuSizeCameraObservation_NotQualification",
                    ["catalogId"]=codedSizeExpected.CatalogId,["size"]=codedSizeExpected.SizeId.ToString(),
                    ["definitionId"]=codedSizeExpected.DefinitionId,["definitionVersion"]=codedSizeExpected.ContentVersion,
                    ["seed"]=codedSizeExpected.Seed,["setupHash"]=codedSizeExpected.SetupHash,
                    ["nativeConfigSha256"]=SkirmishAcceptanceCensusCapture.ComputeConfigHash(codedSizeExpected),
                    ["normalOpeningObserved"]=observed,["reason"]=reason,["cameraAccepted"]=false,
                    ["normalMissionReady"]=false,["qualifiedProductionJobs"]=0,["png"]=codedSizePng};
                using(var stream=new FileStream(Path.Combine(codedSizeDirectory,"journey.json"),FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
                {writer.Write(row.ToString());writer.Flush();stream.Flush(true);}
                receiptWritten=true;
                Debug.Log("[CodedBaSizeCameraJourney] result="+(observed?"Observed":"Failed")+
                    " catalog="+codedSizeExpected.CatalogId+" size="+codedSizeExpected.SizeId+
                    " normalInput=1 cameraAccepted=0 normalMissionReady=0 qualifiedProductionJobs=0");
            }
            finally
            {
                try { RestoreCodedSizeEnvironment(); }
                finally
                {
                    SessionState.SetBool(QuitPendingKey,true);
                    SessionState.SetInt(QuitCodeKey,observed&&receiptWritten?0:2);
                    EditorApplication.update-=FinishQuitAfterPlayMode;EditorApplication.update+=FinishQuitAfterPlayMode;
                    if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();
                }
            }
        }
    }
}
#endif
