using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;

internal static class WorldSchedule
{
    internal static bool Enabled;
    internal static string Profile, Hash;
    internal static string[] Stages = Array.Empty<string>();
    internal static void Load(string path)
    {
        byte[] bytes=System.IO.File.ReadAllBytes(path);Hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        using var doc=JsonDocument.Parse(bytes);var root=doc.RootElement;
        Enabled=root.GetProperty("enabled").GetBoolean();
        if(!Enabled)return;
        var order=root.GetProperty("order");Profile=order.GetProperty("profile").GetString();
        WorldJson.Need(Profile=="IsolatedStationaryCombatScheduleV1"&&order.GetProperty("scope").GetString()=="NativeSortedSubsetOnly"&&!order.GetProperty("full_world").GetBoolean(),"Unsupported native schedule receipt");
        var systems=order.GetProperty("systems").EnumerateArray().Select(x=>x.GetString()).ToArray();
        var mapping=new Dictionary<string,string> {
            ["Unity.Entities.BeginSimulationEntityCommandBufferSystem"]="begin",
            ["Game.Runtime.DynamicOccupancyRebuildSystem"]="occupancy",
            ["Game.Runtime.UnitEngagementSystem"]="acquisition",
            ["Game.Runtime.UnitEngagedMovementSystem"]="stationary_engagement",
            ["Game.Runtime.UnitAttackSystem"]="attack",
            ["Game.Runtime.SkirmishObjectiveFactProjectionSystem"]="projection",
            ["Game.Runtime.SkirmishBaseAssaultObjectiveSystem"]="evaluation",
            ["Game.Runtime.SkirmishOutcomeSystem"]="outcome",
            ["Unity.Entities.EndSimulationEntityCommandBufferSystem"]="end"};
        WorldJson.Need(systems.Length==mapping.Count&&systems.Distinct().Count()==mapping.Count&&systems.All(mapping.ContainsKey),"Native schedule membership differs");
        Stages=systems.Select(x=>mapping[x]).ToArray();
        bool Before(string a,string b)=>Array.IndexOf(Stages,a)<Array.IndexOf(Stages,b);
        WorldJson.Need(Stages[0]=="begin"&&Stages[^1]=="end"&&Before("occupancy","acquisition")&&Before("acquisition","stationary_engagement")&&Before("stationary_engagement","attack")&&Before("projection","evaluation")&&Before("evaluation","outcome"),"Invalid captured partial order");
    }
}
