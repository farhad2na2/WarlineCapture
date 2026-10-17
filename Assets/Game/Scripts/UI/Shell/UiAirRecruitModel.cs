using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public interface IUiAirRecruitGateway {bool TryReadAirRecruit(out AriaAirRecruitObservation model);}
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadAirRecruit(out AriaAirRecruitObservation model)
        {model=default;return current is IUiAirRecruitGateway gateway&&gateway.TryReadAirRecruit(out model);}
    }
}
