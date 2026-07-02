using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Custom Game/Visual Registry Config")]
    public sealed class CustomGameVisualRegistryConfig : ScriptableObject
    {
        [System.Serializable]
        public sealed class VisualEntry
        {
            [SerializeField] private string sourceKey;
            [SerializeField] private GameObject visualPrefab;
            [SerializeField] private Texture2D impostorAtlas;
            [SerializeField, Min(1)] private int directionCount = 8;
            [SerializeField, Min(1)] private int columns = 4;
            [SerializeField, Min(1)] private int rows = 2;
            [SerializeField] private Vector2 worldSize = new(1f, 1.8f);

            public string SourceKey => sourceKey;
            public GameObject VisualPrefab => visualPrefab;
            public Texture2D ImpostorAtlas => impostorAtlas;
            public int DirectionCount => directionCount;
            public int Columns => columns;
            public int Rows => rows;
            public Vector2 WorldSize => worldSize;
        }

        [SerializeField] private List<VisualEntry> visuals = new();

        public IReadOnlyList<VisualEntry> Visuals => visuals;
    }
}
