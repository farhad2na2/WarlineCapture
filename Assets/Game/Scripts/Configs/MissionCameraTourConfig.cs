using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionCameraTourConfig
    {
        public int StartHoldMilliseconds, PostHoldMilliseconds, ApproachHoldMilliseconds, ReturnHoldMilliseconds;
        public float SmoothTimeSeconds;
        public Vector4 PostPerspective, ApproachPerspective;
        public bool IsValid=>StartHoldMilliseconds>=250 && PostHoldMilliseconds>=0 && ApproachHoldMilliseconds>=0 &&
            ReturnHoldMilliseconds>=100 && float.IsFinite(SmoothTimeSeconds) && SmoothTimeSeconds is >=.1f and <=3f &&
            ValidPose(PostPerspective) && ValidPose(ApproachPerspective);
        private static bool ValidPose(Vector4 p)=>float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) && float.IsFinite(p.w) &&
            p.x is >=10 and <=200 && p.y is >=30 and <=85 && p.z is >=-180 and <=360 && p.w is >=25 and <=80;
    }
}
