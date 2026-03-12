using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;

[Serializable]
public class UIDrageData
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [Tooltip("操作名称")]
    public string opName;
    /// <summary>
    /// 跟随点初始位置
    /// </summary>
    [Tooltip("跟随点初始位置")]
    public Transform startPoint;
    /// <summary>
    /// 目标点提示集合
    /// </summary>
    [Tooltip("目标点提示集合")]
    public GameObject[] targets;
}

public class UIDrage : UIOperation
{
    /// <summary>
    /// 操作集合
    /// </summary>
    public List<UIDrageData> ops;

    [HideInInspector]
    public UIDrageData now_op;

    public Transform Point_Min;
    public Transform Point_Max;

    /// <summary>
    /// 按钮闪烁提示动画时间
    /// </summary>
    public float playTime = 1f;

    private Tweener tweener;

    private bool isHorizontal;

    private Camera uiCam;
    private RectTransform rectTransform;

    private void Awake()
    {
        //Vector2 vec = Camera.main.WorldToScreenPoint(Point_Max.position) - Camera.main.WorldToScreenPoint(Point_Min.position);
        Vector2 vec = Point_Max.position - Point_Min.position;
        if (Mathf.Abs(vec.x) > Mathf.Abs(vec.y))
            isHorizontal = true;
        else
            isHorizontal = false;
    }

    public override void Init(string id, string currentState, string opName, Transform model, Action<string> onFinish, Action onFail)
    {
        base.Init(id, currentState, opName, model, onFinish, onFail);

        uiCam = UIManager.Instance.canvas.worldCamera;
        rectTransform = transform as RectTransform;

        now_op = null;

        targetPoint.gameObject.SetActive(false);
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i].opName.Equals(currentState))
                followPoint.position = ops[i].startPoint.GetChild(0).position;

            if (!string.IsNullOrEmpty(opName) && ops[i].opName.Equals(opName))
            {
                now_op = ops[i];
                targetPoint.gameObject.SetActive(true);
                targetPoint.position = now_op.targets[index].transform.GetChild(0).position;
            }
        }

        if (!string.IsNullOrEmpty(opName) && now_op == null)
            Debug.LogError("UI未配置-" + opName + "-操作提示");

        OpenHint();

        main.SetActive(true);
    }

    public override void OnTrigger(GameObject collider)
    {
        //自由模式
        if (now_op == null)
        {
            triggerList.Add(collider);

            //判断触碰对象是否是操作中的最后一个目标点，TODO 待修改多次反复操作问题
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].targets.Length > 0 && ops[i].targets[ops[i].targets.Length - 1] == collider)
                {
                    //判断重复目标点是否完成碰撞
                    var temp = ops[i].targets.ToList();
                    foreach (var go in triggerList)
                        temp.Remove(go);

                    if (temp.Count == 0)
                        currentState = ops[i].opName;
                }
            }
        }
        //教学模式
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
                            currentState = ops[i].opName;
                            break;
                        }
                    }
                }
                else
                {
                    targetPoint.position = now_op.targets[index].transform.GetChild(0).position;
                }
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

            ////跟随鼠标screen space overlay
            //if (isHorizontal)
            //{
            //    float x = Mathf.Clamp(Input.mousePosition.x, Point_Min.position.x, Point_Max.position.x);
            //    followPoint.position = new Vector3(x, followPoint.position.y, 0);
            //}
            //else
            //{
            //    float y = Mathf.Clamp(Input.mousePosition.y, Point_Min.position.y, Point_Max.position.y);
            //    followPoint.position = new Vector3(followPoint.position.x, y, 0);
            //}

            //跟随鼠标 screen space camera
            RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, Input.mousePosition, uiCam, out Vector3 output);
            if (isHorizontal)
            {
                followPoint.position = new Vector3(Mathf.Clamp(output.x, Point_Min.position.x, Point_Max.position.x), followPoint.position.y, followPoint.position.z);
            }
            else
            {
                followPoint.position = new Vector3(followPoint.position.x, Mathf.Clamp(output.y, Point_Min.position.y, Point_Max.position.y), followPoint.position.z);
            }
        }
        else if (main.activeSelf)
        {
            if (!string.IsNullOrEmpty(currentState))
            {
                //自由模式
                if (now_op == null)
                {
                    CloseHint();
                    main.SetActive(false);
                    isSelect = false;
                    onFinish?.Invoke(currentState);
                    return;
                }
                //教学模式
                else if (currentState.Equals(now_op.opName))
                {
                    CloseHint();
                    main.SetActive(false);
                    isSelect = false;
                    onFinish?.Invoke(currentState);
                    return;
                }
            }
            OpenHint();
        }
    }

    void OpenHint()
    {
        if (tweener == null)
        {
            follow.color = new Color(follow.color.r, follow.color.g, follow.color.b, 1);
            tweener = follow.DOFade(0, playTime).SetLoops(-1).SetEase(Ease.InOutQuad).SetId("followPoint");
        }
    }

    void CloseHint()
    {
        if (tweener != null)
        {
            //DOTween.Kill(tweener);
            DOTween.Kill("followPoint");
            tweener = null;
            follow.color = new Color(follow.color.r, follow.color.g, follow.color.b, 1);
        }
    }

    //[ContextMenu("迁移数据")]
    //void DoSomething()
    //{
    //    Point_Min = PointUp;
    //    Point_Max = PointDown;

    //    for (int i = 0; i < ops.Count; i++)
    //    {
    //        UIDrageData UIDrageData = new UIDrageData();
    //        UIDrageData.opName = ops[i].opName;
    //        UIDrageData.startPoint = ops[i].targets[0].transform;
    //        UIDrageData.targets = ops[i].targets;
    //        ops.Add(UIDrageData);
    //    }
    //}
}