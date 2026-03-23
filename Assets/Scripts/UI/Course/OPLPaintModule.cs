using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 一点课画笔模块
/// </summary>
public class OPLPaintModule : HoverHintModule
{
    public delegate bool GroupItemValid<T>(Transform item, out T t);

    #region ENUM
    public enum PaintWidth
    {
        Null,
        MinSize,
        NormalSize,
        MaxSize
    }
    public enum PaintType
    {
        Null,
        Freedom,
        Quad,
        Circle,
        Arrow
    }
    public enum PaintSetting
    {
        Null,
        ChangeColor,
        ChangeWidth,
        ChangeType,
        Paint
    }
    enum PoolType
    {
        Default,
        Allow,
        Point
    }
    #endregion
    #region UI
    protected CanvasGroup canvasGroup;

    /// <summary>
    /// 画板主体 用于获取点击
    /// </summary>
    [HideInInspector]
    public GameObject PaintArea;
    /// <summary>
    /// 画板父物体 用于设置画线的归属
    /// </summary>
    protected Transform PaintStack;

    /// <summary>
    /// 画笔相关图标父节点
    /// </summary>
    protected RectTransform Content;
    protected Image ContentMask;
    /// <summary>
    /// 撤销
    /// </summary>
    protected Button UndoBtn;
    /// <summary>
    /// 清空
    /// </summary>
    protected Button EraseBtn;

    /// <summary>
    /// 线条粗细父物体
    /// </summary>
    protected Transform SizeToggle;
    /// <summary>
    /// 线条形状父物体
    /// </summary>
    protected Transform ShapeToggle;
    /// <summary>
    /// 线条颜色父物体
    /// </summary>
    protected Transform ColorToggle;

    protected Dictionary<PaintWidth, Toggle> sizeTogsDic;
    protected Dictionary<string, Toggle> colorTogsDic;
    protected Dictionary<PaintType, Toggle> typeTogsDic;
    #endregion

    /// <summary>
    /// LineRenderer模板
    /// </summary>
    protected UILineRenderer Line;
    protected Dictionary<int, Stack<UILineRenderer>> userPaintGos;
    /// <summary>
    /// 绘图池
    /// </summary>
    protected Stack<UILineRenderer> paintPool;

    protected bool isDrag = false;
    protected List<Vector2> currentPaintScreenPoses = new List<Vector2>();
    /// <summary>
    /// 本地LineRenderer
    /// </summary>
    protected UILineRenderer currentLr;
    /// <summary>
    /// 本地LineRenderer点
    /// </summary>
    protected List<Vector2> currentPoints = new List<Vector2>();
    /// <summary>
    /// 本地计算图形点
    /// </summary>
    protected Vector2[] currentPoses;
 
    /// <summary>
    /// 画线基础宽度
    /// </summary>
    protected const float paintWidthBase = 2f;
    /// <summary>
    /// 画线额外宽度系数
    /// </summary>
    protected const float paintWidthMul = 2f;
    protected const int circleLineCount = 72;
    protected float halfThicknessH;
    protected float halfThicknessV;

    protected PaintType paintType = PaintType.Freedom;
    protected PaintWidth paintWidth = PaintWidth.MinSize;
    protected Color paintColor = new Color(0.7490196f, 0.2784314f, 0.2666667f);

    public override void Open(UIData uiData = null)
    {
        GlobalInfo.InPaintMode = true;

        base.Open(uiData);
        AddMsg(new ushort[] {
            (ushort)BaikeSelectModuleEvent.BaikeSelect,
            (ushort)PaintEvent.SyncUndo,
            (ushort)PaintEvent.SyncReset,
        });
        Init();
    }

