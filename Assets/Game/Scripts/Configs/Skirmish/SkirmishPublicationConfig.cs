using System;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct SkirmishPublicationRowConfig
    {
        public string CatalogId;
        public string DefinitionId;
        public SkirmishPublicationStatus Status;
        public string EvidencePath;
        public string Notes;
    }

    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Publication Manifest")]
    public sealed class SkirmishPublicationConfig : ScriptableObject
    {
        public const string ResourceName = "SkirmishExpansionPublication";

        [SerializeField] private int manifestVersion = 1;
        [SerializeField] private SkirmishPublicationRowConfig[] rows = Array.Empty<SkirmishPublicationRowConfig>();

        public int ManifestVersion => manifestVersion;
        public SkirmishPublicationRowConfig[] Rows => rows;

        public void ConfigureGroundSlice()
        {
            manifestVersion = 1;
            rows = new[]
            {
                new SkirmishPublicationRowConfig
                {
                    CatalogId = "S002",
                    DefinitionId = "skirmish.s002",
                    Status = SkirmishPublicationStatus.InProgress,
                    EvidencePath = "Design/AgentReports/SkirmishExpansion/S002/",
                    Notes = "SK-12 library/HUD copy + publication validator scaffolding. Asset existence cannot set Playable. Not Playable/ARIA/War certified."
                },
                new SkirmishPublicationRowConfig
                {
                    CatalogId = "S003",
                    DefinitionId = "skirmish.s003",
                    Status = SkirmishPublicationStatus.InProgress,
                    EvidencePath = "Design/AgentReports/SkirmishExpansion/S003/",
                    Notes = "Compiler and Air Mobile Field definition slice. Asset existence cannot set Playable."
                },
                new SkirmishPublicationRowConfig
                {
                    CatalogId = "S004",
                    DefinitionId = "skirmish.s004",
                    Status = SkirmishPublicationStatus.InProgress,
                    EvidencePath = "Design/AgentReports/SkirmishExpansion/S004/",
                    Notes = "Compiler and Air Mobile Established definition slice. Asset existence cannot set Playable."
                }
            };
        }

        public bool TryGet(string catalogId, out SkirmishPublicationRowConfig row)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].CatalogId == catalogId)
                {
                    row = rows[i];
                    return true;
                }
            }

            row = default;
            return false;
        }

        public bool TrySetStatus(string catalogId, SkirmishPublicationStatus status, string notes)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].CatalogId != catalogId)
                    continue;
                SkirmishPublicationRowConfig row = rows[i];
                row.Status = status;
                if (!string.IsNullOrEmpty(notes))
                    row.Notes = notes;
                rows[i] = row;
                return true;
            }

            return false;
        }
    }
}
