using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using UnityFramework.Runtime;

/// <summary>
///   存在三个控制旋转的方法
///   1.滑动改变朝向
///   2.点击一个位置检测到物体 朝向该物体
///   3.AimAtTarget 指向任务流程关注点
///   自动导航时不可操作打断移动，因为该移动的回调中包含走到对应位置后的事件
/// </summary>
public class PlayerController : MonoBase
{
    /// <summary>
    /// 相机最远缩放距离
    /// </summary>
    public float cameraMaxDistance;
    /// <summary>
    /// 相机最近缩放距离
    /// </summary>
    public float cameraMinDistance;
    /// <summary>
    /// 相机移动动画时长
    /// </summary>
    public float cameraMoveDuration;
    /// <summary>
    /// 相机旋转动画时长
    /// </summary>
    public float cameraRotateDuration;
    /// <summary>
    /// 模型移动动画时长
    /// </summary>
    public float modelMoveDuration;
    /// <summary>
    /// 模型旋转动画时长
    /// </summary>
    public float modelRotateDuration;
    /// <summary>
    /// 镜头移动系数
    /// </summary>
    public float moveSensitivity = 4;
    /// <summary>
    /// 镜头旋转系数
    /// </summary>
    public float rotateSensitivity = 1;
    /// <summary>
    /// 镜头缩放系数
    /// </summary>
    public float zoomSensitivity = 2;
    /// <summary>
    /// 相机避障检测层（墙体、地板等场景几何体）
    /// </summary>
    public LayerMask cameraObstacleMask = -1;
    /// <summary>
    /// 检测到遮挡时相机距碰撞面的回退距离
    /// </summary>
    public float cameraCollisionOffset = 0.3f;

    [HideInInspector]
    public bool isFirstPerson
    {
        get;
        private set;
    }
    public Transform CameraFollowPoint
    {
        get
        {
            if (isFirstPerson)
                return firstCameraFollowPoint;
            return cameraFollowPoint;
        }
    }
    public Transform Model => model;
    public Tweener ModelRotateTween => modelRotateFollow;
    public Tweener ModelFollowTween => modelPositionFollow;

    /// <summary>
    /// 将相机归位到跟随点，追加到外部 Sequence
    /// </summary>
    public void AppendCameraReturn(Sequence seq, float duration, Ease ease)
    {
        Transform fp = CameraFollowPoint;
        seq.Append(mainCamera.DOMove(fp.position, duration).SetEase(ease));
        seq.Join(mainCamera.DORotate(fp.eulerAngles, duration).SetEase(ease));
    }

    #region Private
    private Transform mainCamera;
    private float tempFloat;
    private Vector3 tempVector3;
    //缓存速度系数，避免每帧 PlayerPrefs.GetFloat
    public float cachedRotateSpeed;
    public float cachedMoveSpeed;

    private Transform verticalPoint;
    private Transform cameraFollowPoint;
    private Transform firstCameraFollowPoint;

    private Transform model;
    private Animator animator;
    private Tweener modelRotateFollow;
    private Tweener modelPositionFollow;
    private CharacterController controller;
    private bool wasCameraAway;
    /// <summary>
    /// 相机交给其他模式时，延迟隐藏模型的时长（让相机先移开，避免角色消失过程暴露在视角中）
    /// </summary>
    private float hideModelDelay = 0.3f;
    #endregion

