using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using Game.UI.Contracts;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        static void ReadPublicOriginalUnload(EntityManager em,Entity session,UiSkirmishPublicObjectiveModel model)
        {
            if(!em.HasComponent<SkirmishOriginalUnloadPresentation>(session))return;
            var row=em.GetComponentObject<SkirmishOriginalUnloadPresentation>(session);
            if(row==null||row.Ids==null||row.Actors==null||!row.Presented)return;
            bool valid=SkirmishOriginalUnloadPresentation.TryReadProgress(em,session,row,out int complete,out int pending,out bool partial);
            model.UnloadCompleted=complete;model.UnloadPending=pending;
            model.UnloadScopeCompleted=row.RequestAccepted&&row.ScopeAccepted&&valid&&pending==0&&complete==row.Ids.Length;
            model.UnloadPartialPlacement=partial;
            bool fa=GameLocalization.IsRightToLeft;
            model.UnloadStatus=!row.RequestAccepted?(fa?"پیاده شدن پذیرفته نشد.":"Unable to unload."):
                partial?(fa?"برای پیاده شدن همه جا کافی نیست.":"Some soldiers could not exit here."):
                !valid?(fa?"پیاده شدن سربازها تأیید نشد.":"Soldier dismount could not be verified."):
                model.UnloadScopeCompleted?(fa?"سربازهای این نفربر پیاده شدند.":"Soldiers from this carrier dismounted."):
                (fa?"در حال پیاده شدن ":"UNLOADING ")+complete+" / "+row.Ids.Length;
            if((!valid||pending==0)&&UnityEngine.Time.unscaledTime-row.BeganAt>4)model.UnloadStatus=null;
            if(model.Order.Action==Game.UI.Contracts.AriaObjectiveAction.Unload&&model.Order.RequestId==row.RequestId&&model.Order.Frame==row.Frame)
                model.Order.Accepted&=row.ScopeAccepted&&valid;
        }
    }
}
