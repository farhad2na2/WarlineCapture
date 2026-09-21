#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishPublicationValidatorMenu
    {
        [MenuItem("Tools/Warline/Skirmish/Validate S002 Publication")]
        public static void ValidateMenu() => Debug.Log(ValidateS002());

        public static string ValidateS002()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row))
                throw new InvalidOperationException("S002 publication row is missing.");

            var reasons = new List<SkirmishCompileReason>();
            if (!SkirmishMapLayoutValidation.TryValidateDesertBaseAssault(authored.LayoutDbBa, reasons))
                throw new InvalidOperationException(reasons.Count == 0 ? "layout invalid" : reasons[0].ToString());

            var evidence = new SkirmishPublicationEvidence
            {
                CatalogId = "S002",
                DefinitionId = "skirmish.s002",
                AssetExists = true,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            if (!Game.Configs.SkirmishPublicationValidator.TryEvaluate(
                    row,
                    authored.DefinitionS002,
                    null,
                    in evidence,
                    out SkirmishPublicationStatus status,
                    out List<SkirmishCompileReason> publishReasons))
                throw new InvalidOperationException(publishReasons.Count == 0 ? "publication invalid" : publishReasons[0].ToString());

            if (status == SkirmishPublicationStatus.Playable)
                throw new InvalidOperationException("S002 must stay InProgress without complete evidence.");
            return "[SkirmishPublicationValidator] result=Passed catalog=S002 status=" + status;
        }
    }
}
#endif
