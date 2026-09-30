using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class GuidanceGroundMarkerValidation
    {
        public static void Run()
        {
            object helper = null;
            GameObject cameraObject = null;
            Type type = null;
            bool passed = false;
            try
            {
                type = typeof(Game.UI.Runtime.UiShellRuntimeGateway).Assembly.GetType("Game.UI.Runtime.AssistantHighlightPresentationSystemHelper", true);
                helper = Activator.CreateInstance(type, true);
                cameraObject = new GameObject("GuidanceMarkerValidationCamera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 10;
                camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
                type.GetField("_worldCamera", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(helper, camera);
                Invoke(type, helper, "ShowTutorialWorld", Vector3.zero);
                var root = (GameObject)type.GetField("_worldRingRoot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
                var visible = root.GetComponentsInChildren<LineRenderer>().Where(x => x.enabled).ToArray();
                Require(visible.Length == 5 && visible.All(x => !x.loop), "Tap cue must have four open corners and one dash, without any circular loop.");
                Require(visible.Count(x => x.positionCount == 3) == 4 && visible.Count(x => x.positionCount == 2) == 1, "Unexpected ground cue geometry.");
                object[] observation = { default(Vector2), 0, false };
                Require((bool)Invoke(type, helper, "TryObserveVisibleGuide", observation) && (bool)observation[2], "ARIA cannot observe the visible tap cue.");
                Invoke(type, helper, "ShowTutorialArea", Vector3.zero, 4f, false);
                observation = new object[] { default(Vector2), 0, false };
                Require(!(bool)Invoke(type, helper, "TryObserveVisibleGuide", observation), "Defend/wait area must never issue a tap instruction.");
                Require(root.GetComponentsInChildren<LineRenderer>().Count(x => x.enabled) == 4, "Area cue must retain only its open corners.");
                Debug.Log("[GuidanceGroundMarker] result=Passed tap=4corners+dash area=4corners rings=0 crosshairs=0 ARIA=semantic-observation");
                foreach (string test in new[] { "VehicleVisualAdornmentsSystemTests", "BuildingSelectionMarkerPresentationSystemHelperTests" })
                {
                    var suite = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(test)).FirstOrDefault(t => t != null);
                    Require(suite != null, "Focused test suite unavailable: " + test);
                    suite.GetMethod("RunFocusedValidation").Invoke(null, null);
                }
                passed = true;
                Debug.Log("[GroundMarkerValidation] result=Passed");
            }
            catch (Exception e) { Debug.LogException(e); Debug.Log("[GroundMarkerValidation] result=Failed " + e.GetBaseException().Message); }
            finally
            {
                if (helper != null) Invoke(type, helper, "Unbind");
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                MissionEditorValidationExit.Complete(passed);
            }
        }
        private static object Invoke(Type type, object target, string method, params object[] args) => type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
