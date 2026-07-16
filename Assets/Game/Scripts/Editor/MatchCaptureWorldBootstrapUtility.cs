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

            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
                return false;

            DefaultWorldInitialization.Initialize("Default World", editorWorld: false);
            return true;
        }
    }
}
