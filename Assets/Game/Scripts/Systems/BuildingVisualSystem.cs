using System.Collections.Generic;
using System.Globalization;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class BuildingVisualSystem : SystemBase
    {
        private const string BaseColorProperty = "_BaseColor";
        private const string LegacyColorProperty = "_Color";
        private const string EmissionColorProperty = "_EmissionColor";
        private const string AccentColorProperty = "_AccentColor";
        private const string LegacyOilPumpArmName = "SM_Prop_Pipline_OilPump_Arm_01";
        private const string LegacyOilPumpWheelName = "SM_Prop_Pipline_Wheel_01";
        private const float OilPumpArmAngleLimit = 15f;
        private const float OilPumpWheelAngleLimit = 365f;
        private const float OscillationRadiansPerSecond = 1.5f;
        private const float AnimationAngleEpsilon = 0.05f;

        public sealed class AnimatedPart
        {
            public Transform Transform;
            public Vector3 BaseLocalEulerAngles;
            public Quaternion BaseLocalRotation;
            public Vector3 Axis;
            public float AngleLimit;
            public float PhaseOffset;
            public bool ContinuousRotation;
            public bool IsAtRest = true;
            public float LastAppliedAngle = float.NaN;
        }

        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public void ApplyMarkerColor(Renderer[] renderers, Color color, MaterialPropertyBlock propertyBlock)
        {
            if (renderers == null || propertyBlock == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);
                Material material = renderer.sharedMaterial;
                if (material != null && material.HasProperty(BaseColorProperty))
                    propertyBlock.SetColor(BaseColorProperty, color);
                if (material != null && material.HasProperty(LegacyColorProperty))
                    propertyBlock.SetColor(LegacyColorProperty, color);
                if (material != null && material.HasProperty(EmissionColorProperty))
                    propertyBlock.SetColor(EmissionColorProperty, color);
                if (material != null && material.HasProperty(AccentColorProperty))
                    propertyBlock.SetColor(AccentColorProperty, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        public void SetTransformVisible(Transform target, bool visible)
        {
            if (target == null)
                return;

            if (target.gameObject.activeSelf == visible)
                return;

            target.gameObject.SetActive(visible);
        }

        public Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
                return null;
            if (root.name == targetName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendantByName(root.GetChild(i), targetName);
                if (found != null)
                    return found;
            }

            return null;
        }

        public AnimatedPart[] FindAnimatedBuildingParts(Transform root)
        {
            if (root == null)
                return null;

            List<AnimatedPart> matches = null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!TryParseAnimatedPartName(child.name, out Vector3 axis, out float angleLimit))
                    continue;

                AddAnimatedPart(ref matches, child, axis, angleLimit, angleLimit >= 360f);
            }

            if (matches != null)
                return matches.ToArray();

            matches = FindLegacyOilPumpAnimatedParts(root);
            return matches?.ToArray();
        }

        public void UpdateAnimatedBuildingParts(AnimatedPart[] animatedParts, bool active, float time)
        {
            if (animatedParts == null)
                return;

            for (int i = 0; i < animatedParts.Length; i++)
            {
                AnimatedPart part = animatedParts[i];
                if (part?.Transform == null)
                    continue;

                if (!active)
                {
                    if (part.IsAtRest)
                        continue;

                    part.Transform.localRotation = part.BaseLocalRotation;
                    part.IsAtRest = true;
                    part.LastAppliedAngle = float.NaN;
                    continue;
                }

                float angle = part.ContinuousRotation
                    ? time * part.AngleLimit
                    : Mathf.Sin((time * OscillationRadiansPerSecond) + part.PhaseOffset) * part.AngleLimit;
                if (!float.IsNaN(part.LastAppliedAngle) &&
                    Mathf.Abs(Mathf.DeltaAngle(part.LastAppliedAngle, angle)) < AnimationAngleEpsilon)
                {
                    continue;
                }

                part.Transform.localRotation = part.BaseLocalRotation * Quaternion.AngleAxis(angle, part.Axis);
                part.IsAtRest = false;
                part.LastAppliedAngle = angle;
            }
        }

        private static bool TryParseAnimatedPartName(string name, out Vector3 axis, out float angleLimit)
        {
            axis = Vector3.zero;
            angleLimit = 0f;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore <= 0 || lastUnderscore >= name.Length - 1)
                return false;

            string angleToken = name[(lastUnderscore + 1)..];
            if (!float.TryParse(angleToken, NumberStyles.Float, CultureInfo.InvariantCulture, out angleLimit))
                return false;

            int axisUnderscore = name.LastIndexOf('_', lastUnderscore - 1);
            if (axisUnderscore <= 0 || axisUnderscore >= lastUnderscore - 1)
                return false;

            string axisToken = name[(axisUnderscore + 1)..lastUnderscore];
            axis = axisToken switch
            {
                "X" => Vector3.right,
                "Y" => Vector3.up,
                "Z" => Vector3.forward,
                _ => Vector3.zero
            };

            return axis != Vector3.zero && angleLimit > 0f;
        }

        private static List<AnimatedPart> FindLegacyOilPumpAnimatedParts(Transform root)
        {
            List<AnimatedPart> matches = null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!TryResolveLegacyOilPumpPart(child.name, out Vector3 axis, out float angleLimit))
                    continue;

                AddAnimatedPart(ref matches, child, axis, angleLimit, angleLimit >= 360f);
            }

            return matches;
        }

        private static void AddAnimatedPart(
            ref List<AnimatedPart> matches,
            Transform transform,
            Vector3 axis,
            float angleLimit,
            bool continuousRotation)
        {
            matches ??= new List<AnimatedPart>();
            matches.Add(new AnimatedPart
            {
                Transform = transform,
                BaseLocalEulerAngles = transform.localEulerAngles,
                BaseLocalRotation = transform.localRotation,
                Axis = axis,
                AngleLimit = angleLimit,
                PhaseOffset = matches.Count * 0.35f,
                ContinuousRotation = continuousRotation
            });
        }

        private static bool TryResolveLegacyOilPumpPart(string name, out Vector3 axis, out float angleLimit)
        {
            axis = Vector3.zero;
            angleLimit = 0f;
            if (name == LegacyOilPumpArmName)
            {
                axis = Vector3.right;
                angleLimit = OilPumpArmAngleLimit;
                return true;
            }

            if (name == LegacyOilPumpWheelName)
            {
                axis = Vector3.right;
                angleLimit = OilPumpWheelAngleLimit;
                return true;
            }

            return false;
        }
    }
}
