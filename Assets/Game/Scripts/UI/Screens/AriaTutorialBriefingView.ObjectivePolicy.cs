using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private static bool TryPublicObjectiveInstruction(AriaSkirmishIntent intent,bool fa,out string text)
        {
            text=intent switch
            {
                AriaSkirmishIntent.ControlZone => fa ? "دو منطقه را نگه می‌داریم. این گروه را برای کنترل منطقه می‌فرستم." : "We need two zones. I'll send this group to secure one.",
                AriaSkirmishIntent.OpenObjectiveCorridor => fa ? "پیاده‌نظام باید مسیر خروج را پیوسته نگه دارد تا راه خروج باز شود." : "Our infantry must hold a corridor continuously to open the exit.",
                AriaSkirmishIntent.EvacuateOriginals => fa ? "راه خروج باز است. سربازهای مشخص‌شده را به خروج می‌برم." : "The exit is open. I'll move our designated soldiers to it.",
                AriaSkirmishIntent.EscortOriginalTruck => fa ? "کامیون‌های مأموریت را به مقصد می‌بریم و از آن‌ها محافظت می‌کنیم." : "I'll move the objective trucks toward the destination and keep escorts with them.",
                AriaSkirmishIntent.InspectObjective => fa ? "اول مسیر و هدف قابل مشاهده را بررسی می‌کنم؛ جابه‌جایی دوربین دستور حرکت نیست." : "I'll inspect the public route first. Moving the camera does not order our troops.",
                AriaSkirmishIntent.ObserveObjective => fa ? "دستور پذیرفته شده است. پیشرفت هدف و وضعیت نیروها را نگاه می‌کنم." : "The order was accepted. I'll watch objective progress and our units.",
                AriaSkirmishIntent.RepairVehicle => fa ? "هزینه و زمان تعمیر را می‌بینم. پیشرفت تعمیر و سلامت خودرو را بررسی می‌کنم." : "I can see the repair cost and time. I will watch repair progress and vehicle health.",
                AriaSkirmishIntent.UnloadOriginals => fa ? "سربازهای مشخص‌شده باید روی زمین پیاده شوند تا بتوانند خارج شوند." : "Our designated soldiers must unload onto legal ground before they can evacuate.",
                _ => null
            };
            return text!=null;
        }
    }
}
