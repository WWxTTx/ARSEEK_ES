using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using System.Text;
using System;
using System.Linq;

/// <summary>
/// 主画面直播答题统计
/// </summary>
public class JudgeOnlineResultModule : UIModuleBase
{
    private RectTransform background;

    private Button Collapse;

    private Transform ChoiceContent;

    private Text Summary;

    private Text Rate;

    private List<int> correctAnswers;

    private Dictionary<int, Tuple<int, Text, Text, Image>> answerStats;

    /// <summary>
    /// 记录已答题成员
    /// </summary>
    private HashSet<int> answeredUser = new HashSet<int>();
    /// <summary>
    /// 正确作答成员计数
    /// </summary>
    private int correctUser;
    /// <summary>
    /// 记录成员选项
    /// </summary>
    private List<int> userAnswers = new List<int>();

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)JudgeOnlineEvent.Answer
        });

        background = this.FindChildByName("Background").GetComponent<RectTransform>();
        ChoiceContent = this.FindChildByName("ChoiceContent");
        Collapse = this.GetComponentByChildName<Button>("Collapse");
        Summary = this.GetComponentByChildName<Text>("Summary");
        Rate = this.GetComponentByChildName<Text>("Rate");

        Collapse.onClick.AddListener(() =>
        {
            UIManager.Instance.CloseModuleUI<JudgeOnlineResultModule>(ParentPanel);
            SendMsg(new MsgBase((ushort)JudgeOnlineEvent.End));
            NetworkManager.Instance.SendFrameMsg(new MsgBase((ushort)JudgeOnlineEvent.End));
        });

        JudgeOnlineData judgeOnlineData = uiData as JudgeOnlineData;
        int count = judgeOnlineData.ChoiceCount;
        List<int> choices = new List<int>(count);
        answerStats = new Dictionary <int, Tuple<int, Text, Text, Image>>(count);
        for (int i = 0; i < count; i++)
        {
            choices.Add(i);
        }
        correctAnswers = judgeOnlineData.CorrectIndex;

        ChoiceContent.RefreshItemsView(choices, (item, i) =>
        {
#if UNITY_STANDALONE
            item.GetComponentByChildName<Text>("Choice").text = ((char)(65 + i)).ToString();
#endif
            answerStats.Add(i, new Tuple<int, Text, Text, Image>(0, item.GetComponentByChildName<Text>("Count"), item.GetComponentByChildName<Text>("Rate"), item.GetComponentByChildName<Image>("Check")));
        });
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)JudgeOnlineEvent.Answer:
                MsgBrodcastOperate msgBrodcastOperate = (MsgBrodcastOperate)msg;
                if (!answeredUser.Contains(msgBrodcastOperate.senderId))
                {
                    answeredUser.Add(msgBrodcastOperate.senderId);
                    if (answeredUser.Count == NetworkManager.Instance.GetRoomMemberCount() - 1)
                    {
                        SendMsg(new MsgBase((ushort)JudgeOnlineEvent.Complete));
                    }
#if UNITY_STANDALONE
                    Summary.text = $"已答：{answeredUser.Count}人，未答：{NetworkManager.Instance.GetRoomMemberCount() - answeredUser.Count - 1}人";
#else
                    Summary.text = $"已   答：{answeredUser.Count}人\n未   答：{NetworkManager.Instance.GetRoomMemberCount() - answeredUser.Count - 1}人";
#endif
                    string[] answers = msgBrodcastOperate.GetData<MsgString>().arg.Split(',');

                    userAnswers.Clear();
                    foreach (string answer in answers)
                    {
                        if (string.IsNullOrEmpty(answer))
                            continue;

                        int answerIndex = int.Parse(answer);
                        userAnswers.Add(answerIndex);
                        if (answerStats.ContainsKey(answerIndex))
                        {
                            answerStats[answerIndex] = new Tuple<int, Text, Text, Image>(answerStats[answerIndex].Item1 + 1, answerStats[answerIndex].Item2, answerStats[answerIndex].Item3, answerStats[answerIndex].Item4);
                            answerStats[answerIndex].Item2.text = $"{answerStats[answerIndex].Item1}人";
                            float rate = (float)answerStats[answerIndex].Item1 / (NetworkManager.Instance.GetRoomMemberCount() - 1);
                            answerStats[answerIndex].Item3.text = $"{Mathf.CeilToInt(rate * 100)}%";
                            answerStats[answerIndex].Item4.fillAmount = rate;
                        }
                    }

                    bool correct = userAnswers.All(a => correctAnswers.Any(r => r.Equals(a))) && userAnswers.Count == correctAnswers.Count;
                    if (correct)
                        correctUser += 1;

                    Rate.text = $"正确率: {Mathf.CeilToInt(((float)correctUser / answeredUser.Count) * 100)}%";
                }              
                break;
        }
    }

    #region 动效

    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.3f;

    public override void JoinAnim(UnityAction callback)
    {
        JoinSequence.Join(background.DOAnchorPos3DX(0, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {   
        ExitSequence.Join(background.DOAnchorPos3DX(background.sizeDelta.x, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
#endregion
}