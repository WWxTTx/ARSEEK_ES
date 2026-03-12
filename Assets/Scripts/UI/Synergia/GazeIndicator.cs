using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityFramework.Runtime;

/// <summary>
/// 房间成员同步
/// 动画、拆解百科：位置、视线标记
/// 模拟操作百科：角色
/// </summary>
public class GazeIndicator : MonoBase
{
    /// <summary>
    /// 房间成员信息
    /// </summary>
    private int userId;
    private string userDevice;
    private Color color;

    /// <summary>
    /// 射线相关字段
    /// </summary>
    private Transform start;
    private SpriteRenderer Arrow;
    private RectTransform InfoPanel;
    private Transform Device;
    private Text Name;
    private SpriteRenderer MapIcon;
    private LookAtTagert lookAtTagert;
    /// <summary>
    /// 视线在物体表面射击点
    /// </summary>
    private Transform end;
    /// <summary>
    /// 设计点图标
    /// </summary>
    private SpriteRenderer endSprite;

    /// <summary>
    /// 射线
    /// </summary>
    private LineRenderer Line;
    private float lineWidth = 0.015f;
    private float minDistance = 2f;
    private float maxDistance = 5f;
    private Material material;
    private float tileMaterialScale = 6;
    /// <summary>
    /// 射线长度限制
    /// </summary>
    private float minValue;
    private float maxValue;

    /// <summary>
    /// 角色模型相关字段
    /// </summary>
    public GameObject PlayerPrefab;
    private GameObject model;
    private Animator modelAnimator;
    private float baseHeight;
    /// <summary>
    /// 角色模型跟随动画
    /// </summary>
    private Tweener modelRotateFollow;
    private Tweener modelPositionFollow;
    private Vector3 offset = new Vector3(0, 2.1f, 0);
    private Vector3 targetPosition;
    private Vector3 targetEuler;

    /// <summary>
    /// 模型根节点
    /// </summary>
    private Transform target;
    /// <summary>
    /// 是否显示射线
    /// </summary>
    private bool showLine = true;

    /// <summary>
    /// 是否显示为角色模型
    /// </summary>
    private bool ShowPlayer = false;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="id"></param>
    public void Init(int id)
    {
        userId = id;
        color = NetworkManager.Instance.GetPlayerColor(userId);
        userDevice = NetworkManager.Instance.GetUserDevice(id);

        InitVariables();

        //根据配置设置有无漫游模式
        ShowPlayer = GlobalInfo.hasRole;

        Name.text = NetworkManager.Instance.GetUserName(id);
        //Name.color = color;
        foreach (Transform icon in Device)
        {
            if (icon.name.Equals(userDevice))
            {
                icon.GetComponent<Image>().color = color;
                icon.gameObject.SetActive(true);
            }
            else
            {
                icon.GetComponent<Image>().color = Color.white;
                icon.gameObject.SetActive(false);
            }
        }

        //根据百科类型初始化角色模型或射线
        if (ShowPlayer)
            SetPlayer();
        else
        {
            SetLine();

            //拆解百科成员选中模型时隐藏射线
            SelectionModel selectionModel = ModelManager.Instance.modelGo?.GetComponent<SelectionModel>();
            if (selectionModel)
            {
                ShowLine(selectionModel.GetUserSelectedGo(id) == null);
            }
        }
    }

    private void InitVariables()
    {
        target = ModelManager.Instance.modelRoot;

        start = transform.FindChildByName("start");
        Arrow = transform.GetComponentByChildName<SpriteRenderer>("Arrow");
        Line = transform.GetComponentByChildName<LineRenderer>("Line");
        Line.startWidth = lineWidth;
        Line.endWidth = lineWidth;
        InfoPanel = transform.GetComponentByChildName<RectTransform>("InfoPanel");
        end = transform.FindChildByName("end");
        Device = transform.FindChildByName("Device");
        Name = transform.GetComponentByChildName<Text>("Name");
        MapIcon = transform.GetComponentByChildName<SpriteRenderer>("MapIcon");

        lookAtTagert = GetComponentInChildren<LookAtTagert>();
        lookAtTagert.target = Camera.main.GetComponentInChildren<Camera>().transform;

        minValue = int.MaxValue;
        maxValue = int.MinValue;
    }

    /// <summary>
    /// 设置射线显示属性 颜色等
    /// </summary>
    private void SetLine()
    {
        Arrow.color = color;
        Arrow.gameObject.SetActive(true);

        if (material == null)
            material = Line.material;
        material.SetColor("_BaseColor", color);

        if (endSprite == null)
        {
            endSprite = end.GetComponent<SpriteRenderer>();
        }
        endSprite.color = color;
    }

