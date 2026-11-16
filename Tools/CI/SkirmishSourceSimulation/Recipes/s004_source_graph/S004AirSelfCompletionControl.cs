using System;
using Game.Runtime;
using Unity.Mathematics;

internal static class S004AirSelfCompletionControl
{
    sealed class View : ISourceAirSelfCompletionView
    {
        public string Calls = "";
        public bool Ready, Spawn, Removed;
        public readonly SourcePendingProduction Pending = new SourcePendingProduction { ProductionIndex = 7, ReservedProductionSlotIndex = 3 };
        public SourcePendingProduction FindReady(RuntimeBuildingEntity b, SourceUnitPrefab p, float n) { Calls += "F"; return Ready ? Pending : null; }
        public int2 ResolveCell(float3 p) { Calls += "C"; Need(p.x == 12 && p.z == 18, "touchdown"); return new int2(4, 5); }
        public bool TrySpawn(RuntimeBuildingEntity b, int i, int s, float3 p, int2 c, ref uint r, SourcePendingProduction x)
        { Calls += "S"; Need(i == 7 && s == 3 && c.x == 4 && c.y == 5 && x == Pending, "spawn arguments"); r++; return Spawn; }
        public bool Remove(RuntimeBuildingEntity b, SourcePendingProduction p) { Calls += "M"; return Removed; }
        public void Rebuild(RuntimeBuildingEntity b, float n, bool preserve) { Calls += "B"; Need(!preserve && n == 9, "timeline"); }
        public void Align(RuntimeBuildingEntity b, float3 f) { Calls += "A"; Need(f.z == 1, "forward"); }
        public void Release(SourcePendingProduction p) { Calls += "R"; Need(p == Pending, "release"); }
        public void Destroy(RuntimeBuildingEntity b) { Calls += "D"; }
    }
    static void Need(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    static void Case(bool ready, bool spawn, bool removed, bool expected, string calls, uint finalRandom)
    {
        var view = new View { Ready = ready, Spawn = spawn, Removed = removed };
        uint random = 12;
        bool result = SourceAirSelfCompletionProjection.Complete(view, new RuntimeBuildingEntity(), null,
            new float3(12, 0, 18), new float3(0, 0, 1), 9, ref random);
        Need(result == expected && view.Calls == calls && random == finalRandom, "completion order " + view.Calls);
    }
    static void Main()
    {
        Case(false, false, false, false, "F", 12);
        Case(true, false, false, true, "FCSD", 13);
        Case(true, true, true, true, "FCSMBARD", 13);
        Case(true, true, false, true, "FCSMAD", 13);
        Console.WriteLine("[S004AirSelfCompletionControl] result=Passed cases=4 scope=SyntheticFixtureOnly");
    }
}
