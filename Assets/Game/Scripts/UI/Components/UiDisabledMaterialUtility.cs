using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [Flags]
    internal enum UiDisabledVisualReason : byte
    {
        None = 0,
        MissionRestriction = 1 << 0,
        CinematicInteractionLock = 1 << 1
    }

    [DisallowMultipleComponent]
    internal sealed class UiDisabledMaterialState : MonoBehaviour
    {
        [NonSerialized] public Material OriginalMaterial;
        [NonSerialized] public UiDisabledVisualReason Reasons;
    }

    [DisallowMultipleComponent]
    internal sealed class UiDisabledSelectableVisualState : MonoBehaviour
    {
        [NonSerialized] public ColorBlock OriginalColors;
        [NonSerialized] public UiDisabledVisualReason Reasons;
    }

    internal static class UiDisabledMaterialUtility
    {
        private const string ShaderName = "Warline/UI/Disabled Grayscale";
        private static Material _disabledMaterial;

        internal static Material DisabledMaterial
        {
            get
            {
                if (_disabledMaterial != null)
                    return _disabledMaterial;

                Shader shader = Shader.Find(ShaderName);
                if (shader == null)
                    return null;

                _disabledMaterial = new Material(shader)
                {
                    name = "Warline UI Disabled Grayscale (Runtime)",
                    hideFlags = HideFlags.HideAndDontSave
                };
                return _disabledMaterial;
            }
        }

        internal static void SetDisabled(
            GameObject root,
            UiDisabledVisualReason reason,
            bool disabled)
        {
            if (root == null || reason == UiDisabledVisualReason.None)
                return;

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
                SetDisabled(images[index], reason, disabled);
        }

        internal static void SetDisabled(
            Image image,
            UiDisabledVisualReason reason,
            bool disabled)
        {
            if (image == null || reason == UiDisabledVisualReason.None)
                return;

            UiDisabledMaterialState state = image.GetComponent<UiDisabledMaterialState>();
            if (state == null)
            {
                if (!disabled)
                    return;
                state = image.gameObject.AddComponent<UiDisabledMaterialState>();
            }

            if (disabled)
            {
                if (state.Reasons == UiDisabledVisualReason.None)
                    state.OriginalMaterial = image.material;
                state.Reasons |= reason;
                Material material = DisabledMaterial;
                if (material != null)
                    image.material = material;
                return;
            }

            state.Reasons &= ~reason;
            if (state.Reasons != UiDisabledVisualReason.None)
                return;

            image.material = state.OriginalMaterial;
            state.OriginalMaterial = null;
        }

        internal static void SetSelectableDisabled(
            Selectable selectable,
            UiDisabledVisualReason reason,
            bool disabled)
        {
            if (selectable == null || reason == UiDisabledVisualReason.None)
                return;

            UiDisabledSelectableVisualState state =
                selectable.GetComponent<UiDisabledSelectableVisualState>();
            if (state == null)
            {
                if (!disabled)
                    return;
                state = selectable.gameObject.AddComponent<UiDisabledSelectableVisualState>();
            }

            if (disabled)
            {
                if (state.Reasons == UiDisabledVisualReason.None)
                    state.OriginalColors = selectable.colors;
                state.Reasons |= reason;
                ColorBlock colors = state.OriginalColors;
                // The grayscale material owns the disabled appearance. Preserve the normal
                // tint and alpha so Selectable does not add a second translucent treatment.
                colors.disabledColor = colors.normalColor;
                selectable.colors = colors;
                return;
            }

            state.Reasons &= ~reason;
            if (state.Reasons == UiDisabledVisualReason.None)
                selectable.colors = state.OriginalColors;
        }
    }
}
