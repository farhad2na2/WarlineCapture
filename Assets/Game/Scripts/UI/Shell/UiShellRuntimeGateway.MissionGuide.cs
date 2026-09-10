using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        private static IUiMissionGuideContent missionGuideContent;
        public static void BindMissionGuideContent(IUiMissionGuideContent content) => missionGuideContent = content;
        public static IUiMissionGuideSession OpenMissionGuide(Object authoredSource) => missionGuideContent?.Open(authoredSource);
    }
}
