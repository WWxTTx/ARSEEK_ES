using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using WebSocketSharp;

/// <summary>
/// 动画百科列表
/// 可扩展支持类型
/// </summary>
public class AdaptiveListModule : UIModuleBase
{
    private CanvasGroup canvasGroup;

    private RectTransform Background;
    private UnityAction closeEvent;

    private ScrollRect scrollRect;
    private VerticalLayoutGroup layout;

    private AdaptiveListData listData;

    private List<AdaptiveListItem> datas;
    private Dictionary<string, Toggle> itemToggles;

    private Dictionary<string, Button> itemButtons;

    private Toggle firstTog;
    public static string SelectID;

    private GameObject ListItem;

    /// <summary>
    /// 记录点击时间
    /// </summary>
    private float timer;
    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        canvasGroup = this.GetComponent<CanvasGroup>();
        Background = this.GetComponentByChildName<RectTransform>("BackGround");
        scrollRect = GetComponentInChildren<ScrollRect>(true);
        layout = scrollRect.content.GetComponent<VerticalLayoutGroup>();

        ListItem = scrollRect.FindChildByName("ListItem1").gameObject;

        listData = uiData as AdaptiveListData;
        if (listData != null)
        {
            AddMsg(new ushort[]
            {
#if UNITY_ANDROID || UNITY_IOS
                (ushort)ARModuleEvent.Tracking,
#endif
                (ushort)AdaptiveListEvent.Select,
                (ushort)AdaptiveListEvent.SelectWithoutNotify,
                (ushort)HierarchyEvent.UpdateAttachment
            });

            string title = "动画列表";
            datas = new List<AdaptiveListItem>();

            switch (listData.Type)
            {
                case AdaptiveType.Anim:
                    ModelInfo[] modelInfos = ModelManager.Instance.modelGo.GetComponentsInChildren<ModelInfo>();

                    int i = 0;
                    foreach(ModelInfo modelInfo in modelInfos)
                    {
                        if(modelInfo.PropType == PropType.Animation)
                        {
                            //显示名字的是先判断后端传来的数据是否有名字，在判断本地数据
                            string name = "";
                            if (GlobalInfo.currentWiki is EncyclopediaModel && (GlobalInfo.currentWiki as EncyclopediaModel).modelNodes.Count != 0)
                            {
                                name = (GlobalInfo.currentWiki as EncyclopediaModel).modelNodes[i].nodeName;
                            }

                            datas.Add(new AdaptiveListItem() { 
                                ID = modelInfo.ID/*modelInfo.transform.name*/, 
                                Title = string.IsNullOrEmpty(name)?(string.IsNullOrEmpty(modelInfo.Name) ? "演示动画" : modelInfo.Name): name 
                            });
                            i++;
                        }
                    }
                    break;                
            }
            this.GetComponentByChildName<Text>("Title").text = title;

            itemToggles = new Dictionary<string, Toggle>();
            itemButtons = new Dictionary<string, Button>();


#if UNITY_ANDROID || UNITY_IOS
 
            scrollRect.content.RefreshItemsView(ListItem, datas, (item, info) =>
            {
                switch (listData.Type)
                {
                    case AdaptiveType.Anim:
                        item.GetComponentInChildren<Text>().text = $"{item.GetSiblingIndex() + 1}.{info.Title}";
                        break;
                }

                Toggle tog = item.GetComponent<Toggle>();
                tog.onValueChanged.RemoveAllListeners();
                tog.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        if (!NetworkManager.Instance.IsIMSyncState)
                        {
                            ToolManager.SendBroadcastMsg(new MsgString((ushort)AdaptiveListEvent.Select, info.ID), true);
                        }
                    }
                });
                tog.interactable = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(item.GetComponent<RectTransform>());

                if (firstTog == null)
                {
                    if (string.IsNullOrEmpty(SelectID))
                        firstTog = tog;
                    else if (info.ID.Equals(SelectID))
                        firstTog = tog;                      
                }

                UIFade uiFade = item.AutoComponent<UIFade>();
                uiFade.Init(item.GetComponent<Image>(), 0.08f, 0f);
                itemToggles.Add(info.ID, tog);
            });
