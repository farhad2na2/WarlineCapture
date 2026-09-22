using System;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Objective")]
    public sealed class SkirmishObjectiveConfig : ScriptableObject
    {
        [SerializeField] private string objectiveId = "BA";
        [SerializeField] private SkirmishObjectiveKind kind = SkirmishObjectiveKind.BaseAssault;
        [SerializeField] private int standardDeadlineSeconds = 1080;
        [SerializeField] private int warDeadlineSeconds = 1500;
        [SerializeField] private int largeWarDeadlineSeconds = 1800;
        [SerializeField] private string playerBaseRoleId = SkirmishObjectiveIds.BasePlayer;
        [SerializeField] private string enemyBaseRoleId = SkirmishObjectiveIds.BaseEnemy;
        [SerializeField] private int contentVersion = 1;

        public string ObjectiveId => objectiveId;
        public SkirmishObjectiveKind Kind => kind;
        public int StandardDeadlineSeconds => standardDeadlineSeconds;
        public int WarDeadlineSeconds => warDeadlineSeconds;
        public int LargeWarDeadlineSeconds => largeWarDeadlineSeconds;
        public string PlayerBaseRoleId => playerBaseRoleId;
        public string EnemyBaseRoleId => enemyBaseRoleId;
        public int ContentVersion => contentVersion;

        public void ConfigureBaseAssault()
        {
            objectiveId = "BA";
            kind = SkirmishObjectiveKind.BaseAssault;
            standardDeadlineSeconds = 1080;
            warDeadlineSeconds = 1500;
            largeWarDeadlineSeconds = 1800;
            playerBaseRoleId = SkirmishObjectiveIds.BasePlayer;
            enemyBaseRoleId = SkirmishObjectiveIds.BaseEnemy;
            contentVersion = 1;
        }

        public int DeadlineSeconds(SkirmishSizeId size)
        {
            switch (size)
            {
                case SkirmishSizeId.War: return warDeadlineSeconds;
                case SkirmishSizeId.LargeWar: return largeWarDeadlineSeconds;
                default: return standardDeadlineSeconds;
            }
        }
    }
}
