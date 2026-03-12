using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using System.Text;
using System.Linq;

/// <summary>
/// 协同答题
/// </summary>
public class JudgeOnlineData : UIData
{
    /// <summary>
    /// 选项数量
    /// </summary>
    public int ChoiceCount;
    /// <summary>
    /// 是否多选
    /// </summary>
    public bool MultipleChoice;
    /// <summary>
    /// 正确答案Index
    /// </summary>
    public List<int> CorrectIndex;

    public JudgeOnlineData(int count, bool multiple, List<int> correct = null)
    {
        this.ChoiceCount = count;
        this.MultipleChoice = multiple;
        this.CorrectIndex = correct;
    }
}


/// <summary>
/// 非主画面直播答题
/// </summary>
public class JudgeOnlineModule : UIModuleBase
{
    private RectTransform background;

    private Transform ChoiceContent;

    private Button Submit;

    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.3f;

    private JudgeOnlineData JudgeOnlineData;

    private Dictionary<int, bool> answers;


    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        background = this.FindChildByName("Background").GetComponent<RectTransform>();
        ChoiceContent = this.FindChildByName("ChoiceContent");
        Submit = this.GetComponentByChildName<Button>("Submit");

        JudgeOnlineData = uiData as JudgeOnlineData;

        int count = JudgeOnlineData.ChoiceCount;
        List<int> choices = new List<int>(count);
        answers = new Dictionary<int, bool>(count);
        for (int i = 0; i < count; i++)
        {
            choices.Add(i);
            answers.Add(i, false);
        }

        ChoiceContent.RefreshItemsView(choices, (item, i) =>
        {
            item.GetComponentInChildren<Text>().text = ((char)(65 + i)).ToString();
            Toggle toggle = item.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener((isOn) =>
            {
                answers[i] = isOn;

                if (answers.Values.Select(a => a).Where(a => a == true).ToList().Count == 0)
                    Submit.interactable = false;
                else
                    Submit.interactable = true;
            });

            if (JudgeOnlineData.MultipleChoice)
                toggle.group = null;
        });

        Submit.onClick.AddListener(() =>
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach(KeyValuePair<int, bool> a in answers)
            {
                if (a.Value)
                {
                    stringBuilder.Append(a.Key);
                    stringBuilder.Append(",");
                }
            }
            NetworkManager.Instance.SendFrameMsg(new MsgString((ushort)JudgeOnlineEvent.Answer, stringBuilder.ToString()));

            SendMsg(new MsgBase((ushort)JudgeOnlineEvent.End));
        });
    }

    #region 动效
    /// <summary>
    /// 进场动画
    /// </summary>
    /// <param name="callback">回调</param>
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