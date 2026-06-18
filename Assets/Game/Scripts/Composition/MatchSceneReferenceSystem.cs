using Unity.Entities;
using UnityEngine.SceneManagement;

[DisableAutoCreation]
public sealed partial class MatchSceneReferenceBoundarySystem : SystemBase
{
    public MatchSceneView View { get; set; }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        View = null;
    }
}

public sealed class MatchSceneReferenceSystem
{
    private World _referenceWorld;
    private MatchSceneReferenceBoundarySystem _referenceBoundary;

    public void Register(MatchSceneView view)
    {
        if (view == null || !TryGetOrCreateReference(out MatchSceneReferenceBoundarySystem boundary))
            return;

        boundary.View = view;
    }

    public void Clear(MatchSceneView view)
    {
        if (!TryGetReference(out MatchSceneReferenceBoundarySystem boundary))
            return;

        if (view == null || boundary.View == view)
            boundary.View = null;
    }

    public bool TryGetLoadedMatchSceneView(World world, out MatchSceneView view)
    {
        view = null;
        if (!TryGetReference(world, out MatchSceneReferenceBoundarySystem boundary))
            return false;

        MatchSceneView candidate = boundary.View;
        if (!IsLoadedMatchSceneView(candidate))
            return false;

        view = candidate;
        return true;
    }

    private bool TryGetReference(out MatchSceneReferenceBoundarySystem boundary)
    {
        return TryGetReference(World.DefaultGameObjectInjectionWorld, out boundary);
    }

    private bool TryGetReference(World world, out MatchSceneReferenceBoundarySystem boundary)
    {
        boundary = null;

        if (world == null || !world.IsCreated)
            return false;

        if (_referenceWorld == world && _referenceBoundary != null)
        {
            boundary = _referenceBoundary;
            return true;
        }

        MatchSceneReferenceBoundarySystem existing = world.GetExistingSystemManaged<MatchSceneReferenceBoundarySystem>();
        if (existing == null)
            return false;

        _referenceWorld = world;
        _referenceBoundary = existing;
        boundary = existing;
        return true;
    }

    private bool TryGetOrCreateReference(out MatchSceneReferenceBoundarySystem boundary)
    {
        if (TryGetReference(out boundary))
            return true;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        boundary = world.GetOrCreateSystemManaged<MatchSceneReferenceBoundarySystem>();
        _referenceWorld = world;
        _referenceBoundary = boundary;
        return true;
    }

    private static bool IsLoadedMatchSceneView(MatchSceneView view)
    {
        if (view == null || view.gameObject == null)
            return false;

        Scene scene = view.gameObject.scene;
        return scene.IsValid() &&
               scene.isLoaded &&
               scene.name == SceneLifecycleSystem.MatchSceneName;
    }
}