    protected virtual void Init()
    {
        canvasGroup = transform.GetComponent<CanvasGroup>();

        PaintArea = transform.FindChildByName("PaintArea").gameObject;
        PaintStack = transform.FindChildByName("PaintStack");

        Content = transform.GetComponentByChildName<RectTransform>("Content");
        ContentMask = Content.GetComponent<Image>();
        UndoBtn = transform.GetComponentByChildName<Button>("Undo");
        EraseBtn = transform.GetComponentByChildName<Button>("Erase");
        SizeToggle = transform.FindChildByName("SizeToggle");
        ShapeToggle = transform.FindChildByName("ShapeToggle");
        ColorToggle = transform.FindChildByName("ColorToggle");

        Line = transform.GetComponentByChildName<UILineRenderer>("Line");
        userPaintGos = new Dictionary<int, Stack<UILineRenderer>>();
        paintPool = new Stack<UILineRenderer>();

        UndoBtn.onClick.AddListener(Undo);
        EraseBtn.onClick.AddListener(ResetPaint);

        sizeTogsDic = new Dictionary<PaintWidth, Toggle>();
        typeTogsDic = new Dictionary<PaintType, Toggle>();
        colorTogsDic = new Dictionary<string, Toggle>();
        InitTogGroup(SizeToggle, sizeTogsDic, TypeGroupItemValid, WidthGroupItemTogEvent);
        InitTogGroup(ShapeToggle, typeTogsDic, TypeGroupItemValid, TypeGroupItemTogEvent);
        InitTogGroup(ColorToggle, colorTogsDic, ColorGroupItemValid, ColorGroupItemTogEvent);
        InitPaintArea();
    }

    #region UI事件绑定
    /// <summary>
    /// 重置绘图
    /// </summary>
    /// <param name="go"></param>
    protected void ResetPaint()
    {
        ToolManager.SendBroadcastMsg(new MsgBase((ushort)PaintEvent.SyncReset), true);
    }
    /// <summary>
    /// 撤销操作
    /// </summary>
    /// <param name="go"></param>
    protected void Undo()
    {
        ToolManager.SendBroadcastMsg(new MsgBase((ushort)PaintEvent.SyncUndo), true);
    }

    /// <summary>
    /// 初始化绘图区域
    /// </summary>
    protected void InitPaintArea()
    {
        var component = PaintArea.AutoComponent<EventTrigger>();
        component.AddEvent(EventTriggerType.PointerClick, arg => PaintAreaOnClick());
        component.AddEvent(EventTriggerType.BeginDrag, arg => PaintAreaOnBeginDrag());
        component.AddEvent(EventTriggerType.Drag, arg => PaintAreaOnDrag());
        component.AddEvent(EventTriggerType.EndDrag, arg => PaintAreaOnEndDrag());
        PaintArea.SetActive(!GlobalInfo.IsLiveMode() || GlobalInfo.IsUserOperator());
    }

