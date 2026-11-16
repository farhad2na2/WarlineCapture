using System;
using System.IO;
using Game.Configs;
using Newtonsoft.Json;

namespace Game.Runtime
{
    internal static class SourceS004SetupGuardCacheControls
    {
        internal static void Run(SkirmishResolvedSetup setup,string path,string hash,string directory)
        {
            string fixture=Path.Combine(directory,"setup-cache-control-input.json");
            byte[] bytes=File.ReadAllBytes(path);File.WriteAllBytes(fixture,bytes);
            void Compare(SkirmishResolvedSetup value,bool expectedSuccess)
            {
                Exception baseline=null,cached=null;
                try { SourceS004BaselineSetupGuard.RequireExact(value,fixture,hash); } catch(Exception e) { baseline=e; }
                try { SourceS004CompiledSetupInput.RequireExact(value,fixture,hash); } catch(Exception e) { cached=e; }
                if(expectedSuccess ? baseline!=null || cached!=null : baseline==null || cached==null || baseline.GetType()!=cached.GetType() || baseline.Message!=cached.Message)
                    throw new InvalidOperationException("Cached setup guard differs from original",cached);
            }
            Compare(setup,true);Compare(setup,true);Compare(null,false);
            var changed=JsonConvert.DeserializeObject<SkirmishResolvedSetup>(JsonConvert.SerializeObject(setup));
            var force=changed.Forces[0];force.SupplyCost++;changed.Forces[0]=force;Compare(changed,false);
            changed=JsonConvert.DeserializeObject<SkirmishResolvedSetup>(JsonConvert.SerializeObject(setup));
            changed.TitleKey+=".fixture-mutation";
            if(SkirmishSetupCompiler.ComputeHash(changed,changed.Seed)!=SkirmishSetupCompiler.ComputeHash(setup,setup.Seed))
                throw new InvalidOperationException("Expected a field outside original setup hash");
            Compare(changed,false);
            changed=JsonConvert.DeserializeObject<SkirmishResolvedSetup>(JsonConvert.SerializeObject(setup));
            changed.Seed++;Compare(changed,false);
            File.AppendAllText(fixture," ");Compare(setup,false);
            File.WriteAllBytes(fixture,bytes);Compare(setup,true);
        }
    }
}
