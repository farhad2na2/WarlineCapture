using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>Independent minimap dock and mobile-sized mission actions.</summary>
    public sealed class MissionHudTouchLayoutView : MonoBehaviour
    {
        private const float DockGap = 8f;
        [SerializeField] private RectTransform minimap;
        [SerializeField] private RectTransform[] primaryActions;
        private MatchOverlayCommandControlsView commands;
        private BuildPlacementConfirmationBarView placement;
        private MatchHudSelectionPanelView selection;
        private readonly Vector3[] corners=new Vector3[4];
        [SerializeField] private RectTransform dockBorder;
        public RectTransform Minimap => minimap;
        private float commandDockBottom = 175;

        public float AssistantHeight(RectTransform rail)
        {
            var header = (RectTransform)transform;
            var limit = header.TransformPoint(new Vector3(header.rect.xMax,
                header.rect.yMin + commandDockBottom));
            return -rail.InverseTransformPoint(limit).y - 12;
        }

        public void Apply(bool missionActive)
        {
            if(primaryActions!=null)
                foreach(var action in primaryActions)
                    if(action!=null) action.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,72);
            RefreshLayout();
        }

        private void OnEnable() => Canvas.willRenderCanvases += RefreshLayout;
        private void OnDisable() => Canvas.willRenderCanvases -= RefreshLayout;

        public void RefreshLayout()
        {
            if(minimap==null) return;
            var header=(RectTransform)transform;
            if(minimap.parent!=header) minimap.SetParent(header,false);
            if(commands==null) commands=transform.root.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            if(placement==null) placement=transform.root.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
            if(selection==null) selection=transform.root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            float bottom=175;
            if(commands!=null && commands.BuildButton!=null)
                bottom=TopInHeader((RectTransform)commands.BuildButton.transform)-header.rect.yMin+DockGap;
            if(placement!=null && placement.HasPendingPlacement && placement.Root.gameObject.activeInHierarchy)
                bottom=Mathf.Max(bottom,TopInHeader(placement.Root)-header.rect.yMin+DockGap);
            commandDockBottom=bottom;
            bool expanded = UiShellRuntimeGateway.TryReadSkirmish(out var mission) && mission.Expanded;
            // Keep the established left dock. Expanded skirmish raises it above
            // the slim squad pager, leaving both controls readable and tappable.
            bottom=Mathf.Max(bottom,expanded ? 300 : 230);
            minimap.anchorMin=minimap.anchorMax=Vector2.zero;
            minimap.pivot=Vector2.zero;
            minimap.anchoredPosition=new Vector2(15,bottom);
            minimap.sizeDelta=new Vector2(320,220);
            if(selection!=null) selection.FitAboveMinimap(minimap);
            if(dockBorder==null)
            {
                if(dockBorder==null)
                {
                    dockBorder=(RectTransform)new GameObject("DockBorder",typeof(RectTransform),typeof(V3GradientGraphic)).transform;
                    dockBorder.SetParent(minimap,false);
                    var graphic=dockBorder.GetComponent<V3GradientGraphic>();
                    graphic.Configure(Color.clear,Color.clear,new Color32(0,198,235,255),3);graphic.raycastTarget=false;
                }
                dockBorder.anchorMin=Vector2.zero;dockBorder.anchorMax=Vector2.one;
                dockBorder.offsetMin=dockBorder.offsetMax=Vector2.zero;dockBorder.SetAsLastSibling();
            }
        }
        private float TopInHeader(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            float top=float.NegativeInfinity;
            for(int i=0;i<4;i++) top=Mathf.Max(top,transform.InverseTransformPoint(corners[i]).y);
            return top;
        }
    }
}
