using Game.Components;
using Game.Configs;
using UnityEngine.AddressableAssets;

namespace Game.Composition
{
    internal readonly struct OperationMapSceneLoadRequest
    {
        public OperationMapSceneLoadRequest(
            object sourceRuntimeKey,
            object manifestRuntimeKey,
            string operationMapId,
            string sourceSceneGuid,
            bool usesEntityScene)
        {
            SourceRuntimeKey = sourceRuntimeKey;
            ManifestRuntimeKey = manifestRuntimeKey;
            OperationMapId = operationMapId;
            SourceSceneGuid = sourceSceneGuid;
            UsesEntityScene = usesEntityScene;
        }

        public object SourceRuntimeKey { get; }
        public object ManifestRuntimeKey { get; }
        public string OperationMapId { get; }
        public string SourceSceneGuid { get; }
        public bool UsesEntityScene { get; }
    }

    internal static class OperationMapSceneLoadRequestValidation
    {
        public static bool TryCreate(
            OperationMapDefinition definition,
            out OperationMapSceneLoadRequest request,
            out OperationMapLoadResultCode failureCode,
            out string error)
        {
            request = default;
            if (definition == null)
            {
                failureCode = OperationMapLoadResultCode.MissingDefinition;
                error = "Operation-map definition is required.";
                return false;
            }

            if (!definition.TryValidateIdentity(out error))
            {
                failureCode = OperationMapLoadResultCode.InvalidOperationMapId;
                return false;
            }

            AssetReference sourceReference = definition.SourceSceneReference;
            if (sourceReference == null || !sourceReference.RuntimeKeyIsValid())
            {
                failureCode = OperationMapLoadResultCode.MissingSourceContent;
                error = "Operation-map source-scene reference is missing or invalid.";
                return false;
            }

            bool entityScene =
                OperationMapEntityScenePresentationPolicy.ShouldSkipStaticManifestStreamerAndOwnership(
                    definition);
            AssetReference manifestReference = definition.StaticPresentationManifestReference;
            if (!entityScene &&
                (manifestReference == null || !manifestReference.RuntimeKeyIsValid()))
            {
                failureCode = OperationMapLoadResultCode.MissingSourceContent;
                error = "Operation-map presentation-manifest reference is missing or invalid.";
                return false;
            }

            if (entityScene &&
                manifestReference != null &&
                !string.IsNullOrEmpty(manifestReference.AssetGUID))
            {
                failureCode = OperationMapLoadResultCode.StaleContent;
                error =
                    "EntityScene operation-map loads must not bind a production static presentation manifest.";
                return false;
            }

            string expectedSceneOperationMapId = definition.SourceBinding.IsConfigured
                ? definition.SourceBinding.SourceOperationMapId
                : definition.OperationMapId;
            request = new OperationMapSceneLoadRequest(
                sourceReference.RuntimeKey,
                entityScene ? null : manifestReference.RuntimeKey,
                expectedSceneOperationMapId,
                sourceReference.AssetGUID,
                entityScene);
            failureCode = OperationMapLoadResultCode.None;
            error = null;
            return true;
        }
    }
}
