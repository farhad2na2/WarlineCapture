#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishAcceptanceCensusProbe
    {
        [MenuItem("Tools/Warline/Skirmish/Capture S002 Regular Standard Census")]
        public static void CaptureMenu() => Debug.Log(CaptureRegularStandard());

        public static string CaptureRegularStandard()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!SkirmishAcceptanceCensusCapture.TryCaptureRegularStandard(
                    authored,
                    matrix,
                    out SkirmishAcceptanceCensus census,
                    out List<SkirmishCompileReason> reasons))
                throw new InvalidOperationException(reasons == null || reasons.Count == 0 ? "census failed" : reasons[0].ToString());

            if (census.PlayableMarked)
                throw new InvalidOperationException("Census capture must not mark Playable.");
            if (census.CatalogId != SkirmishAcceptanceCensusCapture.CatalogId ||
                census.DefinitionId != SkirmishAcceptanceCensusCapture.DefinitionId)
                throw new InvalidOperationException("Census catalog identity drifted.");
            if (census.Size != "Standard" || census.Difficulty != "Regular" ||
                census.Seed != SkirmishAcceptanceCensusCapture.FirstVisitSeed)
                throw new InvalidOperationException("Census must record Regular Standard seed 104731.");
            if (!census.MeasuredLayoutBound)
                throw new InvalidOperationException("Regular Standard census must bind the measured layout.");
            if (!authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row) ||
                row.Status == SkirmishPublicationStatus.Playable)
                throw new InvalidOperationException("S002 publication must stay InProgress.");

            return SkirmishAcceptanceCensusCapture.FormatLog(census);
        }
    }
}
#endif
