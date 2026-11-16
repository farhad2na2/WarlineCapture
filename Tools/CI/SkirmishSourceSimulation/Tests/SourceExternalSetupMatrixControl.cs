using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Runtime;
using Game.Skirmish.Contracts;

internal static class SourceExternalSetupMatrixControl
{
    static string Hash(byte[] b) { using var h=SHA256.Create(); return BitConverter.ToString(h.ComputeHash(b)).Replace("-", "").ToLowerInvariant(); }
    static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    static void Reject(Action action, string message)
    {
        try { action(); } catch (InvalidOperationException) { return; } catch (DecoderFallbackException) { return; }
        throw new Exception(message);
    }
    static void Main(string[] args)
    {
        var bytes=File.ReadAllBytes(args[0]); string hash=Hash(bytes);
        var rows=SourceExternalSetupMatrix.ReadVerified(bytes,hash);
        Need(rows.Count==360,"all authored rows");
        foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
            for(int i=1;i<=120;i++) {
                string id="S"+i.ToString("D3");
                var row=SourceExternalSetupMatrix.Select(rows,id,size);
                Need(row.CatalogId==id && row.SizeId==size,"exact selected row");
            }
        Reject(()=>SourceExternalSetupMatrix.ReadVerified(bytes,new string('0',64)),"wrong pin");
        Reject(()=>SourceExternalSetupMatrix.ReadVerified(bytes,hash.ToUpperInvariant()),"noncanonical pin");
        Reject(()=>SourceExternalSetupMatrix.Select(rows,"S121",SkirmishSizeId.Standard),"unknown mission");
        Reject(()=>SourceExternalSetupMatrix.Select(rows,"S001",unchecked((SkirmishSizeId)255)),"unknown size");
        var lines=Encoding.UTF8.GetString(bytes).Split('\n');
        int last=lines.Length-1; while(last>0 && string.IsNullOrWhiteSpace(lines[last])) last--;
        string lastRow=lines[last]; lines[last]="";
        var missing=Encoding.UTF8.GetBytes(string.Join("\n",lines));
        Reject(()=>SourceExternalSetupMatrix.ReadVerified(missing,Hash(missing)),"missing identity");
        lines[last]=lastRow;
        lines[last]=lines[1];
        var duplicate=Encoding.UTF8.GetBytes(string.Join("\n",lines));
        Reject(()=>SourceExternalSetupMatrix.ReadVerified(duplicate,Hash(duplicate)),"duplicate identity");
        var invalid=new byte[]{0xff};
        Reject(()=>SourceExternalSetupMatrix.ReadVerified(invalid,Hash(invalid)),"invalid UTF-8");
        Console.WriteLine("[SourceExternalSetupMatrixControl] result=Passed missions=120 sizes=3 wrongPin=Rejected noncanonicalPin=Rejected unknown=Rejected missing=Rejected duplicate=Rejected invalidUtf8=Rejected world=0 admission=0 readiness=0");
    }
}