    /// <summary>
    /// 设置角色 实例化等
    /// </summary>
    private void SetPlayer()
    {
        InfoPanel.anchoredPosition3D = Vector3.zero;
        InfoPanel.eulerAngles = 180f * Vector3.up;
        InfoPanel.localScale = 0.001f * Vector3.one;

        model = Instantiate(PlayerPrefab, start);
        model.transform.localPosition = -offset;
        modelAnimator = model.GetComponent<Animator>();

        baseHeight = ModelManager.Instance.GetCameraSyncHeight();

        MapIcon.color = color;
        MapIcon.gameObject.SetActive(true);

        modelRotateFollow = start.DORotate(Vector3.up * Vector3.SignedAngle(Vector3.back, start.position - targetPosition, Vector3.up), 0.25f).SetLoops(-1).SetAutoKill(false);
        modelPositionFollow = start.DOMove(targetPosition, 0.25f).SetLoops(-1).SetEase(Ease.Linear).SetAutoKill(false).OnUpdate(() =>
        {
            if (Vector3.Distance(targetPosition, start.position) > 0.01f)
            {
                modelAnimator.SetBool("isMove", true);
                modelRotateFollow.ChangeEndValue(Vector3.up * Vector3.SignedAngle(Vector3.back, start.position - targetPosition, Vector3.up), 0.25f, true);
            }
            else
            {
                modelAnimator.SetBool("isMove", false);
                modelRotateFollow.ChangeEndValue(targetEuler, 0.25f, true);
            }
            modelPositionFollow.ChangeEndValue(targetPosition, 0.25f, true);
        });

        // TODO 互相推挤
        //var navObstacle = model.AutoComponent<NavMeshObstacle>();
        //navObstacle.radius = 0.2f;
        //navObstacle.height = 1.8f;
        //navObstacle.carving = true;
        //navObstacle.carveOnlyStationary = true;
        //navObstacle.carvingTimeToStationary = 0.5f;
        //navObstacle.carvingMoveThreshold = 0.1f;
    }

    /// <summary>
    /// 更新成员位置
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rot"></param>
    public void UpdatePose(Vector3 position, Vector4 rot)
    {
        if (target == null || target.childCount == 0)
            return;

        Quaternion rotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);
        if (ShowPlayer)
        {
            SetPlayerPose(target.transform.TransformPoint(position), target.transform.rotation * rotation);
        }
        else
        {
            SetLinePose(target.transform.TransformPoint(position), target.transform.rotation * rotation);
        }
    }

    /// <summary>
    /// 设置位置和方向
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="rotation"></param>
    private void SetPlayerPose(Vector3 startPoint, Quaternion rotation)
    {
        //targetPosition = startPoint + offset;
        //targetEuler = rotation.eulerAngles;
        targetPosition = startPoint;
        //todo 0519
        //targetPosition.y = baseHeight + offset.y;//临时处理VR
        targetPosition.y += offset.y;
        targetEuler = rotation.eulerAngles.y * Vector3.up;
    }

    /// <summary>
    /// 设置位置和视线方向
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="rotation"></param>
    private void SetLinePose(Vector3 startPoint, Quaternion rotation)
    {
        startPoint = ClampDistance(startPoint);
        start.position = startPoint;
        start.rotation = rotation;

        Line.positionCount = 2;
        Line.SetPosition(0, startPoint);

        if (Physics.Raycast(startPoint, start.forward, out RaycastHit hit, 10))
        {
            end.position = hit.point;
            end.rotation = Quaternion.LookRotation(hit.normal);
            end.gameObject.SetActive(showLine);
            Line.SetPosition(1, hit.point);
            UpdateLine();
        }
        else
        {
            end.gameObject.SetActive(false);
            Line.SetPosition(1, startPoint + start.forward * 10);
            UpdateLine();
        }
    }
    /// <summary>
    /// 限制射线长度
    /// </summary>
    /// <param name="startPoint"></param>
    /// <returns></returns>
    private Vector3 ClampDistance(Vector3 startPoint)
    {
        float distance = Vector3.Distance(startPoint, target.position);
        if (distance < minValue)
            minValue = distance;
        if (distance > maxValue)
            maxValue = distance;

        float scaleDistance = minValue == maxValue ? 1f : (distance - minValue) / (maxValue - minValue);
        distance = minDistance + scaleDistance * (maxDistance - minDistance);

        Vector3 dir = (startPoint - target.position).normalized;
        startPoint = target.position + dir * distance;

        return startPoint;
    }

    /// <summary>
    /// 更新射线显示
    /// </summary>
    private void UpdateLine()
    {
        //https://forum.unity.com/threads/urp-lit-possible-to-modify-texture-tiling-offset-without-new-instances.1194931/
        if (material == null)
            material = Line.material;
        material.SetTextureScale("_BaseMap", new Vector2(Vector3.Distance(start.transform.position, end.transform.position) * tileMaterialScale, 1));
    }

    /// <summary>
    /// 控制射线显隐
    /// </summary>
    /// <param name="show"></param>
    public void ShowLine(bool show)
    {
        showLine = show && GlobalInfo.currentBaikeType != BaikeType.SmallScene;
        Line.gameObject.SetActive(show);
        end.gameObject.SetActive(show);
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            default:
                break;
        }
    }
}