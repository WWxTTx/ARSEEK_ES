using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;

public class LoginPanel : UIPanelBase
{
    private Transform ModulePoint;
    private ThemeRawImage[] themeRawImages;

    public override void Open(UIData uiData = null)
    {
        Cursor.lockState = CursorLockMode.None;
        GlobalInfo.CursorLockMode = CursorLockMode.None;

        GlobalInfo.account = null;
        base.Open(uiData);

        AddMsg((ushort)LoginEvent.CheckVersion,
            (ushort)LoginEvent.Login,
            (ushort)LoginEvent.Forget,
            (ushort)LoginEvent.Register);

        ModulePoint = this.FindChildByName("ModulePoint");
        themeRawImages = GetComponentsInChildren<ThemeRawImage>(true);

        if (uiData == null)
            ProcessEvent(new MsgBase((ushort)LoginEvent.Login));
        else
            ProcessEvent(new MsgBase((ushort)(uiData as PanelData).startModule));     
    }


    public override void Show(UIData uiData = null)
    {
        StartCoroutine(UpdateThemeElements(() =>
        {
            base.Show(uiData);
        }));
    }

    private IEnumerator UpdateThemeElements(UnityAction callback)
    {
        if(themeRawImages != null)
        {
            foreach (var themeRawImage in themeRawImages)
            {
                themeRawImage.UpdateElement();
            }
        }
        yield return new WaitUntil(() => themeRawImages == null || themeRawImages.Length == 0 || themeRawImages.All(r => r.Updated));

        callback?.Invoke();
    }

    private void QuitEvent()
    {
        var popupDic = new System.Collections.Generic.Dictionary<string, PopupButtonData>();
        popupDic.Add("取消", new PopupButtonData(null, false));
        popupDic.Add("关闭", new PopupButtonData(Application.Quit, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定关闭软件？", popupDic));
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)LoginEvent.CheckVersion:
                //UIManager.Instance.OpenModuleUI<CheckVersionModule>(this, ModulePoint);
                StartCoroutine(OpenModule<CheckVersionModule>());
                break;
            case (ushort)LoginEvent.Login:
                //UIManager.Instance.OpenModuleUI<LoginModule>(this, ModulePoint);
                StartCoroutine(OpenModule<LoginModule>());
                break;
            case (ushort)LoginEvent.Register:
                //UIManager.Instance.OpenModuleUI<RegisterModule>(this, ModulePoint);
                StartCoroutine(OpenModule<RegisterModule>());
                break;
            case (ushort)LoginEvent.Forget:
                //UIManager.Instance.OpenModuleUI<ForgetModule>(this, ModulePoint);
                StartCoroutine(OpenModule<ForgetModule>());
                break;
            case (ushort)ShortcutEvent.PressAnyKey:
                if (ShortcutManager.Instance.CheckShortcutKey(msg, ShortcutManager.ApplicationQuit))
                {
                    QuitEvent();
                }
                break;
        }
    }

    private IEnumerator OpenModule<T>() where T : UIModuleBase
    {
        yield return new WaitUntil(() => themeRawImages == null || themeRawImages.Length == 0 || themeRawImages.All(r => r.Updated));
        UIManager.Instance.OpenModuleUI<T>(this, ModulePoint);
    }

    public class PanelData : UIData
    {
        /// <summary>
        /// 初始模块
        /// </summary>
        public LoginEvent startModule { get; set; }
    }
}