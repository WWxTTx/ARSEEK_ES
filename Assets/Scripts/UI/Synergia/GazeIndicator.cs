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
    //private Transform end;
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

    /// <summary>
    /// 角色模型导航相关
    /// </summary>
    private NavMeshAgent agent;
    private float moveCoefficient;
    private float destinationThreshold = 0.5f;
    private Vector3 lastNavTarget;
    private Vector3 targetPosition;
    private Vector3 targetEuler;
    private float lastAppliedEulerY;
    private float rotationDeadzone = 10f;
    private Vector3 lastPosition;
    private CharacterGhost cachedGhost;

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
        //end = transform.FindChildByName("end");
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

        //if (endSprite == null)
        //{
        //    endSprite = end.GetComponent<SpriteRenderer>();
        //}
        //endSprite.color = color;
    }

    /// <summary>
    /// 设置角色 实例化等
    /// </summary>
    private void SetPlayer()
    {
        model = Instantiate(PlayerPrefab, start);
        model.transform.localPosition = Vector3.zero;
        modelAnimator = model.GetComponent<Animator>();

        InfoPanel.anchoredPosition3D = new Vector3(0, 2.1f, 0);
        InfoPanel.eulerAngles = 180f * Vector3.up;
        InfoPanel.localScale = 0.001f * Vector3.one;

        MapIcon.color = color;
        MapIcon.gameObject.SetActive(true);

        // NavMeshAgent 导航配置 — 速度对齐 PlayerController 的 baseMoveSpeed * 设置系数
        moveCoefficient = PlayerPrefs.GetFloat(GlobalInfo.moveSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);
        float moveSpeed = GlobalInfo.baseMoveSpeed * moveCoefficient;
        float rotateCoefficient = PlayerPrefs.GetFloat(GlobalInfo.rotateSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);
        agent = start.gameObject.AutoComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = GlobalInfo.baseRotateSpeed * rotateCoefficient;
        agent.acceleration = GlobalInfo.baseMoveSpeed * moveCoefficient;
        agent.stoppingDistance = 0.1f;
        agent.radius = 0.01f;
        agent.height = 1.8f;
        agent.avoidancePriority = 0;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        lastNavTarget = start.position;
        lastPosition = start.position;

        // CharacterGhost 在实例化的 model (PlayerPrefab) 上，不在 GazeIndicator 根节点
        cachedGhost = model != null ? model.GetComponent<CharacterGhost>() : null;
        if (cachedGhost == null)
            cachedGhost = GetComponentInChildren<CharacterGhost>();

        if (cachedGhost != null)
        {
            cachedGhost.positionSource = start;
            OverlapDetection.UnregisterCharacter(cachedGhost.transform);
            OverlapDetection.RegisterCharacter(start);
        }
    }

    private void Update()
    {
        if (!ShowPlayer || modelAnimator == null)
            return;

        // 根据实际位移驱动动画（与 PlayerController 一致），避免依赖 agent.velocity 的滞后/阈值问题
        Vector3 displacement = start.position - lastPosition;
        displacement.y = 0;
        bool hasMoved = displacement.sqrMagnitude > 0.0001f;

        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            // 到达目标点后强制停止，避免 NavMeshAgent 在终点震荡
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                agent.velocity = Vector3.zero;

            modelAnimator.SetBool("isMove", hasMoved);

            if (hasMoved)
            {
                if (agent.velocity.sqrMagnitude > 0.01f)
                {
                    Vector3 moveDir = agent.velocity.normalized;
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    start.rotation = Quaternion.Slerp(start.rotation, targetRot, Time.deltaTime * 10f);
                    lastAppliedEulerY = start.rotation.eulerAngles.y;
                }
            }
            else
            {
                float targetY = targetEuler.y;
                float delta = Mathf.DeltaAngle(lastAppliedEulerY, targetY);
                if (Mathf.Abs(delta) > rotationDeadzone)
                {
                    lastAppliedEulerY = Mathf.MoveTowardsAngle(lastAppliedEulerY, targetY, Mathf.Abs(delta) * Time.deltaTime * 8f);
                    start.rotation = Quaternion.Euler(0, lastAppliedEulerY, 0);
                }
                else if (agent.remainingDistance <= agent.stoppingDistance || (!agent.hasPath && !agent.pathPending))
                {
                    if (Mathf.Abs(delta) > 0.1f)
                    {
                        lastAppliedEulerY = targetY;
                        start.rotation = Quaternion.Euler(0, lastAppliedEulerY, 0);
                    }
                }
            }
        }
        else
        {
            // Fallback: 非 NavMesh 时直线移动
            Vector3 toTarget = targetPosition - start.position;
            float dist = toTarget.magnitude;
            if (dist > 0.01f)
            {
                float step = GlobalInfo.baseMoveSpeed * moveCoefficient * Time.deltaTime;
                if (step > dist) step = dist;
                start.position += toTarget / dist * step;
                modelAnimator.SetBool("isMove", true);

                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                start.rotation = Quaternion.Slerp(start.rotation, targetRot, Time.deltaTime * 10f);
                lastAppliedEulerY = start.rotation.eulerAngles.y;
            }
            else
            {
                modelAnimator.SetBool("isMove", false);
                float targetY = targetEuler.y;
                float delta = Mathf.DeltaAngle(lastAppliedEulerY, targetY);
                if (Mathf.Abs(delta) > rotationDeadzone)
                {
                    lastAppliedEulerY = Mathf.MoveTowardsAngle(lastAppliedEulerY, targetY, Mathf.Abs(delta) * Time.deltaTime * 8f);
                    start.rotation = Quaternion.Euler(0, lastAppliedEulerY, 0);
                }
            }
        }

        // 位置变化时检测角色重叠——远端角色靠近别人时自己变透明
        if (hasMoved)
        {
            lastPosition = start.position;
            if (cachedGhost != null)
                cachedGhost.UpdateSelfGhost();
        }
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
        Vector3 worldPos = target.transform.TransformPoint(position);
        SetPlayerPose(worldPos, target.transform.rotation * rotation);
    }

    /// <summary>
    /// 设置位置和方向
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="rotation"></param>
    private void SetPlayerPose(Vector3 startPoint, Quaternion rotation)
    {
        targetPosition = startPoint;
        targetEuler = rotation.eulerAngles.y * Vector3.up;

        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            Vector3 navTarget = startPoint;
            bool hasArrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
            bool targetChanged = Vector3.Distance(navTarget, lastNavTarget) > 0.1f;
            bool jumpedFar = Vector3.Distance(navTarget, agent.destination) > destinationThreshold;

            // 第一次设置位置，或目标距离当前位置 > 阈值时，直接 Warp
            if (Vector3.Distance(agent.transform.position, navTarget) > 3f)  // 跨楼层距离
            {
                if (NavMesh.SamplePosition(navTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);  // 直接放到目标楼层的 NavMesh 上
                    lastNavTarget = hit.position;
                }
            }
            else
            {
                if (targetChanged && (hasArrived || jumpedFar))
                {
                    agent.SetDestination(navTarget);
                    lastNavTarget = navTarget;
                }
            }
        }

        // 位置更新后检测角色重叠——远端角色靠近别人时自己变透明
        if (cachedGhost != null)
            cachedGhost.UpdateSelfGhost();
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

        //if (Physics.Raycast(startPoint, start.forward, out RaycastHit hit, 10))
        //{
        //    end.position = hit.point;
        //    end.rotation = Quaternion.LookRotation(hit.normal);
        //    //end.gameObject.SetActive(showLine);
        //    Line.SetPosition(1, hit.point);
        //    UpdateLine();
        //}
        //else
        //{
        //    end.gameObject.SetActive(false);
        //    Line.SetPosition(1, startPoint + start.forward * 10);
        //    UpdateLine();
        //}
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
        //if (material == null)
        //    material = Line.material;
        //material.SetTextureScale("_BaseMap", new Vector2(Vector3.Distance(start.transform.position, end.transform.position) * tileMaterialScale, 1));
    }

    /// <summary>
    /// 控制射线显隐
    /// </summary>
    /// <param name="show"></param>
    public void ShowLine(bool show)
    {
        showLine = show && GlobalInfo.currentBaikeType != BaikeType.SmallScene;
        Line.gameObject.SetActive(show);
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