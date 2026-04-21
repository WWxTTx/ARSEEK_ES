using System;
using UnityEngine;
using DG.Tweening;
using UnityFramework.Runtime;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections;

/// <summary>
/// 配合观察操作的UI
/// </summary>
public class UIObserve : MonoBase
{
    [HideInInspector]
    public string id;

    public GameObject main;

    protected Vector3 camPosition;
    protected Vector3 camAngle;

    protected BehaveObserve behaveObserve;

    protected UISmallSceneModule smallSceneModule;

    private bool initialized = false;
    private Func<bool> initializedPredicate;

    protected override void InitComponents()
    {
        base.InitComponents();
        AddMsg(
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep
        );
        InitUI();
        main.SetActive(false);
        smallSceneModule = transform.parent.GetComponentInChildren<UISmallSceneModule>();
    }

    protected virtual void InitUI()
    {
        initialized = true;
        initializedPredicate = () => initialized;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="id">操作设备id</param>
    /// <param name="opName">正确操作名称，自由操作传null</param>
    /// <param name="onFinish">操作结束回调</param>

    public void Init(string id, BehaveObserve behaveObserve, Action<string> onFinish, Action onFail)
    {
        _init(id, behaveObserve, onFinish, onFail).Forget();
    }

    private async UniTaskVoid _init(string id, BehaveObserve behaveObserve, Action<string> onFinish, Action onFail)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        await UniTask.WaitUntil(initializedPredicate, cancellationToken: ct);

        this.id = id;
        this.behaveObserve = behaveObserve;

        if (this.behaveObserve != null)
        {
            camPosition = Camera.main.transform.position;
            camAngle = Camera.main.transform.eulerAngles;

            FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.StartExecute, id, false));

            Sequence sequence = DOTween.Sequence();
            sequence.Join(Camera.main.transform.DOMove(behaveObserve.ctrlGO.transform.position, behaveObserve.time).SetEase((Ease)behaveObserve.ease));
            sequence.Join(Camera.main.transform.DORotate(behaveObserve.ctrlGO.transform.eulerAngles, behaveObserve.time).SetEase((Ease)behaveObserve.ease));
            sequence.OnComplete(() =>
            {
                CheckInput(0, onFinish, onFail);
                main.SetActive(true);
                OnEnter();
            });
        }
        else
        {
            FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.StartExecute, id, false));

            CheckInput(0, onFinish, onFail);
            main.SetActive(true);
            OnEnter();
        }
    }

    protected virtual void OnEnter()
    {

    }

    protected virtual void CheckInput(int index, Action<string> onFinish, Action onFail)
    {

    }
    protected virtual void OnExit()
    {

    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.SelectFlow:
            case (ushort)SmallFlowModuleEvent.SelectStep:
                if (main.activeSelf)
                {
                    main.SetActive(false);
                }
                break;
        }
    }
}