#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Resume after successful metadata generation. Never creates or refreshes assets.
    public static class SkirmishInstalledAircraftSharedPreparation
    {
        static void VerifyInstalledMetadata()
        {
            string report=Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_ROOT_PROVENANCE_REPORT");
            string expected=Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_ROOT_PROVENANCE_SHA256");
            if(string.IsNullOrEmpty(report)||!File.Exists(report)||string.IsNullOrEmpty(expected)||expected.Length!=64)
                throw new InvalidOperationException("Explicit fresh actual seven-role report/SHA required");
            string actual;using(var stream=File.OpenRead(report))using(var hash=SHA256.Create())actual=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
            var rotor=AssetDatabase.LoadAssetAtPath<SkirmishAircraftRotorGeometryConfig>(SkirmishAircraftRotorGeometryAuthoring.Output);
            if(actual!=expected||rotor==null||rotor.NativeTemplateProvenanceSha256!=actual)
                throw new InvalidOperationException("Installed rotor metadata does not join the explicit fresh native provenance");
            SkirmishRigidAircraftGeometryAuthoring.VerifyInstalledTransportMetadata();
            // Includes the unchanged canonical six-role current-source verifier.
            SkirmishAircraftRotorGeometryAuthoring.ValidateInstalled();
            Debug.Log("[InstalledAircraftMetadataVerification] result=Passed transportSources=3 transportDraws=57 additionalRoles=6 rotorRoles=4 freshNativeProvenance=1 metadataGenerated=0 cameraAcceptance=Pending");
        }
        public static void Run()
        {
            var failures=new List<Exception>();
            try{VerifyInstalledMetadata();}
            catch(Exception error){Debug.LogError("[SkirmishInstalledAircraftSharedPreparation] suiteFailed=installedMetadata");Debug.LogException(error);failures.Add(new InvalidOperationException("Installed metadata verification failed",error));}
            try{SkirmishMeasuredRotorAircraftFramingTests.RunFocusedValidation();}
            catch(Exception error){Debug.LogError("[SkirmishInstalledAircraftSharedPreparation] suiteFailed=framing55");Debug.LogException(error);failures.Add(new InvalidOperationException("Measured aircraft framing controls failed",error));}
            try{SkirmishTerminalPresentationObservationTests.RunFocusedValidation();}
            catch(Exception error){Debug.LogError("[SkirmishInstalledAircraftSharedPreparation] suiteFailed=terminal6");Debug.LogException(error);failures.Add(new InvalidOperationException("Terminal presentation controls failed",error));}
            try{SkirmishLogicalRefinementPreparationValidation.Run();}
            catch(Exception error){Debug.LogError("[SkirmishInstalledAircraftSharedPreparation] suiteFailed=logicalRefinement");Debug.LogException(error);failures.Add(new InvalidOperationException("Logical refinement controls failed",error));}
            try{AriaExtendedObjectiveSupportValidation.Run();}
            catch(Exception error){Debug.LogError("[SkirmishInstalledAircraftSharedPreparation] suiteFailed=aria70");Debug.LogException(error);failures.Add(new InvalidOperationException("ARIA objective support controls failed",error));}
            if(failures.Count!=0)throw new AggregateException("Installed shared preparation failed; retain every independent suite failure",failures);
            Debug.Log("[SkirmishSharedNativeProductionPreparation] result=Passed metadataAdditionalRoles=6 rotorRoles=4 framingControls=55 terminalMethods=6 enemy=5 approach=3 view=6 typedSpawn=5 aria=70 cameraAndMissionAcceptance=Pending");
            Debug.Log("[SkirmishInstalledAircraftSharedPreparation] result=Passed sharedControls=150 installedMetadataVerified=1 metadataGenerated=0 geometryChanged=0 cameraAndMissionAcceptance=Pending");
        }
        public static void RunVerifySharedAndAuthoringWarmup()
        {
            Run();
            Game.Editor.SkirmishAll120AuthoringSceneWarmup.Run();
            Debug.Log("[InstalledSharedAndAuthoringWarmup] result=Passed sharedControls=150 sourceGroupsStarted=0 warmupScenes=2 snapshotAfterWrapperExit=1 missionAcceptance=Pending");
        }
    }
}
#endif
