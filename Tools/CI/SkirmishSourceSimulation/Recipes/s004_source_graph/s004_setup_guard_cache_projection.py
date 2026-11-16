"""Opt-in parsed-reference cache; input hash and full live comparison remain per call."""


def apply(arguments, output, root, serialized_candidate=None):
    original = root / 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs'
    source = original.read_text()
    old = '''            var expected = Read(path, expectedSha256);
            if (actual == null || !JToken.DeepEquals(JObject.FromObject(actual), JObject.FromObject(expected)))'''
    new = '''            var expected = CachedExpected(path, expectedSha256);
            if (actual == null || !JToken.DeepEquals(JObject.FromObject(actual), expected))'''
    if source.count(old) != 1:
        raise ValueError('Exact original setup guard required')
    helper = '''
        static readonly object CacheLock=new object();
        static string CachedPath, CachedHash;
        static JObject CachedReference;
        static JObject CachedExpected(string path,string expectedSha256)
        {
            if (!Path.IsPathRooted(path ?? "") || expectedSha256 == null || expectedSha256.Length != 64)
                throw new InvalidDataException("Absolute pinned S004 setup input required");
            byte[] bytes=File.ReadAllBytes(path);
            string digest;
            using(var hash=SHA256.Create()) digest=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
            if(!StringComparer.Ordinal.Equals(digest,expectedSha256)) throw new InvalidDataException("S004 setup input hash changed");
            lock(CacheLock) {
                if(CachedReference==null || !StringComparer.Ordinal.Equals(CachedPath,path) || !StringComparer.Ordinal.Equals(CachedHash,expectedSha256)) {
                    CachedReference=JObject.FromObject(Read(path,expectedSha256));
                    CachedPath=path;CachedHash=expectedSha256;
                }
                return CachedReference;
            }
        }
'''
    anchor = '        internal static SkirmishResolvedSetup Read(string path, string expectedSha256)'
    if source.count(anchor) != 1:
        raise ValueError('Exact original setup reader required')
    cached = source.replace(old,new).replace(anchor,helper+'\n'+anchor)
    if serialized_candidate is not None:
        def once(value, before, after):
            if value.count(before) != 1:
                raise ValueError('Exact serialized-guard projection anchor required: '+before)
            return value.replace(before, after, 1)
        candidate=once(cached, 'if (actual == null || !JToken.DeepEquals(JObject.FromObject(actual), expected))',
            'if (actual == null || !StringComparer.Ordinal.Equals(JsonConvert.SerializeObject(actual, Formatting.None), expected))')
        candidate=once(candidate,'static JObject CachedReference;','static string CachedReference;')
        candidate=once(candidate,'static JObject CachedExpected(','static string CachedExpected(')
        candidate=once(candidate,'CachedReference=JObject.FromObject(Read(path,expectedSha256));',
            'CachedReference=JsonConvert.SerializeObject(Read(path,expectedSha256), Formatting.None);')
        tested=serialized_candidate.read_text().replace('SourceS004SerializedGuardCandidate','SourceS004CompiledSetupInput')
        if tested != candidate:
            raise ValueError('Exact previously tested serialized guard projection required')
        cached=candidate
    dest = output / 'S004CachedSetupGuardProjection.cs'
    dest.write_text(cached)
    baseline = output / 'S004BaselineSetupGuardProjection.cs'
    baseline.write_text(source.replace('SourceS004CompiledSetupInput','SourceS004BaselineSetupGuard'))
    matches=[i for i,v in enumerate(arguments) if v == str(original)]
    if len(matches)!=1:
        raise ValueError('Exact original setup source occurrence required')
    arguments[matches[0]]=str(dest)
    arguments.append(str(baseline))
