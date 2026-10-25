using System;using System.IO;using System.Security.Cryptography;using System.Text;
namespace Game.Simulation
{
    // Versioned config-value bytes only. No Unity objects, serializers, gameplay or ECS.
    public sealed class SkirmishBindingCanonicalBytes : IDisposable
    {
        readonly MemoryStream memory=new MemoryStream(); readonly BinaryWriter writer;
        public SkirmishBindingCanonicalBytes(string domain,byte format)
        {
            if(format!=2)throw new NotSupportedException("Explicit canonical binding format 2 required.");
            writer=new BinaryWriter(memory,Encoding.UTF8,true);writer.Write("SkirmishBindingValues");writer.Write(format);writer.Write(domain??throw new ArgumentNullException(nameof(domain)));
        }
        public void Text(string value){writer.Write(value!=null);if(value!=null)writer.Write(value);}
        public void Bool(bool value)=>writer.Write(value);
        public void Byte(byte value)=>writer.Write(value);
        public void Int(int value)=>writer.Write(value);
        public void UInt(uint value)=>writer.Write(value);
        public void Long(long value)=>writer.Write(value);
        public void Float(float value)=>writer.Write(BitConverter.ToInt32(BitConverter.GetBytes(value),0));
        public void ArrayLength(int length){if(length< -1)throw new ArgumentOutOfRangeException(nameof(length));writer.Write(length);}
        public string Finish(){writer.Flush();using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(memory.ToArray())).Replace("-","").ToLowerInvariant();}
        public void Dispose(){writer.Dispose();memory.Dispose();}
    }
}
