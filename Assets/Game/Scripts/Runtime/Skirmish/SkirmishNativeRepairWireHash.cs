using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Game.Runtime
{
    internal static class SkirmishNativeRepairWireHash
    {
        internal const string Domain="Warline.NativeCheckpoint.RepairWire.v2";
        internal static string Compute(string nativePayload,string repairPayload)
        {
            if(nativePayload==null||repairPayload==null)throw new ArgumentNullException();
            // Explicit byte lengths cover both raw JSON values without ambiguous
            // concatenation. Domain and lengths use BinaryWriter little endian.
            byte[] domain=Encoding.UTF8.GetBytes(Domain),native=Encoding.UTF8.GetBytes(nativePayload),repair=Encoding.UTF8.GetBytes(repairPayload);
            using var stream=new MemoryStream();
            using(var writer=new BinaryWriter(stream,new UTF8Encoding(false),true))
            {
                writer.Write(domain.Length);writer.Write(domain);
                writer.Write(native.Length);writer.Write(native);
                writer.Write(repair.Length);writer.Write(repair);
            }
            using var sha=SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();
        }
    }
}
