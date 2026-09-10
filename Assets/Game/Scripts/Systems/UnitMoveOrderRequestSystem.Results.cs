using Game.Components;

namespace Game.Runtime
{
    public partial struct UnitMoveOrderRequestSystem
    {
        private static UnitMoveOrderResultElement ToResult(
            UnitMoveOrderRequestElement request,
            UnitMoveOrderSystem.MoveOrderCommandResult commandResult)
        {
            return new UnitMoveOrderResultElement
            {
                RequestId = request.RequestId,
                Kind = request.Kind,
                Entity = request.Entity,
                Goal = request.Goal,
                Issued = commandResult.Issued ? (byte)1 : (byte)0,
                StructuralAdds = commandResult.StructuralAdds,
                StructuralRemoves = commandResult.StructuralRemoves,
                PathRequests = commandResult.PathRequests,
                StaggeredPathRequests = commandResult.StaggeredPathRequests,
                MaxStaggerDelayFrames = commandResult.MaxStaggerDelayFrames,
                AirUnits = commandResult.AirUnits,
                RejectionReasonCode = commandResult.RejectionReasonCode
            };
        }
    }
}
