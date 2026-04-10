using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using static AdaptiveListModule;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 课程页面侧边栏 包含各模块开关按钮、百科切换按钮
/// </summary>
public class CourseSideBar : MonoBase
{
    public bool EnableVerticalLayout;

    private OPLCoursePanel ParentPanel;

    protected CanvasGroup SideBarCanvas;

    /// <summary>
    /// 层级面板开关
    /// </summary>
    public Toggle HierarchyTog { get; private set; }
    /// <summary>
    /// 动画列表开关
    /// </summary>
    public Toggle AnimListTog { get; private set; }
    /// <summary>
    /// 操作列表开关
    /// </summary>
    public Toggle OperationListTog { get; private set; }
    /// <summary>
    /// 知识点面板开关
    /// </summary>
    public Toggle KnowledgeTog { get; private set; }
    /// <summary>
    /// 操作记录列表面板开关
    /// </summary>
    public Toggle HistoryTog { get; private set; }
#if UNITY_ANDROID || UNITY_IOS
    /// <summary>
    /// 分割线
    /// </summary>
    //public GameObject SplitLine { get; private set; }
#endif
    /// <summary>
    /// 百科列表开关
    /// </summary>
    public Toggle ShowBaike { get; private set; }
    /// <summary>
    /// 上一个百科
    /// </summary>
    public Button_LinkMode Prev { get; private set; }
    /// <summary>
    /// 下一个百科
    /// </summary>
    public Button_LinkMode Next { get; private set; }

#if UNITY_ANDROID || UNITY_IOS
    protected Text BaikeCurrentPage;
    protected Text BaikeTotalPage;
#else
    protected Text BaikePage;
#endif

    /// <summary>
    /// 记录移动端右侧层级列表开关状态
    /// </summary>
    protected bool isHierarchyTogOn = false;
    /// <summary>
    /// 记录右侧课件资料开关状态
    /// </summary>
    protected bool isKnowledgeTogOn = false;

