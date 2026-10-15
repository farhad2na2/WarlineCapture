using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishEnemyPolicyCompiler
    {
        public static bool TryCompile(SkirmishResolvedSetup setup, SkirmishMapLayoutConfig layout,
            out SkirmishEnemyPolicySnapshot policy, SkirmishDifficultyConfig difficultyConfig = null)
        {
            policy = null;
            if (setup == null || (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault && !SkirmishEnemyObjectivePolicyScope.Supports(setup))) return false;
            if (!SkirmishDifficultyConfig.TryGetPolicyValues(setup.DifficultyId, out float responseMin,
                out float responseMax, out float scoutMin, out float scoutMax, out int fieldOpening,
                out int establishedOpening) ||
                (setup.Readiness != SkirmishReadinessStage.Field && setup.Readiness != SkirmishReadinessStage.Established &&
                 setup.Readiness != SkirmishReadinessStage.FullArsenal)) return false;
            if (difficultyConfig != null)
            {
                if (difficultyConfig.Kind != setup.DifficultyId) return false;
                responseMin = difficultyConfig.ResponseMinSeconds; responseMax = difficultyConfig.ResponseMaxSeconds;
                scoutMin = difficultyConfig.ScoutMinSeconds; scoutMax = difficultyConfig.ScoutMaxSeconds;
                establishedOpening = difficultyConfig.EstablishedOpeningRestraintSeconds;
                if (!float.IsFinite(responseMin) || !float.IsFinite(responseMax) || responseMin <= 0 || responseMax < responseMin ||
                    !float.IsFinite(scoutMin) || !float.IsFinite(scoutMax) || scoutMin <= 0 || scoutMax < scoutMin || establishedOpening < 0) return false;
            }
            var routes = new List<SkirmishEnemyRouteSnapshot>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (layout != null && layout.Routes != null)
            {
                foreach (var route in layout.Routes)
                {
                    if (string.IsNullOrEmpty(route.RouteId) || route.RouteId.Length > 61 || !ids.Add(route.RouteId) ||
                        !float.IsFinite(route.WidthMetres) || route.WidthMetres <= 0 || route.Kind == SkirmishMeasuredRouteKind.None ||
                        !float.IsFinite(route.LengthMetres) || route.LengthMetres <= 0 ||
                        !float.IsFinite(route.InfantryTransitSeconds) || route.InfantryTransitSeconds < 0 ||
                        !float.IsFinite(route.GroundTransitSeconds) || route.GroundTransitSeconds < 0 ||
                        route.WaypointAnchorIds == null || route.WaypointAnchorIds.Length < 2 || route.WaypointAnchorIds.Length > 64)
                        return false;
                    var points = new SkirmishEnemyWaypoint[route.WaypointAnchorIds.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (!layout.TryGetAnchorById(route.WaypointAnchorIds[i], out var anchor)) return false;
                        float x = anchor.WorldX, z = anchor.WorldZ;
                        if (layout.HasWorldBinding && !layout.TryProjectLocalToMap(x, z, out x, out z)) return false;
                        if (!float.IsFinite(x) || !float.IsFinite(z)) return false;
                        points[i] = new SkirmishEnemyWaypoint(x, z);
                    }
                    routes.Add(new SkirmishEnemyRouteSnapshot(route.RouteId, route.Kind, route.WidthMetres,
                        route.AllowsVehicles, route.AllowsAircraft, points, route.LengthMetres,
                        route.InfantryTransitSeconds, route.GroundTransitSeconds, route.ProvisionalTimes));
                }
            }
            var pads = new List<SkirmishEnemyConstructionPad>();
            var padIds = new HashSet<string>(StringComparer.Ordinal);
            if(layout?.Pads!=null) foreach(var pad in layout.Pads)
            {
                if(pad.FactionId!=0&&pad.FactionId!=setup.EnemyFaction)continue;
                if(pad.Kind!=SkirmishLegalPadKind.BaseBarracks&&pad.Kind!=SkirmishLegalPadKind.GroundStaging&&
                   pad.Kind!=SkirmishLegalPadKind.Service&&pad.Kind!=SkirmishLegalPadKind.Supply&&
                   pad.Kind!=SkirmishLegalPadKind.SupplyExpansion&&pad.Kind!=SkirmishLegalPadKind.AirReturn)continue;
                if(string.IsNullOrEmpty(pad.PadId)||pad.PadId.Length>61||!padIds.Add(pad.PadId)||
                   !float.IsFinite(pad.CenterX)||!float.IsFinite(pad.CenterZ)||!float.IsFinite(pad.WidthMetres)||
                   !float.IsFinite(pad.DepthMetres)||pad.WidthMetres<=0||pad.DepthMetres<=0)return false;
                float x=pad.CenterX,z=pad.CenterZ,ax=x+pad.WidthMetres*.5f,az=z,bx=x,bz=z+pad.DepthMetres*.5f;
                if(layout.HasWorldBinding&&(!layout.TryProjectLocalToMap(x,z,out x,out z)||
                    !layout.TryProjectLocalToMap(ax,az,out ax,out az)||!layout.TryProjectLocalToMap(bx,bz,out bx,out bz)))return false;
                var compiled=new SkirmishEnemyConstructionPad(pad.PadId,pad.Kind,pad.FactionId,x,z,ax-x,az-z,bx-x,bz-z);
                if(!compiled.Contains(x,z))return false;
                pads.Add(compiled);
            }
            pads.Sort((a,b)=>string.CompareOrdinal(a.Id,b.Id));
            routes.Sort((a, b) => string.CompareOrdinal(a.RouteId, b.RouteId));
            // Independent stream: canonical seed, source and settings; never UnityEngine.Random.
            uint random = HashText("enemy-policy-v1|" + setup.ContentHash + "|" + setup.Seed.ToString(CultureInfo.InvariantCulture));
            if (random == 0) random = 0x6D2B79F5;
            byte personality = (byte)(random % 3);
            float opening = setup.Readiness == SkirmishReadinessStage.Field ? fieldOpening : establishedOpening;
            var canonical = new StringBuilder("enemy-policy-v2|economy-native-v1|construction-native-v1|capability-native-v1|");
            Append(canonical, setup.ContentHash); Append(canonical, (byte)setup.DifficultyId); Append(canonical, (byte)setup.Readiness);
            Append(canonical, setup.Seed); Append(canonical, responseMin); Append(canonical, responseMax);
            Append(canonical, scoutMin); Append(canonical, scoutMax); Append(canonical, opening);
            Append(canonical, "4|.25|15|.25|2|3|4"); Append(canonical, personality); Append(canonical, random);
            foreach (var route in routes)
            {
                Append(canonical, route.RouteId); Append(canonical, (byte)route.Kind); Append(canonical, route.WidthMetres);
                Append(canonical, route.LengthMetres); Append(canonical, route.InfantryTransitSeconds);
                Append(canonical, route.GroundTransitSeconds); Append(canonical, route.ProvisionalTimes ? 1 : 0);
                Append(canonical, route.AllowsVehicles ? 1 : 0); Append(canonical, route.AllowsAircraft ? 1 : 0);
                foreach (var point in route.Waypoints) { Append(canonical, point.X); Append(canonical, point.Z); }
            }
            if(pads.Count>0)
            {
                Append(canonical,"construction-pads-v1");
                foreach(var pad in pads)
                { Append(canonical,pad.Id);Append(canonical,(byte)pad.Kind);Append(canonical,pad.Faction);
                  Append(canonical,pad.X);Append(canonical,pad.Z);Append(canonical,pad.AX);Append(canonical,pad.AZ);
                  Append(canonical,pad.BX);Append(canonical,pad.BZ); }
            }
            if(setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault)
            {Append(canonical,"public-objective-v1");Append(canonical,(byte)setup.ObjectiveKind);Append(canonical,setup.MissionLogicalBindingFingerprint);}
            string text = canonical.ToString();
            string fingerprint;
            using (var sha = SHA256.Create()) fingerprint = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
            policy = new SkirmishEnemyPolicySnapshot(setup.ContentHash, setup.DifficultyId, setup.Readiness,
                responseMin, responseMax, scoutMin, scoutMax, opening, personality, random, routes.ToArray(), HashText(text), fingerprint, pads.ToArray());
            return true;
        }
        private static void Append(StringBuilder builder, object value)
        {
            string text = value is float f ? f.ToString("R", CultureInfo.InvariantCulture) :
                value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? string.Empty;
            builder.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append('|');
        }
        public static uint HashText(string text)
        {
            unchecked { uint hash = 2166136261; foreach (char c in text ?? string.Empty) hash = (hash ^ c) * 16777619; return hash; }
        }
    }
}
