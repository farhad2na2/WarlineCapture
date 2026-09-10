using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>Reserves room inside ARIA for full-size mission actions without covering the battlefield.</summary>
    public sealed class MissionHudTouchLayoutView : MonoBehaviour
    {
        [SerializeField] private RectTransform minimap;
        [SerializeField] private RectTransform[] primaryActions;
        private Vector2 mapPosition,mapSize;
        private Vector2[] actionSizes;
        private bool captured,active;

        public void Apply(bool missionActive)
        {
            if(minimap==null) return;
            if(!captured)
            {
                mapPosition=minimap.anchoredPosition;mapSize=minimap.sizeDelta;
                actionSizes=new Vector2[primaryActions.Length];
                for(int i=0;i<primaryActions.Length;i++) actionSizes[i]=primaryActions[i].sizeDelta;
                captured=true;
            }
            if(active==missionActive) return;
            active=missionActive;
            minimap.anchoredPosition=active ? new Vector2(mapPosition.x,-472) : mapPosition;
            minimap.sizeDelta=active ? new Vector2(mapSize.x,203) : mapSize;
            for(int i=0;i<primaryActions.Length;i++)
                primaryActions[i].sizeDelta=active ? new Vector2(actionSizes[i].x,72) : actionSizes[i];
        }
    }
}