    public void InitUI(OPLCoursePanel coursePanel)
    {
        this.ParentPanel = coursePanel;

        AddMsg(new ushort[]{
            (ushort)CoursePanelEvent.HierarchyBtn,
            (ushort)CoursePanelEvent.AnimListBtn,
            (ushort)CoursePanelEvent.OperationListBtn,
            (ushort)BaikeSelectModuleEvent.BaikeSelect,
            (ushort)BaikeSelectModuleEvent.Hide,
            (ushort)KnowledgeModuleEvent.Show,
            (ushort)KnowledgeModuleEvent.Hide,
            (ushort)HierarchyEvent.Hide,
            (ushort)AdaptiveListEvent.Hide,
            (ushort)OperationListEvent.Hide,
            (ushort)HistoryEvent.Hide
        });

        SideBarCanvas = transform.GetComponentByChildName<CanvasGroup>("SideBar");
        HierarchyTog = transform.GetComponentByChildName<Toggle>("Hierarchy");
        AnimListTog = transform.GetComponentByChildName<Toggle>("AnimList");
        OperationListTog = transform.GetComponentByChildName<Toggle>("OperationList");
        HistoryTog = transform.GetComponentByChildName<Toggle>("History");
        KnowledgeTog = transform.GetComponentByChildName<Toggle>("Knowledge");

        ShowBaike = this.GetComponentByChildName<Toggle>("ShowBaike");
        Prev = this.GetComponentByChildName<Button_LinkMode>("Prev");
        Next = this.GetComponentByChildName<Button_LinkMode>("Next");
        SwitchBaikeAnim();
#if UNITY_ANDROID || UNITY_IOS
        //SplitLine = this.FindChildByName("SplitLine").gameObject;
        BaikeCurrentPage = this.GetComponentByChildName<Text>("BaikeCurrentPage");
        BaikeTotalPage = this.GetComponentByChildName<Text>("BaikeTotalPage");
#else
        BaikePage = this.GetComponentByChildName<Text>("BaikePage");
#endif

        ShowBaike.onValueChanged.AddListener((isOn) =>
        {
            active = true;
            if (isOn)
            {
#if UNITY_STANDALONE
                if(HierarchyTog)
                    HierarchyTog.isOn = false;
                if(AnimListTog)
                    AnimListTog.isOn = false;
                OperationListTog.isOn = false;
                HistoryTog.isOn = false;
#elif UNITY_ANDROID || UNITY_IOS
                ParentPanel.EmptyClick.gameObject.SetActive(true);
#endif
                UIManager.Instance.OpenModuleUI<BaikeSelectModule>(ParentPanel, ParentPanel.ListModulePoint);
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.LeftFlex, true));
            }
            else
            {
#if UNITY_STANDALONE
                //考核自动显示historyModule
                if (GlobalInfo.IsExamMode())
                    HistoryTog.isOn = true;
#endif
                SendMsg(new MsgBase((ushort)BaikeSelectModuleEvent.Hide));
            }
        });

        Prev.onClick.AddListener(() =>
        {
            active = true;
            if (BaikeSelectModule.CurrentBaikeIndex == 0)
                return;

            Encyclopedia prevPedia = GlobalInfo.currentWikiList[--BaikeSelectModule.CurrentBaikeIndex];
            ToolManager.SendBroadcastMsg(new MsgInt((ushort)BaikeSelectModuleEvent.BaikeSelect, prevPedia.id), true);
        });
        Next.onClick.AddListener(() =>
        {
            active = true;
            if (BaikeSelectModule.CurrentBaikeIndex == GlobalInfo.currentWikiList.Count - 1)
                return;

            Encyclopedia nextPedia = GlobalInfo.currentWikiList[++BaikeSelectModule.CurrentBaikeIndex];
            ToolManager.SendBroadcastMsg(new MsgInt((ushort)BaikeSelectModuleEvent.BaikeSelect, nextPedia.id), true);
        });

        if (HierarchyTog)
        {
            HierarchyTog.onValueChanged.AddListener((isOn) =>
            {
                active = true;
                isHierarchyTogOn = isOn;
                if (isOn)
                {
                    CloseExclusiveModule();
                    UIManager.Instance.OpenModuleUI<ModelHierarchyModule>(ParentPanel, ParentPanel.HierarchyModulePoint);
                }
                else
                {
                    SendMsg(new MsgBase((ushort)HierarchyEvent.Hide));
                }
            });
        }

        if (AnimListTog)
        {
            AnimListTog.onValueChanged.AddListener((isOn) =>
            {
                active = true;
                isHierarchyTogOn = isOn;
                if (isOn)
                {
                    CloseExclusiveModule();
                    UIManager.Instance.OpenModuleUI<AdaptiveListModule>(ParentPanel, ParentPanel.HierarchyModulePoint,
                        new AdaptiveListData(AdaptiveType.Anim, (id) => SendMsg(new MsgString((ushort)IntegrationModuleEvent.AnimSelect, id))));
                }
                else
                    SendMsg(new MsgBase((ushort)AdaptiveListEvent.Hide));
            });
        }
      
        OperationListTog.onValueChanged.AddListener((isOn) =>
        {
            active = true;
            isHierarchyTogOn = isOn;
            if (isOn)
            {
                CloseExclusiveModule();
                HistoryTog.isOn = false;
                SendMsg(new MsgBool((ushort)OperationListEvent.Open, isHierarchyTogOn));
            }
            else
                SendMsg(new MsgBase((ushort)OperationListEvent.Hide));
        });

        HistoryTog.onValueChanged.AddListener((isOn) =>
        {
            active = true;
            //isHierarchyTogOn = isOn;
            if (isOn)
            {
                CloseExclusiveModule();
                if(!GlobalInfo.IsExamMode())
                    OperationListTog.isOn = false;
                SendMsg(new MsgBool((ushort)HistoryEvent.Open, true));
            }
            else
                SendMsg(new MsgBase((ushort)HistoryEvent.Hide));
        });

        KnowledgeTog.onValueChanged.AddListener((isOn) =>
        {
            active = true;
            if (isOn)
                SendMsg(new MsgBase((ushort)KnowledgeModuleEvent.Show));
            else
                SendMsg(new MsgBase((ushort)KnowledgeModuleEvent.Hide));
        });
    }

    /// <summary>
    /// 百科切换按钮悬浮动效
    /// </summary>
    private void SwitchBaikeAnim()
    {
        Transform prevArrow = Prev.FindChildByName("Image");
        Transform nextArrow = Next.FindChildByName("Image");
        EventTrigger prevEventTrigger = Prev.AutoComponent<EventTrigger>();
        prevEventTrigger.AddEvent(EventTriggerType.PointerEnter, (arg) =>
        {
            if (!Prev.interactable)
                return;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(prevArrow.DOLocalMoveY(3, 0.3f));
            sequence.Append(prevArrow.DOLocalMoveY(0, 0.3f));
            sequence.SetId("prevArrow");
        });
        prevEventTrigger.AddEvent(EventTriggerType.PointerExit, (arg) =>
        {
            DOTween.Kill("prevArrow", true);
        });
        EventTrigger nextEventTrigger = Next.AutoComponent<EventTrigger>();
        nextEventTrigger.AddEvent(EventTriggerType.PointerEnter, (arg) =>
        {
            if (!Next.interactable)
                return;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(nextArrow.DOLocalMoveY(3, 0.3f));
            sequence.Append(nextArrow.DOLocalMoveY(0, 0.3f));
            sequence.SetId("nextArrow");
        });
        nextEventTrigger.AddEvent(EventTriggerType.PointerExit, (arg) =>
        {
            DOTween.Kill("nextArrow", true);
        });
    }

    /// <summary>
    /// 关闭互斥面板
    /// 打开模型层级、动画列表、操作列表时收起百科列表
    /// 移动端还要收起知识点列表
    /// </summary>
    private void CloseExclusiveModule()
    {
        ShowBaike.isOn = false;
#if UNITY_ANDROID || UNITY_IOS
        KnowledgeTog.isOn = false;
#endif
    }

    public void ShowBaikeSelectModule(bool show, bool withNotify = true)
    {
        if (withNotify)
        {
            ShowBaike.isOn = show;
        }
        else
        {
            ShowBaike.SetIsOnWithoutNotify(show);
        }
    }

    public void SetBaikePage()
    {
#if UNITY_ANDROID || UNITY_IOS
        BaikeTotalPage.text = $"{GlobalInfo.currentWikiList.Count}";
#else
        BaikePage.text = $"1/{GlobalInfo.currentWikiList.Count}";
#endif
    }

    public void SetBaikeIndex(int baikeIndex)
    {
#if UNITY_ANDROID || UNITY_IOS
        BaikeCurrentPage.text = $"{baikeIndex + 1}";
#else
        BaikePage.text = $"{baikeIndex + 1}/{GlobalInfo.currentWikiList.Count}";
#endif
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            #region 百科列表模块
            case (ushort)BaikeSelectModuleEvent.Hide:
                ShowBaike.SetIsOnWithoutNotify(false);
                UIManager.Instance.HideModuleUI<BaikeSelectModule>(ParentPanel);
#if UNITY_ANDROID || UNITY_IOS
                ParentPanel.EmptyClick.gameObject.SetActive(false);
#endif
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.LeftFlex, ShowBaike.isOn || OperationListTog.isOn));
                break;
            #endregion

            #region 知识点模块
            case (ushort)KnowledgeModuleEvent.Show:
