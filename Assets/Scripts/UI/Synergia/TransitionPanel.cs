using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 转场界面
/// </summary>
public class TransitionPanel : UIPanelBase
{
    public class TransitionData : UIData
    {
        public string message;
        public bool examIcon;
        public TransitionData(string message)
        {
            this.message = message;
        }

        public TransitionData(string message, bool examIcon)
        {
            this.message = message;
            this.examIcon = examIcon;
        }
    }

    private Scrollbar Progress;

    private Text Tip;

    public override void Open(UIData uiData = null)
    {
        GlobalInfo.ShowTransition = true;

        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)CoursePanelEvent.Transition
        });

        Progress = transform.GetComponentByChildName<Scrollbar>("Progress");
        Tip = transform.GetComponentByChildName<Text>("Tip");

        if (uiData != null)
        {
            TransitionData transitionData = uiData as TransitionData;
            Tip.text = transitionData.message;

            if (transitionData.examIcon)
            {
                this.FindChildByName("ExamIcon").gameObject.SetActive(true);
                this.FindChildByName("NormalIcon").gameObject.SetActive(false);
            }
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)CoursePanelEvent.Transition:
                MsgStringFloat msgStringFloat = (MsgStringFloat)msg;
                DOTween.To(() => Progress.size, value => Progress.size = value, msgStringFloat.arg1, 3f);
                if (!string.IsNullOrEmpty(msgStringFloat.arg))
                    Tip.text = msgStringFloat.arg;
                break;
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        GlobalInfo.ShowTransition = false;
    }
}