#else

            bool isfirst = true;
            Button firstButton = null;

            scrollRect.content.RefreshItemsView(ListItem, datas, (item, info) =>
            {
                switch (listData.Type)
                {
                    case AdaptiveType.Anim:
                        item.GetComponentInChildren<InputField>().text = $"{item.GetSiblingIndex() + 1}.{(info.Title.IsNullOrEmpty() ? "演示动画" : info.Title)}";
                        break;
                }

                Button button = item.GetComponent<Button>();
                button.onClick.RemoveAllListeners();

                //双击编辑
                InputField opInputField = item.GetComponentByChildName<InputField>("InputField");
                CanvasGroup opCanvasGroup = opInputField.GetComponent<CanvasGroup>();
                opCanvasGroup.interactable = false;
                opCanvasGroup.blocksRaycasts = false;

                button.onClick.AddListener(() =>
                {
                    if (!NetworkManager.Instance.IsIMSyncState)
                    {
                        ToolManager.SendBroadcastMsg(new MsgString((ushort)AdaptiveListEvent.Select, info.ID), true);
                    }

                    if (Time.time - timer < 0.4f)//双击且可编辑
                    {
                        opCanvasGroup.interactable = true;
                        opCanvasGroup.blocksRaycasts = true;
                        opInputField.transform.parent.GetComponent<Image>().color = new Vector4(1, 1, 1, 1);
                        if (opInputField.text.Length > 4 && opInputField.text.Substring(opInputField.text.Length - 4, 4) == "演示动画") 
                        {
                            opInputField.text = "";
                        }
                        opInputField.Select();
                    }

                    timer = Time.time;
                });

                UpdateAttachment(button, !string.IsNullOrEmpty(info.ID) && GlobalInfo.currentWikiKnowledges.ContainsKey(info.ID));

                LayoutRebuilder.ForceRebuildLayoutImmediate(item.GetComponent<RectTransform>());

                itemButtons.Add(info.ID, button);

                //编辑完成后，发送数据到后端
                opInputField.onEndEdit.AddListener((string str) =>
                {
                    opCanvasGroup.interactable = false;
                    opCanvasGroup.blocksRaycasts = false;

                    opInputField.transform.parent.GetComponent<Image>().color = new Vector4(1, 1, 1, 1f / 255f);

                    //数据发送后端（发送）

                    string UUID = info.ID;

                    //更新显示的节点的文字
                    string textStr = (item.GetSiblingIndex() + 1).ToString() + ".";
                    if (str.Length >= textStr.Length && str.Substring(0, textStr.Length) == textStr) 
                    {
                        Debug.Log(str);
                        str = str.Substring(textStr.Length, str.Length - textStr.Length);
                    }


                    item.GetComponentInChildren<InputField>().text = $"{textStr}{(str == "" ? "演示动画" : str)}";

                    //发送修改节点数据
                    RequestManager.Instance.ChangeModelNodeName(GlobalInfo.currentWiki.id, UUID, str, () =>
                    {
                        if (!GlobalInfo.currentWikiNames.ContainsKey(UUID))
                            GlobalInfo.currentWikiNames.Add(UUID, str);
                        else
                            GlobalInfo.currentWikiNames[UUID] = str;

                        LayoutRebuilder.ForceRebuildLayoutImmediate(opInputField.GetComponent<RectTransform>());
                        LayoutRebuilder.ForceRebuildLayoutImmediate(item.GetComponent<RectTransform>());
                        //callback?.Invoke(true);
                    }, (code, msg) =>
                    {
                        UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("修改模型节点名称失败"));
                        //callback?.Invoke(false);
                    });
                });

                if (isfirst) 
                {
                    isfirst = false;
                    firstButton = button;
                }
            });
            if (firstButton != null)
            {
                firstButton.onClick.Invoke();
            }
#endif

            closeEvent = listData.OnModuleClose;

            Button Collapse = this.GetComponentByChildName<Button>("Collapse");
            Collapse.onClick.AddListener(() => SendMsg(new MsgBase((ushort)AdaptiveListEvent.Hide)));
        }

        RefreshLayouGroup();
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
            case (ushort)AdaptiveListEvent.Select:
#if UNITY_ANDROID || UNITY_IOS
                string selectedId = ((MsgBrodcastOperate)msg).GetData<MsgString>().arg;
                int selectedIndex = datas.FindIndex(i => i.ID.Equals(selectedId));
                Select(selectedIndex);
                bool select;
                foreach (KeyValuePair<string, Toggle> itemTog in itemToggles)
                {
                    select = itemTog.Key.Equals(selectedId);
                    itemTog.Value.SetIsOnWithoutNotify(select);
                }
                listData.OnItemSelect?.Invoke(selectedId);
