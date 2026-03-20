using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 一道练习百科
/// </summary>
public class OPLExerciseModule : UIModuleBase
{
    private Dictionary<int, Toggle> answerToggles = new Dictionary<int, Toggle>();//选项

    private Exercise exercise;

    private CanvasGroup canvasGroup;
    private Text Type;
    private Text Title;
    private Transform Content;
    private ScrollRect ScrollRect;
    private Transform TitleTexture;
    private Transform TitleVideo;
    private Button ConfirmAnswer;

#if UNITY_ANDROID || UNITY_IOS
    private Button JudgeBtn;
#endif

    /// <summary>
    /// 答案改变事件
    /// </summary>
    public UnityEvent OnAnswerChanged = new UnityEvent();

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
#endif
            (ushort)ExercisesModuleEvent.ChooseAnswer,
            (ushort)ExercisesModuleEvent.ConfirmAnswer,
            (ushort)ExercisesModuleEvent.OpenAnswerImg,
            (ushort)ExercisesModuleEvent.CloseAnswerImg,
            (ushort)ExercisesModuleEvent.OpenAnswerVideo,
            (ushort)ExercisesModuleEvent.CloseAnswerVideo,
            (ushort)JudgeOnlineEvent.Start,
            (ushort)JudgeOnlineEvent.End,
            (ushort)JudgeOnlineEvent.Complete
        });

        Init();

        EncyclopediaExercise exercisePedia = GlobalInfo.currentWiki as EncyclopediaExercise;

        if (exercisePedia == null || exercisePedia.data == null)
        {
            Log.Error($"打开的百科为空! 百科ID:{exercisePedia?.id}");

            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(null, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "获取练习失败，请重试", popupDic));
        }
        else
        {
            exercise = exercisePedia.data.exercise;
            switch (exercise.type)
            {
                case 1:
                    LoadExercise();
                    break;
                case 2:
                    LoadJudgementExercise();
                    break;
            }

            this.WaitTime(0.1f, () =>
            {
                //延迟 通知模块开始答题
                SendMsg(new MsgBase((ushort)SmallFlowModuleEvent.CompleteStep));
            });
        }
    }

    private void Init()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        Type = this.GetComponentByChildName<Text>("Type");
        Title = this.GetComponentByChildName<Text>("Title");
        Content = this.FindChildByName("Content");
        TitleTexture = this.FindChildByName("TitleTexture");
        TitleVideo = this.FindChildByName("TitleVideo");

        ScrollRect = this.GetComponentByChildName<ScrollRect>("ScrollRect");

        ConfirmAnswer = this.GetComponentByChildName<Button>("ConfirmAnswer");
        ConfirmAnswer.onClick.RemoveAllListeners();
        ConfirmAnswer.onClick.AddListener(() =>
        {
            MsgList<int> msgAnswers = new MsgList<int>((ushort)ExercisesModuleEvent.ConfirmAnswer, selectedAnswers.ToList());
            SendMsg(new MsgBrodcastOperate(msgAnswers.msgId, JsonTool.Serializable(msgAnswers)));
        });
        ConfirmAnswer.gameObject.SetActive(!GlobalInfo.IsExamMode());

        #region 直连答题
