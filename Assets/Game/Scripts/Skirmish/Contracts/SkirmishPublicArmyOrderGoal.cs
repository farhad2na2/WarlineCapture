using System;
namespace Game.Skirmish.Contracts
{
    public enum SkirmishPublicArmyOrderGoalKind : byte { None, Approach, CityFlank, CityFinal, VisibleObjective, HomeDefense }
    // The requested public goal is distinct from the actual accepted click/cell.
    public readonly struct SkirmishPublicArmyOrderGoal : IEquatable<SkirmishPublicArmyOrderGoal>
    {
        readonly ulong session0,session1,session2,session3,session4,session5,session6,session7;
        readonly byte sessionLength;
        public readonly ulong WorldSequenceNumber;
        public string SessionId
        {
            get {var bytes=new byte[sessionLength];for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)(SessionPart(i/8)>>(8*(i%8)));return System.Text.Encoding.UTF8.GetString(bytes);}
        }
        ulong SessionPart(int part)=>part==0?session0:part==1?session1:part==2?session2:part==3?session3:part==4?session4:part==5?session5:part==6?session6:session7;
        public readonly SkirmishPublicArmyOrderGoalKind Kind;
        public readonly int PublicObjectiveIdentity;
        public readonly float ObjectiveX, ObjectiveZ;
        public bool Known => sessionLength!=0 && WorldSequenceNumber!=0 && Kind != SkirmishPublicArmyOrderGoalKind.None && PublicObjectiveIdentity!=0;
        public SkirmishPublicArmyOrderGoal(string sessionId, SkirmishPublicArmyOrderGoalKind kind, int publicObjectiveIdentity, float x, float z, ulong worldSequenceNumber)
        {
            if (string.IsNullOrEmpty(sessionId) || sessionId.Length > 128 || worldSequenceNumber==0 || publicObjectiveIdentity==0 || kind <= SkirmishPublicArmyOrderGoalKind.None || kind > SkirmishPublicArmyOrderGoalKind.HomeDefense || float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(z) || float.IsInfinity(z)) throw new ArgumentException("Current public army goal required");
            var bytes=new System.Text.UTF8Encoding(false,true).GetBytes(sessionId);
            if(bytes.Length>64)throw new ArgumentException("Native fixed session identity exceeds public goal capacity");
            var parts=new ulong[8];for(int i=0;i<bytes.Length;i++)parts[i/8]|=(ulong)bytes[i]<<(8*(i%8));
            sessionLength=(byte)bytes.Length;session0=parts[0];session1=parts[1];session2=parts[2];session3=parts[3];session4=parts[4];session5=parts[5];session6=parts[6];session7=parts[7];
            WorldSequenceNumber=worldSequenceNumber;Kind=kind;PublicObjectiveIdentity=publicObjectiveIdentity;ObjectiveX=x;ObjectiveZ=z;
        }
        public bool SamePublicObjective(SkirmishPublicArmyOrderGoal other) => Known && other.Known && WorldSequenceNumber==other.WorldSequenceNumber && sessionLength==other.sessionLength && session0==other.session0 && session1==other.session1 && session2==other.session2 && session3==other.session3 && session4==other.session4 && session5==other.session5 && session6==other.session6 && session7==other.session7 && PublicObjectiveIdentity==other.PublicObjectiveIdentity && ObjectiveX.Equals(other.ObjectiveX) && ObjectiveZ.Equals(other.ObjectiveZ);
        public bool Equals(SkirmishPublicArmyOrderGoal other)=>Kind==other.Kind&&SamePublicObjective(other);
        public override bool Equals(object value) => value is SkirmishPublicArmyOrderGoal goal && Equals(goal);
        public override int GetHashCode() => (SessionId ?? string.Empty).GetHashCode() ^ (int)Kind ^ PublicObjectiveIdentity ^ WorldSequenceNumber.GetHashCode() ^ ObjectiveX.GetHashCode() ^ ObjectiveZ.GetHashCode();
    }
}
