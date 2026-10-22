using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Operations.Contracts;
using Game.Operations.Strategic;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    public static class MainMenuModeProgressComposition
    {
        public static MainMenuModeProgress Read(MainMenuMode mode)
        {
            try { return Project(mode, SaveService.CreateDefault().LoadProfile(), id => ContentAccessRuntime.Evaluate(id)); }
            catch (System.IO.IOException) { return default; }
            catch (UnauthorizedAccessException) { return default; }
        }
        // Explicit inputs make publication, replay, access and duplicate-save cases independently verifiable.
        public static MainMenuModeProgress Project(MainMenuMode mode, PlayerProfileSaveData profile,
            Func<string, ContentAccessState> access)
        {
            if (profile == null) return default;
            return mode == MainMenuMode.Skirmish ? Skirmish(profile, access) : Operations(profile, access);
        }
        private static MainMenuModeProgress Skirmish(PlayerProfileSaveData profile, Func<string, ContentAccessState> access)
        {
            var catalog = SkirmishBattleCatalogConfig.Load();
            if (catalog == null) return default;
            var model = new MainMenuModeProgress { Available = true, Total = catalog.Entries.Count };
            var ledger = new SkirmishMedalLedger(profile.skirmishMedals, catalog.Entries);
            SkirmishBattleCatalogEntry? first = null, next = null;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in catalog.Entries)
            {
                if (!seen.Add(entry.ScenarioId)) continue;
                bool won = false;
                for (int tier=0;tier<4;tier++)
                {
                    if (!ledger.Has(entry.ScenarioId, (SkirmishDifficultyId)(tier+1))) continue;
                    won = true; AddMedal(ref model, tier);
                }
                if (won) model.Completed++;
                if (!entry.IsPlayable) continue;
                model.Published++;
                if (access("skirmish."+entry.ScenarioId.ToLowerInvariant()) != ContentAccessState.Allowed) continue;
                if (!first.HasValue) first = entry;
                if (!won && !next.HasValue) next = entry;
            }
            var chosen = next ?? first;
            if (chosen.HasValue)
            {
                var entry = chosen.Value;
                model.HasNext = true; model.Replay = !next.HasValue;
                model.MissionId = entry.ScenarioId; model.TitleKey = entry.TitleKey;
                model.Title = entry.TitleEnglish;
                model.PreviewKey = SkirmishLibraryPreviewAssets.ResolvePreviewKey(entry.ScenarioId, true, entry.MapId);
            }
            return model;
        }
        private static MainMenuModeProgress Operations(PlayerProfileSaveData profile, Func<string, ContentAccessState> access)
        {
            var model = new MainMenuModeProgress { Available = true, Total = OperationsCatalogIndex.Entries.Length };
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in OperationsCatalogIndex.Entries) known.Add(entry.MissionId);
            var cleared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in profile.operations?.firstClearRewardIds ?? Array.Empty<string>())
                if (known.Contains(id)) cleared.Add(id);
            model.Completed = cleared.Count;
            var medals = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in profile.operationsMissionMedals ?? Array.Empty<OperationsMissionMedalSaveData>())
                if (row != null && known.Contains(row.missionId) && row.difficulty < 4 &&
                    medals.Add(row.missionId + ":" + row.difficulty)) AddMedal(ref model, row.difficulty);

            // O001 is the sole tactical Operations adapter currently published. A catalog row alone
            // never makes O002..O060 playable. This list grows only with actual launch adapters.
            var definition = Resources.Load<OperationsReconMissionConfig>(OperationsReconMissionConfig.ResourcePath);
            if (definition == null || !definition.TryValidate(out _) || access(definition.missionId) != ContentAccessState.Allowed)
                return model;
            model.Published = 1;
            if (profile.operations != null && !OperationsSaveMigration.Migrate(profile.operations).CanWrite) return model;
            // Exercise the same strategic reducer on a detached in-memory snapshot. No AP, save,
            // run, journal or deployment is written by the recommendation.
            var detached = JsonUtility.FromJson<OperationsSaveData>(JsonUtility.ToJson(profile.operations ?? new OperationsSaveData()));
            var session = OperationsStrategicSession.FromMigration(OperationsSaveMigration.Migrate(detached));
            if (!session.HasActiveRun)
            {
                var created = session.SubmitNewRun(Command(session, OperationsCommandKind.NewRun), 1102, OperationsDifficultyKind.Regular);
                if (!created.Accepted) return model;
            }
            if (session.Save.pendingDeployment?.reserved == true)
            {
                var pending = Array.Find(session.Save.activeRun.attempts ?? Array.Empty<OperationsAttemptSaveData>(),
                    a => a.sessionId == session.Save.pendingDeployment.sessionId && !a.practice);
                if (pending == null || pending.missionId != definition.missionId) return model;
                model.Resume = true;
            }
            else
            {
                var offer = Array.Find(session.Offers, item => item.missionId == definition.missionId);
                if (offer == null) return model;
                var deploy = new OperationsCommand("cmd.operations.homepreview", session.Revision, OperationsCommandKind.Deploy,
                    offer.districtId, offer.offerId, string.Empty);
                if (!session.Submit(deploy).Accepted) return model;
            }
            model.HasNext = true; model.Replay = cleared.Contains(definition.missionId) && !model.Resume;
            model.MissionId = definition.missionId; model.TitleKey = "operations.o001.title";
            model.Title = "STREET SIGNALS — OLD QUARTER"; model.PreviewKey = "operation.o001";
            return model;
        }
        private static OperationsCommand Command(OperationsStrategicSession session, OperationsCommandKind kind) =>
            new("cmd.operations.homenewrun", session.Revision, kind, "", "", "");
        private static void AddMedal(ref MainMenuModeProgress model, int tier)
        {
            switch(tier) { case 0:model.Recruit++;break;case 1:model.Regular++;break;
                case 2:model.Veteran++;break;case 3:model.Commander++;break; }
        }
    }
}
