namespace Game.Configs
{
    // Keep the Addressables-backed source reference inside its owning Configs
    // boundary; Runtime consumes only the explicit stable value.
    public static class SkirmishNativeRepairSourceIdentity
    {
        public static string SceneGuid(OperationMapDefinition source)=>source==null?null:source.SourceSceneReference.AssetGUID;
    }
}
