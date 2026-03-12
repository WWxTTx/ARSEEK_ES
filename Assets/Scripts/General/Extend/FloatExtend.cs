using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloatExtend
{
    public static float NormalizedAngle180(this float angle)
    {
        angle = (angle + 180) % 360 - 180;

        if (angle < -180)
            angle += 360;

        return Mathf.Abs(angle);
    }

    // Normalize Euler angles to 0-360 range
    public static float NormalizeAngle(this float angle)
    {
        while (angle > 360) angle -= 360;
        while (angle < 0) angle += 360;
        return angle;
    }
}