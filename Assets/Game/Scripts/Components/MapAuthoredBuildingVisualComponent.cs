using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapAuthoredBuildingVisualComponent : MonoBehaviour
{
    [SerializeField] private bool preserveAuthoredTransform = true;
    [SerializeField] private bool preserveAuthoredMaterials = true;

    public bool PreserveAuthoredTransform => preserveAuthoredTransform;
    public bool PreserveAuthoredMaterials => preserveAuthoredMaterials;
}
