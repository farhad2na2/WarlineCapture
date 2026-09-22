using System;
using System.Collections.Generic;
using Game.Operations.Contracts;

namespace Game.Operations.Strategic
{
    public static class OperationsStableIds
    {
        public static uint Mix(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619u;
                return hash;
            }
        }

        public static uint Mix(uint hash, string text)
        {
            if (string.IsNullOrEmpty(text))
                return Mix(hash, 0);
            for (int index = 0; index < text.Length; index++)
                hash = Mix(hash, text[index]);
            return hash;
        }

        public static string Hex8(uint hash)
        {
            string text = hash.ToString("x8");
            return text;
        }

        public static string OfferId(string runId, int day, string missionId, int directorVersion)
        {
            uint hash = Mix(2166136261u, runId);
            hash = Mix(hash, day);
            hash = Mix(hash, missionId);
            hash = Mix(hash, directorVersion);
            string missionNumber = "o000";
            if (OperationsIdentityRules.TryParseMissionNumber(missionId, out int number))
                missionNumber = "o" + number.ToString("000");
            return "offer.operations." + Hex8(hash) + missionNumber;
        }

        public static string IncidentId(string runId, int day, string districtId, int kind)
        {
            uint hash = Mix(2166136261u, runId);
            hash = Mix(hash, day);
            hash = Mix(hash, districtId);
            hash = Mix(hash, kind);
            return "incident.operations." + Hex8(hash);
        }

        public static string Generated(string name, int sequence)
        {
            uint mixed = Mix(2166136261u, name);
            mixed = Mix(mixed, sequence);
            return name + ".operations." + Hex8(mixed);
        }

        public static string RunId(int seed) => "run.operations." + ((uint)seed).ToString("x8");

