using UnityEngine;
using UnityFramework.Runtime;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Events;

public class LoadingPanel : UIPanelBase
{
    public class LoadingData : UIData
    {
        public string msg;
    }

    private Transform Action;
    private Sequence s;
    private float playtime = 0.65f;
    private int showNum;
    private Text progressValue;
    /// <summary>
    /// 加载时长高于这个时间才会显示
    /// </summary>
    private const float waitTime = 0.2f;
    public static bool Loading = false;
    void Awake()
    {
         AddMsg((ushort)LoadingPanelEvent.ProgressValue);
    }

    /// <summary>
    /// 初始化动画序列
    /// </summary>
    public override void Open(UIData uiData = null)
    {
        base.Open(); 
        Loading = true;
        Action = this.FindChildByName("Action");
        progressValue = GetComponentInChildren<Text>();

        if (uiData != null)
        {
            progressValue.text = (uiData as LoadingData)?.msg;
        }

        Init();

        var canvasGroup = gameObject.AddComponent<CanvasGroup>();
        {
            canvasGroup.alpha = 0;

            Timer.AddTimer(waitTime, name).OnCompleted(() =>
            {
                if (canvasGroup)
                {
                    canvasGroup.alpha = 1;
                    s.Play();
                    Destroy(canvasGroup);
                }
            });
        }
    }

    private void Init()
    {
        Tweener t1 = Action.DOLocalRotate(new Vector3(0, 0, 25f), playtime);
        Tweener t2 = Action.DOLocalRotate(new Vector3(0, 0, 155f), playtime).SetEase(Ease.Linear);
        //s.SetAutoKill(false).SetLoops(-1);
        SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));

        s = DOTween.Sequence();
        s.Append(t1);
        s.Insert(0.1f + t1.Duration(), t2);

        foreach (Transform child in Action)
        {
            Transform r = child.FindChildByName("R");
            s.Insert(0.1f + t1.Duration(), r.DOLocalRotate(new Vector3(0, 0, 76), playtime).SetLoops(2, LoopType.Yoyo)).SetEase(Ease.Linear);
        }
     
        s.SetLoops(-1);
        s.Pause();
    }
    public override void Show(UIData uiData = null)
    {
        showNum += 1;
        gameObject.SetActive(true);
    }

    public override void Hide(UIData uiData = null, UnityAction callback = null)
    {
        SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
        showNum -= 1;
        if (showNum <= 0)
        {
            showNum = 0;
            gameObject.SetActive(false);
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
        Timer.DelTimer(name);
        Loading = false;
    }
    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)LoadingPanelEvent.ProgressValue:
                float progress = ((MsgFloat)msg).arg;
                progressValue.text = "加载中 " + progress.ToString();
                break;
            default:
                break;
        }
    }
}
