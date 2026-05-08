using UnityEngine;

public sealed class WarlineCaptureIso2DPrototypeAgent : MonoBehaviour
{
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private Vector3[] waypoints;
    [SerializeField] private float travelDuration = 3f;
    [SerializeField] private bool pingPong = true;

    private WarlineCaptureIso2DSorting sorting;
    private float elapsed;

    public Vector3 StartPosition => startPosition;
    public Vector3 EndPosition => endPosition;
    public float NormalizedTime => travelDuration > 0f ? Mathf.Clamp01(elapsed / travelDuration) : 1f;

    private void Awake()
    {
        sorting = GetComponent<WarlineCaptureIso2DSorting>();
        if (startPosition == Vector3.zero && endPosition == Vector3.zero)
        {
            startPosition = transform.position;
            endPosition = transform.position;
        }
    }

    private void Update()
    {
        Simulate(Time.deltaTime);
    }

    public void Configure(Vector3 start, Vector3 end, float duration, bool loop)
    {
        ConfigureWaypoints(new[] { start, end }, duration, loop);
    }

    public void ConfigureWaypoints(Vector3[] path, float duration, bool loop)
    {
        waypoints = path != null && path.Length >= 2
            ? path
            : new[] { transform.position, transform.position };
        startPosition = waypoints[0];
        endPosition = waypoints[waypoints.Length - 1];
        travelDuration = Mathf.Max(0.01f, duration);
        pingPong = loop;
        elapsed = 0f;
        transform.position = startPosition;
        sorting = GetComponent<WarlineCaptureIso2DSorting>();
        sorting?.ApplySorting();
    }

    public void SetNormalizedTime(float normalizedTime)
    {
        elapsed = Mathf.Clamp01(normalizedTime) * Mathf.Max(0.01f, travelDuration);
        ApplyPosition();
    }

    public void Simulate(float deltaTime)
    {
        elapsed += Mathf.Max(0f, deltaTime);
        if (pingPong && elapsed > travelDuration)
        {
            elapsed %= travelDuration;
        }
        else if (!pingPong)
        {
            elapsed = Mathf.Min(elapsed, travelDuration);
        }

        ApplyPosition();
    }

    private void ApplyPosition()
    {
        var t = NormalizedTime;
        if (pingPong)
        {
            t = Mathf.PingPong(elapsed / Mathf.Max(0.01f, travelDuration), 1f);
        }

        transform.position = SamplePath(Mathf.SmoothStep(0f, 1f, t));
        sorting?.ApplySorting();
    }

    private Vector3 SamplePath(float normalizedTime)
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            return Vector3.Lerp(startPosition, endPosition, normalizedTime);
        }

        var segmentPosition = Mathf.Clamp01(normalizedTime) * (waypoints.Length - 1);
        var segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentPosition), waypoints.Length - 2);
        var segmentTime = segmentPosition - segmentIndex;
        return Vector3.Lerp(waypoints[segmentIndex], waypoints[segmentIndex + 1], segmentTime);
    }
}
