using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;


/// <summary>
/// 设置
/// </summary>
public class OptionPanel : UIPanelBase
{
    private Transform ModuleNode;

    private CanvasGroup CanvasGroup;
    private RectTransform Panel;

    #region 画面居中全局弱提示
    private GameObject Toast;
    private CanvasGroup ToastBackground;
    private GameObject True;
    private GameObject False;
    private Text InfoText;
    #endregion

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        ModuleNode = this.FindChildByName("ModuleNode");
        Panel = this.GetComponentByChildName<RectTransform>("Panel");
        CanvasGroup = Panel.GetComponent<CanvasGroup>();

        Toast = this.FindChildByName("Toast").gameObject;
        ToastBackground = Toast.GetComponentInChildren<CanvasGroup>(true);
        True = this.FindChildByName("True").gameObject;
        False = this.FindChildByName("False").gameObject;
        InfoText = this.GetComponentByChildName<Text>("Info");

       var UserInfo = this.GetComponentByChildName<Toggle>("UserInfo");
        {
            UserInfo.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    UIManager.Instance.OpenModuleUI<Option_UserInfoModule>(this, ModuleNode);
                else
                    UIManager.Instance.CloseModuleUI<Option_UserInfoModule>(this);
            });

            UserInfo.isOn = true;
            UserInfo.group.allowSwitchOff = false;
        }

        var General = this.GetComponentByChildName<Toggle>("General");
        {
            General.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    UIManager.Instance.OpenModuleUI<Option_GeneralModule>(this, ModuleNode);
                else
                    UIManager.Instance.CloseModuleUI<Option_GeneralModule>(this);
            });
        }

        var Help = this.GetComponentByChildName<Toggle>("Help");
        {
            Help.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    UIManager.Instance.OpenModuleUI<Option_HelpModule>(this, ModuleNode);
                else
                    UIManager.Instance.CloseModuleUI<Option_HelpModule>(this);
            });
        }

        var Hotkey = this.GetComponentByChildName<Toggle>("Hotkey");
        {
            Hotkey.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    UIManager.Instance.OpenModuleUI<Option_HotKeyModule>(this, ModuleNode);
                else
                    UIManager.Instance.CloseModuleUI<Option_HotKeyModule>(this);
            });
        }

        var About = this.GetComponentByChildName<Toggle>("About");
        {
            About.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    UIManager.Instance.OpenModuleUI<Option_AboutModule>(this, ModuleNode);
                else
                    UIManager.Instance.CloseModuleUI<Option_AboutModule>(this);
            });
        }

        this.GetComponentByChildName<Button>("Minimize").onClick.AddListener(() =>
        {
            WindowCtrl.MinimizeWindow();
            UIManager.Instance.CloseUI<OptionPanel>();
        });

        this.GetComponentByChildName<Button>("Logout").onClick.AddListener(() =>
        {
            SendMsg(new MsgBase((ushort)OptionPanelEvent.Logout));
            //Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            //popupDic.Add("取消", new PopupButtonData(null));
            //popupDic.Add("确定", new PopupButtonData(Logout, true));
            //UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("登出提示", "确定要退出登录吗?", popupDic));
        });

        this.GetComponentByChildName<Button>("Quit").onClick.AddListener(() =>
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("取消", new PopupButtonData(null));
                popupDic.Add("确定", new PopupButtonData(UnityEngine.Application.Quit, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定关闭软件?", popupDic));
            }
        });

        this.GetComponentByChildName<Button>("Close").onClick.AddListener(() => UIManager.Instance.CloseUI<OptionPanel>());

        SendMsg(new MsgBool((ushort)CoursePanelEvent.Option, true));

        GlobalInfo.ShowPopup = true;
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        if (!GlobalInfo.MultiplePopup)
            Cursor.lockState = GlobalInfo.CursorLockMode;
        UIManager.Instance.CloseAllModuleUI<ToastPanel>(this);
        SendMsg(new MsgBool((ushort)CoursePanelEvent.Option, false));
        base.Close(uiData, callback);
        GlobalInfo.ShowPopup = false;
    }

    public void ShowToast(bool result, string info)
    {
        DOTween.Kill("ToastFade");
        True.SetActive(result);
        False.SetActive(!result);
        InfoText.text = info;
        ToastBackground.alpha = 1f;
        Toast.SetActive(true);
        DOTween.To(() => ToastBackground.alpha, value => ToastBackground.alpha = value, 0, 1.5f).SetDelay(1f).SetId("ToastFade").onComplete = () =>
        {
            Toast.SetActive(false);
        };
    }
    #region 动效
    protected override float exitAnimePlayTime => 0.1f;

    public override void JoinAnim(UnityAction callback)
    {
        SoundManager.Instance.PlayEffect("Popup");
        JoinSequence.Join(DOTween.To(() => 0.2f * Vector3.one, (value) => Panel.transform.localScale = value, Vector3.one, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => 0f, (value) => CanvasGroup.alpha = value, 1f, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(DOTween.To(() => Vector3.one, (value) => Panel.transform.localScale = value, 0.8f * Vector3.one, ExitAnimePlayTime));
        ExitSequence.Join(DOTween.To(() => 1f, (value) => CanvasGroup.alpha = value, 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion
}