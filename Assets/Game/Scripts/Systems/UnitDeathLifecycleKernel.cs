using System;
namespace Game.Runtime
{
    public struct InfantryDeathState { public float TimeRemaining; public byte PoseFrozen; }
    public enum DeathFinalizationAction : byte { Retain, Freeze, Remove }
    [Flags] public enum DeathStripFlags : ushort
    {
        None=0, Selected=1, ManualMove=2, AutoWander=4, Engage=8, RecentAttacker=16,
        RecentDamageVisibility=32, PathFollow=64, PathRange=128, PathRequest=256,
        ResetAttackAnimation=512, ResetMoveVisual=1024, RequestLightCleanup=2048
    }
    // Hosts preserve begin-candidate and finalize-query order, including same-update
    // animation ticking. Camera projection, GPU clip traversal and destruction hooks
    // remain explicit host facts/actions; this kernel never enqueues a respawn.
    public static class UnitDeathLifecycleKernel
    {
        public const float MinimumPlaybackSeconds=0.01f;
        public static float ClampPlaybackDuration(float duration)=>Max(MinimumPlaybackSeconds,duration);
        public static bool TryBegin(int health,float configuredDuration,out float duration)
        { duration=0; if(health>0)return false; duration=ClampPlaybackDuration(configuredDuration);return true; }
        public static bool Tick(int health,float deltaTime,ref InfantryDeathState state)
        {
            if(health>0)return false;
            if(state.PoseFrozen!=0)return true;
            state.TimeRemaining=Max(0f,state.TimeRemaining-deltaTime);
            return state.TimeRemaining<=0f;
        }
        public static DeathFinalizationAction DecideFinalize(byte poseFrozen,bool hasValidCamera,bool insideViewport)=>
            poseFrozen==0?DeathFinalizationAction.Freeze:!hasValidCamera||insideViewport?DeathFinalizationAction.Retain:DeathFinalizationAction.Remove;
        public static void Freeze(ref InfantryDeathState state){state.TimeRemaining=0f;state.PoseFrozen=1;}
        public static DeathStripFlags PlanStrip(DeathStripFlags present,bool hasLightSetup,bool hasLightCleanup)=>
            (present & (DeathStripFlags.Selected|DeathStripFlags.ManualMove|DeathStripFlags.AutoWander|DeathStripFlags.Engage|DeathStripFlags.RecentAttacker|
                DeathStripFlags.RecentDamageVisibility|DeathStripFlags.PathFollow|DeathStripFlags.PathRange|DeathStripFlags.PathRequest|DeathStripFlags.ResetAttackAnimation|DeathStripFlags.ResetMoveVisual)) |
            (hasLightSetup&&!hasLightCleanup?DeathStripFlags.RequestLightCleanup:DeathStripFlags.None);
        public static float ResolveSinglePlaybackDuration(int frames,byte fps,byte speed,float fallbackDuration)
        {
            float safeFallback=ClampPlaybackDuration(fallbackDuration);
            if(frames<=1||fps==0||speed==0)return safeFallback;
            float frameRate=fps*speed;
            float finalFrameTime=(frames-1)/frameRate;
            return !float.IsNaN(finalFrameTime)&&!float.IsInfinity(finalFrameTime)&&finalFrameTime>0f?ClampPlaybackDuration(finalFrameTime):safeFallback;
        }
        public static float AccumulatePlaybackDuration(float duration,float clipDuration)=>Min(duration,clipDuration);
        private static float Max(float a,float b)=>float.IsNaN(b)||a>b?a:b;
        private static float Min(float a,float b)=>float.IsNaN(b)||a<b?a:b;
    }
}
