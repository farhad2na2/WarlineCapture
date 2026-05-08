using UnityEngine;

public sealed class FactionVisualSettings
{
    private FactionVisualSettingsConfig _config;
    private Color _playerColor = new(0.12f, 0.72f, 1f, 1f);
    private Color _enemyColor = new(1f, 0.35f, 0.2f, 1f);
    private Color _neutralColor = new(0.82f, 0.82f, 0.82f, 1f);

    public void Init(FactionVisualSettingsConfig config)
    {
        _config = config;
        ApplyConfigIfAvailable();
    }

    public Color GetColor(byte factionId)
    {
        return factionId switch
        {
            0 => _playerColor,
            1 => _enemyColor,
            _ => _neutralColor
        };
    }

    private void ApplyConfigIfAvailable()
    {
        if (_config == null)
            return;

        _playerColor = _config.PlayerColor;
        _enemyColor = _config.EnemyColor;
        _neutralColor = _config.NeutralColor;
    }
}
