using Unity.Entities;

[DisableAutoCreation]
public sealed partial class PerformanceDiagnosticsReferenceBoundarySystem : SystemBase
{
    public PerformanceDiagnosticsSystem Diagnostics { get; set; }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        Diagnostics = null;
    }
}

public sealed class PerformanceDiagnosticsReferenceSystem
{
    private World _referenceWorld;
    private PerformanceDiagnosticsReferenceBoundarySystem _referenceBoundary;

    public void Register(PerformanceDiagnosticsSystem diagnostics)
    {
        if (diagnostics == null || !TryGetOrCreateReference(out PerformanceDiagnosticsReferenceBoundarySystem boundary))
            return;

        boundary.Diagnostics = diagnostics;
    }

    public void Clear(PerformanceDiagnosticsSystem diagnostics)
    {
        if (!TryGetReference(out PerformanceDiagnosticsReferenceBoundarySystem boundary))
            return;

        if (diagnostics == null || boundary.Diagnostics == diagnostics)
            boundary.Diagnostics = null;
    }

    public bool TryGet(World world, out PerformanceDiagnosticsSystem diagnostics)
    {
        diagnostics = null;
        if (!TryGetReference(world, out PerformanceDiagnosticsReferenceBoundarySystem boundary))
            return false;

        diagnostics = boundary.Diagnostics;
        return diagnostics != null;
    }

    private bool TryGetReference(out PerformanceDiagnosticsReferenceBoundarySystem boundary)
    {
        return TryGetReference(World.DefaultGameObjectInjectionWorld, out boundary);
    }

    private bool TryGetReference(World world, out PerformanceDiagnosticsReferenceBoundarySystem boundary)
    {
        boundary = null;

        if (world == null || !world.IsCreated)
            return false;

        if (_referenceWorld == world && _referenceBoundary != null)
        {
            boundary = _referenceBoundary;
            return true;
        }

        PerformanceDiagnosticsReferenceBoundarySystem existing =
            world.GetExistingSystemManaged<PerformanceDiagnosticsReferenceBoundarySystem>();
        if (existing == null)
            return false;

        _referenceWorld = world;
        _referenceBoundary = existing;
        boundary = existing;
        return true;
    }

    private bool TryGetOrCreateReference(out PerformanceDiagnosticsReferenceBoundarySystem boundary)
    {
        if (TryGetReference(out boundary))
            return true;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        boundary = world.GetOrCreateSystemManaged<PerformanceDiagnosticsReferenceBoundarySystem>();
        _referenceWorld = world;
        _referenceBoundary = boundary;
        return true;
    }
}
