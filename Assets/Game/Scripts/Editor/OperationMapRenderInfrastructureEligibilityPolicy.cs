#if UNITY_EDITOR

using System;
using Game.Authoring;
using Game.Components;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct OperationMapRenderInfrastructureEligibilityDecision
    {
        internal OperationMapRenderInfrastructureEligibilityDecision(
            bool selected,
            string reasonCode)
        {
            Selected = selected;
            ReasonCode = reasonCode ?? throw new ArgumentNullException(nameof(reasonCode));
        }

        internal bool Selected { get; }
        internal string ReasonCode { get; }
    }

    internal static class OperationMapRenderInfrastructureEligibilityPolicy
    {
        internal static OperationMapRenderInfrastructureEligibilityDecision Evaluate(
            DenseCityPresentationIdentityAuthoring owner)
        {
            if (owner == null)
            {
                return Reject("infrastructure-owner-missing");
            }
            if (!owner.TryValidate(out _) ||
                owner.Role != OperationMapEntityPresentationRole.RenderOnly ||
                owner.Category != DenseCityPresentationSemanticCategory.Infrastructure)
            {
                return Reject("infrastructure-owner-contract-invalid");
            }
            if (owner.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)
                    .Length != 1)
            {
                return Reject("infrastructure-owner-identity-ambiguous");
            }

            Component[] components = owner.GetComponentsInChildren<Component>(true);
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                    return Reject("infrastructure-owner-missing-script");
                if (component is Transform or MeshFilter or
                    DenseCityPresentationIdentityAuthoring)
                {
                    continue;
                }
                if (component is MeshRenderer meshRenderer)
                {
                    if (meshRenderer.HasPropertyBlock())
                    {
                        return Reject(
                            "infrastructure-owner-material-property-block");
                    }
                    continue;
                }
                if (component is Animator or Animation)
                    return Reject("infrastructure-owner-animation");
                if (component is Light)
                    return Reject("infrastructure-owner-light");
                if (component is ParticleSystem or ParticleSystemRenderer)
                    return Reject("infrastructure-owner-particle");
                if (component is Collider or Rigidbody or Joint)
                    return Reject("infrastructure-owner-gameplay-physics");
                if (component is LODGroup)
                    return Reject("infrastructure-owner-lod-pending-vrp072");
                if (component is AudioSource or ReflectionProbe or Projector or
                    WindZone or Cloth or TrailRenderer or LineRenderer)
                {
                    return Reject("infrastructure-owner-special-presentation");
                }
                if (component is MonoBehaviour)
                    return Reject("infrastructure-owner-custom-behaviour");
                if (component is Renderer)
                    return Reject("infrastructure-owner-unsupported-renderer");
                return Reject("infrastructure-owner-unsupported-component");
            }

            return new OperationMapRenderInfrastructureEligibilityDecision(
                true,
                "eligible");
        }

        private static OperationMapRenderInfrastructureEligibilityDecision Reject(
            string reasonCode) =>
            new(false, reasonCode);
    }
}

#endif
