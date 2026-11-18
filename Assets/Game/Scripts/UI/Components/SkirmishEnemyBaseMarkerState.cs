using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>What the player is entitled to know about the enemy main base marker.</summary>
    public enum SkirmishEnemyBaseMarkerKnowledge : byte
    {
        /// <summary>The base is in the player's vision now.</summary>
        InSight,
        /// <summary>The base was seen earlier; only that sighting is shown.</summary>
        LastSeen,
        /// <summary>Never seen in this match: only the briefed (designated) position is shown.</summary>
        Reported
    }

    /// <summary>
    /// Pure rules for the enemy-base marker. Inputs are the fog-respecting HUD readout
    /// (Hidden 0 / LastSeen 1 / Visible 2) and public positions only; nothing here reads
    /// hidden health, units or contacts.
    /// </summary>
    public static class SkirmishEnemyBaseMarkerState
    {
        public const byte ReadoutHidden = 0;
        public const byte ReadoutLastSeen = 1;
        public const byte ReadoutVisible = 2;

        public static SkirmishEnemyBaseMarkerKnowledge Resolve(bool readoutKnown, byte readout, bool targetVisible)
        {
            if (!readoutKnown)
                return targetVisible ? SkirmishEnemyBaseMarkerKnowledge.InSight : SkirmishEnemyBaseMarkerKnowledge.Reported;
            return readout == ReadoutVisible ? SkirmishEnemyBaseMarkerKnowledge.InSight
                : readout == ReadoutLastSeen ? SkirmishEnemyBaseMarkerKnowledge.LastSeen
                : SkirmishEnemyBaseMarkerKnowledge.Reported;
        }

        /// <summary>"0:42", "12:05". Negative or invalid ages read as 0:00.</summary>
        public static string FormatAge(float seconds)
        {
            int whole = float.IsNaN(seconds) || seconds <= 0f ? 0 : Mathf.FloorToInt(Mathf.Min(seconds, 359999f));
            return (whole / 60) + ":" + (whole % 60).ToString("00");
        }

        /// <summary>Ground distance between two public positions, rounded to 10 m.</summary>
        public static int RoundedGroundDistance(Vector3 from, Vector3 to)
        {
            float metres = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
            return Mathf.RoundToInt(metres / 10f) * 10;
        }
    }
}
