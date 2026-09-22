using System;
using System.Collections.Generic;
using Game.Operations.Contracts;

namespace Game.Operations.Strategic
{
    public sealed class OperationsCityWorld
    {
        public OperationsRunComponent Run;
        public OperationsDistrictComponent[] Districts = new OperationsDistrictComponent[OperationsIdentityRules.DistrictCount];
        public List<OperationsSiteStateComponent> Sites = new();
        public List<OperationsRouteStateComponent> Routes = new();
        public List<OperationsMilestoneComponent> Milestones = new();
        public List<OperationsOfferComponent> Offers = new();
        public List<OperationsIncidentComponent> Incidents = new();
        public List<OperationsActionUseComponent> ActionUses = new();
        public List<OperationsAttemptComponent> Attempts = new();
        public List<OperationsCooldownComponent> Cooldowns = new();
        public List<string> PriorityOfferIds = new();
        public List<string> EvidenceFlags = new();

        public bool HasActiveRun =>
            !string.IsNullOrEmpty(Run.RunId) &&
            Run.Phase != OperationsRunPhaseKind.None &&
            Run.Phase != OperationsRunPhaseKind.Archived;

        public static OperationsCityWorld FromSave(OperationsSaveData save)
        {
            OperationsCityWorld world = new();
            if (save?.activeRun == null || string.IsNullOrEmpty(save.activeRun.runId))
                return world;

            OperationsRunSaveData run = save.activeRun;
            world.Run = new OperationsRunComponent
            {
                RunId = run.runId,
                Revision = run.revision,
                Day = run.day,
                ActionPoints = run.actionPoints,
                Difficulty = run.difficulty,
                Seed = run.seed,
                Phase = run.phase,
                ConsecutiveStableDays = run.consecutiveStableDays,
                DirectorVersion = run.directorVersion < 1 ? 1 : run.directorVersion,
                PrngState = run.prngState == 0 ? 1u : run.prngState,
                CityCompleted = run.cityCompleted,
                LiveVictoriesToday = run.liveVictoriesToday
            };

            OperationsDistrictSaveData[] districts = run.districts ?? Array.Empty<OperationsDistrictSaveData>();
            for (int index = 0; index < districts.Length && index < world.Districts.Length; index++)
            {
                OperationsDistrictSaveData source = districts[index];
                int number = index + 1;
                if (OperationsIdentityRules.TryParseDistrictNumber(source.districtId, out int parsed))
                    number = parsed;
                world.Districts[number - 1] = ReadDistrict(source, number);
            }

            CopySites(run.sites, world.Sites);
            CopyRoutes(run.routes, world.Routes);
            CopyMilestones(run.milestones, world.Milestones);
            CopyOffers(run.offers, world.Offers);
            CopyIncidents(run.incidents, world.Incidents);
            CopyActions(run.actionUses, world.ActionUses);
            CopyAttempts(run.attempts, world.Attempts);
            CopyCooldowns(run.cooldowns, world.Cooldowns);
            CopyStrings(run.citywidePriorityOfferIds, world.PriorityOfferIds);
            CopyStrings(run.evidenceFlags, world.EvidenceFlags);
            return world;
        }

