using Game.Rendering;
using Game.Rendering.Contracts;
using UnityEngine;

namespace Game.Composition
{
    internal static class RenderingQueryComposition
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Bind() => RenderBoundsGateway.Bind(RenderBoundsPresentation.Create());
    }
}
