using UnityEngine;

/// <summary>
/// 模型限制
/// </summary>
public partial class ModelRestrict : MonoBehaviour
{
    public CameraMoveType moveType = CameraMoveType.Pan;
    public RestrictCameraMove restrictCameraMove;

    public CameraRotateType rotateType = CameraRotateType.RotateAround;
    public RestrictCameraRotate restrictCameraRotate;

    public CameraZoomType zoomType = CameraZoomType.Pivot;
    public RestrictCameraZoom restrictCameraZoom;

    public ModelHighlight modelHighlight;

    public ModelGhost modelGhost;
}