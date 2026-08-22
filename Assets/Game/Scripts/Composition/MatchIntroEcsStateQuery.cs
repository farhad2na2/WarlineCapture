using Unity.Entities;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Runtime;
using Game.Components;

namespace Game.Composition
{
    internal sealed class MatchIntroEcsStateQuery : IMatchIntroStateQuery
    {
        private enum ReadResult : byte
        {
            Missing = 0,
            Found = 1,
            Invalid = 2
        }

        private readonly WorldScopedComponentQueryCache<MatchIntroTransitionComponent> _queryCache = new(readOnly: true);
        private readonly WorldScopedComponentQueryCache<CampaignMissionFinalePresentationComponent>
            _missionFinaleQueryCache = new(readOnly: true);
        private World _world;

        public void Bind(World world)
        {
            if (_world == world)
                return;

            _queryCache.Invalidate();
            _missionFinaleQueryCache.Invalidate();
            _world = world;
        }

        public bool IsGameplayInputLocked()
        {
            ReadResult result = TryReadState(out MatchIntroTransitionComponent state);
            return result == ReadResult.Invalid ||
                   result == ReadResult.Found && state.InputLocked != 0 ||
                   IsMissionFinaleInputLocked();
        }

        public bool IsIntroComplete()
        {
            ReadResult result = TryReadState(out MatchIntroTransitionComponent state);
            return result == ReadResult.Missing ||
                   result == ReadResult.Found &&
                   state.State == MatchIntroTransitionStateKind.Complete &&
                   state.InputLocked == 0;
        }

        public void Reset()
        {
            _queryCache.Invalidate();
            _missionFinaleQueryCache.Invalidate();
            _world = null;
        }

        private ReadResult TryReadState(out MatchIntroTransitionComponent state)
        {
            state = default;
            if (_world == null || !_world.IsCreated)
                return ReadResult.Invalid;

            EntityManager entityManager = _world.EntityManager;
            EntityQuery query = _queryCache.Get(entityManager);
            int boundaryCount = query.CalculateEntityCount();
            if (boundaryCount == 0)
                return ReadResult.Missing;
            if (boundaryCount != 1)
                return ReadResult.Invalid;

            Entity entity = query.GetSingletonEntity();
            if (!entityManager.HasComponent<UiShellStateComponent>(entity))
                return ReadResult.Invalid;

            state = entityManager.GetComponentData<MatchIntroTransitionComponent>(entity);
            return ReadResult.Found;
        }

        private bool IsMissionFinaleInputLocked()
        {
            if (_world == null || !_world.IsCreated)
                return true;
            EntityQuery query = _missionFinaleQueryCache.Get(_world.EntityManager);
            if (query.CalculateEntityCount() != 1)
                return false;
            CampaignMissionFinalePresentationComponent finale =
                query.GetSingleton<CampaignMissionFinalePresentationComponent>();
            return finale.Required != 0 && finale.Stage is >= 1 and <= 3;
        }
    }
}
