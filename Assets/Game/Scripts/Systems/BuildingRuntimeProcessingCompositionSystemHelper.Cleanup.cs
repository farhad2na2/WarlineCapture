using System;

namespace Game.Runtime
{
    public sealed partial class BuildingRuntimeProcessingCompositionSystemHelper
    {
        private readonly BuildingRuntimeDeleteCommandProcessor _deleteCommandProcessor=new();
        private Func<int,bool> _delete;
        private Action<int> _cleanup;
        internal void ConfigureDeleteBuildingById(Func<int,bool> deleteBuildingById)=>_delete=deleteBuildingById;
        internal void ConfigureAttemptCleanup(Action<int> cleanupBuildingById)=>_cleanup=cleanupBuildingById;
    }
}