        public static uint TieBreak(int seed, int day, int districtNumber)
        {
            uint hash = Mix(2166136261u, seed);
            hash = Mix(hash, day);
            return Mix(hash, districtNumber);
        }
    }

    public static class OperationsRunInitializationSystem
    {
        public static OperationsCityWorld Create(int seed, OperationsDifficultyKind difficulty)
        {
            if (seed == 0)
                throw new ArgumentOutOfRangeException(nameof(seed));

            OperationsCityWorld world = new()
            {
                Run = new OperationsRunComponent
                {
                    RunId = OperationsStableIds.RunId(seed),
                    Day = OperationsCampaignSchema.StartingDay,
                    ActionPoints = OperationsCampaignSchema.DefaultStartingActionPoints,
                    Difficulty = difficulty,
                    Seed = seed,
                    Phase = OperationsRunPhaseKind.Dashboard,
                    DirectorVersion = OperationsOfferDirectorSystem.DirectorVersion,
                    PrngState = (uint)seed
                }
            };

            for (int number = 1; number <= OperationsIdentityRules.DistrictCount; number++)
            {
                OperationsDistrictMetricTuple start = OperationsDistrictMetricTuple.StartingForDistrict(number);
                world.Districts[number - 1] = new OperationsDistrictComponent
                {
                    DistrictId = OperationsIdentityRules.DistrictId(number),
                    Number = number,
                    Security = start.Security,
                    Trust = start.Trust,
                    Infrastructure = start.Infrastructure,
                    EnemyInfluence = start.EnemyInfluence,
                    IntelConfidence = start.IntelConfidence,
                    Heat = start.Heat,
                    SupplyReadiness = start.SupplyReadiness,
                    CivilianDensity = OperationsCivilianDensityKind.Medium,
                    PublicHintMissionId = string.Empty
                };
                world.Sites.Add(new OperationsSiteStateComponent
                {
                    SiteId = OperationsCityWorld.ServiceSiteId(number),
                    DistrictId = OperationsIdentityRules.DistrictId(number),
                    State = OperationsSiteStateKind.Damaged
                });
                world.Sites.Add(new OperationsSiteStateComponent
                {
                    SiteId = OperationsCityWorld.BackupSiteId(number),
                    DistrictId = OperationsIdentityRules.DistrictId(number),
                    State = OperationsSiteStateKind.Intact
                });
                world.Routes.Add(new OperationsRouteStateComponent
                {
                    RouteId = OperationsCityWorld.MainRouteId(number),
                    DistrictId = OperationsIdentityRules.DistrictId(number),
                    State = OperationsRouteStateKind.Open
                });
                string crossingId = OperationsCityWorld.CrossingRouteId(number);
                if (!string.IsNullOrEmpty(crossingId))
                {
                    world.Routes.Add(new OperationsRouteStateComponent
                    {
                        RouteId = crossingId,
                        DistrictId = OperationsIdentityRules.DistrictId(number),
                        State = OperationsRouteStateKind.Open
                    });
                }
            }

            OperationsOfferDirectorSystem.PublishOffers(world);
            return world;
        }
    }

    public static class OperationsOfferDirectorSystem
    {
        public const int DirectorVersion = 1;
        public const int MaxOffersPerDistrict = 2;
        public const int IncidentScoreThreshold = 60;

        public static bool IsSlotEligible(OperationsCityWorld world, string districtId, int slot)
        {
            if (slot <= 2)
                return true;
            if (slot <= 5)
                return Count(world, districtId, 1, 2, false) >= 1;
            if (slot <= 9)
                return Count(world, districtId, 1, 5, true) >= 2;
            return HasSlotVictory(world, districtId, 3) &&
                   HasSlotVictory(world, districtId, 6) &&
                   HasSlotVictory(world, districtId, 9);
        }

        public static void PublishOffers(OperationsCityWorld world)
        {
            world.Offers.Clear();
            for (int number = 1; number <= OperationsIdentityRules.DistrictCount; number++)
            {
                string districtId = OperationsIdentityRules.DistrictId(number);
                List<OperationsCatalogEntry> eligible = EligibleMissions(world, districtId);
                eligible.Sort((left, right) => CompareOffer(world, left, right));
                int take = eligible.Count < MaxOffersPerDistrict ? eligible.Count : MaxOffersPerDistrict;
                for (int index = 0; index < take; index++)
                    world.Offers.Add(CreateOffer(world, eligible[index], false));
            }

            RefreshPriorities(world);
            world.Run.PrngState = OperationsStableIds.Mix(world.Run.PrngState == 0 ? 1u : world.Run.PrngState, world.Run.Day);
        }

        public static void TryCreateIncident(OperationsCityWorld world)
        {
            if (ActiveIncidentCount(world) >= 2 || CreatedToday(world) >= 1)
                return;

            int bestIndex = -1;
            int bestScore = IncidentScoreThreshold;
            uint bestTie = uint.MaxValue;
            for (int index = 0; index < world.Districts.Length; index++)
            {
                if (DistrictHasIncident(world, world.Districts[index].DistrictId))
                    continue;
                int score = Score(world.Districts[index]);
                if (score <= IncidentScoreThreshold)
                    continue;
                uint tie = OperationsStableIds.TieBreak(world.Run.Seed, world.Run.Day, world.Districts[index].Number);
                if (score > bestScore || (score == bestScore && tie < bestTie))
                {
                    bestScore = score;
                    bestTie = tie;
                    bestIndex = index;
                }
            }

            if (bestIndex < 0)
                return;

            OperationsDistrictComponent district = world.Districts[bestIndex];
            if (!TryChooseIncident(world, district, out OperationsIncidentKind kind, out OperationsCatalogEntry mission, out string siteId, out string routeId))
                return;
            if (!TryReserveOfferSlot(world, mission, out string offerId))
                return;

            OperationsIncidentComponent incident = new()
            {
                IncidentId = OperationsStableIds.IncidentId(world.Run.RunId, world.Run.Day, district.DistrictId, (int)kind),
                DistrictId = district.DistrictId,
                MissionId = mission.MissionId,
                OfferId = offerId,
                Kind = kind,
                CreatedDay = world.Run.Day,
                DueDay = world.Run.Day + 1,
                SiteId = siteId ?? string.Empty,
                RouteId = routeId ?? string.Empty
            };
            world.Incidents.Add(incident);
            MarkUrgent(world, offerId);
            RefreshPriorities(world);
        }

        public static int Count(OperationsCityWorld world, string districtId, int minSlot, int maxSlot, bool victoryOnly)
        {
            int count = 0;
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                OperationsCatalogEntry entry = OperationsCatalogIndex.Entries[index];
                if (entry.DistrictId != districtId || entry.LocalSlot < minSlot || entry.LocalSlot > maxSlot)
                    continue;
                if (victoryOnly)
                {
                    if (world.HasVictory(entry.MissionId))
                        count++;
                }
                else if (world.HasAttempt(entry.MissionId))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasSlotVictory(OperationsCityWorld world, string districtId, int slot)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                OperationsCatalogEntry entry = OperationsCatalogIndex.Entries[index];
                if (entry.DistrictId == districtId && entry.LocalSlot == slot)
                    return world.HasVictory(entry.MissionId);
            }

            return false;
        }

        private static List<OperationsCatalogEntry> EligibleMissions(OperationsCityWorld world, string districtId)
        {
            List<OperationsCatalogEntry> eligible = new();
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                OperationsCatalogEntry entry = OperationsCatalogIndex.Entries[index];
                if (entry.DistrictId != districtId)
                    continue;
                if (entry.LocalSlot == 10 && world.HasVictory(entry.MissionId))
                    continue;
                if (!IsSlotEligible(world, districtId, entry.LocalSlot))
                    continue;
                eligible.Add(entry);
            }

            return eligible;
        }

        private static int CompareOffer(OperationsCityWorld world, OperationsCatalogEntry left, OperationsCatalogEntry right)
        {
            int rank = Rank(world, left).CompareTo(Rank(world, right));
            if (rank != 0)
                return rank;
            int slot = left.LocalSlot.CompareTo(right.LocalSlot);
            if (slot != 0)
                return slot;
            return string.CompareOrdinal(left.MissionId, right.MissionId);
        }

        private static int Rank(OperationsCityWorld world, OperationsCatalogEntry entry)
        {
            if (IsNominated(world, entry.MissionId))
                return 0;
            if (!world.HasVictory(entry.MissionId))
                return 1;
            return 2;
        }

        private static bool IsNominated(OperationsCityWorld world, string missionId)
        {
            for (int index = 0; index < world.Incidents.Count; index++)
            {
                if (world.Incidents[index].MissionId == missionId)
                    return true;
            }

            return false;
        }

        private static OperationsOfferComponent CreateOffer(OperationsCityWorld world, OperationsCatalogEntry entry, bool urgent)
        {
            return new OperationsOfferComponent
            {
                OfferId = OperationsStableIds.OfferId(world.Run.RunId, world.Run.Day, entry.MissionId, world.Run.DirectorVersion),
                MissionId = entry.MissionId,
                DistrictId = entry.DistrictId,
                Day = world.Run.Day,
                Deployable = IsDeployable(world, entry),
                Urgent = urgent
            };
        }

        public static bool IsDeployable(OperationsCityWorld world, OperationsCatalogEntry entry)
        {
            int intel = DistrictIntel(world, entry.DistrictId);
            if (intel < entry.MinIntel)
                return false;
            if ((entry.Family == OperationsMissionFamilyKind.Raid || entry.Family == OperationsMissionFamilyKind.Breach) &&
                intel < OperationsBalanceSchema.DefaultRaidMinimumIntel)
                return false;
            return true;
        }

        private static int DistrictIntel(OperationsCityWorld world, string districtId)
        {
            int index = world.DistrictIndex(districtId);
            if (index < 0)
                return 0;
            return world.Districts[index].IntelConfidence;
        }

        private static void RefreshPriorities(OperationsCityWorld world)
        {
            List<OperationsOfferComponent> ranked = new(world.Offers);
            ranked.Sort((left, right) =>
            {
                int urgent = right.Urgent.CompareTo(left.Urgent);
                if (urgent != 0)
                    return urgent;
                return string.CompareOrdinal(left.MissionId, right.MissionId);
            });
            world.PriorityOfferIds.Clear();
            int take = ranked.Count < 3 ? ranked.Count : 3;
            for (int index = 0; index < take; index++)
                world.PriorityOfferIds.Add(ranked[index].OfferId);
        }

        private static int ActiveIncidentCount(OperationsCityWorld world) => world.Incidents.Count;

        private static int CreatedToday(OperationsCityWorld world)
        {
            int count = 0;
            for (int index = 0; index < world.Incidents.Count; index++)
            {
                if (world.Incidents[index].CreatedDay == world.Run.Day)
                    count++;
            }

            return count;
        }

        private static bool DistrictHasIncident(OperationsCityWorld world, string districtId)
        {
            for (int index = 0; index < world.Incidents.Count; index++)
            {
                if (world.Incidents[index].DistrictId == districtId)
                    return true;
            }

            return false;
        }

        public static int Score(OperationsDistrictComponent district) =>
            (2 * district.EnemyInfluence) + district.Heat - district.Security - (district.Infrastructure / 2);

        private static bool TryChooseIncident(
            OperationsCityWorld world,
            OperationsDistrictComponent district,
            out OperationsIncidentKind kind,
            out OperationsCatalogEntry mission,
            out string siteId,
            out string routeId)
        {
            kind = OperationsIncidentKind.None;
            mission = default;
            siteId = OperationsCityWorld.ServiceSiteId(district.Number);
            routeId = OperationsCityWorld.MainRouteId(district.Number);

            if (district.Infrastructure < 40 &&
                !OnCooldown(world, district.DistrictId, OperationsIncidentKind.ServiceDisruption) &&
                TryLowest(world, district.DistrictId, OperationsMissionFamilyKind.Repair, out mission))
            {
                kind = OperationsIncidentKind.ServiceDisruption;
                return true;
            }

            if (district.SupplyReadiness < 40 &&
                !OnCooldown(world, district.DistrictId, OperationsIncidentKind.RoadBlockade) &&
                (TryLowest(world, district.DistrictId, OperationsMissionFamilyKind.Escort, out mission) ||
                 TryLowest(world, district.DistrictId, OperationsMissionFamilyKind.Interdict, out mission)))
            {
                kind = OperationsIncidentKind.RoadBlockade;
                return true;
            }

            if (OnCooldown(world, district.DistrictId, OperationsIncidentKind.HostilePressure))
                return false;
            if (TryLowest(world, district.DistrictId, OperationsMissionFamilyKind.Defense, out mission) ||
                TryLowest(world, district.DistrictId, OperationsMissionFamilyKind.Patrol, out mission) ||
                TryLowestEligible(world, district.DistrictId, out mission))
            {
                kind = OperationsIncidentKind.HostilePressure;
                return true;
            }

            return false;
        }

        private static bool OnCooldown(OperationsCityWorld world, string districtId, OperationsIncidentKind kind)
        {
            for (int index = 0; index < world.Cooldowns.Count; index++)
            {
                OperationsCooldownComponent cooldown = world.Cooldowns[index];
                if (cooldown.DistrictId == districtId && cooldown.Kind == kind && world.Run.Day < cooldown.AvailableOnDay)
                    return true;
            }

            return false;
        }

        private static bool TryLowest(
            OperationsCityWorld world,
            string districtId,
            OperationsMissionFamilyKind family,
            out OperationsCatalogEntry mission)
        {
            mission = default;
            bool found = false;
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                OperationsCatalogEntry entry = OperationsCatalogIndex.Entries[index];
                if (entry.DistrictId != districtId || entry.Family != family)
                    continue;
                if (!IsSlotEligible(world, districtId, entry.LocalSlot) || world.HasVictory(entry.MissionId))
                    continue;
                if (!found || string.CompareOrdinal(entry.MissionId, mission.MissionId) < 0)
                {
                    mission = entry;
                    found = true;
                }
            }

            return found;
        }

        private static bool TryLowestEligible(OperationsCityWorld world, string districtId, out OperationsCatalogEntry mission)
        {
            mission = default;
            bool found = false;
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                OperationsCatalogEntry entry = OperationsCatalogIndex.Entries[index];
                if (entry.DistrictId != districtId || entry.LocalSlot == 10)
                    continue;
                if (!IsSlotEligible(world, districtId, entry.LocalSlot) || world.HasVictory(entry.MissionId))
                    continue;
                if (!found || string.CompareOrdinal(entry.MissionId, mission.MissionId) < 0)
                {
                    mission = entry;
                    found = true;
                }
            }

            return found;
        }

        private static bool TryReserveOfferSlot(OperationsCityWorld world, OperationsCatalogEntry mission, out string offerId)
        {
            for (int index = 0; index < world.Offers.Count; index++)
            {
                if (world.Offers[index].MissionId == mission.MissionId)
                {
                    offerId = world.Offers[index].OfferId;
                    return true;
                }
            }

            int districtOffers = 0;
            int replaceIndex = -1;
            for (int index = 0; index < world.Offers.Count; index++)
            {
                if (world.Offers[index].DistrictId != mission.DistrictId)
                    continue;
                districtOffers++;
                if (!IsStarter(world.Offers[index].MissionId))
                    replaceIndex = index;
            }

            OperationsOfferComponent created = CreateOffer(world, mission, true);
            if (districtOffers < MaxOffersPerDistrict)
            {
                world.Offers.Add(created);
                offerId = created.OfferId;
                return true;
            }

            if (replaceIndex < 0)
                replaceIndex = HighestSlotOffer(world, mission.DistrictId);
            if (replaceIndex < 0 || districtOffers < 2)
            {
                offerId = string.Empty;
                return false;
            }

            world.Offers[replaceIndex] = created;
            offerId = created.OfferId;
            return true;
        }

        private static int HighestSlotOffer(OperationsCityWorld world, string districtId)
        {
            int replaceIndex = -1;
            int slot = -1;
            for (int index = 0; index < world.Offers.Count; index++)
            {
                if (world.Offers[index].DistrictId != districtId)
                    continue;
                int candidate = SlotOf(world.Offers[index].MissionId);
                if (candidate > slot)
                {
                    slot = candidate;
                    replaceIndex = index;
                }
            }

            return replaceIndex;
        }

        private static int SlotOf(string missionId)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId == missionId)
                    return OperationsCatalogIndex.Entries[index].LocalSlot;
            }

            return 0;
        }

        private static bool IsStarter(string missionId)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId == missionId)
                    return OperationsCatalogIndex.Entries[index].LocalSlot <= 2;
            }

            return false;
        }

        private static void MarkUrgent(OperationsCityWorld world, string offerId)
        {
            for (int index = 0; index < world.Offers.Count; index++)
            {
                if (world.Offers[index].OfferId != offerId)
                    continue;
                OperationsOfferComponent offer = world.Offers[index];
                offer.Urgent = true;
                world.Offers[index] = offer;
                return;
            }
        }
    }

    public static class OperationsActionSystem
    {
        public static bool TryApply(OperationsCityWorld world, OperationsCommand command, out OperationsReasonCode reason)
        {
            reason = OperationsReasonCode.None;
            if (!world.HasActiveRun)
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            if (world.Run.Phase != OperationsRunPhaseKind.Dashboard || world.FindReservedLiveAttempt(out _))
            {
                reason = OperationsReasonCode.AttemptConflict;
                return false;
            }

            if (!TrySchema(command.ActionId, out OperationsActionSchema schema))
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            int districtIndex = world.DistrictIndex(command.DistrictId);
            if (districtIndex < 0)
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            string limitKey = schema.CitywideOncePerDay ? string.Empty : command.DistrictId;
            if (world.ActionUsed(limitKey, schema.Kind))
            {
                reason = OperationsReasonCode.ActionLimitReached;
                return false;
            }

            if (world.Run.ActionPoints < 1)
            {
                reason = OperationsReasonCode.InsufficientActionPoints;
                return false;
            }

            if (schema.Kind == OperationsAbstractActionKind.Service &&
                world.Districts[districtIndex].EnemyInfluence > 70)
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            OperationsSignedMetricDelta delta = new(
                schema.SecurityDelta,
                schema.TrustDelta,
                schema.InfrastructureDelta,
                0,
                schema.IntelDelta,
                schema.HeatDelta,
                schema.SupplyDelta);
            OperationsMetricApplication applied = OperationsMetricMath.Apply(world.Districts[districtIndex], delta);
            world.Districts[districtIndex] = applied.District;
            if (schema.Kind == OperationsAbstractActionKind.Analyze)
            {
                OperationsDistrictComponent district = world.Districts[districtIndex];
                district.PublicHintMissionId = PublicHint(world, command.DistrictId);
                world.Districts[districtIndex] = district;
            }

            world.ActionUses.Add(new OperationsActionUseComponent
            {
                DistrictId = limitKey,
                Kind = schema.Kind
            });
            world.Run.ActionPoints--;
            return true;
        }

        public static bool TrySchema(string actionId, out OperationsActionSchema schema)
        {
            OperationsActionSchema[] actions = OperationsActionSchema.CreateBaseline();
            for (int index = 0; index < actions.Length; index++)
            {
                if (actions[index].ActionId == actionId)
                {
                    schema = actions[index];
                    return true;
                }
            }

            schema = default;
            return false;
        }

        private static string PublicHint(OperationsCityWorld world, string districtId)
        {
            string hint = string.Empty;
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                OperationsCatalogEntry entry = OperationsCatalogIndex.Entries[index];
                if (entry.DistrictId != districtId || world.HasVictory(entry.MissionId))
                    continue;
                if (hint.Length == 0 || string.CompareOrdinal(entry.MissionId, hint) < 0)
                    hint = entry.MissionId;
            }

            return hint;
        }
    }

    public static class OperationsDayAdvanceSystem
    {
        public const int StableSecurity = 60;
        public const int StableTrust = 50;
        public const int StableInfrastructure = 50;
        public const int StableEnemy = 35;
        public const int StableSupply = 40;
        public const int RequiredStableDays = 2;

        public static bool TryAdvance(
            OperationsCityWorld world,
            out OperationsReasonCode reason,
            out OperationsDayReportSaveData report)
        {
            reason = OperationsReasonCode.None;
            report = null;
            if (!world.HasActiveRun)
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            if (world.Run.Phase != OperationsRunPhaseKind.Dashboard || world.FindReservedLiveAttempt(out _))
            {
                reason = OperationsReasonCode.AttemptConflict;
                return false;
            }

            OperationsSignedMetricDelta[] requested = new OperationsSignedMetricDelta[world.Districts.Length];
            int expired = ExpireIncidents(world, requested);
            AccumulatePressure(world, requested);
            OperationsDistrictDeltaSaveData[] deltas = new OperationsDistrictDeltaSaveData[world.Districts.Length];
            for (int index = 0; index < world.Districts.Length; index++)
            {
                OperationsDistrictComponent before = world.Districts[index];
                OperationsMetricApplication applied = OperationsMetricMath.Apply(before, requested[index]);
                world.Districts[index] = applied.District;
                deltas[index] = ToDelta(before.DistrictId, applied);
            }

            bool stable = EvaluateStability(world);
            if (stable)
                world.Run.ConsecutiveStableDays++;
            else
                world.Run.ConsecutiveStableDays = 0;
            if (world.Run.ConsecutiveStableDays >= RequiredStableDays)
                world.Run.CityCompleted = true;

            int closedDay = world.Run.Day;
            report = new OperationsDayReportSaveData
            {
                day = closedDay,
                reportId = "report.operations." + OperationsStableIds.Hex8(OperationsStableIds.Mix(world.Run.PrngState, closedDay)),
                expiredIncidentCount = expired,
                cityStableToday = stable,
                consecutiveStableDays = world.Run.ConsecutiveStableDays,
                districtDeltas = deltas
            };

            world.Run.Day++;
            world.Run.ActionPoints = OperationsCampaignSchema.DefaultStartingActionPoints;
            world.Run.LiveVictoriesToday = 0;
            world.ActionUses.Clear();
            AbandonPracticeAttempts(world);
            OperationsOfferDirectorSystem.PublishOffers(world);
            OperationsOfferDirectorSystem.TryCreateIncident(world);
            world.Run.Phase = OperationsRunPhaseKind.Dashboard;
            return true;
        }

        private static int ExpireIncidents(OperationsCityWorld world, OperationsSignedMetricDelta[] requested)
        {
            int expired = 0;
            for (int index = world.Incidents.Count - 1; index >= 0; index--)
            {
                OperationsIncidentComponent incident = world.Incidents[index];
                if (incident.DueDay > world.Run.Day)
                    continue;
                int districtIndex = world.DistrictIndex(incident.DistrictId);
                if (districtIndex >= 0)
                    requested[districtIndex] += OperationsSignedMetricDelta.IncidentExpiry;
                if (incident.Kind == OperationsIncidentKind.ServiceDisruption)
                    world.SetSite(incident.SiteId, OperationsSiteStateKind.Damaged);
                else if (incident.Kind == OperationsIncidentKind.RoadBlockade)
                    world.SetRoute(incident.RouteId, OperationsRouteStateKind.Contested);
                world.Cooldowns.Add(new OperationsCooldownComponent
                {
                    DistrictId = incident.DistrictId,
                    Kind = incident.Kind,
                    AvailableOnDay = world.Run.Day + 3
                });
                world.Incidents.RemoveAt(index);
                expired++;
            }

            return expired;
        }

        private static void AccumulatePressure(OperationsCityWorld world, OperationsSignedMetricDelta[] requested)
        {
            OperationsDistrictComponent[] snapshot = new OperationsDistrictComponent[world.Districts.Length];
            for (int index = 0; index < world.Districts.Length; index++)
            {
                snapshot[index] = world.Districts[index];
                snapshot[index].Security += requested[index].Security;
                snapshot[index].Trust += requested[index].Trust;
                snapshot[index].Infrastructure += requested[index].Infrastructure;
                snapshot[index].EnemyInfluence += requested[index].EnemyInfluence;
                snapshot[index].IntelConfidence += requested[index].IntelConfidence;
                snapshot[index].Heat += requested[index].Heat;
                snapshot[index].SupplyReadiness += requested[index].SupplyReadiness;
            }

            OperationsCampaignSchema campaign = OperationsCampaignSchema.CreateBaseline();
            for (int index = 0; index < snapshot.Length; index++)
            {
                OperationsDistrictComponent metric = snapshot[index];
                int pressure = (metric.EnemyInfluence >= 60 ? 2 : 0) +
                               (metric.Heat >= 70 ? 1 : 0) +
                               (metric.Infrastructure < 30 ? 1 : 0);
                int enemy = metric.Security < 30 ? 2 : 0;
                if (HasAdjacentHighEnemy(campaign, snapshot, metric.Number))
                    enemy++;
                bool finale = HasFinaleVictory(world, metric.DistrictId);
                requested[index] += new OperationsSignedMetricDelta(
                    -pressure + (finale ? 2 : 0),
                    0,
                    0,
                    enemy + (finale ? -2 : 0),
                    -3,
                    -5,
                    -(metric.EnemyInfluence >= 70 ? 2 : 0) + (finale ? 2 : 0));
            }
        }

        private static bool HasAdjacentHighEnemy(
            OperationsCampaignSchema campaign,
            OperationsDistrictComponent[] snapshot,
            int districtNumber)
        {
            for (int index = 0; index < campaign.Adjacency.Length; index++)
            {
                OperationsAdjacencyPair pair = campaign.Adjacency[index];
                int other = pair.LeftDistrict == districtNumber ? pair.RightDistrict :
                    pair.RightDistrict == districtNumber ? pair.LeftDistrict : 0;
                if (other == 0)
                    continue;
                if (snapshot[other - 1].EnemyInfluence >= 80)
                    return true;
            }

            return false;
        }

        public static bool HasFinaleVictory(OperationsCityWorld world, string districtId)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                OperationsCatalogEntry entry = OperationsCatalogIndex.Entries[index];
                if (entry.DistrictId == districtId && entry.LocalSlot == 10)
                    return world.HasVictory(entry.MissionId);
            }

            return false;
        }

        public static bool EvaluateStability(OperationsCityWorld world)
        {
            for (int index = 0; index < world.Districts.Length; index++)
            {
                OperationsDistrictComponent district = world.Districts[index];
                if (!HasFinaleVictory(world, district.DistrictId))
                    return false;
                if (district.Security < StableSecurity ||
                    district.Trust < StableTrust ||
                    district.Infrastructure < StableInfrastructure ||
                    district.EnemyInfluence > StableEnemy ||
                    district.SupplyReadiness < StableSupply)
                    return false;
            }

            return true;
        }

        private static void AbandonPracticeAttempts(OperationsCityWorld world)
        {
            for (int index = 0; index < world.Attempts.Count; index++)
            {
                if (!world.Attempts[index].Practice || world.Attempts[index].Phase != OperationsAttemptPhase.Reserved)
                    continue;
                OperationsAttemptComponent attempt = world.Attempts[index];
                attempt.Phase = OperationsAttemptPhase.RolledBack;
                world.Attempts[index] = attempt;
            }
        }

        private static OperationsDistrictDeltaSaveData ToDelta(string districtId, OperationsMetricApplication applied) => new()
        {
            districtId = districtId ?? string.Empty,
            requestedSecurity = applied.Requested.Security,
            requestedTrust = applied.Requested.Trust,
            requestedInfrastructure = applied.Requested.Infrastructure,
            requestedEnemyInfluence = applied.Requested.EnemyInfluence,
            requestedIntelConfidence = applied.Requested.IntelConfidence,
            requestedHeat = applied.Requested.Heat,
            requestedSupplyReadiness = applied.Requested.SupplyReadiness,
            appliedSecurity = applied.Applied.Security,
            appliedTrust = applied.Applied.Trust,
            appliedInfrastructure = applied.Applied.Infrastructure,
            appliedEnemyInfluence = applied.Applied.EnemyInfluence,
            appliedIntelConfidence = applied.Applied.IntelConfidence,
            appliedHeat = applied.Applied.Heat,
            appliedSupplyReadiness = applied.Applied.SupplyReadiness
        };
    }

    public static class OperationsRewardSystem
    {
        public static void ApplyLiveVictory(OperationsSaveData profile, string missionId, string transactionId)
        {
            OperationsRewardSchema schema = OperationsRewardSchema.CreateBaseline();
            int credits = 0;
            int xp = 0;
            if (!Contains(profile.firstClearRewardIds, missionId))
            {
                credits += schema.FirstClearCreditsAmount;
                xp += schema.FirstClearCommanderXpAmount;
                profile.firstClearRewardIds = Append(profile.firstClearRewardIds, missionId);
            }

            int room = schema.RepeatCreditsDailyCapAmount - profile.repeatCreditsGrantedOnDay;
            if (room < 0)
                room = 0;
            if (room > 0)
            {
                int grant = schema.RepeatVictoryCreditsAmount;
                if (grant > room)
                    grant = room;
                credits += grant;
                profile.repeatCreditsGrantedOnDay += grant;
            }

            Grant(profile, missionId, transactionId, "live-victory", credits, xp);
        }

        public static void ApplyEndDayBonus(OperationsSaveData profile, string transactionId, int victoriesToday)
        {
            if (victoriesToday <= 0)
                return;
            OperationsRewardSchema schema = OperationsRewardSchema.CreateBaseline();
            Grant(profile, string.Empty, transactionId, "end-day-victory", schema.FirstEndDayWithVictoryCreditsAmount, 0);
        }

        public static void Grant(
            OperationsSaveData profile,
            string missionId,
            string transactionId,
            string grantKind,
            int credits,
            int xp)
        {
            profile.operationsRewardCredits += credits;
            profile.operationsRewardCommanderXp += xp;
            OperationsRewardEntrySaveData[] ledger = profile.rewardLedger ?? Array.Empty<OperationsRewardEntrySaveData>();
            OperationsRewardEntrySaveData[] grown = new OperationsRewardEntrySaveData[ledger.Length + 1];
            Array.Copy(ledger, grown, ledger.Length);
            grown[ledger.Length] = new OperationsRewardEntrySaveData
            {
                transactionId = transactionId ?? string.Empty,
                missionId = missionId ?? string.Empty,
                rewardCredits = credits,
                rewardCommanderXp = xp,
                grantKind = grantKind ?? string.Empty
            };
            profile.rewardLedger = grown;
        }

        private static bool Contains(string[] values, string missionId)
        {
            if (values == null)
                return false;
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == missionId)
                    return true;
            }

            return false;
        }

        private static string[] Append(string[] values, string missionId)
        {
            string[] source = values ?? Array.Empty<string>();
            string[] grown = new string[source.Length + 1];
            Array.Copy(source, grown, source.Length);
            grown[source.Length] = missionId;
            return grown;
        }
    }

    public static class OperationsConsequenceSystem
    {
        public static bool TryApply(
            OperationsCityWorld world,
            OperationsSaveData profile,
            OperationsMissionResult result,
            out OperationsReasonCode reason)
        {
            reason = OperationsReasonCode.None;
            int attemptIndex = world.FindAttemptBySession(result.SessionId);
            if (attemptIndex < 0)
            {
                reason = OperationsReasonCode.AttemptConflict;
                return false;
            }

            OperationsAttemptComponent attempt = world.Attempts[attemptIndex];
            if (attempt.Phase != OperationsAttemptPhase.Reserved ||
                attempt.OfferId != result.OfferId ||
                attempt.MissionId != result.MissionId ||
                attempt.AttemptOrdinal != result.AttemptOrdinal ||
                result.RunId != world.Run.RunId)
            {
                reason = OperationsReasonCode.Conflict;
                return false;
            }

            if (!string.IsNullOrEmpty(attempt.ResultHash))
            {
                reason = attempt.ResultHash == result.ResultHash
                    ? OperationsReasonCode.DuplicateCommand
                    : OperationsReasonCode.Conflict;
                return false;
            }

            if (result.Outcome == OperationsOutcomeKind.TechnicalFailure)
            {
                reason = OperationsReasonCode.TechnicalFailure;
                return false;
            }

            if (!TryFamily(result.MissionId, out OperationsMissionFamilyKind family, out string districtId))
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            if (!attempt.Practice && result.Outcome != OperationsOutcomeKind.TechnicalFailure)
            {
                OperationsSignedMetricDelta delta = OutcomeDelta(result.Outcome, family);
                delta += OperationsMetricMath.Harm(result);
                int districtIndex = world.DistrictIndex(districtId);
                OperationsMetricApplication applied = OperationsMetricMath.Apply(world.Districts[districtIndex], delta);
                world.Districts[districtIndex] = applied.District;
                ApplySiteFacts(world, result, family, districtId);
                if (result.Outcome == OperationsOutcomeKind.Victory)
                {
                    world.MarkVictory(result.MissionId);
                    RememberEvidence(world, result.ExtractedEvidenceIds);
                    OperationsRewardSystem.ApplyLiveVictory(profile, result.MissionId, attempt.TransactionId);
                    world.Run.LiveVictoriesToday++;
                    ResolveIncident(world, result.MissionId);
                }
            }

            attempt.Phase = OperationsAttemptPhase.Settled;
            attempt.ResultHash = result.ResultHash;
            world.Attempts[attemptIndex] = attempt;
            if (!attempt.Practice)
                world.Run.Phase = OperationsRunPhaseKind.Dashboard;
            return true;
        }

        public static OperationsSignedMetricDelta OutcomeDelta(OperationsOutcomeKind outcome, OperationsMissionFamilyKind family)
        {
            switch (outcome)
            {
                case OperationsOutcomeKind.Victory:
                    return OperationsSignedMetricDelta.Victory(family);
                case OperationsOutcomeKind.Partial:
                    return OperationsSignedMetricDelta.HalfTowardZero(OperationsSignedMetricDelta.Victory(family));
                case OperationsOutcomeKind.Defeat:
                    return OperationsSignedMetricDelta.Defeat;
                case OperationsOutcomeKind.Withdrawn:
                    return OperationsSignedMetricDelta.Withdrawn;
                default:
                    return OperationsSignedMetricDelta.Zero;
            }
        }

        private static bool TryFamily(string missionId, out OperationsMissionFamilyKind family, out string districtId)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId != missionId)
                    continue;
                family = OperationsCatalogIndex.Entries[index].Family;
                districtId = OperationsCatalogIndex.Entries[index].DistrictId;
                return true;
            }

            family = OperationsMissionFamilyKind.None;
            districtId = string.Empty;
            return false;
        }

        private static void ApplySiteFacts(
            OperationsCityWorld world,
            OperationsMissionResult result,
            OperationsMissionFamilyKind family,
            string districtId)
        {
            OperationsObjectiveFact[] facts = result.ProtectedSiteFacts ?? Array.Empty<OperationsObjectiveFact>();
            for (int index = 0; index < facts.Length; index++)
            {
                string siteId = facts[index].NodeId;
                if (!OperationsIdentityRules.IsValidAnchorId(siteId))
                {
                    if (!OperationsIdentityRules.TryParseDistrictNumber(districtId, out int number))
                        continue;
                    siteId = OperationsCityWorld.ServiceSiteId(number);
                }

                if (facts[index].Failed)
                    world.SetSite(siteId, OperationsSiteStateKind.Damaged);
                else if (facts[index].Completed &&
                         family == OperationsMissionFamilyKind.Repair &&
                         (result.Outcome == OperationsOutcomeKind.Victory || result.Outcome == OperationsOutcomeKind.Partial))
                    world.SetSite(siteId, OperationsSiteStateKind.Restored);
            }
        }

        private static void RememberEvidence(OperationsCityWorld world, string[] evidenceIds)
        {
            if (evidenceIds == null)
                return;
            for (int index = 0; index < evidenceIds.Length; index++)
            {
                string evidenceId = evidenceIds[index];
                if (string.IsNullOrEmpty(evidenceId) || world.EvidenceFlags.Contains(evidenceId))
                    continue;
                world.EvidenceFlags.Add(evidenceId);
            }
        }

        private static void ResolveIncident(OperationsCityWorld world, string missionId)
        {
            for (int index = world.Incidents.Count - 1; index >= 0; index--)
            {
                if (world.Incidents[index].MissionId != missionId)
                    continue;
                OperationsIncidentComponent incident = world.Incidents[index];
                world.Cooldowns.Add(new OperationsCooldownComponent
                {
                    DistrictId = incident.DistrictId,
                    Kind = incident.Kind,
                    AvailableOnDay = world.Run.Day + 3
                });
                world.Incidents.RemoveAt(index);
            }
        }
    }
}
