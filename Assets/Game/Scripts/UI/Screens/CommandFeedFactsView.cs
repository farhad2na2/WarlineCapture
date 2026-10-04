using Game.Configs;
using TMPro;
using UnityEngine;
namespace Game.UI.Runtime
{
    // Saved player facts replace the authored example events and timestamps.
    [DefaultExecutionOrder(650)]
    public sealed class CommandFeedFactsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] titles,bodies;
        private float nextRefresh;
        public void Configure(TMP_Text[] headings,TMP_Text[] copy){titles=headings;bodies=copy;}
        private void LateUpdate()
        {
            if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.25f;
            bool fa=GameLocalization.IsRightToLeft;
            if(UiShellRuntimeGateway.TryReadCommanderProfile(out var p))
            {
                Set(0,fa?"کارنامه ماموریت":"MISSION RECORD",fa?$"ماموریت‌های تکمیل‌شده: {p.Missions}\nستاره‌ها: {p.Stars}\nپیروزی: {p.Victories} · شکست: {p.Defeats}":$"MISSIONS COMPLETED  {p.Missions}\nSTARS EARNED  {p.Stars}\nVICTORIES  {p.Victories} · DEFEATS  {p.Defeats}");
                Set(2,fa?"پیشرفت فرمانده":"COMMANDER PROGRESS",fa?$"سطح: {p.Level}\nامتیاز تجربه: {p.Xp:N0}":$"LEVEL  {p.Level}\nCOMMANDER XP  {p.Xp:N0}");
            }
            Set(1,"ARIA",fa?"پیش از اعزام، اهداف ماموریت را مرور کنید.":"Review the mission objectives before deployment.");
            if(UiShellRuntimeGateway.TryReadOperationsMission(out var op))Set(3,op.Title,op.Status+"\n"+op.Description);
            else Set(3,fa?"عملیات":"OPERATIONS",fa?"برای مرور ماموریت‌های موجود، عملیات را باز کنید.":"Open Operations to review available missions.");
            if(UiShellRuntimeGateway.TryReadMissionBriefing(out var m)&&m.IsValid)Set(4,GameLocalization.Get(m.DisplayNameKey),GameLocalization.Get(m.DisplaySummaryKey));
            else Set(4,fa?"ماموریت بعدی":"NEXT MISSION",fa?"یک ماموریت را از کمپین انتخاب کنید.":"Select a mission from Campaign.");
        }
        private void Set(int index,string title,string body){if(titles!=null&&index<titles.Length)UiLocalizedText.Set(titles[index],title);if(bodies!=null&&index<bodies.Length)UiLocalizedText.Set(bodies[index],body);}
    }
}
