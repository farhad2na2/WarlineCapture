using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed partial class BuildingRuntimeProcessingCompositionSystemHelper
    {
        internal static bool TryResolveStartingGrantSetup(EntityManager em, Entity session,
            bool requestRequiresTypedPad, out SkirmishResolvedSetup setup,
            out SkirmishLaunchDefinitionRecord launch, out bool setupRequiresBinding, out bool launchMatches)
        {
            setup = null;
            launch = null;
            setupRequiresBinding = false;
            launchMatches = false;
            if (em == default || !em.Exists(session)) return false;

            // Campaign mission roots own their own authored placement policy and
            // intentionally have no expanded-Skirmish setup records.
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return !requestRequiresTypedPad;

            // Once the expanded-session marker exists, missing or null setup data
            // is corruption, not evidence that this is an unbound legacy grant.
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session)) return false;
            setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session)?.Setup;
            if (setup == null) return false;
            setupRequiresBinding = SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup);

            if (em.HasComponent<SkirmishLaunchDefinitionRecord>(session))
            {
                launch = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
                launchMatches = launch?.Definition != null && launch.Matches(
                    setup, em.GetComponentData<SkirmishExpandedSessionComponent>(session));
                if (!launchMatches) return false;
            }

            if (setupRequiresBinding)
                return requestRequiresTypedPad && launchMatches;

            // Preserve historical no-binding Skirmish grants, while preventing
            // them from opting into a typed search without an explicit launch bind.
            return !requestRequiresTypedPad;
        }

        internal static bool IsExactCheckpointReconstructionRequest(EntityManager em,
            in BuildingRuntimeSpawnRequest request, Unity.Mathematics.int2 configuredFootprint)
        {
            if (request.AuthoredStartingGrant == 0 || request.PlanEntity == Entity.Null || request.RequirePreferredOrigin == 0 ||
                request.RequireTypedPad != 0 || !em.Exists(request.PlanEntity) ||
                !em.HasComponent<BuildingStartingGrantOwner>(request.PlanEntity) ||
                !em.HasComponent<SkirmishNativeCheckpointStructureRequestGuardTag>(request.PlanEntity) ||
                !em.HasComponent<SkirmishNativeCheckpointStructureRequestGuard>(request.PlanEntity))
                return false;
            var guard = em.GetComponentObject<SkirmishNativeCheckpointStructureRequestGuard>(request.PlanEntity);
            return guard != null && guard.Matches(request, configuredFootprint);
        }
    }
}
