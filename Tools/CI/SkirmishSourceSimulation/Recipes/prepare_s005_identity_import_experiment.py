#!/usr/bin/env python3
"""Generate an isolated S005 identity-preserving importer experiment.

No default host files are altered and diagnostic handles are not Unity IDs.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
IMPORTER=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs'
CODEC=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/080_ExactInitialValueCodec.cs'
PROJECTION=ROOT/'T Logs/SkirmishProduction/s005-prefab-object-projection-20261105-T1/projection.json'
RECEIPT=PROJECTION.with_name('receipt.json')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def replace(text,old,new):
    if text.count(old)!=1:raise ValueError('Exact source anchor required: '+old[:90])
    return text.replace(old,new)
def prepare(out):
    evidence=json.loads(RECEIPT.read_text())
    if evidence['status']!='ProjectedInspectionOnly' or sha(PROJECTION)!=evidence['projection_sha256']:raise ValueError('Exact inspection projection required')
    importer=IMPORTER.read_text();codec=CODEC.read_text()
    importer=replace(importer,'bool focused=(string)source["kind"]=="AuthoredSkirmishPrefabReferenceData";',
       'bool s005=(string)source["kind"]=="S005PrefabObjectIdentityProjection"; bool focused=(string)source["kind"]=="AuthoredSkirmishPrefabReferenceData" || s005; if(s005) S005IdentityHandles.Bind(source);')
    importer=replace(importer,'(string)source["authority"]=="ActualSourceAssetsAndNativeBakers_NotMissionState","Actual source-asset bake profile required");',
       '(s005 || (string)source["authority"]=="ActualSourceAssetsAndNativeBakers_NotMissionState"),"Actual source-asset bake profile required");')
    importer=replace(importer,'(bool?)source["geometry_exported"]==false&&FocusedUnsupportedValuesAreUnconsumed(source)&&requested!=null',
       '((s005 && (bool?)source["replay_ready"]==false && (bool?)source["mission_ready"]==false) || ((bool?)source["geometry_exported"]==false&&FocusedUnsupportedValuesAreUnconsumed(source)))&&requested!=null')
    importer=replace(importer,'var codec=new ExactInitialValueCodec(binaryDirectory,new JObject());\n            var assembly=typeof(GeneratedReferenceTemplateImporter).Assembly;',
       'var codec=new ExactInitialValueCodec(binaryDirectory,new JObject(),identityHandle: s005 ? (Func<string,string,int>)S005IdentityHandles.Resolve : null);\n            var assembly=typeof(GeneratedReferenceTemplateImporter).Assembly;')
    importer=replace(importer,'if(PresentationOnly.Contains(property.Name)){omitted++;continue;}','if(PresentationOnly.Contains(property.Name) && !s005){omitted++;continue;}')
    codec=replace(codec,'readonly string directory;readonly JObject artifacts;readonly IInitialRawLayout raw;',
       'readonly Func<string,string,int> identityHandle;readonly string directory;readonly JObject artifacts;readonly IInitialRawLayout raw;')
    codec=replace(codec,'Func<Type,bool> allowedManagedDto=null){directory=Path.GetFullPath(root);',
       'Func<Type,bool> allowedManagedDto=null,Func<string,string,int> identityHandle=null){this.identityHandle=identityHandle;directory=Path.GetFullPath(root);')
    anchor='   if(codec=="unsupported-v1")throw new NotSupportedException('
    codec=replace(codec,anchor,'   if(codec=="source-object-identity-v1"){Keys(o,"codec","identity_sha256");Need(identityHandle!=null && expected.IsGenericType && expected.GetGenericTypeDefinition()==typeof(UnityObjectRef<>) && expected.GetGenericArguments()[0].FullName=="UnityEngine.GameObject","Unbound diagnostic source identity");object value=Activator.CreateInstance(expected);var field=expected.GetField("InstanceId");Need(field!=null && field.FieldType==typeof(int),"Source reference handle field missing");field.SetValue(value,identityHandle(path,Text(o,"identity_sha256")));return value;}\n'+anchor)
    out.mkdir(parents=True,exist_ok=False)
    (out/'importer.cs').write_text(importer);(out/'codec.cs').write_text(codec)
    pins={str(p):sha(p) for p in (IMPORTER,CODEC,PROJECTION,RECEIPT,Path(__file__).resolve())}
    (out/'preparation.json').write_text(json.dumps({'status':'PreparedExperimentalOnly','source_pins':pins,
       'scope':'Diagnostic source identity handles, no native instance IDs, no gameplay admission',
       'native_parity':False,'mission_ready':False},indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);prepare(p.parse_args().output.resolve())
