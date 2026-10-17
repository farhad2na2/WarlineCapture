#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    /// <summary>Owns only a temporary validation settings clone, never its source.</summary>
    public sealed class ValidationBackgroundInputScope : IDisposable
    {
        private const string OwnedNamePrefix = "Warline.ValidationBackgroundInput.";
        private const string SettingsConfigKey = "com.unity.input.settings";
        private readonly InputSettings original;
        private readonly InputSettings.BackgroundBehavior originalBackground;
        private readonly InputSettings.EditorInputBehaviorInPlayMode originalEditorBehavior;
        private readonly bool originalRunInBackground;
        private InputSettings owned;
        private bool disposed, released;

        public InputSettings OriginalSettings => original;
        public InputSettings OwnedSettings => owned;
        public bool IsActive => !disposed && owned != null;
        public bool IsReleased => released;

        // The public getter asserts when the manager retains a destroyed Unity object.
        // Read that one installed-package reference; all assignments use the public API.
        public static InputSettings ReadCurrentReference()
        {
            var managerField = typeof(InputSystem).GetField("s_Manager", BindingFlags.Static | BindingFlags.NonPublic);
            if (managerField == null) throw new InvalidOperationException("InputSystem manager reference is unavailable.");
            var manager = managerField.GetValue(null);
            if (manager == null) throw new InvalidOperationException("InputSystem is not initialized.");
            var settingsField = manager.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic);
            if (settingsField == null) throw new InvalidOperationException("InputSystem settings reference is unavailable.");
            return settingsField.GetValue(manager) as InputSettings;
        }

        public static InputSettings ResolveExistingOriginal(InputSettings remembered = null, InputSettings excluded = null)
        {
            if (remembered != null && !ReferenceEquals(remembered, excluded)) return remembered;
            if (EditorBuildSettings.TryGetConfigObject(SettingsConfigKey, out InputSettings configured) &&
                configured != null && !ReferenceEquals(configured, excluded)) return configured;
            InputSettings candidate = null;
            foreach (var existing in Resources.FindObjectsOfTypeAll<InputSettings>())
            {
                if (existing == null || ReferenceEquals(existing, excluded) ||
                    existing.name.StartsWith(OwnedNamePrefix, StringComparison.Ordinal)) continue;
                if (candidate != null)
                    throw new InvalidOperationException("Destroyed InputSettings binding has more than one existing original; refusing to guess.");
                candidate = existing;
            }
            if (candidate == null)
                throw new InvalidOperationException("No live original InputSettings exists; validation will not fabricate defaults.");
            return candidate;
        }

        public ValidationBackgroundInputScope()
        {
            var current = ReadCurrentReference();
            original = current != null ? current : ResolveExistingOriginal();
            if (original.name.StartsWith(OwnedNamePrefix, StringComparison.Ordinal))
                throw new InvalidOperationException("A background validation already owns InputSettings.");
            if (current == null) InputSystem.settings = original;
            originalBackground = original.backgroundBehavior;
            originalEditorBehavior = original.editorInputBehaviorInPlayMode;
            originalRunInBackground = Application.runInBackground;
            owned = UnityEngine.Object.Instantiate(original);
            owned.name = OwnedNamePrefix + Guid.NewGuid().ToString("N");
            // Explicitly keep this object through scene/Play cleanup. DontSave also
            // distinguishes our ownership from the package's HideAndDontSave default.
            owned.hideFlags = HideFlags.DontSave;
            owned.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            owned.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = owned;
            Application.runInBackground = true;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void RestoreOriginalIfOwned()
        {
            var current = ReadCurrentReference();
            // A subsequent owner or a deliberate user settings change wins.
            if (current != null && !ReferenceEquals(current, owned) && !ReferenceEquals(current, original)) return;
            var target = ResolveExistingOriginal(original, owned);
            InputSystem.settings = target;
            target.backgroundBehavior = originalBackground;
            target.editorInputBehaviorInPlayMode = originalEditorBehavior;
        }

        public void Dispose()
        {
            if (released || disposed) return;
            disposed = true;
            try { RestoreOriginalIfOwned(); }
            finally { Application.runInBackground = originalRunInBackground; }
            // A Play transition may still use its saved settings reference. Keep the
            // clone alive until after the package's EnteredEditMode callbacks.
            EditorApplication.delayCall -= CompleteRetirement;
            EditorApplication.delayCall += CompleteRetirement;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            if (!disposed) Dispose();
            EditorApplication.delayCall -= CompleteRetirement;
            EditorApplication.delayCall += CompleteRetirement;
        }

        private void CompleteRetirement() => TryCompleteRetirement();

        public bool TryCompleteRetirement()
        {
            if (released) return true;
            if (!disposed || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return false;
            RestoreOriginalIfOwned();
            var current = ReadCurrentReference();
            if (ReferenceEquals(current, owned)) return false;
            EditorApplication.delayCall -= CompleteRetirement;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (owned != null) UnityEngine.Object.DestroyImmediate(owned);
            owned = null;
            released = true;
            return true;
        }
    }
}
#endif
