using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// ARSeek工具杂项
/// </summary>
public class ARSeekMenuItems
{
    [MenuItem("ARSeek工具/播放动画")]
    public static void PlayAnime()
    {
        var target = Selection.activeTransform;
        if (target == null)
            return;
        if (target.TryGetComponent(out UnityEngine.Playables.PlayableDirector playableDirector))
        {
            playableDirector.Play();
        }
    }
}