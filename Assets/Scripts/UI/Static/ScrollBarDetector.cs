using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 监听滑动条显隐，修改共用此滑动条的多个滑动区域的运动类型
/// </summary>
public class ScrollBarDetector : MonoBehaviour
{
    public List<ScrollRect> SharedScrollRects;

    public float Elasticity;

    private void OnEnable()
    {
        for(int i = 0; i < SharedScrollRects.Count; i++)
        {
            SharedScrollRects[i].movementType = ScrollRect.MovementType.Elastic;
            SharedScrollRects[i].elasticity = Elasticity;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < SharedScrollRects.Count; i++)
        {
            SharedScrollRects[i].movementType = ScrollRect.MovementType.Clamped;
        }
    }
}