#else
                string selectedId = ((MsgBrodcastOperate)msg).GetData<MsgString>().arg;
                int selectedIndex = datas.FindIndex(i => i.ID.Equals(selectedId));
                Select(selectedIndex);

                foreach (KeyValuePair<string, Button> itemButton in itemButtons)
                {
                    if (itemButton.Key.Equals(selectedId))
                    {
                        itemButton.Value.GetComponent<Image>().color = new Vector4(1, 1, 1, 64 / 255f);
                    }
                    else 
                    {
                        itemButton.Value.GetComponent<Image>().color = new Vector4(1, 1, 1, 1 / 255f);
                    }
                }

                listData.OnItemSelect?.Invoke(selectedId);
#endif
                break;
            case (ushort)AdaptiveListEvent.SelectWithoutNotify:
#if UNITY_ANDROID || UNITY_IOS
                string id = ((MsgString)msg).arg;
                int index = datas.FindIndex(i => i.ID.Equals(id));
                Select(index);
                foreach (KeyValuePair<string, Toggle> itemTog in itemToggles)
                {
                    itemTog.Value.SetIsOnWithoutNotify(itemTog.Key.Equals(id));
                }
#else
                string id = ((MsgString)msg).arg;
                int index = datas.FindIndex(i => i.ID.Equals(id));
                Select(index);
#endif
                break;
            case (ushort)HierarchyEvent.UpdateAttachment:
                MsgStringInt msgStringInt = ((MsgStringInt)msg);
                if (itemButtons.TryGetValue(msgStringInt.arg1, out Button button))
                {
                    UpdateAttachment(button, msgStringInt.arg2 > 0);
                }
                break;
        }
    }

    private void UpdateAttachment(Button button, bool attachment)
    {
        Image attach = button.GetComponentByChildName<Image>("Attach");
        attach.color = attachment ? Color.white : Color.black;
        attach.SetAlpha(attachment ? 1 : 0.3f);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        SelectID = string.Empty;
        closeEvent?.Invoke();
        base.Close(uiData, callback);
    }

    /// <summary>
    /// 自动定位
    /// </summary>
    /// <param name="index"></param>

    public void Select(int index)
    {
        var remainingHeight = scrollRect.content.rect.height - scrollRect.viewport.rect.height - layout.padding.top;

        if (index > 0)
        {
            foreach (RectTransform child in scrollRect.content)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    remainingHeight -= (child.rect.height + layout.spacing);

                    if (--index <= 0 || remainingHeight <= 0)
                    {
                        break;
                    }
                }
            }
        }

        if (index > 0 || remainingHeight <= 0)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
        else
        {
            scrollRect.verticalNormalizedPosition = remainingHeight / (scrollRect.content.rect.height - scrollRect.viewport.rect.height);
        }
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public override void JoinAnim(UnityAction callback)
    {
#if UNITY_ANDROID || UNITY_IOS
        JoinSequence.Join(Background.DOAnchorPos3DX(-Background.sizeDelta.x, JoinAnimePlayTime));
#else
        JoinSequence.Join(Background.DOAnchorPos3DX(44f, JoinAnimePlayTime));
#endif
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
#if UNITY_ANDROID || UNITY_IOS
        ExitSequence.Join(Background.DOAnchorPos3DX(0f, ExitAnimePlayTime));
#else
        ExitSequence.Join(Background.DOAnchorPos3DX(-Background.sizeDelta.x, ExitAnimePlayTime));
#endif
        base.ExitAnim(callback);
    }
    #endregion


    public enum AdaptiveType
    {
        /// <summary>
        /// 动画列表
        /// </summary>
        Anim
    }

    public class AdaptiveListItem
    {
        public string ID;
        public string Title;
        public int Score;
    }

    /// <summary>
    /// 可变列表模块数据
    /// </summary>

    public class AdaptiveListData : UIData
    {
        /// <summary>
        /// 列表类型
        /// </summary>
        public AdaptiveType Type;
        /// <summary>
        /// 列表项选中事件
        /// </summary>
        public UnityAction<string> OnItemSelect { get; set; }

        public UnityAction OnModuleClose { get; set; }

        public AdaptiveListData(AdaptiveType type, UnityAction<string> onItemSelect, UnityAction onModuleClose = null)
        {
            Type = type;
            OnItemSelect = onItemSelect;
            OnModuleClose = onModuleClose;
        }
    }
}