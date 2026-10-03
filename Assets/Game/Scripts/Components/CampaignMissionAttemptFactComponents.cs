using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct CampaignMissionAttemptFactsComponent : IComponentData
    {
        public byte NetworkNodesVerified, NetworkNodesDisabled, NetworkAuditRecovered, NetworkExtracted, NetworkEngineerAboard, NetworkCommsReady;
        public NetworkCollapseFailure NetworkFailure;
        public byte CorridorSuppliesDelivered, CorridorLinkRecovered, CorridorEngineerDelivered, CorridorKeysDelivered, CorridorCommsReady;
        public int CorridorFuelReceived;
        public LastCorridorFailure CorridorFailure;
        public byte CommandNodeNetworkSeparated, CommandNodeAuditReleased, CommandNodeSpecialistsSafe, CommandNodeServicesIntact, CommandNodeCommsReady;
        public CommandNodeFailure CommandNodeFailure;
        public byte TrustNorthArrived, TrustSouthArrived, TrustRelayVerified, TrustCommsReady, TrustMilitaryCleared, TrustNorthCrossed, TrustSouthCrossed;
        public int TrustStableMilliseconds;
        public TrustUnderFireFailure TrustFailure;
        public byte CitywideClinicRecovered, CitywideUtilityRecovered, CitywideReinforcementReady, CitywideCommsReady;
        public byte CitywideCoverageReady, CitywideAirCleared, CitywideMilitaryCleared;
        public int CitywideStableMilliseconds;
        public CitywideAlertFailure CitywideFailure;
        public byte GroundedSignalInserted, GroundedSignalRelayDisabled, GroundedSignalTerminalLost;
        public byte ArmorBreakAirCleared, ArmorBreakHeavyDisabled, ArmorBreakCommandDisabled, ArmorBreakAuthorityRecovered, ArmorBreakReliefLost, ArmorBreakCloseCompleted, ArmorBreakDebriefCompleted;
        public byte ArmorBreakCoverageReady, ArmorBreakArmoredThreatsCleared, ArmorBreakAircraftUsed, ArmorBreakArmorApproached;
        public int ArmorBreakLauncherShots, ArmorBreakRecoveryMilliseconds;
        public ArmorBreakFailure ArmorBreakFailure;
        public byte SupplyOilTransferred, SupplyFuelTransferred, SupplyReserveComplete;
        public int SupplyStoredFuel;
        public SupplyLineFailure SupplyFailure;
        public byte MarketReliefDelivered, MarketManifestVerified, MarketOpen;
        public MarketLifelineFailure MarketFailure;
        public byte PowerSafeRouteConfirmed, PowerFamiliesSheltered, PowerRestored, PowerRelaySecured, PowerExposedRouteUsed;
        public PowerRelayFailure PowerRelayFailure;
        public byte RouteReliefDelivered, RouteFuelDelivered, RouteLinkRestored, RouteHubEntered, RouteGarrisonCleared, RouteRecordsPreserved, RouteRelayNodeActivated;
        public RouteReopenedFailure RouteReopenedFailure;
        public byte GridlockSiteAComplete, GridlockSiteBComplete, GridlockDelivered;
        public GridlockFailure GridlockFailure;
        public int ElapsedMilliseconds;
        public byte BreachGateDestroyed, BreachCoreDestroyed, BreachArchiveSecured, BreachSupportLost, BreachTimedOut;
        public int BreachSecureMilliseconds;
        public int SquadLossCount;
        public int HostileTotalCount;
        public int HostileDefeatedCount;
        public int RequiredBuildingPlacedCount;
        public int RequiredBuildingCompletedCount;
        public int RequiredUnitProducedCount;
        public int CivilianTotalCount;
        public int CivilianLossCount;
        public int ExtractionPassengerTotal, ExtractionPassengersAboard, ExtractionPassengersDelivered;
        public int ExtractionCarrierLegCount, ExtractionSecureMilliseconds;
        public byte ExtractionCarrierLost, ExtractionAircraftLost, ExtractionContested, ExtractionDeparted, ExtractionTimedOut;
        public byte ForwardPostBound;
        public byte ForwardPostDamaged;
        public byte ForwardPostDestroyed;
        public byte CoreBreached;
        public byte HostileRosterIntegrityFault;
        public byte DefenseWaveWarningIssued;
        public byte DefenseWaveActivated;
        public byte InteractiveBriefCompleted;
        public byte CommandSquadSpawned;
        public byte CommandSquadAlive;
        public byte MoveToCoverComplete;
        public byte ThreatConfirmed;
        public byte AttackIssued;
        public byte FinalePresentationRequired;
        public byte FinalePresentationComplete;
    }

    public struct CampaignMissionAttemptFactProjectionStateComponent : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public int BuildingRequestBaselineId;
        public int RequiredBuildingOwnedCountBaseline;
        public int ProducedUnitReadModelBaselineCount;
        public uint SourceVersion;
        public byte Initialized;
    }
}
