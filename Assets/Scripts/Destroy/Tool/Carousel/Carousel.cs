using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Events;
using UnityFramework.Runtime;

/// <summary>
/// 首页轮播组件
/// </summary>
public class Carousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //选择组父对象
    public Transform OptionGroup;

    //选项总数
    private int optionNum;
    public int OptionNum { get { return optionNum; } }

    //选项物体
    private Transform[] options;
    //选项物体CanvasGroup组件
    private CanvasGroup[] cg;
    //选项物体FrontPlate CanvasGroup组件
    private CanvasGroup[] frontCg;
    //选项物体Mask CanvasGroup组件
    private CanvasGroup[] maskCg;

    /// <summary>
    /// 位置字典
    /// </summary>
    private Dictionary<Transform, Vector3> optionPos = new Dictionary<Transform, Vector3>();
    /// <summary>
    /// 缩放字典
    /// </summary>
    private Dictionary<Transform, float> optionScale = new Dictionary<Transform, float>();
    /// <summary>
    /// 透明度字典
    /// </summary>
    private Dictionary<Transform, float> optionAlpha = new Dictionary<Transform, float>();

    /// <summary>
    /// 初始选中项
    /// </summary>
    public int InitFocusIndex;
    /// <summary>
    /// 当前序列号
    /// </summary>
    private int focusIndex;
    public int CurrentIndex
    {
        get { return focusIndex; }
        private set { focusIndex = value; }
    }

    /// <summary>
    /// 移动时长
    /// </summary>
    public float AnimTime = 1f;

    private bool InTransition = false;

    /// <summary>
    /// 移动步长
    /// </summary>
    public int step;

    /// <summary>
    /// 初始选中左侧选项个数
    /// </summary>
    public int LeftCount;
    /// <summary>
    /// 初始选中右侧选项个数
    /// </summary>
    public int RightCount;

    /// <summary>
    /// 向左移动消失点相对第0项偏移量
    /// </summary>
    public Vector2 LeftExtremeDelta;
    /// <summary>
    /// 向右移动消失点相对第n-1项偏移量
    /// </summary>
    public Vector2 RightExtremeDelta;

    private Vector3 leftExtremePos;
    private Vector3 rightExtremePos;

    private float leftExtremeScale;
    private float rightExtremeScale;

    public UnityEvent OnBeginMove = new UnityEvent();
    public UnityEvent OnEndMove = new UnityEvent();

    private void Awake()
    {
        optionNum = OptionGroup.childCount;
        options = new Transform[optionNum];
        cg = new CanvasGroup[optionNum];
        frontCg = new CanvasGroup[optionNum];
        maskCg = new CanvasGroup[optionNum];
        for (int i = 0; i < optionNum; i++)
        {
            options[i] = OptionGroup.GetChild(i);
            cg[i] = options[i].GetComponent<CanvasGroup>();
            frontCg[i] = options[i].Find("FrontPlate").GetComponent<CanvasGroup>();
            maskCg[i] = options[i].Find("Mask").GetComponent<CanvasGroup>();
        }

        InitPresetValue();
        CurrentIndex = InitFocusIndex;
        frontCg[CurrentIndex].blocksRaycasts = true;
    }


    /// <summary>
    /// 记录初始位置、缩放、透明度
    /// </summary>
    void InitPresetValue()
    {
        for (int i = 0; i < optionNum; i++)
        {
            if (i == 0)
                leftExtremePos = options[i].localPosition - new Vector3(LeftExtremeDelta.x, LeftExtremeDelta.y);
            if (i == optionNum - 1)
                rightExtremePos = options[i].localPosition + new Vector3(RightExtremeDelta.x, RightExtremeDelta.y);

            optionPos.Add(options[i], options[i].localPosition);
        }

        leftExtremeScale = 0f;
        for (int i = 0; i < optionNum; i++)
        {
            if (i == optionNum - 1)
                rightExtremeScale = options[i].localScale.x * 1.5f;

            optionScale.Add(options[i], options[i].localScale.x);
        }
        for (int i = 0; i < optionNum; i++)
        {
            optionAlpha.Add(/*options[i]*/frontCg[i].transform, frontCg[i].alpha);
        }
    }



    /// <summary>
    /// Index向左移动
    /// </summary>
    /// <param name="step">步长</param>
    /// <param name="animTime"></param>
    /// <param name="pass">步长>1时 是否跳过中间步骤动效</param>
    public void MoveLeftByStep(int step, float animTime, bool pass = false)
    {
        if (InTransition)
            return;
        
        InTransition = true;
        frontCg[CurrentIndex].blocksRaycasts = false;
        foreach (CanvasGroup c in cg)
            c.blocksRaycasts = false;

        if (!pass)
            OnBeginMove?.Invoke();

        Sequence sequence = DOTween.Sequence();

        //记录第0项的信息
        Vector3 p = optionPos[options[0]];
        float scale = optionScale[options[0]];
        float alpha = optionAlpha[frontCg[0].transform];

        Vector3 targetP;
        float targetS;
        float targetA;

        for (int i = 0; i < optionNum; i++)
        {
            if (i == optionNum - 1)
            {
                targetP = p;
                targetS = scale;
                targetA = alpha;

            }
            else
            {
                targetP = options[(i + 1) % optionNum].localPosition;
                targetS = options[(i + 1) % optionNum].localScale.x;
                targetA = optionAlpha[frontCg[(i + 1) % optionNum].transform];
            }

            if (i == (CurrentIndex + RightCount) % optionNum)
            {
                sequence.Join(Move(options[i], targetP, rightExtremePos, leftExtremePos, animTime));
                sequence.Join(Scale(options[i], targetS, rightExtremeScale, leftExtremeScale, animTime));
                //sequence.Join(Fade(cg[i], targetA, 0, 0, animTime));
            }
            else
            {
                sequence.Join(Move(options[i], targetP, animTime));
                sequence.Join(Scale(options[i], targetS, animTime));
                //sequence.Join(Fade(cg[i], targetA, animTime));
            }

            if (i == CurrentIndex - 1 || (CurrentIndex == 0 && i == optionNum - 1))
            {
                sequence.Join(Fade(frontCg[i], maskCg[i], 1, 0, animTime));
            }
            else
            {
                sequence.Join(Fade(frontCg[i], maskCg[i], 0, 1, animTime));
            }
        }

        sequence.OnComplete(() =>
        {
            //设置最前项
            if (CurrentIndex == 0)
            {
                CurrentIndex = optionNum - 1;
            }
            else
            {
                CurrentIndex--;
            }

            frontCg[CurrentIndex].blocksRaycasts = true;
            cg[CurrentIndex].blocksRaycasts = true;
            InTransition = false;

            if (--step > 0)
            {
                MoveLeftByStep(step, animTime, true);
            }
            else
            {
                OnEndMove?.Invoke();
            }
        });
    }

    /// <summary>
    /// 向右移动
    /// </summary>
    /// <param name="step">步长</param>
    /// <param name="animTime"></param>
    /// <param name="pass">步长>1时 是否跳过中间步骤动效</param>
    public void MoveRightByStep(int step, float animTime, bool pass = false)
    {
        if (InTransition)
            return;

        InTransition = true;
        frontCg[CurrentIndex].blocksRaycasts = false;
        foreach (CanvasGroup c in cg)
            c.blocksRaycasts = false;

        if(!pass)
            OnBeginMove?.Invoke();

        Sequence sequence = DOTween.Sequence();

        //记录第n-1项的信息
        Vector3 p = optionPos[options[optionNum - 1]];
        float scale = optionScale[options[optionNum - 1]];
        float alpha = optionAlpha[frontCg[optionNum - 1].transform];

        Vector3 targetP;
        float targetS;
        float targetA;

        for (int i = optionNum - 1; i >= 0; i--)
        {
            if (i == 0)
            {
                targetP = p;
                targetS = scale;
                targetA = alpha;
            }
            else
            {
                targetP = options[(i - 1) % optionNum].localPosition;
                targetS = options[(i - 1) % optionNum].localScale.x;
                targetA = optionAlpha[frontCg[(i - 1) % optionNum].transform];
            }

            if (i == (CurrentIndex + optionNum - LeftCount) % optionNum)
            {
                sequence.Join(Move(options[i], targetP, leftExtremePos, rightExtremePos, animTime));
                sequence.Join(Scale(options[i], targetS, leftExtremeScale, rightExtremeScale, animTime));
                //sequence.Join(Fade(cg[i], targetA, 0, 0, animTime));
            }
            else
            {
                sequence.Join(Move(options[i], targetP, animTime));
                sequence.Join(Scale(options[i], targetS, animTime));
                //sequence.Join(Fade(cg[i], targetA, animTime));
            }


            if (i == CurrentIndex + 1 || (CurrentIndex == optionNum - 1 && i == 0))
            {
                sequence.Join(Fade(frontCg[i], maskCg[i], 1, 0, animTime));
            }
            else
            {
                sequence.Join(Fade(frontCg[i], maskCg[i], 0, 1, animTime));
            }
        }

        sequence.OnComplete(() =>
        {
            //设置最前端
            if (CurrentIndex == optionNum - 1)
            {
                CurrentIndex = 0;
            }
            else
            {
                CurrentIndex++;
            }

            frontCg[CurrentIndex].blocksRaycasts = true;
            cg[CurrentIndex].blocksRaycasts = true;
            InTransition = false;

            if (--step > 0)
            {
                MoveRightByStep(step, animTime, true);
            }
            else
            {
                OnEndMove?.Invoke();
            }
        });
    }

    Tween Move(Transform tf, Vector3 target, float animTime)
    {
        return tf.DOLocalMove(target, animTime).OnComplete(() => optionPos[tf] = target).SetEase(Ease.Linear);
    }

    Tween Move(Transform tf, Vector3 target, Vector3 fade, Vector3 appear, float animTime)
    {
        float time = Mathf.Max(animTime / 2 - 0.1f, 0);
        return tf.DOLocalMove(fade, time).OnComplete(() =>
        {
            tf.localPosition = appear;
            tf.DOLocalMove(target, time).OnComplete(() => optionPos[tf] = target).SetEase(Ease.Linear);
        }).SetEase(Ease.Linear);
    }


    Tween Scale(Transform tf, float target, float animTime)
    {
        return tf.DOScale(target, animTime).OnComplete(() => optionScale[tf] = target).SetEase(Ease.Linear);
    }

    Tween Scale(Transform tf, float target, float fade, float appear, float animTime)
    {
        float time = Mathf.Max(animTime / 2 - 0.1f, 0);
        return tf.DOScale(fade, time).OnComplete(() =>
        {
            tf.localScale = appear * Vector3.one;
            tf.DOScale(target, time).OnComplete(() => optionScale[tf] = target).SetEase(Ease.Linear);
        }).SetEase(Ease.Linear);
    }

    Tween Fade(CanvasGroup tf, float target, float animTime)
    {
        return tf.DOFade(target, animTime).OnComplete(() => optionAlpha[tf.transform] = target).SetEase(Ease.Linear);
    }

    Tween Fade(CanvasGroup tf, CanvasGroup mask, float target, float targetMask, float animTime)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Join(tf.DOFade(target, animTime));
        sequence.Join(mask.DOFade(targetMask, animTime));
        sequence.OnComplete(() => optionAlpha[tf.transform] = target).SetEase(Ease.Linear);
        return sequence;
    }

    Tween Fade(CanvasGroup tf, float target, float fade, float appear, float animTime)
    {
        float time = animTime / 2 - 0.1f;
        return tf.DOFade(fade, time).OnComplete(() =>
        {
            tf.alpha = appear;
            tf.DOFade(target, time).OnComplete(() => optionAlpha[tf.transform] = target).SetEase(Ease.Linear);
        }).SetEase(Ease.Linear);
    }

    #region 拖拽
    Vector2 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InTransition)
            return;

        offset = eventData.delta;

        if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y))
        {
            if (offset.x > 0)
            {
                MoveLeftByStep(1, AnimTime);
            }
            else
            {
                MoveRightByStep(1, AnimTime);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {

    }
    #endregion
}