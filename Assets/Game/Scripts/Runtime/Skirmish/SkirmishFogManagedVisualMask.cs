using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
namespace Game.Runtime
{
    [DefaultExecutionOrder(10000)]
    internal sealed class SkirmishFogManagedVisualMask : MonoBehaviour
    {
        private World world;
        private Entity contact;
        private readonly Dictionary<Renderer, bool> renderers = new();
        private readonly Dictionary<Canvas, CanvasGroup> canvases = new();
        internal void Bind(World ownerWorld, Entity owner) { world = ownerWorld; contact = owner; }
        private void LateUpdate()
        {
            if (world == null || !world.IsCreated || !world.EntityManager.Exists(contact) ||
                SkirmishFogConsumerUtility.PlayerVisible(world.EntityManager, contact)) { Restore(); return; }
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!renderers.ContainsKey(renderer)) renderers.Add(renderer, renderer.forceRenderingOff);
                renderer.forceRenderingOff = true;
            }
            foreach (var canvas in GetComponentsInChildren<Canvas>(true))
            {
                if (!canvases.TryGetValue(canvas, out var fogGroup) || fogGroup == null)
                {
                    fogGroup = canvas.gameObject.AddComponent<CanvasGroup>();
                    fogGroup.interactable = false; fogGroup.blocksRaycasts = false;
                    canvases[canvas] = fogGroup;
                }
                fogGroup.alpha = 0;
            }
        }
        private void Restore()
        {
            foreach (var pair in renderers) if (pair.Key != null) pair.Key.forceRenderingOff = pair.Value;
            foreach (var pair in canvases) if (pair.Value != null) { pair.Value.alpha = 1; Destroy(pair.Value); }
            renderers.Clear(); canvases.Clear();
        }
        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
