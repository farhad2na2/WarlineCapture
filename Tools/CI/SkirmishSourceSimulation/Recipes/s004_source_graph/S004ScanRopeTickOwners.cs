using System;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal sealed class S004ScanRopeTickOwners
{
    private delegate void Call<T>(ref T owner, ref SystemState state) where T : struct;
    private sealed class Owner<T> where T : struct
    {
        private T owner; private SystemState state; private readonly Call<T> update;
        internal int Updates;
        internal Owner(EntityManager em, Call<T> create, Call<T> step)
        {
            state = new SystemState { EntityManager = em, Enabled = true }; update = step;
            state.Begin(); try { create(ref owner, ref state); } finally { state.End(); }
            state.LastSystemVersion = 0;
        }
        internal void Run() { state.Begin(); try { if (state.Enabled && state.RequiredQueriesPresent) { update(ref owner, ref state); Updates++; } } finally { state.End(); } }
    }
    private readonly Owner<UnitScanOrderExecutionSystem> scanExecute;
    private readonly Owner<ScanIntelCommandSystem> scanCommand;
    private readonly Owner<UnitTransportRopeDisembarkSystem> ropeDisembark;
    private readonly Owner<UnitTransportRopeDisperseSystem> ropeDisperse;
    private readonly Owner<UnitTransportRopeDropSystem> ropeDrop;
    private readonly Owner<TransportBoardingDiagnosticLogFlushSystem> boardingLog;
    internal S004ScanRopeTickOwners(EntityManager em)
    {
        if (em == null) throw new ArgumentNullException(nameof(em));
        scanExecute = new Owner<UnitScanOrderExecutionSystem>(em, (ref UnitScanOrderExecutionSystem s, ref SystemState st) => s.OnCreate(ref st), (ref UnitScanOrderExecutionSystem s, ref SystemState st) => s.OnUpdate(ref st));
        scanCommand = new Owner<ScanIntelCommandSystem>(em, (ref ScanIntelCommandSystem s, ref SystemState st) => s.OnCreate(ref st), (ref ScanIntelCommandSystem s, ref SystemState st) => s.OnUpdate(ref st));
        ropeDisembark = new Owner<UnitTransportRopeDisembarkSystem>(em, (ref UnitTransportRopeDisembarkSystem s, ref SystemState st) => s.OnCreate(ref st), (ref UnitTransportRopeDisembarkSystem s, ref SystemState st) => s.OnUpdate(ref st));
        ropeDisperse = new Owner<UnitTransportRopeDisperseSystem>(em, (ref UnitTransportRopeDisperseSystem s, ref SystemState st) => s.OnCreate(ref st), (ref UnitTransportRopeDisperseSystem s, ref SystemState st) => s.OnUpdate(ref st));
        ropeDrop = new Owner<UnitTransportRopeDropSystem>(em, (ref UnitTransportRopeDropSystem s, ref SystemState st) => s.OnCreate(ref st), (ref UnitTransportRopeDropSystem s, ref SystemState st) => s.OnUpdate(ref st));
        boardingLog = new Owner<TransportBoardingDiagnosticLogFlushSystem>(em, (ref TransportBoardingDiagnosticLogFlushSystem s, ref SystemState st) => s.OnCreate(ref st), (ref TransportBoardingDiagnosticLogFlushSystem s, ref SystemState st) => s.OnUpdate(ref st));
    }
    internal void ScanExecute() => scanExecute.Run(); internal void ScanCommand() => scanCommand.Run();
    internal void RopeDisembark() => ropeDisembark.Run(); internal void RopeDisperse() => ropeDisperse.Run();
    internal void RopeDrop() => ropeDrop.Run(); internal void BoardingLog() => boardingLog.Run();
    internal JObject Observe() => new JObject { ["scan_execute_updates"] = scanExecute.Updates, ["scan_command_updates"] = scanCommand.Updates,
        ["rope_disembark_updates"] = ropeDisembark.Updates, ["rope_disperse_updates"] = ropeDisperse.Updates,
        ["rope_drop_updates"] = ropeDrop.Updates, ["boarding_log_updates"] = boardingLog.Updates };
}
