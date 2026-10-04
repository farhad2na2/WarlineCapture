using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game.UI.Runtime
{
    public interface IUiBackOverlayParticipant { bool IsOpen {get;} bool HandleBack(); }
    public static class UiBackOverlayRegistry
    {
        private static readonly List<IUiBackOverlayParticipant> overlays=new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>overlays.Clear();
        public static void Register(IUiBackOverlayParticipant overlay){if(!overlays.Contains(overlay))overlays.Add(overlay);}
        public static void Unregister(IUiBackOverlayParticipant overlay)=>overlays.Remove(overlay);
        public static bool HasOpenOverlay=>overlays.Any(v=>v.IsOpen);
        public static bool HandleBack(){for(int i=overlays.Count-1;i>=0;i--)if(overlays[i].IsOpen)return overlays[i].HandleBack();return false;}
    }
}
