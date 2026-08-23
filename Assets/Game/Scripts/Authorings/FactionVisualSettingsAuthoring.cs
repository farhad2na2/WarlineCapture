using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class FactionVisualSettingsAuthoring : MonoBehaviour
    {
        [SerializeField] private FactionVisualSettingsConfig config;

        [Header("Marker Colors")]
        [SerializeField, HideInInspector] private Color playerColor = new(0.12f, 0.72f, 1f, 1f);
        [SerializeField, HideInInspector] private Color enemyColor = new(1f, 0.35f, 0.2f, 1f);
        [SerializeField, HideInInspector] private Color neutralColor = new(0.82f, 0.82f, 0.82f, 1f);

        private void OnValidate()
        {
            ApplyConfigIfAvailable();
        }

        private void OnEnable()
        {
            ApplyConfigIfAvailable();
        }

        private void ApplyConfigIfAvailable()
        {
            if (config == null)
                return;

            playerColor = config.PlayerColor;
            enemyColor = config.EnemyColor;
            neutralColor = config.NeutralColor;
        }

        public Color GetColor(byte factionId)
        {
            return factionId switch
            {
                0 => neutralColor,
                1 => playerColor,
                _ => enemyColor
            };
        }

        [BakingVersion("WarlineCapture", 1)]
        private sealed class Baker : Baker<FactionVisualSettingsAuthoring>
        {
            public override void Bake(FactionVisualSettingsAuthoring authoring)
            {
                authoring.ApplyConfigIfAvailable();
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new FactionVisualConfig
                {
                    PlayerColor = ToFloat4(authoring.playerColor),
                    EnemyColor = ToFloat4(authoring.enemyColor),
                    NeutralColor = ToFloat4(authoring.neutralColor)
                });
            }

            private static float4 ToFloat4(Color color)
            {
                return new float4(color.r, color.g, color.b, color.a);
            }
        }
    }
}
