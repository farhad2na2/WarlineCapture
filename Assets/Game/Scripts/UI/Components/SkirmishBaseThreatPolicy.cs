using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Tracks "your main base is under fire" from the player's own, always-public base
    /// health. The first drop after a quiet period is a new attack; the warning stays
    /// up while drops keep arriving and clears <see cref="QuietSeconds"/> after the last.
    /// Pure and time-injected so it is EditMode testable.
    /// </summary>
    public struct SkirmishBaseFireTracker
    {
        public const float QuietSeconds = 8f;

        private bool known;
        private int lastHealth;
        private float lastDropAt;
        private bool underFire;

        public bool UnderFire => underFire;
        public float LastDropAt => lastDropAt;

        /// <summary>Feed the displayed own-base health; returns true on the first hit of a new attack.</summary>
        public bool Observe(int health, float now)
        {
            if (underFire && now - lastDropAt > QuietSeconds)
                underFire = false;
            if (!known)
            {
                known = true;
                lastHealth = health;
                return false;
            }
            bool dropped = health < lastHealth;
            lastHealth = health;
            if (!dropped)
                return false;
            bool first = !underFire;
            underFire = true;
            lastDropAt = now;
            return first;
        }

        public void Reset() => this = default;
    }

    public static class SkirmishBaseThreatPolicy
    {
        /// <summary>
        /// Index of the group whose public center is nearest the base, or -1.
        /// Callers pass only the player's own groups.
        /// </summary>
        public static int NearestGroup(Vector2 basePoint, Vector2[] centers)
        {
            if (centers == null) return -1;
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < centers.Length; i++)
            {
                float distance = (centers[i] - basePoint).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = i; }
            }
            return best;
        }

        /// <summary>Squads within this distance of the base centre count as its home guard.</summary>
        public const float HomeZoneMinimumMetres = 120f;

        public static float HomeZoneRadius(float perimeterRadius) => Mathf.Max(HomeZoneMinimumMetres, perimeterRadius * 2f);

        /// <summary>
        /// The defender the HUD names when the base is threatened. Inside the home zone an
        /// anti-armour squad is preferred (it can hurt the S002 tank), then the nearest squad;
        /// both are labelled Home Guard. With nobody home, the nearest squad anywhere.
        /// Inputs are the player's own squads only.
        /// </summary>
        public static int PickDefender(Vector2 basePoint, Vector2[] centers, bool[] antiArmour, float homeZoneRadius, out bool homeGuard)
        {
            homeGuard = false;
            if (centers == null || centers.Length == 0) return -1;
            float zone = homeZoneRadius * homeZoneRadius;
            int bestHome = -1, bestArmour = -1, bestAny = -1;
            float home = float.MaxValue, armour = float.MaxValue, any = float.MaxValue;
            for (int i = 0; i < centers.Length; i++)
            {
                float distance = (centers[i] - basePoint).sqrMagnitude;
                if (distance < any) { any = distance; bestAny = i; }
                if (distance > zone) continue;
                if (distance < home) { home = distance; bestHome = i; }
                bool armourCapable = antiArmour != null && i < antiArmour.Length && antiArmour[i];
                if (armourCapable && distance < armour) { armour = distance; bestArmour = i; }
            }
            if (bestArmour >= 0) { homeGuard = true; return bestArmour; }
            if (bestHome >= 0) { homeGuard = true; return bestHome; }
            return bestAny;
        }

        /// <summary>True when a projected point is in front of the camera and inside the screen rect.</summary>
        public static bool OnScreen(Vector3 screen, Rect area) =>
            screen.z > 0 && area.Contains(new Vector2(screen.x, screen.y));

        /// <summary>
        /// Screen-space direction (degrees, 0 = right, counter-clockwise) from the screen
        /// centre toward a projected point. Points behind the camera are mirrored so the
        /// arrow still points the way the player has to turn.
        /// </summary>
        public static float ArrowDegrees(Vector3 screen, Vector2 centre)
        {
            Vector2 delta = new Vector2(screen.x, screen.y) - centre;
            if (screen.z < 0) delta = -delta;
            if (delta.sqrMagnitude < 1e-4f) return -90f;
            return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }
    }
}
