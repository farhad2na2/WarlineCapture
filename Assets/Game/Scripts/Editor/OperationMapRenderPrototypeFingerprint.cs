using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Unity.Mathematics;

namespace Game.Editor
{
    internal struct OperationMapRenderPrototypeFingerprintInput
    {
        internal string RendererPath;
        internal string MeshAssetGuid;
        internal long MeshLocalId;
        internal string MaterialAssetGuid;
        internal long MaterialLocalId;
        internal int SubMeshIndex;
        internal float4x4 LocalToPlacement;
        internal OperationMapRenderBoundsBlob LocalBounds;
        internal float4 LinearBaseColor;
        internal OperationMapRenderPolicyBucket PolicyBucket;
        internal OperationMapRenderShadowFlags ShadowFlags;
        internal OperationMapRenderLodFlags LodFlags;
    }

    internal static class OperationMapRenderPrototypeFingerprint
    {
        private const int SchemaVersion = 1;
        private static readonly UTF8Encoding Utf8WithoutBom =
            new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        internal static bool TryCompute(
            in OperationMapRenderPrototypeFingerprintInput input,
            out OperationMapRenderIdentity128 fingerprint,
            out string error)
        {
            fingerprint = default;
            if (!TryValidate(in input, out error))
                return false;

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Utf8WithoutBom, leaveOpen: true))
            {
                writer.Write(SchemaVersion);
                WriteString(writer, input.RendererPath);
                WriteString(writer, input.MeshAssetGuid);
                writer.Write(input.MeshLocalId);
                WriteString(writer, input.MaterialAssetGuid);
                writer.Write(input.MaterialLocalId);
                writer.Write(input.SubMeshIndex);
                Write(writer, input.LocalToPlacement);
                Write(writer, input.LocalBounds.Center);
                Write(writer, input.LocalBounds.Extents);
                Write(writer, input.LinearBaseColor);
                writer.Write((byte)input.PolicyBucket);
                writer.Write((byte)input.ShadowFlags);
                writer.Write((byte)input.LodFlags);
                writer.Flush();
            }

            byte[] digest;
            stream.Position = 0;
            using (SHA256 sha256 = SHA256.Create())
                digest = sha256.ComputeHash(stream);

            fingerprint = OperationMapRenderIdentityProjection.FromSha256Digest(digest);
            error = null;
            return true;
        }

        private static bool TryValidate(
            in OperationMapRenderPrototypeFingerprintInput input,
            out string error)
        {
            if (string.IsNullOrEmpty(input.RendererPath) ||
                input.RendererPath[0] == '/' ||
                input.RendererPath.IndexOf('\\') >= 0 ||
                input.RendererPath.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                input.RendererPath.IndexOf(':') >= 0)
            {
                error = "Renderer path must be a non-empty normalized relative hierarchy path.";
                return false;
            }

            if (!IsLowerHexGuid(input.MeshAssetGuid) || input.MeshLocalId == 0)
            {
                error = "Mesh identity requires a 32-character lowercase GUID and nonzero local id.";
                return false;
            }

            if (!IsLowerHexGuid(input.MaterialAssetGuid) || input.MaterialLocalId == 0)
            {
                error = "Material identity requires a 32-character lowercase GUID and nonzero local id.";
                return false;
            }

            if (input.SubMeshIndex < 0)
            {
                error = "Submesh index must be nonnegative.";
                return false;
            }

            if (!IsFinite(input.LocalToPlacement) ||
                !IsFinite(input.LocalBounds.Center) ||
                !IsFinite(input.LocalBounds.Extents) ||
                math.any(input.LocalBounds.Extents < 0f))
            {
                error = "Prototype matrices and bounds must be finite with nonnegative extents.";
                return false;
            }

            if (!IsFinite(input.LinearBaseColor) ||
                math.any(input.LinearBaseColor.xyz < 0f) ||
                input.LinearBaseColor.w < 0f ||
                input.LinearBaseColor.w > 1f)
            {
                error = "Linear base color must be finite, nonnegative, and have alpha in [0,1].";
                return false;
            }

            if (!Enum.IsDefined(typeof(OperationMapRenderPolicyBucket), input.PolicyBucket))
            {
                error = $"Unknown render-policy bucket: {(byte)input.PolicyBucket}.";
                return false;
            }

            const OperationMapRenderShadowFlags knownShadowFlags =
                OperationMapRenderShadowFlags.CastShadows |
                OperationMapRenderShadowFlags.ReceiveShadows |
                OperationMapRenderShadowFlags.StaticShadowCaster;
            if ((input.ShadowFlags & ~knownShadowFlags) != 0)
            {
                error = $"Unknown render shadow flags: {(byte)input.ShadowFlags}.";
                return false;
            }

            const OperationMapRenderLodFlags knownLodFlags =
                OperationMapRenderLodFlags.Lod0 |
                OperationMapRenderLodFlags.Lod1 |
                OperationMapRenderLodFlags.Lod2;
            if (input.LodFlags == OperationMapRenderLodFlags.None ||
                (input.LodFlags & ~knownLodFlags) != 0)
            {
                error = $"Invalid render LOD flags: {(byte)input.LodFlags}.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsLowerHexGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float4x4 value)
        {
            return IsFinite(value.c0) &&
                   IsFinite(value.c1) &&
                   IsFinite(value.c2) &&
                   IsFinite(value.c3);
        }

        private static bool IsFinite(float4 value)
        {
            return math.all(math.isfinite(value));
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Utf8WithoutBom.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static void Write(BinaryWriter writer, float4x4 value)
        {
            Write(writer, value.c0);
            Write(writer, value.c1);
            Write(writer, value.c2);
            Write(writer, value.c3);
        }

        private static void Write(BinaryWriter writer, float4 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        private static void Write(BinaryWriter writer, float3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }
    }
}
