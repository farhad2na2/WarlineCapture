using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Typed projection of the serialized S073 combat fields. This is source data
    // evidence only; it does not establish Unity startup or mission readiness.
    internal sealed class SourceS073LegacyCombatPreset
    {
        internal readonly float rifleRange;
        internal readonly float rifleCooldown;
        internal readonly float armoredCarRange;
        internal readonly float armoredCarCooldown;
        internal readonly float watchtowerRange;
        internal readonly float watchtowerCooldown;
        internal readonly float firstAttackSeconds;
        internal readonly float baseDefenseRadius;
        internal readonly int rifleDamage;
        internal readonly int armoredCarDamage;
        internal readonly int watchtowerDamage;
        internal readonly Vector3 playerReinforcementRallyOffset;

        internal SourceS073LegacyCombatPreset(float rifleRange, float rifleCooldown,
            float armoredCarRange, float armoredCarCooldown, float watchtowerRange,
            float watchtowerCooldown, float firstAttackSeconds, float baseDefenseRadius,
            int rifleDamage, int armoredCarDamage, int watchtowerDamage,
            Vector3 playerReinforcementRallyOffset)
        {
            this.rifleRange = rifleRange;
            this.rifleCooldown = rifleCooldown;
            this.armoredCarRange = armoredCarRange;
            this.armoredCarCooldown = armoredCarCooldown;
            this.watchtowerRange = watchtowerRange;
            this.watchtowerCooldown = watchtowerCooldown;
            this.firstAttackSeconds = firstAttackSeconds;
            this.baseDefenseRadius = baseDefenseRadius;
            this.rifleDamage = rifleDamage;
            this.armoredCarDamage = armoredCarDamage;
            this.watchtowerDamage = watchtowerDamage;
            this.playerReinforcementRallyOffset = playerReinforcementRallyOffset;
        }
    }

    internal static class SourceS073LegacyCombatPresetProvider
    {
        // Pinned to Logs/SkirmishProduction/s073-legacy-runtime-seams-20261101-r1/
        // combat-preset-source.json. The expected values are the descriptor, not
        // fallback values: a mismatch is an error and no preset is returned.
        const string AssetSha256 = "354b73f15a90a0653e263207789ba677c5251767b5a796b9c7f991af2a20f0f3";
        internal const string SourceAssetSha256 = AssetSha256;

        sealed class BoundPreset { internal SourceS073LegacyCombatPreset Value; }
        static readonly ConditionalWeakTable<EntityManager, BoundPreset> Bindings =
            new ConditionalWeakTable<EntityManager, BoundPreset>();

        internal static void Bind(EntityManager em, string assetPath)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            var preset = ReadAndValidatePinnedSource(assetPath);
            Bindings.Remove(em);
            Bindings.Add(em, new BoundPreset { Value = preset });
        }

        internal static SourceS073LegacyCombatPreset Resolve(EntityManager em)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            if (!Bindings.TryGetValue(em, out BoundPreset bound))
                throw new NotSupportedException("S073 legacy combat preset must be explicitly bound to this source EntityManager");
            return bound.Value;
        }

        internal static bool TryGetBound(EntityManager em, out SourceS073LegacyCombatPreset preset)
        {
            if (em != null && Bindings.TryGetValue(em, out BoundPreset bound)) { preset=bound.Value;return true; }
            preset=null;return false;
        }

        internal static void RestoreBinding(EntityManager em, SourceS073LegacyCombatPreset preset)
        {
            if(em==null)throw new ArgumentNullException(nameof(em));
            Bindings.Remove(em);
            if(preset!=null)Bindings.Add(em,new BoundPreset{Value=preset});
        }

        internal static void Unbind(EntityManager em)
        {
            if (em != null) Bindings.Remove(em);
        }

        static SourceS073LegacyCombatPreset ReadAndValidatePinnedSource(string assetPath)
        {
            if (!Path.IsPathRooted(assetPath ?? "")) throw new InvalidDataException("Absolute source asset path required");
            byte[] bytes = File.ReadAllBytes(assetPath);
            string hash;
            using (SHA256 sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if (!String.Equals(hash, AssetSha256, StringComparison.Ordinal))
                throw new InvalidDataException("S073 preset source asset hash differs from the pinned descriptor");
            string text = System.Text.Encoding.UTF8.GetString(bytes);
            float rifleRange = Float(text, "rifleRange", 32f);
            float rifleCooldown = Float(text, "rifleCooldown", 0.8f);
            float armoredCarRange = Float(text, "armoredCarRange", 40f);
            float armoredCarCooldown = Float(text, "armoredCarCooldown", 1.2f);
            float watchtowerRange = Float(text, "watchtowerRange", 55f);
            float watchtowerCooldown = Float(text, "watchtowerCooldown", 0.8f);
            float firstAttackSeconds = Float(text, "firstAttackSeconds", 60f);
            float baseDefenseRadius = Float(text, "baseDefenseRadius", 70f);
            int rifleDamage = Integer(text, "rifleDamage", 6);
            int armoredCarDamage = Integer(text, "armoredCarDamage", 18);
            int watchtowerDamage = Integer(text, "watchtowerDamage", 10);
            Vector3 rally = Vector(text, "playerReinforcementRallyOffset", 18f, 0f, -22f);
            return new SourceS073LegacyCombatPreset(rifleRange, rifleCooldown,
                armoredCarRange, armoredCarCooldown, watchtowerRange,
                watchtowerCooldown, firstAttackSeconds, baseDefenseRadius,
                rifleDamage, armoredCarDamage, watchtowerDamage, rally);
        }

        static string Field(string text, string name)
        {
            MatchCollection matches = Regex.Matches(text, "^  " + Regex.Escape(name) + ": ([^\\r\\n]+)$", RegexOptions.Multiline);
            if (matches.Count != 1) throw new InvalidDataException("Expected exactly one S073 serialized field: " + name);
            return matches[0].Groups[1].Value;
        }

        static float ParseFinite(string value, string name)
        {
            float parsed;
            if (!Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ||
                Single.IsNaN(parsed) || Single.IsInfinity(parsed))
                throw new InvalidDataException("Invalid finite S073 float field: " + name);
            return parsed;
        }

        static float Float(string text, string name, float expected)
        {
            float value = ParseFinite(Field(text, name), name);
            if (value != expected) throw new InvalidDataException("S073 source value differs from pinned descriptor: " + name);
            return value;
        }

        static int Integer(string text, string name, int expected)
        {
            string raw = Field(text, name);
            int value;
            if (!Regex.IsMatch(raw, "^-?(?:0|[1-9][0-9]*)$") ||
                !Int32.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value != expected)
                throw new InvalidDataException("S073 source integer differs from pinned descriptor: " + name);
            return value;
        }

        static Vector3 Vector(string text, string name, float expectedX, float expectedY, float expectedZ)
        {
            Match match = Regex.Match(Field(text, name), "^\\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\\}$");
            if (!match.Success) throw new InvalidDataException("Invalid S073 serialized vector: " + name);
            float x = ParseFinite(match.Groups[1].Value, name + ".x");
            float y = ParseFinite(match.Groups[2].Value, name + ".y");
            float z = ParseFinite(match.Groups[3].Value, name + ".z");
            if (x != expectedX || y != expectedY || z != expectedZ)
                throw new InvalidDataException("S073 source vector differs from pinned descriptor: " + name);
            return new Vector3(x, y, z);
        }
    }
}