#if UNITY_ANDROID || UNITY_IOS
                if(HierarchyTog)
                    HierarchyTog.isOn = false;
                if(AnimListTog)
                    AnimListTog.isOn = false;
                OperationListTog.isOn = false;
                HistoryTog.isOn = false;
#endif
                UIManager.Instance.OpenModuleUI<KnowledgeModule>(ParentPanel, ParentPanel.KnowledgeModulePoint);
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.RightFlex, true));
                break;
            case (ushort)KnowledgeModuleEvent.Hide:
                KnowledgeTog.SetIsOnWithoutNotify(false);
                UIManager.Instance.HideModuleUI<KnowledgeModule>(ParentPanel);
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.RightFlex, false));
                break;
            #endregion

            #region 模型层级模块
            case (ushort)CoursePanelEvent.HierarchyBtn:
                HierarchyTog.gameObject.SetActive(true);
                KnowledgeTog.gameObject.SetActive(true);
#if UNITY_ANDROID || UNITY_IOS
                //SplitLine.gameObject.SetActive(true);
                if (!EnableVerticalLayout)
                {
                    HierarchyTog.interactable = true;
                    HierarchyTog.image.raycastTarget = true;
                    KnowledgeTog.interactable = true;
                    KnowledgeTog.image.raycastTarget = true;
                    if (AnimListTog)
                        AnimListTog.gameObject.SetActive(false);
                    OperationListTog.gameObject.SetActive(false);
                }
                HierarchyTog.isOn = isHierarchyTogOn;
#else
                HierarchyTog.isOn = true;         
