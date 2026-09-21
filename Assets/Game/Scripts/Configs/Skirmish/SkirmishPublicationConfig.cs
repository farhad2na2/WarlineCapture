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
                    Notes = "Compiler + S002 overlays + BA facts + capacity/production gate. Starting set prefab-keyed (SpawnVisualPending=0). Not Playable/ARIA/War certified."
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
    }
}
