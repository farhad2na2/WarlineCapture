"""Finite current-source resolver for the shared source simulation host.

This is build plumbing, not another gameplay/runtime implementation. Current
native files are linked; explicit original members are extracted anew; only
named finite engine/data adapters live in the package.
"""
from __future__ import annotations
import hashlib,json,os,sys,re,tempfile
from pathlib import Path,PurePosixPath
sys.dont_write_bytecode=True
import skirmish_owned_source as owned
MAX_SOURCE=16*1024*1024
MODULE_SOURCE_SHA=owned.module_pin(__name__,__file__)

class UnsupportedSourceShape(ValueError):pass
def need(ok,message):
    if not ok:raise UnsupportedSourceShape(message)
def sha(raw):return hashlib.sha256(raw).hexdigest()
def relative(root,name):
    need(isinstance(name,str)and name and not Path(name).is_absolute(),"Relative source path required")
    p=PurePosixPath(name)
    need(".."not in p.parts and "\\"not in name,"Source path escape")
    value=(root/Path(*p.parts)).resolve(strict=True)
    need(value.is_relative_to(root),"Source outside declared root")
    return value
def read(path,pins):
    raw=path.read_bytes();need(0<len(raw)<=MAX_SOURCE,"Bounded nonempty source required: "+str(path))
    value=sha(raw)
    need(str(path)not in pins or pins[str(path)]==value,"Source changed between member reads: "+str(path))
    pins[str(path)]=value
    return raw.decode("utf-8").replace("\r\n","\n")
def shape(text,spec):
    for declaration in spec.get("require_declarations",[]):
        need(len(re.findall(re.escape(declaration)+r"(?!\w)",text))==1,"Unsupported/ambiguous source declaration: "+declaration)
    for forbidden in spec.get("forbid",[]):
        need(forbidden not in text,"Unsupported source shape contains "+forbidden)
def bind(text,bindings,proof):
    for item in bindings:
        old,new,count=item["from"],item["to"],item["count"]
        actual=len(re.findall(r"\b"+re.escape(old)+r"\b",text))if item.get("word")else text.count(old)
        need(isinstance(count,int)and count>0 and old and actual==count,
             "Unsupported finite binding shape: "+old)
        text=re.sub(r"\b"+re.escape(old)+r"\b",lambda _:new,text)if item.get("word")else text.replace(old,new)
        proof.append({"kind":"finite_binding","from":old,"to":new,"count":count,"whole_identifier":bool(item.get("word"))})
    return text
def select(text,spec,method_span):
    owner=spec.get("owner")
    if owner:
        need(text.count(owner)==1,"Unsupported/ambiguous owner: "+owner)
        a,b=method_span(text,owner);text=text[a:b]
    if "constant_name"in spec:
        name=spec["constant_name"]
        need(isinstance(name,str)and re.fullmatch(r"[A-Za-z_]\w*",name),"Finite constant identifier required")
        pattern=r'(?m)^[ \t]*(?:public|internal|private)\s+const\s+(?:int|float|string)\s+'+re.escape(name)+r'\s*=\s*(?:"[^"\r\n]*"|[-+]?\d+(?:\.\d+)?[fF]?)\s*;'
        values=list(re.finditer(pattern,text))
        need(len(values)==1,"Unsupported/ambiguous literal constant declaration: "+name)
        return values[0].group().strip()
    signature=spec["signature"]
    need(text.count(signature)==1,"Unsupported/ambiguous member: "+signature)
    if spec.get("expression_body"):
        a=text.index(signature)+len(signature)
        match=re.match(r"\s*=>\s*([^;{}\"']+);",text[a:])
        need(match is not None,"Unsupported simple original expression member: "+signature)
        return match.group(1).strip()
    a,b=method_span(text,signature)
    value=text[a:b]
    if spec.get("body_only"):
        start=value.index("{")
        value=value[start+1:-1]
    def anchor_spans(anchor):
        need(isinstance(anchor,str)and anchor,"Nonempty source statement anchor required")
        prefix=r"(?<![\w.])"if re.match(r"\w",anchor)else""
        return list(re.finditer(prefix+re.escape(anchor),value))
    if "start_at"in spec or "through"in spec:
        first,last=anchor_spans(spec["start_at"]),anchor_spans(spec["through"])
        need(len(first)==1 and len(last)==1,"Unsupported/ambiguous source statement range")
        a=first[0].start();b=last[0].end()
        need(a<b,"Ordered source statement range required")
        value=value[a:b]
    if "stop_before"in spec:
        matches=anchor_spans(spec["stop_before"])
        need(len(matches)==1,"Unsupported/ambiguous active-domain frontier")
        value=value[:matches[0].start()]
    return value
