using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;
using WebSocketSharp;
using static UnityEngine.UI.InputField;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 考核历史记录
/// </summary>
public class RecordPanel : UIPanelBase
{
    /// <summary>
    /// 考核记录搜索输入框
    /// </summary>
    private InputField SearchRecord;
    /// <summary>
    /// 考核记录列表
    /// </summary>
    private List<HisExam> records;

    /// <summary>
    /// 考生搜索输入框
    /// </summary>
    private InputField SearchPersonnal;
    /// <summary>
    /// 考核人员列表
    /// </summary>
    private List<ExamResult> personnalList;
    /// <summary>
    /// 人员列表UI
    /// </summary>
    private Transform PersonnalMenu;

    /// <summary>
    /// 答题结果UI
    /// </summary>
    private Transform DetailMenu;
    private Transform DetailContent;
    /// <summary>
    /// 习题item
    /// </summary>
    private Transform item_Exercise;
    /// <summary>
    /// 操作百科item
    /// </summary>
    private Transform item_OpWiki;
    /// <summary>
    /// 操作任务item
    /// </summary>
    private Transform item_OpFlow;
    /// <summary>
    /// 操作步骤item
    /// </summary>
    private Transform item_OpStep;
    /// <summary>
    /// 答题item集合
    /// </summary>
    private List<Transform> items = new List<Transform>();
    //private List<RecordListDetail> joinRecords = new List<RecordListDetail>();
    //private List<RecordListDetail> createRecords = new List<RecordListDetail>();
    //private List<ResultListDetail> results = new List<ResultListDetail>();

    //private bool isCreater;

    /// <summary>
    /// 百科记录结果列表
    /// </summary>
    private List<Answer> answers = new List<Answer>();
    /// <summary>
    /// 考核-学生id
    /// </summary>
    private int id;
    ///// <summary>
    ///// 选择的考核id
    ///// </summary>
    private int examid;

    public override bool canOpenOption => true;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        RequestManager.Instance.GetExamRecordList(data =>
        {
            records = data.records;
            RefreshList();
        }, error =>
        {
            //todo 页面显示提示
            Log.Error("获取考核记录列表失败：" + error);
        });

        this.GetComponentByChildName<Button>("Exit").onClick.AddListener(() =>
        {
            UIManager.Instance.OpenUI<ExamTrainingPanel>();
            UIManager.Instance.CloseUI<RecordPanel>();
        });

        PersonnalMenu = this.FindChildByName("PersonnalMenu");
        this.GetComponentByChildName<Button>("PersonnalBack").onClick.AddListener(() =>
        {
            PersonnalMenu.gameObject.SetActive(false);
        });

        DetailMenu = this.FindChildByName("DetailMenu");
        this.GetComponentByChildName<Button>("DetailBack").onClick.AddListener(() =>
        {
            DetailMenu.gameObject.SetActive(false);
        });

        //详细界面的提交和保存按钮
        this.GetComponentByChildName<Button>("DetailMenuSubmit").onClick.AddListener(() =>
        {
            SubmitOrSaveData(true);
        });
        this.GetComponentByChildName<Button>("DetailMenuSave").onClick.AddListener(() =>
        {
            SubmitOrSaveData(false);
        });

        DetailContent = DetailMenu.FindChildByName("DetailContent"); ;
        item_Exercise = DetailMenu.FindChildByName("Item_Exercise"); ;
        item_OpWiki = DetailMenu.FindChildByName("Item_OpWiki"); ;
        item_OpFlow = DetailMenu.FindChildByName("Item_OpFlow"); ;
        item_OpStep = DetailMenu.FindChildByName("Item_OpStep"); ;

        SearchRecord = this.GetComponentByChildName<InputField>("SearchRecord");
        SearchRecord.onValueChanged.AddListener(content =>
        {
            RefreshList();
        });

        SearchPersonnal = this.GetComponentByChildName<InputField>("SearchPersonnal");
        SearchPersonnal.onValueChanged.AddListener(content =>
        {
            RefreshPersonnalList();
        });

        this.FindChildByName("Join").parent.gameObject.SetActive(false);

        //TODO 考虑是否去掉筛选
        //var joinText = this.GetComponentByChildName<Toggle>("Join").GetComponentInChildren<Text>();
        //var createrText = this.GetComponentByChildName<Toggle>("Creater").GetComponentInChildren<Text>();

