using System;
using Game.Configs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Composition
{
    /// <summary>
    /// Resolves the isolated dense-city definition only in the explicitly scoped
    /// candidate player. Production players never mount the candidate catalog.
    /// </summary>
    internal sealed class OperationMapDenseCityCandidateRuntimeOverride : IDisposable
    {
        internal const string BuildDefine = "WARLINE_DENSE_CITY_CANDIDATE";
        internal const string OperationMapId = "opmap.skirmish.desert_base_01";
        internal const string EntitySceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        internal const string DefinitionAddress =
            "operation-map-candidate/opmap.skirmish.desert_base_01/dense-city/definition";
        internal const string CatalogRelativePath = "aa/DenseCityCandidate/catalog.bin";

        private AsyncOperationHandle<IResourceLocator> catalogHandle;
        private AsyncOperationHandle<OperationMapDefinition> definitionHandle;
        private bool catalogStarted;
        private bool definitionStarted;
        private bool disposed;

        internal static bool CandidateBuildEnabled
        {
            get
            {
#if WARLINE_DENSE_CITY_CANDIDATE
                return true;
#else
                return false;
#endif
            }
        }

        internal bool TryResolve(
            OperationMapCatalogConfig productionCatalog,
            string operationMapId,
            out OperationMapDefinition definition,
            out bool waiting,
            out string error)
        {
            definition = null;
            waiting = false;
            error = null;

            if (!CandidateBuildEnabled)
            {
                if (productionCatalog == null)
                {
                    error = "Operation-map catalog is required.";
                    return false;
                }

                if (!productionCatalog.TryValidate(out error))
                    return false;

                if (!productionCatalog.TryResolve(operationMapId, out definition))
                {
                    error =
                        $"Operation-map id '{operationMapId ?? "<null>"}' is not present in the catalog.";
                    return false;
                }

                return true;
            }

            if (disposed)
            {
                error = "Dense-city candidate runtime override is disposed.";
                return false;
            }

            if (!string.Equals(operationMapId, OperationMapId, StringComparison.Ordinal))
            {
                error =
                    $"Dense-city candidate player refuses operation-map id '{operationMapId ?? "<null>"}'.";
                return false;
            }

            if (!catalogStarted)
            {
                string catalogPath =
                    Application.streamingAssetsPath.TrimEnd('/', '\\') + "/" +
                    CatalogRelativePath;
                catalogHandle = Addressables.LoadContentCatalogAsync(
                    catalogPath,
                    autoReleaseHandle: false);
                catalogStarted = true;
                waiting = true;
                Debug.Log(
                    "[DenseCityCandidateRuntime] stage=CatalogLoadStarted " +
                    $"path={catalogPath}");
                return false;
            }

            if (!catalogHandle.IsDone)
            {
                waiting = true;
                return false;
            }

            if (catalogHandle.Status != AsyncOperationStatus.Succeeded)
            {
                error =
                    "Dense-city candidate catalog failed to load: " +
                    (catalogHandle.OperationException?.Message ?? "unknown error");
                return false;
            }

            if (!definitionStarted)
            {
                definitionHandle =
                    Addressables.LoadAssetAsync<OperationMapDefinition>(DefinitionAddress);
                definitionStarted = true;
                waiting = true;
                Debug.Log(
                    "[DenseCityCandidateRuntime] stage=DefinitionLoadStarted " +
                    $"address={DefinitionAddress}");
                return false;
            }

            if (!definitionHandle.IsDone)
            {
                waiting = true;
                return false;
            }

            if (definitionHandle.Status != AsyncOperationStatus.Succeeded ||
                definitionHandle.Result == null)
            {
                error =
                    "Dense-city candidate definition failed to load: " +
                    (definitionHandle.OperationException?.Message ?? "missing result");
                return false;
            }

            OperationMapDefinition candidate = definitionHandle.Result;
            if (!candidate.TryValidateMetadata(out error))
            {
                error = "Dense-city candidate definition metadata is invalid: " + error;
                return false;
            }

            if (!string.Equals(
                    candidate.OperationMapId,
                    OperationMapId,
                    StringComparison.Ordinal))
            {
                error =
                    "Dense-city candidate definition operation-map id does not match the " +
                    $"candidate player: '{candidate.OperationMapId ?? "<null>"}'.";
                return false;
            }

            if (candidate.PresentationKind != OperationMapPresentationKind.EntityScene)
            {
                error =
                    "Dense-city candidate definition must use EntityScene presentation.";
                return false;
            }

            if (!string.Equals(
                    candidate.NavigationMetadata.AuthoredSubSceneGuid,
                    EntitySceneGuid,
                    StringComparison.Ordinal))
            {
                error =
                    "Dense-city candidate definition EntityScene GUID mismatch: " +
                    $"expected={EntitySceneGuid} " +
                    $"actual={candidate.NavigationMetadata.AuthoredSubSceneGuid ?? "<null>"}.";
                return false;
            }

            definition = candidate;
            Debug.Log(
                "[DenseCityCandidateRuntime] result=Resolved " +
                $"operationMapId={candidate.OperationMapId} entitySceneGuid={EntitySceneGuid}");
            return true;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (definitionHandle.IsValid())
                Addressables.Release(definitionHandle);
            if (catalogHandle.IsValid())
            {
                if (catalogHandle.Status == AsyncOperationStatus.Succeeded)
                    Addressables.RemoveResourceLocator(catalogHandle.Result);
                Addressables.Release(catalogHandle);
            }
        }
    }
}
