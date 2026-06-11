public interface IUnitAttackTraceRenderer : System.IDisposable
{
    void LateUpdate();
}

public interface IUnitImpostorRenderer : System.IDisposable
{
    int LastDrawnCount { get; }

    void LateUpdate();
}
