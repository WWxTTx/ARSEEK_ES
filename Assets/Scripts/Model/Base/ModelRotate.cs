using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 旋转类型
/// </summary>
public enum RotateType
{
    /// <summary>
    /// 禁用旋转
    /// </summary>
    None,
    /// <summary>
    /// 物体水平方向旋转
    /// </summary>
    GameObject_H,
    /// <summary>
    /// 物体垂直方向旋转
    /// </summary>
    GameObject_V,
    /// <summary>
    /// 物体水平和垂直方向旋转
    /// </summary>
    GameObject_H_V,
    /// <summary>
    /// 物体自身y轴方向旋转
    /// </summary>
    GameObject_Y,
    /// <summary>
    /// 物体自身x轴方向旋转
    /// </summary>
    GameObject_X,
    /// <summary>
    /// 物体自身y轴和垂直方向旋转
    /// </summary>
    GameObject_Y_V,
    /// <summary>
    /// 相机旋转
    /// </summary>
    Camera
}

/// <summary>
/// 旋转脚本
/// </summary>
public class ModelRotate : MonoBase
{
    /// <summary>
    /// 旋转类型
    /// </summary>
    public RotateType rotateType;
    /// <summary>
    /// 水平方向的旋转增量
    /// </summary>
    [Header("水平方向的旋转增量")]
    public float axisX = 5;
    /// <summary>
    /// 垂直方向的旋转增量
    /// </summary>
    [Header("垂直方向的旋转增量")]
    public float axisY = 5;
    /// <summary>
    /// 联动对象
    /// </summary>
    [Header("联动对象")]
    public Transform LinkAgeTrans;

    private bool isMouseDown = false;
    /// <summary>
    /// 旋转比例，区分触屏和鼠标
    /// </summary>
    public float ratio = 1f;

    /// <summary>
    /// 初始角度值
    /// </summary>
    private Vector3 oldAngle;
    public Vector3 OldAngle
    {
        get { return oldAngle; }
    }

    /// <summary>
    /// 相机
    /// </summary>
    private Transform mainCamTrans;

    #region 同步
    /// <summary>
    /// 是否接收到角度同步消息
    /// </summary>
    private bool revMsg = false;
    /// <summary>
    /// 目标角度值
    /// </summary>
    private Vector3 targetDir;

    /// <summary>
    /// 上一帧的角度
    /// </summary>
    private Vector3 lastEuler;

    /// <summary>
    /// 是否进行同步
    /// </summary>
    public bool sync = false;

    private float deltaTime;
    /// <summary>
    /// 同步时间间隔
    /// </summary>
    private float interval = 0.1f;
    /// <summary>
    /// 当前取得控制权的用户
    /// </summary>
    private int controller;
    public int ControlUser
    {
        get
        {
            return controller;
        }
        set
        {
            controller = value;
            if (controller == -1)
            {
                transform.localEulerAngles = oldAngle;
                revMsg = false;
                rotateType = RotateType.None;
            }
        }
    }
    #endregion

    void Awake()
    {
        AddMsg((ushort)ModelOperateEvent.Rotate);
        oldAngle = transform.localEulerAngles;
        targetDir = transform.forward;
        mainCamTrans = Camera.main.transform;

        TapRecognizer.Instance.RegistOnLeftMouseDoubleClick(ResetAngle);
    }

    public void ResetAngle()
    {
        revMsg = true;
        targetDir = oldAngle;
    }

