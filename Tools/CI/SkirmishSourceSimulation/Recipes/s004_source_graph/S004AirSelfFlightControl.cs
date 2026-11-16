using System;
using Game.Runtime;
using Game.Runtime.TransportPose;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004AirSelfFlightControl
{
    sealed class PoseAdapter : ISourceAirSelfPose
    {
        readonly PoseGraph pose;
        internal PoseAdapter(PoseGraph value) { pose = value; }
        public float3 RootPosition => pose.RootPosition;
        public float3 Forward => pose.Forward;
        public void SetRoot(float3 position, quaternion rotation) => pose.SetRoot(position, rotation);
        public void Spin(float delta) => pose.SourceSpin(delta);
    }
    sealed class Completion : ISourceAirSelfCompletionView
    {
        internal bool Ready; internal int Spawned, Destroyed;
        readonly SourcePendingProduction p = new SourcePendingProduction();
        public SourcePendingProduction FindReady(RuntimeBuildingEntity b, SourceUnitPrefab prefab, float now) => Ready ? p : null;
        public int2 ResolveCell(float3 p) => int2.zero;
        public bool TrySpawn(RuntimeBuildingEntity b, int index, int slot, float3 p, int2 cell, ref uint r, SourcePendingProduction pending) { Spawned++; return true; }
        public bool Remove(RuntimeBuildingEntity b, SourcePendingProduction p) => true;
        public void Rebuild(RuntimeBuildingEntity b, float now, bool preserve) { }
        public void Align(RuntimeBuildingEntity b, float3 forward) { }
        public void Release(SourcePendingProduction p) { }
        public void Destroy(RuntimeBuildingEntity b) { Destroyed++; }
    }
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void Main()
    {
        PoseMath.AdmitInstalledMono();
        var pose = new PoseGraph(new[] { new PoseFrame { Parent = -1, Name = "fixture", Rotation = quaternion.identity, Scale = new float3(1) } },
            new MeshBounds[0], new int[0], true, true, true, true);
        var completion = new Completion(); int publications = 0, fallbacks = 0;
        var flight = new SourceAirSelfFlightProjection(new RuntimeBuildingEntity(), new SourceUnitPrefab("fixture", new Entity { Index = 1, Version = 1 }),
            new PoseAdapter(pose), completion, new float3(0, 8, 0), new float3(10, 8, 0), new float3(10, 2, 0), quaternion.identity, quaternion.identity,
            2, 0, (float now, ref uint random) => { fallbacks++; random++; }, now => { throw new InvalidOperationException("unexpected departure"); }, () => publications++);
        uint rng = 1;
        flight.Tick(1, 1f / 60f, ref rng); Need(flight.Phase == 0 && pose.RootPosition.x == 5, "approach");
        flight.Tick(2, 1f / 60f, ref rng); Need(flight.Phase == 1 && flight.PhaseStartedAt == 2, "arrival");
        flight.Tick(2.75f, 1f / 60f, ref rng); Need(pose.RootPosition.y == 5 && completion.Spawned == 0, "landing");
        flight.Tick(3.5f, 1f / 60f, ref rng); Need(fallbacks == 1 && rng == 2 && flight.Present, "original fallback handoff");
        completion.Ready = true;
        flight.Tick(3.6f, 1f / 60f, ref rng); Need(!flight.Present && completion.Spawned == 1 && completion.Destroyed == 1, "completion");
        flight.Tick(3.7f, 1f / 60f, ref rng); Need(publications == 5 && completion.Spawned == 1, "removed transport not dispatched");
        Console.WriteLine("[S004AirSelfFlightControl] result=Passed scope=SyntheticFixture_NoQueueOrMission");
    }
}
