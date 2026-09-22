using System;
using System.Collections.Generic;
using Game.Operations.Contracts;

namespace Game.Operations.Tactical
{
    /// <summary>
    /// Neutral tactical rules for one attempt. Dispatch is by objective rule kind.
    /// Mission identity is stored on the result and is not a branch.
    /// Tick order: activation, deaths and destruction, movement, channels, facts, outcome, waves.
    /// </summary>
    public sealed class OperationsTacticalSession
    {
        private enum OrderKind : byte
        {
            None = 0,
            Scan = 1,
            Hold = 2,
            Interact = 3,
            Repair = 4,
            Move = 5,
            Extract = 6,
            EscortGo = 7,
            EscortHold = 8,
            Attack = 9
        }

        private enum WorldEventKind : byte
        {
            Death = 0,
            Destroy = 1,
            Move = 2
        }

        private sealed class Actor
        {
            public string ObjectId;
            public string RoleId;
            public string SessionId;
            public OperationsRosterRoleKind RosterRole;
            public OperationsTacticalFaction Faction;
            public OperationsTacticalBodyKind Body;
            public bool Alive;
            public bool Spawned;
            public bool Commandable;
            public bool OriginalInfantry;
            public int Group;
            public float X;
            public float Z;
            public float StagingX;
            public float StagingZ;
            public float HomeX;
            public float HomeZ;
            public int Health;
            public bool HasCargo;
            public bool HiddenUntilObserved;
            public bool Observed;
            public string CarriedObjectId = string.Empty;
            public int ChannelTicks;
            public int WaypointIndex;
            public int UnloadTicks;
            public bool Delivered;
            public OrderKind Order;
            public string OrderTarget = string.Empty;
            public int OrderIssuedTick = -1;
        }

        private sealed class Node
        {
            public OperationsCompiledNode Spec;
            public OperationsTacticalNodePhase Phase;
            public int ProgressTicks;
            public int ProgressCount;
            public int CompletionTick = -1;
            public bool MaterialsCharged;
            public bool Pending;
            public readonly HashSet<string> Confirmed = new(StringComparer.Ordinal);
            public int LockedRoute = -1;
        }

        private struct WorldEvent
        {
            public WorldEventKind Kind;
            public string ObjectId;
            public string AnchorId;
        }

        private sealed class Wave
        {
            public OperationsCompiledWave Spec;
            public bool Armed;
            public int ArmedTick;
            public bool Spawned;
        }

        private readonly OperationsCompiledTactical _definition;
        private readonly OperationsMapGreybox _map;
        private readonly string _missionId;
        private readonly List<Actor> _actors = new();
        private readonly List<Node> _nodes = new();
        private readonly List<Wave> _waves = new();
        private readonly List<OperationsTacticalFact> _facts = new();
        private readonly List<WorldEvent> _events = new();
        private int _tick;
        private int _materials;
        private bool _paused;
        private bool _terminal;
        private bool _released;
        private OperationsOutcomeKind _outcome = OperationsOutcomeKind.None;
        private string _reason = string.Empty;

        public OperationsTacticalSession(OperationsCompiledTactical definition, OperationsLaunchPayload launch)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (!OperationsMapGreyboxCatalog.TryGet(definition.MapId, out _map))
                throw new InvalidOperationException("missing_map:" + definition.MapId);
            _definition = definition;
            _missionId = launch.MissionId;
            _materials = definition.Materials;
            for (int index = 0; index < definition.Spawns.Length; index++)
                _actors.Add(CreateActor(definition.Spawns[index], launch.SessionId));
            for (int index = 0; index < definition.Nodes.Length; index++)
            {
                _nodes.Add(new Node
                {
                    Spec = definition.Nodes[index],
                    Phase = OperationsTacticalNodePhase.Inactive
                });
            }

            for (int index = 0; index < definition.Waves.Length; index++)
                _waves.Add(new Wave { Spec = definition.Waves[index] });

            for (int index = 0; index < _nodes.Count; index++)
            {
                if (_nodes[index].Spec.Activation != OperationsActivationPolicyKind.Launch)
                    continue;
                _nodes[index].Phase = OperationsTacticalNodePhase.Active;
                OnActivated(_nodes[index]);
            }

            EvaluateOutcome();
        }

        public int Tick => _tick;
        public int Materials => _materials;
        public bool IsPaused => _paused;
        public bool IsTerminal => _terminal;
        public bool AttemptReleased => _released;
        public OperationsOutcomeKind Outcome => _outcome;
        public string TerminalReason => _reason;
        public string MissionId => _missionId;
        public string MapId => _definition.MapId;
        public int SpawnedHostileCount => CountSpawnedHostiles();

        public void SetPaused(bool paused) => _paused = paused;

        public void Advance(int ticks)
        {
            if (ticks < 1)
                throw new ArgumentOutOfRangeException(nameof(ticks));
            for (int index = 0; index < ticks; index++)
                Step();
        }

        public bool TryGetNode(string nodeId, out OperationsTacticalNodeState state)
        {
            Node node = FindNode(nodeId);
            if (node == null)
            {
                state = default;
                return false;
            }

            string[] targets = node.Spec.TargetIds ?? Array.Empty<string>();
            var targetCopy = new string[targets.Length];
            Array.Copy(targets, targetCopy, targets.Length);
            string[] routes = Array.Empty<string>();
            if (node.Spec.LegalRoutes != null && node.Spec.LegalRoutes.Length > 0)
            {
                routes = new string[node.Spec.LegalRoutes.Length];
                for (int route = 0; route < node.Spec.LegalRoutes.Length; route++)
                    routes[route] = node.Spec.LegalRoutes[route].RouteId;
            }

            state = new OperationsTacticalNodeState(
                node.Spec.NodeId,
                node.Spec.Rule,
                node.Spec.Optional,
                node.Phase,
                node.ProgressTicks,
                node.ProgressCount,
                node.CompletionTick,
                node.MaterialsCharged,
                targetCopy,
                node.Spec.ZoneAnchorId,
                node.Spec.TargetCount,
                routes);
            return true;
        }