    private void Update()
    {
        if (rotateType == RotateType.None)//禁用
            return;

        if ((Input.touchCount == 0 || Input.touchCount == 1))
        {
            if (Input.GetMouseButtonDown(0) && !GUITool.IsOverGUI(Input.mousePosition))
            {
                isMouseDown = true;
                revMsg = false;
                lastEuler = transform.localEulerAngles;
            }

            if (Input.GetMouseButton(0) && isMouseDown && (!sync || GlobalInfo.account.id == ControlUser))
            {
                BeginRotate();
                if (sync)
                {
                    deltaTime += Time.deltaTime;
                    if (deltaTime > interval && Vector3.Distance(lastEuler, transform.localEulerAngles) > 0.1f)
                    {
                        deltaTime = 0;
                        ToolManager.SendBroadcastMsg(new MsgStringVector3((ushort)ModelOperateEvent.Rotate, transform.name, transform.localEulerAngles));
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isMouseDown = false;
                if (Vector3.Distance(lastEuler, transform.localEulerAngles) > 0.001f)
                {
                    if (GlobalInfo.IsLiveMode() && GlobalInfo.IsOperator())
                    {
                        ToolManager.SendBroadcastMsg(new MsgStringVector3((ushort)ModelOperateEvent.Rotate, transform.name, transform.localEulerAngles));
                    }
                }
            }
        }

        if (revMsg)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(targetDir), Time.deltaTime * 5);
            if (Quaternion.Angle(transform.localRotation, Quaternion.Euler(targetDir)) < 0.1f)
            {
                revMsg = false;
            }
        }
    }

    public void BeginRotate()
    {
        //TODO
        if (GlobalInfo.IsLiveMode() && !GlobalInfo.IsOperator())
            return;

        RefreshAxis();
        _Rotate();
    }

    /// <summary>
    /// 刷新旋转系数
    /// </summary>
    private void RefreshAxis()
    {
        if (Input.touchCount == 0)
        {
            axisX = 5f;
            axisY = 5f;
        }
        else
        {
            axisX = 0.5f;
            axisY = 0.5f;
        }
    }

