using System;
using UnityEngine;

namespace Game.Configs
{
    public enum OperationMapDeliveryKind : byte
    {
        BuiltInLocal = 0,
        RemoteDownload = 1
    }

    [Serializable]
    public struct OperationMapContentPackConfig
    {
        [SerializeField] private string contentPackId;
        [SerializeField] private OperationMapDeliveryKind deliveryKind;
        [SerializeField, Min(1)] private int contentVersion;
        [SerializeField] private string contentHash;

        public string ContentPackId => contentPackId;
        public OperationMapDeliveryKind DeliveryKind => deliveryKind;
        public int ContentVersion => contentVersion;
        public string ContentHash => contentHash;

        public OperationMapContentPackConfig(
            string contentPackId,
            OperationMapDeliveryKind deliveryKind,
            int contentVersion,
            string contentHash)
        {
            this.contentPackId = contentPackId;
            this.deliveryKind = deliveryKind;
            this.contentVersion = contentVersion;
            this.contentHash = contentHash;
        }

        public bool TryValidate(OperationMapDefinition definition, out string error)
        {
            if (definition == null || !definition.TryValidateIdentity(out _))
            {
                error = "Operation-map content pack requires a valid definition identity.";
                return false;
            }

            string expectedPackId = "opmap-pack." + definition.OperationMapId.Substring("opmap.".Length);
            if (!string.Equals(contentPackId, expectedPackId, StringComparison.Ordinal))
            {
                error = $"Content-pack id must be '{expectedPackId}'.";
                return false;
            }

            if (!Enum.IsDefined(typeof(OperationMapDeliveryKind), deliveryKind))
            {
                error = $"Unsupported operation-map delivery kind: {(byte)deliveryKind}.";
                return false;
            }

            if (contentVersion != definition.ContentVersion ||
                !string.Equals(contentHash, definition.ContentHash, StringComparison.Ordinal))
            {
                error = "Content-pack version and hash must match its operation-map definition.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct OperationMapCatalogEntryConfig
    {
        [SerializeField] private OperationMapDefinition definition;
        [SerializeField] private OperationMapContentPackConfig contentPack;

        public OperationMapDefinition Definition => definition;
        public OperationMapContentPackConfig ContentPack => contentPack;

        public OperationMapCatalogEntryConfig(
            OperationMapDefinition definition,
            OperationMapContentPackConfig contentPack)
        {
            this.definition = definition;
            this.contentPack = contentPack;
        }

        public bool TryValidate(out string error)
        {
            if (definition == null)
            {
                error = "Operation-map catalog entry requires a definition.";
                return false;
            }

            if (!definition.TryValidateMetadata(out error))
            {
                error = $"Operation-map catalog entry definition is invalid: {error}";
                return false;
            }

            return contentPack.TryValidate(definition, out error);
        }
    }
}
