using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using UnityFramework.Runtime;
using UnityEngine.Events;

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
    public Tweener CameraRotateTween => cameraRotateFollow;
    public Tweener CameraFollowTween => cameraPositionFollow;
    public Transform Model => model;
    public Tweener ModelRotateTween => modelRotateFollow;
    public Tweener ModelFollowTween => modelPositionFollow;

    #region Private
    private Transform mainCamera;
    private float tempFloat;
    private Vector3 tempVector3;

    private Transform verticalPoint;
    private Transform cameraFollowPoint;
    private Transform firstCameraFollowPoint;
    private Tweener cameraRotateFollow;
    private Tweener cameraPositionFollow;

    private Transform model;
    private Animator animator;
    private Tweener modelRotateFollow;
    private Tweener modelPositionFollow;
    private CharacterController controller;
    #endregion

    private void Awake()
    {
        isFirstPerson = true;
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


        cameraRotateFollow = mainCamera.DORotate(cameraFollowPoint.eulerAngles, cameraRotateDuration).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
        {
            cameraRotateFollow.ChangeEndValue(cameraFollowPoint.eulerAngles, cameraMoveDuration, true);
        });

        cameraPositionFollow = mainCamera.DOMove(cameraFollowPoint.position, cameraMoveDuration).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
        {
            cameraPositionFollow.ChangeEndValue(cameraFollowPoint.position, cameraMoveDuration, true);
        });

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
        //横向轴
        transform.localEulerAngles += Vector3.up * rotateJoystick.Horizontal * GlobalInfo.baseRotateSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.rotateSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);

        // 计算新的旋转角度
        tempFloat = verticalPoint.localEulerAngles.x - rotateJoystick.Vertical * GlobalInfo.baseRotateSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.rotateSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);

        // 将角度转换到[-180, 180]范围
        if (tempFloat > 180)
        {
            tempFloat -= 360;
        }

        // 限制在(-90, 90)度范围（不包含90度）
        float clampedAngle = Mathf.Clamp(tempFloat, -89.999f, 89.999f);

        // 应用限制后的角度
        verticalPoint.localEulerAngles = new Vector3(clampedAngle, 0f, 0f);
#else
        //横向轴
        transform.localEulerAngles += Vector3.up * Input.GetAxis("Mouse X") * GlobalInfo.baseRotateSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.rotateSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);

        //纵向轴
        {
            tempFloat = verticalPoint.localEulerAngles.x - Input.GetAxis("Mouse Y") * GlobalInfo.baseRotateSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.rotateSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);

            if (tempFloat > 180)
            {
                tempFloat -= 360;
            }

            verticalPoint.localEulerAngles = Vector3.right * tempFloat;        
        }
#endif
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
            agent.Move(((moveJoystick.Vertical * transform.forward) + (moveJoystick.Horizontal * transform.right)) * GlobalInfo.baseMoveSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.moveSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient));
        }
        else
        {
            controller.SimpleMove(((moveJoystick.Vertical * transform.forward) + (moveJoystick.Horizontal * transform.right)) * GlobalInfo.baseMoveSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.moveSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient));
        }