    private void _Rotate()
    {
        switch (rotateType)
        {
            case RotateType.GameObject_H:
                GameObject_H();
                break;
            case RotateType.GameObject_V:
                GameObject_V();
                break;
            case RotateType.GameObject_H_V:
                GameObject_H_V();
                break;
            case RotateType.GameObject_Y:
                GameObject_Y();
                break;
            case RotateType.GameObject_X:
                GameObject_X();
                break;
            case RotateType.GameObject_Y_V:
                GameObject_Y_V();
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// 物体水平方向旋转
    /// </summary>
    private void GameObject_H()
    {
        Vector2 mousePoint = GetMousePoint();

        Vector3 haxis = mainCamTrans.up.normalized;
        transform.Rotate(haxis, -mousePoint.x * axisX * ratio, Space.World);

        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(haxis, -mousePoint.x * axisX * ratio, Space.World);
    }
    /// <summary>
    /// 物体垂直方向旋转
    /// </summary>
    private void GameObject_V()
    {
        Vector2 mousePoint = GetMousePoint();
        Vector3 haxis = -mainCamTrans.right.normalized;
        transform.Rotate(haxis, -mousePoint.y * axisY * ratio, Space.World);

        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(haxis, -mousePoint.y * axisY * ratio, Space.World);
    }
    /// <summary>
    /// 物体水平和垂直方向旋转
    /// </summary>
    private void GameObject_H_V()
    {
        Vector2 mousePoint = GetMousePoint();

        Vector3 haxis = mainCamTrans.up.normalized;
        transform.Rotate(haxis, -mousePoint.x * axisX * ratio, Space.World);

        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(haxis, -mousePoint.x * axisX * ratio, Space.World);

        haxis = -mainCamTrans.right.normalized;
        transform.Rotate(haxis, -mousePoint.y * axisY * ratio, Space.World);

        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(haxis, -mousePoint.y * axisY, Space.World);
    }
    /// <summary>
    /// 物体自身y轴方向旋转
    /// </summary>
    private void GameObject_Y()
    {
        Vector2 mousePoint = GetMousePoint();
        transform.Rotate(-new Vector3(0, mousePoint.x * axisX, 0) * ratio, Space.Self);
        //存在联动对象
        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(-new Vector3(0, mousePoint.x * axisX, 0) * ratio, Space.Self);
    }
    /// <summary>
    /// 物体自身x轴方向旋转
    /// </summary>
    private void GameObject_X()
    {
        Vector2 mousePoint = GetMousePoint();
        transform.Rotate(new Vector3(mousePoint.y * axisY, 0, 0) * ratio, Space.Self);

        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(new Vector3(mousePoint.y * axisY, 0, 0) * ratio, Space.Self);
    }
    /// <summary>
    /// 物体自身y轴和垂直方向旋转
    /// </summary>
    private void GameObject_Y_V()
    {
        Vector2 mousePoint = GetMousePoint();
        transform.Rotate(-new Vector3(0, mousePoint.x * axisX, 0) * ratio, Space.Self);

        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(-new Vector3(0, mousePoint.x * axisX, 0) * ratio, Space.Self);

        Vector3 haxis = -mainCamTrans.right.normalized;
        transform.Rotate(haxis, -mousePoint.y * axisY * ratio, Space.World);
        Restrict(haxis);
        if (LinkAgeTrans != null)
            LinkAgeTrans.Rotate(haxis, -mousePoint.y * axisY * ratio, Space.World);
    }

    public float minAngleV = 10;
    public float maxAngleV = 80;
    /// <summary>
    /// 限制函数
    /// </summary>
    private void Restrict(Vector3 haxis)
    {
        float angleT = SignedAngleBetween(transform.up, mainCamTrans.forward, haxis, true);
        if (angleT > maxAngleV && angleT <= 225)
        {
            angleT = angleT - maxAngleV;
            transform.Rotate(haxis, angleT, Space.World);
        }
        else if (angleT >= 0 && angleT < minAngleV)
        {
            angleT = minAngleV - angleT;
            transform.Rotate(haxis, -angleT, Space.World);
        }
        else if (angleT > 225 && angleT <= 360)
        {
            angleT = minAngleV + 360 - angleT;
            transform.Rotate(haxis, -angleT, Space.World);
        }
    }
    /// <summary>
    /// 计算两个向量间的实际角度
    /// </summary>
    /// <param name="a">向量a</param>
    /// <param name="b">向量b</param>
    /// <param name="n">垂直于a、b向量所在平面的向量</param>
    /// <param name="is360">实际角度是否按360度计算，true：[0,360]，flase：[-179,180]</param>
    /// <returns>实际角度</returns>
    private float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n, bool is360 = false)
    {
        // angle in [0,180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;

        if (is360)
        {
            // angle in [0,360] 
            float angle360 = (signed_angle + 180) % 360;
            return angle360;
        }
        return signed_angle;
    }
    /// <summary>
    /// 初始化限制参数
    /// </summary>
    public void SetRange(float min, float max)
    {
        minAngleV = min;
        maxAngleV = max;
    }
    public void ResetRange()
    {
        minAngleV = 10;
        maxAngleV = 80;
    }
    /// <summary>
    /// 获取鼠标位置差值
    /// </summary>
    /// <returns></returns>
    private Vector2 GetMousePoint()
    {
        if (Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Moved)
        {
            return Input.GetTouch(0).deltaPosition;
        }
        else
        {
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
    }

    //private void OnDisable()
    //{
    //    isMouseDown = false;
    //    oldAngle = transform.localEulerAngles;
    //}

    //private void OnEnable()
    //{
    //    transform.localEulerAngles = oldAngle;
    //}

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        if (msg.msgId == (ushort)ModelOperateEvent.Rotate)
        {
            if (GlobalInfo.IsLiveMode())
            {
                MsgStringVector3 msgStringVector3 = ((MsgBrodcastOperate)msg).GetData<MsgStringVector3>();
                //状态同步
                if(NetworkManager.Instance.IsIMSyncState)
                {
                    if (!string.Equals(transform.name, msgStringVector3.arg))
                        return;
                    targetDir = msgStringVector3.vector3;
                    transform.localRotation = Quaternion.Euler(targetDir);
                }
                else
                {
                    //协同房间同步取得对当前物体操作权的用户操作
                    if (ControlUser == -1)
                        return;
                    if (!string.Equals(transform.name, msgStringVector3.arg) || GlobalInfo.account.id == ControlUser)
                        return;
                    revMsg = true;
                    targetDir = msgStringVector3.vector3;
                }
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        TapRecognizer.Instance?.UnRegistOnLeftMouseDoubleClick(ResetAngle);
    }
}
