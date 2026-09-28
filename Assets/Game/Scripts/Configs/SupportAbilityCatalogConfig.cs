using System;
using Game.Components;
using UnityEngine;
namespace Game.Configs
{
    [CreateAssetMenu(menuName="Game/Support/Ability Catalog")]
    public sealed class SupportAbilityCatalogConfig : ScriptableObject
    {
        public uint Revision = 1;
        public string SourceHash;
        public SupportAbilityConfig[] Abilities = Array.Empty<SupportAbilityConfig>();
        public bool TryValidate(out string error)
        {
            error = null;
            if (Revision == 0 || Abilities == null || Abilities.Length != 4) { error="Exactly four abilities and a nonzero revision required."; return false; }
            int mask=0;
            foreach (var a in Abilities)
            {
                int kind=(int)a.Kind;
                if (kind<1 || kind>4 || (mask & (1<<kind))!=0 || a.Id!=Id(a.Kind)) { error="Missing, duplicate or mismatched Support identity."; return false; }
                mask |= 1<<kind;
                if (a.Charges<1 || a.Charges>8 || a.FuelCost<0 || !Finite(a.CooldownSeconds) || a.CooldownSeconds<0 ||
                    !Finite(a.Radius) || a.Radius<0 || !Finite(a.DurationSeconds) || a.DurationSeconds<0 ||
                    !Finite(a.ApproachSeconds) || a.ApproachSeconds<0 || a.Damage<0 || a.Materials<0 ||
                    a.DirectDamagePermille<1 || a.DirectDamagePermille>1000 || a.Icon==null || a.SourcePrefab==null)
                { error="Invalid Support scalar or missing project asset: "+a.Id; return false; }
                if (a.Kind==SupportAbilityKind.Smoke && (a.Radius<=0 || a.DurationSeconds<=0)) { error="Smoke requires a finite footprint and lifetime."; return false; }
                if (a.Kind!=SupportAbilityKind.Smoke && a.ApproachSeconds<=0) { error="Aircraft require a positive approach duration."; return false; }
            }
            return true;
        }
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static string Id(SupportAbilityKind kind) => kind switch
        { SupportAbilityKind.Smoke=>"ability.smoke_screen", SupportAbilityKind.Strike=>"ability.precision_strike",
          SupportAbilityKind.Paratroopers=>"ability.paratrooper_reinforcements", SupportAbilityKind.Supply=>"ability.supply_drop", _=>string.Empty };
    }
    [Serializable]
    public struct SupportAbilityConfig
    {
        public SupportAbilityKind Kind; public string Id, NameKey, DescriptionKey, UnlockMission;
        public int Charges, FuelCost, Damage, Materials;
        public float CooldownSeconds, Radius, DurationSeconds, ApproachSeconds;
        public ushort DirectDamagePermille; public bool ProductionReady;
        public Sprite Icon; public GameObject SourcePrefab;
    }
}
