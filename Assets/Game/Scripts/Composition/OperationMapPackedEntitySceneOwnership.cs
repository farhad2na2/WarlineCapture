using Game.Configs;
using Unity.Entities;
using Unity.Scenes;

namespace Game.Composition
{
    internal interface IOperationMapEntitySceneApi
    {
        bool TryEnsureReady(
            string sceneGuid,
            string expectedOperationMapId,
            OperationMapRenderResidencyMode renderResidencyMode,
            ref Entity sceneEntity,
            ref bool ownsScene,
            out bool ready,
            out string error);

        bool TryReleaseOwned(
            ref Entity sceneEntity,
            ref bool ownsScene,
            ref bool releaseStarted,
            out bool complete,
            out string error);
    }

    internal sealed class OperationMapEntitySceneApi : IOperationMapEntitySceneApi
    {
        public bool TryEnsureReady(
            string sceneGuidValue,
            string expectedOperationMapId,
            OperationMapRenderResidencyMode renderResidencyMode,
            ref Entity sceneEntity,
            ref bool ownsScene,
            out bool ready,
            out string error)
        {
            ready = false;
            error = null;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Packed EntityScene loading requires the default ECS world.";
                return false;
            }

            var sceneGuid = new Hash128(sceneGuidValue);
            if (!sceneGuid.IsValid)
            {
                error =
                    "Packed EntityScene definition has an invalid authored SubScene GUID: " +
                    $"'{sceneGuidValue}'.";
                return false;
            }

            if (sceneEntity == Entity.Null || !world.EntityManager.Exists(sceneEntity))
                sceneEntity = SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid);

            if (sceneEntity != Entity.Null &&
                world.EntityManager.HasComponent<RequestSceneLoaded>(sceneEntity))
            {
                ready = SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity);
                if (ready &&
                    !OperationMapEntityPresentationReadinessUtility.TryValidate(
                        world.EntityManager,
                        sceneEntity,
                        expectedOperationMapId,
                        renderResidencyMode,
                        out error))
                {
                    ready = false;
                    return false;
                }
                return true;
            }

            sceneEntity = SceneSystem.LoadSceneAsync(world.Unmanaged, sceneGuid);
            ownsScene = sceneEntity != Entity.Null;
            if (!ownsScene)
            {
                error = $"Packed EntityScene load did not start for GUID '{sceneGuid}'.";
                return false;
            }

            ready = SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity);
            if (ready &&
                !OperationMapEntityPresentationReadinessUtility.TryValidate(
                    world.EntityManager,
                    sceneEntity,
                    expectedOperationMapId,
                    renderResidencyMode,
                    out error))
            {
                ready = false;
                return false;
            }
            return true;
        }

        public bool TryReleaseOwned(
            ref Entity sceneEntity,
            ref bool ownsScene,
            ref bool releaseStarted,
            out bool complete,
            out string error)
        {
            complete = false;
            error = null;
            if (!ownsScene)
            {
                sceneEntity = Entity.Null;
                releaseStarted = false;
                complete = true;
                return true;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Packed EntityScene unload requires the default ECS world.";
                return false;
            }

            if (sceneEntity == Entity.Null || !world.EntityManager.Exists(sceneEntity))
            {
                sceneEntity = Entity.Null;
                ownsScene = false;
                releaseStarted = false;
                complete = true;
                return true;
            }

            if (!releaseStarted)
            {
                SceneSystem.UnloadScene(
                    world.Unmanaged,
                    sceneEntity,
                    SceneSystem.UnloadParameters.DestroyMetaEntities);
                releaseStarted = true;
            }

            complete = !world.EntityManager.Exists(sceneEntity);
            if (!complete)
                return true;

            sceneEntity = Entity.Null;
            ownsScene = false;
            releaseStarted = false;
            return true;
        }
    }

    internal sealed class OperationMapPackedEntitySceneOwnership
    {
        private readonly IOperationMapEntitySceneApi api;
        private Entity sceneEntity;
        private bool ownsScene;
        private bool releaseStarted;

        public OperationMapPackedEntitySceneOwnership(IOperationMapEntitySceneApi api)
        {
            this.api = api;
        }

        public bool OwnsScene => ownsScene;
        public int LoadRequestCount { get; private set; }
        public int UnloadRequestCount { get; private set; }

        public bool TryEnsureReady(
            OperationMapSceneView view,
            string expectedOperationMapId,
            out bool ready,
            out string error)
        {
            ready = false;
            error = null;
            if (view.MapSubScene.SceneGUID.IsValid)
            {
                ready = true;
                return true;
            }

            bool ownedBefore = ownsScene;
            bool result = api.TryEnsureReady(
                view.Definition.NavigationMetadata.AuthoredSubSceneGuid,
                expectedOperationMapId,
                view.Definition.RenderResidencyMode,
                ref sceneEntity,
                ref ownsScene,
                out ready,
                out error);
            if (!ownedBefore && ownsScene)
                LoadRequestCount++;
            return result;
        }

        public bool TryReleaseOwned(out bool complete, out string error)
        {
            bool releaseStartedBefore = releaseStarted;
            bool result = api.TryReleaseOwned(
                ref sceneEntity,
                ref ownsScene,
                ref releaseStarted,
                out complete,
                out error);
            if (!releaseStartedBefore && releaseStarted)
                UnloadRequestCount++;
            return result;
        }

        public void ResetReleasedState()
        {
            sceneEntity = Entity.Null;
            ownsScene = false;
            releaseStarted = false;
        }
    }
}
