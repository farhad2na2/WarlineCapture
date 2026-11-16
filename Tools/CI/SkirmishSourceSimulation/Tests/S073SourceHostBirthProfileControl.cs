using System;
using System.IO;
using System.Linq;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// Standalone source-host contract controls. Inputs are a captured S073 prefab
// bundle; fixtures below exercise profile rejection only and never run mission
// owners or claim native-world completeness.
internal static class S073SourceHostBirthProfileControl
{
    const string RecordName = "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord";
    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("S073 source-host birth profile: " + message);
    }

    static EntityManager NewManager()
    {
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        em.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        return em;
    }

    static GeneratedReferenceTemplateImporter.Result Import(EntityManager em, JObject capture,
        string binaryDirectory, string[] roots)
        => GeneratedReferenceTemplateImporter.Import(em, (JObject)capture.DeepClone(), binaryDirectory, roots);

    static void MustReject(Action action, string message)
    {
        bool rejected = false;
        try { action(); }
        catch (NotSupportedException) { rejected = true; }
        catch (InvalidDataException) { rejected = true; }
        catch (InvalidOperationException) { rejected = true; }
        Need(rejected, message);
    }

    static void Main(string[] args)
    {
        Need(args.Length == 2, "S073 authored prefab capture and binary directory required");
        var capture = JObject.Parse(File.ReadAllText(args[0]));
        string[] roots = ((JObject)capture["opening_prefabs"]).Properties().Select(x => x.Name).ToArray();
        Need(roots.Length == 4, "exact four-root S073 initial source template required");
        Need(!GeneratedReferenceTemplateImporter.BirthRecordIsInPresentationOmissionSet,
            "birth record must never be listed as presentation-only");

        // The generic importer must either reject a birth-record row or count
        // it as decoded; it must never count it among presentation omissions.
        var baselineProjection = GeneratedReferenceTemplateImporter.Preflight(capture, args[1], roots);
        var contaminated = (JObject)capture.DeepClone();
        var firstComponents = (JObject)contaminated["entities"][0]["components"];
        firstComponents[RecordName] = new JObject
        {
            ["present"] = true, ["buffer"] = false, ["enabled"] = JValue.CreateNull(), ["value"] = new JObject()
        };
        bool rejectedBirthRecord = false;
        JObject withRecord = null;
        try
        {
            withRecord = GeneratedReferenceTemplateImporter.Preflight(contaminated, args[1], roots);
        }
        catch (InvalidOperationException) { rejectedBirthRecord = true; }
        catch (InvalidDataException) { rejectedBirthRecord = true; }
        catch (NotSupportedException) { rejectedBirthRecord = true; }
        if (withRecord != null)
        {
            Need((int)withRecord["decoded_components"] == (int)baselineProjection["decoded_components"] + 1 &&
                 (int)withRecord["omitted_presentation_components"] == (int)baselineProjection["omitted_presentation_components"],
                "captured birth record was silently omitted by prefab importer");
        }
        Need(rejectedBirthRecord || withRecord != null,
            "birth record can be silently omitted as presentation data");

        // A hidden disabled/system/meta-shaped live entity is included in the
        // all-flags census and an opaque captured record on it blocks binding.
        var hiddenEm = NewManager();
        var hiddenImport = Import(hiddenEm, capture, args[1], roots);
        Entity hidden = hiddenEm.CreateEntity(typeof(Disabled));
        hiddenEm.PreserveInitialNativeFact(hidden, "Unity.Entities.SystemInstance", new JObject());
        hiddenEm.PreserveInitialNativeFact(hidden, "Unity.Entities.ChunkHeader", new JObject());
        hiddenEm.PreserveInitialNativeFact(hidden, RecordName, new JObject());
        MustReject(() => RuntimeActorBirthContext.BindS073SourceHostNoNativeBirthRecordProfile(hiddenEm, hiddenImport),
            "record on disabled/system/meta entity escaped the all-flags census");

        // A record on an actual imported prefab root also blocks profile bind.
        var prefabEm = NewManager();
        var prefabImport = Import(prefabEm, capture, args[1], roots);
        Entity root = prefabImport.Prefabs[roots[0]];
        prefabEm.PreserveInitialNativeFact(root, RecordName, new JObject());
        MustReject(() => RuntimeActorBirthContext.BindS073SourceHostNoNativeBirthRecordProfile(prefabEm, prefabImport),
            "active record on imported prefab was accepted");

        // A clean run-manager import binds; later live changes and wrong-manager
        // calls are rejected. These are contract fixtures, not source authority.
        var em = NewManager();
        var imported = Import(em, capture, args[1], roots);
        Need(imported.Prefabs.Count == 4 && imported.ImportedEntities == 140 && imported.ImportedComponents == 862,
            "captured S073 prefab closure/counts changed");
        MustReject(() => RuntimeActorBirthContext.RequireNoRecordProfile(em, imported.Prefabs[roots[0]]),
            "RequireNoRecordProfile succeeded before binding");
        RuntimeActorBirthContext.BindS073SourceHostNoNativeBirthRecordProfile(em, imported);
        root = imported.Prefabs[roots[0]];
        RuntimeActorBirthContext.RequireNoRecordProfile(em, root);
        Entity late = em.Instantiate(root);
        RuntimeActorBirthContext.RequireNoRecordProfile(em, late);
        em.PreserveInitialNativeFact(late, RecordName, new JObject());
        MustReject(() => RuntimeActorBirthContext.RequireNoRecordProfile(em, late),
            "live record appearance after bind was not observed");

        var foreign = NewManager();
        MustReject(() => RuntimeActorBirthContext.BindS073SourceHostNoNativeBirthRecordProfile(foreign, imported),
            "imported template result from another EntityManager was accepted");
        foreign.ImportEntity(root);
        MustReject(() => RuntimeActorBirthContext.RequireNoRecordProfile(foreign, root),
            "profile bound to another EntityManager was reused");
        MustReject(() => RuntimeActorBirthContext.BindS073SourceHostNoNativeBirthRecordProfile(em, imported),
            "second source-host profile bind was accepted");
        em.DestroyEntity(late);
        MustReject(() => RuntimeActorBirthContext.RequireNoRecordProfile(em, late),
            "destroyed actor was accepted by the profile");

        Console.WriteLine("[S073SourceHostBirthProfileControl] result=Passed scope=ExactSourceHostEntityManagerOnly nativeWorldCompleteness=0 missionReadiness=0");
    }
}
