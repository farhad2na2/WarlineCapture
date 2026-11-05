using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        public static string AtlasMissionName(string objective,int slot)
        {
            string[] en,fa;
            switch(objective)
            {
                case "FC":
                    en=new[]{"FORWARD LINE","HOLD THE LINE","SKYWATCH","AIR CONTROL","COMBINED FRONT","STRONGPOINT"};
                    fa=new[]{"خط مقدم","حفظ خط","نگهبان آسمان","کنترل هوایی","جبهه ترکیبی","نقطه استوار"};break;
                case "BT":
                    en=new[]{"BREAK THE LINE","BASE BREAKER","AIR BREAKTHROUGH","AIRBASE BREACH","COMBINED PUSH","FULL ASSAULT"};
                    fa=new[]{"شکستن خط","شکننده پایگاه","پیشروی هوایی","نفوذ هوایی","پیشروی ترکیبی","یورش کامل"};break;
                case "CE":
                    en=new[]{"ROAD ESCORT","SUPPLY CONVOY","AIR COVER","BASE RESUPPLY","COMBINED ESCORT","RELIEF COLUMN"};
                    fa=new[]{"اسکورت جاده","کاروان تدارکات","پوشش هوایی","تدارک پایگاه","اسکورت ترکیبی","ستون پشتیبانی"};break;
                default:
                    en=new[]{"OUTPOST STRIKE","COMMAND SIEGE","AIR RAID","AIRBASE STRIKE","COMBINED ADVANCE","COMBINED SIEGE"};
                    fa=new[]{"یورش به پاسگاه","محاصره ستاد","یورش هوایی","حمله به پایگاه هوایی","پیشروی ترکیبی","محاصره ترکیبی"};break;
            }
            int index=Mathf.Clamp(slot,0,5);
            return UiShellRuntimeGateway.Localization.Get("ui.atlas.mission."+objective?.ToLowerInvariant()+"."+index,Game.Configs.GameLocalization.CurrentLocaleCode==Game.Configs.GameLocalization.PersianLocaleCode?fa[index]:en[index]);
        }
        private static string AtlasObjectivePurpose(string objective)
        {
            switch(objective)
            {
                case "FC":return AtlasWords.Get("purpose.fc","Control the front line. Hold your ground against the enemy.","خط مقدم را کنترل کن و در برابر دشمن نگه دار.");
                case "BT":return AtlasWords.Get("purpose.bt","Break through the enemy defenses.","از دفاع دشمن عبور کن.");
                case "CE":return AtlasWords.Get("purpose.ce","Protect the convoy along its route.","در مسیر از کاروان محافظت کن.");
                default:return AtlasWords.Get("purpose.ba","Destroy the enemy HQ. Keep your own HQ standing.","ستاد دشمن را نابود کن و از ستاد خودت محافظت کن.");
            }
        }
    }
}