        public bool TryGetActor(string objectId, out OperationsTacticalActorState state)
        {
            Actor actor = FindActor(objectId);
            if (actor == null)
            {
                state = default;
                return false;
            }

            state = ToState(actor);
            return true;
        }

        public OperationsTacticalActorState[] CopyActors()
        {
            var copy = new OperationsTacticalActorState[_actors.Count];
            for (int index = 0; index < _actors.Count; index++)
                copy[index] = ToState(_actors[index]);
            return copy;
        }

        public OperationsTacticalFact[] CopyFacts()
        {
            var copy = new OperationsTacticalFact[_facts.Count];
            for (int index = 0; index < _facts.Count; index++)
                copy[index] = _facts[index];
            return copy;
        }

        public bool IsWaveArmed(int group)
        {
            Wave wave = FindWave(group);
            return wave != null && wave.Armed;
        }

        public bool IsWaveSpawned(int group)
        {
            Wave wave = FindWave(group);
            return wave != null && wave.Spawned;
        }

        public string Trace()
        {
            var parts = new List<string>
            {
                "tick=" + _tick.ToString(),
                "outcome=" + _outcome.ToString(),
                "reason=" + _reason,
                "map=" + _definition.MapId,
                "materials=" + _materials.ToString(),
                "hostiles=" + SpawnedHostileCount.ToString()
            };
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                parts.Add(node.Spec.NodeId + ":" + node.Phase + ":" + node.CompletionTick.ToString());
            }

