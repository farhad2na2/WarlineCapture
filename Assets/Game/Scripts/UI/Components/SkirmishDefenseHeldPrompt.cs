namespace Game.UI.Runtime
{
    /// <summary>
    /// "Base held" guidance: after a base alert clears with the main base alive, offer the existing
    /// reported-position action with a short choice. Shown for <see cref="Seconds"/> or until tapped,
    /// a new alert or the end of the match. Presentation only.
    /// </summary>
    public struct SkirmishDefenseHeldPrompt
    {
        public const float Seconds = 25f;
        private bool alertSeen, shown, dismissed;
        private float shownAt;

        public bool Shown => shown;

        /// <param name="alertPresented">The base alert row is on screen this frame.</param>
        /// <param name="baseAlive">The player's main base still stands.</param>
        /// <param name="eligible">Base Assault HUD, enemy base alive and not in sight, match running.</param>
        public bool Observe(bool alertPresented, bool baseAlive, bool eligible, float now)
        {
            if (alertPresented)
            {
                alertSeen = true; shown = false; dismissed = false;
                return false;
            }
            if (alertSeen)
            {
                alertSeen = false;
                if (baseAlive && eligible) { shown = true; shownAt = now; }
            }
            if (shown && (dismissed || !baseAlive || !eligible || now - shownAt >= Seconds)) shown = false;
            return shown;
        }

        public void Dismiss() { dismissed = true; shown = false; }

        public void Reset() => this = default;
    }
}
