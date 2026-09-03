using Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Covers UI created at runtime (match warnings, build panels, tutorial prompts, and similar
    /// transient views). Authored prefabs keep explicit bindings; this scanner only attaches a
    /// binding when the displayed English source already exists in the shared catalog.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class V3LocalizationRuntimeBinder : MonoBehaviour
    {
        private const float ScanIntervalSeconds = 0.5f;
        private float nextScanAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<V3LocalizationRuntimeBinder>() != null)
                return;

            GameObject host = new("V3LocalizationRuntimeBinder");
            host.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(host);
            host.AddComponent<V3LocalizationRuntimeBinder>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameLocalization.LocaleChanged += ScanNow;
            ScanNow();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameLocalization.LocaleChanged -= ScanNow;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanAt)
                return;
            ScanNow();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ScanNow();

        private void ScanNow()
        {
            nextScanAt = Time.unscaledTime + ScanIntervalSeconds;
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.scene.IsValid() ||
                    text.GetComponent<V3LocalizedTextBinding>() != null)
                {
                    continue;
                }

                string source = text is RTLTMPro.RTLTextMeshPro rtl
                    ? rtl.OriginalText
                    : text.text;
                if (!GameLocalization.TryGetBySource(source, out string key, out _))
                    continue;

                V3LocalizedTextBinding binding =
                    text.gameObject.AddComponent<V3LocalizedTextBinding>();
                binding.Configure(key, source, observeRuntimeChanges: true);
            }
        }
    }
}
