using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiOperationsMissionGateway
    {
        public bool TryReadOperationsMission(out UiOperationsMissionModel model)
        {
            model = default;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;
            using var query = world.EntityManager.CreateEntityQuery(typeof(UiOperationsMissionReadModel));
            if (query.CalculateEntityCount() != 1) return false;
            model = world.EntityManager.GetComponentObject<UiOperationsMissionReadModel>(query.GetSingletonEntity()).Value;
            return true;
        }

        public bool TryRequestOperationsMission(UiOperationsMissionAction action, int siteIndex)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;
            using var query = world.EntityManager.CreateEntityQuery(typeof(UiOperationsMissionReadModel), typeof(UiOperationsMissionRequest));
            if (query.CalculateEntityCount() != 1) return false;
            var requests = world.EntityManager.GetBuffer<UiOperationsMissionRequest>(query.GetSingletonEntity());
            if (requests.Length != 0) return false;
            requests.Add(new UiOperationsMissionRequest { Action = action, SiteIndex = siteIndex });
            return true;
        }
    }
}
