# Optional authored navigation facts

Build normally with the source CLI. The build generates its producer identity from the current resolved decoder, importer, cache format/preparation helper, finite type/helper declaration bytes and generator code before compilation. No fixed historical producer hash is admitted.

Prepare once through the built executable (no world or runtime entities are created):

```sh
python3 Tools/CI/prepare_skirmish_navigation_facts.py --build-receipt /absolute/build/build.json --data-bundle /absolute/original/bundle.json --output /absolute/new/nav-cache
```

Pass `/absolute/new/nav-cache/data-bundle.json` to the ordinary source CLI run command. The original bundle retains the raw path. Absence of `facts` uses the raw parser/decoder; invalid supplied facts fail explicitly before entity writes. Runtime still stream-hashes the original raw artifact and always runs the original fresh entity materializer.

The finite8type format includes every recursive instance field, original component insertion order, exact authored source-ID strings and enabled flags. Floats retain rawbits; FixedString128 adapter null, empty and exact UTF8 remain distinct. Invalid UTF8/unpaired surrogates reject. No runtime entity IDs, grid results, bank state, current factory decisions, outcomes or future state are cached.

`navigation-inputs-before-materialization.json` records verified raw/cache/manifest hashes after full decode before entity writes. `navigation-inputs-after-run.json` rehashes them after the run. Relevant producer/schema changes require cache preparation again; unrelated gameplay body changes do not alter the key when those dependencies remain unchanged.

Candidate evidence:44finite controls+8source/protocol controls, original decoded-field bit/order/string equality and matching actual52actor/18building/4954navigation state plus5normal ticks. In one final serialized pair startup8.351362→6.809610s (18.46%); OS cache uncontrolled. This is a CLI startup result, not native game speed or a new full-mission/UI/device acceptance result.
