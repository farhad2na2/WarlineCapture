namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    internal static class Aph700CSharpSourceReferenceScanner
    {
        private const string IdentifierPattern = @"@?[A-Za-z_]\w*";
        private const string QualifiedName = IdentifierPattern + @"(?:\s*\.\s*" + IdentifierPattern + @")*";
        private const string Modifiers =
            @"(?:(?:public|internal|private|protected|abstract|sealed|static|partial|readonly|ref|unsafe|new|file)\s+)*";

        private static readonly Regex NamespaceRegex = new(
            @"\bnamespace\s+(?<name>" + QualifiedName + @")\s*(?:\{|;)",
            RegexOptions.CultureInvariant);

        private static readonly Regex UsingRegex = new(
            @"(?m)^[ \t]*(?:global[ \t]+)?using[ \t]+(?<body>[^;\r\n]+)[ \t]*;",
            RegexOptions.CultureInvariant);

        private static readonly Regex QualifiedDirectiveTargetRegex = new(
            @"^(?:global::)?" + QualifiedName + @"$",
            RegexOptions.CultureInvariant);

        private static readonly Regex TypeRegex = new(
            @"(?m)(?:^[ \t]*|[;{}][ \t\r\n]*)" +
            @"(?<modifiers>" + Modifiers + @")" +
            @"(?<kind>class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+" +
            @"(?<name>" + IdentifierPattern + @")",
            RegexOptions.CultureInvariant);

        private static readonly Regex DelegateRegex = new(
            @"(?m)(?:^[ \t]*|[;{}][ \t\r\n]*)" +
            @"(?<modifiers>" + Modifiers + @")delegate\s+" +
            @"[^;{}()=]+?\s+(?<name>" + IdentifierPattern + @")\s*(?:<[^;{}()]*>)?\s*\(",
            RegexOptions.CultureInvariant);

        private static readonly Regex SyntaxTokenRegex = new(
            IdentifierPattern + @"|::|=>|==|!=|<=|>=|\?\?|[.<>()[\]{},;:=?*&]",
            RegexOptions.CultureInvariant);

        private static readonly Regex QualifiedPrefixRegex = new(
            @"(?:(?:global\s*::\s*)?)(?<name>" + QualifiedName + @")\s*\.\s*$",
            RegexOptions.CultureInvariant);

        private static readonly HashSet<string> DirectTypeContextKeywords = new(StringComparer.Ordinal)
        {
            "as",
            "is",
            "new",
            "case",
            "stackalloc"
        };

        private static readonly HashSet<string> ParenthesizedTypeContextKeywords = new(StringComparer.Ordinal)
        {
            "catch",
            "default",
            "sizeof",
            "typeof"
        };

        public static Aph700ReferenceScanResult Scan(
            string projectRoot,
            IReadOnlyList<Aph700AssemblyDefinition> assemblies)
        {
            var files = LoadSourceFiles(projectRoot, assemblies);
            List<Aph700TypeDeclaration> declarations = DiscoverDeclarations(files);
            var declarationCountByAssembly = declarations
                .GroupBy(item => item.AssemblyName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var declarationsBySimpleName = declarations
                .GroupBy(item => item.SimpleName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

            var assemblyByName = assemblies.ToDictionary(item => item.Name, StringComparer.Ordinal);
            var assemblyByGuid = assemblies.Where(item => !string.IsNullOrWhiteSpace(item.Guid))
                .ToDictionary(item => item.Guid, StringComparer.OrdinalIgnoreCase);
            var directTargets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (Aph700AssemblyDefinition assembly in assemblies)
            {
                var targets = new HashSet<string>(StringComparer.Ordinal);
                foreach (string reference in assembly.References)
                {
                    Aph700AssemblyDefinition target = ResolveReference(reference, assemblyByName, assemblyByGuid);
                    if (target != null)
                        targets.Add(target.Name);
                }
                directTargets[assembly.Name] = targets;
            }

            var result = new Aph700ReferenceScanResult(declarationCountByAssembly);
            foreach (Aph700SourceFile file in files)
            {
                HashSet<string> targets = directTargets[file.AssemblyName];
                if (targets.Count > 0)
                    ScanFile(file, targets, declarationsBySimpleName, result);
            }

            return result;
        }

        private static void ScanFile(
            Aph700SourceFile file,
            HashSet<string> directTargets,
            IReadOnlyDictionary<string, List<Aph700TypeDeclaration>> declarationsBySimpleName,
            Aph700ReferenceScanResult result)
        {
            List<Match> usingMatches = UsingRegex.Matches(file.Sanitized).Cast<Match>()
                .Where(IsUsingDirective)
                .ToList();
            List<Match> namespaceMatches = NamespaceRegex.Matches(file.Sanitized).Cast<Match>().ToList();
            List<Match> declarationMatches = TypeRegex.Matches(file.Sanitized).Cast<Match>()
                .Concat(DelegateRegex.Matches(file.Sanitized).Cast<Match>())
                .OrderBy(item => item.Index)
                .ToList();
            var importedNamespaces = new HashSet<string>(StringComparer.Ordinal);
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
            var staticImports = new List<string>();

            foreach (Match usingMatch in usingMatches)
            {
                string body = NormalizeQualifiedName(usingMatch.Groups["body"].Value.Trim());
                if (body.StartsWith("static ", StringComparison.Ordinal))
                {
                    staticImports.Add(body.Substring("static ".Length).Trim());
                    continue;
                }

                int equalsIndex = body.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    string alias = body.Substring(0, equalsIndex).Trim().TrimStart('@');
                    string target = body.Substring(equalsIndex + 1).Trim();
                    aliases[alias] = target;
                }
                else
                {
                    importedNamespaces.Add(body);
                }
            }

            var sourceNamespaces = new HashSet<string>(namespaceMatches
                .Select(item => NormalizeQualifiedName(item.Groups["name"].Value)), StringComparer.Ordinal);
            char[] scanBuffer = file.Sanitized.ToCharArray();
            foreach (Match match in usingMatches)
                Blank(scanBuffer, match.Index, match.Length);
            foreach (Match match in namespaceMatches)
                Blank(scanBuffer, match.Groups["name"].Index, match.Groups["name"].Length);
            foreach (Match match in declarationMatches)
                Blank(scanBuffer, match.Groups["name"].Index, match.Groups["name"].Length);
            string scanText = new(scanBuffer);
            List<SyntaxToken> tokens = Tokenize(scanText);
            var knownTypeNames = new HashSet<string>(declarationsBySimpleName.Keys, StringComparer.Ordinal);

            foreach (string staticImport in staticImports.OrderBy(item => item, StringComparer.Ordinal))
            {
                Aph700TypeDeclaration target = ResolveFullName(
                    staticImport,
                    directTargets,
                    declarationsBySimpleName);
                if (target != null)
                    result.AddReference(file.AssemblyName, target, file.Path);
            }

            foreach (KeyValuePair<string, string> alias in aliases.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                Aph700TypeDeclaration target = ResolveFullName(
                    alias.Value,
                    directTargets,
                    declarationsBySimpleName);
                if (target == null)
                    continue;

                // The alias declaration itself is one explicit type reference.
                result.AddReference(file.AssemblyName, target, file.Path);
                for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
                {
                    if (string.Equals(tokens[tokenIndex].Identifier, alias.Key, StringComparison.Ordinal) &&
                        GetExplicitTypeContext(
                            tokens,
                            tokenIndex,
                            knownTypeNames,
                            aliases) != TypeContextKind.None)
                    {
                        result.AddReference(file.AssemblyName, target, file.Path);
                    }
                }
            }

            for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
            {
                SyntaxToken token = tokens[tokenIndex];
                string simpleName = token.Identifier;
                if (simpleName == null || aliases.ContainsKey(simpleName))
                {
                    continue;
                }

                TypeContextKind context = GetExplicitTypeContext(
                    tokens,
                    tokenIndex,
                    knownTypeNames,
                    aliases);
                if (context == TypeContextKind.None)
                    continue;

                var allCandidates = new List<Aph700TypeDeclaration>();
                if (declarationsBySimpleName.TryGetValue(
                        simpleName,
                        out List<Aph700TypeDeclaration> exactCandidates))
                {
                    allCandidates.AddRange(exactCandidates);
                }
                if ((context & TypeContextKind.Attribute) != 0 &&
                    !simpleName.EndsWith("Attribute", StringComparison.Ordinal) &&
                    declarationsBySimpleName.TryGetValue(
                        simpleName + "Attribute",
                        out List<Aph700TypeDeclaration> suffixedCandidates))
                {
                    allCandidates.AddRange(suffixedCandidates);
                }
                if (allCandidates.Count == 0)
                    continue;

                List<Aph700TypeDeclaration> candidates = allCandidates
                    .Where(item => directTargets.Contains(item.AssemblyName))
                    .Distinct()
                    .ToList();
                if (candidates.Count == 0)
                    continue;

                Aph700TypeDeclaration resolved = ResolveToken(
                    scanText,
                    token,
                    candidates,
                    importedNamespaces,
                    sourceNamespaces,
                    aliases);
                if (resolved == null)
                {
                    result.AmbiguousTypeTokenOccurrenceCount++;
                    continue;
                }

                result.AddReference(file.AssemblyName, resolved, file.Path);
            }
        }

        private static TypeContextKind GetExplicitTypeContext(
            IReadOnlyList<SyntaxToken> tokens,
            int tokenIndex,
            HashSet<string> knownTypeNames,
            IReadOnlyDictionary<string, string> aliases)
        {
            int qualifiedStart = GetQualifiedStart(tokens, tokenIndex);
            if (TryParseTypeExpression(
                    tokens,
                    qualifiedStart,
                    knownTypeNames,
                    aliases,
                    out int typeEnd,
                    out int terminalIdentifier) &&
                terminalIdentifier == tokenIndex)
            {
                bool attribute = IsAttributeType(tokens, qualifiedStart, typeEnd);
                if (IsAnchoredTypeExpression(tokens, qualifiedStart, typeEnd, attribute))
                    return attribute ? TypeContextKind.Attribute : TypeContextKind.Type;
            }

            return IsGenericTypeArgument(tokens, tokenIndex, knownTypeNames, aliases)
                ? TypeContextKind.Type
                : TypeContextKind.None;
        }

        private static bool IsAnchoredTypeExpression(
            IReadOnlyList<SyntaxToken> tokens,
            int typeStart,
            int typeEnd,
            bool attribute)
        {
            string previous = TokenText(tokens, typeStart - 1);
            if (DirectTypeContextKeywords.Contains(previous) ||
                (previous == "(" &&
                 ParenthesizedTypeContextKeywords.Contains(TokenText(tokens, typeStart - 2))))
            {
                return true;
            }

            return LooksLikeDeclaration(tokens, typeEnd) ||
                   attribute ||
                   IsBaseOrConstraintType(tokens, typeStart) ||
                   IsCastType(tokens, typeStart, typeEnd) ||
                   TokenText(tokens, typeEnd + 1) == "(";
        }

        private static bool LooksLikeDeclaration(IReadOnlyList<SyntaxToken> tokens, int typeEnd)
        {
            int nameIndex = typeEnd + 1;
            if (!IsIdentifierToken(tokens, nameIndex))
                return false;

            int afterName = nameIndex + 1;
            string next = TokenText(tokens, afterName);
            if (next == "<")
            {
                int genericEnd = FindMatching(tokens, afterName, "<", ">");
                if (genericEnd < 0)
                    return false;
                afterName = genericEnd + 1;
                next = TokenText(tokens, afterName);
            }

            return next is ";" or "=" or "," or ")" or "(" or "{" or "=>" or "[" or ":";
        }

        private static bool IsAttributeType(
            IReadOnlyList<SyntaxToken> tokens,
            int qualifiedStart,
            int typeEnd)
        {
            int openBracket = -1;
            for (int index = qualifiedStart - 1; index >= 0; index--)
            {
                string text = tokens[index].Text;
                if (text == "]" || text == ";" || text == "{" || text == "}")
                    break;
                if (text == "[")
                {
                    openBracket = index;
                    break;
                }
            }

            if (openBracket < 0)
                return false;

            string beforeBracket = TokenText(tokens, openBracket - 1);
            if (beforeBracket != null &&
                beforeBracket is not "{" and not "}" and not ";" and not "]" and not "(" and not ",")
            {
                return false;
            }

            int closeBracket = FindMatching(tokens, openBracket, "[", "]");
            if (closeBracket <= typeEnd)
                return false;

            string beforeType = TokenText(tokens, qualifiedStart - 1);
            string afterType = TokenText(tokens, typeEnd + 1);
            return (beforeType == "[" || beforeType == ",") &&
                   (afterType == "]" || afterType == "(" || afterType == ",");
        }

        private static bool IsBaseOrConstraintType(IReadOnlyList<SyntaxToken> tokens, int qualifiedStart)
        {
            int separatorIndex = qualifiedStart - 1;
            string separator = TokenText(tokens, separatorIndex);
            if (separator != ":" && separator != ",")
                return false;

            for (int index = separatorIndex - 1; index >= 0; index--)
            {
                string text = tokens[index].Text;
                if (text is ";" or "{" or "}" or "=")
                    return false;
                if (text is "class" or "struct" or "interface" or "record" or "where")
                    return true;
            }

            return false;
        }

        private static bool IsCastType(
            IReadOnlyList<SyntaxToken> tokens,
            int typeStart,
            int typeEnd)
        {
            if (TokenText(tokens, typeStart - 1) != "(" || TokenText(tokens, typeEnd + 1) != ")")
                return false;

            string beforeOpen = TokenText(tokens, typeStart - 2);
            if (beforeOpen != null &&
                beforeOpen is not "=" and not "return" and not "=>" and not "(" and not "," and
                    not "{" and not ";" and not ":")
            {
                return false;
            }

            int followerIndex = typeEnd + 2;
            return IsIdentifierToken(tokens, followerIndex) ||
                   TokenText(tokens, followerIndex) is "new" or "this" or "base" or "(";
        }

        private static bool IsGenericTypeArgument(
            IReadOnlyList<SyntaxToken> tokens,
            int tokenIndex,
            HashSet<string> knownTypeNames,
            IReadOnlyDictionary<string, string> aliases)
        {
            int searchStart = Math.Max(0, tokenIndex - 96);
            for (int typeStart = searchStart; typeStart <= tokenIndex; typeStart++)
            {
                if (!IsIdentifierToken(tokens, typeStart) ||
                    !TryParseTypeExpression(
                        tokens,
                        typeStart,
                        knownTypeNames,
                        aliases,
                        out int typeEnd,
                        out int _unusedTerminal) ||
                    typeEnd < tokenIndex ||
                    !HasGenericEnvelope(tokens, typeStart, tokenIndex, typeEnd))
                {
                    continue;
                }

                bool attribute = IsAttributeType(tokens, typeStart, typeEnd);
                if (IsAnchoredTypeExpression(tokens, typeStart, typeEnd, attribute))
                    return true;
            }

            return false;
        }

        private static bool HasGenericEnvelope(
            IReadOnlyList<SyntaxToken> tokens,
            int typeStart,
            int tokenIndex,
            int typeEnd)
        {
            bool openBeforeToken = false;
            int depth = 0;
            for (int index = typeStart; index <= typeEnd; index++)
            {
                string text = TokenText(tokens, index);
                if (text == "<")
                {
                    depth++;
                    if (index < tokenIndex)
                        openBeforeToken = true;
                }
                else if (text == ">")
                {
                    if (index > tokenIndex && depth > 0 && openBeforeToken)
                        return true;
                    depth--;
                }
            }
            return false;
        }

        private static bool TryParseTypeExpression(
            IReadOnlyList<SyntaxToken> tokens,
            int start,
            HashSet<string> knownTypeNames,
            IReadOnlyDictionary<string, string> aliases,
            out int end,
            out int terminalIdentifier)
        {
            end = -1;
            terminalIdentifier = -1;
            int cursor = start;
            if (TokenText(tokens, cursor) == "global" && TokenText(tokens, cursor + 1) == "::")
                cursor += 2;

            if (!IsIdentifierToken(tokens, cursor) ||
                !IsPlausibleTypeHead(tokens[cursor].Identifier, knownTypeNames, aliases))
            {
                return false;
            }

            terminalIdentifier = cursor++;
            if (TokenText(tokens, cursor) == "<" &&
                !TryParseGenericArguments(tokens, ref cursor, knownTypeNames, aliases))
            {
                return false;
            }

            while (TokenText(tokens, cursor) == ".")
            {
                if (!IsIdentifierToken(tokens, cursor + 1))
                    return false;
                terminalIdentifier = cursor + 1;
                cursor += 2;
                if (TokenText(tokens, cursor) == "<" &&
                    !TryParseGenericArguments(tokens, ref cursor, knownTypeNames, aliases))
                {
                    return false;
                }
            }

            bool consumedSuffix;
            do
            {
                consumedSuffix = false;
                string next = TokenText(tokens, cursor);
                if (next is "?" or "*")
                {
                    cursor++;
                    consumedSuffix = true;
                }
                else if (next == "[")
                {
                    int arrayCursor = cursor + 1;
                    while (TokenText(tokens, arrayCursor) == ",")
                        arrayCursor++;
                    if (TokenText(tokens, arrayCursor) != "]")
                        return false;
                    cursor = arrayCursor + 1;
                    consumedSuffix = true;
                }
            }
            while (consumedSuffix);

            end = cursor - 1;
            return true;
        }

        private static bool TryParseGenericArguments(
            IReadOnlyList<SyntaxToken> tokens,
            ref int cursor,
            HashSet<string> knownTypeNames,
            IReadOnlyDictionary<string, string> aliases)
        {
            if (TokenText(tokens, cursor) != "<")
                return false;
            cursor++;
            while (true)
            {
                if (!TryParseTypeExpression(
                        tokens,
                        cursor,
                        knownTypeNames,
                        aliases,
                        out int argumentEnd,
                        out int _unusedTerminal))
                    return false;
                cursor = argumentEnd + 1;
                if (TokenText(tokens, cursor) == ",")
                {
                    cursor++;
                    continue;
                }
                if (TokenText(tokens, cursor) != ">")
                    return false;
                cursor++;
                return true;
            }
        }

        private static bool IsPlausibleTypeHead(
            string identifier,
            HashSet<string> knownTypeNames,
            IReadOnlyDictionary<string, string> aliases)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;
            if (knownTypeNames.Contains(identifier) || aliases.ContainsKey(identifier))
                return true;
            if (identifier is "bool" or "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or
                "long" or "ulong" or "nint" or "nuint" or "char" or "float" or "double" or "decimal" or
                "string" or "object" or "dynamic" or "var" or "void")
            {
                return true;
            }
            return char.IsUpper(identifier[0]);
        }

        private static int GetQualifiedStart(IReadOnlyList<SyntaxToken> tokens, int tokenIndex)
        {
            int start = tokenIndex;
            while (start >= 2 && TokenText(tokens, start - 1) == "." && IsIdentifierToken(tokens, start - 2))
                start -= 2;
            if (start >= 2 && TokenText(tokens, start - 1) == "::" && TokenText(tokens, start - 2) == "global")
                start -= 2;
            return start;
        }

        private static int FindMatching(
            IReadOnlyList<SyntaxToken> tokens,
            int openIndex,
            string open,
            string close)
        {
            int depth = 0;
            for (int index = openIndex; index < tokens.Count; index++)
            {
                string text = tokens[index].Text;
                if (text == open)
                    depth++;
                else if (text == close && --depth == 0)
                    return index;
            }
            return -1;
        }

        private static Aph700TypeDeclaration ResolveToken(
            string source,
            SyntaxToken token,
            IReadOnlyList<Aph700TypeDeclaration> candidates,
            HashSet<string> importedNamespaces,
            HashSet<string> sourceNamespaces,
            IReadOnlyDictionary<string, string> aliases)
        {
            string prefix = GetQualifiedPrefix(source, token.Index);
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                int separatorIndex = prefix.IndexOf('.');
                string firstSegment = separatorIndex >= 0 ? prefix.Substring(0, separatorIndex) : prefix;
                if (aliases.TryGetValue(firstSegment, out string aliasTarget))
                {
                    prefix = separatorIndex >= 0
                        ? aliasTarget + prefix.Substring(separatorIndex)
                        : aliasTarget;
                }

                List<Aph700TypeDeclaration> exact = candidates
                    .Where(item => string.Equals(
                        item.FullName,
                        prefix + "." + item.SimpleName,
                        StringComparison.Ordinal))
                    .ToList();
                if (exact.Count == 1)
                    return exact[0];

                List<Aph700TypeDeclaration> importedQualified = candidates.Where(item =>
                        importedNamespaces.Any(imported => string.Equals(
                            item.FullName,
                            imported + "." + prefix + "." + item.SimpleName,
                            StringComparison.Ordinal)))
                    .ToList();
                if (importedQualified.Count == 1)
                    return importedQualified[0];

                List<Aph700TypeDeclaration> sameNamespaceQualified = candidates.Where(item =>
                        sourceNamespaces.Any(sourceNamespace => string.Equals(
                            item.FullName,
                            sourceNamespace + "." + prefix + "." + item.SimpleName,
                            StringComparison.Ordinal)))
                    .ToList();
                return sameNamespaceQualified.Count == 1 ? sameNamespaceQualified[0] : null;
            }

            List<Aph700TypeDeclaration> imported = candidates
                .Where(item => importedNamespaces.Contains(item.Namespace))
                .ToList();
            if (imported.Count == 1)
                return imported[0];

            List<Aph700TypeDeclaration> sameNamespace = candidates
                .Where(item => sourceNamespaces.Contains(item.Namespace))
                .ToList();
            if (sameNamespace.Count == 1)
                return sameNamespace[0];

            List<Aph700TypeDeclaration> global = candidates
                .Where(item => string.IsNullOrEmpty(item.Namespace))
                .ToList();
            return global.Count == 1 ? global[0] : null;
        }

        private static Aph700TypeDeclaration ResolveFullName(
            string fullName,
            HashSet<string> directTargets,
            IReadOnlyDictionary<string, List<Aph700TypeDeclaration>> declarationsBySimpleName)
        {
            string normalized = NormalizeQualifiedName(fullName);
            int separatorIndex = normalized.LastIndexOf('.');
            string simpleName = separatorIndex >= 0 ? normalized.Substring(separatorIndex + 1) : normalized;
            if (!declarationsBySimpleName.TryGetValue(simpleName, out List<Aph700TypeDeclaration> candidates))
                return null;

            List<Aph700TypeDeclaration> matches = candidates.Where(item =>
                    directTargets.Contains(item.AssemblyName) &&
                    string.Equals(item.FullName, normalized, StringComparison.Ordinal))
                .ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static List<Aph700SourceFile> LoadSourceFiles(
            string projectRoot,
            IReadOnlyList<Aph700AssemblyDefinition> assemblies)
        {
            var files = new List<Aph700SourceFile>();
            foreach (Aph700AssemblyDefinition assembly in assemblies)
            {
                foreach (string path in assembly.SourceFiles.OrderBy(item => item, StringComparer.Ordinal))
                {
                    string content = File.ReadAllText(Path.Combine(projectRoot, path));
                    files.Add(new Aph700SourceFile
                    {
                        AssemblyName = assembly.Name,
                        Path = path,
                        Sanitized = Sanitize(content)
                    });
                }
            }
            return files;
        }

        private static List<Aph700TypeDeclaration> DiscoverDeclarations(IReadOnlyList<Aph700SourceFile> files)
        {
            var declarations = new Dictionary<string, Aph700TypeDeclaration>(StringComparer.Ordinal);
            foreach (Aph700SourceFile file in files)
            {
                List<Match> namespaceMatches = NamespaceRegex.Matches(file.Sanitized).Cast<Match>().ToList();
                List<Match> matches = TypeRegex.Matches(file.Sanitized).Cast<Match>()
                    .Concat(DelegateRegex.Matches(file.Sanitized).Cast<Match>())
                    .OrderBy(item => item.Groups["name"].Index)
                    .ToList();
                var candidates = matches.Select(match => CreateDeclarationCandidate(file.Sanitized, match)).ToList();
                foreach (DeclarationCandidate candidate in candidates)
                {
                    candidate.Parent = candidates
                        .Where(parent => parent.BodyStart >= 0 &&
                                         parent.BodyStart < candidate.NameIndex &&
                                         parent.BodyEnd > candidate.NameIndex)
                        .OrderBy(parent => parent.BodyEnd - parent.BodyStart)
                        .FirstOrDefault();
                }

                foreach (DeclarationCandidate candidate in candidates)
                {
                    bool nested = candidate.Parent != null;
                    if ((nested && !ContainsModifier(candidate.Modifiers, "public")) ||
                        (!nested && (ContainsModifier(candidate.Modifiers, "private") ||
                                     ContainsModifier(candidate.Modifiers, "protected"))))
                    {
                        continue;
                    }

                    string declarationNamespace = namespaceMatches
                        .Where(item => item.Index < candidate.NameIndex)
                        .Select(item => NormalizeQualifiedName(item.Groups["name"].Value))
                        .LastOrDefault() ?? string.Empty;
                    string typePath = BuildContainingTypePath(candidate);
                    string fullName = string.IsNullOrEmpty(declarationNamespace)
                        ? typePath
                        : declarationNamespace + "." + typePath;
                    string key = file.AssemblyName + "\0" + fullName;
                    if (!declarations.TryGetValue(key, out Aph700TypeDeclaration declaration))
                    {
                        declaration = new Aph700TypeDeclaration
                        {
                            AssemblyName = file.AssemblyName,
                            SimpleName = candidate.SimpleName,
                            Namespace = declarationNamespace,
                            FullName = fullName
                        };
                        declarations.Add(key, declaration);
                    }
                    declaration.DeclarationFiles.Add(file.Path);
                }
            }

            return declarations.Values
                .OrderBy(item => item.AssemblyName, StringComparer.Ordinal)
                .ThenBy(item => item.FullName, StringComparer.Ordinal)
                .ToList();
        }

        private static DeclarationCandidate CreateDeclarationCandidate(string source, Match match)
        {
            int bodyStart = FindDeclarationBodyStart(source, match.Index + match.Length);
            return new DeclarationCandidate
            {
                SimpleName = match.Groups["name"].Value.TrimStart('@'),
                NameIndex = match.Groups["name"].Index,
                Modifiers = match.Groups["modifiers"].Value,
                BodyStart = bodyStart,
                BodyEnd = bodyStart >= 0 ? FindMatchingBrace(source, bodyStart) : -1
            };
        }

        private static int FindDeclarationBodyStart(string source, int start)
        {
            int parentheses = 0;
            for (int index = start; index < source.Length; index++)
            {
                char current = source[index];
                if (current == '(')
                    parentheses++;
                else if (current == ')')
                    parentheses = Math.Max(0, parentheses - 1);
                else if (parentheses == 0 && current == '{')
                    return index;
                else if (parentheses == 0 && current == ';')
                    return -1;
            }
            return -1;
        }

        private static int FindMatchingBrace(string source, int openBrace)
        {
            int depth = 0;
            for (int index = openBrace; index < source.Length; index++)
            {
                if (source[index] == '{')
                    depth++;
                else if (source[index] == '}' && --depth == 0)
                    return index;
            }
            return source.Length;
        }

        private static string BuildContainingTypePath(DeclarationCandidate candidate)
        {
            var names = new Stack<string>();
            for (DeclarationCandidate current = candidate; current != null; current = current.Parent)
                names.Push(current.SimpleName);
            return string.Join(".", names);
        }

        private static List<SyntaxToken> Tokenize(string source)
        {
            return SyntaxTokenRegex.Matches(source).Cast<Match>()
                .Select(match => new SyntaxToken(match.Value, match.Index))
                .ToList();
        }

        private static string Sanitize(string source)
        {
            var output = source.ToCharArray();
            ScanState state = ScanState.Code;
            for (int index = 0; index < source.Length; index++)
            {
                char current = source[index];
                char next = index + 1 < source.Length ? source[index + 1] : '\0';
                if (state == ScanState.Code)
                {
                    if (current == '/' && next == '/')
                    {
                        output[index] = output[index + 1] = ' ';
                        index++;
                        state = ScanState.LineComment;
                    }
                    else if (current == '/' && next == '*')
                    {
                        output[index] = output[index + 1] = ' ';
                        index++;
                        state = ScanState.BlockComment;
                    }
                    else if (current == '\'' || current == '"')
                    {
                        bool verbatim = current == '"' && index > 0 && source[index - 1] == '@';
                        output[index] = ' ';
                        state = current == '\'' ? ScanState.Character :
                            verbatim ? ScanState.VerbatimString : ScanState.String;
                    }
                }
                else if (state == ScanState.LineComment)
                {
                    if (current == '\n')
                        state = ScanState.Code;
                    else
                        output[index] = ' ';
                }
                else if (state == ScanState.BlockComment)
                {
                    output[index] = current == '\n' ? '\n' : ' ';
                    if (current == '*' && next == '/')
                    {
                        output[index + 1] = ' ';
                        index++;
                        state = ScanState.Code;
                    }
                }
                else if (state == ScanState.VerbatimString)
                {
                    output[index] = current == '\n' ? '\n' : ' ';
                    if (current == '"' && next == '"')
                    {
                        output[index + 1] = ' ';
                        index++;
                    }
                    else if (current == '"')
                    {
                        state = ScanState.Code;
                    }
                }
                else
                {
                    output[index] = current == '\n' ? '\n' : ' ';
                    if (current == '\\' && next != '\0')
                    {
                        output[index + 1] = next == '\n' ? '\n' : ' ';
                        index++;
                    }
                    else if ((state == ScanState.String && current == '"') ||
                             (state == ScanState.Character && current == '\''))
                    {
                        state = ScanState.Code;
                    }
                }
            }
            return new string(output);
        }

        private static string GetQualifiedPrefix(string source, int tokenIndex)
        {
            int start = Math.Max(0, tokenIndex - 256);
            Match match = QualifiedPrefixRegex.Match(source.Substring(start, tokenIndex - start));
            return match.Success ? NormalizeQualifiedName(match.Groups["name"].Value) : null;
        }

        private static string NormalizeQualifiedName(string value)
        {
            string normalized = Regex.Replace(value, @"\s*\.\s*", ".");
            normalized = Regex.Replace(normalized, @"\s*::\s*", "::");
            return normalized.Replace("global::", string.Empty).Trim().TrimStart('@');
        }

        private static bool IsUsingDirective(Match match)
        {
            string body = NormalizeQualifiedName(match.Groups["body"].Value.Trim());
            if (body.StartsWith("static ", StringComparison.Ordinal))
            {
                return QualifiedDirectiveTargetRegex.IsMatch(
                    body.Substring("static ".Length).Trim());
            }

            int equalsIndex = body.IndexOf('=');
            if (equalsIndex < 0)
                return QualifiedDirectiveTargetRegex.IsMatch(body);

            string alias = body.Substring(0, equalsIndex).Trim();
            string target = body.Substring(equalsIndex + 1).Trim();
            return Regex.IsMatch(alias, "^" + IdentifierPattern + "$", RegexOptions.CultureInvariant) &&
                   QualifiedDirectiveTargetRegex.IsMatch(target);
        }

        private static bool ContainsModifier(string modifiers, string expected)
        {
            return Regex.IsMatch(modifiers, @"\b" + Regex.Escape(expected) + @"\b");
        }

        private static string TokenText(IReadOnlyList<SyntaxToken> tokens, int index)
        {
            return index >= 0 && index < tokens.Count ? tokens[index].Text : null;
        }

        private static bool IsIdentifierToken(IReadOnlyList<SyntaxToken> tokens, int index)
        {
            return index >= 0 && index < tokens.Count && tokens[index].Identifier != null;
        }

        private static void Blank(char[] buffer, int index, int length)
        {
            for (int offset = 0; offset < length; offset++)
            {
                int target = index + offset;
                if (buffer[target] != '\n')
                    buffer[target] = ' ';
            }
        }

        private static Aph700AssemblyDefinition ResolveReference(
            string reference,
            IReadOnlyDictionary<string, Aph700AssemblyDefinition> byName,
            IReadOnlyDictionary<string, Aph700AssemblyDefinition> byGuid)
        {
            if (reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
            {
                string guid = reference.Substring("GUID:".Length).Trim();
                return byGuid.TryGetValue(guid, out Aph700AssemblyDefinition target) ? target : null;
            }
            return byName.TryGetValue(reference, out Aph700AssemblyDefinition namedTarget) ? namedTarget : null;
        }

        private sealed class SyntaxToken
        {
            public SyntaxToken(string text, int index)
            {
                Text = text;
                Index = index;
                Identifier = Regex.IsMatch(text, "^" + IdentifierPattern + "$", RegexOptions.CultureInvariant)
                    ? text.TrimStart('@')
                    : null;
            }

            public string Text { get; }
            public int Index { get; }
            public string Identifier { get; }
        }

        private sealed class DeclarationCandidate
        {
            public string SimpleName { get; set; }
            public string Modifiers { get; set; }
            public int NameIndex { get; set; }
            public int BodyStart { get; set; }
            public int BodyEnd { get; set; }
            public DeclarationCandidate Parent { get; set; }
        }

        [Flags]
        private enum TypeContextKind
        {
            None = 0,
            Type = 1,
            Attribute = 2
        }

        private enum ScanState
        {
            Code,
            LineComment,
            BlockComment,
            String,
            VerbatimString,
            Character
        }
    }
}
