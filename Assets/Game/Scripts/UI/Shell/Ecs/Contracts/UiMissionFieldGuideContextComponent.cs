using Unity.Collections;
using Unity.Entities;
using Game.UI.Contracts;

namespace Game.UI.Shell.Contracts.Ecs
{
    public struct UiMissionFieldGuideContextComponent : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public UiShellPopupKind ReturnPopup;
        public byte HasReturnPopup;
    }
}
