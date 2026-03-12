using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class HomePagePanel : UIPanelBase
{
    protected override bool CanLogout { get { return true; } }
    public override bool canOpenOption => true;

//#if UNITY_ANDROID || UNITY_IOS
//    private Button AR;
//#endif

    private Button Setting;
    
    /// <summary>
    /// 资源
    /// </summary>
    private Button Resources;
    /// <summary>
    /// 考核
    /// </summary>
    private Button Exam;
    /// <summary>
    /// 协同
    /// </summary>
    private Button Synergia;

    /// <summary>
    /// 用户昵称
    /// </summary>
    private Text userName;
    /// <summary>
    /// 单位
    /// </summary>
    private Text company;


    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        //从这个页面进入资源选择页的时候 重置状态
        ResourcesPanel.request = true;
        ResourcesPanel.isOverview = true;
        ResourcesPanel.state = -2;
        ResourcesPanel.category = string.Empty;
        ResourcesPanel.categoryIndex = 0;
        ResourcesPanel.searchKeyword = string.Empty;
        ResourcesPanel.subCategory = string.Empty;
        ResourcesPanel.courseTag = string.Empty;

        ARPanel.request = true;

        AddMsg(new ushort[]
        {
            (ushort)OptionPanelEvent.Name,
            (ushort)OptionPanelEvent.Org
        });

        Init(uiData);
    }

    private void Init(UIData uiData = null)
    {
        Setting = transform.GetComponentByChildName<Button>("User");
        Setting.onClick.AddListener(() =>
        {
            UIManager.Instance.OpenUI<OptionPanel>(UILevel.Fixed);
        });

        company = Setting.GetComponentByChildName<Text>("CompanyText");
        //company.text = GlobalInfo.account.schoolName;
        company.text = GlobalInfo.account.userOrgName.EllipsisText(15, "...");

        userName = Setting.GetComponentByChildName<Text>("NameText");
        userName.text = GlobalInfo.account.nickname;

        Resources = transform.GetComponentByChildName<Button>("Resources");
        Synergia = transform.GetComponentByChildName<Button>("Synergia");
        Exam = transform.GetComponentByChildName<Button>("Exam");

        //扫描快速加入已弃用
//#if UNITY_ANDROID || UNITY_IOS
//        AR = transform.GetComponentByChildName<Button>("AR");
//        AR.onClick.AddListener(() =>
//        {
//            UIManager.Instance.CloseUI<HomePagePanel>();
//            UIManager.Instance.OpenUI<ARPanel>();
//        });
//#endif

        Resources.onClick.AddListener(() =>
        {
            UIManager.Instance.CloseUI<HomePagePanel>();
            UIManager.Instance.OpenUI<ResourcesPanel>();
        });

        Exam.onClick.AddListener(() =>
        {
            UIManager.Instance.CloseUI<HomePagePanel>();
            UIManager.Instance.OpenUI<ExamTrainingPanel>();
        });

        Synergia.onClick.AddListener(() =>
        {
            if (GlobalInfo.isOffLine)
            {
                ToolManager.UnsupportOffline();
                return;
            }
            UIManager.Instance.CloseUI<HomePagePanel>();
            UIManager.Instance.OpenUI<TrainingPanel>();
        });

        if (!GlobalInfo.account.allowCourse)
            Resources.interactable = false;

        if (!GlobalInfo.account.allowCoordination)
            Synergia.interactable = false;

        if (!GlobalInfo.account.allowExamine)
            Exam.interactable = false;
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)OptionPanelEvent.Name:
                userName.text = GlobalInfo.account.nickname;
                break;
            case (ushort)OptionPanelEvent.Org:
                company.text = GlobalInfo.account.userOrgName.EllipsisText(15, "...");
                break;
        }
    }
}