using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadMissionExtraction(out UiMissionExtractionModel model)
        {model=default; return current is IUiMissionExtractionGateway gateway && gateway.TryReadMissionExtraction(out model);}
        public static bool TryRequestExtractionAction(UiMissionExtractionAction action)=>current is IUiMissionExtractionGateway gateway && gateway.TryRequestExtractionAction(action);
        public static bool IsExtractionGuideContext()=>current is IUiMissionExtractionGateway gateway && gateway.IsExtractionGuideContext();
    }
}
