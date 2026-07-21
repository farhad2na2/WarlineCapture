using Game.Configs;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Authoring
{
    public enum OperationMapEntityPresentationRole : byte
    {
        Unknown = 0,
        GameplayBuildings = 1,
        GameplayVehicles = 2,
        RenderOnly = 3
    }

    [DisallowMultipleComponent]
    public sealed class OperationMapEntityPresentationRootAuthoring : MonoBehaviour
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private string operationMapId;
        [SerializeField] private OperationMapEntityPresentationRole role;
        [SerializeField, Min(1)] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string migrationRecordSetHash;

        public string OperationMapId => operationMapId;
        public OperationMapEntityPresentationRole Role => role;
        public int SchemaVersion => schemaVersion;
        public string MigrationRecordSetHash => migrationRecordSetHash;

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }

            if (role != OperationMapEntityPresentationRole.GameplayBuildings &&
                role != OperationMapEntityPresentationRole.GameplayVehicles &&
                role != OperationMapEntityPresentationRole.RenderOnly)
            {
                error = $"Unknown operation-map entity presentation role: {(byte)role}.";
                return false;
            }

            if (schemaVersion != CurrentSchemaVersion)
            {
                error =
                    $"Operation-map entity presentation schema version must be {CurrentSchemaVersion}.";
                return false;
            }

            if (!OperationMapHashRules.IsValidSha256(migrationRecordSetHash))
            {
                error =
                    "Migration record-set hash must be 64 lowercase hexadecimal characters.";
                return false;
            }

            error = null;
            return true;
        }

        private sealed class RootBaker : Baker<OperationMapEntityPresentationRootAuthoring>
        {
            public override void Bake(OperationMapEntityPresentationRootAuthoring authoring)
            {
                if (!authoring.TryValidate(out _))
                    return;

                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new OperationMapEntityPresentationRoot
                {
                    OperationMapId = new FixedString128Bytes(authoring.operationMapId),
                    Role = (byte)authoring.role,
                    SchemaVersion = authoring.schemaVersion,
                    MigrationRecordSetHash = new FixedString128Bytes(authoring.migrationRecordSetHash)
                });
            }
        }
    }
}
