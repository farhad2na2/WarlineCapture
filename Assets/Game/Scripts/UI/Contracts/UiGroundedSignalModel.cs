using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiGroundedSignalModel
    {
        public readonly int Stage, RecoverySeconds, ExitSeconds, RemainingSeconds;
        public readonly bool RelayDisabled, HardwareRecovered, Contested;
        public readonly Vector3 Apron, Hardware, Exit;
        public UiGroundedSignalModel(int stage,int recoverySeconds,int exitSeconds,int remainingSeconds,
            bool relayDisabled,bool hardwareRecovered,bool contested,Vector3 apron,Vector3 hardware,Vector3 exit)
        {Stage=stage;RecoverySeconds=recoverySeconds;ExitSeconds=exitSeconds;RemainingSeconds=remainingSeconds;
            RelayDisabled=relayDisabled;HardwareRecovered=hardwareRecovered;Contested=contested;Apron=apron;Hardware=hardware;Exit=exit;}
    }
    public interface IUiGroundedSignalGateway { bool TryReadGroundedSignal(out UiGroundedSignalModel model); }
}
