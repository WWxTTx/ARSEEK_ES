using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 拆分百科模块
/// </summary>
public class DismantlingModule : UIModuleBase
{
    private CanvasGroup canvasGroup;

    /// <summary>
    /// 拆分按钮
    /// </summary>
    private Button SplitBtn;
    /// <summary>
    /// 组合按钮
    /// </summary>
    private Button CombineBtn;
    /// <summary>
    /// 一键还原按钮
    /// </summary>
    private Button ResetBtn;
    /// <summary>
    /// 单独显示按钮
    /// </summary>
    private Toggle CheckToggle;
    private Text CheckText;

    private string checkTxt = "单独显示";
    private string uncheckText = "全部显示";

    /// <summary>
    /// 拆解控制脚本
    /// </summary>
    private DismantlingController iController;

    protected override float exitAnimePlayTime => 0.3f;

    private Transform Background;
    private Vector3 _backgroundPos;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        AddMsg(new ushort[] {
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
            (ushort)ARModuleEvent.Close,
#endif
            (ushort)IntegrationModuleEvent.Split,
            (ushort)IntegrationModuleEvent.Comb,
            (ushort)IntegrationModuleEvent.Check,
            (ushort)IntegrationModuleEvent.UnCheck,
            (ushort)IntegrationModuleEvent.JumpToSelect,
            (ushort)IntegrationModuleEvent.CombAll,
            (ushort)RoomChannelEvent.UpdateControl
        });

        InitVariables();

