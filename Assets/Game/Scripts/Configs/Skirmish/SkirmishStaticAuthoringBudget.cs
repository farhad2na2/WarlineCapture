using System;
using Game.Skirmish.Contracts;
namespace Game.Configs
{
    // No setup, content/native hash or launch payload is exposed by this DTO.
    public sealed class SkirmishStaticAuthoringBudget
    {
        readonly SkirmishResolvedForceEntry[] forces;readonly SkirmishResolvedStructureEntry[] structures;readonly string[] errors;
        public string CatalogId {get;} public string DefinitionId {get;} public SkirmishSizeId Size {get;}
        public bool NormalCompilerPassed {get;} public bool ExactCanonicalQuantities=>errors.Length==0;
        public SkirmishResolvedForceEntry[] Forces=>(SkirmishResolvedForceEntry[])forces.Clone();
        public SkirmishResolvedStructureEntry[] Structures=>(SkirmishResolvedStructureEntry[])structures.Clone();
        public string[] CanonicalBudgetErrors=>(string[])errors.Clone();
        internal SkirmishStaticAuthoringBudget(string catalog,string definition,SkirmishSizeId size,
            SkirmishResolvedForceEntry[] forceRows,SkirmishResolvedStructureEntry[] structureRows,bool normal,string[] diagnostics)
        {CatalogId=catalog;DefinitionId=definition;Size=size;NormalCompilerPassed=normal;
            forces=(SkirmishResolvedForceEntry[])forceRows.Clone();structures=(SkirmishResolvedStructureEntry[])structureRows.Clone();errors=(string[])diagnostics.Clone();}
    }
}
