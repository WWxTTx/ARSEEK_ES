using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityFramework.Runtime;

/// <summary>
/// 协同画笔模块
/// </summary>
public class SynPaintModule : OPLPaintModule
{
    /// <summary>
    /// 同步LineRenderer
    /// </summary>
    protected UILineRenderer currentSynLr;
    /// <summary>
    /// 同步LineRenderer点
    /// </summary>
    protected private List<Vector2> currentSynPoints = new List<Vector2>();
    /// <summary>
    /// 同步计算图形点
    /// </summary>
    protected private Vector2[] currentSynPoses;


    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        AddMsg(new ushort[] {
            (ushort)PaintEvent.SyncPaint,
            (ushort)RoomChannelEvent.UpdateControl,
            (ushort)RoomChannelEvent.OtherLeave,
        });
    }

    protected override void Init()
    {
        paintColor = NetworkManager.Instance.GetPlayerColor(GlobalInfo.account.id);// "#CE2879".HexToColor();

        canvasGroup = transform.GetComponent<CanvasGroup>();

        PaintArea = transform.FindChildByName("PaintArea").gameObject;
        PaintStack = transform.FindChildByName("PaintStack");
        Content = transform.GetComponentByChildName<RectTransform>("Content");
        ContentMask = Content.GetComponent<Image>();
        UndoBtn = transform.GetComponentByChildName<Button>("Undo");
        EraseBtn = transform.GetComponentByChildName<Button>("Erase");
        SizeToggle = transform.FindChildByName("SizeToggle");
        ShapeToggle = transform.FindChildByName("ShapeToggle");

        Line = transform.GetComponentByChildName<UILineRenderer>("Line");
        userPaintGos = new Dictionary<int, Stack<UILineRenderer>>();
        paintPool = new Stack<UILineRenderer>();

        UndoBtn.onClick.AddListener(Undo);
        EraseBtn.onClick.AddListener(ResetPaint);

        sizeTogsDic = new Dictionary<PaintWidth, Toggle>();
        typeTogsDic = new Dictionary<PaintType, Toggle>();
        InitTogGroup(SizeToggle, sizeTogsDic, TypeGroupItemValid, WidthGroupItemTogEvent);
        InitTogGroup(ShapeToggle, typeTogsDic, TypeGroupItemValid, TypeGroupItemTogEvent);
        InitPaintArea();
    }

    #region 同步绘图
    /// <summary>
    /// 同步绘图
    /// </summary>
    /// <param name="msp"></param>
    protected void SyncPaint(int user, MsgSyncPaint msp)
    {
        if (msp == null) return;

        Vector2[] dtoPoints = msp.GetPoints();
        if (dtoPoints.Length < 1) return;

        Vector2[] points = PointsConvert(dtoPoints, msp.screenWidth, msp.screenHeight);
        currentSynPoints.Clear();
        currentSynLr = GetPaintGo(user, msp.pw, msp.rgba.HexToColor());

        switch (msp.pt)
        {
            case PaintType.Freedom:
                if (points.Length > 1)
                {
                    currentSynPoints.Clear();
                    currentSynPoints.AddRange(points);
                    currentSynLr.Points = currentSynPoints.ToArray();
                }
                else//画点
                {
                    currentSynPoses = GetPointPoses(points[0], currentSynLr.LineThickness / 3);
                    currentSynLr.Points = currentSynPoses;
                }
                break;
            case PaintType.Quad:
                currentSynPoses = GetQuadPoses(points[0], points[points.Length - 1]);
                currentSynLr.Points = currentSynPoses;
                break;
            case PaintType.Circle:
                currentSynPoses = GetCirclePoses(points[0], points[points.Length - 1]);
                currentSynLr.Points = currentSynPoses;
                break;
            case PaintType.Arrow:
                currentSynLr.Points = new Vector2[] { points[0], points[points.Length - 1] };
                currentSynLr.LineArrow = true;
                break;
            default:
                break;
        }

        currentSynLr = null;
    }
    #endregion

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)PaintEvent.SyncPaint:
                int paintUser = ((MsgBrodcastOperate)msg).senderId;
                if (paintUser == GlobalInfo.account.id && !NetworkManager.Instance.IsIMSyncState)
                    return;
                if (!GlobalInfo.IsUserOperator(paintUser))
                    return;
                SyncPaint(paintUser, ((MsgBrodcastOperate)msg).GetData<MsgSyncPaint>());
                break;
            case (ushort)RoomChannelEvent.UpdateControl:
                MsgIntBool msgIntBool = (MsgIntBool)msg;
                if (!msgIntBool.arg2)
                    ClearUserPaint(msgIntBool.arg1);
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                ClearUserPaint(((MsgIntString)msg).arg1);
                break;
        }
    }
}