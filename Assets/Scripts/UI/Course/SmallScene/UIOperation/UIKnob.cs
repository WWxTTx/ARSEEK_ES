using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityFramework.Runtime;

[Serializable]
public class UIKnobData
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
}
/// <summary>
/// 旋钮
/// </summary>
public class UIKnob : UIOperation
{
    /// <summary>
    /// 操作集合
    /// </summary>
    public List<OpData> ops;

    [HideInInspector]
    public OpData now_op;

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
    private Vector3 previousPoint;
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
    private Image follow;

    private Vector3 targetStart;


    private void Awake()
    {
        if (clockwise)
            clockwise.gameObject.SetActive(false);
        if (anticlockwise)
            anticlockwise.gameObject.SetActive(false);
        follow = followPoint.GetChild(0).GetComponent<Image>();

        rangeStart = ClampAngle(rangeStart, 0, 360);
        rangeEnd = ClampAngle(rangeEnd, 0, 360);
    }
    public override void Init(string id, string currentState, string opName, Transform model, Action<string> onFinish, Action onFail)
    {
        base.Init(id, currentState, opName, model, onFinish, onFail);

        if (model)
            targetStart = model.localEulerAngles;

        targetPoint.gameObject.SetActive(false);
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i].opName.Equals(currentState))
                followPoint.localEulerAngles = ops[i].startAngle;

            if (!string.IsNullOrEmpty(opName) && ops[i].opName.Equals(opName))
            {
                now_op = ops[i];
                targetPoint.gameObject.SetActive(true);
                targetPoint.localEulerAngles = now_op.targets[index].transform.localEulerAngles;
                switch (now_op.RDHint)
                {
                    case RD.Clockwise:
                        directionHint = clockwise;
                        break;
                    case RD.Anticlockwise:
                        directionHint = anticlockwise;
                        break;
                    default:
                        break;
                }
            }
        }
        if (!string.IsNullOrEmpty(opName) && now_op == null)
            Debug.LogError("未配置-" + opName + "-操作提示");

        main.SetActive(true);

        follow.color = new Color(follow.color.r, follow.color.g, follow.color.b, 1);
        follow.DOFade(0, playTime).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad).SetId("followPoint");

        this.currentState = string.Empty;
    }

    public override void OnTrigger(GameObject collider)
    {
        //判断触碰对象是否是操作中的最后一个目标点
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i].targets[ops[i].targets.Length - 1] == collider)
            {
                currentState = ops[i].opName;
            }
        }
        if (now_op != null)
        {
            if (collider == now_op.targets[index])
            {
                //正确操作
                index += 1;
                if (index >= now_op.targets.Length)
                { index = now_op.targets.Length - 1; targetPoint.gameObject.SetActive(false); }
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
            if (directionHint && !directionHint.gameObject.activeSelf)
            {
                directionHint.localEulerAngles = Vector3.zero;
                directionHint.gameObject.SetActive(true);
                switch (now_op.RDHint)
                {
                    case RD.Clockwise:
                        directionHint.DOLocalRotate(-endAngleHint, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("directionHint"); ;
                        break;
                    case RD.Anticlockwise:
                        directionHint.DOLocalRotate(endAngleHint, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("directionHint"); ;
                        break;
                    default:
                        break;
                }
                DOTween.Kill("followPoint");
                follow.color = new Color(follow.color.r, follow.color.g, follow.color.b, 1);
            }

            //跟随增量旋转

            cacheAngle = Vector3.SignedAngle(previousPoint - transform.position, Input.mousePosition - transform.position, transform.forward);
            if (cacheAngle >= minAngle || cacheAngle <= -minAngle)
            {
                if (cacheAngle >= maxAngle)
                    cacheAngle = maxAngle;
                if (cacheAngle <= -maxAngle)
                    cacheAngle = -maxAngle;

                cacheAngle = followPoint.localEulerAngles.z + cacheAngle;

                if (rangeStart == 0 && rangeEnd == 0)
                {
                    cacheAngle = ClampAngle(cacheAngle, 0, 360);
                    followPoint.localEulerAngles = cacheAngle * Vector3.forward;
                }
                else
                {
                    cacheAngle = ClampAngle(cacheAngle, rangeStart, rangeEnd);
                    followPoint.localEulerAngles = cacheAngle * Vector3.forward;
                }
            }
            previousPoint = Input.mousePosition;
            if (model)
                model.localEulerAngles = new Vector3(targetStart.x, targetStart.y, targetStart.z - followPoint.localEulerAngles.z);

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
            if (!string.IsNullOrEmpty(currentState))
            {
                if (now_op == null)
                {//自由模式
                    DOTween.Kill("followPoint");
                    DOTween.Kill("directionHint");
                    main.SetActive(false);
                    isSelect = false;
                    onFinish?.Invoke(currentState);
                    return;
                }
                else if (currentState.Equals(now_op.opName))
                {//教学模式
                    DOTween.Kill("followPoint");
                    DOTween.Kill("directionHint");
                    Log.Debug("完成操作");
                    main.SetActive(false);
                    isSelect = false;
                    onFinish?.Invoke(currentState);
                    return;
                }
            }

            if (directionHint && directionHint.gameObject.activeSelf)
            {
                follow.color = new Color(follow.color.r, follow.color.g, follow.color.b, 1);
                follow.DOFade(0, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("followPoint");
                directionHint.gameObject.SetActive(false);
                DOTween.Kill("directionHint");
            }
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
}
