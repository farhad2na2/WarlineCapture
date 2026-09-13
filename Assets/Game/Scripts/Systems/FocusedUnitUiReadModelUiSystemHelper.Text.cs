using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed partial class FocusedUnitUiReadModelUiSystemHelper
    {
        private static FixedString32Bytes ToFixed32(string value)
        {
            FixedString32Bytes result = default;
            result.Append(Trim(value, 29));
            return result;
        }

        private static FixedString64Bytes ToFixed64(string value)
        {
            FixedString64Bytes result = default;
            result.Append(Trim(value, 61));
            return result;
        }

        private static FixedString128Bytes ToFixed128(string value)
        {
            FixedString128Bytes result = default;
            result.Append(Trim(value, 125));
            return result;
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            int length=value.Length;
            while(length>0 && System.Text.Encoding.UTF8.GetByteCount(value,0,length)>maxLength) length--;
            if(length>0 && char.IsHighSurrogate(value[length-1])) length--;
            return length==value.Length ? value : value.Substring(0,length);
        }
    }
}
