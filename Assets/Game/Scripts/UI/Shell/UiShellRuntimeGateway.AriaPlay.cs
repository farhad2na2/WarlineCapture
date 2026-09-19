using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static AriaPlayCapability ReadAriaPlayCapability() => current is IUiAriaPlayGateway gateway ? gateway.ReadAriaPlayCapability() : AriaPlayCapability.None;
        public static void PublishAriaObservation(AriaPlayObservation observation)
        { if (current is IUiAriaPlayGateway gateway) gateway.PublishAriaObservation(observation); }
        public static void PublishAriaSkirmishObservation(AriaSkirmishObservation observation)
        { if (current is IUiAriaPlayGateway gateway) gateway.PublishAriaSkirmishObservation(observation); }
        public static AriaSkirmishIntent ReadAriaSkirmishIntent() => current is IUiAriaPlayGateway gateway ? gateway.ReadAriaSkirmishIntent() : default;
        public static bool TryStartAriaPlay() => current is IUiAriaPlayGateway gateway && gateway.TryStartAriaPlay();
        public static void StopAriaPlay()
        { if (current is IUiAriaPlayGateway gateway) gateway.StopAriaPlay(); }
        public static AriaPlayModel ReadAriaPlay() => current is IUiAriaPlayGateway gateway ? gateway.ReadAriaPlay() : default;
    }
}
