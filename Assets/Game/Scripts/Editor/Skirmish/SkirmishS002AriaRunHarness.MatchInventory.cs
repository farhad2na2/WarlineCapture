#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// One diagnostic line per match, at the first Playing frame: what is loaded and running. Used to compare a direct launch
    /// with a launch through the menu on the same code (does the menu leave anything running during the match?). Read-only.
    /// [MatchSceneInventory] scenes=A,B canvases=N cameras=N audioListeners=N eventSystems=N menuRoots=... graphics=N
    /// behaviours=N rootsByScene=Name:activeRoots/roots,... dontDestroyRoots=N:[...]
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private const string MatchInventoryTag = "[MatchSceneInventory]";
        private const int InventoryNameCap = 24;

        private static void LogMatchSceneInventory()
        {
            try { Debug.Log(BuildMatchSceneInventory()); }
            catch (Exception error) { Debug.LogWarning(MatchInventoryTag + " result=Failed error=" + error.Message); }
        }

        private static string BuildMatchSceneInventory()
        {
            var names = new List<string>();
            var roots = new StringBuilder();
            var menuRoots = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                string name = string.IsNullOrEmpty(scene.name) ? "(unnamed)" : scene.name;
                names.Add(name);
                GameObject[] sceneRoots = scene.GetRootGameObjects();
                int active = 0;
                bool isMenuScene = name.IndexOf("menu", StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (GameObject root in sceneRoots)
                    if (root.activeInHierarchy)
                    {
                        active++;
                        if (isMenuScene && menuRoots.Count < InventoryNameCap) menuRoots.Add(root.name);
                    }
                if (roots.Length > 0) roots.Append(',');
                roots.Append(name).Append(':').Append(active).Append('/').Append(sceneRoots.Length);
            }

            // Objects moved to DontDestroyOnLoad (a runtime-built shell/menu lives here, not in a loaded scene).
            var probe = new GameObject("MatchSceneInventoryProbe");
            UnityEngine.Object.DontDestroyOnLoad(probe);
            GameObject[] ddol = probe.scene.GetRootGameObjects();
            var ddolActive = new List<string>();
            foreach (GameObject root in ddol)
                if (root != probe && root.activeInHierarchy && ddolActive.Count < InventoryNameCap) ddolActive.Add(root.name);
            int ddolActiveCount = 0;
            foreach (GameObject root in ddol) if (root != probe && root.activeInHierarchy) ddolActiveCount++;
            UnityEngine.Object.Destroy(probe);

            int canvases = CountEnabled(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            int cameras = CountEnabled(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            int listeners = CountEnabled(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            int eventSystems = CountEnabled(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            int graphics = CountEnabled(UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            int behaviours = CountEnabled(UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

            return MatchInventoryTag + " catalog=" + catalogId + " scenes=" + string.Join(",", names) +
                " canvases=" + canvases + " cameras=" + cameras + " audioListeners=" + listeners +
                " eventSystems=" + eventSystems +
                " menuRoots=" + (menuRoots.Count == 0 ? "none" : menuRoots.Count + ":[" + string.Join(",", menuRoots) + "]") +
                " graphics=" + graphics + " behaviours=" + behaviours + " rootsByScene=" + roots +
                " dontDestroyRoots=" + ddolActiveCount + ":[" + string.Join(",", ddolActive) + "]" +
                " menuJourney=" + (GenericMenuJourney ? 1 : 0);
        }

        private static int CountEnabled<T>(T[] objects) where T : Behaviour
        {
            int count = 0;
            foreach (T item in objects) if (item != null && item.isActiveAndEnabled) count++;
            return count;
        }
    }
}
#endif
