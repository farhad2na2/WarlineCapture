using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Game.Components;

namespace Game.Editor
{
    internal static class OperationMapRenderIdentityProjection
    {
        internal static bool TryProject(
            string stableIdentity,
            out OperationMapRenderIdentity128 identity,
            out string error)
        {
            identity = default;
            if (string.IsNullOrEmpty(stableIdentity))
            {
                error = "Render identity source must be non-empty.";
                return false;
            }

            byte[] sourceBytes = Encoding.UTF8.GetBytes(stableIdentity);
            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
                digest = sha256.ComputeHash(sourceBytes);

            identity = new OperationMapRenderIdentity128
            {
                Low = ReadUInt64LittleEndian(digest, 0),
                High = ReadUInt64LittleEndian(digest, sizeof(ulong))
            };
            error = null;
            return true;
        }

        internal static int Compare(
            in OperationMapRenderIdentity128 left,
            in OperationMapRenderIdentity128 right)
        {
            int low = left.Low.CompareTo(right.Low);
            return low != 0 ? low : left.High.CompareTo(right.High);
        }

        private static ulong ReadUInt64LittleEndian(byte[] bytes, int offset)
        {
            ulong value = 0;
            for (int index = 0; index < sizeof(ulong); index++)
                value |= (ulong)bytes[offset + index] << (index * 8);
            return value;
        }
    }

    internal sealed class OperationMapRenderIdentityCollisionDetector
    {
        private readonly Dictionary<OperationMapRenderIdentity128, string> sources =
            new(new IdentityEqualityComparer());

        internal int Count => sources.Count;

        internal bool TryRegister(
            string stableIdentity,
            out OperationMapRenderIdentity128 identity,
            out string error)
        {
            if (!OperationMapRenderIdentityProjection.TryProject(
                    stableIdentity,
                    out identity,
                    out error))
            {
                return false;
            }

            return TryRegister(identity, stableIdentity, out error);
        }

        internal bool TryRegister(
            OperationMapRenderIdentity128 identity,
            string stableIdentity,
            out string error)
        {
            if (string.IsNullOrEmpty(stableIdentity))
            {
                error = "Render identity source must be non-empty.";
                return false;
            }

            if (sources.TryGetValue(identity, out string existing))
            {
                if (string.Equals(existing, stableIdentity, StringComparison.Ordinal))
                {
                    error = null;
                    return true;
                }

                error =
                    $"Render identity collision between '{existing}' and '{stableIdentity}'.";
                return false;
            }

            sources.Add(identity, stableIdentity);
            error = null;
            return true;
        }

        private sealed class IdentityEqualityComparer :
            IEqualityComparer<OperationMapRenderIdentity128>
        {
            public bool Equals(
                OperationMapRenderIdentity128 left,
                OperationMapRenderIdentity128 right)
            {
                return left.Low == right.Low && left.High == right.High;
            }

            public int GetHashCode(OperationMapRenderIdentity128 identity)
            {
                unchecked
                {
                    int low = (int)identity.Low ^ (int)(identity.Low >> 32);
                    int high = (int)identity.High ^ (int)(identity.High >> 32);
                    return (low * 397) ^ high;
                }
            }
        }
    }

    internal sealed class OperationMapRenderIdentityComparer :
        IComparer<OperationMapRenderIdentity128>
    {
        internal static readonly OperationMapRenderIdentityComparer Instance = new();

        public int Compare(
            OperationMapRenderIdentity128 left,
            OperationMapRenderIdentity128 right)
        {
            return OperationMapRenderIdentityProjection.Compare(in left, in right);
        }
    }
}