        TapRecognizer.Instance.RegistOnRightMouseDoubleClick(ResetState);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        TapRecognizer.Instance.RegistOnLeftMouseEmptyDoubleClick(ResetPosition);
#else
        TapRecognizer.Instance.RegistOnRightMouseEmptyClick(ResetPosition);
#endif
    }

    private void InitVariables()
    {
#if UNITY_ANDROID || UNITY_IOS
        canvasGroup = transform.GetComponent<CanvasGroup>();
        canvasGroup.interactable = !GlobalInfo.isARTracking;
#endif
        ResetBtn = transform.GetComponentByChildName<Button>("ResetBtn");
        CombineBtn = transform.GetComponentByChildName<Button>("CombineBtn");
        SplitBtn = transform.GetComponentByChildName<Button>("SplitBtn");
        CheckToggle = transform.GetComponentByChildName<Toggle>("CheckToggle");
        CheckText = CheckToggle.GetComponentInChildren<Text>();

        Background = transform.FindChildByName("Background");
        _backgroundPos = Background.localPosition;

        SplitBtn.onClick.AddListener(() => ToolManager.SendBroadcastMsg(new MsgBase((ushort)IntegrationModuleEvent.Split), true));
        CombineBtn.onClick.AddListener(() => ToolManager.SendBroadcastMsg(new MsgBase((ushort)IntegrationModuleEvent.Comb), true));
        ResetBtn.onClick.AddListener(() => ToolManager.SendBroadcastMsg(new MsgBase((ushort)IntegrationModuleEvent.CombAll), true));
        CheckToggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
                ToolManager.SendBroadcastMsg(new MsgBase((ushort)IntegrationModuleEvent.Check), true);
            else
                ToolManager.SendBroadcastMsg(new MsgBase((ushort)IntegrationModuleEvent.UnCheck), true);
        });
    }

    /// <summary>
    /// 初始化模型集成控制脚本
    /// </summary>
    /// <param name="go"></param>
    public void ChangeBaike(GameObject go)
    {
        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);

        iController = go.AutoComponent<DismantlingController>();
        if (iController)
        {
            iController.selectModel = go;

            //默认居中显示
            ModelManager.Instance.RevertCameraPose();
            ModelManager.Instance.ResetCameraPose(true);
            //iController.centerDis = ModelManager.Instance.centerDis; // Vector3.Distance(Camera.main.transform.position, ModelManager.Instance.modelBoundsCenter);
            iController.onSelectionChanged.AddListener(UpdateBtnUI);
        }
    }

    private void UpdateBtnUI(GameObject go)
    {
        if (go == null || iController == null)
        {
            CombineBtn.interactable = false;
            SplitBtn.interactable = false;
            CheckToggle.interactable = false;
            return;
        }
        else
        {
            CombineBtn.interactable = iController.CanLocalFold;
            SplitBtn.interactable = iController.CanLocalUnpick;
            CheckToggle.interactable = iController.CanLocalLook;
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
#if UNITY_ANDROID || UNITY_IOS
            case (ushort)ARModuleEvent.Tracking:
                bool tracking = ((MsgBool)msg).arg1;
                canvasGroup.blocksRaycasts = !tracking;
                canvasGroup.DOFade(tracking ? 0 : 1, 0.3f);
                break;
            case (ushort)ARModuleEvent.Close:
                //退出AR重新居中显示当前选中模型
                if (iController != null)
                {
                    GameObject selected = iController.localSelectModel;
                    if (selected == null)
                    {
                        if (GlobalInfo.IsLiveMode() && GlobalInfo.IsOperator())
                            selected = iController.SelectionCtrl.GetUserSelectedGo(GlobalInfo.mainScreenId);
                    }
                    if (selected == null)
                        selected = iController.gameObject;

                    ModelManager.Instance.AdaptModelRestrict(selected);
                    ModelManager.Instance.RevertCameraPose();
                    ModelManager.Instance.ResetCameraPose(true, false, () =>
                    {
                        NetworkManager.Instance.SendFrameMsg(new MsgBase((ushort)GazeEvent.SyncCamera));
                    });
                }
                break;
#endif
            case (ushort)IntegrationModuleEvent.Split:
                if (iController)
                {
                    if (iController.SelectionCtrl == null)
                        return;

                    int splitUserId = ((MsgBrodcastOperate)msg).senderId;
                    GameObject unpickGo = iController.SelectionCtrl.GetUserSelectedGo(splitUserId);
                    if (unpickGo == null)
                        return;

                    //同步层级前清除本地状态
                    ClearLocalState(false, () =>
                    {
                        //全部取消选中
                        iController.SelectionCtrl.ClearSelection();
                        iController.onSelectionChanged?.Invoke(null);

                        iController.controlUser = splitUserId;
                        iController.UpdateSelect(unpickGo);
                        iController.Disperse();
                    });
                }
                break;
            case (ushort)IntegrationModuleEvent.Comb:
                if (iController)
                {
                    if (iController.SelectionCtrl == null)
                        return;

                    int combUserId = ((MsgBrodcastOperate)msg).senderId;
                    GameObject combGo = iController.SelectionCtrl.GetUserSelectedGo(combUserId);
                    if (combGo == null)
                        return;

                    //同步层级前清除本地状态
                    ClearLocalState(false, () =>
                    {
                        //全部取消选中
                        iController.SelectionCtrl.ClearSelection();
                        iController.onSelectionChanged?.Invoke(null);

                        iController.controlUser = combUserId;
                        iController.UpdateSelect(combGo);
                        iController.Fold();
                    });
                }
                break;           
            case (ushort)IntegrationModuleEvent.Check:
                int checkId = ((MsgBrodcastOperate)msg).senderId;
                if (GlobalInfo.ShouldProcess(checkId))
                {
                    if (iController && iController.SelectionCtrl)
                    {
                        GameObject lookGo = iController.SelectionCtrl.GetUserSelectedGo(checkId);
                        if (lookGo == null)
                            return;

                        FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, false));
                        //进出观察模式过程中暂停同步
                        NetworkManager.Instance.IsIMSync = false;

                        iController.controlUser = checkId;
                        iController.UpdateSelect(lookGo);

                        SplitBtn.interactable = false;
                        CombineBtn.interactable = false;
                        ResetBtn.interactable = false;

                        CheckToggle.SetIsOnWithoutNotify(true);
                        CheckText.text = uncheckText;
                        iController.Check(true, () =>
                        {
                            FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, true));
                            NetworkManager.Instance.IsIMSync = true;
                        });
                    }
                }
                break;
            case (ushort)IntegrationModuleEvent.UnCheck:
                int unCheckId = ((MsgBrodcastOperate)msg).senderId;
                if (GlobalInfo.ShouldProcess(unCheckId))
                {
                    if (iController && iController.SelectionCtrl)
                    {
                        GameObject lookGo = iController.SelectionCtrl.GetUserSelectedGo(unCheckId);
                        if (lookGo == null)
                            return;

                        //进出观察模式过程中暂停同步
                        NetworkManager.Instance.IsIMSync = false;

                        iController.controlUser = unCheckId;
                        iController.UpdateSelect(lookGo);

                        CheckToggle.SetIsOnWithoutNotify(false);
                        CheckText.text = checkTxt;
                        iController.Check(false, () =>
                        {
                            SplitBtn.interactable = iController.CanLocalUnpick;
                            CombineBtn.interactable = iController.CanLocalFold;
                            ResetBtn.interactable = true;

                            NetworkManager.Instance.IsIMSync = true;
                        });
                    }
                }
                break;
            case (ushort)IntegrationModuleEvent.JumpToSelect:
                MsgBrodcastOperate msgBrodcast = ((MsgBrodcastOperate)msg);
                MsgStringBool jumpSelectMsg = msgBrodcast.GetData<MsgStringBool>();
                var target = ModelManager.Instance.GetModelByUUID(jumpSelectMsg.arg1);
                {
                    if (target == null)
                    {
                        Log.Error($"知识点没有对应位置点 位置点信息为{jumpSelectMsg.arg1}");
                        return;
                    }
                    iController.JumpToSelect(target, msgBrodcast.senderId, jumpSelectMsg.arg2);
                }
                break;
            case (ushort)IntegrationModuleEvent.CombAll:
                if (!iController.isResetting)
                {
                    iController.isResetting = true;
                    ClearLocalState(true, () =>
                    {
                        iController.FoldAll();
                    });
                }
                break;
            case (ushort)RoomChannelEvent.UpdateControl:
                MsgIntBool msgIntBool = (MsgIntBool)msg;
                if (msgIntBool.arg1 == GlobalInfo.account.id)
                {
                    ClearLocalState();
                }
                break;
        }
    }

    /// <summary>
    /// 拆解层级变化时清除本地UI状态  
    /// 退出动画、观察模式, 关闭知识点
    /// </summary>
    /// <param name="controlKnowledge"></param>
    /// <param name="callback"></param>
    private void ClearLocalState(bool playRatio = false, UnityAction callback = null)
    {
        UpdateBtnUI(null);

        if (CheckToggle.isOn)
        {
            CheckToggle.SetIsOnWithoutNotify(false);
            CheckText.text = checkTxt;

            if (playRatio) GlobalInfo.playTimeRatio = 0;
            iController.Check(false, () =>
            {
                if (playRatio) GlobalInfo.playTimeRatio = 1f;
                callback?.Invoke();
            });
        }
        else
        {
            callback?.Invoke();
        }
    }

    /// <summary>
    /// 同步主画面时清除本地状态  
    /// </summary>
    /// <param name="controlKnowledge"></param>
    /// <param name="callback"></param>
    private void ClearLocalState()
    {
        UpdateBtnUI(null);

        if (CheckToggle.isOn)
        {
            CheckToggle.SetIsOnWithoutNotify(false);
            CheckText.text = checkTxt;

            GlobalInfo.playTimeRatio = 0;
            iController.Check(false, () => GlobalInfo.playTimeRatio = 1f);
        }
    }

    private void ResetState()
    {
        if (iController != null && iController.InCheckMode)
            return;
        ToolManager.SendBroadcastMsg(new MsgBase((ushort)IntegrationModuleEvent.CombAll), true);
    }

    private void ResetPosition()
    {
        ModelManager.Instance.RevertCameraPose();
        ModelManager.Instance.ResetCameraPose();
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        TapRecognizer.Instance?.UnRegistOnRightMouseDoubleClick(ResetState);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        TapRecognizer.Instance?.UnRegistOnLeftMouseEmptyDoubleClick(ResetPosition);
#else
        TapRecognizer.Instance?.UnRegistOnRightMouseEmptyClick(ResetPosition);
#endif
        base.Close(uiData, callback);
    }

    /// <summary>
    /// 进场动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void JoinAnim(UnityAction callback)
    {
        var pos = new Vector3(_backgroundPos.x, _backgroundPos.y - 50f, _backgroundPos.z);
        Background.localScale = Vector3.one * 0.001f;
        JoinSequence.Append(Background.DOLocalMoveY(_backgroundPos.y, JoinAnimePlayTime).SetEase(Ease.Linear));
        JoinSequence.Join(Background.DOScale(Vector3.one, JoinAnimePlayTime).SetEase(Ease.Linear));
        base.JoinAnim(callback);
    }

    /// <summary>
    /// 退场动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Append(Background.DOLocalMoveY(_backgroundPos.y - 50f, ExitAnimePlayTime));
        ExitSequence.Join(Background.DOScale(Vector3.zero, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
}