            return string.Join(";", parts);
        }

        public OperationsTacticalCommandResult IssueObserve(string unitId, string siteId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor unit = FindActor(unitId);
            Actor site = FindActor(siteId);
            if (unit == null || site == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!IsEligibleInfantry(unit))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            if (!InRange(unit, site.X, site.Z, OperationsTacticalRules.ObserveMeters))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.OutOfRange);
            if (!HasSight(unit.X, unit.Z, site.X, site.Z))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.BlockedLineOfSight);
            site.Observed = true;
            AddFact(OperationsTacticalFactKind.Observed, site.ObjectId);
            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueScan(string unitId, string siteId) =>
            IssueChannel(unitId, siteId, OperationsObjectiveRuleKind.Scan, OrderKind.Scan, OperationsTacticalRules.ScanMeters, true);

        public OperationsTacticalCommandResult IssueInteract(string unitId, string objectId) =>
            IssueChannel(unitId, objectId, OperationsObjectiveRuleKind.Interact, OrderKind.Interact, OperationsTacticalRules.InteractMeters, false);

        public OperationsTacticalCommandResult IssueRepair(string unitId, string siteId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor unit = FindActor(unitId);
            Actor site = FindActor(siteId);
            if (unit == null || site == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!unit.Alive || !unit.Spawned || unit.RosterRole != OperationsRosterRoleKind.RepairSpecialist)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            Node node = FindRuleTarget(OperationsObjectiveRuleKind.Repair, siteId, out OperationsTacticalRejectKind failure);
            if (node == null)
                return OperationsTacticalCommandResult.Reject(failure);
            if (!site.Alive)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.SiteDestroyed);
            if (!InRange(unit, site.X, site.Z, OperationsTacticalRules.RepairMeters))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.OutOfRange);
            if (!node.MaterialsCharged && _materials < OperationsTacticalRules.RepairMaterialCost)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.InsufficientMaterials);
            unit.Order = OrderKind.Repair;
            unit.OrderTarget = siteId;
            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueHold(string unitId, string zoneAnchorId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor unit = FindActor(unitId);
            if (unit == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!IsEligibleInfantry(unit))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            Node node = FindHold(zoneAnchorId, out OperationsTacticalRejectKind failure);
            if (node == null)
                return OperationsTacticalCommandResult.Reject(failure);
            unit.Order = OrderKind.Hold;
            unit.OrderTarget = zoneAnchorId;
            unit.OrderIssuedTick = _tick;
            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueMove(string unitId, string anchorId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor unit = FindActor(unitId);
            if (unit == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!IsEligibleInfantry(unit) && unit.RosterRole != OperationsRosterRoleKind.RepairSpecialist)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            if (!_map.TryGetById(anchorId, out _))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            unit.Order = OrderKind.Move;
            unit.OrderTarget = anchorId;
            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueEscortGo(string routeId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Node node = FindEscort(routeId, out int routeIndex, out OperationsTacticalRejectKind failure);
            if (node == null)
                return OperationsTacticalCommandResult.Reject(failure);
            if (node.LockedRoute >= 0 && node.LockedRoute != routeIndex)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.RouteLocked);
            node.LockedRoute = routeIndex;
            OperationsCompiledRoute route = node.Spec.LegalRoutes[routeIndex];
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor cargo = _actors[index];
                if (!IsEscortCargo(node, cargo) || cargo.Delivered)
                    continue;
                bool starting = cargo.Order != OrderKind.EscortGo && cargo.Order != OrderKind.EscortHold;
                cargo.Order = OrderKind.EscortGo;
                cargo.OrderTarget = node.Spec.NodeId;
                if (starting)
                {
                    cargo.WaypointIndex = OperationsTacticalRules.Within(cargo.X, cargo.Z, route.Xs[0], route.Zs[0], 0.5f) ? 1 : 0;
                    cargo.UnloadTicks = 0;
                }
            }

            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueEscortHold()
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Node node = FindActive(OperationsObjectiveRuleKind.Escort);
            if (node == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NodeInactive);
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor cargo = _actors[index];
                if (!IsEscortCargo(node, cargo) || cargo.Delivered)
                    continue;
                cargo.Order = OrderKind.EscortHold;
                cargo.OrderTarget = node.Spec.NodeId;
            }

            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueExtract(string unitId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor unit = FindActor(unitId);
            if (unit == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!unit.OriginalInfantry || !unit.Alive || !unit.Spawned)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            unit.Order = OrderKind.Extract;
            unit.OrderTarget = "exit.ground";
            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueAttack(string unitId, string hostileId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor unit = FindActor(unitId);
            Actor hostile = FindActor(hostileId);
            if (unit == null || hostile == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!IsEligibleInfantry(unit) && unit.RosterRole != OperationsRosterRoleKind.RepairSpecialist)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            if (!hostile.Alive || !hostile.Spawned || hostile.Faction != OperationsTacticalFaction.Hostile)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!InRange(unit, hostile.X, hostile.Z, OperationsTacticalRules.AttackMeters))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.OutOfRange);
            unit.Order = OrderKind.Attack;
            unit.OrderTarget = hostileId;
            return QueueWorld(WorldEventKind.Death, hostileId, string.Empty);
        }

        /// <summary>
        /// True when Conclude would be accepted (authoritative partial predicate).
        /// Read-only; does not latch Partial or change facts.
        /// </summary>
        public bool IsPartialPredicateSatisfied => !_terminal && PartialSatisfied();

        public OperationsTacticalCommandResult IssueConclude()
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            if (!PartialSatisfied())
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.PreconditionFailed);
            Latch(OperationsOutcomeKind.Partial, "conclude");
            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult IssueWithdraw()
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Latch(OperationsOutcomeKind.Withdrawn, "withdraw");
            return OperationsTacticalCommandResult.Ok();
        }

        public OperationsTacticalCommandResult ReportDeath(string objectId) =>
            QueueWorld(WorldEventKind.Death, objectId, string.Empty);

        public OperationsTacticalCommandResult ReportSiteDestroyed(string siteId) =>
            QueueWorld(WorldEventKind.Destroy, siteId, string.Empty);

        public OperationsTacticalCommandResult ReportMoved(string objectId, string anchorId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor actor = FindActor(objectId);
            if (actor == null || !_map.TryGetById(anchorId, out _))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (actor.Faction == OperationsTacticalFaction.Player)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            return QueueWorld(WorldEventKind.Move, objectId, anchorId);
        }

        private void Step()
        {
            if (_paused || _terminal)
                return;
            _tick++;
            ActivatePending();
            ApplyEvents();
            MoveActors();
            ProgressChannels();
            ProjectExitFacts();
            EvaluateOutcome();
            if (!_terminal)
                UpdateWaves();
            QueuePending();
        }

        private void ActivatePending()
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (!node.Pending || node.Phase != OperationsTacticalNodePhase.Inactive)
                    continue;
                node.Pending = false;
                node.Phase = OperationsTacticalNodePhase.Active;
                OnActivated(node);
            }
        }

        private void OnActivated(Node node)
        {
            if (node.Spec.Rule != OperationsObjectiveRuleKind.Repair || node.Phase != OperationsTacticalNodePhase.Active)
                return;
            string[] targets = node.Spec.TargetIds;
            if (targets.Length == 0)
                return;
            for (int index = 0; index < targets.Length; index++)
            {
                Actor site = FindActor(targets[index]);
                if (site == null || !site.Alive)
                {
                    FailNode(node);
                    return;
                }

                if (!Listed(_definition.LaunchRestoredSiteIds, site.ObjectId) || site.Health < OperationsTacticalRules.RepairHealthPercent)
                    return;
            }

            for (int index = 0; index < targets.Length; index++)
                AddFact(OperationsTacticalFactKind.Repaired, targets[index]);
            CompleteNode(node);
        }

        private void ApplyEvents()
        {
            for (int index = 0; index < _events.Count; index++)
            {
                WorldEvent worldEvent = _events[index];
                Actor actor = FindActor(worldEvent.ObjectId);
                if (actor == null || !actor.Spawned)
                    continue;
                if (worldEvent.Kind == WorldEventKind.Death)
                    Kill(actor);
                else if (worldEvent.Kind == WorldEventKind.Destroy)
                    DestroySite(actor);
                else if (worldEvent.Kind == WorldEventKind.Move && _map.TryGetById(worldEvent.AnchorId, out OperationsGreyboxAnchor anchor))
                {
                    actor.X = anchor.X;
                    actor.Z = anchor.Z;
                }
            }

            _events.Clear();
        }

        private void Kill(Actor actor)
        {
            if (!actor.Alive)
                return;
            actor.Alive = false;
            actor.Health = 0;
            actor.CarriedObjectId = string.Empty;
            actor.Order = OrderKind.None;
            AddFact(OperationsTacticalFactKind.UnitDied, actor.ObjectId);
        }

        private void DestroySite(Actor actor)
        {
            if (!actor.Alive)
                return;
            actor.Alive = false;
            actor.Health = 0;
            AddFact(OperationsTacticalFactKind.SiteDestroyed, actor.ObjectId);
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (node.Spec.Rule != OperationsObjectiveRuleKind.Repair || node.Phase == OperationsTacticalNodePhase.Complete)
                    continue;
                if (Lists(node.Spec.TargetIds, actor.ObjectId))
                    FailNode(node);
            }
        }

        private void MoveActors()
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (!actor.Spawned || !actor.Alive)
                    continue;
                if (actor.Order == OrderKind.Move && _map.TryGetById(actor.OrderTarget, out OperationsGreyboxAnchor anchor))
                {
                    OperationsTacticalRules.StepToward(ref actor.X, ref actor.Z, anchor.X, anchor.Z, Speed(actor));
                }
                else if (actor.Order == OrderKind.Extract)
                {
                    OperationsTacticalRules.StepToward(ref actor.X, ref actor.Z, _definition.ExitX, _definition.ExitZ, Speed(actor));
                }
                else if (actor.Order == OrderKind.EscortGo)
                {
                    Node node = FindNode(actor.OrderTarget);
                    if (node == null || node.LockedRoute < 0)
                        continue;
                    OperationsCompiledRoute route = node.Spec.LegalRoutes[node.LockedRoute];
                    if (actor.WaypointIndex >= route.Xs.Length)
                        continue;
                    OperationsTacticalRules.StepToward(
                        ref actor.X,
                        ref actor.Z,
                        route.Xs[actor.WaypointIndex],
                        route.Zs[actor.WaypointIndex],
                        OperationsTacticalRules.CargoMetersPerTick);
                    if (OperationsTacticalRules.Within(actor.X, actor.Z, route.Xs[actor.WaypointIndex], route.Zs[actor.WaypointIndex], 0.2f))
                        actor.WaypointIndex++;
                }
            }
        }

        private void ProgressChannels()
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (node.Phase != OperationsTacticalNodePhase.Active)
                    continue;
                switch (node.Spec.Rule)
                {
                    case OperationsObjectiveRuleKind.Scan:
                        ProgressScan(node);
                        break;
                    case OperationsObjectiveRuleKind.Hold:
                        ProgressHold(node);
                        break;
                    case OperationsObjectiveRuleKind.Interact:
                        ProgressInteract(node);
                        break;
                    case OperationsObjectiveRuleKind.Repair:
                        ProgressRepair(node);
                        break;
                    case OperationsObjectiveRuleKind.Escort:
                        ProgressEscort(node);
                        break;
                    case OperationsObjectiveRuleKind.Extract:
                        ProgressExtract(node, true);
                        break;
                    case OperationsObjectiveRuleKind.Clear:
                        ProgressClear(node);
                        break;
                    case OperationsObjectiveRuleKind.Protect:
                        ProgressProtect(node);
                        break;
                }
            }

            LatchIntactProtects();
        }

        /// <summary>
        /// True when a Protect node and a Hold node cover the same authored site.
        /// </summary>
        public static bool ProtectSharesHoldSite(
            OperationsCompiledTactical definition,
            OperationsCompiledNode protect,
            OperationsCompiledNode hold)
        {
            if (definition == null || protect == null || hold == null)
                return false;
            if (protect.Rule != OperationsObjectiveRuleKind.Protect ||
                hold.Rule != OperationsObjectiveRuleKind.Hold)
                return false;
            if (string.IsNullOrEmpty(hold.ZoneAnchorId))
                return false;
            string[] targets = protect.TargetIds ?? Array.Empty<string>();
            OperationsCompiledSpawn[] spawns = definition.Spawns ?? Array.Empty<OperationsCompiledSpawn>();
            for (int target = 0; target < targets.Length; target++)
            {
                for (int spawn = 0; spawn < spawns.Length; spawn++)
                {
                    if (!string.Equals(spawns[spawn].ObjectId, targets[target], StringComparison.Ordinal))
                        continue;
                    if (string.Equals(spawns[spawn].AnchorId, hold.ZoneAnchorId, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Protect stays a live failure condition until the paired hold is complete
        /// (or, with no paired hold, until the other required nodes are complete)
        /// and every protected site is still intact.
        /// </summary>
        private void LatchIntactProtects()
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (node.Spec.Rule != OperationsObjectiveRuleKind.Protect ||
                    node.Phase != OperationsTacticalNodePhase.Active)
                    continue;
                if (!ProtectedSitesIntact(node))
                    continue;
                if (ProtectWindowClosed(node))
                    CompleteNode(node);
            }
        }

        private bool ProtectedSitesIntact(Node node)
        {
            string[] targets = node.Spec.TargetIds ?? Array.Empty<string>();
            if (targets.Length == 0)
                return false;
            for (int index = 0; index < targets.Length; index++)
            {
                Actor site = FindActor(targets[index]);
                if (site == null || !site.Alive)
                    return false;
            }

            return true;
        }

        private bool ProtectWindowClosed(Node protect)
        {
            bool pairedHold = false;
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node other = _nodes[index];
                if (!ProtectSharesHoldSite(_definition, protect.Spec, other.Spec))
                    continue;
                pairedHold = true;
                if (other.Phase != OperationsTacticalNodePhase.Complete)
                    return false;
            }

            if (pairedHold)
                return true;

            bool otherRequired = false;
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node other = _nodes[index];
                if (ReferenceEquals(other, protect) || !other.Spec.Required)
                    continue;
                if (other.Spec.Rule == OperationsObjectiveRuleKind.Protect)
                    continue;
                otherRequired = true;
                if (other.Phase != OperationsTacticalNodePhase.Complete)
                    return false;
            }

            return otherRequired;
        }

        private void ProgressClear(Node node)
        {
            string[] roles = node.Spec.TargetIds;
            bool any = false;
            bool pending = false;
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (actor.Faction != OperationsTacticalFaction.Hostile)
                    continue;
                if (!Lists(roles, actor.RoleId) && !Lists(roles, actor.ObjectId))
                    continue;
                any = true;
                if (!actor.Spawned || actor.Alive)
                    pending = true;
            }

            if (!any)
                return;
            if (!pending)
            {
                node.ProgressCount = 1;
                CompleteNode(node);
            }
        }

        private void ProgressProtect(Node node)
        {
            for (int index = 0; index < node.Spec.TargetIds.Length; index++)
            {
                Actor site = FindActor(node.Spec.TargetIds[index]);
                if (site == null || !site.Alive)
                {
                    FailNode(node);
                    return;
                }
            }
        }

        private void ProgressScan(Node node)
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor unit = _actors[index];
                if (unit.Order != OrderKind.Scan || !Lists(node.Spec.TargetIds, unit.OrderTarget))
                    continue;
                Actor site = FindActor(unit.OrderTarget);
                if (site == null || !site.Alive || !unit.Alive)
                {
                    unit.ChannelTicks = 0;
                    continue;
                }

                bool blocked = !InRange(unit, site.X, site.Z, OperationsTacticalRules.ScanMeters) ||
                               !HasSight(unit.X, unit.Z, site.X, site.Z);
                if (blocked)
                {
                    unit.ChannelTicks = 0;
                    continue;
                }

                unit.ChannelTicks++;
                if (unit.ChannelTicks < OperationsTacticalRules.ScanSeconds || node.Confirmed.Contains(site.ObjectId))
                    continue;
                node.Confirmed.Add(site.ObjectId);
                node.ProgressCount++;
                unit.ChannelTicks = 0;
                unit.Order = OrderKind.None;
                AddFact(OperationsTacticalFactKind.ScanConfirmed, site.ObjectId);
                if (node.ProgressCount >= node.Spec.TargetCount)
                    CompleteNode(node);
            }
        }

        private void ProgressHold(Node node)
        {
            bool holder = false;
            bool staleHold = false;
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor unit = _actors[index];
                if (unit.Order != OrderKind.Hold || !string.Equals(unit.OrderTarget, node.Spec.ZoneAnchorId, StringComparison.Ordinal))
                    continue;
                if (!IsEligibleInfantry(unit) || !InRange(unit, node.Spec.ZoneX, node.Spec.ZoneZ, OperationsTacticalRules.HoldMeters))
                    continue;
                // AFK cut: freeze progress when Hold has not been re-issued recently.
                // Do not reset ProgressTicks so a refreshed Hold can resume the compressed window.
                if (unit.OrderIssuedTick >= 0 &&
                    _tick - unit.OrderIssuedTick > OperationsTacticalRules.HoldRefreshSeconds)
                {
                    staleHold = true;
                    continue;
                }

                holder = true;
            }

            if (!holder || HostileWithin(node.Spec.ZoneX, node.Spec.ZoneZ, OperationsTacticalRules.HoldMeters))
            {
                if (!staleHold)
                    node.ProgressTicks = 0;
                return;
            }

            node.ProgressTicks++;
            if (node.ProgressTicks >= node.Spec.DurationTicks)
                CompleteNode(node);
        }

        private void ProgressInteract(Node node)
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor unit = _actors[index];
                if (unit.Order != OrderKind.Interact || !Lists(node.Spec.TargetIds, unit.OrderTarget))
                    continue;
                Actor target = FindActor(unit.OrderTarget);
                if (target == null || !unit.Alive)
                {
                    unit.ChannelTicks = 0;
                    continue;
                }

                bool interrupted = !InRange(unit, target.X, target.Z, OperationsTacticalRules.InteractMeters) ||
                                   HostileWithin(target.X, target.Z, OperationsTacticalRules.InteractThreatMeters);
                if (interrupted)
                {
                    unit.ChannelTicks = 0;
                    continue;
                }

                unit.ChannelTicks++;
                if (unit.ChannelTicks < node.Spec.DurationTicks)
                    continue;
                if (target.Body == OperationsTacticalBodyKind.Evidence)
                    unit.CarriedObjectId = target.ObjectId;
                unit.ChannelTicks = 0;
                unit.Order = OrderKind.None;
                node.ProgressCount++;
                AddFact(OperationsTacticalFactKind.InteractCompleted, target.ObjectId);
                if (node.ProgressCount >= node.Spec.TargetCount)
                    CompleteNode(node);
            }
        }

        private void ProgressRepair(Node node)
        {
            bool worked = false;
            for (int index = 0; index < _actors.Count && !worked; index++)
            {
                Actor unit = _actors[index];
                if (unit.Order != OrderKind.Repair || !Lists(node.Spec.TargetIds, unit.OrderTarget))
                    continue;
                Actor site = FindActor(unit.OrderTarget);
                if (site == null || !site.Alive)
                {
                    FailNode(node);
                    return;
                }

                bool paused = !unit.Alive ||
                              !InRange(unit, site.X, site.Z, OperationsTacticalRules.RepairMeters) ||
                              HostileWithin(site.X, site.Z, OperationsTacticalRules.RepairThreatMeters);
                if (paused)
                    continue;
                if (!node.MaterialsCharged)
                {
                    if (_materials < OperationsTacticalRules.RepairMaterialCost)
                        continue;
                    _materials -= OperationsTacticalRules.RepairMaterialCost;
                    node.MaterialsCharged = true;
                }

                worked = true;
                node.ProgressTicks++;
                if (node.ProgressTicks < OperationsTacticalRules.RepairSeconds)
                    continue;
                if (site.Health < OperationsTacticalRules.RepairHealthPercent)
                    site.Health = OperationsTacticalRules.RepairHealthPercent;
                AddFact(OperationsTacticalFactKind.Repaired, site.ObjectId);
                unit.Order = OrderKind.None;
                CompleteNode(node);
            }
        }

        private void ProgressEscort(Node node)
        {
            if (node.LockedRoute < 0)
                return;
            OperationsCompiledRoute route = node.Spec.LegalRoutes[node.LockedRoute];
            int delivered = 0;
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor cargo = _actors[index];
                if (!IsEscortCargo(node, cargo))
                    continue;
                if (cargo.Delivered)
                {
                    delivered++;
                    continue;
                }

                if (!cargo.Alive || !cargo.HasCargo)
                {
                    cargo.UnloadTicks = 0;
                    continue;
                }

                bool atEnd = cargo.WaypointIndex >= route.Xs.Length &&
                             cargo.Order == OrderKind.EscortGo &&
                             OperationsTacticalRules.Within(
                                 cargo.X,
                                 cargo.Z,
                                 route.Xs[route.Xs.Length - 1],
                                 route.Zs[route.Zs.Length - 1],
                                 0.5f);
                if (!atEnd)
                {
                    if (cargo.Order != OrderKind.EscortHold)
                        cargo.UnloadTicks = 0;
                    continue;
                }

                cargo.UnloadTicks++;
                if (cargo.UnloadTicks < OperationsTacticalRules.EscortUnloadSeconds)
                    continue;
                cargo.Delivered = true;
                cargo.Order = OrderKind.None;
                delivered++;
                AddFact(OperationsTacticalFactKind.CargoDelivered, cargo.ObjectId);
            }

            int shown = delivered;
            if (node.Spec.TargetCount > 0 && shown > node.Spec.TargetCount)
                shown = node.Spec.TargetCount;
            node.ProgressCount = shown;
            if (delivered >= node.Spec.TargetCount)
                CompleteNode(node);
        }

        private void ProgressExtract(Node node, bool completeNode)
        {
            if (!ExitSecured())
                return;
            if (!EvidenceAtExit())
                return;
            int arrived = CountExtractArrivals();
            if (arrived < OperationsTacticalRules.ExtractMinimumInfantry)
                return;
            AddFact(OperationsTacticalFactKind.ExitReached, "exit.ground");
            if (!completeNode || node == null || node.Phase != OperationsTacticalNodePhase.Active)
                return;
            if (arrived >= node.Spec.TargetCount)
                CompleteNode(node);
        }

        private void ProjectExitFacts()
        {
            Node extract = FindActive(OperationsObjectiveRuleKind.Extract);
            if (extract != null)
                return;
            ProgressExtract(null, false);
        }

        private void EvaluateOutcome()
        {
            if (_terminal)
                return;
            if (LivingCommandable() == 0)
            {
                Latch(OperationsOutcomeKind.Defeat, "no_commandable_units");
                return;
            }

            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (!node.Spec.Required || node.Phase != OperationsTacticalNodePhase.Failed)
                    continue;
                Latch(OperationsOutcomeKind.Defeat, "node_failed:" + node.Spec.NodeId);
                return;
            }

            if (AllRequiredComplete())
            {
                Latch(OperationsOutcomeKind.Victory, "mandatory_complete");
                return;
            }

            if (_definition.DeadlineTicks > 0 && _tick >= _definition.DeadlineTicks)
            {
                if (PartialSatisfied())
                    Latch(OperationsOutcomeKind.Partial, "deadline");
                else
                    Latch(OperationsOutcomeKind.Defeat, "deadline");
            }
        }

        private void UpdateWaves()
        {
            for (int index = 0; index < _waves.Count; index++)
            {
                Wave wave = _waves[index];
                if (wave.Spawned)
                    continue;
                if (!wave.Armed && TriggerSatisfied(wave.Spec))
                {
                    wave.Armed = true;
                    wave.ArmedTick = _tick;
                    AddFact(OperationsTacticalFactKind.WaveWarned, "wave." + wave.Spec.Group.ToString());
                }

                if (!wave.Armed || _tick < wave.ArmedTick + wave.Spec.WarningSeconds)
                    continue;
                SpawnWave(wave);
            }
        }

        private bool TriggerSatisfied(OperationsCompiledWave wave)
        {
            switch (wave.Trigger)
            {
                case OperationsWaveTriggerKind.FirstRequiredCompletion:
                    Node first = FindNode(_definition.FirstRequiredNodeId);
                    return first != null && first.Phase == OperationsTacticalNodePhase.Complete;
                case OperationsWaveTriggerKind.FinalRequiredActivation:
                    Node finalNode = FindNode(_definition.FinalRequiredNodeId);
                    return finalNode != null &&
                           (finalNode.Phase == OperationsTacticalNodePhase.Active ||
                            finalNode.Phase == OperationsTacticalNodePhase.Complete);
                case OperationsWaveTriggerKind.NodeCompletion:
                    Node completed = FindNode(wave.TriggerNodeId);
                    return completed != null && completed.Phase == OperationsTacticalNodePhase.Complete;
                case OperationsWaveTriggerKind.NodeActivation:
                    Node activated = FindNode(wave.TriggerNodeId);
                    return activated != null && activated.Phase != OperationsTacticalNodePhase.Inactive;
                case OperationsWaveTriggerKind.Elapsed:
                    return _tick >= wave.ElapsedTicks;
                default:
                    return false;
            }
        }

        private void SpawnWave(Wave wave)
        {
            bool spawnedAny = false;
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (actor.Spawned || actor.Group != wave.Spec.Group)
                    continue;
                bool homeOccupied = PlayerOccupies(actor.HomeX, actor.HomeZ);
                bool stagingOccupied = PlayerOccupies(actor.StagingX, actor.StagingZ);
                if (homeOccupied && stagingOccupied)
                    return;
                if (homeOccupied)
                {
                    actor.X = actor.StagingX;
                    actor.Z = actor.StagingZ;
                }
                else
                {
                    actor.X = actor.HomeX;
                    actor.Z = actor.HomeZ;
                }

                actor.Spawned = true;
                actor.Alive = true;
                spawnedAny = true;
                AddFact(OperationsTacticalFactKind.WaveSpawned, actor.ObjectId);
            }

            if (spawnedAny)
                wave.Spawned = true;
        }

        private void QueuePending()
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (node.Phase != OperationsTacticalNodePhase.Inactive || node.Spec.Activation == OperationsActivationPolicyKind.Launch)
                    continue;
                if (PrerequisitesMet(node))
                    node.Pending = true;
            }
        }

        private bool PrerequisitesMet(Node node)
        {
            string[] prerequisites = node.Spec.Prerequisites;
            if (prerequisites.Length == 0)
                return false;
            if (node.Spec.Join == OperationsGraphJoinKind.AnyOf)
            {
                for (int index = 0; index < prerequisites.Length; index++)
                {
                    Node prerequisite = FindNode(prerequisites[index]);
                    if (prerequisite != null && prerequisite.Phase == OperationsTacticalNodePhase.Complete)
                        return true;
                }

                return false;
            }

            for (int index = 0; index < prerequisites.Length; index++)
            {
                Node prerequisite = FindNode(prerequisites[index]);
                if (prerequisite == null || prerequisite.Phase != OperationsTacticalNodePhase.Complete)
                    return false;
            }

            return true;
        }

        private OperationsTacticalCommandResult IssueChannel(
            string unitId,
            string targetId,
            OperationsObjectiveRuleKind rule,
            OrderKind order,
            float range,
            bool scan)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            Actor unit = FindActor(unitId);
            Actor target = FindActor(targetId);
            if (unit == null || target == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            if (!IsEligibleInfantry(unit))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotEligible);
            Node node = FindRuleTarget(rule, targetId, out OperationsTacticalRejectKind failure);
            if (node == null)
                return OperationsTacticalCommandResult.Reject(failure);
            if (scan && node.Confirmed.Contains(targetId))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.AlreadyConsumed);
            if (scan && target.HiddenUntilObserved && !target.Observed)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.NotObserved);
            if (!target.Alive && target.Body == OperationsTacticalBodyKind.Site)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.SiteDestroyed);
            if (!InRange(unit, target.X, target.Z, range))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.OutOfRange);
            if (scan && !HasSight(unit.X, unit.Z, target.X, target.Z))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.BlockedLineOfSight);
            if (!scan && HostileWithin(target.X, target.Z, OperationsTacticalRules.InteractThreatMeters))
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.HostilePresent);
            unit.Order = order;
            unit.OrderTarget = targetId;
            unit.ChannelTicks = 0;
            return OperationsTacticalCommandResult.Ok();
        }

        private Node FindRuleTarget(OperationsObjectiveRuleKind rule, string targetId, out OperationsTacticalRejectKind failure)
        {
            failure = OperationsTacticalRejectKind.UnknownTarget;
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (node.Spec.Rule != rule || !Lists(node.Spec.TargetIds, targetId))
                    continue;
                if (node.Phase == OperationsTacticalNodePhase.Complete)
                {
                    failure = OperationsTacticalRejectKind.AlreadyConsumed;
                    return null;
                }

                if (node.Phase == OperationsTacticalNodePhase.Active)
                    return node;
                failure = OperationsTacticalRejectKind.NodeInactive;
            }

            return null;
        }

        private Node FindHold(string zoneAnchorId, out OperationsTacticalRejectKind failure)
        {
            failure = OperationsTacticalRejectKind.UnknownTarget;
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (node.Spec.Rule != OperationsObjectiveRuleKind.Hold)
                    continue;
                if (!string.Equals(node.Spec.ZoneAnchorId, zoneAnchorId, StringComparison.Ordinal))
                    continue;
                if (node.Phase == OperationsTacticalNodePhase.Complete)
                {
                    failure = OperationsTacticalRejectKind.AlreadyConsumed;
                    return null;
                }

                if (node.Phase == OperationsTacticalNodePhase.Active)
                    return node;
                failure = OperationsTacticalRejectKind.NodeInactive;
                return null;
            }

            return null;
        }

        private Node FindEscort(string routeId, out int routeIndex, out OperationsTacticalRejectKind failure)
        {
            routeIndex = -1;
            failure = OperationsTacticalRejectKind.UnknownTarget;
            for (int index = 0; index < _nodes.Count; index++)
            {
                Node node = _nodes[index];
                if (node.Spec.Rule != OperationsObjectiveRuleKind.Escort)
                    continue;
                int found = -1;
                for (int route = 0; route < node.Spec.LegalRoutes.Length; route++)
                {
                    if (string.Equals(node.Spec.LegalRoutes[route].RouteId, routeId, StringComparison.Ordinal))
                        found = route;
                }

                if (found < 0)
                    continue;
                if (node.Phase == OperationsTacticalNodePhase.Complete)
                {
                    failure = OperationsTacticalRejectKind.AlreadyConsumed;
                    return null;
                }

                if (node.Phase != OperationsTacticalNodePhase.Active)
                {
                    failure = OperationsTacticalRejectKind.NodeInactive;
                    return null;
                }

                routeIndex = found;
                failure = OperationsTacticalRejectKind.None;
                return node;
            }

            return null;
        }

        private Node FindActive(OperationsObjectiveRuleKind rule)
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                if (_nodes[index].Spec.Rule == rule && _nodes[index].Phase == OperationsTacticalNodePhase.Active)
                    return _nodes[index];
            }

            return null;
        }

        private bool EvidenceAtExit()
        {
            string[] evidence = _definition.MandatoryEvidenceIds;
            for (int index = 0; index < evidence.Length; index++)
            {
                bool carried = false;
                for (int actorIndex = 0; actorIndex < _actors.Count; actorIndex++)
                {
                    Actor actor = _actors[actorIndex];
                    if (!AtExit(actor))
                        continue;
                    if (string.Equals(actor.CarriedObjectId, evidence[index], StringComparison.Ordinal))
                        carried = true;
                }

                if (!carried)
                    return false;
            }

            return true;
        }

        private int CountExtractArrivals()
        {
            int arrived = 0;
            for (int index = 0; index < _actors.Count; index++)
            {
                if (AtExit(_actors[index]))
                    arrived++;
            }

            return arrived;
        }

        private bool AtExit(Actor actor) =>
            actor.OriginalInfantry &&
            actor.Alive &&
            actor.Spawned &&
            actor.Order == OrderKind.Extract &&
            OperationsTacticalRules.Within(actor.X, actor.Z, _definition.ExitX, _definition.ExitZ, OperationsTacticalRules.ExtractArrivalMeters);

        private bool ExitSecured() => !HostileWithin(_definition.ExitX, _definition.ExitZ, OperationsTacticalRules.ExitSecureMeters);

        private bool HostileWithin(float x, float z, float meters)
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (!actor.Spawned || !actor.Alive || actor.Faction != OperationsTacticalFaction.Hostile)
                    continue;
                if (actor.Body != OperationsTacticalBodyKind.Infantry && actor.Body != OperationsTacticalBodyKind.Vehicle)
                    continue;
                if (OperationsTacticalRules.Within(actor.X, actor.Z, x, z, meters))
                    return true;
            }

            return false;
        }

        private bool PlayerOccupies(float x, float z)
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (!actor.Spawned || !actor.Alive || !actor.OriginalInfantry)
                    continue;
                if (OperationsTacticalRules.Within(actor.X, actor.Z, x, z, OperationsTacticalRules.SpawnOccupationMeters))
                    return true;
            }

            return false;
        }

        private bool HasSight(float x0, float z0, float x1, float z1)
        {
            for (int index = 0; index < _definition.BlockerAx.Length; index++)
            {
                if (OperationsTacticalRules.SegmentsBlockSight(
                        x0, z0, x1, z1,
                        _definition.BlockerAx[index], _definition.BlockerAz[index],
                        _definition.BlockerBx[index], _definition.BlockerBz[index]))
                    return false;
            }

            return true;
        }

        private bool AllRequiredComplete()
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                if (!_nodes[index].Spec.Required)
                    continue;
                if (_nodes[index].Spec.Rule == OperationsObjectiveRuleKind.Protect)
                    continue;
                if (_nodes[index].Phase != OperationsTacticalNodePhase.Complete)
                    return false;
            }

            return true;
        }

        private bool PartialSatisfied()
        {
            string[] partials = _definition.PartialNodeIds;
            if (_definition.PartialMinimumComplete > 0)
            {
                if (partials.Length == 0)
                    return false;
                int complete = 0;
                for (int index = 0; index < partials.Length; index++)
                {
                    Node node = FindNode(partials[index]);
                    if (node != null && node.Phase == OperationsTacticalNodePhase.Complete)
                        complete++;
                }

                if (complete < _definition.PartialMinimumComplete)
                    return false;
            }
            else if (partials.Length > 0)
            {
                for (int index = 0; index < partials.Length; index++)
                {
                    Node node = FindNode(partials[index]);
                    if (node == null || node.Phase != OperationsTacticalNodePhase.Complete)
                        return false;
                }
            }

            if (!string.IsNullOrEmpty(_definition.PartialProgressNodeId))
            {
                Node progress = FindNode(_definition.PartialProgressNodeId);
                if (progress == null || progress.ProgressCount < _definition.PartialProgressMinimum)
                    return false;
            }
            else if (partials.Length == 0 &&
                     string.IsNullOrEmpty(_definition.PartialProgressNodeId) &&
                     _definition.PartialExtractMinimum <= 0)
            {
                return false;
            }

            if (_definition.PartialExtractMinimum > 0 &&
                CountExtractArrivals() < _definition.PartialExtractMinimum)
                return false;

            for (int index = 0; index < _nodes.Count; index++)
            {
                if (_nodes[index].Spec.Rule == OperationsObjectiveRuleKind.Protect &&
                    _nodes[index].Phase == OperationsTacticalNodePhase.Failed)
                    return false;
            }

            return true;
        }

        private int LivingCommandable()
        {
            int count = 0;
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (actor.Spawned && actor.Alive && actor.Commandable)
                    count++;
            }

            return count;
        }

        private int CountSpawnedHostiles()
        {
            int count = 0;
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (actor.Faction == OperationsTacticalFaction.Hostile && actor.Spawned)
                    count++;
            }

            return count;
        }

        private void AddFact(OperationsTacticalFactKind kind, string objectId)
        {
            for (int index = 0; index < _facts.Count; index++)
            {
                if (_facts[index].Kind == kind && _facts[index].ObjectId == objectId)
                    return;
            }

            _facts.Add(new OperationsTacticalFact(kind, objectId, _tick, _facts.Count + 1));
        }

        private void CompleteNode(Node node)
        {
            if (node.Phase == OperationsTacticalNodePhase.Complete)
                return;
            node.Phase = OperationsTacticalNodePhase.Complete;
            node.CompletionTick = _tick;
        }

        private void FailNode(Node node)
        {
            if (node.Phase == OperationsTacticalNodePhase.Complete || node.Phase == OperationsTacticalNodePhase.Failed)
                return;
            node.Phase = OperationsTacticalNodePhase.Failed;
            node.CompletionTick = _tick;
        }

        private void Latch(OperationsOutcomeKind outcome, string reason)
        {
            if (_terminal)
                return;
            _terminal = true;
            _outcome = outcome;
            _reason = reason;
            Release();
        }

        private void Release()
        {
            _released = true;
            _events.Clear();
            for (int index = 0; index < _actors.Count; index++)
                _actors[index].Order = OrderKind.None;
        }

        private OperationsTacticalCommandResult QueueWorld(WorldEventKind kind, string objectId, string anchorId)
        {
            if (_terminal)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.Terminal);
            if (FindActor(objectId) == null)
                return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.UnknownTarget);
            _events.Add(new WorldEvent { Kind = kind, ObjectId = objectId, AnchorId = anchorId });
            return OperationsTacticalCommandResult.Ok();
        }

        private static Actor CreateActor(OperationsCompiledSpawn spawn, string sessionId)
        {
            bool spawned = spawn.Group == 0;
            return new Actor
            {
                ObjectId = spawn.ObjectId,
                RoleId = spawn.RoleId,
                SessionId = sessionId,
                RosterRole = spawn.RosterRole,
                Faction = spawn.Faction,
                Body = spawn.Body,
                Alive = spawned,
                Spawned = spawned,
                Commandable = spawn.Commandable,
                OriginalInfantry = spawn.OriginalInfantry,
                Group = spawn.Group,
                X = spawn.X,
                Z = spawn.Z,
                HomeX = spawn.X,
                HomeZ = spawn.Z,
                StagingX = spawn.StagingX,
                StagingZ = spawn.StagingZ,
                Health = spawn.Health,
                HasCargo = spawn.HasCargo,
                HiddenUntilObserved = spawn.HiddenUntilObserved
            };
        }

        private static OperationsTacticalActorState ToState(Actor actor) =>
            new(
                actor.ObjectId,
                actor.RoleId,
                actor.SessionId,
                actor.Faction,
                actor.Body,
                actor.RosterRole,
                actor.Alive,
                actor.Spawned,
                actor.Commandable,
                actor.Group,
                actor.X,
                actor.Z,
                actor.Health,
                actor.CarriedObjectId,
                actor.ChannelTicks);

        private Actor FindActor(string objectId)
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                if (string.Equals(_actors[index].ObjectId, objectId, StringComparison.Ordinal))
                    return _actors[index];
            }

            return null;
        }

        private Node FindNode(string nodeId)
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                if (string.Equals(_nodes[index].Spec.NodeId, nodeId, StringComparison.Ordinal))
                    return _nodes[index];
            }

            return null;
        }

        private Wave FindWave(int group)
        {
            for (int index = 0; index < _waves.Count; index++)
            {
                if (_waves[index].Spec.Group == group)
                    return _waves[index];
            }

            return null;
        }

        private static bool IsEligibleInfantry(Actor actor) =>
            actor.Alive &&
            actor.Spawned &&
            actor.Faction == OperationsTacticalFaction.Player &&
            actor.Body == OperationsTacticalBodyKind.Infantry &&
            actor.Commandable;

        private static bool IsEscortCargo(Node node, Actor cargo) =>
            cargo.Spawned &&
            cargo.Body == OperationsTacticalBodyKind.Cargo &&
            Lists(node.Spec.TargetIds, cargo.ObjectId);

        private static bool InRange(Actor actor, float x, float z, float meters) =>
            OperationsTacticalRules.Within(actor.X, actor.Z, x, z, meters);

        private static float Speed(Actor actor) =>
            actor.Body == OperationsTacticalBodyKind.Cargo
                ? OperationsTacticalRules.CargoMetersPerTick
                : OperationsTacticalRules.InfantryMetersPerTick;

        private static bool Lists(string[] values, string value)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool Listed(string[] values, string value) => Lists(values, value);
    }
}