#endif
                KnowledgeTog.isOn = isKnowledgeTogOn;
                break;
            case (ushort)HierarchyEvent.Hide:
                HierarchyTog.SetIsOnWithoutNotify(false);
                UIManager.Instance.HideModuleUI<ModelHierarchyModule>(ParentPanel);
                break;
            #endregion

            #region 动画列表模块
            case (ushort)CoursePanelEvent.AnimListBtn:
                AnimListTog.gameObject.SetActive(true); 
                KnowledgeTog.gameObject.SetActive(true);
#if UNITY_ANDROID || UNITY_IOS
                //SplitLine.gameObject.SetActive(true);
                if (!EnableVerticalLayout)
                {
                    if (AnimListTog)
                    {
                        AnimListTog.interactable = true;
                        AnimListTog.image.raycastTarget = true;
                    }
                    KnowledgeTog.interactable = true;
                    KnowledgeTog.image.raycastTarget = true;
                    if (HierarchyTog)
                        HierarchyTog.gameObject.SetActive(false);
                    OperationListTog.gameObject.SetActive(false);
                }
                AnimListTog.isOn = isHierarchyTogOn;
#else
                AnimListTog.isOn = true;
#endif
                KnowledgeTog.isOn = isKnowledgeTogOn;
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.RightFlex, isKnowledgeTogOn));
                break;
            case (ushort)AdaptiveListEvent.Hide:
                AnimListTog.SetIsOnWithoutNotify(false);
                UIManager.Instance.HideModuleUI<AdaptiveListModule>(ParentPanel);
                break;
            #endregion

            #region 操作列表模块
            case (ushort)CoursePanelEvent.OperationListBtn:
                OperationListTog.gameObject.SetActive(true);//!GlobalInfo.IsExamMode()
                HistoryTog.gameObject.SetActive(true);
                KnowledgeTog.gameObject.SetActive(false);// !GlobalInfo.IsExamMode()
#if UNITY_ANDROID || UNITY_IOS
                //SplitLine.gameObject.SetActive(true);
                if (!EnableVerticalLayout)
                {
                    OperationListTog.interactable = true;
                    OperationListTog.image.raycastTarget = true;
                    KnowledgeTog.interactable = true;
                    KnowledgeTog.image.raycastTarget = true;
                    if (AnimListTog)
                        AnimListTog.gameObject.SetActive(false);
                    if (HierarchyTog)
                        HierarchyTog.gameObject.SetActive(false);
                }
                OperationListTog.SetIsOnWithoutNotify(isHierarchyTogOn);
                //确保移动端初始化UISmallSceneFlowModule模块
                SendMsg(new MsgBool((ushort)OperationListEvent.Open, isHierarchyTogOn));
#else
                OperationListTog.isOn = true;
