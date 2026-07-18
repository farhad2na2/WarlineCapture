using System;

namespace Game.Configs
{
    public static class OperationMapContentAddressContract
    {
        private const int MaximumChunkIdLength = 96;

        public static bool TryBuildPresentationChunkAddress(
            string operationMapId,
            string chunkId,
            out string address,
            out string error)
        {
            address = null;
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }

            if (!IsValidChunkId(chunkId))
            {
                error = $"Invalid static-presentation chunk id: '{chunkId ?? "<null>"}'.";
                return false;
            }

            address = $"operation-map/{operationMapId}/presentation/{chunkId}";
            error = null;
            return true;
        }

        private static bool IsValidChunkId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumChunkIdLength)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!char.IsLetterOrDigit(character) &&
                    character != '.' && character != '_' && character != '-')
                    return false;
            }

            return true;
        }
    }
}