def resolve_manifest(repo,package,manifest_path,output,mission="S002",seed=104731):
    repo=Path(repo).resolve(strict=True);package=Path(package).resolve(strict=True)
    profiles=owned.require("skirmish_source_profiles");require_profile=profiles.require_profile;generate_guard=profiles.generate_guard
    profile_pins=require_profile(mission,seed)
    # Reuse repository primitives; importing has no build/run effects.
    sys.path.insert(0,str(repo/"Tools/CI"))
    causal=owned.require("skirmish_causal_worker")
    aria=owned.require("skirmish_source_syntax")
    raw_manifest=Path(manifest_path).read_bytes()
    data=json.loads(raw_manifest,object_pairs_hook=aria.no_duplicates)
    need(data.get("schema_version")==1 and data.get("profile")==({"mission":"S026","seed":104755,"numeric_runtime":"InstalledMono"}if mission=="S026"else{"mission":"S002","seed":104731,"numeric_runtime":"InstalledMono"}),
         "Unsupported source manifest profile")
    need(isinstance(data.get("compiler_defines",[]),list)and"UNITY_EDITOR"not in data.get("compiler_defines",[]),"Editor source profile is not admitted")
    output=Path(output).absolute();generated_root=(output/"generated").resolve()
    entries=data.get("sources");need(isinstance(entries,list)and 0<len(entries)<=2048,"Finite source list required")
    pins={str(Path(manifest_path).resolve()):sha(raw_manifest),str(Path(__file__).resolve()):MODULE_SOURCE_SHA}
    pins.update(owned.pins());pins.update(profile_pins)
    for helper in [repo/"Tools/CI/skirmish_causal_worker.py",Path(aria.__file__).resolve()]:
        key=str(helper);digest=sha(helper.read_bytes())
        need(key not in pins or pins[key]==digest,"Conflicting executed helper epoch: "+key)
        pins[key]=digest
    resolved=[];pending=[];ids=set();destinations=set();emissions=[];generator_cache={}
    for entry in entries:
        identity=entry.get("id");need(isinstance(identity,str)and identity and identity not in ids,"Unique source id required");ids.add(identity)
        kind=entry.get("kind");proof=[]
        if kind=="unresolved":
            pending.append({"id":identity,"reason":entry.get("reason","Missing explicit current-source recipe")});continue
        if kind=="native_direct":
            path=relative(repo,entry["path"]);text=read(path,pins);shape(text,entry)
            resolved.append({"id":identity,"kind":kind,"path":str(path),"sha256":pins[str(path)],"origin":"current_repository"})
            continue
        if kind=="finite_adapter":
            need(entry.get("role")in {"engine_boundary","data_decoder","host_glue","entrypoint"}and entry.get("contains_original_gameplay")is False,
                 "Finite adapters cannot smuggle frozen original gameplay")
            path=relative(package,entry["path"]);text=read(path,pins);shape(text,entry)
            need("/private/tmp/"not in text and "/tmp/"not in text,"External TEMP code dependency in finite adapter: "+identity)
            resolved.append({"id":identity,"kind":kind,"path":str(path),"sha256":pins[str(path)],"role":entry["role"],"origin":"owned_package_adapter"})
            continue
        need(kind=="current_original","Unknown source kind: "+str(kind))
        destination=entry["output"]
        need(isinstance(destination,str)and destination and "\\"not in destination,"Nonempty forward-slash generated output required")
        parts=destination.split("/");posix=PurePosixPath(destination)
        need(not Path(destination).is_absolute()and all(p not in {"",".",".."}for p in parts)and posix.as_posix()==destination,
             "Canonical relative generated output required")
        canonical=(generated_root/Path(*parts)).resolve()
        need(canonical.is_relative_to(generated_root)and str(canonical).casefold()not in destinations,
             "Unique canonical generated output required");destinations.add(str(canonical).casefold())
        if "generator" in entry:
            # Only explicit package recipe modules; execute the pinned bytes once.
            module=relative(package,entry["generator"])
            key=str(module)
            if key not in generator_cache:
                code=read(module,pins)
                need("/private/tmp/"not in code and "/tmp/"not in code,"TEMP code dependency in recipe module")
                for name in entry["generator_inputs"]:read(relative(repo,name),pins)
                for name in entry.get("generator_helpers",[]):read(relative(package,name),pins)
                namespace={"__name__":"source_recipe","__file__":str(module)}
                exec(compile(code,str(module),"exec"),namespace)
                need(callable(namespace.get("generate")),"Finite recipe generate(repo,output) required")
                with tempfile.TemporaryDirectory(prefix="skirmish-source-recipe-")as temporary:
                    recipe_output=Path(temporary)/"generated"
                    detail=namespace["generate"](repo,recipe_output)
                    outputs={f.name:f.read_text()for f in recipe_output.glob("*.cs")}
                for name,expected in detail.get("source_pins",{}).items():
                    dependency=(Path(name)if Path(name).is_absolute()else repo/Path(name)).resolve(strict=True)
                    need(dependency.is_relative_to(repo)and isinstance(expected,str)and len(expected)==64,"Recipe source pin outside repository or malformed")
                    need(str(dependency)not in pins or pins[str(dependency)]==expected,"Conflicting recipe source pin")
                    need(sha(dependency.read_bytes())==expected,"Source changed after recipe read: "+str(dependency))
                    pins[str(dependency)]=expected
                for path,expected in pins.items():
                    need(sha(Path(path).read_bytes())==expected,"Source changed during recipe generation: "+path)
                generator_cache[key]=(outputs,detail)
            outputs,detail=generator_cache[key]
            need(entry["generator_output"]in outputs,"Recipe output missing")
            text=outputs[entry["generator_output"]]
            proof.append({"generator":key,"generator_sha256":pins[key],"generation_proof":detail})
        elif "template" in entry:
            template=relative(package,entry["template"]);text=read(template,pins)
            need("/private/tmp/"not in text and "/tmp/"not in text,"TEMP dependency in extraction template")
            for member in entry.get("members",[]):
                src=relative(repo,member["path"]);original=read(src,pins);shape(original,member)
                projected=causal._shipping(original)if member.get("shipping",True)else original
                piece=select(projected,member,aria.method_span)
                proof.append({"source":str(src),"source_sha256":pins[str(src)],"owner":member.get("owner"),"signature":member.get("signature"),"constant_name":member.get("constant_name"),"extracted_sha256":sha(piece.encode())})
                piece=bind(piece,member.get("bindings",[]),proof);marker="/*SOURCE:"+member["id"]+"*/"
                need(text.count(marker)==1,"Unique extraction marker required: "+marker);text=text.replace(marker,piece)
            need("/*SOURCE:"not in text,"Unresolved original member in template")
        else:
            src=relative(repo,entry["path"]);text=read(src,pins);shape(text,entry)
            if entry.get("shipping",True):text=causal._shipping(text)
            for omitted in entry.get("omit_members",[]):
                need(text.count(omitted)==1,"Unsupported/ambiguous omitted API: "+omitted)
                a,b=aria.method_span(text,omitted)
                proof.append({"unsupported_api_omitted":omitted,"original_member_sha256":sha(text[a:b].encode())})
                text=text[:a]+text[b:]
            # Whole-source or explicitly selected original scope; never old-body hashes.
            if "signature"in entry:text=select(text,entry,aria.method_span)
            proof.append({"source":str(src),"source_sha256":pins[str(src)],"selection":entry.get("signature","whole_source"),"projected_sha256":sha(text.encode())})
            text=entry.get("prefix","")+text+entry.get("suffix","")
        text=bind(text,entry.get("bindings",[]),proof)
        need("/private/tmp/"not in text and "/tmp/"not in text,"Generated executable source has external TEMP dependency")
        emissions.append((destination,text))
        resolved.append({"id":identity,"kind":kind,"generated":destination,"sha256":sha(text.encode()),"proof":proof,"origin":"fresh_current_repository_extraction"})
    # Finite generators can declare coherent CURRENT native companion links.
    direct_paths={item["path"]for item in resolved if item["kind"]=="native_direct"}
    for item in resolved:
        for detail in item.get("proof",[]):
            for required in detail.get("generation_proof",{}).get("required_current_links",[]):
                need(str((Path(required)if Path(required).is_absolute()else repo/Path(required)).resolve())in direct_paths,"Missing coherent current source companion: "+required)
    guard,guard_pins=generate_guard(mission);pins.update(guard_pins)
    emissions.append(("SourceAdmittedProfiles.cs",guard));resolved.append({"id":"central_admitted_profiles","kind":"finite_adapter","generated":"SourceAdmittedProfiles.cs","sha256":sha(guard.encode()),"proof":[],"origin":"strict_profile_json"})
    if mission=="S026":
        template=relative(package,"Tools/CI/SkirmishSourceSimulation/Templates/S026/SourceTransformConsumedReadset.cs.template");body=read(template,pins)
        source_pins={str(output/"generated"/x["generated"])if "generated"in x else x["path"]:x["sha256"] for x in resolved}
        readset=json.dumps({"schema_version":1,"kind":"ExactConsumedTransformVersionReadset","scope":"Finite S026 current compiled owner readset, single61 and no consumed transform changed-filter reads; unsupported domains remain guarded", "source_pins":source_pins},indent=2)+"\n"
        need(body.count("READSET_SHA256")==1,"One explicit transform readset seal required");body=body.replace("READSET_SHA256",sha(readset.encode()))
        emissions.append(("SourceTransformConsumedReadset.cs",body));resolved.append({"id":"s026_consumed_transform_readset","kind":"finite_adapter","generated":"SourceTransformConsumedReadset.cs","sha256":sha(body.encode()),"proof":[{"current_source_pins":source_pins,"readset_sha256":sha(readset.encode())}],"origin":"current_compiled_source_readset"})
    # No source writes until all entries and shapes were checked.
    for path,expected in owned.pins().items():
        need(path not in pins or pins[path]==expected,"Conflicting executed owned-source epoch: "+path)
        pins[path]=expected
    for path,expected in pins.items():
        need(sha(Path(path).read_bytes())==expected,"Source/helper/manifest changed before emission: "+path)
    need(not output.exists(),"New isolated output directory required")
    output.mkdir(parents=True)
    for name,text in emissions:
        p=output/"generated"/name;p.parent.mkdir(parents=True,exist_ok=True)
        with p.open("w",encoding="utf-8",newline="\n")as stream:stream.write(text)
    if mission=="S026":(output/"compiled-transform-readset.json").write_text(readset)
    for item in resolved:
        if "generated"in item:item["path"]=str(output/"generated"/item["generated"])
    receipt={"schema_version":1,"kind":"SkirmishSourceResolution","status":"NeedsExplicitRecipes"if pending else"Resolved",
             "profile":data["profile"],"runtime_backend":"TypedGroundS026V1"if mission=="S026"else"LegacyGroundS002V1","resolved":resolved,"unresolved":pending,"pins":pins,
             "native_direct_count":sum(x["kind"]=="native_direct"for x in resolved),
             "fresh_extracted_count":sum(x["kind"]=="current_original"for x in resolved),
             "adapter_count":sum(x["kind"]=="finite_adapter"for x in resolved),
             "external_temp_source_dependencies":0,"gameplay_executed":False}
    (output/"sources.rsp").write_text("\n".join(json.dumps(x["path"])for x in resolved)+"\n")
    tmp=output/"resolution.json.tmp";tmp.write_text(json.dumps(receipt,indent=2)+"\n");os.replace(tmp,output/"resolution.json")
    return receipt
