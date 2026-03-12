using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityFramework.Runtime;


/// <summary>
/// 旋转方向
/// </summary>
[Serializable]
public enum RD
{
    /// <summary>
    /// 顺时针
    /// </summary>
    Clockwise,
    /// <summary>
    /// 逆时针
    /// </summary>
    Anticlockwise
}

[Serializable]
public class OpData
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [Tooltip("操作名称")]
    public string opName;
    /// <summary>
    /// 跟随点初始角度
    /// </summary>
    [Tooltip("跟随点初始角度")]
    public Vector3 startAngle;
    /// <summary>
    /// 目标点提示集合
    /// </summary>
    [Tooltip("目标点提示集合")]
    public GameObject[] targets;
    /// <summary>
    /// 旋转方向
    /// </summary>
    [Tooltip("旋转方向")]
    public RD RDHint;

    [Tooltip("旋转角度")]
    public float requiredAngle;
}

[Serializable]
public class RangeOpData
{
    [Tooltip("操作名称")]
    public string opName;

    [Tooltip("操作触发角度范围")]
    public Vector2 angleRange;

    //todo
    public float sumAngle = -1;
}

public class UIRotate : UIOperation
{
    /// <summary>
    /// 区间触发
    /// </summary>
    public bool UseRange;

    /// <summary>
    /// 操作集合
    /// </summary>
    public List<OpData> ops;

    /// <summary>
    /// 操作集合
    /// </summary>
    public List<RangeOpData> rangeOpDatas;


    [HideInInspector]
    private OpData now;
    public OpData now_op
    {
        get { return now; }
        set { now = value; }
    }

    /// <summary>
    /// 最大旋转角度(逆时针终点)
    /// </summary>
    public float maxAngle = 5f;
    /// <summary>
    /// 最小旋转角度(逆时针起点)
    /// </summary>
    public float minAngle = 1f;
    public float rangeStart = 0;
    public float rangeEnd = 0;
    /// <summary>
    /// 上一帧的点
    /// </summary>
    public Vector3 previousPoint;
    /// <summary>
    /// 缓存角度
    /// </summary>
    private float cacheAngle;
    /// <summary>
    /// 顺时针旋转指示
    /// </summary>
    public Transform clockwise;
    /// <summary>
    /// 顺时针旋转指示
    /// </summary>
    public Transform anticlockwise;
    /// <summary>
    /// 方向指示
    /// </summary>
    private Transform directionHint;
    public Vector3 endAngleHint = new Vector3(0, 0, 25);
    /// <summary>
    /// 旋转指示动画时间
    /// </summary>
    public float playTime = 1f;

    /// <summary>
    /// 模型初始角度
    /// </summary>
    private Vector3 targetStart;
    /// <summary>
    /// 中心点相对于屏幕左下角的坐标
    /// </summary>
    private Vector3 centerPoint;

    public override bool isSelect
    {
        get { return mIsSelect; }
        set
        {
            mIsSelect = value;
            previousPoint = Input.mousePosition;
        }
    }

    /// <summary>
    /// 累积旋转角度
    /// </summary>
    [SerializeField]
    private float sumAngle;

    /// <summary>
    /// 归一化旋转量
    /// </summary>
    private float normalizedAngleDelta;

    public float MinTotalAngle;
    public float MaxTotalAngle;

    private Tweener followTweener;

    private bool actualClick;
    private bool actualRotate;

    //private void Awake()
    //{     
    //    rangeStart = ClampAngle(rangeStart, 0, 360);
    //    rangeEnd = ClampAngle(rangeEnd, 0, 360);
    //}

    private bool firstInitialized = true;
    protected override void InitComponents()
    {
        base.InitComponents();
        AddMsg(
            (ushort)ModelOperateEvent.Rotate
        );
    }

    public override void Init(string id, string currentState, string opName, Transform model, Action<string> onFinish, Action onFail)
    {
        base.Init(id, currentState, opName, model, onFinish, onFail);

        now_op = null;
        StopHighlight();
        targetPoint.gameObject.SetActive(false);

        if (model)
        {
            if (firstInitialized)
            {
                targetStart = model.localEulerAngles;
                firstInitialized = false;
            }
        }

        if (UseRange)
        {
            for (int i = 0; i < rangeOpDatas.Count; i++)
            {
                if (rangeOpDatas[i].opName.Equals(currentState))
                {
                    if (rangeOpDatas[i].sumAngle != -1)
                    {
                        sumAngle = rangeOpDatas[i].sumAngle;
                    }
                    else
                    {
                        sumAngle = model.transform.localEulerAngles.z.NormalizeAngle();
                    }
                }
            }

            followPoint.localEulerAngles = Vector3.forward * sumAngle;
            //followPoint.GetComponentInChildren<Image>().SetAlpha(1);
            if (model)
            {
                ModelOperationEventManager.Publish<DragEvent>(new DragEvent(this.id, model.gameObject, sumAngle, (Mathf.Abs(sumAngle) - MinTotalAngle) / (MaxTotalAngle - MinTotalAngle)));
            }
        }
        else
        {
            if (clockwise)
                clockwise.gameObject.SetActive(false);
            if (anticlockwise)
                anticlockwise.gameObject.SetActive(false);

            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].opName.Equals(currentState))
                    followPoint.localEulerAngles = ops[i].startAngle;

