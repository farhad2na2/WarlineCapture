using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes.Editor;
using UnityEditor;

namespace Game.Editor
{
    public sealed class OperationMapEntitySceneBuildAdditions : IEntitySceneBuildAdditions
    {
        public HashSet<Hash128> RegisterAdditionalEntityScenesToBuild()
        {
            string guid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.SourceSubScenePath);
            var sceneGuid = new Hash128(guid);
            if (!sceneGuid.IsValid)
            {
                throw new InvalidOperationException(
                    "The operation-map source subscene must resolve to a valid asset GUID.");
            }

            return new HashSet<Hash128>
            {
                sceneGuid
            };
        }
    }
}