    private void Awake()
    {
        isFirstPerson = true;
        RefreshSpeedCache();
        AddMsg(new ushort[]{
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.StartExecute,
            (ushort)SmallFlowModuleEvent.CompleteExecute,
            (ushort)SmallFlowModuleEvent.CompleteStep
        });

        mainCamera = Camera.main.transform;
        controller = GetComponent<CharacterController>();
        InitNavigation();

        verticalPoint = this.FindChildByName("VerticalPoint");
        cameraFollowPoint = this.FindChildByName("CameraFollowPoint");

        firstCameraFollowPoint = new GameObject("FirstCameraFollowPoint").transform;
        firstCameraFollowPoint.parent = verticalPoint;
        firstCameraFollowPoint.localPosition = Vector3.zero;
        firstCameraFollowPoint.localEulerAngles = Vector3.zero;

        model = this.FindChildByName("Model");
        {
            model.parent = transform.parent;

            animator = model.GetComponentInChildren<Animator>();
            //animator.keepAnimatorStateOnDisable = true;

            modelRotateFollow = model.DORotate(Vector3.up * Vector3.SignedAngle(Vector3.back, model.position - transform.position, Vector3.up), modelRotateDuration).SetLoops(-1).SetAutoKill(false);

            modelPositionFollow = model.DOMove(transform.position, modelMoveDuration).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
            {
                if (Vector3.Distance(transform.position, model.position) > 0.01f)
                {
                    animator.SetBool("isMove", true);
                    modelRotateFollow.ChangeEndValue(Vector3.up * Vector3.SignedAngle(Vector3.back, model.position - transform.position, Vector3.up), modelRotateDuration, true);
                }
                else
                {
                    animator.SetBool("isMove", false);
                    modelRotateFollow.ChangeEndValue(transform.eulerAngles, modelRotateDuration, true);
                }

                modelPositionFollow.ChangeEndValue(transform.position, modelMoveDuration, true);
            });
        }

