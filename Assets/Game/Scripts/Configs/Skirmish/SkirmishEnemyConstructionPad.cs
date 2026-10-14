using System;
namespace Game.Configs
{
    public readonly struct SkirmishEnemyConstructionPad
    {
        public string Id { get; }
        public SkirmishLegalPadKind Kind { get; }
        public byte Faction { get; }
        public float X { get; }
        public float Z { get; }
        // Half-extent vectors retain the existing authored frame, including rotated map binding.
        public float AX { get; } public float AZ { get; }
        public float BX { get; } public float BZ { get; }
        public SkirmishEnemyConstructionPad(string id, SkirmishLegalPadKind kind, byte faction,
            float x,float z,float ax,float az,float bx,float bz)
        { Id=id;Kind=kind;Faction=faction;X=x;Z=z;AX=ax;AZ=az;BX=bx;BZ=bz; }
        public bool Contains(float x,float z)
        {
            float det=AX*BZ-AZ*BX;
            if(!float.IsFinite(det)||Math.Abs(det)<.0001f)return false;
            float dx=x-X,dz=z-Z;
            float a=(dx*BZ-dz*BX)/det,b=(AX*dz-AZ*dx)/det;
            return float.IsFinite(a)&&float.IsFinite(b)&&Math.Abs(a)<=1.00001f&&Math.Abs(b)<=1.00001f;
        }
    }
}
