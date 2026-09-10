using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>Localizes newly rendered dynamic text. Authored text keeps its serialized bindings.</summary>
    [DisallowMultipleComponent]
    internal sealed class V3LocalizationRuntimeBinderView : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Runtime initialization runs once per play session. No global object scan or retained view registry.
            var host = new GameObject("V3LocalizationRuntimeBinderView") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(host);
            host.AddComponent<V3LocalizationRuntimeBinderView>();
        }

        private void OnEnable() => TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        private void OnDisable() => TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);

        private void HandleTextChanged(Object changed)
        {
            if(changed is not TMP_Text text || !text.gameObject.scene.IsValid() ||
               text.GetComponent<V3LocalizedTextBindingView>() != null || IsSpecializedNarrativeText(text)) return;
            string source = text is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : text.text;
            if(!UiShellRuntimeGateway.Localization.TryGetBySource(source, out string key, out _)) return;
            var binding = text.gameObject.AddComponent<V3LocalizedTextBindingView>();
            binding.Configure(key, source, observeRuntimeChanges: true);
        }

        internal static bool IsSpecializedNarrativeText(TMP_Text text) => text != null &&
            (text.GetComponentInParent<AriaTutorialBriefingView>(true)?.OwnsLocalizedText(text) == true ||
             text.GetComponentInParent<FirstLaunchLanguageChoiceView>(true) != null ||
             text.GetComponentInParent<NarrativeSequenceView>(true) != null);
    }
}
