using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public readonly struct SkirmishEnemyWaypoint
    {
        public float X { get; }
        public float Z { get; }
        public SkirmishEnemyWaypoint(float x, float z) { X = x; Z = z; }
    }

    public sealed class SkirmishEnemyRouteSnapshot
    {
        public string RouteId { get; }
        public SkirmishMeasuredRouteKind Kind { get; }
        public float WidthMetres { get; }
        public float LengthMetres { get; }
        public float InfantryTransitSeconds { get; }
        public float GroundTransitSeconds { get; }
        public bool ProvisionalTimes { get; }
        public bool AllowsVehicles { get; }
        public bool AllowsAircraft { get; }
        public IReadOnlyList<SkirmishEnemyWaypoint> Waypoints { get; }
        public SkirmishEnemyRouteSnapshot(string id, SkirmishMeasuredRouteKind kind, float width,
            bool vehicles, bool aircraft, SkirmishEnemyWaypoint[] waypoints, float lengthMetres,
            float infantryTransitSeconds, float groundTransitSeconds, bool provisionalTimes)
        {
            RouteId = id; Kind = kind; WidthMetres = width;
            AllowsVehicles = vehicles; AllowsAircraft = aircraft;
            LengthMetres = lengthMetres; InfantryTransitSeconds = infantryTransitSeconds;
            GroundTransitSeconds = groundTransitSeconds; ProvisionalTimes = provisionalTimes;
            Waypoints = Array.AsReadOnly((SkirmishEnemyWaypoint[])waypoints.Clone());
        }
    }

    // Compiled once, never reads live ScriptableObjects during a match.
    // Canonical identity is explicit; this object is not a JsonUtility wire payload.
    public sealed class SkirmishEnemyPolicySnapshot
    {
        public const int CurrentVersion = 2;
        public int Version => CurrentVersion;
        public uint Hash { get; }
        public string Fingerprint { get; }
        public string SourceContentHash { get; }
        public SkirmishDifficultyId Difficulty { get; }
        public SkirmishReadinessStage Readiness { get; }
        public float ResponseMinSeconds { get; }
        public float ResponseMaxSeconds { get; }
        public float ScoutMinSeconds { get; }
        public float ScoutMaxSeconds { get; }
        public float OpeningRestraintSeconds { get; }
        public float StrategicCadenceSeconds => 4f;
        public float ObservationCadenceSeconds => .25f;
        public int PlanHysteresisScore => 15;
        public float ReserveSupplyFraction => .25f;
        public int MaxDispatchPerDecision => 2;
        public int MaxIdenticalRejects => 3;
        public float RejectBackoffSeconds => 4f;
        public byte Personality { get; }
        public uint InitialRandomState { get; }
        public IReadOnlyList<SkirmishEnemyRouteSnapshot> Routes { get; }
        public IReadOnlyList<SkirmishEnemyConstructionPad> ConstructionPads { get; }
        internal SkirmishEnemyPolicySnapshot(string source, SkirmishDifficultyId difficulty,
            SkirmishReadinessStage readiness, float responseMin, float responseMax, float scoutMin,
            float scoutMax, float opening, byte personality, uint initialRandomState,
            SkirmishEnemyRouteSnapshot[] routes, uint hash, string fingerprint,
            SkirmishEnemyConstructionPad[] constructionPads = null)
        {
            SourceContentHash = source; Difficulty = difficulty; Readiness = readiness;
            ResponseMinSeconds = responseMin; ResponseMaxSeconds = responseMax;
            ScoutMinSeconds = scoutMin; ScoutMaxSeconds = scoutMax; OpeningRestraintSeconds = opening;
            Personality = personality; InitialRandomState = initialRandomState;
            Routes = Array.AsReadOnly((SkirmishEnemyRouteSnapshot[])routes.Clone());
            Hash = hash; Fingerprint = fingerprint;
            ConstructionPads = Array.AsReadOnly((SkirmishEnemyConstructionPad[])(constructionPads ?? Array.Empty<SkirmishEnemyConstructionPad>()).Clone());
        }
    }
}
