using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class ExamInfoConfirmPanel : UIPanelBase
{
    public override bool canOpenOption => false;

    //private Toggle MaskToggle;
    private UIPanelBase ParentPanel;

    private InputField changeName;

    private InputField changeUserNo;
    private Text MaskUserNo;

    private InputField changeUserOrg;
    private Text MaskUserOrg;

    //private Button closeBtn;
    private Button confirmBtn;

    CanvasGroup BackGround;
    CanvasGroup Content;

    public class ConfirmData : UIData
    {
        public UnityAction onConfirmed;
    }

    public override void Open(UIData uiData = null)
    {
        AddMsg((ushort)CoursePanelEvent.Option);

        base.Open(uiData);

        BackGround = transform.GetComponentByChildName<CanvasGroup>("BackGround");
        Content = transform.GetComponentByChildName<CanvasGroup>("Content");
        BackGround.alpha = 0;
        Content.alpha = 0;

        #region 修改真实姓名
        changeName = this.GetComponentByChildName<InputField>("ChangeNameInputField");
        changeName.text = GlobalInfo.account.nickname;//设置初始值
        changeName.interactable = false;//设置初始不能编辑
        Button nameEditor = changeName.GetComponentByChildName<Button>("Editor");
        nameEditor.gameObject.SetActive(true);//设置初始显示编辑按钮
        nameEditor.onClick.AddListener(() =>
        {
            //编辑按钮点击事件
            changeName.interactable = true;
            changeName.Select();
            nameEditor.gameObject.SetActive(false);
        });

        changeName.onValueChanged.AddListener(content =>
        {
            changeName.text = changeName.text.RemoveSpecialSymbols_Chinese();
        });
        changeName.onEndEdit.AddListener(content =>
        {   //编辑完成事件
            changeName.interactable = false;
            nameEditor.gameObject.SetActive(true);

            if (content == GlobalInfo.account.nickname)
                return;

            if (content.Length < 2)
            {
                changeName.text = GlobalInfo.account.nickname;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("请输入2-4个中文字符!"));
                return;
            }

            RequestManager.Instance.ChangeNickName(content, () =>
            {
                Relogin(() =>
                {
                    GlobalInfo.account.nickname = content;
                    changeName.text = content;
                    SendMsg(new MsgBase((ushort)OptionPanelEvent.Name));
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("修改成功!"));
                }, null);//重新登录  
            }, (code, errorMessage) =>
            {
                changeName.text = GlobalInfo.account.nickname;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(errorMessage));
            });
        });
        #endregion

        #region 修改工号
        changeUserNo = this.GetComponentByChildName<InputField>("ChangeUserNoInputField");
        changeUserNo.text = GlobalInfo.account.userNo;
        changeUserNo.textComponent.gameObject.SetActive(false);
        changeUserNo.placeholder.gameObject.SetActive(false);
        changeUserNo.interactable = false;

        MaskUserNo = changeUserNo.GetComponentByChildName<Text>("MaskUserNo");
        MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
        MaskUserNo.gameObject.SetActive(true);

        Button userNoEditor = changeUserNo.GetComponentByChildName<Button>("" +
            "Editor");
        userNoEditor.gameObject.SetActive(true);
        userNoEditor.onClick.AddListener(() =>
        {
            //编辑按钮点击事件
            changeUserNo.textComponent.gameObject.SetActive(true);
            changeUserNo.placeholder.gameObject.SetActive(true);
            changeUserNo.interactable = true;
            changeUserNo.Select();
            MoveLast(changeUserNo).Forget();
            userNoEditor.gameObject.SetActive(false);
            MaskUserNo.gameObject.SetActive(false);      
        });

        changeUserNo.onEndEdit.AddListener(content =>
        {
            MaskUserNo.gameObject.SetActive(true);
            userNoEditor.gameObject.SetActive(true);
            changeUserNo.textComponent.gameObject.SetActive(false);
            changeUserNo.placeholder.gameObject.SetActive(false);
            changeUserNo.interactable = false;

            //if (content.Equals(GlobalInfo.account.userNo) ||
            //    (GlobalInfo.account.userNo == null && string.IsNullOrEmpty(content)))
            //    return;

            if (content.Equals(GlobalInfo.account.userNo))
                return;

            //不允许将工号改为空
            if (string.IsNullOrEmpty(content))
            {
                changeUserNo.text = GlobalInfo.account.userNo;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("请输入工号"));
                return;
            }

            RequestManager.Instance.ChangeUserNo(content, () =>
            {
                Relogin(() =>
                {
                    GlobalInfo.account.userNo = content;
                    changeUserNo.text = GlobalInfo.account.userNo;
                    MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("修改成功!"));
                }, null);//重新登录  
            }, (code, errorMessage) =>
            {
                changeUserNo.text = GlobalInfo.account.userNo;
                MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(errorMessage));
            });
        });
        #endregion

        #region 修改单位
        changeUserOrg = this.GetComponentByChildName<InputField>("ChangeUserOrgInputField");
        changeUserOrg.text = GlobalInfo.account.userOrgName;
        changeUserOrg.textComponent.gameObject.SetActive(false);
        changeUserOrg.placeholder.gameObject.SetActive(false);
        changeUserOrg.interactable = false;

        MaskUserOrg = changeUserOrg.GetComponentByChildName<Text>("MaskUserOrg");
        MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
        MaskUserOrg.gameObject.SetActive(true);

        Button userOrgEditor = changeUserOrg.GetComponentByChildName<Button>("Editor");
        userOrgEditor.gameObject.SetActive(true);
        userOrgEditor.onClick.AddListener(() =>
        {
            changeUserOrg.textComponent.gameObject.SetActive(true);
            changeUserOrg.placeholder.gameObject.SetActive(true);
            changeUserOrg.interactable = true;
            changeUserOrg.Select();
            MoveLast(changeUserOrg).Forget();
            userOrgEditor.gameObject.SetActive(false);
            MaskUserOrg.gameObject.SetActive(false);
        });

        changeUserOrg.onEndEdit.AddListener(content =>
        {
            MaskUserOrg.gameObject.SetActive(true);
            userOrgEditor.gameObject.SetActive(true);
            changeUserOrg.textComponent.gameObject.SetActive(false);
            changeUserOrg.placeholder.gameObject.SetActive(false);
            changeUserOrg.interactable = false;

            if (content.Equals(GlobalInfo.account.userOrgName) ||
                (GlobalInfo.account.userOrgName == null && string.IsNullOrEmpty(content)))
                return;

            RequestManager.Instance.ChangeUserOrg(content, () =>
            {
                Relogin(() =>
                {
                    GlobalInfo.account.userOrgName = content;
                    changeUserOrg.text = GlobalInfo.account.userOrgName;
                    MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
                    SendMsg(new MsgBase((ushort)OptionPanelEvent.Org));
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("修改成功!"));
                }, null);//重新登录  
            }, (code, errorMessage) =>
            {
                changeUserOrg.text = GlobalInfo.account.userOrgName;
                MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(errorMessage));
            });
        });
        #endregion

        //旧版是退出房间时选择是否解散
        //closeBtn = transform.GetComponentByChildName<Button>("CloseBtn");
        //closeBtn.onClick.AddListener(() =>
        //{
        //    UIManager.Instance.CloseUI<ExamInfoConfirmPanel>();
        //});

        confirmBtn = transform.GetComponentByChildName<Button>("ConfirmBtn");
        confirmBtn.onClick.AddListener(() =>
        {
            if(string.IsNullOrEmpty(changeUserNo.text))
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, UILevel.PopUp, new LocalTipModule.ModuleData("请输入工号", 120));
                return;
            }

            UIManager.Instance.CloseUI<ExamInfoConfirmPanel>();
            if (uiData != null)
                (uiData as ConfirmData)?.onConfirmed?.Invoke();
        });
    }

    private async UniTaskVoid MoveLast(InputField inputField)
    {
        await UniTask.WaitForEndOfFrame(this);
        inputField.MoveTextEnd(true);
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)CoursePanelEvent.Option:
                if (!(msg as MsgBool).arg1)
                {
                    if (!changeName.interactable)
                    {
                        changeName.text = GlobalInfo.account.nickname;
                    }
                    if (!changeUserNo.interactable)
                    {
                        changeUserNo.text = GlobalInfo.account.userNo;
                        MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
                    }
                    if (!changeUserOrg.interactable)
                    {
                        changeUserOrg.text = GlobalInfo.account.userOrgName;
                        MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
                    }
                }
                break;
        }
    }

    #region 动效
    public override void JoinAnim(UnityAction callback)
    {
        SoundManager.Instance.PlayEffect("Popup");
        BackGround.transform.localScale = Vector3.one * 0.001f;
        JoinSequence.Append(BackGround.transform.DOScale(Vector3.one, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => BackGround.alpha, (value) => BackGround.alpha = value, 1f, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => Content.alpha, (value) => Content.alpha = value, 1f, JoinAnimePlayTime));
        JoinSequence.AppendCallback(() => Content.blocksRaycasts = true);
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        Content.blocksRaycasts = false;
        Content.alpha = 0f;
        BackGround.transform.localScale = Vector3.one;
        ExitSequence.Append(BackGround.transform.DOScale(Vector3.one * 0.001f, ExitAnimePlayTime));
        ExitSequence.Join(DOTween.To(() => BackGround.alpha, (value) => BackGround.alpha = value, 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion

    private static void Relogin(UnityAction success, UnityAction failure)
    {
        //后台重新登录
        string account = PlayerPrefs.GetString(GlobalInfo.accountCacheKey);
        string password = PlayerPrefs.GetString(GlobalInfo.passwordCacheKey);
        ApiData.AccessToken = null;
        RequestManager.Instance.Login(account, password, (data, message) =>
        {
            ApiData.AccessToken = data.token.accessToken;
            GlobalInfo.account = data;

            //记录服务器返回数据到本地
            var tempSave = RequestBase.GetNewUrl(ApiData.Login);
            string value = ConfigXML.GetData(ConfigType.Cache, DtataType.LocalSever, tempSave);
            if (string.IsNullOrEmpty(value))
                ConfigXML.AddData(ConfigType.Cache, DtataType.LocalSever, tempSave, message);
            else
                ConfigXML.UpdateData(ConfigType.Cache, DtataType.LocalSever, tempSave, message);

            success.Invoke();
        }, (code, errorMessage) =>
        {
            failure?.Invoke();
        });
    }
}