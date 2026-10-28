using Game.Simulation;
public enum Intent { Initial, ReturnAircraft, ServiceAircraft }
public enum Kind { Initial, Waiting, Unavailable, Control }
public struct TargetValue {public bool Available;public int Id;}
public struct View {public bool AirProfile,PadPresent,DrawerOpen,SelectedAircraft;public int OwnAttackAirActive,OwnAttackAirLive,OwnAttackAirLanded,OwnAirFuel,ExpandedSelectedGroups,ExpandedAirMask;public float Time;public TargetValue CloseDrawer,SelectedSquadToToggle,NextSquadPage,OpenAircraftActions,ReturnAircraft,S0,S1,S2,S3,S4;public TargetValue Squad(int i)=>i==0?S0:i==1?S1:i==2?S2:i==3?S3:S4;}
public struct Plan {public int AirCycleCount,AirCycleStage,AirCycleAction,AssaultIssued,PagesOrdered,StructureOrdered;public float AirSortieObservedAt,AirCycleDeadline,AirServiceReadyAt,ObserveUntil;public Intent Intent;}
public struct Touch {public int Actions;}
public struct Output {public Kind Kind;public int Id;}
public static class Original {static void Target(TargetValue t,bool world,ref Output o){o.Kind=t.Available?Kind.Control:Kind.Unavailable;o.Id=t.Available?t.Id:0;}static bool TryDeselectSquads(in View v,ref Output o){if(v.ExpandedSelectedGroups<=0)return false;if(v.SelectedSquadToToggle.Available)Target(v.SelectedSquadToToggle,false,ref o);else o.Kind=Kind.Waiting;return true;}
        public static bool StepAircraftCycle(in View view,
            ref Plan plan, ref Touch touch,
            ref Output output)
        {
            if (!view.AirProfile || plan.AirCycleCount != 0) return false;
            if (plan.AirCycleStage == 0)
            {
                if (view.OwnAttackAirActive == 0 || plan.AssaultIssued == 0)
                { plan.AirSortieObservedAt = 0; return false; }
                if (plan.AirSortieObservedAt == 0) plan.AirSortieObservedAt = view.Time;
                if (view.Time - plan.AirSortieObservedAt < 45f) return false;
                plan.AirCycleStage = 1;
                plan.AirCycleDeadline = view.Time + 120f;
            }
            if (view.OwnAttackAirLive == 0 || !view.PadPresent || view.Time >= plan.AirCycleDeadline)
            {
                plan.AirCycleStage = 0;
                plan.AirCycleCount = 2; // Bounded recovery; normal assault remains available.
                return false;
            }
            if (plan.AirCycleStage == 1)
            {
                plan.Intent = Intent.ReturnAircraft;
                if (view.DrawerOpen) { Target(view.CloseDrawer, false, ref output); return true; }
                if (view.SelectedAircraft && view.ExpandedSelectedGroups <= 1) { plan.AirCycleStage = 2; }
                else
                {
                    if (TryDeselectSquads(view, ref output)) return true;
                    for (int slot = 0; slot < 5; slot++)
                        if ((view.ExpandedAirMask & (1 << slot)) != 0 && view.Squad(slot).Available)
                        { Target(view.Squad(slot), false, ref output); return true; }
                    if (TryDeselectSquads(view, ref output)) return true;
                    if (view.NextSquadPage.Available)
                        Target(view.NextSquadPage, false, ref output);
                    else output.Kind = Kind.Waiting;
                    return true;
                }
            }
            if (plan.AirCycleStage == 2)
            {
                plan.Intent = Intent.ReturnAircraft;
                if (!view.SelectedAircraft) { plan.AirCycleStage = 1; return true; }
                if (!view.ReturnAircraft.Available)
                {
                    if (view.OpenAircraftActions.Available) Target(view.OpenAircraftActions, false, ref output);
                    else output.Kind = Kind.Waiting;
                    return true;
                }
                plan.AirCycleAction = touch.Actions;
                plan.AirCycleStage = 3;
                Target(view.ReturnAircraft, false, ref output);
                return true;
            }
            if (plan.AirCycleStage == 3)
            {
                plan.Intent = Intent.ReturnAircraft;
                if (touch.Actions <= plan.AirCycleAction)
                {
                    if (view.ReturnAircraft.Available) Target(view.ReturnAircraft, false, ref output);
                    else output.Kind = Kind.Waiting;
                    return true;
                }
                if (view.OwnAttackAirActive == 0 && view.OwnAttackAirLanded > 0)
                {
                    plan.AirCycleStage = 4;
                    plan.AirServiceReadyAt = view.Time + 5f;
                }
                else { output.Kind = Kind.Waiting; return true; }
            }
            plan.Intent = Intent.ServiceAircraft;
            if (view.Time < plan.AirServiceReadyAt || view.OwnAirFuel <= 0)
            { output.Kind = Kind.Waiting; return true; }
            // The landed aircraft is serviced by shared home/fuel owners. Allow
            // the visible squad-card attack sequence to issue a second sortie.
            plan.AirCycleStage = 0;
            plan.AirCycleCount = 1;
            plan.AssaultIssued = 0;
            plan.PagesOrdered = 0;
            plan.StructureOrdered = 0;
            plan.ObserveUntil = view.Time + 1f;
            output.Kind = Kind.Waiting;
            return true;
        }
}
