using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色位置注册表。本地角色（PlayerController）和远端角色（GazeIndicator）都注册进来，
/// 用于判断角色之间是否重叠。
/// </summary>
public static class OverlapDetection
{
    private static readonly List<Transform> characters = new List<Transform>();

    public static void RegisterCharacter(Transform t)
    {
        if (t != null && !characters.Contains(t))
            characters.Add(t);
    }

    public static void UnregisterCharacter(Transform t)
    {
        characters.Remove(t);
    }

    /// <summary>
    /// 判断 self 水平方向上是否有其他角色处于 distance 之内
    /// </summary>
    public static bool IsOverlapping(Transform self, float distance)
    {
        float sqrThreshold = distance * distance;
        Vector3 selfPos = self.position;

        for (int i = characters.Count - 1; i >= 0; i--)
        {
            Transform other = characters[i];
            if (other == null)
            {
                characters.RemoveAt(i);
                continue;
            }
            if (other == self)
                continue;

            Vector3 delta = other.position - selfPos;
            delta.y = 0f;
            if (delta.sqrMagnitude < sqrThreshold)
                return true;
        }

        return false;
    }
}
