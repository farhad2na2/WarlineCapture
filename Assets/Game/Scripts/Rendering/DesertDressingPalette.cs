using System;
using UnityEngine;

namespace Game.Rendering
{
    // Mesh-only references keep decorative objects outside the gameplay and physics worlds.
    public sealed class DesertDressingPalette : ScriptableObject
    {
        [Serializable]
        public struct Piece
        {
            public Mesh mesh;
            public Material material;
            public Color tint;
            public float width;
            public float height;
        }

        public Texture2D windSand;
        public Piece[] pieces;
        public string[] scrubMapIds;
    }
}
