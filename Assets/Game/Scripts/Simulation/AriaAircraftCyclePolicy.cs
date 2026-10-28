namespace Game.Simulation
{
    // Pure projection of AriaSkirmishPlanSystem.StepAircraftCycle. Adapters own
    // public control identities and must map Unavailable exactly like Target().
    public enum AriaAircraftCycleAction { None, Waiting, CloseDrawer, ToggleSelected, SelectSlot, NextPage, OpenActions, ReturnAircraft }
    public enum AriaAircraftCycleIntent { None, ReturnAircraft, ServiceAircraft }
    public struct AriaAircraftCycleView
    {
        public bool AirProfile, PadPresent, DrawerOpen, SelectedAircraft;
        public bool CloseDrawerAvailable, ToggleAvailable, NextPageAvailable, OpenActionsAvailable, ReturnAvailable;
        public int Active, Live, Landed, Fuel, SelectedGroups, AirMask, AvailableSquadMask, Actions;
        public float Time;
    }
    public struct AriaAircraftCycleState
    {
        public int Stage, Action, Count, AssaultIssued, PagesOrdered, StructureOrdered;
        public float SortieObservedAt, Deadline, ServiceReadyAt, ObserveUntil;
    }
    public struct AriaAircraftCycleDecision
    {
        public AriaAircraftCycleAction Action;
        public AriaAircraftCycleIntent Intent;
        public int Slot;
    }
    public static class AriaAircraftCyclePolicy
    {
        public static bool Step(in AriaAircraftCycleView v, ref AriaAircraftCycleState p, out AriaAircraftCycleDecision d)
        {
            d = default;
            if (!v.AirProfile || p.Count != 0) return false;
            if (p.Stage == 0)
            {
                if (v.Active == 0 || p.AssaultIssued == 0) { p.SortieObservedAt = 0; return false; }
                if (p.SortieObservedAt == 0) p.SortieObservedAt = v.Time;
                if (v.Time - p.SortieObservedAt < 45f) return false;
                p.Stage = 1; p.Deadline = v.Time + 120f;
            }
            if (v.Live == 0 || !v.PadPresent || v.Time >= p.Deadline)
            { p.Stage = 0; p.Count = 2; return false; }
            if (p.Stage == 1)
            {
                d.Intent = AriaAircraftCycleIntent.ReturnAircraft;
                if (v.DrawerOpen) { d.Action = AriaAircraftCycleAction.CloseDrawer; return true; }
                if (v.SelectedAircraft && v.SelectedGroups <= 1) p.Stage = 2;
                else
                {
                    if (v.SelectedGroups > 0) { d.Action = v.ToggleAvailable ? AriaAircraftCycleAction.ToggleSelected : AriaAircraftCycleAction.Waiting; return true; }
                    for (int slot = 0; slot < 5; slot++)
                        if ((v.AirMask & v.AvailableSquadMask & (1 << slot)) != 0)
                        { d.Action = AriaAircraftCycleAction.SelectSlot; d.Slot = slot; return true; }
                    d.Action = v.NextPageAvailable ? AriaAircraftCycleAction.NextPage : AriaAircraftCycleAction.Waiting;
                    return true;
                }
            }
            if (p.Stage == 2)
            {
                d.Intent = AriaAircraftCycleIntent.ReturnAircraft;
                if (!v.SelectedAircraft) { p.Stage = 1; return true; }
                if (!v.ReturnAvailable) { d.Action = v.OpenActionsAvailable ? AriaAircraftCycleAction.OpenActions : AriaAircraftCycleAction.Waiting; return true; }
                p.Action = v.Actions; p.Stage = 3; d.Action = AriaAircraftCycleAction.ReturnAircraft; return true;
            }
            if (p.Stage == 3)
            {
                d.Intent = AriaAircraftCycleIntent.ReturnAircraft;
                if (v.Actions <= p.Action) { d.Action = v.ReturnAvailable ? AriaAircraftCycleAction.ReturnAircraft : AriaAircraftCycleAction.Waiting; return true; }
                if (v.Active == 0 && v.Landed > 0) { p.Stage = 4; p.ServiceReadyAt = v.Time + 5f; }
                else { d.Action = AriaAircraftCycleAction.Waiting; return true; }
            }
            d.Intent = AriaAircraftCycleIntent.ServiceAircraft;
            if (v.Time < p.ServiceReadyAt || v.Fuel <= 0) { d.Action = AriaAircraftCycleAction.Waiting; return true; }
            p.Stage = 0; p.Count = 1; p.AssaultIssued = 0; p.PagesOrdered = 0; p.StructureOrdered = 0; p.ObserveUntil = v.Time + 1f;
            d.Action = AriaAircraftCycleAction.Waiting; return true;
        }
    }
}