#if UNITY_ANDROID || UNITY_IOS
        //if (GlobalInfo.IsHomeowner())
        //{
        //    JudgeBtn = this.GetComponentByChildName<Button>("JudgeOnline");
        //    JudgeBtn.onClick.AddListener(() =>
        //    {
        //        int choiceCount = 0;
        //        bool multipleChoice = false;
        //        List<int> correctIndex = new List<int>();

        //        Exercise exercise = (GlobalInfo.currentWiki as EncyclopediaExercise).data.exercise;
        //        switch (exercise.type)
        //        {
        //            //选择题
        //            case 1:
        //                ExerciseContent ec = JsonTool.DeSerializable<ExerciseContent>(exercise.content);
        //                choiceCount = ec.answers.Count;
        //                multipleChoice = ec.answers.FindAll(a => a.right == true).Count > 1;
        //                for (int i = 0; i < ec.answers.Count; i++)
        //                {
        //                    if (ec.answers[i].right)
        //                    {
        //                        correctIndex.Add(i);
        //                    }
        //                }
        //                break;
        //            //判断题
        //            case 2:
        //                JudgementExerciseContent jec = JsonTool.DeSerializable<JudgementExerciseContent>(exercise.content);
        //                choiceCount = 2;
        //                correctIndex.Add(jec.answers ? 0 : 1);
        //                break;
        //        }
        //        UIManager.Instance.OpenModuleUI<JudgeOnlineResultModule>(ParentPanel, ((OPLSynCoursePanel)ParentPanel).JudgeOnlineMenuPoint, new JudgeOnlineData(choiceCount, multipleChoice, correctIndex));
        //        SendMsg(new MsgBase((ushort)JudgeOnlineEvent.Start));
        //        NetworkManager.Instance.SendFrameMsg(new MsgJudgeOnline((ushort)JudgeOnlineEvent.Start, GlobalInfo.currentWiki.id, choiceCount, multipleChoice));
        //    });
        //    JudgeBtn.gameObject.SetActive(true);
        //}
