using System;
using UnityEngine;

/// <summary>
/// 相机移动类型
/// </summary>
public enum CameraMoveType
{
    /// <summary>
    /// 禁用移动
    /// </summary>
    None,
    /// <summary>
    /// 水平左右移动
    /// </summary>
    Vertical,
    /// <summary>
    /// 垂直上下移动
    /// </summary>
    Horizontal,
    /// <summary>
    /// 上下左右自由移动
    /// </summary>
    Pan
}

/// <summary>
/// 相机旋转类型
/// </summary>
public enum CameraRotateType
{
    /// <summary>
    /// 禁用旋转
    /// </summary>
    None,
    /// <summary>
    /// 以自己为中心旋转
    /// </summary>
    LookAround,
    /// <summary>
    /// 绕锚点旋转
    /// </summary>
    RotateAround,
    /// <summary>
    /// 绕鼠标位置旋转
    /// </summary>
    RotateAroundMouse,
    RotateAroundScreen
}

/// <summary>
/// 相机缩放类型
/// </summary>
public enum CameraZoomType
{
    /// <summary>
    /// 禁用缩放
    /// </summary>
    None,
    /// <summary>
    /// 朝相机正前方拉近拉远
    /// </summary>
    Forward,
    /// <summary>
    /// 朝鼠标位置拉近拉远
    /// </summary>
    Mouse,
    /// <summary>
    /// 朝锚点拉近拉远
    /// </summary>
    Pivot
}

[Serializable]
public class RestrictMove
{
    [Tooltip("水平方向移动最小值")]
    [Range(0f, 1f)]
    public float minMove_H = 0f;
    [Tooltip("水平方向移动最大值")]
    [Range(0f, 1f)]
    public float maxMove_H = 1f;
    [Tooltip("垂直方向移动最小值")]
    [Range(0f, 1f)]
    public float minMove_V = 0f;
    [Tooltip("垂直方向移动最大值")]
    [Range(0f, 1f)]
    public float maxMove_V = 1f;
}

[Serializable]
public class RestrictCameraMove
{
    [Tooltip("移动方向（勾选时模型移动方向和鼠标移动方向一致）")]
    public bool moveAlongMouse = true;

    [Tooltip("向左移动最大值")]
    public float maxMove_L = 0f;
    [Tooltip("向右移动最大值")]
    public float maxMove_R = 0f;

    [Tooltip("向上移动最大值")]
    public float maxMove_U = 0f;
    [Tooltip("向下移动最大值")]
    public float maxMove_D = 0f;
}

[Serializable]
public class RestrictRotate
{
    [Tooltip("最小角度")]
    [Range(1f, 90f)]
    public float minAngle = 10f;
    [Tooltip("最大角度")]
    [Range(1f, 180f)]
    public float maxAngle = 170f;
}

[Serializable]
public class RestrictCameraRotate
{
    public bool allowPitch = true;

    [Tooltip("俯仰角最小角度")]
    public float minAngle_P = -80f;
    [Tooltip("俯仰角最大角度")]
    public float maxAngle_P = 80f;

    public bool allowYaw = true;

    [Tooltip("偏航角最小角度")]
    public float minAngle_Y = -180f;
    [Tooltip("偏航角最大角度")]
    public float maxAngle_Y = 180f;
}

[Serializable]
public class RestrictScale
{
    [Tooltip("最小缩放比例")]
    public float minScale = 0.5f;
    [Tooltip("最大缩放比例")]
    public float maxScale = 2.5f;
}

[Serializable]
public class RestrictCameraZoom
{
    [Tooltip("最小缩进距离")]
    public float minDistance = 1f;
    [Tooltip("最大缩进距离")]
    public float maxDistance = 20f;
}

/// <summary>
/// 模型高亮
/// </summary>
[Serializable]
public class ModelHighlight
{
    [Tooltip("高亮节点")]
    public Transform highlightNode;

    [Tooltip("高亮宽度")]
    public float outlineWidth = 0.15f;

    [Tooltip("渲染模式")]
    public bool constantWidth = false;

    [Tooltip("可见性")]
    public HighlightPlus.Visibility visibility = HighlightPlus.Visibility.AlwaysOnTop;
}

/// <summary>
/// 模型虚影
/// </summary>
[Serializable]
public class ModelGhost
{
    [Tooltip("虚影节点")]
    public Transform ghostNode;
}