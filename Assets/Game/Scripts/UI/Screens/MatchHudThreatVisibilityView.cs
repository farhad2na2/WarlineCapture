using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>Owns alert visibility across refreshes, expiry, and overlapping tactical surfaces.</summary>
    public sealed class MatchHudThreatVisibilityView : MonoBehaviour
    {
        private readonly HashSet<Object> suppressors = new();
        private bool requested;
        public void SetRequested(bool visible) { requested=visible; Apply(); }
        public void SetSuppressed(Object owner,bool suppressed)
        {
            if(owner==null) return;
            if(suppressed) suppressors.Add(owner); else suppressors.Remove(owner);
            Apply();
        }
        private void Apply()
        {
            bool visible=requested && suppressors.Count==0;
            if(gameObject.activeSelf!=visible) gameObject.SetActive(visible);
        }
    }
}
