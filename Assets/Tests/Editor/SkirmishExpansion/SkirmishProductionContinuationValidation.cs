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
            VerifySource("S005NormalNorthFlank1632Observer.cs", "5dda43e1f4a9b6c2aad6d7b2d5b0e6659aeb5541851113da8153fea02ba90fd7");
            VerifySource("SkirmishS002AriaRunHarness.NormalPaidPlaneLineageCanary.cs", "03da52225f3d16ba66b457e8695299ab8d92327b615ecc6fba372005190c776e");
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
