using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using UnityFramework.Runtime;
using System.Collections.Generic;

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
    public float cameraCollisionOffset = 0.5f;
    /// <summary>
    /// 避障球体半径（大于0可提前感知墙壁，过渡更平滑）
    /// </summary>
    public float cameraObstacleRadius = 0.2f;
    /// <summary>
    /// 避障后相机距玩家头部的最小距离（防止贴太近导致无法瞄准）
    /// </summary>
    public float cameraMinAvoidanceDist = 0.5f;
    /// <summary>
    /// 避障释放（相机后退）的速度（单位/秒），收缩瞬间完成不做限制
    /// </summary>
    public float cameraObstacleReleaseSpeed = 3f;

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
    public bool ModelFollowPaused => modelFollowPaused;

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
    private Transform cameraYawPivot;

    private Transform model;
    private Animator animator;
    private bool modelFollowPaused;
    private bool animationOverridden; // 操作流程执行时外部接管动画控制
    private CharacterController controller;
    private bool wasCameraAway;
    private float obstacleMaxDist;       // 狭窄区域相机最远距离上限，只减不增
    private bool obstacleTargetInit;

    /// <summary>
    /// 上一帧位置，用于判断角色是否在移动（驱动动画）
    /// </summary>
    private Vector3 lastPosition;
    /// <summary>
    /// 上次检测到位移的时间，0.2秒持久化避免动画闪烁
    /// </summary>
    private float lastMoveTime;
    /// <summary>
    /// 当前动画移动状态,避免每帧重复 SetBool
    /// </summary>
    private bool animIsMoving;
    /// <summary>
    /// 相机交给其他模式时，延迟隐藏模型的时长（让相机先移开，避免角色消失过程暴露在视角中）
    /// </summary>
    private float hideModelDelay = 0.3f;

    // 相机狭窄区域 debug
    #endregion

    private void Awake()
    {
        isFirstPerson = false;
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

        // 禁用角色间的物理碰撞
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            Physics.IgnoreLayerCollision(playerLayer, playerLayer, true);

        verticalPoint = this.FindChildByName("VerticalPoint");
        cameraFollowPoint = this.FindChildByName("CameraFollowPoint");

        firstCameraFollowPoint = this.FindChildByName("FirstCameraFollowPoint");

        cameraYawPivot = new GameObject("CameraYawPivot").transform;
        cameraYawPivot.parent = transform;
        cameraYawPivot.localPosition = Vector3.zero;
        cameraYawPivot.localEulerAngles = Vector3.zero;
        verticalPoint.parent = cameraYawPivot;
        if (cameraFollowPoint != null && cameraFollowPoint.parent == transform)
            cameraFollowPoint.parent = cameraYawPivot;

        model = this.FindChildByName("Model");
        {
            model.parent = transform.parent;
            animator = model.GetComponentInChildren<Animator>();
        }

    }
    /// <summary>
    /// 控制旋转
    /// </summary>
    private void Rotate()
    {
#if UNITY_ANDROID || UNITY_IOS
        // 用户主动旋转时打断自动瞄准
        if (isAiming && rotateJoystick != null &&
            (Mathf.Abs(rotateJoystick.Horizontal) > 0.01f || Mathf.Abs(rotateJoystick.Vertical) > 0.01f))
        {
            isAiming = false;
        }

        if (isAiming)
        {
            // 瞄准中由 UpdateAim 接管，不处理输入
        }
        else if (rotateJoystick is VariableJoystick vj && VariableJoystick.tapRotationTriggered)
        {
            // 立即消费点击事件，避免下帧重复触发
            VariableJoystick.tapRotationTriggered = false;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(vj.TapScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
                AimAtTarget(hit.point);
            else
                AimAtTarget(ray.GetPoint(100f));
        }
        else
        {
            //横向轴：滑动相机时角色朝向同步跟随（yaw 直接放 transform 上，相机挂在子下跟着转，
            //语义与 AimAtTarget 一致：偏航由 transform 承担，cameraYawPivot 始终归零）
            transform.eulerAngles += Vector3.up * rotateJoystick.Horizontal * GlobalInfo.baseRotateSpeed * Time.deltaTime * cachedRotateSpeed;

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
        //横向轴（仅旋转相机偏航，角色朝向由移动方向驱动）
        cameraYawPivot.localEulerAngles += Vector3.up * Input.GetAxis("Mouse X") * GlobalInfo.baseRotateSpeed * Time.deltaTime * cachedRotateSpeed;

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
    /// 点击屏幕和任务流程瞄准共用 AimAtTarget 的闭环收敛逻辑，详见下方。
    /// </summary>
    private bool isAiming;
    private Vector3 aimTargetPos;
    private float aimEndTime;
    // 到时后继续修正一小段，等待 CameraFollow 的插值把相机追平
    private const float aimHoldTime = 0.4f;

    /// <summary>
    /// 将相机朝向对准目标，并取消点击锁定视角。
    /// 相机挂在 cameraFollowPoint 下、且该点位于旋转半径外，旋转本身会把相机移到新位置，
    /// 因此不能一次算完角度就补间：这里改为在锁定过程中每帧用实时相机位置重算朝向（闭环收敛）。
    /// </summary>
    public void AimAtTarget(Transform targetPos)
    {
        if (targetPos != null) AimAtTarget(targetPos.position);
    }

    /// <param name="targetPos">目标世界坐标</param>
    public void AimAtTarget(Vector3 targetPos)
    {
        cameraYawPivot.DOKill();
        verticalPoint.DOKill();
        transform.DOKill();

        aimTargetPos = targetPos;
        aimEndTime = Time.time + 1;
        isAiming = true;
    }

    /// <summary>
    /// 锁定过程中的每帧修正：用当前相机跟随点位置重算目标 yaw/pitch，按指数衰减逼近（先快后慢）。
    /// </summary>
    private void UpdateAim()
    {
        if (!isAiming) return;

        // 偏航全部由 transform 承担，pivot 归零（与手动旋转的累加量对冲）
        cameraYawPivot.localEulerAngles = Vector3.zero;

        // 指数衰减：每帧消除剩余角度的固定比例，自然形成"先快后慢"。
        // decayRate=6 时，时间常数约 0.17 秒，1 秒内可消除 99.75% 的剩余距离。
        const float decayRate = 6f;
        float k = 1f - Mathf.Exp(-Time.deltaTime * decayRate);

        // 两次迭代：第一次用旧位置解算并应用，应用后子物体世界位置即刻更新，
        // 第二次用新位置修正，抵消"旋转改变了相机位置"带来的残差
        for (int i = 0; i < 2; i++)
        {
            if (!SolveAimAngles(out float targetYaw, out float targetPitch))
                break;

            float currYaw = transform.eulerAngles.y;
            float currPitch = NormalizeAngle(verticalPoint.localEulerAngles.x);

            float deltaY = Mathf.DeltaAngle(currYaw, targetYaw);
            float deltaPitch = Mathf.DeltaAngle(currPitch, targetPitch);

            // 剩余角度足够小时直接吸附，避免指数收敛永远到不了 100%
            if (Mathf.Abs(deltaY) < 0.1f && Mathf.Abs(deltaPitch) < 0.1f)
            {
                transform.eulerAngles = new Vector3(0f, targetYaw, 0f);
                verticalPoint.localEulerAngles = new Vector3(Mathf.Clamp(targetPitch, -89.999f, 89.999f), 0f, 0f);
            }
            else
            {
                transform.eulerAngles = new Vector3(0f, currYaw + deltaY * k, 0f);
                verticalPoint.localEulerAngles = new Vector3(Mathf.Clamp(currPitch + deltaPitch * k, -89.999f, 89.999f), 0f, 0f);
            }

            if (k >= 1f) continue; // 极端帧率下才需要第二次精修
            break;
        }

        if (Time.time > aimEndTime + aimHoldTime)
            isAiming = false;
    }

    /// <summary>
    /// 由实时相机位置反解 transform 偏航与 verticalPoint 俯仰。
    /// cameraFollowPoint 自带固定 local 欧拉偏移（如 -2° 偏航），需从期望相机朝向中扣除。
    /// </summary>
    private bool SolveAimAngles(out float targetYaw, out float targetPitch)
    {
        targetYaw = 0f;
        targetPitch = 0f;

        Vector3 camPos = cameraFollowPoint != null ? cameraFollowPoint.position : mainCamera.position;
        Vector3 toTarget = aimTargetPos - camPos;
        float horizDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        if (horizDist <= 0.0001f) return false;

        Vector3 cfpLocalEul = cameraFollowPoint != null ? cameraFollowPoint.localEulerAngles : Vector3.zero;
        float desiredCamYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float desiredCamPitch = Mathf.Clamp(-Mathf.Atan2(toTarget.y, horizDist) * Mathf.Rad2Deg, -89.999f, 89.999f);

        targetYaw = desiredCamYaw - NormalizeAngle(cfpLocalEul.y);
        targetPitch = desiredCamPitch - NormalizeAngle(cfpLocalEul.x);
        return true;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private float mVertical;
    private float mHorizontal;
    private int debugMoveFrame;
    /// <summary>
    /// 控制移动
    /// </summary>
    private void Move()
    {
        // 移动方向基于相机朝向（投影到水平面）
        Vector3 camForward = Vector3.ProjectOnPlane(cameraYawPivot.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraYawPivot.right, Vector3.up).normalized;

#if UNITY_ANDROID || UNITY_IOS
        if (moveJoystick.Vertical == 0 && moveJoystick.Horizontal == 0)
        {
            if (agent.isOnNavMesh)
                agent.velocity = Vector3.zero;
            return;
        }
        Vector3 moveDir = (moveJoystick.Vertical * camForward + moveJoystick.Horizontal * camRight).normalized;
        Vector3 desiredVelocity = moveDir * GlobalInfo.baseMoveSpeed * cachedMoveSpeed;
        if (agent.isOnNavMesh)
            agent.velocity = desiredVelocity;
        else
            controller.SimpleMove(desiredVelocity * Time.deltaTime);

        // 角色朝向移动方向（保持相机世界朝向不变）
        if (moveDir.sqrMagnitude > 0.01f)
        {
            float camWorldYaw = cameraYawPivot.eulerAngles.y;
            Quaternion targetBodyRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRot, Time.deltaTime * 10f);
            cameraYawPivot.localEulerAngles -= Vector3.up * (cameraYawPivot.eulerAngles.y - camWorldYaw);
        }
#else
        mVertical = Input.GetAxis("Vertical");
        mHorizontal = Input.GetAxis("Horizontal");
        if (mVertical == 0 && mHorizontal == 0)
        {
            if (agent.isOnNavMesh)
                agent.velocity = Vector3.zero;
            return;
        }

        debugMoveFrame++;
        if (debugMoveFrame % 60 == 0)
        {
            Debug.LogWarning($"[PlayerController] Move input — V={mVertical:F3} H={mHorizontal:F3}, agentOnNavMesh={agent.isOnNavMesh}, isNavigating={isNavigating}, agentVel={agent.velocity.magnitude:F3}, hasPath={agent.hasPath}, remainingDist={agent.remainingDistance:F3}, dest={agent.destination}");
        }
        Vector3 moveDir2 = (mVertical * camForward + mHorizontal * camRight).normalized;
        Vector3 desiredVelocity2 = moveDir2 * GlobalInfo.baseMoveSpeed * cachedMoveSpeed;
        if (agent.isOnNavMesh)
            agent.velocity = desiredVelocity2;
        else
            controller.Move(desiredVelocity2 * Time.deltaTime);

        // 角色朝向移动方向（保持相机世界朝向不变）
        if (moveDir2.sqrMagnitude > 0.01f)
        {
            float camWorldYaw = cameraYawPivot.eulerAngles.y;
            Quaternion targetBodyRot = Quaternion.LookRotation(moveDir2);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRot, Time.deltaTime * 10f);
            cameraYawPivot.localEulerAngles -= Vector3.up * (cameraYawPivot.eulerAngles.y - camWorldYaw);
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
        obstacleTargetInit = false;
        UpdateCameraFollowValue();
        UpdateModelVisibility();
    }

    private void ThirdPerson()
    {
        isFirstPerson = false;
        obstacleTargetInit = false;
        mainCamera.position = cameraFollowPoint.position;
        mainCamera.rotation = cameraFollowPoint.rotation;
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
        if (animator != null)
            animator.speed = Mathf.Clamp(cachedMoveSpeed, 1f, 1.5f);
    }

    public void KillCameraTweens()
    {
        cameraYawPivot.DOKill();
        verticalPoint.DOKill();
        transform.DOKill();
        cameraYawPivot.DOLocalRotate(Vector3.zero, 0.3f);
        isAiming = false;
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
    private int debugLateFrame;
    private void LateUpdate()
    {
        debugLateFrame++;

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

        // 相机自动锁定：在锁定过程中每帧重算朝向（闭环），完成后立即恢复手动控制
        UpdateAim();


        //导航的相机跟随不受其他条件影响
        if (isNavigating)
        {
            CameraFollow();
            // 导航时角色转向移动方向
            if (agent.velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetBodyRot = Quaternion.LookRotation(agent.velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRot, Time.deltaTime * 10f);
            }
            // 相机自动跟随角色朝向：把 cameraYawPivot 残留的 localYaw 平滑归零，
            // 相机回到 transform 朝向上（角色背后），抵消玩家之前手动旋转累积的偏移
            float yaw = NormalizeAngle(cameraYawPivot.localEulerAngles.y);
            if (Mathf.Abs(yaw) > 0.05f)
            {
                yaw = Mathf.LerpAngle(yaw, 0f, Time.deltaTime * 5f);
                cameraYawPivot.localEulerAngles = new Vector3(0f, yaw, 0f);
            }
            else if (Mathf.Abs(yaw) > 0f)
            {
                cameraYawPivot.localEulerAngles = new Vector3(0f, 0f, 0f);
            }
            if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
            {
                EndNavigation(navSnapToTarget ? targetPoint : null);
            }
        }

        //CameraDotween: 观察/聚焦等行为由DOTween控制相机位置，不可跟随
        if (GlobalInfo.SysPopup || ModelManager.Instance.CameraDotween)
        {
            return;
        }

#if !UNITY_ANDROID && !UNITY_IOS
        //弹窗但非系统弹窗：光标解锁，阻断输入但相机仍需跟随角色到新位置
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (!isNavigating)
                CameraFollow();
            return;
        }
#endif
#if UNITY_ANDROID || UNITY_IOS
        if (rotateJoystick == null)
        {
            return;
        }
#endif

        if (!isNavigating)
        {
            Zoom();
            Rotate();
            Move();
        }

        CameraFollow();

        // 移动完成后检测位移和旋转，0.2秒持久化驱动角色动画（操作流程执行时禁用自动检测）
        if (!animationOverridden)
        {
            Vector3 displacement = transform.position - lastPosition;
            displacement.y = 0;

            // 位置变化时检测角色重叠，使附近角色变透明
            if (displacement.sqrMagnitude > 0.0001f)
            {
                var ghost = GetComponent<CharacterGhost>();
                if (ghost != null)
                    ghost.OnPositionUpdated();
            }

            // 模型平滑跟随 controller（替代 DOTween 无限循环 tween，避免移动端时序问题导致的抖动）
            bool modelIsChasing = false;
            if (!modelFollowPaused && model != null)
            {
                float posT = Mathf.Clamp01(Time.deltaTime / Mathf.Max(modelMoveDuration, 0.001f));
                model.position = Vector3.Lerp(model.position, transform.position, posT);

                Vector3 toController = model.position - transform.position;
                toController.y = 0;

                // 检测模型是否还在追赶 controller（距离超过可见阈值 0.02，约 2cm）
                modelIsChasing = toController.sqrMagnitude > 0.02f;

                float targetY;
                if (toController.sqrMagnitude > 0.0001f)
                    targetY = Vector3.SignedAngle(Vector3.back, toController, Vector3.up);
                else
                    targetY = transform.eulerAngles.y;

                float rotT = Mathf.Clamp01(Time.deltaTime / Mathf.Max(modelRotateDuration, 0.001f));
                model.eulerAngles = new Vector3(0, Mathf.LerpAngle(model.eulerAngles.y, targetY, rotT), 0);
            }

            // 动画驱动：controller 有位移或模型正在追赶时播放动画
            // 防止焦点切换时 Time.deltaTime 过大导致误触发
            bool hasMovement = displacement.sqrMagnitude > 0.0001f;

            // 只有在合理帧时间内、且应用有焦点时才更新 lastMoveTime（防止 Alt 切换焦点导致的异常）
            if ((hasMovement || modelIsChasing) && Time.deltaTime < 0.1f && Application.isFocused)
                lastMoveTime = Time.time;

            bool shouldMove = Time.time - lastMoveTime < 0.2f;
            if (shouldMove != animIsMoving)
            {
                animIsMoving = shouldMove;
                if (animator != null)
                    animator.SetBool("isMove", animIsMoving);
            }
        }
        else
        {
            // 操作流程执行时也需要检测角色重叠
            Vector3 displacement = transform.position - lastPosition;
            displacement.y = 0;
            if (displacement.sqrMagnitude > 0.0001f)
            {
                var ghost = GetComponent<CharacterGhost>();
                if (ghost != null)
                    ghost.OnPositionUpdated();
            }
        }
        lastPosition = transform.position;
    }


    /// <summary>
    /// 相机跟随（每帧LateUpdate调用）
    ///
    /// 第三人称避障策略：
    /// 从角色头部(origin)向理想相机位置(desiredPos)做射线检测。
    /// - 无遮挡：safePos = desiredPos（相机理想位置）
    /// - 有遮挡：safePos = 碰撞点前方（收缩到墙前）
    bool HasAnyInput()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (moveJoystick != null && (moveJoystick.Vertical != 0 || moveJoystick.Horizontal != 0))
            return true;
#else
        if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
            return true;
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
            return true;
#endif
        return false;
    }

    void CameraFollow()
    {
        t = 1f / cameraMoveDuration * Time.deltaTime;
        if (isFirstPerson)
        {
            // 第一人称：直接跟随，无避障
            obstacleTargetInit = false;
            mainCamera.position = Vector3.Lerp(mainCamera.position, firstCameraFollowPoint.position, t);
            mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, firstCameraFollowPoint.rotation, t);
        }
        else
        {
            // —— 第三人称避障 ——
            Vector3 origin = verticalPoint.position;        // 射线起点（角色头部）
            Vector3 desiredPos = cameraFollowPoint.position; // 理想相机位置（cameraFollowPoint 跟随 cameraYawPivot 旋转）
            Vector3 dir = desiredPos - origin;
            float dist = dir.magnitude;

            if (dist > 0.01f)
            {
                Vector3 rayDir = dir / dist;

                // 1. 障碍避让：纯射线检测，无状态，碰到障碍就缩
                if (cameraObstacleRadius > 0.001f
                    ? Physics.SphereCast(origin, cameraObstacleRadius, rayDir, out RaycastHit hit, dist, cameraObstacleMask)
                    : Physics.Raycast(origin, rayDir, out hit, dist, cameraObstacleMask))
                {
                    float pushedDist = Mathf.Max(cameraMinAvoidanceDist, hit.distance - cameraObstacleRadius);
                    desiredPos = origin + rayDir * pushedDist + hit.normal * cameraCollisionOffset;
                }

                // 2. 无任何输入时锁距离（只缩不放），有输入时释放
                if (HasAnyInput())
                {
                    if (obstacleTargetInit)
                        obstacleTargetInit = false;
                }
                else
                {
                    float curDesiredDist = Vector3.Distance(origin, desiredPos);
                    if (!obstacleTargetInit)
                    {
                        obstacleMaxDist = curDesiredDist;
                        obstacleTargetInit = true;
                    }
                    else if (curDesiredDist < obstacleMaxDist)
                    {
                        obstacleMaxDist = curDesiredDist;
                    }
                    if (Vector3.Distance(origin, desiredPos) > obstacleMaxDist)
                    {
                        desiredPos = origin + rayDir * obstacleMaxDist;
                    }
                }
            }
            // dist ≈ 0：相机跟随点与头部重合（极小概率），desiredPos 保持不变

            // 3. 平滑插值到目标位置/旋转
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
        agent.updateRotation = false;
        agent.autoBraking = false;
        agent.acceleration = 100f;
        agent.stoppingDistance = 0.5f;
        agent.radius = 0.3f;
        agent.avoidancePriority = 0;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
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
        if (agent.SetDestination(target.position))
        {
            targetPoint = target;
            navSnapToTarget = snapToTarget;
            isNavigating = true;
        }
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
            agent.velocity = Vector3.zero;
            verticalPoint.localEulerAngles = new Vector3(verticalPoint.localEulerAngles.x, 0, 0);
        }

        if (target)
        {
            inAnime = true;

            float moveOverTime = 0.5f / agent.speed;
            duration = moveOverTime > duration ? moveOverTime : duration;

            transform.DOMove(target.position, duration).OnUpdate(() =>
            {
                if (agent.enabled && agent.isOnNavMesh)
                    agent.Warp(transform.position);
            });
            verticalPoint.DOLocalRotate(Vector3.zero, duration);
            cameraYawPivot.DOLocalRotate(Vector3.zero, duration);

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
                modelFollowPaused = true;
                animationOverridden = true;
                Model.transform.SetParent(transform);
                Model.transform.localPosition = Vector3.zero;
                Model.transform.localEulerAngles = Vector3.zero;
                break;
            case (ushort)SmallFlowModuleEvent.SelectFlow:
            case (ushort)SmallFlowModuleEvent.SelectStep:
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                Model.transform.SetParent(transform.parent);
                Model.position = transform.position;
                Model.eulerAngles = transform.eulerAngles;
                modelFollowPaused = false;
                animationOverridden = false;
                break;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

}
