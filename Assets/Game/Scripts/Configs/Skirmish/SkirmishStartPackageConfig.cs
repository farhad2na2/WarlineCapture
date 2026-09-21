using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Start Package")]
    public sealed class SkirmishStartPackageConfig : ScriptableObject
    {
        [SerializeField] private string packageId = "E";
        [SerializeField] private SkirmishStartPackageId kind = SkirmishStartPackageId.EstablishedBase;
        [SerializeField] private SkirmishReadinessStage readiness = SkirmishReadinessStage.Established;
        [SerializeField] private int standardStructureCount = 10;
        [SerializeField] private int contentVersion = 1;

        public string PackageId => packageId;
        public SkirmishStartPackageId Kind => kind;
        public SkirmishReadinessStage Readiness => readiness;
        public int StandardStructureCount => standardStructureCount;
        public int ContentVersion => contentVersion;

        public void ConfigureEstablished()
        {
            packageId = "E";
            kind = SkirmishStartPackageId.EstablishedBase;
            readiness = SkirmishReadinessStage.Established;
            standardStructureCount = 10;
            contentVersion = 1;
        }

        public void ConfigureField()
        {
            packageId = "F";
            kind = SkirmishStartPackageId.FieldBase;
            readiness = SkirmishReadinessStage.Field;
            standardStructureCount = 7;
            contentVersion = 1;
        }
    }
}
