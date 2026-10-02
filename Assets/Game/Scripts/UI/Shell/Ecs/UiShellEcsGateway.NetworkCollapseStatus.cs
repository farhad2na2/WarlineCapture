using Game.Configs;
using Game.Runtime;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static (int Stage,int Recon,int Audit,int Extraction) cachedNetworkCollapseStatusStamp;
        private static (int Stage,int Recon,int Audit,int Extraction) ReadNetworkCollapseStatusStamp(){if(!TryNetworkCollapse(out _,out _,out var m))return(-1,0,0,0);return(CampaignMissionNetworkCollapseRuleUtility.Stage(in m),m.VerificationMilliseconds/1000,m.RecoveryMilliseconds/1000,m.ExtractionMilliseconds/1000);}
        private static string AppendNetworkCollapseStatus(string text)
        {var s=ReadNetworkCollapseStatusStamp();if(s.Stage<0)return text;if(s.Stage is 1 or 3 or 5)text+="\n"+GameText.Format("mission.network_collapse.hud.recon_hold","Verify node {0}: {1}/6 s",(s.Stage+1)/2,s.Recon);if(s.Stage==7)text+="\n"+GameText.Format("mission.network_collapse.hud.audit_hold","Recover audit: {0}/6 s",s.Audit);if(s.Stage==8)text+="\n"+GameText.Format("mission.network_collapse.hud.extraction_hold","Evidence custody: {0}/6 s",s.Extraction);return text;}
    }
}