        //GetComponent<NavMeshAgent>().enabled = false;
    }
    /// <summary>
    /// 控制旋转
    /// </summary>
    private void Rotate()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (rotateJoystick is VariableJoystick vj && VariableJoystick.tapRotationTriggered)
        {
            if (!hasTapTarget)
            {
                Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(vj.TapScreenPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                    tapTargetPoint = hit.point;
                else
                    tapTargetPoint = ray.GetPoint(100f);
                hasTapTarget = true;
            }
            RotateTowardsTarget();
        }
        else
        {
            hasTapTarget = false;

            //横向轴
            transform.localEulerAngles += Vector3.up * rotateJoystick.Horizontal * GlobalInfo.baseRotateSpeed * Time.deltaTime * cachedRotateSpeed;

            // 计算新的旋转角度
            tempFloat = verticalPoint.localEulerAngles.x - rotateJoystick.Vertical * GlobalInfo.baseRotateSpeed * Time.deltaTime * cachedRotateSpeed;

            // 将角度转换到[-180, 180]范围
            if (tempFloat > 180)
            {
                tempFloat -= 360;
            }

            // 限制在(-90, 90)度范围（不包含90度）
            float clampedAngle = Mathf.Clamp(tempFloat, -89.999f, 89.999f);

            // 应用限制后的角度
            verticalPoint.localEulerAngles = new Vector3(clampedAngle, 0f, 0f);
        }
#else
        //横向轴
        transform.localEulerAngles += Vector3.up * Input.GetAxis("Mouse X") * GlobalInfo.baseRotateSpeed * Time.deltaTime * cachedRotateSpeed;

        //纵向轴
        {
            tempFloat = verticalPoint.localEulerAngles.x - Input.GetAxis("Mouse Y") * GlobalInfo.baseRotateSpeed * Time.deltaTime * cachedRotateSpeed;

            if (tempFloat > 180)
            {
                tempFloat -= 360;
            }

            verticalPoint.localEulerAngles = Vector3.right * tempFloat;
        }
#endif
    }

    /// <summary>
    /// 以默认旋转速度转向点击目标点
    /// </summary>
    private void RotateTowardsTarget()
    {
        float rotSpeed = GlobalInfo.baseRotateSpeed * Time.deltaTime * cachedRotateSpeed;

        // 水平旋转（Y轴）
        Vector3 flatDir = new Vector3(tapTargetPoint.x - transform.position.x, 0, tapTargetPoint.z - transform.position.z);
        if (flatDir.sqrMagnitude > 0.001f)
        {
            float targetY = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
            float deltaY = Mathf.DeltaAngle(transform.localEulerAngles.y, targetY);
            if (Mathf.Abs(deltaY) > 0.01f)
            {
                float step = Mathf.Sign(deltaY) * rotSpeed;
                if (Mathf.Abs(step) >= Mathf.Abs(deltaY))
                    step = deltaY;
                transform.localEulerAngles += Vector3.up * step;
            }
        }

        // 垂直旋转（X轴），在transform本地坐标系中计算
        Vector3 localDir = transform.InverseTransformDirection(tapTargetPoint - verticalPoint.position);
        float horizDist = new Vector2(localDir.x, localDir.z).magnitude;
        if (horizDist > 0.001f)
        {
            float targetPitch = -Mathf.Atan2(localDir.y, horizDist) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, -89.999f, 89.999f);

            float currentPitch = verticalPoint.localEulerAngles.x;
            if (currentPitch > 180) currentPitch -= 360;

            float deltaPitch = targetPitch - currentPitch;
            if (Mathf.Abs(deltaPitch) > 0.01f)
            {
                float step = Mathf.Sign(deltaPitch) * rotSpeed;
                if (Mathf.Abs(step) >= Mathf.Abs(deltaPitch))
                    step = deltaPitch;
                verticalPoint.localEulerAngles = new Vector3(currentPitch + step, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// 将相机/角色朝向对准世界坐标目标（一次性转向），并取消点击锁定视角。
    /// 复用 RotateTowardsTarget 的偏航/俯仰角度公式，相机靠 CameraFollow 自动跟随。
    /// </summary>
    /// <param name="targetPos">目标世界坐标</param>
    /// <param name="duration">转向时长，负值时取 cameraRotateDuration</param>
    public void AimAtTarget(Vector3 targetPos, float duration = -1f)
    {
        if (duration < 0f)
            duration = cameraRotateDuration;

        // 取消点击屏幕锁定视角
        VariableJoystick.tapRotationTriggered = false;

            // 机身水平偏航(Y)
        Vector3 flatDir = new Vector3(targetPos.x - transform.position.x, 0f, targetPos.z - transform.position.z);
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            float targetY = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
            transform.DORotate(new Vector3(0f, targetY, 0f), duration);
        }

        // 俯仰(X) 挂在 verticalPoint
        Vector3 dir = targetPos - verticalPoint.position;
        float horizDist = new Vector2(dir.x, dir.z).magnitude;
        if (horizDist > 0.0001f)
        {
            float targetPitch = Mathf.Clamp(-Mathf.Atan2(dir.y, horizDist) * Mathf.Rad2Deg, -89.999f, 89.999f);
            verticalPoint.DOLocalRotate(new Vector3(targetPitch, 0f, 0f), duration);
        }
    }

    private float mVertical;
    private float mHorizontal;
    /// <summary>
    /// 控制移动
    /// </summary>
    private void Move()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (moveJoystick.Vertical == 0 && moveJoystick.Horizontal == 0)
            return;
        if (agent.isOnNavMesh)
        {
            agent.Move(((moveJoystick.Vertical * transform.forward) + (moveJoystick.Horizontal * transform.right)) * GlobalInfo.baseMoveSpeed * Time.deltaTime * cachedMoveSpeed);
        }
        else
        {
            controller.SimpleMove(((moveJoystick.Vertical * transform.forward) + (moveJoystick.Horizontal * transform.right)) * GlobalInfo.baseMoveSpeed * Time.deltaTime * cachedMoveSpeed);
        }
#else
        mVertical = Input.GetAxis("Vertical");
        mHorizontal = Input.GetAxis("Horizontal");
        if (mVertical == 0 && mHorizontal == 0)
            return;
        if (agent.isOnNavMesh)
        {
            agent.Move((mVertical * transform.forward + mHorizontal * transform.right) * GlobalInfo.baseMoveSpeed * Time.deltaTime * cachedMoveSpeed);
        }
        else
        {
            controller.Move((mVertical * transform.forward + mHorizontal * transform.right) * GlobalInfo.baseMoveSpeed * Time.deltaTime * cachedMoveSpeed);
        }
#endif
    }
    /// <summary>
    /// 控制缩放
    /// </summary>
    private void Zoom()
    {
#if UNITY_ANDROID || UNITY_IOS
#else
        tempVector3 = cameraFollowPoint.localPosition;
        tempVector3.z = Mathf.Clamp(tempVector3.z + Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity, cameraMinDistance, cameraMaxDistance);

        if (!isFirstPerson && tempVector3.z == cameraMaxDistance)
        {
            FirstPerson();
        }
        else if (isFirstPerson && tempVector3.z != cameraMaxDistance)
        {
            ThirdPerson();
        }

        cameraFollowPoint.localPosition = tempVector3;
#endif
    }

    private void FirstPerson()
    {
        isFirstPerson = true;
        UpdateModelVisibility();
    }

    private void ThirdPerson()
    {
        isFirstPerson = false;
        modelPositionFollow.ChangeStartValue(transform.position);
        this.WaitTime(0.1f, () =>
        {
            if (model != null)
                UpdateModelVisibility();
        });
    }

    private void UpdateModelVisibility()
    {
        if (model == null) return;
        bool cameraAway = GlobalInfo.SysPopup || ModelManager.Instance.CameraDotween;
        model.gameObject.SetActive(!isFirstPerson && !cameraAway);
    }

    #region 人称切换
    private Vector3 cameraFollowPosition;

    public void ToLast()
    {
        if(isFirstPerson)
            FirstPerson();
        else
            ThirdPerson();
    }

    public void ToFirst()
    {
        FirstPerson();
    }

    public void ToThird()
    {
        ThirdPerson();
    }
    #endregion

    /// <summary>
    /// 刷新速度缓存（设置变更时调用）
    /// </summary>
    public void RefreshSpeedCache()
    {
        cachedRotateSpeed = PlayerPrefs.GetFloat(GlobalInfo.rotateSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);
        cachedMoveSpeed = PlayerPrefs.GetFloat(GlobalInfo.moveSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);
    }

    private void OnEnable()
    {
        RefreshSpeedCache();
        UpdateCameraFollowValue();
    }

    private void UpdateCameraFollowValue()
    {
        Transform followPoint = isFirstPerson ? firstCameraFollowPoint : cameraFollowPoint;
        mainCamera.position = followPoint.position;
        mainCamera.rotation = followPoint.rotation;
    }

    float t;
    private void LateUpdate()
    {
        //检测相机是否被其他代码调度（DOTween动画/SysPopup），切换时更新模型显隐
        bool cameraAway = GlobalInfo.SysPopup || ModelManager.Instance.CameraDotween;
        if (cameraAway != wasCameraAway)
        {
            wasCameraAway = cameraAway;
            if (cameraAway)
            {
                // 相机被其他模式接管时，延迟隐藏模型，让相机先移开角色位置
                this.WaitTime(hideModelDelay, () =>
                {
                    if (wasCameraAway && model != null)
                        UpdateModelVisibility();
                });
            }
            else
            {
                // 相机归还时立即显示模型（提前于相机归位）
                UpdateModelVisibility();
            }
        }

        //导航的相机跟随不受其他条件影响
        if (isNavigating)
        {
            CameraFollow();
            if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
            {
                EndNavigation(navSnapToTarget ? targetPoint : null);
            }
        }

        //CameraDotween: 观察/聚焦等行为由DOTween控制相机位置，不可跟随
        if (GlobalInfo.SysPopup || ModelManager.Instance.CameraDotween)
            return;

        //弹窗但非系统弹窗：光标解锁，阻断输入但相机仍需跟随角色到新位置
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (!isNavigating)
                CameraFollow();
            return;
        }
#if UNITY_ANDROID || UNITY_IOS
        if (rotateJoystick == null)
            return;
#endif

        if (!isNavigating)
        {
            Zoom();
            Rotate();
            Move();
        }

        CameraFollow();
    }

    // 跟随角色(强制使用玩家跟随点)
    void CameraFollow()
    {
        t = 1f / cameraMoveDuration * Time.deltaTime;
        if (isFirstPerson)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.position, firstCameraFollowPoint.position, t);
            mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, firstCameraFollowPoint.rotation, t);
        }
        else
        {
            Vector3 desiredPos = cameraFollowPoint.position;
            Vector3 origin = verticalPoint.position;
            Vector3 dir = desiredPos - origin;
            float dist = dir.magnitude;
            if (dist > 0.01f && Physics.Raycast(origin, dir / dist, out RaycastHit hit, dist, cameraObstacleMask))
            {
                desiredPos = hit.point + hit.normal * cameraCollisionOffset;
            }
            mainCamera.position = Vector3.Lerp(mainCamera.position, desiredPos, t);
            mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, cameraFollowPoint.rotation, t);
        }
    }

    public bool NavPathComplete => isNavigating && !agent.pathPending && agent.remainingDistance < agent.stoppingDistance;
    public bool NavEnd => !isNavigating && !inAnime;

    #region 导航部分
    private NavMeshAgent agent;
    public NavMeshAgent Agent => agent;
    private bool isNavigating;
    private Transform targetPoint;
    private bool inAnime;
    // 到达后是否将角色位移并旋转吸附到目标姿态（姿态标记点为 true；对准可操作对象时为 false）
    private bool navSnapToTarget = true;

    private void InitNavigation()
    {
        agent = transform.AutoComponent<NavMeshAgent>();
        agent.stoppingDistance = 1f;
        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            //moveSensitivity = 4f;
            controller.enabled = true;
        }
    }
    public void StartNavigation(Transform target)
    {
        StartNavigation(target, true);
    }

    /// <summary>
    /// 开始导航到目标。
    /// </summary>
    /// <param name="target">目标点</param>
    /// <param name="snapToTarget">到达后是否将角色位移/旋转吸附到目标姿态。
    /// 姿态标记点传 true；仅靠近可操作对象时传 false（不贴到物体上）</param>
    public void StartNavigation(Transform target, bool snapToTarget)
    {
        //GetComponent<NavMeshAgent>().enabled = true;

        if (agent.SetDestination(target.position))
        {
            targetPoint = target;
            navSnapToTarget = snapToTarget;
            isNavigating = true;
        }
        //else
        //{
        //    GetComponent<NavMeshAgent>().enabled = false;
        //}
    }
    public void EndNavigation(Transform target = null, float duration = 0.5f)
    {
        if (inAnime)
        {
            return;
        }

        if (isNavigating && agent.enabled)
        {
            agent.ResetPath();
            verticalPoint.localEulerAngles = new Vector3(verticalPoint.localEulerAngles.x, 0, 0);
        }

        if (target)
        {
            inAnime = true;

            float moveOverTime = 0.5f / agent.speed;
            duration = moveOverTime > duration ? moveOverTime : duration;

            transform.DOMove(target.position, duration);
            verticalPoint.DOLocalRotate(Vector3.zero, duration);

            transform.DORotate(target.eulerAngles, duration).OnComplete(() =>
            {
                if (isNavigating)
                {
                    isNavigating = false;
                    //GetComponent<NavMeshAgent>().enabled = false;
                }

                inAnime = false;
            });
        }
        else if (isNavigating)
        {
            isNavigating = false;
            //GetComponent<NavMeshAgent>().enabled = false;
        }
    }
    #endregion


    private Joystick moveJoystick;
    private Joystick rotateJoystick;


    // 点击旋转目标点
    private Vector3 tapTargetPoint;
    public bool hasTapTarget;


    public void SetJoystick(Joystick moveJoystick, Joystick rotateJoystick)
    {
        this.moveJoystick = moveJoystick;
        this.rotateJoystick = rotateJoystick;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.StartExecute:
                ModelFollowTween.Pause();
                ModelRotateTween.Pause();
                Model.transform.SetParent(transform);
                Model.transform.localPosition = Vector3.zero;
                Model.transform.localEulerAngles = Vector3.zero;
                break;
            case (ushort)SmallFlowModuleEvent.SelectFlow:
            case (ushort)SmallFlowModuleEvent.SelectStep:
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                Model.transform.SetParent(transform.parent);
                ModelFollowTween.ChangeStartValue(transform.position);
                ModelFollowTween.ChangeEndValue(transform.position);
                ModelRotateTween.ChangeStartValue(transform.eulerAngles);
                ModelRotateTween.ChangeEndValue(transform.eulerAngles);
                ModelFollowTween.Play();
                ModelRotateTween.Play();
                break;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (modelPositionFollow != null)
            modelPositionFollow.Kill();
        if (modelRotateFollow != null)
            modelRotateFollow.Kill();
    }

}