using UnityEngine;
using Game.Components;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class BridgeSurfaceAuthoring : MonoBehaviour
    {
        [SerializeField] private Transform bridgeDeckRoot;
        [SerializeField] private Transform lowerSurfaceRoot;
        [SerializeField] private Transform[] approachRoots;
        [SerializeField] private MapSurfaceMovementMask allowedMovementMask = MapSurfaceMovementMask.AllGroundUnits;
        [SerializeField, Min(0f)] private float lowerSurfaceClearance = 4f;
        [SerializeField] private int bridgeLayerId = 1;
        [SerializeField] private int lowerLayerId;

        public Transform BridgeDeckRoot => bridgeDeckRoot;
        public Transform LowerSurfaceRoot => lowerSurfaceRoot;
        public Transform[] ApproachRoots => approachRoots;
        public MapSurfaceMovementMask AllowedMovementMask => allowedMovementMask;
        public float LowerSurfaceClearance => lowerSurfaceClearance;
        public int BridgeLayerId => bridgeLayerId;
        public int LowerLayerId => lowerLayerId;
    }
}
