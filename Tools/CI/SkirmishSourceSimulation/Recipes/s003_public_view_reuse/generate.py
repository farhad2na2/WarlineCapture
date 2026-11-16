"""Opt-in source-host experiment; native gameplay and default host stay unchanged."""
import hashlib
from pathlib import Path

CATALOG = 'Tools/CI/SkirmishSourceSimulation/Adapters/223_PublicCommandCatalog.cs'
CONTROLLER = 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceS003AirOpeningController.cs'


def generate(repo, output):
    repo, output = Path(repo).resolve(strict=True), Path(output)
    paths = [repo / CATALOG, repo / CONTROLLER]
    raw = [path.read_bytes() for path in paths]
    catalog, controller = (value.decode('utf-8') for value in raw)

    def once(text, old, new):
        if text.count(old) != 1:
            raise ValueError('Exact current source-host seam required: ' + old[:100])
        return text.replace(old, new, 1)

    catalog = once(catalog,
        'internal static View Read(EntityManager em,Entity session,SkirmishArmyProfileConfig army){',
        'internal static View Read(EntityManager em,Entity session,SkirmishArmyProfileConfig army,SkirmishAriaPublicView? sameStepView=null,bool auditSameStep=false){')
    catalog = once(catalog,
        'view.PublicView=SkirmishAriaPublicProjection.FromSession(em,session,army);',
        '''if(sameStepView.HasValue){
  if(auditSameStep){var fresh=SkirmishAriaPublicProjection.FromSession(em,session,army);if(!Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JObject.FromObject(fresh),Newtonsoft.Json.Linq.JObject.FromObject(sameStepView.Value)))throw new InvalidOperationException("Same-step public observation differs");}
  view.PublicView=sameStepView.Value;
 }else view.PublicView=SkirmishAriaPublicProjection.FromSession(em,session,army);''')
    controller = once(controller,
        'receipt = StepGround(tick, frame, time, match, ref action);',
        'receipt = StepGround(tick, frame, time, match, view, ref action);')
    controller = once(controller,
        'JObject StepGround(long tick, int frame, float time, SkirmishMatchState match, ref string action)',
        'JObject StepGround(long tick, int frame, float time, SkirmishMatchState match, SkirmishAriaPublicView sameStepView, ref string action)')
    controller = once(controller,
        'var current = PublicCommandCatalog.Read(em, session, army);',
        '''bool auditSameStep = Environment.GetEnvironmentVariable("WARLINE_S003_PUBLIC_VIEW_AUDIT") == "1";
            var current = PublicCommandCatalog.Read(em, session, army, sameStepView, auditSameStep);
            if (auditSameStep && frame % 600 == 0)
                Console.WriteLine("[S003SameStepPublicViewAudit] frame=" + frame + " freshEqualsReused=1 sourceOnly=1 readiness=0");''')
    if any(path.read_bytes() != value for path, value in zip(paths, raw)):
        raise ValueError('Source host changed during experimental generation')
    output.mkdir(parents=True, exist_ok=True)
    outputs = {'223_PublicCommandCatalog.cs': catalog, 'SourceS003AirOpeningController.cs': controller}
    for name, text in outputs.items():
        (output / name).write_text(text)
    return {
        'kind': 'S003SameStepPublicObservationExperiment',
        'experimental_only': True,
        'source_pins': {str(path): hashlib.sha256(value).hexdigest() for path, value in zip(paths, raw)},
        'outputs': {name: hashlib.sha256(text.encode()).hexdigest() for name, text in outputs.items()},
        'scope': 'Source-host same-step observation reuse; audit optionally performs the original second read',
        'native_parity': False,
        'mission_ready': False,
    }
