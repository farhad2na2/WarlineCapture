using System;
using Unity.Entities;

// Source-host boundary for the two element types used by the selected owners.
// Inline storage is essential: copying a FixedList must copy its contents.
namespace Unity.Collections
{
    public unsafe struct FixedList128Bytes<T> where T : struct
    {
        private ushort length;
        private fixed byte bytes[126];

        public int Length => length;
        public int Capacity => typeof(T) == typeof(byte) ? 126 :
            typeof(T) == typeof(Entity) ? 15 :
            throw new NotSupportedException("FixedList boundary admits only byte and Entity");

        public T this[int index]
        {
            get
            {
                if ((uint)index >= length) throw new IndexOutOfRangeException();
                fixed (byte* p = bytes)
                {
                    if (typeof(T) == typeof(byte)) return (T)(object)p[index];
                    if (typeof(T) == typeof(Entity))
                    {
                        int offset = index * 8;
                        return (T)(object)new Entity { Index = ReadInt(p + offset), Version = ReadInt(p + offset + 4) };
                    }
                }
                throw new NotSupportedException("FixedList boundary admits only byte and Entity");
            }
        }

        public void Add(T value)
        {
            if (length >= Capacity) throw new InvalidOperationException("FixedList capacity exceeded");
            fixed (byte* p = bytes)
            {
                if (typeof(T) == typeof(byte)) p[length] = (byte)(object)value;
                else if (typeof(T) == typeof(Entity))
                {
                    Entity entity = (Entity)(object)value;
                    int offset = length * 8;
                    WriteInt(p + offset, entity.Index);
                    WriteInt(p + offset + 4, entity.Version);
                }
                else throw new NotSupportedException("FixedList boundary admits only byte and Entity");
            }
            length++;
        }

        private static int ReadInt(byte* p) => p[0] | p[1] << 8 | p[2] << 16 | p[3] << 24;
        private static void WriteInt(byte* p, int value)
        {
            p[0] = (byte)value; p[1] = (byte)(value >> 8);
            p[2] = (byte)(value >> 16); p[3] = (byte)(value >> 24);
        }
    }
}
