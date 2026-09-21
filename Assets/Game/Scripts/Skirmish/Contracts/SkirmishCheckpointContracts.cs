namespace Game.Skirmish.Contracts
{
    public enum SkirmishCheckpointSchemaKind : byte
    {
        None = 0,
        ExpandedV1 = 1
    }

    public enum SkirmishCheckpointStatus : byte
    {
        None = 0,
        Requested = 1,
        Written = 2,
        Restored = 3,
        Rejected = 4
    }

    public sealed class SkirmishCheckpointHeader
    {
        public int SchemaVersion = 1;
        public SkirmishCheckpointSchemaKind Kind = SkirmishCheckpointSchemaKind.ExpandedV1;
        public string SessionId = string.Empty;
        public string CatalogId = string.Empty;
        public string DefinitionId = string.Empty;
        public int ContentVersion;
        public uint SetupHash;
        public int SimulationTick;
        public string Checksum = string.Empty;
    }
}
