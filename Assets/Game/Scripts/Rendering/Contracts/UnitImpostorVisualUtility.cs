using Unity.Collections;
using UnityEngine;

public static class UnitImpostorVisualUtility
{
    private const float CharacterTacticalBillboardStartCameraY = 80f;
    private const float CharacterTacticalBillboardFullCameraY = 200f;
    private const float CharacterTacticalBillboardMaxScale = 16f;

    public static bool HasUnitPrefix(FixedString64Bytes sourceKey)
    {
        return sourceKey.Length >= 5 &&
               sourceKey[0] == (byte)'U' &&
               sourceKey[1] == (byte)'n' &&
               sourceKey[2] == (byte)'i' &&
               sourceKey[3] == (byte)'t' &&
               sourceKey[4] == (byte)'_';
    }

    public static bool HasCharacterUnitPrefix(FixedString64Bytes sourceKey)
    {
        return sourceKey.Length >= 9 &&
               HasUnitPrefix(sourceKey) &&
               sourceKey[5] == (byte)'C' &&
               sourceKey[6] == (byte)'h' &&
               sourceKey[7] == (byte)'r' &&
               sourceKey[8] == (byte)'_';
    }

    public static float ResolveCharacterTacticalScale(float cameraY)
    {
        float t = Mathf.InverseLerp(
            CharacterTacticalBillboardStartCameraY,
            CharacterTacticalBillboardFullCameraY,
            cameraY);
        return Mathf.Lerp(1f, CharacterTacticalBillboardMaxScale, t);
    }

    public static Quaternion ResolveBillboardRotation(
        bool isCharacter,
        Vector3 position,
        Vector3 cameraPosition,
        Quaternion cameraRotation)
    {
        if (isCharacter && cameraPosition.y >= CharacterTacticalBillboardStartCameraY)
            return Quaternion.LookRotation(-(cameraRotation * Vector3.forward), cameraRotation * Vector3.up);

        Vector3 toCamera = cameraPosition - position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = Vector3.forward;

        return Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }
}
