using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiNetworkCollapseModel
    {
        public readonly int Stage,NodesVerified,NodesDisabled,HostilesDefeated,ReconSeconds,AuditSeconds,ExtractionSeconds,RemainingSeconds;
        public readonly bool AuditRecovered,EngineerAboard,Extracted;
        public readonly Vector3 ReconOne,ReconTwo,ReconThree,AuditGate,Extraction;
        public UiNetworkCollapseModel(int stage,int verified,int disabled,int defeated,int recon,int audit,int extraction,int remaining,bool recovered,bool aboard,bool extracted,Vector3 one,Vector3 two,Vector3 three,Vector3 gate,Vector3 exit){Stage=stage;NodesVerified=verified;NodesDisabled=disabled;HostilesDefeated=defeated;ReconSeconds=recon;AuditSeconds=audit;ExtractionSeconds=extraction;RemainingSeconds=remaining;AuditRecovered=recovered;EngineerAboard=aboard;Extracted=extracted;ReconOne=one;ReconTwo=two;ReconThree=three;AuditGate=gate;Extraction=exit;}
    }
    public readonly struct UiNetworkCollapseResultModel
    {
        public readonly bool NodesSecured,AuditRecovered,Extracted;
        public readonly int CivilianLosses;
        public UiNetworkCollapseResultModel(bool nodes,bool audit,bool extracted,int losses){NodesSecured=nodes;AuditRecovered=audit;Extracted=extracted;CivilianLosses=losses;}
    }
    public interface IUiNetworkCollapseResultGateway { bool TryReadNetworkCollapseResult(out UiNetworkCollapseResultModel model); }
    public interface IUiNetworkCollapseGateway { bool TryReadNetworkCollapse(out UiNetworkCollapseModel model); }
}
