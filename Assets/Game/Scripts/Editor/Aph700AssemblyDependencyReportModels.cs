namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class Aph700AssemblyDefinition
    {
        public string Name { get; set; }
        public string AsmdefPath { get; set; }
        public string RootPath { get; set; }
        public string Guid { get; set; }
        public List<string> References { get; set; } = new();
        public List<string> SourceFiles { get; } = new();
    }

    internal sealed class Aph700ReportDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public string TaskId { get; set; } = "APH-700";
        public string Scope { get; set; } =
            "Direct dependencies and source-level cross-domain type references for first-party asmdefs under Assets/Game, Assets/Tests, and Assets/Editor.";
        public string DeterminismContract { get; set; } =
            "Explicit evidence identity, no timestamp, ordinal path ordering, normalized LF output, and a content-derived source fingerprint.";
        public string ExactCommit { get; set; }
        public string EnvironmentIdentitySha256 { get; set; }
        public bool? Dirty { get; set; }
        public string SourceFingerprintSha256 { get; set; }
        public Aph700SummaryRecord Summary { get; set; }
        public List<Aph700AssemblyRecord> Assemblies { get; set; } = new();
        public List<Aph700AssemblyEdgeRecord> FirstPartyEdges { get; set; } = new();
        public List<Aph700ExternalReferenceRecord> ExternalReferences { get; set; } = new();
        public List<Aph700CrossDomainTypeReferenceRecord> TopCrossDomainTypeReferences { get; set; } = new();
        public List<string> Limitations { get; set; } = new();
    }

    internal sealed class Aph700SummaryRecord
    {
        public int AssemblyCount { get; set; }
        public int FirstPartyEdgeCount { get; set; }
        public int ExternalReferenceCount { get; set; }
        public int SourceFileCount { get; set; }
        public int DeclaredTypeCount { get; set; }
        public int ResolvedCrossDomainTypeOccurrenceCount { get; set; }
        public int DistinctCrossDomainTypeReferenceCount { get; set; }
        public int AmbiguousTypeTokenOccurrenceCount { get; set; }
        public int UnownedScopedSourceFileCount { get; set; }
    }

    internal sealed class Aph700AssemblyRecord
    {
        public string Name { get; set; }
        public string AsmdefPath { get; set; }
        public string AsmdefGuid { get; set; }
        public int SourceFileCount { get; set; }
        public int DeclaredTypeCount { get; set; }
        public int FirstPartyDependencyCount { get; set; }
        public int ExternalDependencyCount { get; set; }
    }

    internal sealed class Aph700AssemblyEdgeRecord
    {
        public string SourceAssembly { get; set; }
        public string TargetAssembly { get; set; }
        public string SourceAsmdefPath { get; set; }
        public string TargetAsmdefPath { get; set; }
        public string DeclaredReference { get; set; }
        public int ResolvedTypeOccurrenceCount { get; set; }
        public int DistinctResolvedTypeCount { get; set; }
        public int ReferencingSourceFileCount { get; set; }
        public List<Aph700CrossDomainTypeReferenceRecord> TopTypeReferences { get; set; } = new();
    }

    internal sealed class Aph700ExternalReferenceRecord
    {
        public string SourceAssembly { get; set; }
        public string SourceAsmdefPath { get; set; }
        public string DeclaredReference { get; set; }
        public string ReferenceKind { get; set; }
    }

    internal sealed class Aph700CrossDomainTypeReferenceRecord
    {
        public string SourceAssembly { get; set; }
        public string TargetAssembly { get; set; }
        public string FullTypeName { get; set; }
        public string TypeName { get; set; }
        public int OccurrenceCount { get; set; }
        public int SourceFileCount { get; set; }
        public List<string> SourceFiles { get; set; } = new();
    }

    internal sealed class Aph700SourceFile
    {
        public string AssemblyName { get; set; }
        public string Path { get; set; }
        public string Sanitized { get; set; }
    }

    internal sealed class Aph700TypeDeclaration
    {
        public string AssemblyName { get; set; }
        public string SimpleName { get; set; }
        public string Namespace { get; set; }
        public string FullName { get; set; }
        public SortedSet<string> DeclarationFiles { get; } = new(StringComparer.Ordinal);
    }

    internal sealed class Aph700TypeReferenceSummary
    {
        public string SourceAssembly { get; set; }
        public string TargetAssembly { get; set; }
        public string FullTypeName { get; set; }
        public string TypeName { get; set; }
        public int OccurrenceCount { get; set; }
        public SortedSet<string> SourceFiles { get; } = new(StringComparer.Ordinal);

        public Aph700CrossDomainTypeReferenceRecord ToRecord(string sourceAssembly, string targetAssembly)
        {
            return new Aph700CrossDomainTypeReferenceRecord
            {
                SourceAssembly = sourceAssembly,
                TargetAssembly = targetAssembly,
                FullTypeName = FullTypeName,
                TypeName = TypeName,
                OccurrenceCount = OccurrenceCount,
                SourceFileCount = SourceFiles.Count,
                SourceFiles = SourceFiles.Take(10).ToList()
            };
        }
    }

    internal sealed class Aph700EdgeReferenceSummary
    {
        public int OccurrenceCount => TypeReferences.Sum(item => item.OccurrenceCount);
        public List<Aph700TypeReferenceSummary> TypeReferences { get; } = new();
        public SortedSet<string> SourceFiles { get; } = new(StringComparer.Ordinal);
    }

    internal sealed class Aph700ReferenceScanResult
    {
        private readonly IReadOnlyDictionary<string, int> _declarationCountByAssembly;
        private readonly Dictionary<string, Aph700TypeReferenceSummary> _typeReferences =
            new(StringComparer.Ordinal);

        public Aph700ReferenceScanResult(IReadOnlyDictionary<string, int> declarationCountByAssembly)
        {
            _declarationCountByAssembly = declarationCountByAssembly;
        }

        public int AmbiguousTypeTokenOccurrenceCount { get; set; }
        public int UnownedScopedSourceFileCount { get; set; }
        public List<Aph700TypeReferenceSummary> AllTypeReferences => _typeReferences.Values.ToList();

        public int GetDeclaredTypeCount(string assemblyName)
        {
            return _declarationCountByAssembly.TryGetValue(assemblyName, out int count) ? count : 0;
        }

        public void AddReference(string sourceAssembly, Aph700TypeDeclaration target, string sourceFile)
        {
            string key = sourceAssembly + "\0" + target.AssemblyName + "\0" + target.FullName;
            if (!_typeReferences.TryGetValue(key, out Aph700TypeReferenceSummary summary))
            {
                summary = new Aph700TypeReferenceSummary
                {
                    SourceAssembly = sourceAssembly,
                    TargetAssembly = target.AssemblyName,
                    FullTypeName = target.FullName,
                    TypeName = target.SimpleName
                };
                _typeReferences.Add(key, summary);
            }
            summary.OccurrenceCount++;
            summary.SourceFiles.Add(sourceFile);
        }

        public Aph700EdgeReferenceSummary GetEdge(string sourceAssembly, string targetAssembly)
        {
            var result = new Aph700EdgeReferenceSummary();
            foreach (Aph700TypeReferenceSummary reference in _typeReferences.Values.Where(item =>
                         string.Equals(item.SourceAssembly, sourceAssembly, StringComparison.Ordinal) &&
                         string.Equals(item.TargetAssembly, targetAssembly, StringComparison.Ordinal)))
            {
                result.TypeReferences.Add(reference);
                result.SourceFiles.UnionWith(reference.SourceFiles);
            }
            return result;
        }
    }
}
