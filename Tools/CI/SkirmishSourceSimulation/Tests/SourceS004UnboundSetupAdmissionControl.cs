using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Synthetic negative inputs derived from actual compiler readback. No world is created.
internal static class SourceS004UnboundSetupAdmissionControl
{
    static string Hash(byte[] bytes) { using var h=SHA256.Create(); return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    internal static void Main(string[] args)
    {
        if(args.Length!=4)throw new ArgumentException("original export, export hash, compiler comparison and fresh output directory required");
        var accepted=SourceS004CompiledSetupInput.Read(args[0],args[1]);
        if(!accepted.MeasuredLayoutBound)throw new InvalidDataException("Baseline source setup must be measured");
        var template=JObject.Parse(File.ReadAllText(args[0]));
        var cases=(JArray)JObject.Parse(File.ReadAllText(args[2]))["rows"];
        if(Directory.Exists(args[3]))throw new IOException("Fresh fixture directory required");
        Directory.CreateDirectory(args[3]); int rejected=0;
        // The original setup hash does not cover measured-layout state. Keep all
        // other fields and the setup hash unchanged while independently pinning
        // these test bytes, so hash checking alone cannot satisfy this control.
        var sameIdentity=(JObject)template.DeepClone();
        ((JObject)sameIdentity["compiled_setup"])["MeasuredLayoutBound"]=false;
        byte[] changedBytes=Encoding.UTF8.GetBytes(sameIdentity.ToString(Formatting.None));
        string sameIdentityFile=Path.Combine(args[3],"same-identity-unbound.json");
        File.WriteAllBytes(sameIdentityFile,changedBytes);
        bool sameIdentityRejected=false;
        try { SourceS004CompiledSetupInput.Read(sameIdentityFile,Hash(changedBytes)); }
        catch(InvalidDataException) { sameIdentityRejected=true; }
        if(!sameIdentityRejected)throw new InvalidOperationException("Unbound setup retained its historical hash and was accepted");
        int badDimensionsRejected=0;
        foreach(string field in new[]{"WorldWidthMetres","WorldDepthMetres","CellSize"})
        foreach(int value in new[]{0,-1})
        {
            var badDimension=(JObject)template.DeepClone();
            ((JObject)badDimension["compiled_setup"])[field]=value;
            byte[] badBytes=Encoding.UTF8.GetBytes(badDimension.ToString(Formatting.None));
            string badFile=Path.Combine(args[3],field+"-"+value+".json");
            File.WriteAllBytes(badFile,badBytes);
            try { SourceS004CompiledSetupInput.Read(badFile,Hash(badBytes)); }
            catch(InvalidDataException) { badDimensionsRejected++; continue; }
            throw new InvalidOperationException("Invalid dimension accepted: "+badFile);
        }
        if(badDimensionsRejected!=6)throw new InvalidDataException("All invalid dimension cases must be rejected");
        foreach(JObject row in cases)
        {
            var payload=(JObject)row["compiled_setup"];
            if((bool)payload["MeasuredLayoutBound"])continue;
            var synthetic=(JObject)template.DeepClone(); synthetic["compiled_setup"]=payload.DeepClone();
            byte[] bytes=Encoding.UTF8.GetBytes(synthetic.ToString(Formatting.None));
            string file=Path.Combine(args[3],(string)row["size"]+"-"+(string)row["difficulty"]+".json");
            File.WriteAllBytes(file,bytes);
            try { SourceS004CompiledSetupInput.Read(file,Hash(bytes)); }
            catch(InvalidDataException) { rejected++; continue; }
            throw new InvalidOperationException("Unbound S004 setup accepted: "+file);
        }
        if(rejected!=11)throw new InvalidDataException("Expected eleven unbound setup negatives");
        Console.WriteLine("[SourceS004UnboundSetupAdmissionControl] result=Passed measuredBaseline=Accepted sameHashUnbound=Rejected invalidDimensions=6 unbound=11 rejected=11 world=0 readiness=0");
    }
}