        public void WriteInto(OperationsSaveData save)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));

            OperationsRunSaveData run = new()
            {
                runId = Run.RunId ?? string.Empty,
                revision = Run.Revision,
                day = Run.Day,
                actionPoints = Run.ActionPoints,
                difficulty = Run.Difficulty,
                seed = Run.Seed,
                phase = Run.Phase,
                consecutiveStableDays = Run.ConsecutiveStableDays,
                directorVersion = Run.DirectorVersion,
                prngState = Run.PrngState,
                cityCompleted = Run.CityCompleted,
                liveVictoriesToday = Run.LiveVictoriesToday,
                districts = new OperationsDistrictSaveData[Districts.Length],
                milestones = new OperationsMilestoneSaveData[Milestones.Count],
                offers = new OperationsOfferSaveData[Offers.Count],
                incidents = new OperationsIncidentSaveData[Incidents.Count],
                sites = new OperationsSiteSaveData[Sites.Count],
                routes = new OperationsRouteSaveData[Routes.Count],
                actionUses = new OperationsActionUseSaveData[ActionUses.Count],
                attempts = new OperationsAttemptSaveData[Attempts.Count],
                cooldowns = new OperationsCooldownSaveData[Cooldowns.Count],
                citywidePriorityOfferIds = PriorityOfferIds.ToArray(),
                evidenceFlags = EvidenceFlags.ToArray()
            };

            for (int index = 0; index < Districts.Length; index++)
                run.districts[index] = WriteDistrict(Districts[index]);
            for (int index = 0; index < Milestones.Count; index++)
                run.milestones[index] = WriteMilestone(Milestones[index]);
            for (int index = 0; index < Offers.Count; index++)
                run.offers[index] = WriteOffer(Offers[index]);
            for (int index = 0; index < Incidents.Count; index++)
                run.incidents[index] = WriteIncident(Incidents[index]);
            for (int index = 0; index < Sites.Count; index++)
                run.sites[index] = WriteSite(Sites[index]);
            for (int index = 0; index < Routes.Count; index++)
                run.routes[index] = WriteRoute(Routes[index]);
            for (int index = 0; index < ActionUses.Count; index++)
                run.actionUses[index] = new OperationsActionUseSaveData
                {
                    districtId = ActionUses[index].DistrictId ?? string.Empty,
                    actionKind = (byte)ActionUses[index].Kind
                };
            for (int index = 0; index < Attempts.Count; index++)
                run.attempts[index] = WriteAttempt(Attempts[index]);
            for (int index = 0; index < Cooldowns.Count; index++)
                run.cooldowns[index] = new OperationsCooldownSaveData
                {
                    districtId = Cooldowns[index].DistrictId ?? string.Empty,
                    kind = (byte)Cooldowns[index].Kind,
                    availableOnDay = Cooldowns[index].AvailableOnDay
                };

            save.activeRun = run;
            save.pendingDeployment = FindReservedLiveAttempt(out OperationsAttemptComponent reserved)
                ? new OperationsPendingDeploymentSaveData
                {
                    reserved = true,
                    offerId = reserved.OfferId,
                    sessionId = reserved.SessionId,
                    transactionId = reserved.TransactionId,
                    snapshotHash = reserved.SnapshotHash
                }
                : null;
        }

        public ref OperationsDistrictComponent District(int number)
        {
            if (number < 1 || number > Districts.Length)
                throw new ArgumentOutOfRangeException(nameof(number));
            return ref Districts[number - 1];
        }

        public int DistrictIndex(string districtId)
        {
            if (!OperationsIdentityRules.TryParseDistrictNumber(districtId, out int number))
                return -1;
            return number - 1;
        }

        public bool TryOfferById(string offerId, out OperationsOfferComponent offer)
        {
            for (int index = 0; index < Offers.Count; index++)
            {
                if (Offers[index].OfferId == offerId)
                {
                    offer = Offers[index];
                    return true;
                }
            }

            offer = default;
            return false;
        }

        public bool TryOfferByMission(string missionId, out OperationsOfferComponent offer)
        {
            for (int index = 0; index < Offers.Count; index++)
            {
                if (Offers[index].MissionId == missionId)
                {
                    offer = Offers[index];
                    return true;
                }
            }

            offer = default;
            return false;
        }

        public bool TryMilestone(string missionId, out int index)
        {
            for (index = 0; index < Milestones.Count; index++)
            {
                if (Milestones[index].MissionId == missionId)
                    return true;
            }

            index = -1;
            return false;
        }

        public bool HasVictory(string missionId)
        {
            return TryMilestone(missionId, out int index) && Milestones[index].Victory;
        }

        public bool HasAttempt(string missionId)
        {
            return TryMilestone(missionId, out int index) && Milestones[index].Attempted;
        }

        public void MarkAttempted(string missionId)
        {
            if (!TryMilestone(missionId, out int index))
            {
                Milestones.Add(new OperationsMilestoneComponent
                {
                    MissionId = missionId,
                    Attempted = true,
                    AttemptCount = 1
                });
                return;
            }

            OperationsMilestoneComponent milestone = Milestones[index];
            milestone.Attempted = true;
            milestone.AttemptCount++;
            Milestones[index] = milestone;
        }

        public void MarkVictory(string missionId)
        {
            if (!TryMilestone(missionId, out int index))
            {
                Milestones.Add(new OperationsMilestoneComponent
                {
                    MissionId = missionId,
                    Attempted = true,
                    Victory = true,
                    AttemptCount = 1
                });
                return;
            }

            OperationsMilestoneComponent milestone = Milestones[index];
            milestone.Attempted = true;
            milestone.Victory = true;
            Milestones[index] = milestone;
        }

        public bool FindReservedLiveAttempt(out OperationsAttemptComponent attempt)
        {
            for (int index = 0; index < Attempts.Count; index++)
            {
                if (Attempts[index].Phase == OperationsAttemptPhase.Reserved && !Attempts[index].Practice)
                {
                    attempt = Attempts[index];
                    return true;
                }
            }

            attempt = default;
            return false;
        }

        public int FindAttemptBySession(string sessionId)
        {
            for (int index = 0; index < Attempts.Count; index++)
            {
                if (Attempts[index].SessionId == sessionId)
                    return index;
            }

            return -1;
        }

        public bool ActionUsed(string districtId, OperationsAbstractActionKind kind)
        {
            for (int index = 0; index < ActionUses.Count; index++)
            {
                if (ActionUses[index].Kind == kind && ActionUses[index].DistrictId == districtId)
                    return true;
            }

            return false;
        }

        public void SetSite(string siteId, OperationsSiteStateKind state)
        {
            for (int index = 0; index < Sites.Count; index++)
            {
                if (Sites[index].SiteId != siteId)
                    continue;
                OperationsSiteStateComponent site = Sites[index];
                site.State = state;
                Sites[index] = site;
                return;
            }
        }

        public void SetRoute(string routeId, OperationsRouteStateKind state)
        {
            for (int index = 0; index < Routes.Count; index++)
            {
                if (Routes[index].RouteId != routeId)
                    continue;
                OperationsRouteStateComponent route = Routes[index];
                route.State = state;
                Routes[index] = route;
                return;
            }
        }

        public static string ServiceSiteId(int districtNumber) =>
            "anchor.operations.d" + districtNumber.ToString("00") + ".service";

        public static string BackupSiteId(int districtNumber) =>
            "anchor.operations.d" + districtNumber.ToString("00") + ".backup";

        public static string MainRouteId(int districtNumber) =>
            "route.operations.d" + districtNumber.ToString("00") + ".main";

        /// <summary>
        /// D04's second independent land crossing. This is an abstract route id.
        /// Demo 2 bridge meshes are not bound here.
        /// </summary>
        public static string CrossingRouteId(int districtNumber) =>
            districtNumber == 4 ? "route.operations.d04.south" : string.Empty;

        private static OperationsDistrictComponent ReadDistrict(OperationsDistrictSaveData source, int number)
        {
            return new OperationsDistrictComponent
            {
                DistrictId = string.IsNullOrEmpty(source.districtId) ? OperationsIdentityRules.DistrictId(number) : source.districtId,
                Number = number,
                Security = source.security,
                Trust = source.trust,
                Infrastructure = source.infrastructure,
                EnemyInfluence = source.enemyInfluence,
                IntelConfidence = source.intelConfidence,
                Heat = source.heat,
                SupplyReadiness = source.supplyReadiness,
                CivilianDensity = source.civilianDensity,
                ChangeVersion = source.changeVersion,
                PublicHintMissionId = source.publicHintMissionId ?? string.Empty
            };
        }

        private static OperationsDistrictSaveData WriteDistrict(OperationsDistrictComponent source)
        {
            return new OperationsDistrictSaveData
            {
                districtId = source.DistrictId ?? string.Empty,
                security = source.Security,
                trust = source.Trust,
                infrastructure = source.Infrastructure,
                enemyInfluence = source.EnemyInfluence,
                intelConfidence = source.IntelConfidence,
                heat = source.Heat,
                supplyReadiness = source.SupplyReadiness,
                civilianDensity = source.CivilianDensity,
                changeVersion = source.ChangeVersion,
                publicHintMissionId = source.PublicHintMissionId ?? string.Empty
            };
        }

        private static void CopySites(OperationsSiteSaveData[] source, List<OperationsSiteStateComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsSiteStateComponent
                {
                    SiteId = source[index].siteId ?? string.Empty,
                    DistrictId = source[index].districtId ?? string.Empty,
                    State = (OperationsSiteStateKind)source[index].state
                });
            }
        }

        private static void CopyRoutes(OperationsRouteSaveData[] source, List<OperationsRouteStateComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsRouteStateComponent
                {
                    RouteId = source[index].routeId ?? string.Empty,
                    DistrictId = source[index].districtId ?? string.Empty,
                    State = (OperationsRouteStateKind)source[index].state
                });
            }
        }

        private static void CopyMilestones(OperationsMilestoneSaveData[] source, List<OperationsMilestoneComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsMilestoneComponent
                {
                    MissionId = source[index].missionId ?? string.Empty,
                    Attempted = source[index].attempted,
                    Victory = source[index].victory,
                    AttemptCount = source[index].attemptCount
                });
            }
        }

        private static void CopyOffers(OperationsOfferSaveData[] source, List<OperationsOfferComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsOfferComponent
                {
                    OfferId = source[index].offerId ?? string.Empty,
                    MissionId = source[index].missionId ?? string.Empty,
                    DistrictId = source[index].districtId ?? string.Empty,
                    Day = source[index].day,
                    Deployable = source[index].deployable,
                    Urgent = source[index].urgent
                });
            }
        }

        private static void CopyIncidents(OperationsIncidentSaveData[] source, List<OperationsIncidentComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsIncidentComponent
                {
                    IncidentId = source[index].incidentId ?? string.Empty,
                    DistrictId = source[index].districtId ?? string.Empty,
                    MissionId = source[index].missionId ?? string.Empty,
                    OfferId = source[index].offerId ?? string.Empty,
                    Kind = (OperationsIncidentKind)source[index].kind,
                    CreatedDay = source[index].createdDay,
                    DueDay = source[index].dueDay,
                    SiteId = source[index].siteId ?? string.Empty,
                    RouteId = source[index].routeId ?? string.Empty
                });
            }
        }

        private static void CopyActions(OperationsActionUseSaveData[] source, List<OperationsActionUseComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsActionUseComponent
                {
                    DistrictId = source[index].districtId ?? string.Empty,
                    Kind = (OperationsAbstractActionKind)source[index].actionKind
                });
            }
        }

        private static void CopyAttempts(OperationsAttemptSaveData[] source, List<OperationsAttemptComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsAttemptComponent
                {
                    SessionId = source[index].sessionId ?? string.Empty,
                    OfferId = source[index].offerId ?? string.Empty,
                    MissionId = source[index].missionId ?? string.Empty,
                    DistrictId = source[index].districtId ?? string.Empty,
                    AttemptOrdinal = source[index].attemptOrdinal,
                    TransactionId = source[index].transactionId ?? string.Empty,
                    SnapshotHash = source[index].snapshotHash ?? string.Empty,
                    ResultHash = source[index].resultHash ?? string.Empty,
                    Phase = source[index].phase,
                    Practice = source[index].practice,
                    ApRefunded = source[index].apRefunded
                });
            }
        }

        private static void CopyCooldowns(OperationsCooldownSaveData[] source, List<OperationsCooldownComponent> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
            {
                target.Add(new OperationsCooldownComponent
                {
                    DistrictId = source[index].districtId ?? string.Empty,
                    Kind = (OperationsIncidentKind)source[index].kind,
                    AvailableOnDay = source[index].availableOnDay
                });
            }
        }

        private static void CopyStrings(string[] source, List<string> target)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Length; index++)
                target.Add(source[index] ?? string.Empty);
        }

        private static OperationsMilestoneSaveData WriteMilestone(OperationsMilestoneComponent source) => new()
        {
            missionId = source.MissionId ?? string.Empty,
            attempted = source.Attempted,
            victory = source.Victory,
            attemptCount = source.AttemptCount
        };

        private static OperationsOfferSaveData WriteOffer(OperationsOfferComponent source) => new()
        {
            offerId = source.OfferId ?? string.Empty,
            missionId = source.MissionId ?? string.Empty,
            districtId = source.DistrictId ?? string.Empty,
            day = source.Day,
            deployable = source.Deployable,
            urgent = source.Urgent
        };

        private static OperationsIncidentSaveData WriteIncident(OperationsIncidentComponent source) => new()
        {
            incidentId = source.IncidentId ?? string.Empty,
            districtId = source.DistrictId ?? string.Empty,
            missionId = source.MissionId ?? string.Empty,
            offerId = source.OfferId ?? string.Empty,
            kind = (byte)source.Kind,
            createdDay = source.CreatedDay,
            dueDay = source.DueDay,
            siteId = source.SiteId ?? string.Empty,
            routeId = source.RouteId ?? string.Empty
        };

        private static OperationsSiteSaveData WriteSite(OperationsSiteStateComponent source) => new()
        {
            siteId = source.SiteId ?? string.Empty,
            districtId = source.DistrictId ?? string.Empty,
            state = (byte)source.State
        };

        private static OperationsRouteSaveData WriteRoute(OperationsRouteStateComponent source) => new()
        {
            routeId = source.RouteId ?? string.Empty,
            districtId = source.DistrictId ?? string.Empty,
            state = (byte)source.State
        };

        private static OperationsAttemptSaveData WriteAttempt(OperationsAttemptComponent source) => new()
        {
            sessionId = source.SessionId ?? string.Empty,
            offerId = source.OfferId ?? string.Empty,
            missionId = source.MissionId ?? string.Empty,
            districtId = source.DistrictId ?? string.Empty,
            attemptOrdinal = source.AttemptOrdinal,
            transactionId = source.TransactionId ?? string.Empty,
            snapshotHash = source.SnapshotHash ?? string.Empty,
            resultHash = source.ResultHash ?? string.Empty,
            phase = source.Phase,
            practice = source.Practice,
            apRefunded = source.ApRefunded
        };
    }
}
