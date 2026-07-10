using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    public static class StaticMapMeshReadbackUtility
    {
        private const MeshUpdateFlags CopyFlags =
            MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontValidateIndices |
            MeshUpdateFlags.DontNotifyMeshUsers;

        internal static Mesh CreateReadableClone(Mesh source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            VertexAttributeDescriptor[] vertexAttributes = source.GetVertexAttributes();
            Bounds sourceBounds = source.bounds;

            using Mesh.MeshDataArray sourceDataArray = MeshUtility.AcquireReadOnlyMeshData(source);
            Mesh.MeshDataArray destinationDataArray = Mesh.AllocateWritableMeshData(1);
            bool destinationDataNeedsDisposal = true;
            Mesh clone = null;

            try
            {
                Mesh.MeshData sourceData = sourceDataArray[0];
                Mesh.MeshData destinationData = destinationDataArray[0];

                destinationData.SetVertexBufferParams(sourceData.vertexCount, vertexAttributes);
                for (int stream = 0; stream < sourceData.vertexBufferCount; stream++)
                {
                    NativeArray<byte> sourceStream = sourceData.GetVertexData<byte>(stream);
                    NativeArray<byte> destinationStream = destinationData.GetVertexData<byte>(stream);
                    destinationStream.CopyFrom(sourceStream);
                }

                NativeArray<byte> sourceIndices = sourceData.GetIndexData<byte>();
                int bytesPerIndex = sourceData.indexFormat == IndexFormat.UInt16 ? sizeof(ushort) : sizeof(uint);
                destinationData.SetIndexBufferParams(sourceIndices.Length / bytesPerIndex, sourceData.indexFormat);
                destinationData.GetIndexData<byte>().CopyFrom(sourceIndices);

                destinationData.subMeshCount = sourceData.subMeshCount;
                for (int subMesh = 0; subMesh < sourceData.subMeshCount; subMesh++)
                    destinationData.SetSubMesh(subMesh, sourceData.GetSubMesh(subMesh), CopyFlags);

                clone = new Mesh
                {
                    name = $"{source.name} (Readable Clone)"
                };

                destinationDataNeedsDisposal = false;
                Mesh.ApplyAndDisposeWritableMeshData(destinationDataArray, clone, CopyFlags);
                clone.bounds = sourceBounds;
                return clone;
            }
            catch
            {
                if (clone != null)
                    UnityEngine.Object.DestroyImmediate(clone);
                throw;
            }
            finally
            {
                if (destinationDataNeedsDisposal)
                    destinationDataArray.Dispose();
            }
        }
    }
}
