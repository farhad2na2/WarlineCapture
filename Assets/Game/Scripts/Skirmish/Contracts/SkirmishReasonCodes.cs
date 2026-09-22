namespace Game.Skirmish.Contracts
{
    public enum SkirmishReasonCode : ushort
    {
        None = 0,
        UnsupportedRole = 1,
        UnsupportedMap = 2,
        UnsupportedSize = 3,
        UnsupportedDifficulty = 4,
        InsufficientMaterials = 5,
        InsufficientFuel = 6,
        InsufficientCapacity = 7,
        MissingProducer = 8,
        MissingReadiness = 9,
        StaleCommand = 10,
        BlockedSpawn = 11,
        IncompatibleSave = 12,
        MissingDefinition = 13,
        MissingReference = 14,
        DuplicateId = 15,
        Overflow = 16,
        InsufficientRunway = 17,
        InsufficientAnchor = 18,
        UnsupportedCapability = 19,
        MatrixMismatch = 20,
        InvalidIdentity = 21,
        DuplicateInitialization = 22,
        MissingResolvedSetup = 23,
        SpawnBoundaryUnavailable = 24,
        CleanupRequired = 25,
        ProductionRejected = 26,
        HiddenContact = 27,
        IncompatibleOrder = 28,
        InvalidSelection = 29,
        AlreadyCompleted = 30,
        QueueLocked = 31,
        NotCancellable = 32
    }
}
