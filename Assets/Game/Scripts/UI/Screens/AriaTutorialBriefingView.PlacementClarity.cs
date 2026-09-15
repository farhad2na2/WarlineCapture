using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        internal void ApplyPlacementInstruction()
            => ApplyContextInstruction("ui.aria.placement.title","ui.aria.placement.body");
        internal void ApplyContextInstruction(string titleKey,string bodyKey)
        {
            var body=UiShellRuntimeGateway.Localization.Get(bodyKey);
            if(_currentInstructionBody==body) return;
            ApplyInstruction(UiShellRuntimeGateway.Localization.Get(titleKey),body,UiTutorialNarrationPhase.WorldTarget);
        }
    }
}
