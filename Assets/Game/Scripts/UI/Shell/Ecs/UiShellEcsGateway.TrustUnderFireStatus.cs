using Game.Configs;
using Game.Runtime;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static (int Stage, int North, int South, int Verification, int Stable) cachedTrustUnderFireStatusStamp;
        private static (int Stage, int North, int South, int Verification, int Stable) ReadTrustUnderFireStatusStamp()
        { if (!TryTrustUnderFire(out _, out _, out var m)) return (-1, 0, 0, 0, 0); return (CampaignMissionTrustUnderFireRuleUtility.Stage(in m), m.NorthHoldMilliseconds / 1000, m.SouthHoldMilliseconds / 1000, m.VerificationMilliseconds / 1000, m.StableMilliseconds / 1000); }
        private static string AppendTrustUnderFireStatus(string text)
        {
            var s = ReadTrustUnderFireStatusStamp(); if (s.Stage < 0) return text;
            if (s.Stage == 2) text += "\n" + GameText.Format("mission.trust_under_fire.hud.north_hold", "North shelter: {0}/6 s", s.North);
            if (s.Stage == 4) text += "\n" + GameText.Format("mission.trust_under_fire.hud.south_hold", "South shelter: {0}/6 s", s.South);
            if (s.Stage == 7) text += "\n" + GameText.Format("mission.trust_under_fire.hud.verification", "Verify broadcast: {0}/6 s", s.Verification);
            if (s.Stage == 8) text += "\n" + GameText.Format("mission.trust_under_fire.hud.stable", "Protected routes: {0}/6 s", s.Stable);
            return text;
        }
    }
}