#endif
        #endregion
    }

    /// <summary>
    /// 加载选择题练习
    /// </summary>
    /// <param name="id"></param>
    private void LoadExercise()
    {
        ExerciseContent exerciseContent = JsonTool.DeSerializable<ExerciseContent>(exercise.content);
        Type.text = CheckType(exercise.type, exerciseContent, out bool multipleChoice);
        LoadQuestion(exerciseContent.question);
        LoadAnswers(exerciseContent.answers, multipleChoice);
        RefreshLayouGroup();
    }

    /// <summary>
    /// 加载判断练习
    /// </summary>
    /// <param name="id"></param>
    private void LoadJudgementExercise()
    {
        JudgementExerciseContent exerciseContent = JsonTool.DeSerializable<JudgementExerciseContent>(exercise.content);
        Type.text = "判断题";
        LoadQuestion(exerciseContent.question);
        List<ExerciseAnswer> answers = new List<ExerciseAnswer>()
        {
            new ExerciseAnswer()
            {
                content = new ExerciseQuestion("正确"),
                right = exerciseContent.answers
            },
            new ExerciseAnswer()
            {
                content = new ExerciseQuestion("错误"),
                right = !exerciseContent.answers
            },
        };
        LoadAnswers(answers);
        RefreshLayouGroup();
    }

    private int score;
    /// <summary>
    /// 加载题目
    /// </summary>
    /// <param name="question"></param>
    private void LoadQuestion(ExerciseQuestion question)
    {
        if (GlobalInfo.IsExamMode())
        {
            var target = GlobalInfo.currentWikiList.Find(wiki => wiki.id == GlobalInfo.currentWiki.id);
            if (target != null)
                score = target.totalScore;
            else
                Log.Error($"分值获取出错");
            Title.text = $"({score}分) {question.text}";
        }
        else
        {
            Title.text = question.text;
        }

        TitleTexture.gameObject.SetActive(false);
        TitleVideo.gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(question.image))
        {
            LoadImageContent(question.image, TitleTexture, exercise.id);
        }
        else if (!string.IsNullOrEmpty(question.video))
        {
            LoadVideoContent(question.video, TitleVideo, exercise.id);
        }
    }

    public List<int> _selectedAnswers { get { return selectedAnswers.ToList(); } }
    private HashSet<int> selectedAnswers = new HashSet<int>();

    /// <summary>
    /// 加载选项
    /// </summary>
    /// <param name="answers"></param>
    private void LoadAnswers(List<ExerciseAnswer> answers, bool multipleChoice = false)
    {
        int index = 0;
        bool hasExtend = false;
        Content.UpdateItemsView(answers, (item, info) =>
        {
            Toggle choice = item.GetComponent<Toggle>();
            {
                choice.interactable = true;

                if (multipleChoice)
                    choice.group = null;

                if (answerToggles.ContainsKey(index))
                    answerToggles[index] = choice;
                else
                    answerToggles.Add(index, choice);

                var sendID = index;
                choice.onValueChanged.RemoveAllListeners();
                choice.onValueChanged.AddListener(isOn =>
                {
                    if (GlobalInfo.IsGroupMode())
                    {
                        ToolManager.SendBroadcastMsg(new MsgIntBool()
                        {
                            msgId = (ushort)ExercisesModuleEvent.ChooseAnswer,
                            arg1 = sendID,
                            arg2 = isOn
                        });
                    }
                    else
                    {
                        SendMsg(new MsgIntBool((ushort)ExercisesModuleEvent.ChooseAnswer, sendID, isOn));
                    }

                    if (isOn)
                        selectedAnswers.Add(sendID);
                    else
                        selectedAnswers.Remove(sendID);
                });

                item.FindChildByName("True").gameObject.SetActive(false);
                item.FindChildByName("False1").gameObject.SetActive(false);
                item.FindChildByName("False2").gameObject.SetActive(false);
            }

            item.name = info.right ? "T" : "F";
            item.GetComponentByChildName<Text>("ItemTitle").text = ((char)('A' + index++)).ToString();
            item.GetComponentByChildName<Text>("ItemContent").text = info.content.text;

            Transform ItemTexture = item.FindChildByName("ItemTexture");
            Transform ItemVideo = item.FindChildByName("ItemVideo");
            ItemTexture.gameObject.SetActive(false);
            ItemVideo.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(info.content.image))
            {
                LoadImageContent(info.content.image, ItemTexture, exercise.id);
            }
            else if (!string.IsNullOrEmpty(info.content.video))
            {
                LoadVideoContent(info.content.video, ItemVideo, exercise.id);
            }

            if (!string.IsNullOrEmpty(info.content.image) || !string.IsNullOrEmpty(info.content.video))
            {
                hasExtend = true;
            }
        });

        if (hasExtend)
        {
            if (isAndroid)
                Content.GetComponent<GridLayoutGroup>().cellSize = new Vector2(720, 266);
            else
                Content.GetComponent<GridLayoutGroup>().cellSize = new Vector2(480, 214);
        }
        else
        {
            if (isAndroid)
                Content.GetComponent<GridLayoutGroup>().cellSize = new Vector2(720, 200);
            else
                Content.GetComponent<GridLayoutGroup>().cellSize = new Vector2(480, 54);
        }
    }

    /// <summary>
    /// 加载图片
    /// </summary>
    /// <param name="contentUrl"></param>
    /// <param name="target"></param>
    /// <param name="id"></param>
    private void LoadImageContent(string contentUrl, Transform target, int id = -1)
    {
        target.GetComponentInChildren<RawImage>().texture = null;

        Button button = target.GetComponentInChildren<Button>();
        {
            button.onClick.RemoveAllListeners();

            ResManager.Instance.LoadExerciseImage(contentUrl, (texture) =>
            {
                if (texture)
                {
                    if (target)
                    {
                        target.GetComponentInChildren<RawImage>().texture = texture;
                        target.GetComponentInChildren<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
                    }
                    if (button)
                    {
                        button.onClick.AddListener(() =>
                        {
                            //ToolManager.SendBroadcastMsg(new MsgString((ushort)ExercisesModuleEvent.OpenAnswerImg, contentUrl), true);
                            SendMsg(new MsgString((ushort)ExercisesModuleEvent.OpenAnswerImg, contentUrl));
                        });
                    }
                }
                else
                {
                    //todo 加载失败 刷新
                }
            });
        }

        target.gameObject.SetActive(true);
    }

    /// <summary>
    /// 加载视频
    /// </summary>
    /// <param name="contentUrl"></param>
    /// <param name="target"></param>
    /// <param name="id"></param>
    private void LoadVideoContent(string contentUrl, Transform target, int id = -1)
    {
        target.gameObject.SetActive(true);

        RawImage rawImage = target.GetComponentByChildName<RawImage>("Texture");
        rawImage.gameObject.SetActive(false);

        Button button = target.GetComponentInChildren<Button>(true);
        {
            button.onClick.RemoveAllListeners();

            target.GetComponent<GetFirstVideoImage>().LoadVideoPreview(ResManager.Instance.OSSDownLoadPath + contentUrl, (texture) =>
            {
                rawImage.texture = texture;
                rawImage.gameObject.SetActive(true);
            });

            button.onClick.AddListener(() =>
            {
                //ToolManager.SendBroadcastMsg(new MsgString((ushort)ExercisesModuleEvent.OpenAnswerVideo, ResManager.Instance.OSSDownLoadPath + contentUrl), true);
                SendMsg(new MsgString((ushort)ExercisesModuleEvent.OpenAnswerVideo, ResManager.Instance.OSSDownLoadPath + contentUrl));
            });
        }
    }

    private string CheckType(int type, ExerciseContent exerciseContent, out bool multipleChoice)
    {
        string content;
        multipleChoice = false;
        switch (type)
        {
            case 1:
                int rightNum = 0;
                foreach (var answer in exerciseContent.answers)
                {
                    if (answer.right)
                        rightNum++;
                }

                if (rightNum >= 2)
                {
                    content = "多选题";
                    multipleChoice = true;
                }
                else
                    content = "单选题";
                break;
            case 2:
                content = "判断题";
                break;
            case 3:
                content = "填空题";
                break;
            default:
                content = "未知";
                break;
        }
        return content;
    }

    private void OnAnswerConfirm(int userId, List<int> answers)
    {
        //if (GlobalInfo.IsExamMode())
        //    return;
        bool isTrue = true;
        Toggle tempToggle = null;
        {
            foreach (Transform child in Content)
            {
                tempToggle = child.GetComponentInChildren<Toggle>();
                tempToggle.interactable = false;
                tempToggle.SetIsOnWithoutNotify(answers.Contains(child.GetSiblingIndex() - 1));

                if (child.name == "T")
                {
                    if (tempToggle.isOn)
                    {
                        tempToggle.FindChildByName("True").gameObject.SetActive(true);
                    }
                    else
                    {
                        tempToggle.FindChildByName("False2").gameObject.SetActive(true);
                        isTrue = false;
                    }
                }
                else if (tempToggle.isOn)
                {
                    tempToggle.FindChildByName("False1").gameObject.SetActive(true);
                    isTrue = false;
                }
            }
        }

        if (isTrue)
            SoundManager.Instance.PlayEffect("TrueProblem", true);
        else
            SoundManager.Instance.PlayEffect("FalseProblem", true);

        ////为了保留提交答案记录可见
        //if (GlobalInfo.IsExamMode() && userId == GlobalInfo.account.id)
        //    SendMsg(new MsgInt((ushort)ExamPanelEvent.ExerciseScore, isTrue ? score : 0));
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
#endif
            case (ushort)ExercisesModuleEvent.ChooseAnswer:
                MsgIntBool answerClickMsg;
                if (GlobalInfo.IsGroupMode())
                    answerClickMsg = (msg as MsgBrodcastOperate).GetData<MsgIntBool>();
                else
                    answerClickMsg = (MsgIntBool)msg;
                if (answerToggles.TryGetValue(answerClickMsg.arg1, out Toggle answer))
                {
                    answer.isOn = answerClickMsg.arg2;
                }
                OnAnswerChanged?.Invoke();
                break;
            case (ushort)ExercisesModuleEvent.OpenAnswerImg:
                string imgPath = ((MsgString)msg).arg;
                ShowLinkModuleData moduleData = new ShowLinkModuleData(
                    0, string.Empty, imgPath, FileExtension.IMG,
                    () => SendMsg(new MsgString((ushort)ExercisesModuleEvent.CloseAnswerImg, exercise.id.ToString())), true
                );
                UIManager.Instance.OpenModuleUI<ShowImgModule>(ParentPanel, ((OPLCoursePanel)ParentPanel).ShowModulePoint, moduleData);
                break;
            case (ushort)ExercisesModuleEvent.CloseAnswerImg:
                MsgString imgClose = (MsgString)msg;
                UIManager.Instance.CloseModuleUI<ShowImgModule>(ParentPanel, new ShowLinkModuleData(0, imgClose.arg, FileExtension.IMG, true));
                break;
            case (ushort)ExercisesModuleEvent.OpenAnswerVideo:
                string videoUrl = ((MsgString)msg).arg;
                UIManager.Instance.OpenModuleUI<ShowVideoModule>(ParentPanel, ((OPLCoursePanel)ParentPanel).ShowModulePoint, new ShowLinkModuleData(
                     0, string.Empty, videoUrl, FileExtension.IMG,
                     () => SendMsg(new MsgString((ushort)ExercisesModuleEvent.CloseAnswerVideo, exercise.id.ToString())), true
                ));
                break;
            case (ushort)ExercisesModuleEvent.CloseAnswerVideo:
                MsgString videoClose = (MsgString)msg;
                UIManager.Instance.CloseModuleUI<ShowVideoModule>(ParentPanel, new ShowLinkModuleData(0, videoClose.arg, FileExtension.MP4, true));
                break;
            case (ushort)ExercisesModuleEvent.ConfirmAnswer:
                OnAnswerConfirm(((MsgBrodcastOperate)msg).senderId, ((MsgBrodcastOperate)msg).GetData<MsgList<int>>().arg);
                break;
            #region 直连答题
            case (ushort)JudgeOnlineEvent.Start:
                ClearAnswer();
                break;
            case (ushort)JudgeOnlineEvent.Complete:
            case (ushort)JudgeOnlineEvent.End:
                break;
                #endregion
        }
    }

    /// <summary>
    /// 清空答案
    /// </summary>
    private void ClearAnswer()
    {
        Toggle tempToggle = null;
        foreach (Transform child in Content)
        {
            tempToggle = child.GetComponentInChildren<Toggle>();
            tempToggle.SetIsOnWithoutNotify(false);
            tempToggle.interactable = true;
            tempToggle.FindChildByName("True").gameObject.SetActive(false);
            tempToggle.FindChildByName("False1").gameObject.SetActive(false);
            tempToggle.FindChildByName("False2").gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 选项打勾
    /// </summary>
    /// <param name="indexs"></param>
    public void SelectAnswerToggles(List<int> indexs)
    {
        foreach (var item in indexs)
        {
            if (answerToggles.ContainsKey(item))
            {
                answerToggles[item].isOn = true;
            }
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        Timer.DelTimer(name);
    }

    /// <summary>
    /// 进入动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void JoinAnim(UnityAction callback)
    {
        CanvasGroup canvasGroup = transform.GetComponentInChildren<CanvasGroup>();
        if (canvasGroup != null)
        {
            JoinSequence.Append(DOTween.To(() => 0f, (value) => canvasGroup.alpha = (value), 1f, JoinAnimePlayTime));
        }
        //transform.GetChild(0).localScale = Vector3.one * 0.001f;
        //JoinSequence.Join(transform.GetChild(0).DOScale(Vector3.one, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    /// <summary>
    /// 退出动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void ExitAnim(UnityAction callback)
    {
        CanvasGroup canvasGroup = transform.GetComponentInChildren<CanvasGroup>();
        if (canvasGroup != null)
        {
            ExitSequence.Append(DOTween.To(() => 1f, (value) => canvasGroup.alpha = (value), 0f, ExitAnimePlayTime));
        }
        //transform.GetChild(0).localScale = Vector3.one * 0.001f;
        //ExitSequence.Join(transform.GetChild(0).DOScale(Vector3.one * 0.001f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
}
