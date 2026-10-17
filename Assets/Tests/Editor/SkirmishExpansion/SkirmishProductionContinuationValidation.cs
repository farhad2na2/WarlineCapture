using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishProductionContinuationValidation
    {
        public static void RunFocusedValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Load-only validation requires stopped Play.");
            VerifySource("S005NormalNorthFlank1632Observer.cs", "2237d7d0a6310e13cdbe0bbf4e331563c42f0945bf4225aba2716ea8b83e6276");
            VerifySource("SkirmishS002AriaRunHarness.NormalPaidPlaneLineageCanary.cs", "8678eac8964c8d48d5e9bb36f8c4a2f40343e375a864e5749be76da36c95bdc5");
            VerifySource("SkirmishS002AriaRunHarness.NormalPaidTransportCruiseExtension.cs", "a248fab5f6385b4f02df073e4da3d2ddcd6b24cc7cc2343d2f53fb99ca0dc656");
            VerifySource("SkirmishS002AriaRunHarness.NormalPaidPlaneFollowTouchSeam.cs", "e97c1036069fb8f87c0d99309f6c7500de61f7a3a6c447d21c9b00f3aaf61fa0");
            VerifySource("SkirmishS002AriaRunHarness.OpeningCameraCalibration.cs", "0ee0682a541b7c0f2a3f1ba38a2f58f6fa5a1eb1f86883883c136bc2871b2ae8");
            VerifySource("SkirmishS002AriaRunHarness.OpeningDriverDiagnostic.cs", "cd17610e3773a38df12b88a19bd0dc824f1c0236b95bc52259cec30448d91a6a");
            VerifySource("S005SceneArtifactWarmup.cs", "89b2e7c67026cec9f8e0df821b2345ff1dde19c38ca720cd1b1421d04825d6fb");
            VerifySource("SkirmishS002AriaRunHarness.cs", "e0f96b44d4faebb2099a045c62d5d214b99944456819d6d62ad27120b5348376");
            VerifySource("SkirmishS002AriaRunHarness.NativeRenderingDiagnostic.cs", "a67a381913142bb4b81291d65401a02e12d6072a03fd9985078bac4cd2fa4b29");
            SkirmishSharedObjectiveReducerValidation.RunFocusedValidation();
            S005NormalNorthFlank1632Observer.Run();
            MethodInfo paidPlane = typeof(SkirmishS002AriaRunHarness).GetMethod(
                "RunS005NormalPaidPlaneLineageCanary", BindingFlags.Public | BindingFlags.Static);
            if (paidPlane == null || paidPlane.GetParameters().Length != 0 || paidPlane.ReturnType != typeof(void))
                throw new InvalidOperationException("Paid-plane canary entry signature differs.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("A load-only check started Play unexpectedly.");
            Debug.Log("[SkirmishProductionContinuationValidation] result=Passed objectiveRules=42 northLoaded=1 paidPlaneLoaded=1 gameplayStarted=0 editorMvid=" + typeof(SkirmishS002AriaRunHarness).Module.ModuleVersionId);
        }

        private static void VerifySource(string filename, string expected)
        {
            string path = Path.Combine(Application.dataPath, "Game/Scripts/Editor/Skirmish", filename);
            using (var hash = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                string actual = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                if (actual != expected) throw new InvalidDataException("Reviewed canary source differs: " + filename);
            }
        }
    }
}
