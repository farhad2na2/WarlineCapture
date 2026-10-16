using Game.UI.Contracts;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    // Read-only view of controls already presented by the normal passenger UI.
    // This API never opens the drawer, selects a carrier or invokes a command.
    public readonly struct MatchHudPresentedTransportControls
    {
        public readonly MatchHudTransportPassengersModel Model;
        public readonly Button PassengerChip,ExitAll,Close;
        public readonly bool DrawerOpen;
        public MatchHudPresentedTransportControls(MatchHudTransportPassengersModel model,Button chip,Button exitAll,Button close,bool open)
        {Model=model;PassengerChip=chip;ExitAll=exitAll;Close=close;DrawerOpen=open;}
    }
    public sealed partial class MatchHudSelectionPanelView
    {
        public bool TryReadPresentedTransportControls(out MatchHudPresentedTransportControls controls)
        {
            controls=default;
            var model=_latestPassengerModel;
            if(!isActiveAndEnabled||!gameObject.activeInHierarchy||!model.Visible||
                model.StorageKind!=MatchHudStorageChipKind.Passengers||model.Transport.IsNull||
                passengerChipButton==null||!passengerChipButton.gameObject.activeInHierarchy||!passengerChipButton.interactable)return false;
            bool open=_passengerDrawerOpen&&_passengerDrawerTransport==model.Transport;
            Button exit=null,close=null;
            if(open&&(passengerDrawer==null||!passengerDrawer.TryReadPresentedExitControls(out exit,out close)))return false;
            controls=new MatchHudPresentedTransportControls(model,passengerChipButton,exit,close,open);
            return true;
        }
    }
}