    /// <summary>
    /// 为toggle绑事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="group"></param>
    /// <param name="dic"></param>
    /// <param name="isValid"></param>
    /// <param name="action"></param>
    protected void InitTogGroup<T>(Transform group, Dictionary<T, Toggle> dic, GroupItemValid<T> isValid, UnityAction<bool, T> action, UnityAction<Transform, bool> uiAction = null)
    {
        ToggleGroup tg = group.GetComponent<ToggleGroup>();
        int childCount = group.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = group.GetChild(i);
            Toggle childToggle = child.GetComponent<Toggle>();
            T t;
            if (!isValid(child, out t))
                action = null;
            else
                dic.Add(t, childToggle);
            childToggle.onValueChanged.AddListener((arg) =>
            {
                uiAction?.Invoke(childToggle.transform, arg);
                action?.Invoke(arg, t);
            });
        }
    }

    /// <summary>
    /// 通过名称验证是否是粗细组的正确名
    /// </summary>
    /// <param name="item"></param>
    /// <param name="pt"></param>
    /// <returns></returns>
    protected bool TypeGroupItemValid(Transform item, out PaintWidth pw)
    {
        pw = PaintWidth.Null;
        PaintWidth tmp;
        if(int.TryParse(item.name, out int i))
        {
            var result = Enum.TryParse(item.name, out tmp);
            pw = tmp;
            return result;
        }
        return false;
    }
    /// <summary>
    /// 通过名称验证是否是类型组的正确名
    /// </summary>
    /// <param name="item"></param>
    /// <param name="pt"></param>
    /// <returns></returns>
    protected bool TypeGroupItemValid(Transform item, out PaintType pt)
    {
        PaintType tmp;
        var result = Enum.TryParse(item.name, out tmp);
        pt = tmp;
        return result;
    }
    /// <summary>
    /// 通过名称验证是否是颜色组的正确名
    /// </summary>
    /// <param name="item"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    protected bool ColorGroupItemValid(Transform item, out string c)
    {
        c = item.name;
        return true;
    }
    /// <summary>
    /// 线宽Tog事件
    /// </summary>
    /// <param name="isOn"></param>
    /// <param name="pw"></param>
    protected void WidthGroupItemTogEvent(bool isOn, PaintWidth pw)
    {
        if (isOn)
            paintWidth = pw;
    }
    /// <summary>
    /// 类型Tog事件
    /// </summary>
    /// <param name="isOn"></param>
    /// <param name="pt"></param>
    protected void TypeGroupItemTogEvent(bool isOn, PaintType pt)
    {
        if (isOn)
            paintType = pt;
    }
    /// <summary>
    /// 颜色Tog事件
    /// </summary>
    /// <param name="isOn"></param>
    /// <param name="c"></param>
    protected void ColorGroupItemTogEvent(bool isOn, string c)
    {
        if (isOn)
            paintColor = c.HexToColor();
    }
    #endregion

    #region 本地绘图
    /// <summary>
    /// 开始绘图
    /// </summary>
    /// <param name="go"></param>
    protected void PaintAreaOnBeginDrag()
    {
        isDrag = true;
        currentPaintScreenPoses.Add(PointScale(Input.mousePosition));
        currentPoints.Clear();
        if (currentLr == null)
            currentLr = GetPaintGo(GlobalInfo.account.id, paintWidth, paintColor);
    }
    /// <summary>
    /// 绘图ing
    /// </summary>
    /// <param name="go"></param>
    protected void PaintAreaOnDrag()
    {
        if (!isDrag || currentLr == null) return;
        Vector2 screenPos = PointScale(Input.mousePosition);
        currentPaintScreenPoses.Add(screenPos);

        Vector2 startPos = currentPaintScreenPoses[0];
        switch (paintType)
        {
            case PaintType.Freedom:
                currentPoints.Add(screenPos);
                currentLr.Points = currentPoints.ToArray();
                break;
            case PaintType.Quad:
                currentPoses = GetQuadPoses(startPos, screenPos);
                currentLr.Points = currentPoses;
                break;
            case PaintType.Circle:
                currentPoses = GetCirclePoses(startPos, screenPos);
                currentLr.Points = currentPoses;
                break;
            case PaintType.Arrow:
                currentLr.Points = new Vector2[] { startPos, screenPos };
                currentLr.LineArrow = true;
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// 结束绘图
    /// </summary>
    /// <param name="go"></param>
    protected void PaintAreaOnEndDrag()
    {
        isDrag = false;
        currentLr = null;

        if (GlobalInfo.IsLiveMode() && GlobalInfo.IsUserOperator())
        {
            MsgSyncPaint msgBase = new MsgSyncPaint((ushort)PaintEvent.SyncPaint, paintWidth, paintType, paintColor, GetSendPoses(), GlobalInfo.CanvasWidth, GlobalInfo.CanvasHeight);//Screen.width, Screen.height
            MsgBrodcastOperate msg = new MsgBrodcastOperate(msgBase.msgId, JsonTool.Serializable(msgBase));
            NetworkManager.Instance.SendIMMsg(msg);
        }
        currentPaintScreenPoses.Clear();
        currentPoints.Clear();
    }
    /// <summary>
    /// 在绘图区域单击
    /// </summary>
    /// <param name="go"></param>
    protected void PaintAreaOnClick()
    {
        if (paintType != PaintType.Freedom || isDrag) return;

        Vector2 screenPos = PointScale(Input.mousePosition);
        currentPoints.Clear();
        currentLr = GetPaintGo(GlobalInfo.account.id, paintWidth, paintColor);
        currentPoses = GetPointPoses(screenPos, currentLr.LineThickness / 3);
        currentLr.Points = currentPoses;

        if (GlobalInfo.IsLiveMode() && GlobalInfo.IsUserOperator())
        {
            MsgSyncPaint msgBase = new MsgSyncPaint((ushort)PaintEvent.SyncPaint, paintWidth, paintType, paintColor, new Vector2[1] { screenPos }, GlobalInfo.CanvasWidth, GlobalInfo.CanvasHeight);//Screen.width, Screen.height
            MsgBrodcastOperate msg = new MsgBrodcastOperate(msgBase.msgId, JsonTool.Serializable(msgBase));
            NetworkManager.Instance.SendIMMsg(msg);
        }
        currentLr = null;
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 获取当前绘图的points
    /// </summary>
    /// <returns></returns>
    protected Vector2[] GetSendPoses()
    {
        if (currentPaintScreenPoses.Count > 1 && paintType != PaintType.Freedom)
        {
            Vector2[] result = new Vector2[2];
            result[0] = currentPaintScreenPoses[0];
            result[1] = currentPaintScreenPoses[currentPaintScreenPoses.Count - 1];
            return result;
        }
        else
        {
            return currentPaintScreenPoses.ToArray();
        }
    }

    protected Vector2 PointScale(Vector3 pos)
    {
        return new Vector2(pos.x / Screen.width * GlobalInfo.CanvasWidth, pos.y / Screen.height * GlobalInfo.CanvasHeight);
    }
    /// <summary>
    /// 根据不同的分辨率对绘图points做修正
    /// </summary>
    /// <param name="point"></param>
    /// <param name="screenWidth"></param>
    /// <param name="screenHeight"></param>
    /// <returns></returns>
    protected Vector2 PointConvert(Vector2 point, int screenWidth, int screenHeight)
    {
        //return new Vector2(point.x / screenWidth * Screen.width, point.y / screenHeight * Screen.height);
        return new Vector2(point.x / screenWidth * GlobalInfo.CanvasWidth, point.y / screenHeight * GlobalInfo.CanvasHeight);
    }
    /// <summary>
    /// 批量坐标修正
    /// </summary>
    /// <param name="screenPoints"></param>
    /// <param name="screenWidth"></param>
    /// <param name="screenHeight"></param>
    /// <returns></returns>
    protected Vector2[] PointsConvert(Vector2[] screenPoints, int screenWidth, int screenHeight)
    {
        int len = screenPoints.Length;
        Vector2[] result = new Vector2[len];
        for (int i = 0; i < len; i++)
        {
            result[i] = PointConvert(screenPoints[i], screenWidth, screenHeight);
        }
        return result;
    }
    /// <summary>
    /// 通过圆心计算圆的36个点（绘制点）
    /// </summary>
    /// <param name="point"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected Vector2[] GetPointPoses(Vector2 point, float offset)
    {
        var start = new Vector2(point.x - offset, point.y + offset);
        var end = new Vector2(point.x + offset, point.y - offset);
        return GetCirclePoses(start, end);
    }

    /// <summary>
    /// 通过圆心计算圆的36个点（绘制圆）
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    protected Vector2[] GetCirclePoses(Vector2 start, Vector2 end)
    {
        Vector2[] result = new Vector2[circleLineCount];
        float width = (end.x - start.x) / 2;
        float height = (end.y - start.y) / 2;
        Vector2 center = (start + end) / 2;
        float baseNumb = 360f / circleLineCount * Mathf.Deg2Rad;
        for (int i = 0; i < circleLineCount - 1; i++)
        {
            var x = Mathf.Sin(i * baseNumb) * width;//横坐标
            var y = Mathf.Cos(i * baseNumb) * height;//纵坐标
            result[i] = new Vector2(x, y);
            //偏移
            result[i] += center;
        }

        Vector2 closePoint = result[0];
        Vector2 lastPoint = result[circleLineCount - 2];
        if (lastPoint.x > closePoint.x)
            closePoint -= new Vector2(halfThicknessH, 0);
        else if (lastPoint.x < closePoint.x)
            closePoint += new Vector2(halfThicknessH, 0);
        else if (lastPoint.y > closePoint.y)
            closePoint -= new Vector2(0, halfThicknessV);
        else if (lastPoint.x < closePoint.x)
            closePoint += new Vector2(0, halfThicknessV);

        result[circleLineCount - 1] = closePoint;

        return result;
    }
    /// <summary>
    /// 计算组成矩形的点
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    protected Vector2[] GetQuadPoses(Vector2 start, Vector2 end)
    {
        Vector2[] result = new Vector2[5];
        result[0] = start;
        result[1] = new Vector2(start.x, end.y);
        result[2] = end;
        result[3] = new Vector2(end.x, start.y);

        Vector2 closePoint = start;
        if (end.x > start.x)
            closePoint -= new Vector2(halfThicknessH, 0);
        else if (end.x < start.x)
            closePoint += new Vector2(halfThicknessH, 0);
        else if (end.y > start.y)
            closePoint -= new Vector2(0, halfThicknessV);
        else if (end.x < start.x)
            closePoint += new Vector2(0, halfThicknessV);

        result[4] = closePoint;
        return result;
    }
    #endregion

    #region 对象池、对象操作
    /// <summary>
    /// 通过对象池获取绘图对象
    /// </summary>
    /// <returns></returns>
    protected UILineRenderer GetPaintGo(int userId, PaintWidth paintWidth, Color paintColor)
    {
        UILineRenderer result = null;
        if (paintPool.Count > 0)
        {
            result = paintPool.Pop();
            result.gameObject.SetActive(true);
        }
        else
        {
            result = Instantiate(Line, PaintStack);
            result.gameObject.SetActive(true);
        }
        PushPaintGo(userId, result);
        UpdatePaintData(result, paintWidth, paintColor);
        return result;
    }
    /// <summary>
    /// 删除绘图对象
    /// </summary>
    /// <param name="lr"></param>
    protected void DeletePaintGo(UILineRenderer lr)
    {
        paintPool.Push(lr);
        lr.gameObject.SetActive(false);
    }
    /// <summary>
    /// 将绘图对象添加到用户字典
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="line"></param>
    protected void PushPaintGo(int userId, UILineRenderer line)
    {
        if (userPaintGos.ContainsKey(userId))
        {
            userPaintGos[userId].Push(line);
        }
        else
        {
            Stack<UILineRenderer> lineStack = new Stack<UILineRenderer>();
            lineStack.Push(line);
            userPaintGos.Add(userId, lineStack);
        }
    }

    /// <summary>
    /// 清除所有绘画
    /// </summary>
    protected void ClearAllPaint()
    {
        if (userPaintGos == null || NetworkManager.Instance.IsIMSyncState)
            return;

        foreach (KeyValuePair<int, Stack<UILineRenderer>> userStack in userPaintGos)
        {
            if (userStack.Key == GlobalInfo.account.id)
            {
                UILineRenderer deleteLr;
                while (userStack.Value.Count > 0)
                {
                    deleteLr = userStack.Value.Pop();
                    if (currentLr == null || deleteLr != currentLr)
                    {
                        DeletePaintGo(deleteLr);
                    }
                }
                //保留本地正在绘制的对象
                if (currentLr != null)
                {
                    PushPaintGo(GlobalInfo.account.id, currentLr);
                }
            }
            else
            {
                while (userStack.Value.Count > 0)
                {
                    DeletePaintGo(userStack.Value.Pop());
                }
            }
        }
    }
    /// <summary>
    /// 清除用户绘画
    /// </summary>
    /// <param name="userId"></param>
    protected void ClearUserPaint(int userId)
    {
        if (userPaintGos == null || !userPaintGos.ContainsKey(userId))
            return;

        while (userPaintGos[userId].Count > 0)
        {
            DeletePaintGo(userPaintGos[userId].Pop());
        }
    }

    /// <summary>
    /// 更新绘图设置
    /// </summary>
    /// <param name="lr"></param>
    protected void UpdatePaintData(UILineRenderer lr, PaintWidth paintWidth, Color paintColor)
    {
        //宽度
        //float value = (int)paintWidth * paintWidthMul + paintWidthBase;
        float value = (int)paintWidth * (paintWidthMul + 1);
        lr.lineThickness = value;

        halfThicknessH = lr.lineThickness / 2;
        halfThicknessV = lr.lineThickness / 2;
        //颜色
        lr.color = paintColor;

        lr.LineArrow = false;
    }
    #endregion


    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)PaintEvent.SyncUndo:
                int undoUser = ((MsgBrodcastOperate)msg).senderId;
                if (userPaintGos == null || !userPaintGos.ContainsKey(undoUser))
                    return;
                if (userPaintGos[undoUser].Count > 0)
                {
                    DeletePaintGo(userPaintGos[undoUser].Pop());
                }
                break;
            case (ushort)BaikeSelectModuleEvent.BaikeSelect:
                ClearAllPaint();
                break;
            case (ushort)PaintEvent.SyncReset:
                ClearUserPaint(((MsgBrodcastOperate)msg).senderId);
                break;
            default:
                break;
        }
    }

    public override void Hide(UIData uiData = null, UnityAction callback = null)
    {
        GlobalInfo.InPaintMode = false;
        base.Hide(uiData, callback);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        GlobalInfo.InPaintMode = false;
        base.Close(uiData, callback);
    }

    protected override void InitHoverHint()
    {
        AddHoverHint(SizeToggle.GetComponentByChildName<Toggle>("1"), "粗细1");
        AddHoverHint(SizeToggle.GetComponentByChildName<Toggle>("2"), "粗细2");
        AddHoverHint(SizeToggle.GetComponentByChildName<Toggle>("3"), "粗细3");
        if (ColorToggle)
        {
            AddHoverHint(ColorToggle.GetChild(0).GetComponent<Toggle>(), "红色");
            AddHoverHint(ColorToggle.GetChild(1).GetComponent<Toggle>(), "蓝色");
            AddHoverHint(ColorToggle.GetChild(2).GetComponent<Toggle>(), "黄色");
            AddHoverHint(ColorToggle.GetChild(3).GetComponent<Toggle>(), "绿色");
        }
        AddHoverHint(ShapeToggle.GetComponentByChildName<Toggle>("Freedom"), "画线");
        AddHoverHint(ShapeToggle.GetComponentByChildName<Toggle>("Arrow"), "箭头");
        AddHoverHint(ShapeToggle.GetComponentByChildName<Toggle>("Circle"), "圆形");
        AddHoverHint(ShapeToggle.GetComponentByChildName<Toggle>("Quad"), "矩形");
        AddHoverHint(UndoBtn, "撤销");
        AddHoverHint(EraseBtn, "清屏");
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public override void JoinAnim(UnityAction callback)
    {
        //JoinSequence.Join(Content.DOSizeDelta(new Vector2(Content.sizeDelta.x, 700f), JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() =>0f, value => ContentMask.fillAmount = value, 0.8f, JoinAnimePlayTime));
        JoinSequence.Join(Content.DOAnchorPos3DY(0f, JoinAnimePlayTime));
        JoinSequence.Insert(0.15f, canvasGroup.DOFade(1f, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        //ExitSequence.Join(Content.DOSizeDelta(new Vector2(Content.sizeDelta.x, 0f), ExitAnimePlayTime));//32f
        ExitSequence.Join(DOTween.To(() => 0.8f, value => ContentMask.fillAmount = value, 0f, ExitAnimePlayTime));
        ExitSequence.Join(Content.DOAnchorPos3DY(-682f, ExitAnimePlayTime));
        ExitSequence.Insert(0.15f, canvasGroup.DOFade(0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion
}