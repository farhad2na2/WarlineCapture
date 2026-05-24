using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WarlineCaptureShellLoadingProgressView : MonoBehaviour
{
    [SerializeField] private RectTransform progressFill;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float fillWidth = 648f;

    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;

    public void Configure(RectTransform fill, TMP_Text percent, TMP_Text status, float maxFillWidth)
    {
        progressFill = fill;
        percentText = percent;
        statusText = status;
        fillWidth = Mathf.Max(1f, maxFillWidth);
        ApplyProgress(0f, new FixedString64Bytes("Preparing command interface"));
    }

    private void OnEnable()
    {
        ApplyProgress(0f, new FixedString64Bytes("Preparing command interface"));
    }

    private void Update()
    {
        if (!TryGetLoading(out UiShellLoadingProgressComponent loading))
            return;

        ApplyProgress(loading.Progress01, loading.Status);
    }

    private void ApplyProgress(float progress01, FixedString64Bytes status)
    {
        float clamped = Mathf.Clamp01(progress01);
        if (progressFill != null)
        {
            Vector2 size = progressFill.sizeDelta;
            size.x = fillWidth * clamped;
            progressFill.sizeDelta = size;
        }

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";

        if (statusText != null)
            statusText.text = status.Length == 0 ? "Preparing command interface" : status.ToString();
    }

    private bool TryGetLoading(out UiShellLoadingProgressComponent loading)
    {
        loading = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world || !hasBoundaryQuery)
        {
            cachedWorld = world;
            boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                ComponentType.ReadOnly<UiShellLoadingProgressComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        Entity boundary = boundaryQuery.GetSingletonEntity();
        loading = world.EntityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
        return true;
    }
}