        //this.GetComponentByChildName<Toggle>("Join").onValueChanged.AddListener(isOn =>
        //{
        //    if (isOn)
        //    {
        //        isCreater = false;
        //        joinText.SetAlpha(1);
        //        createrText.SetAlpha(0.5f);
        //    }
        //    else
        //    {
        //        isCreater = true;
        //        joinText.SetAlpha(0.5f);
        //        createrText.SetAlpha(1f);
        //    }
        //    RefreshList();
        //});
    }
    /// <summary>
    /// 搜索考核记录
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    private List<HisExam> FilterList(List<HisExam> datas)
    {
        var newDatas = new List<HisExam>(datas);

        if (!string.IsNullOrEmpty(SearchRecord.text))
        {
            newDatas = newDatas.Where(value => value.name.Contains(SearchRecord.text) ||
                value.createTime.Replace("-", string.Empty).Contains(SearchRecord.text.Replace("/", string.Empty))).ToList();
        }
        if (newDatas.Count == 0)
        {
            this.FindChildByName("List").gameObject.SetActive(false);
            this.FindChildByName("Empty").gameObject.SetActive(true);
            this.GetComponentByChildName<Text>("EmptyText").text = string.IsNullOrEmpty(SearchRecord.text) ? "无该条记录" : "当前暂无考核记录";
        }
        else
        {
            this.FindChildByName("List").gameObject.SetActive(true);
            this.FindChildByName("Empty").gameObject.SetActive(false);
        }

        return newDatas;
    }
    private void RefreshList()
    {
        this.FindChildByName("Content").RefreshItemsView(FilterList(records), (item, data) =>
        {
            var time = DateTime.Parse(data.createTime);

            item.GetComponentByChildName<Text>("Name").text = data.name;
            item.GetComponentByChildName<Text>("CourseName").text = data.courseName;
            item.GetComponentByChildName<Text>("CreateTime").text = time.ToString("yyyy/MM/dd HH:mm");
            item.GetComponentByChildName<Text>("Duration").text = data.duration.ToString();
            item.GetComponentByChildName<Text>("Checked").text = data.allChecked ? "已阅卷" : "未阅卷";

            var button = item.GetComponentInChildren<Button>(true);
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    PersonnalMenu.GetComponentByChildName<Text>("DetailTitle").text = data.name;
                    PersonnalMenu.GetComponentByChildName<Text>("DetailTime").text = time.ToString("yyyy/MM/dd HH:mm");

                    RequestManager.Instance.GetExamResultList(data.id, personnal =>
                    {
                        examid = data.id;
                        personnalList = personnal.records;
                        RefreshPersonnalList();
                        PersonnalMenu.gameObject.SetActive(true);
                    }, error =>
                    {
                        Debug.LogError(error);
                    });
                });
            }
        });
    }

    /// <summary>
    /// 搜索考核人员
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    private List<ExamResult> FilterList(List<ExamResult> datas)
    {
        var newDatas = new List<ExamResult>(datas);

        if (!string.IsNullOrEmpty(SearchPersonnal.text))
        {
            newDatas = newDatas.Where(value => value.examineeName.Contains(SearchPersonnal.text)).ToList();
        }

        return newDatas;
    }
    private void RefreshPersonnalList()
    {
        PersonnalMenu.FindChildByName("PersonnalContent").RefreshItemsView(FilterList(personnalList), (item, data) =>
        {
            item.GetComponentByChildName<Text>("ExamineeName").text = data.examineeName;
            item.GetComponentByChildName<Text>("ExamineTime").text = data.examineTime.ToString();
            item.GetComponentByChildName<Text>("Score").text = data.score.ToString();
            item.GetComponentByChildName<Text>("Checked").text = data.allChecked ? "已阅卷" : "未阅卷";

            var button = item.GetComponentInChildren<Button>(true);
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    DetailMenu.GetComponentByChildName<Text>("DetailName").text = data.examineeName;
                    DetailMenu.GetComponentByChildName<Text>("DetailTime").text = data.examineTime.ToString();

                    //TODO
                    //RequestManager.Instance.GetExamRecordByRecordId(data.id, (id, answers, accessoryList) =>
                    //{
                    //    this.id = id;
                    //    RefreshDetailList(answers, accessoryList);
                    //    DetailMenu.gameObject.SetActive(true);

                    //    this.FindChildByName("SubmitOrSaveButton").gameObject.SetActive(!data.allChecked);
                    //}, error =>
                    //{
                    //    Debug.LogError(error);
                    //});
                });
            }
        });
    }
    /// <summary>
    /// 记录点击时间
    /// </summary>
    private float timer;
    private void RefreshDetailList(List<Answer> answersList, List<Accessory> accessoryList)
    {
        answers = answersList;

        for (int x = 0; x < items.Count; x++)
        {
            Destroy(items[x].gameObject);
        }
        items.Clear();

        if (answersList == null)
            return;

        for (int i = 0; i < answersList.Count; i++)
        {
            switch (answersList[i].typeId)
            {
                case (int)PediaType.Exercise:
                    AnswerExercise answerExercise = answersList[i] as AnswerExercise;
                    Transform exercise = Instantiate(item_Exercise, DetailContent);
                    exercise.GetComponentByChildName<Text>("Num").text = (i + 1).ToString();
                    exercise.GetComponentByChildName<Text>("title").text = answerExercise.title;
                    exercise.GetComponentByChildName<Text>("operation").text = answerExercise.operation;
                    exercise.GetComponentByChildName<Text>("score").text = answerExercise.score.ToString();
                    exercise.GetComponentByChildName<Text>("standard").text = answerExercise.standard;
                    InputField getScore = exercise.GetComponentByChildName<InputField>("InputField_getScore");
                    getScore.text = answerExercise.getScore.ToString();
                    CanvasGroup canvasGroup = getScore.GetComponent<CanvasGroup>();
                    canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false;
                    exercise.GetComponentByChildName<Button>("Btn_getScore").onClick.AddListener(()=> {
                        if (Time.time - timer < 1 && GlobalInfo.account.roleType == 1 && !GetCurrentPersonal() .allChecked)//双击且可编辑
                        {
                            canvasGroup.blocksRaycasts = true;
                            canvasGroup.interactable = true;
                            getScore.Select();
                        }
                        timer = Time.time;
                    });

                    getScore.onEndEdit.AddListener((string str) => 
                    {
                        canvasGroup.blocksRaycasts = false;
                        canvasGroup.interactable = false;
                        answerExercise.getScore = int.Parse(str.IsNullOrEmpty() ? "0" : str);
                        //answerExercise.state = 1;
                    });

                    //只允许输入数字
                    getScore.onValidateInput = (string text, int charIndex, char addedChar) =>
                    {
                        if (Convert.ToInt32(addedChar) >= 48 && Convert.ToInt32(addedChar) <= 57)
                            return addedChar;
                        else
                            return '\0';

                    };

                    exercise.gameObject.SetActive(true);
                    items.Add(exercise);
                    break;
                case (int)PediaType.Operation:
                    AnswerOp answerOp = answersList[i] as AnswerOp;
                    Transform opWiki = Instantiate(item_OpWiki, DetailContent);

                    opWiki.GetComponentByChildName<Text>("Num").text = (i + 1).ToString();
                    opWiki.GetComponentByChildName<Text>("title").text = answerOp.title;

                    opWiki.GetComponentByChildName<Button>("video").onClick.AddListener(() =>
                    {
                        string url = accessoryList.FirstOrDefault(item => item.encyclopediaId == answerOp.baikeId && item.examineeId == GlobalInfo.account.id)?.filePath;
                        if (!url.IsNullOrEmpty())
                        {
                            UIModuleBase item = null;
                            item = UIManager.Instance.OpenModuleUI<ShowExamVideoModule>(this, DetailMenu, new ShowExamModuleData(answerOp.title, url, FileExtension.MP4));
                        }
                    });
                    opWiki.gameObject.SetActive(true);
                    items.Add(opWiki);
                    int childCount = 0;
                    for (int j = 0; j < answerOp.children.Count; j++)
                    {
                        childCount += 1;
                        Transform opFlow = Instantiate(item_OpFlow, DetailContent);
                        opFlow.GetComponentByChildName<Text>("Num").text = $"({j + 1})";
                        opFlow.GetComponentByChildName<Text>("title").text = answerOp.children[j].title;
                        opFlow.gameObject.SetActive(true);
                        items.Add(opFlow);
                        for (int k = 0; k < answerOp.children[j].children.Count; k++)
                        {
                            childCount += 1;
                            AnswerStep answerStep = answerOp.children[j].children[k];
                            Transform opStep = Instantiate(item_OpStep, DetailContent);
                            opStep.GetComponentByChildName<Text>("Num").text = $"({j + 1}.{k + 1})";
                            opStep.GetComponentByChildName<Text>("title").text = answerStep.title;
                            //opStep.GetComponentByChildName<Text>("operation").text = answerStep.operation;
                            opStep.GetComponentByChildName<Text>("score").text = answerStep.score.ToString();
                            opStep.GetComponentByChildName<Text>("standard").text = answerStep.standard;
                            //opStep.GetComponentByChildName<Text>("getScore").text = answerStep.getScore.ToString();

                            InputField opGetScore = opStep.GetComponentByChildName<InputField>("InputField_getScore");
                            opGetScore.text = answerStep.getScore.ToString();
                            CanvasGroup opCanvasGroup = opGetScore.GetComponent<CanvasGroup>();
                            opCanvasGroup.blocksRaycasts = false; opCanvasGroup.interactable = false;
                            opStep.GetComponentByChildName<Button>("Btn_getScore").onClick.AddListener(() => {
                                if (Time.time - timer < 1 && GlobalInfo.account.roleType == 1 && !GetCurrentPersonal().allChecked)//双击且可编辑
                                {
                                    opCanvasGroup.blocksRaycasts = true;
                                    opCanvasGroup.interactable = true;
                                    opGetScore.Select();
                                }
                                timer = Time.time;
                            });

                            //只允许输入数字
                            opGetScore.onValidateInput = (string text, int charIndex, char addedChar) =>
                            {
                                if (Convert.ToInt32(addedChar) >= 48 && Convert.ToInt32(addedChar) <= 57)
                                    return addedChar;
                                else
                                    return '\0';

                            };

                            opGetScore.onEndEdit.AddListener((string str) =>
                            {
                                opCanvasGroup.blocksRaycasts = false;
                                opCanvasGroup.interactable = false;
                                answerStep.getScore = int.Parse(str.IsNullOrEmpty() ? "0" : str);
                                //answerStep.state = 1;
                            });

                            opStep.gameObject.SetActive(true);
                            items.Add(opStep);
                        }
                    }

                      (opWiki.FindChildByName("bg") as RectTransform).sizeDelta += new Vector2(0, childCount * (64 + 16));
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// 获取当前选中的学生试卷（需要在学生试卷查看界面才能使用）
    /// </summary>
    /// <returns></returns>
    private ExamResult GetCurrentPersonal() 
    {
        foreach (var item in personnalList) 
        {
            if (item.id == id) 
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// 上传阅卷数据
    /// </summary>
    /// <param name="isCheckAll">true表示阅卷完成，false只保存数据，不进行阅卷</param>
    private void SubmitOrSaveData(bool isCheckAll) 
    {
        //todo 异常处理
        if (answers == null)
            return;

        float score = 0;

        //拼凑最终得分和判断是否有题目被打分（是否阅卷）
        foreach (var item in answers) 
        {
            switch (item.typeId)
            {
                case (int)PediaType.Exercise:
                    score += (item as AnswerExercise).getScore;

                    break;
                case (int)PediaType.Operation:
                    AnswerOp answerOp = item as AnswerOp;

                    for (int j = 0; j < answerOp.children.Count; j++)
                    {
                        for (int k = 0; k < answerOp.children[j].children.Count; k++)
                        {
                            AnswerStep answerStep = answerOp.children[j].children[k];
                            score += answerStep.getScore;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        //TODO
        //RequestManager.Instance.SubmitCheckPaper(isCheckAll, JsonTool.Serializable(answers), 0, id, score, 0, () =>
        //{
        //    if (isCheckAll)
        //    {
        //        UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("阅卷完成!"));
        //        this.FindChildByName("SubmitOrSaveButton").gameObject.SetActive(false);
        //    }
        //    else 
        //    {
        //        UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("已保存阅卷记录!"));
        //    }

        //    Log.Debug("阅卷记录提交成功");
        //    //todo:需要增加阅卷成功后的提示
        //    //刷新学生考核界面的分数数据
        //    RequestManager.Instance.GetExamResultList(examid, personnal =>
        //    {
        //        personnalList = personnal.records;
        //        RefreshPersonnalList();
        //        PersonnalMenu.gameObject.SetActive(true);
        //    }, error =>
        //    {
        //        Log.Error(error);
        //    });

        //    //刷新考核试卷选择界面
        //    RequestManager.Instance.GetExamRecordList(data =>
        //    {
        //        records = data.records;
        //        RefreshList();
        //    }, error =>
        //    {
        //        //todo 页面显示提示
        //        Log.Error("获取考核记录列表失败：" + error);
        //    });
        //},
        //(error) =>
        //{
        //    //TODO待完善异常处理
        //    Log.Error("阅卷记录提交失败：" + error);
        //});
    }
}