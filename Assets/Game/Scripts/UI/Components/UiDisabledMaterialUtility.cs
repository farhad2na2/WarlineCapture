using System;
using TMPro;
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
        [NonSerialized] public Color OriginalColor;
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

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int index = 0; index < graphics.Length; index++)
                SetDisabled(graphics[index], reason, disabled);
        }

        internal static void SetDisabled(
            Image image,
            UiDisabledVisualReason reason,
            bool disabled)
        {
            SetDisabled((Graphic)image, reason, disabled);
        }

        private static void SetDisabled(
            Graphic graphic,
            UiDisabledVisualReason reason,
            bool disabled)
        {
            if (graphic == null || reason == UiDisabledVisualReason.None)
                return;

            // TMP creates child submesh graphics for fallback-font glyphs. Their material is
            // owned by the parent TMP_Text and TMP_SubMeshUI's setter dereferences that owner;
            // restoring a cached null material here throws every frame and prevents the M02 HUD
            // command state from completing. The parent text already receives the readable
            // disabled tint, so generated submeshes must never be material-swapped independently.
            if (graphic is TMP_SubMeshUI)
                return;

            UiDisabledMaterialState state = graphic.GetComponent<UiDisabledMaterialState>();
            if (state == null)
            {
                if (!disabled)
                    return;
                state = graphic.gameObject.AddComponent<UiDisabledMaterialState>();
            }

            if (disabled)
            {
                if (state.Reasons == UiDisabledVisualReason.None)
                {
                    state.OriginalMaterial = graphic.material;
                    state.OriginalColor = graphic.color;
                }
                state.Reasons |= reason;

                // TMP text uses an SDF font material. Replacing it with the regular UI
                // grayscale material makes every glyph disappear, which is especially
                // visible while M01/M02 mission restrictions disable HUD controls.
                // Keep the font material and express the disabled state through a
                // readable neutral tint instead.
                if (graphic is TMP_Text)
                {
                    Color original = state.OriginalColor;
                    float luminance = Mathf.Clamp(original.grayscale * 0.82f, 0.58f, 0.78f);
                    graphic.material = state.OriginalMaterial;
                    graphic.color = new Color(luminance, luminance, luminance, original.a);
                    return;
                }

                Material material = DisabledMaterial;
                if (material != null)
                    graphic.material = material;
                return;
            }

            state.Reasons &= ~reason;
            if (state.Reasons != UiDisabledVisualReason.None)
                return;

            graphic.material = state.OriginalMaterial;
            graphic.color = state.OriginalColor;
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
