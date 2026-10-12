using System;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using Game.Components;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        // Objective anchors can own functional buildings too. Keep their complete
        // footprints and the placement cache's one-metre clearance free of scenery.
        static Rect[] CampaignProtectedBuildingLots(string map,IEnumerable<OperationMapDefinition> definitions)
        {
            var configs=definitions.Distinct().ToArray();
            var lots=configs.SelectMany(m=>m.Anchors.ToArray()).Where(a=>a.Kind==OperationMapAnchorKind.Build)
                .Select(a=>new Rect(a.Position.x-2,a.Position.z-2,34,30)).ToList();
            if(map=="CH05M01CitywideAlert")
                lots=CH05M01CitywideAlertConfigBuilder.BuildingLots.Select(l=>new Rect(l.xMin-2,l.yMin-2,l.width+4,l.height+4)).ToList();
            foreach(var config in configs.Where(c=>c.OperationMapId==CH02M02SupplyLineConfigBuilder.MapId))
            foreach(var lot in new[]{(suffix:"oil",width:15,height:4),(suffix:"refinery",width:25,height:15),(suffix:"storage",width:24,height:23)})
            {
                var anchor=config.Anchors.ToArray().Single(a=>a.AnchorId==CH02M02SupplyLineConfigBuilder.Prefix+lot.suffix);
                lots.Add(new Rect(anchor.Position.x-2,anchor.Position.z-2,lot.width+4,lot.height+4));
            }
            return lots.ToArray();
        }
    }
}