                if (ops[i].RDHint == RD.Clockwise && ops[i].requiredAngle > 0)
                    ops[i].requiredAngle *= -1;

                if (!string.IsNullOrEmpty(opName) && ops[i].opName.Equals(opName))
                {
                    now_op = ops[i];
                    targetPoint.gameObject.SetActive(true);
                    targetPoint.localEulerAngles = now_op.targets[index].transform.localEulerAngles;
                    uiHighlight = UIHighlight(now_op.targets[index].transform);
                    //switch (now_op.RDHint)
                    //{
                    //    case RD.Clockwise:
                    //        directionHint = clockwise;
                    //        break;
                    //    case RD.Anticlockwise:
                    //        directionHint = anticlockwise;
                    //        break;
                    //    default:
                    //        break;
                    //}
                }
            }
            
            if (!string.IsNullOrEmpty(opName))
            {
                if (now_op == null)
                    Debug.LogError("UI未配置-" + opName + "-操作提示");
                else
                {
                    if (now_op.requiredAngle != 0)
                        directionHint = now_op.requiredAngle > 0 ? anticlockwise : clockwise;
                    else
                        directionHint = targetPoint.localEulerAngles.z - followPoint.localEulerAngles.z > 0 ? anticlockwise : clockwise;
                }
            }
        }

        //OpenHint();
        //OpenDirectionHint();

        main.SetActive(true);

        centerPoint = (transform as RectTransform).anchoredPosition + new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (!UseRange)
            sumAngle = 0;
    }


    public override void OnTrigger(GameObject collider)
    {
        if (UseRange)
            return;
  
        //判断触碰对象是否是操作中的最后一个目标点
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i].targets.Length > 0 && ops[i].targets[ops[i].targets.Length - 1] == collider)
            {
                //区分最终目标点相同的不同操作
                if (ops[i].requiredAngle == 0 || (ops[i].requiredAngle * sumAngle > 0 && Mathf.Abs(ops[i].requiredAngle - sumAngle) <= 30f))
                {
                    currentState = ops[i].opName;
                }
            }
        }

        //自由模式
        if (now_op == null)
        {
            ////判断触碰对象是否是操作中的最后一个目标点
            //for (int i = 0; i < ops.Count; i++)
            //{
            //    if (ops[i].targets.Length > 0 && ops[i].targets[ops[i].targets.Length - 1] == collider)
            //    {
            //        //区分最终目标点相同的不同操作
            //        if (ops[i].requiredAngle == 0 || (ops[i].requiredAngle * sumAngle > 0 && Mathf.Abs(ops[i].requiredAngle - sumAngle) <= 30f))
            //        {
            //            currentState = ops[i].opName;
            //        }
            //    }
            //}
        }
        //教学模型
        else
        {
            if (collider == now_op.targets[index])
            {
                //正确操作
                triggerList.Add(collider);

                index += 1;
                if (index >= now_op.targets.Length)
                {
                    index = now_op.targets.Length - 1;
                    targetPoint.gameObject.SetActive(false);

                    for (int i = 0; i < ops.Count; i++)
                    {
                        if (Enumerable.SequenceEqual(triggerList, ops[i].targets))
                        {
                            //currentState = ops[i].opName;
                            CloseDirectionHint();
                            break;
                        }
                    }
                }
                else
                    targetPoint.localEulerAngles = now_op.targets[index].transform.localEulerAngles;
            }
            else
            {
                //错误操作
                onFail?.Invoke();
            }
        }
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (isSelect)
        {
            CloseHint();

            //跟随增量旋转
            cacheAngle = Vector3.SignedAngle(previousPoint - centerPoint, Input.mousePosition - centerPoint, transform.forward);

            if (cacheAngle >= minAngle || cacheAngle <= -minAngle)
            {
                actualRotate = true;

                if (cacheAngle >= maxAngle)
                    cacheAngle = maxAngle;

                if (cacheAngle <= -maxAngle)
                    cacheAngle = -maxAngle;

                ////教学模式 限制旋转方向
                //if (now_op != null)
                //{
                //    switch (now_op.RDHint)
                //    {
                //        case RD.Clockwise:
                //            if (cacheAngle > 0)
                //                return;
                //            break;
                //        case RD.Anticlockwise:
                //            if (cacheAngle < 0)
                //                return;
                //            break;
                //    }
                //}

                if (MinTotalAngle != MaxTotalAngle)
                {
                    if (sumAngle + cacheAngle > MaxTotalAngle)
                        cacheAngle = Mathf.Max(0, MaxTotalAngle - sumAngle);
                    else if (sumAngle + cacheAngle < MinTotalAngle)
                        cacheAngle = Mathf.Min(0, MinTotalAngle - sumAngle);
                }

                sumAngle += cacheAngle;

                cacheAngle = followPoint.localEulerAngles.z + cacheAngle;

                if (rangeStart == 0 && rangeEnd == 0)
                {
                    //cacheAngle = ClampAngle(cacheAngle, 0, 360);
                    followPoint.localEulerAngles = cacheAngle * Vector3.forward;
                }
                else
                {
                    //cacheAngle = ClampAngle(cacheAngle, rangeStart, rangeEnd);

                    if (rangeStart > rangeEnd)
                    {
                        while (cacheAngle > 180)
                        {
                            cacheAngle -= 360;
                        }
                        while (cacheAngle < -180)
                        {
                            cacheAngle += 360;
                        }
                        cacheAngle = Mathf.Clamp(cacheAngle, rangeEnd, rangeStart);
                    }
                    else
                    {
                        cacheAngle = ClampAngle(cacheAngle, rangeStart, rangeEnd);
                    }

                    followPoint.localEulerAngles = cacheAngle * Vector3.forward;
                }

                if (model)
                {
                    model.localEulerAngles = new Vector3(targetStart.x, targetStart.y, /*targetStart.z - */followPoint.localEulerAngles.z);

                    normalizedAngleDelta = (Mathf.Abs(sumAngle) - MinTotalAngle) / (MaxTotalAngle - MinTotalAngle);

                    ModelOperationEventManager.Publish<DragEvent>(new DragEvent(this.id, model.gameObject, sumAngle, normalizedAngleDelta));

                    if(UseRange && GlobalInfo.isLive && (!GlobalInfo.isExam || GlobalInfo.IsGroupMode()))
                    {
                        NetworkManager.Instance.SendFrameMsg(new MsgModelRotate((ushort)ModelOperateEvent.Rotate, id, followPoint.localEulerAngles.z, sumAngle, normalizedAngleDelta));
                    }
                }
            }
            else
            {
                actualClick = true;
            }

            previousPoint = Input.mousePosition;

            //跟随鼠标旋转
            //{
            //    cacheAngle = AngleAtoB(transform.up, Input.mousePosition - transform.position, transform.forward);
            //    followPoint.localEulerAngles = new Vector3(0, 0, cacheAngle);
            //    if (model)
            //        model.localEulerAngles = new Vector3(targetStart.x, targetStart.y, targetStart.z - cacheAngle);
            //}
        }
        else if (main.activeSelf)
        {
            string newState = string.Empty;

            if (actualRotate && UseRange)
            {
                foreach (RangeOpData opData in rangeOpDatas)
                {
                    //if (followPoint.localEulerAngles.z >= opData.angleRange.x && followPoint.localEulerAngles.z <= opData.angleRange.y)
                    if (sumAngle >= opData.angleRange.x && sumAngle <= opData.angleRange.y)
                    {
                        newState = opData.opName;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(newState))
            {
                //OnClose();
                isSelect = false;
                actualRotate = false;
                actualClick = false;

                if (!newState.Equals(currentState))
                {
                    currentState = newState;
                    onFinish?.Invoke(currentState);
                }

                ////自由模式
                //if (now_op == null)
                //{
                //    OnClose();
                //    isSelect = false;
                //    onFinish?.Invoke(currentState);
                //    return;
                //}
                ////教学模式
                //else if (currentState.Equals(now_op.opName))
                //{
                //    OnClose();
                //    isSelect = false;
                //    onFinish?.Invoke(currentState);
                //    return;
                //}
            }
            else if (actualClick)
            {
                isSelect = false;
                actualRotate = false;
                actualClick = false;

                if (!string.IsNullOrEmpty(ActiveProp))
                {
                    //OnClose();
                    onFinish?.Invoke($"{SmallFlowCtrl.backpackFlag}_{ActiveProp}");
                }
            }
            //OpenHint();
        }
    }

    void OpenHint()
    {
        if (followTweener == null)
        {
            follow.color = new Color(follow.color.r, follow.color.g, follow.color.b, 1);
            followTweener = follow.DOFade(0, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("followPoint");
        }
    }

    void CloseHint()
    {
        if (followTweener != null)
        {
            DOTween.Kill("followPoint");
            followTweener = null;
            follow.color = new Color(follow.color.r, follow.color.g, follow.color.b, 1);
        }
    }

    void OpenDirectionHint()
    {
        //显示旋转提示
        if (directionHint && !directionHint.gameObject.activeSelf)
        {
            directionHint.localEulerAngles = Vector3.zero;
            directionHint.gameObject.SetActive(true);
            if (directionHint == clockwise)
            {
                directionHint.DOLocalRotate(-endAngleHint, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("directionHint");
            }
            else
            {
                directionHint.DOLocalRotate(endAngleHint, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("directionHint");
            }

            //if(now_op != null)
            //{
            //    switch (now_op.RDHint)
            //    {
            //        case RD.Clockwise:
            //            directionHint.DOLocalRotate(-endAngleHint, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("directionHint");
            //            break;
            //        case RD.Anticlockwise:
            //            directionHint.DOLocalRotate(endAngleHint, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("directionHint");
            //            break;
            //        default:
            //            break;
            //    }
            //}
        }
    }

    void CloseDirectionHint()
    {
        //隐藏旋转提示
        if (directionHint/* && directionHint.gameObject.activeSelf*/)
        {
            directionHint.gameObject.SetActive(false);
            DOTween.Kill("directionHint", true);
        }
    }

    protected override void OnClose()
    {
        CloseHint();
        CloseDirectionHint();
        base.OnClose();
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)ModelOperateEvent.Rotate:
                if ((msg as MsgBrodcastOperate).senderId == GlobalInfo.account.id)
                    return;
                MsgModelRotate msgModelRotate = (msg as MsgBrodcastOperate).GetData<MsgModelRotate>();
                if (id.Equals(msgModelRotate.id))
                {
                    sumAngle = msgModelRotate.sumAngle;
                }
                break;
        }
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    /// <summary>
    /// 获取2向量夹角，0-360
    /// </summary>
    /// <param name="from">起点向量</param>
    /// <param name="to">终点向量</param>
    /// <param name="rhs">垂直于2向量所在平面的向量</param>
    /// <returns></returns>
    public float AngleAtoB(Vector3 from, Vector3 to, Vector3 rhs)
    {
        float angle = Vector3.Angle(from, to);
        Vector3 nordir = Vector3.Cross(from, to);
        float dot = Vector3.Dot(nordir, rhs);
        if (dot < 0)
        {
            angle *= -1;
            angle += 360;
        }
        return angle;
    }

    #region 操作提示
    private Sequence uiHighlight;

    private Sequence UIHighlight(Transform item)
    {
        var text = item.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text == null)
            return null;

        var sequence = DOTween.Sequence();
        {
            text.SetAlpha(1f);
            sequence.Join(text.DOFade(0, 0.8f));
            sequence.SetId(item.GetInstanceID());
            sequence.SetLoops(-1, LoopType.Yoyo);
            sequence.OnKill(() =>
            {
                text.SetAlpha(1f);
            });
        }

        return sequence;
    }

    private void StopHighlight()
    {
        if (uiHighlight != null)
        {
            uiHighlight.Kill();
        }

        uiHighlight = null;
    }
    #endregion

    public override void SetFinalState(string opName)
    {
        base.SetFinalState(opName);

        if (UseRange)
        {
            for (int i = 0; i < rangeOpDatas.Count; i++)
            {
                if (rangeOpDatas[i].opName.Equals(opName))
                {
                    if (rangeOpDatas[i].sumAngle != -1)
                    {
                        sumAngle = rangeOpDatas[i].sumAngle;
                    }
                    else
                    {
                        sumAngle = model.transform.localEulerAngles.z.NormalizeAngle();
                    }
                }
            }
            followPoint.localEulerAngles = Vector3.forward * sumAngle;

            if (model)
            {
                ModelOperationEventManager.Publish<DragEvent>(new DragEvent(this.id, model.gameObject, sumAngle, (Mathf.Abs(sumAngle) - MinTotalAngle) / (MaxTotalAngle - MinTotalAngle)));
            }
        }
        else
        {
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].opName.Equals(opName))
                    followPoint.localEulerAngles = ops[i].startAngle;
            }
        }
    }
}