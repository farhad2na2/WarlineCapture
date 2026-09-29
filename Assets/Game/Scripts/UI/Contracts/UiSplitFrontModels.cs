namespace Game.UI.Contracts
{
    public readonly struct UiSplitFrontLauncherModel
    {
        public readonly bool Active;
        public readonly bool ShowRange,ShowImpact;
        public readonly UnityEngine.Vector3 Launcher,Impact,ProtectedCenter;
        public readonly float MinimumRange,MaximumRange,ImpactRadius,ProtectedRadius;
        public UiSplitFrontLauncherModel(bool active,
            bool range=false,bool impact=false,UnityEngine.Vector3 launcher=default,UnityEngine.Vector3 target=default,UnityEngine.Vector3 protectedCenter=default,
            float minimum=0,float maximum=0,float radius=0,float protectedRadius=0)
        {Active=active;
            ShowRange=range;ShowImpact=impact;Launcher=launcher;Impact=target;ProtectedCenter=protectedCenter;
            MinimumRange=minimum;MaximumRange=maximum;ImpactRadius=radius;ProtectedRadius=protectedRadius;}
    }
    public interface IUiSplitFrontGateway
    {
        bool TryReadSplitFrontLauncher(out UiSplitFrontLauncherModel model);
    }
}
