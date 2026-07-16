using Game.UI.Shell.Ecs;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    internal static class MatchCaptureWorldBootstrapUtility
    {
        internal static bool EnsureDefaultGameWorld()
        {
            if (!Application.isBatchMode)
                return false;

            bool changed = false;
            World world = ResolveDefaultGameWorld(ref changed);
            if (world.GetExistingSystem<UiShellStateSystem>() == SystemHandle.Null)
            {
                world.GetOrCreateSystem<UiShellStateSystem>();
                changed = true;
            }

            return changed;
        }

        private static World ResolveDefaultGameWorld(ref bool changed)
        {
            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (IsGameWorld(defaultWorld))
                return defaultWorld;

            foreach (World world in World.All)
            {
                if (!IsGameWorld(world))
                    continue;

                World.DefaultGameObjectInjectionWorld = world;
                changed = true;
                return world;
            }

            changed = true;
            return DefaultWorldInitialization.Initialize("Default World", editorWorld: false);
        }

        private static bool IsGameWorld(World world)
        {
            return world != null &&
                   world.IsCreated &&
                   (world.Flags & WorldFlags.Game) == WorldFlags.Game;
        }
    }
}
