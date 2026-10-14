using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class SkirmishEnemyCommanderService
    {
        public static SkirmishStrategyScore Evaluate(EntityManager em, Entity session,
            SkirmishResolvedSetup setup, SkirmishArmyProfileConfig army)
        {
            if (setup.EnemyPolicy == null || !SkirmishEnemyPlannerService.TryReadActiveClock(em, session, out float now) ||
                !SkirmishEnemyPlannerService.EnsureSession(em, session, setup)) return default;
            SkirmishEnemyEconomicCoordinator.Reconcile(em,session);
            var policy = setup.EnemyPolicy;
            var state = em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
            if (!SkirmishEnemyDecisionRules.ObserveDue(now, state.NextObservationAt, state.ResponsePending != 0, state.ResponseDueAt)) return Project(state);
            state.NextObservationAt = now + policy.ObservationCadenceSeconds;
            var facts = SkirmishEnemyPerceptionBuilder.Build(em, session, setup);
            var ownLiving = SkirmishEnemyCapabilityService.ReadLivingRoleCounts(em,session);
            SkirmishEnemyCapabilityService.RemoveReservedGroundGroups(em,session,facts);
            if (!facts.HomeAlive || !facts.ObjectiveAlive)
            {
                state.CurrentPlan = SkirmishEnemyPlanKind.None;
                em.SetComponentData(session, state);
                return Project(state);
            }
            bool changed = state.LastObservationHash != 0 && state.LastObservationHash != facts.EventHash;
            if (changed && state.ResponsePending == 0)
            {
                state.ResponseDueAt = now + SkirmishEnemyPlannerService.RandomRange(ref state.RandomState,
                    policy.ResponseMinSeconds, policy.ResponseMaxSeconds);
                state.ResponsePending = 1;
            }
            state.LastObservationHash = facts.EventHash;
            if (!SkirmishEnemyDecisionRules.Due(now, state.NextDecisionAt, state.ResponsePending != 0, state.ResponseDueAt))
            { em.SetComponentData(session, state); return Project(state); }
            state.ResponsePending = 0;
            state.NextDecisionAt = now + policy.StrategicCadenceSeconds;
            state.LastDecisionAt = now; state.DecisionCount++;
            bool takingDamage = state.LastHomeHealth > 0 && facts.HomeHealth < state.LastHomeHealth;
            state.LastHomeHealth = facts.HomeHealth;
            bool localThreat = facts.LocalThreatSupply >= 4 || takingDamage;
            if (localThreat) state.HomeThreatClearedAt = now + policy.StrategicCadenceSeconds * 2;
            bool defend = localThreat || (state.CurrentPlan == SkirmishEnemyPlanKind.DefendHome && now < state.HomeThreatClearedAt);
            bool emergency = takingDamage && facts.HomeMaximum > 0 && facts.HomeHealth * 3 < facts.HomeMaximum;
            var package = SkirmishEnemyGroupAllocation.Allocate(facts, (int)(policy.ReserveSupplyFraction * 100));
            var rally = new float3(setup.EnemyRallyPadX, 0, setup.EnemyRallyPadZ);
            bool assembled = package.Viable && Assembled(package, rally);
            bool openingDone = now >= state.OpeningUntil;
            var desired = SkirmishEnemyPlanKind.Hold;
            int desiredScore = 0;
            foreach (var candidate in new[] { SkirmishEnemyPlanKind.DefendHome, SkirmishEnemyPlanKind.Assemble,
                SkirmishEnemyPlanKind.Assault, SkirmishEnemyPlanKind.Reinforce })
            {
                if (!Valid(candidate,package,defend,openingDone,assembled)) continue;
                if (candidate == SkirmishEnemyPlanKind.Assault && !assembled && state.CurrentPlan != SkirmishEnemyPlanKind.Assault) continue;
                int value = Utility(candidate,facts,package,defend,openingDone);
                if (value > desiredScore) { desired = candidate; desiredScore = value; }
            }
            int currentScore = Utility(state.CurrentPlan, facts, package, defend, openingDone);
            bool currentValid = Valid(state.CurrentPlan, package, defend, openingDone, assembled);
            if (desired == state.CurrentPlan || SkirmishEnemyDecisionRules.ShouldSwitch(currentScore, currentValid,
                desiredScore, policy.PlanHysteresisScore, emergency))
            {
                if (desired != state.CurrentPlan) { state.PlanRevision++; state.PlanStartedAt = now; }
                state.CurrentPlan = desired; state.CurrentScore = desiredScore;
            }
            else state.CurrentScore = currentScore;

            int capabilityDispatched;
            var capabilityRole = SkirmishEnemyCapabilityService.Tick(em,session,setup,army,facts,package,defend,now,ref state,out capabilityDispatched,defend?math.max(0,policy.MaxDispatchPerDecision-1):policy.MaxDispatchPerDecision);
            var capabilityGoal = SkirmishEnemyCapabilityService.ProgressionGoal(
                SkirmishEnemyCapabilityService.ReadOwnActors(em,session),facts,package,defend,setup.DeadlineSeconds-now,army);
            var wanted = WantedRole(facts, package, setup,ownLiving);
            // Ground defense, replacement and viable screened assault remain first.
            if (wanted == SkirmishRoleKind.None) wanted = capabilityRole;
            // Invest in a supported native ground counter only from legal visible
            // armored pressure and a viable screened package, never hidden state.
            var readinessGoal = package.Viable && package.Reserve.Count > 0 &&
                facts.Visible.Exists(c => !c.Structure && c.Category == SkirmishPopulationCategory.Ground &&
                    (c.Role == SkirmishRoleKind.Tank || c.Role == SkirmishRoleKind.ApcHeavy || c.Role == SkirmishRoleKind.Siege)) &&
                !facts.Groups.Exists(g => g.Role == SkirmishRoleKind.Tank)
                ? SkirmishRoleKind.Tank : SkirmishRoleKind.None;
            if (readinessGoal == SkirmishRoleKind.None && WantedRole(facts,package,setup,ownLiving) == SkirmishRoleKind.None)
                readinessGoal = capabilityGoal;
            var economic = SkirmishEnemyEconomicCoordinator.Tick(em,session,wanted,
                localThreat || takingDamage,readinessGoal,ref state,capabilityGoal);
            var economy = economic.Economy;
            if (economy.SuggestedCombatRole != SkirmishRoleKind.None) wanted = economy.SuggestedCombatRole;
            if (economy.PermitCombatPurchase && wanted != SkirmishRoleKind.None)
            {
                var intent = em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
                if (MayStartProduction(intent, wanted) &&
                    SkirmishEnemyPaidProductionAdapter.CanDispatchNow(em, session, wanted))
                {
                    // A distinct purchase gets a persisted sequence identity. Income,
                    // a plan rescore, and hostile motion cannot rename rejected work.
                    var key = new FixedString64Bytes("enemy.purchase." + intent.NextRequestId);
                    SkirmishEnemyPaidProductionAdapter.TryRequest(em, session, key, wanted, state.PlanRevision, army, now);
                }
            }
            var defenders = AllocateDefenders(facts, package, state.CurrentPlan == SkirmishEnemyPlanKind.DefendHome);
            int dispatched = capabilityDispatched;
            var capabilityReserved = SkirmishEnemyCapabilityService.ReservedGroups(em,session);
            bool scoutDue = now >= state.NextScoutAt;
            bool scoutDispatched = false;
            var assignments = em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session);
            for (int i = assignments.Length - 1; i >= 0; i--)
                if (!facts.Groups.Exists(g => g.Id == assignments[i].GroupId)) assignments.RemoveAt(i);
            // Do not hold a DynamicBuffer across structural production or orders.
            foreach (var group in facts.Groups)
            {
                if (capabilityReserved.Contains(group.Id)) continue;
                var duty = Duty(group, package, state.CurrentPlan);
                var assignment = ReadAssignment(em, session, group.Id);
                if (assignment.Retries >= policy.MaxIdenticalRejects)
                {
                    duty = SkirmishEnemyDuty.Recover;
                    // Time spent near the same blocked destination is not evidence
                    // of recovery. Only real own progress in Issue resets retries.
                }
                if (duty == SkirmishEnemyDuty.Scout && !scoutDue)
                {
                    var existing = ReadAssignment(em, session, group.Id);
                    if (existing.GroupId == 0 || existing.Duty != SkirmishEnemyDuty.Scout) duty = SkirmishEnemyDuty.HomeReserve;
                }
                if (!SkirmishEnemyGroupAllocation.Healthy(group)) duty = SkirmishEnemyDuty.Recover;
                float3 destination = rally;
                Entity target = Entity.Null;
                FixedString64Bytes targetId = default;
                var order = SkirmishGroupOrderKind.Move;
                bool assault = duty == SkirmishEnemyDuty.MainAssault || duty == SkirmishEnemyDuty.Escort || duty == SkirmishEnemyDuty.Flank;
                if (defenders.Contains(group.Id) && duty != SkirmishEnemyDuty.Recover)
                {
                    if (TryVisibleTarget(facts, group, true, out var contact))
                    { target = contact.Entity; targetId = contact.StableId; destination = contact.Position; order = SkirmishGroupOrderKind.Attack; }
                    else destination = facts.Home;
                }
                else if (assault && (state.CurrentPlan == SkirmishEnemyPlanKind.Assault ||
                    (state.CurrentPlan == SkirmishEnemyPlanKind.DefendHome && now >= state.OpeningUntil &&
                     (assignment.Duty == SkirmishEnemyDuty.MainAssault || assignment.Duty == SkirmishEnemyDuty.Escort || assignment.Duty == SkirmishEnemyDuty.Flank))))
                {
                    if (TryVisibleTarget(facts, group, false, out var contact, state.CurrentPlan == SkirmishEnemyPlanKind.DefendHome))
                    { target = contact.Entity; targetId = contact.StableId; destination = contact.Position; order = SkirmishGroupOrderKind.Attack; }
                    else destination = RouteDestination(policy, facts, group, ref assignment, false, setup);
                }
                else if (duty == SkirmishEnemyDuty.Scout)
                    destination = RouteDestination(policy, facts, group, ref assignment, true, setup);
                else if (duty == SkirmishEnemyDuty.HomeReserve || duty == SkirmishEnemyDuty.Recover)
                    destination = rally;
                if (order == SkirmishGroupOrderKind.Move && math.distancesq(group.Position.xz, destination.xz) <= 8f * 8f)
                    order = SkirmishGroupOrderKind.Hold;
                if (dispatched < policy.MaxDispatchPerDecision && now >= assignment.RetryAfter)
                    if (Issue(em, session, group, duty, order, target, targetId, destination, now, ref state, ref assignment, policy))
                    {
                        dispatched++;
                        if (duty == SkirmishEnemyDuty.Scout && assignment.OrderAccepted != 0) scoutDispatched = true;
                    }
                StoreAssignment(em, session, assignment);
            }
            if (scoutDue && scoutDispatched)
                state.NextScoutAt = now + SkirmishEnemyPlannerService.RandomRange(ref state.RandomState,
                    policy.ScoutMinSeconds, policy.ScoutMaxSeconds);
            em.SetComponentData(session, state);
            var legacy = em.GetComponentData<SkirmishEnemyStrategyComponent>(session);
            var projection = Project(state);
            legacy.Priority = projection.Priority; legacy.LastScore = state.CurrentScore;
            legacy.RecruitRole = wanted; em.SetComponentData(session, legacy);
            return projection;
        }

        public static HashSet<uint> AllocateDefenders(SkirmishEnemyPerceptionBuilder.Snapshot facts,
            SkirmishEnemyGroupAllocation.Package package, bool active)
        {
            var ids = new HashSet<uint>();
            if (!active) return ids;
            var candidates = new List<SkirmishEnemyPerceptionBuilder.Group>(facts.Groups);
            candidates.Sort((a,b) =>
            {
                int reserve = package.Reserve.Contains(b).CompareTo(package.Reserve.Contains(a));
                if (reserve != 0) return reserve;
                int distance = math.distancesq(a.Position.xz,facts.Home.xz).CompareTo(math.distancesq(b.Position.xz,facts.Home.xz));
                return distance != 0 ? distance : a.Id.CompareTo(b.Id);
            });
            int supply = 0;
            int needed = math.max(4, facts.LocalThreatSupply);
            foreach (var group in candidates)
            {
                if (!TryVisibleTarget(facts, group, true, out _)) continue;
                ids.Add(group.Id); supply += group.Supply;
                if (supply >= needed) break;
            }
            return ids;
        }
        public static bool MayStartProduction(in SkirmishEnemyProductionIntentV1Component intent,
            SkirmishRoleKind wanted)
        {
            if (SkirmishEnemyPaidProductionAdapter.IsOutstanding(intent)) return false;
            // A feasibility rejection can be reconsidered only after the adapter's
            // exact authoritative preflight passes. Native/correlation abandonment
            // remains abandoned for this role: an apparently valid preflight alone
            // cannot prove a missing payment/receipt witness is safe to replay.
            return intent.State != SkirmishEnemyProductionIntentState.Abandoned ||
                intent.Role != wanted || intent.RejectDomain == (byte)SkirmishEnemyRejectDomain.Feasibility;
        }
        private static bool Assembled(SkirmishEnemyGroupAllocation.Package package, float3 rally)
        {
            foreach (var group in package.Assault) if (math.distancesq(group.Position.xz, rally.xz) > 25f*25f) return false;
            foreach (var group in package.Escort) if (math.distancesq(group.Position.xz, rally.xz) > 25f*25f) return false;
            return true;
        }
        private static bool Valid(SkirmishEnemyPlanKind plan, SkirmishEnemyGroupAllocation.Package package,
            bool defend, bool opening, bool assembled) => plan switch
        {
            SkirmishEnemyPlanKind.DefendHome => defend,
            SkirmishEnemyPlanKind.Assault => package.Viable && opening,
            SkirmishEnemyPlanKind.Assemble => package.Viable && !defend && (!opening || !assembled),
            SkirmishEnemyPlanKind.Reinforce => !package.Viable,
            _ => false
        };
        private static int Utility(SkirmishEnemyPlanKind plan, SkirmishEnemyPerceptionBuilder.Snapshot facts,
            SkirmishEnemyGroupAllocation.Package package, bool defend, bool opening) => plan switch
        {
            SkirmishEnemyPlanKind.DefendHome => defend ? 100 : -1000,
            SkirmishEnemyPlanKind.Assault => package.Viable && opening ? 40 : -1000,
            SkirmishEnemyPlanKind.Assemble => package.Viable ? 20 : -1000,
            SkirmishEnemyPlanKind.Reinforce => !package.Viable ? 40 : 0,
            SkirmishEnemyPlanKind.Hold => 0,
            _ => -1000
        };
        private static SkirmishEnemyDuty Duty(SkirmishEnemyPerceptionBuilder.Group group,
            SkirmishEnemyGroupAllocation.Package package, SkirmishEnemyPlanKind plan)
        {
            if (package.Reserve.Contains(group)) return SkirmishEnemyDuty.HomeReserve;
            if (package.Scout == group) return SkirmishEnemyDuty.Scout;
            if (package.Assault.Contains(group)) return SkirmishEnemyDuty.MainAssault;
            return SkirmishEnemyDuty.Escort;
        }
        public static SkirmishRoleKind WantedRole(SkirmishEnemyPerceptionBuilder.Snapshot facts,
            SkirmishEnemyGroupAllocation.Package package, SkirmishResolvedSetup setup,
            IReadOnlyDictionary<SkirmishRoleKind,int> ownLiving = null)
        {
            if (facts.Visible.Exists(c => c.Category == SkirmishPopulationCategory.Air) &&
                !facts.Groups.Exists(g => g.Role == SkirmishRoleKind.AntiAir)) return SkirmishRoleKind.AntiAir;
            if (!facts.Groups.Exists(g => g.StructureCapable && SkirmishEnemyGroupAllocation.Healthy(g))) return SkirmishRoleKind.Rocketeer;
            if (!package.Viable) return SkirmishRoleKind.Rifle;
            if (facts.Visible.Exists(c => c.Role == SkirmishRoleKind.Tank) &&
                facts.Groups.FindAll(g => g.Role == SkirmishRoleKind.Rocketeer).Count < 2) return SkirmishRoleKind.Rocketeer;
            // Replenish the authored combat package, subject to current real
            // costs, producers and caps. This does not create free reserve units.
            foreach (var force in setup.Forces)
            {
                if (force.FactionId != setup.EnemyFaction ||
                    (force.RoleKind != SkirmishRoleKind.Rifle && force.RoleKind != SkirmishRoleKind.Gunner &&
                     force.RoleKind != SkirmishRoleKind.Rocketeer && force.RoleKind != SkirmishRoleKind.Tank)) continue;
                int living = 0;
                foreach (var group in facts.Groups) if (group.Role == force.RoleKind) living += group.Members;
                if (ownLiving != null) living = ownLiving.TryGetValue(force.RoleKind,out int ownCount) ? ownCount : 0;
                if (living < force.Quantity) return force.RoleKind;
            }
            // Acquire a real reconnaissance packet only after retaining the supported
            // assault, its screen and a genuine reserve. The paid adapter still owns
            // costs, research, producer availability and population feasibility.
            if (package.Viable && package.Reserve.Count > 0 && package.Scout == null) return SkirmishRoleKind.Rifle;
            return SkirmishRoleKind.None;
        }
        public static bool TryVisibleTarget(SkirmishEnemyPerceptionBuilder.Snapshot facts,
            SkirmishEnemyPerceptionBuilder.Group group, bool homeOnly, out SkirmishEnemyPerceptionBuilder.Contact chosen, bool excludeHome = false)
        {
            chosen = default; float best = float.MaxValue; bool found = false;
            foreach (var contact in facts.Visible)
            {
                if (!SkirmishExpandedEngagementService.DomainAllows(group.Domains, contact.Structure, contact.Category)) continue;
                float localDistance = math.distancesq(contact.Position.xz, facts.Home.xz);
                bool homeContact = localDistance <= 90f*90f || facts.OwnFacilities.Exists(
                    p => math.distancesq(p.xz, contact.Position.xz) <= 55f*55f);
                if ((homeOnly && !homeContact) || (excludeHome && homeContact)) continue;
                float distance = math.distancesq(group.Position.xz, contact.Position.xz);
                if (distance >= best) continue;
                // Escorts stay with the assault instead of chasing distant contacts.
                if (!homeOnly && !contact.Structure && distance > 90f*90f) continue;
                best = distance; chosen = contact; found = true;
            }
            return found;
        }
        public static float3 RouteDestination(SkirmishEnemyPolicySnapshot policy,
            SkirmishEnemyPerceptionBuilder.Snapshot facts, SkirmishEnemyPerceptionBuilder.Group group,
            ref SkirmishEnemyGroupAssignmentV1 assignment, bool scout, SkirmishResolvedSetup setup)
        {
            SkirmishEnemyRouteSnapshot route = null;
            float bestCost = float.MaxValue;
            foreach (var candidate in policy.Routes)
            {
                if (candidate.Waypoints.Count == 0 || candidate.WidthMetres < (group.Category == SkirmishPopulationCategory.Ground ? 3f : 1f) ||
                    (group.Category == SkirmishPopulationCategory.Ground && !candidate.AllowsVehicles)) continue;
                if (candidate.RouteId == assignment.RouteId.ToString() && assignment.Retries == 0) { route = candidate; break; }
                float transit = group.Category == SkirmishPopulationCategory.Ground
                    ? candidate.GroundTransitSeconds : candidate.InfantryTransitSeconds;
                float cost = transit > 0 ? transit : candidate.LengthMetres;
                // Known exposure uses only legal current observations and frozen
                // last-seen points; it never samples hidden dynamic occupancy.
                int exposed = 0;
                foreach (var exposureWaypoint in candidate.Waypoints)
                {
                    float2 exposurePoint = new(exposureWaypoint.X, exposureWaypoint.Z);
                    foreach (var contact in facts.Visible)
                        if (math.distancesq(exposurePoint,contact.Position.xz) < 60f*60f) exposed += 2;
                    foreach (var lastSeen in facts.LastSeen)
                        if (math.distancesq(exposurePoint,lastSeen.xz) < 60f*60f) exposed++;
                }
                cost += math.min(30,exposed*3);
                if (assignment.Retries > 0 && candidate.RouteId == assignment.RouteId.ToString()) cost += 1000;
                // A bounded personality preference changes a tie, not feasibility.
                if (scout && candidate.Kind != SkirmishMeasuredRouteKind.MainHighway) cost -= 3;
                if (policy.Personality == 1 && candidate.Kind == SkirmishMeasuredRouteKind.FlankSouthSweep) cost -= 2;
                if (policy.Personality == 2 && candidate.Kind == SkirmishMeasuredRouteKind.FlankNorthRuins) cost -= 2;
                if (cost < bestCost) { route = candidate; bestCost = cost; }
            }
            if (route == null) return new float3(setup.EnemyRallyPadX, 0, setup.EnemyRallyPadZ);
            int last = route.Waypoints.Count - 1;
            float3 firstPoint = new(route.Waypoints[0].X, 0, route.Waypoints[0].Z);
            float3 lastPoint = new(route.Waypoints[last].X, 0, route.Waypoints[last].Z);
            int direction = math.distancesq(firstPoint.xz, facts.Home.xz) <= math.distancesq(lastPoint.xz, facts.Home.xz) ? 1 : -1;
            if (assignment.RouteId.ToString() != route.RouteId)
            {
                assignment.RouteId = new FixedString64Bytes(route.RouteId);
                assignment.RouteWaypointIndex = direction > 0 ? 0 : last;
            }
            // Count is the persisted completed-route sentinel, independent of
            // direction. Continue and later centroid movement cannot erase it.
            bool completed = !scout && assignment.RouteWaypointIndex == route.Waypoints.Count;
            int index = math.clamp(assignment.RouteWaypointIndex, 0, last);
            var waypoint = route.Waypoints[index];
            if (math.distancesq(group.Position.xz, new float2(waypoint.X, waypoint.Z)) <= 12f*12f)
                index = math.clamp(index + direction, 0, last);
            if (scout)
            {
                // Patrol the nearest third of an authored approach, keeping a
                // scout from becoming an unsupported full-map base assault.
                var scoutPoint = SkirmishEnemyDecisionRules.ScoutWaypoint(route, direction, index, out index);
                assignment.RouteWaypointIndex = index;
                // Investigate only current-session frozen observations inside the
                // bounded patrol reach. Never resolve a hidden live target Entity.
                float reach = 0;
                for (int i = 1; i <= last; i++)
                    reach += math.distance(new float2(route.Waypoints[i-1].X,route.Waypoints[i-1].Z),
                        new float2(route.Waypoints[i].X,route.Waypoints[i].Z));
                var endpoint = direction > 0 ? firstPoint : lastPoint;
                return SkirmishEnemyDecisionRules.InvestigationDestination(endpoint, reach / 3f,
                    scoutPoint, group.Position, facts.LastSeen);
            }
            assignment.RouteWaypointIndex = index;
            waypoint = route.Waypoints[index];
            var point = new float3(waypoint.X, 0, waypoint.Z);
            bool terminal = index == (direction > 0 ? last : 0);
            if (terminal && math.distancesq(group.Position.xz, point.xz) <= 12f*12f) completed = true;
            if (completed) assignment.RouteWaypointIndex = route.Waypoints.Count;
            // Route staging is not an attack destination. Completion remains
            // persisted until route replacement/recovery resets its index. Advance
            // only to public objective terrain through the native Move validator.
            return completed ? new float3(setup.PlayerBaseWorldX, 0, setup.PlayerBaseWorldZ) : point;
        }
        private static bool Issue(EntityManager em, Entity session, SkirmishEnemyPerceptionBuilder.Group group,
            SkirmishEnemyDuty duty, SkirmishGroupOrderKind order, Entity target, FixedString64Bytes targetId,
            float3 destination, float now, ref SkirmishEnemyPlannerV1Component state,
            ref SkirmishEnemyGroupAssignmentV1 assignment, SkirmishEnemyPolicySnapshot policy)
        {
            bool sameDestination = SkirmishEnemyDecisionRules.SameDestination(
                assignment.DestinationX,assignment.DestinationZ,destination.x,destination.z,3f);
            bool moveGoalChanged = assignment.GroupId == 0 || assignment.Order != order ||
                (order == SkirmishGroupOrderKind.Move && !sameDestination);
            bool changed = assignment.GroupId == 0 || assignment.Duty != duty || assignment.Order != order ||
                !assignment.TargetStableId.Equals(targetId) || (order != SkirmishGroupOrderKind.Attack && !sameDestination);
            if (assignment.GroupId == 0)
            {
                assignment.GroupId = group.Id; assignment.AssignedAt = now;
            }
            // A different native Move waypoint/order starts its own bounded
            // observation window. Duty/action-label changes alone do not.
            if (moveGoalChanged && assignment.Retries <= policy.MaxIdenticalRejects)
            {
                assignment.LastProgressAt = now; assignment.LastProgressX = group.Position.x; assignment.LastProgressZ = group.Position.z;
            }
            else if (assignment.Order == SkirmishGroupOrderKind.Move && SkirmishEnemyDecisionRules.TowardMoveGoalProgressed(
                assignment.LastProgressX,assignment.LastProgressZ,group.Position.x,group.Position.z,
                assignment.DestinationX,assignment.DestinationZ,2f))
            {
                assignment.LastProgressAt = now; assignment.LastProgressX = group.Position.x;
                assignment.LastProgressZ = group.Position.z; assignment.Retries = 0;
            }
            // After the bounded route retries and one recovery command fail, wait
            // for progress toward the current Move goal (or a fresh group).
            // Do not wrap a byte or spend dispatch slots on an impossible pad.
            if (assignment.Retries > policy.MaxIdenticalRejects) return false;
            // Accepted Hold and Attack are legitimate stationary states. Shared
            // tactical combat keeps firing and validating its own target normally.
            if (assignment.OrderAccepted != 0 && (!changed || (order == SkirmishGroupOrderKind.Move && !moveGoalChanged)))
            {
                if (order != SkirmishGroupOrderKind.Move) return false;
                if (now - assignment.LastProgressAt < 10f)
                {
                    if (!changed) return false;
                }
                else
                {
                    assignment.OrderAccepted = 0;
                    assignment.Retries = (byte)Math.Min(policy.MaxIdenticalRejects + 1, assignment.Retries + 1);
                    if (assignment.Retries >= policy.MaxIdenticalRejects)
                    {
                        assignment.RetryAfter = now + policy.RejectBackoffSeconds * 3;
                        assignment.Duty = SkirmishEnemyDuty.Recover;
                        assignment.RouteId = default; return false;
                    }
                    assignment.RetryAfter = now + policy.RejectBackoffSeconds;
                    return false;
                }
            }
            if (changed)
            {
                assignment.Revision = state.PlanRevision;
                assignment.ActionKey = ++state.NextActionKey;
                assignment.Duty = duty; assignment.Order = order; assignment.TargetStableId = targetId;
                assignment.DestinationX = destination.x; assignment.DestinationZ = destination.z;
                assignment.OrderAccepted = 0;
            }
            bool accepted = SkirmishArmyCommandService.TryIssueGroupOrder(em, session, group.Id, 2, order, target, destination, out _);
            assignment.OrderAccepted = accepted ? (byte)1 : (byte)0;
            assignment.LastIssuedAt = now;
            if (!accepted)
            {
                assignment.Retries = (byte)Math.Min(policy.MaxIdenticalRejects + 1, assignment.Retries + 1); assignment.RetryAfter = now + policy.RejectBackoffSeconds;
                if (assignment.Retries >= policy.MaxIdenticalRejects) assignment.RetryAfter += policy.RejectBackoffSeconds * 2;
            }
            return true;
        }
        private static SkirmishEnemyGroupAssignmentV1 ReadAssignment(EntityManager em, Entity session, uint groupId)
        {
            var buffer = em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session);
            for (int i = 0; i < buffer.Length; i++) if (buffer[i].GroupId == groupId) return buffer[i];
            return default;
        }
        private static void StoreAssignment(EntityManager em, Entity session, SkirmishEnemyGroupAssignmentV1 assignment)
        {
            if (assignment.GroupId == 0) return;
            var buffer = em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session);
            for (int i = 0; i < buffer.Length; i++) if (buffer[i].GroupId == assignment.GroupId) { buffer[i] = assignment; return; }
            buffer.Add(assignment);
        }
        private static SkirmishStrategyScore Project(SkirmishEnemyPlannerV1Component state) => new()
        {
            Priority = state.CurrentPlan == SkirmishEnemyPlanKind.DefendHome ? SkirmishStrategyPriority.DefendHome :
                state.CurrentPlan == SkirmishEnemyPlanKind.Assault ? SkirmishStrategyPriority.AttackBase :
                state.CurrentPlan == SkirmishEnemyPlanKind.Reinforce ? SkirmishStrategyPriority.RecruitCounter :
                state.CurrentPlan == SkirmishEnemyPlanKind.None ? SkirmishStrategyPriority.None : SkirmishStrategyPriority.Reserve,
            Total = state.CurrentScore, Field = "enemy.policy.v1." + state.CurrentPlan
        };
    }
}