#endif
                if (GlobalInfo.IsExamMode())
                {  
                    //默认打开操作记录列表
                    HistoryTog.isOn = true;
                }
                else
                {
                    SendMsg(new MsgBool((ushort)HistoryEvent.Open, false));
                }
                KnowledgeTog.isOn = isKnowledgeTogOn;
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.RightFlex, isKnowledgeTogOn));
                break;
            case (ushort)OperationListEvent.Hide:
                OperationListTog.SetIsOnWithoutNotify(false);
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.LeftFlex, ShowBaike.isOn || OperationListTog.isOn || HistoryTog.isOn));
                break;
            case (ushort)HistoryEvent.Hide:
                HistoryTog.SetIsOnWithoutNotify(false);
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.LeftFlex, ShowBaike.isOn || OperationListTog.isOn || HistoryTog.isOn));
                break;
            #endregion
        }
    }

     /// <summary>
     /// 切换百科事件
     /// </summary>
     /// <param name="closeKnowledge"></param>
    public void OnBaikeChanged(bool closeKnowledge = true)
    {
        Prev.interactable = BaikeSelectModule.CurrentBaikeIndex > 0;
        Next.interactable = BaikeSelectModule.CurrentBaikeIndex < GlobalInfo.currentWikiList.Count - 1;
#if UNITY_ANDROID || UNITY_IOS
        BaikeCurrentPage.text = $"{BaikeSelectModule.CurrentBaikeIndex + 1}";
        BaikeTotalPage.text = $"{GlobalInfo.currentWikiList.Count}";
#else
        BaikePage.text = $"{BaikeSelectModule.CurrentBaikeIndex + 1}/{ GlobalInfo.currentWikiList.Count}";
#endif

        if (HierarchyTog)
        {
            HierarchyTog.SetIsOnWithoutNotify(false);
            //HierarchyTog.onValueChanged.RemoveAllListeners();
        }
        if (AnimListTog)
        {
            AnimListTog.SetIsOnWithoutNotify(false);
            //AnimListTog.onValueChanged.RemoveAllListeners();
        }
        OperationListTog.SetIsOnWithoutNotify(false);
        HistoryTog.SetIsOnWithoutNotify(false);
        //OperationListTog.onValueChanged.RemoveAllListeners();
        ShowBaike.SetIsOnWithoutNotify(false);

        UIManager.Instance.CloseModuleUI<ModelHierarchyModule>(ParentPanel);
        UIManager.Instance.CloseModuleUI<BaikeSelectModule>(ParentPanel);
        UIManager.Instance.CloseModuleUI<AdaptiveListModule>(ParentPanel);

        if (closeKnowledge)
        {
            isKnowledgeTogOn = KnowledgeTog.isOn;
            KnowledgeTog.SetIsOnWithoutNotify(false);
            //KnowledgeTog.onValueChanged.RemoveAllListeners();
            UIManager.Instance.CloseModuleUI<KnowledgeModule>(ParentPanel);
        }

#if UNITY_ANDROID || UNITY_IOS
        if (EnableVerticalLayout)
        {
            if (HierarchyTog)
                HierarchyTog.gameObject.SetActive(false);
            if (AnimListTog)
                AnimListTog.gameObject.SetActive(false);
            OperationListTog.gameObject.SetActive(false);
            HistoryTog.gameObject.SetActive(false);
            KnowledgeTog.gameObject.SetActive(false);
            //SplitLine.gameObject.SetActive(false);
        }
        else//移动端默认禁用，根据百科类型修改可交互性
        {
            if (HierarchyTog && HierarchyTog.gameObject.activeInHierarchy)
            {
                HierarchyTog.interactable = false;
                HierarchyTog.image.raycastTarget = false;
            }
            if (AnimListTog && AnimListTog.gameObject.activeInHierarchy)
            {
                AnimListTog.interactable = false;
                AnimListTog.image.raycastTarget = false;
            }
            if (OperationListTog.gameObject.activeInHierarchy)
            {
                OperationListTog.interactable = false;
                OperationListTog.image.raycastTarget = false;
            }
            KnowledgeTog.interactable = false;
            KnowledgeTog.image.raycastTarget = false;
            HistoryTog.gameObject.SetActive(false);
        }    
#else
        //PC端默认隐藏，根据百科类型控制显隐
        if(HierarchyTog)
            HierarchyTog.gameObject.SetActive(false);
        if(AnimListTog)
            AnimListTog.gameObject.SetActive(false);
        OperationListTog.gameObject.SetActive(false);
        HistoryTog.gameObject.SetActive(false);
        KnowledgeTog.gameObject.SetActive(false);
#endif
    }

    #region  移动端侧边栏睡眠状态
    private float activeInterval = 10f;
    private bool active;
    private float time;

    private void Update()
    {

#if UNITY_ANDROID || UNITY_IOS
        if (active)
        {
            active = false;
            time = 0;
            if (SideBarCanvas.alpha < 1 && !GlobalInfo.isARTracking)
            {
                SideBarCanvas.DOFade(1, 0.3f);
            }
        }
        else
        {
            time += Time.deltaTime;
            if (SideBarCanvas.alpha == 1 && time > activeInterval)
            {
                SideBarCanvas.DOFade(0.5f, 1.5f);
            }
        }
#endif
    }

    public void Fade(bool fade)
    {
        SideBarCanvas.blocksRaycasts = !fade;
        SideBarCanvas.DOFade(fade ? 0 : 1, 0.3f);
    }

    public void ActiveCanvas()
    {
        active = true;
    }

    /// <summary>
    /// 还原等待考核状态
    /// </summary>
    public void Clear()
    {
        Prev.interactable = false;
        Next.interactable = false;
#if UNITY_ANDROID || UNITY_IOS
        BaikeCurrentPage.text = $"{0}";
        BaikeTotalPage.text = $"{0}";
#else
        BaikePage.text = $"0/0";
#endif
        HistoryTog.gameObject.SetActive(false);
    }
    #endregion

}