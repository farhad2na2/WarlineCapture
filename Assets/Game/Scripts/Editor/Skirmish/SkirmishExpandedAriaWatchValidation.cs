#if UNITY_EDITOR
using System;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishExpandedAriaWatchValidation
    {
        public const string SelectedPrefix = "[SkirmishExpandedAriaWatch] selected ";

        [MenuItem("Tools/Warline/Skirmish/Log S002 Regular Standard ARIA Watch Payload")]
        public static void LogS002RegularStandardMenu()
        {
            if (!SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(
                    SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                    GameLocalization.EnglishLocaleCode,
                    out SkirmishAriaAcceptancePayload payload,
                    out string error))
                throw new InvalidOperationException(error);
            Debug.Log(AcceptExpandedPayload(in payload));
        }

        public static string AcceptExpandedPayload(in SkirmishAriaAcceptancePayload payload)
        {
            if (!payload.TryValidate(out string error))
                throw new ArgumentException(error, nameof(payload));

            string line = SelectedPrefix + payload.FormatSelectedConfiguration();
            Debug.Log(line);
            return line;
        }

        public static string AcceptExpandedPayload(
            string definitionId,
            SkirmishSizeId size,
            SkirmishDifficultyId difficulty,
            int seed,
            string locale)
        {
            if (!SkirmishAriaAcceptancePayload.TryCreate(
                    SkirmishAcceptanceCensusCapture.CatalogId,
                    definitionId,
                    SkirmishAcceptanceCensusCapture.DefinitionVersion,
                    size,
                    difficulty,
                    seed,
                    locale,
                    out SkirmishAriaAcceptancePayload payload,
                    out string error))
                throw new ArgumentException(error);

            return AcceptExpandedPayload(in payload);
        }
    }
}
#endif