#else
        mVertical = Input.GetAxis("Vertical");
        mHorizontal = Input.GetAxis("Horizontal");
        if (mVertical == 0 && mHorizontal == 0)
            return;
        if (agent.isOnNavMesh)
        {
            agent.Move((mVertical * transform.forward + mHorizontal * transform.right) * GlobalInfo.baseMoveSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.moveSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient));
        }
        else
        {
            controller.Move((mVertical * transform.forward + mHorizontal * transform.right) * GlobalInfo.baseMoveSpeed * Time.deltaTime * PlayerPrefs.GetFloat(GlobalInfo.moveSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient));
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

        cameraPositionFollow.Pause();
        cameraRotateFollow.Pause();
        cameraRotateFollow = mainCamera.DORotate(firstCameraFollowPoint.eulerAngles, cameraRotateDuration).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
        {
            cameraRotateFollow.ChangeEndValue(firstCameraFollowPoint.eulerAngles, cameraMoveDuration, true);
        });

        cameraPositionFollow = mainCamera.DOMove(firstCameraFollowPoint.position, cameraMoveDuration).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
        {
            cameraPositionFollow.ChangeEndValue(firstCameraFollowPoint.position, cameraMoveDuration, true);
        });

        cameraPositionFollow.Play();
        cameraRotateFollow.Play();

        model.gameObject.SetActive(false);
    }

    private void ThirdPerson()
    {
        isFirstPerson = false;

        cameraPositionFollow.Pause();
        cameraRotateFollow.Pause();
        cameraRotateFollow = mainCamera.DORotate(cameraFollowPoint.eulerAngles, cameraRotateDuration).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
        {
            cameraRotateFollow.ChangeEndValue(cameraFollowPoint.eulerAngles, cameraRotateDuration, true);
        });

        cameraPositionFollow = mainCamera.DOMove(cameraFollowPoint.position, cameraMoveDuration).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
        {
            cameraPositionFollow.ChangeEndValue(cameraFollowPoint.position, cameraMoveDuration, true);
        });

        cameraPositionFollow.Play();
        cameraRotateFollow.Play();

        modelPositionFollow.ChangeStartValue(transform.position);
        this.WaitTime(0.1f, () =>
        {
            if (model != null)
                model.gameObject.SetActive(!isFirstPerson);
        });
    }

    /// <summary>
    /// 提供给相机跟随配置的视角切换
    /// </summary>
    /// <param name="followPoint"></param>
    /// <param name="duration"></param>
    /// <param name="callback"></param>
    public void CameraFollow(Transform followPoint, float duration, UnityAction callback = null)
    {
        //如果相机跟随的对象是VerticalPoint则按照第一第三人称的设置 否则跟队目标物体
        if (followPoint.name == "VerticalPoint")
        {
            ToLast();
        }
        else
        {
            cameraPositionFollow.Pause();
            cameraRotateFollow.Pause();

            cameraRotateFollow = mainCamera.DORotate(followPoint.eulerAngles, duration/*cameraRotateDuration*/).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
            {
                cameraRotateFollow.ChangeEndValue(followPoint.eulerAngles, duration/*cameraMoveDuration*/, true);
            });

            cameraPositionFollow = mainCamera.DOMove(followPoint.position, duration/*cameraMoveDuration*/).SetLoops(-1).SetAutoKill(false).OnUpdate(() =>
            {
                cameraPositionFollow.ChangeEndValue(followPoint.position, duration/*cameraMoveDuration*/, true);
            });
            cameraPositionFollow.Play();
            cameraRotateFollow.Play();
        }


        modelPositionFollow.ChangeStartValue(transform.position);
        this.WaitTime(0.1f, () =>
        {
            if (model != null)
                model.gameObject.SetActive(!isFirstPerson);
        });
        this.WaitTime(duration, () =>
        {
            callback?.Invoke();
        });
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

    private void OnEnable()
    {
        UpdateCameraFollowValue();
        if (isFirstPerson)
        {
            FirstPerson();
        }
        else
        {
            ThirdPerson();
        }
    }
    private void OnDisable()
    {
        EndNavigation();

        transform.position = model.position;
        modelPositionFollow.ChangeStartValue(transform.position);
        modelPositionFollow.ChangeEndValue(transform.position);

        cameraPositionFollow.ChangeStartValue(mainCamera.position);
        cameraRotateFollow.ChangeStartValue(mainCamera.eulerAngles);
        cameraPositionFollow.ChangeEndValue(cameraFollowPoint.position);
        cameraRotateFollow.ChangeEndValue(cameraFollowPoint.eulerAngles);
        UpdateCameraFollowValue();

        cameraPositionFollow.Pause();
        cameraRotateFollow.Pause();
    }

    private void UpdateCameraFollowValue()
    {
        Transform followPoint = cameraFollowPoint;
        if (isFirstPerson)
            followPoint = firstCameraFollowPoint;
        cameraPositionFollow.ChangeStartValue(followPoint.position);
        cameraRotateFollow.ChangeStartValue(followPoint.eulerAngles);
        cameraPositionFollow.ChangeEndValue(followPoint.position);
        cameraRotateFollow.ChangeEndValue(followPoint.eulerAngles);
    }

    private void LateUpdate()
    {
        if (GlobalInfo.ShowPopup || rotateJoystick == null)
            return;

        if (isNavigating)
        {
            // 导航期间直接用 Lerp 跟随相机跟随点，绕过 SetLoops(-1) tween 的问题
            Transform followPoint = isFirstPerson ? firstCameraFollowPoint : cameraFollowPoint;
            float followSpeed = 1f / cameraMoveDuration * Time.deltaTime;
            mainCamera.position = Vector3.Lerp(mainCamera.position, followPoint.position, followSpeed);
            mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, followPoint.rotation, followSpeed);

            if (Input.anyKey)
            {
                EndNavigation();
            }
            else if (isNavigating && !agent.pathPending && agent.remainingDistance < agent.stoppingDistance)//remainingDistance 距离很不稳定
            {
                EndNavigation(targetPoint);
            }
        }
        else
        {
            Zoom();
            Rotate();
            Move();
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
        //GetComponent<NavMeshAgent>().enabled = true;

        if (agent.SetDestination(target.position))
        {
            targetPoint = target;
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
    private float moveRatio;
    private float rotateRatio;

    public void SetJoystick(Joystick moveJoystick, Joystick rotateJoystick, float moveRatio, float rotateRatio)
    {
        this.moveJoystick = moveJoystick;
        this.rotateJoystick = rotateJoystick;
        this.moveRatio = moveRatio;
        this.rotateRatio = rotateRatio;
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

        if (cameraPositionFollow != null)
            cameraPositionFollow.Kill();
        if (cameraRotateFollow != null)
            cameraRotateFollow.Kill();

        if (modelPositionFollow != null)
            modelPositionFollow.Kill();
        if (modelRotateFollow != null)
            modelRotateFollow.Kill();
    }

}