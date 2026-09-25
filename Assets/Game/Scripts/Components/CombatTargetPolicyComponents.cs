using System;
using Unity.Entities;

namespace Game.Components
{
    [Flags]
    public enum CombatTargetDomain : byte
    {
        None = 0, Infantry = 1, Ground = 2, Structure = 4, Air = 8,
        All = Infantry | Ground | Structure | Air
    }

    /// <summary>Optional mode-authored targeting policy consumed by shared combat.</summary>
    public struct CombatTargetPolicy : IComponentData
    {
        public CombatTargetDomain AllowedTargets;
        public CombatTargetDomain Domain;
        public byte Visible;
    }
}
