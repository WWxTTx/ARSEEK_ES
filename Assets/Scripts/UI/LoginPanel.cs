using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;

public class LoginPanel : UIPanelBase
{
    private Transform ModulePoint;
    private ThemeRawImage[] themeRawImages;
    private Func<bool> themeReadyPredicate;

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
        themeReadyPredicate = () => themeRawImages == null || themeRawImages.Length == 0 || themeRawImages.All(r => r.Updated);

        if (uiData == null)
            ProcessEvent(new MsgBase((ushort)LoginEvent.Login));
        else
            ProcessEvent(new MsgBase((ushort)(uiData as PanelData).startModule));     
    }


    public override void Show(UIData uiData = null)
    {
        UpdateThemeElements(() =>
        {
            base.Show(uiData);
        }, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid UpdateThemeElements(UnityAction callback, System.Threading.CancellationToken ct)
    {
        if(themeRawImages != null)
        {
            foreach (var themeRawImage in themeRawImages)
            {
                themeRawImage.UpdateElement();
            }
        }
        await UniTask.WaitUntil(themeReadyPredicate, cancellationToken: ct);

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
                OpenModule<CheckVersionModule>(this.GetCancellationTokenOnDestroy()).Forget();
                break;
            case (ushort)LoginEvent.Login:
                //UIManager.Instance.OpenModuleUI<LoginModule>(this, ModulePoint);
                OpenModule<LoginModule>(this.GetCancellationTokenOnDestroy()).Forget();
                break;
            case (ushort)LoginEvent.Register:
                //UIManager.Instance.OpenModuleUI<RegisterModule>(this, ModulePoint);
                OpenModule<RegisterModule>(this.GetCancellationTokenOnDestroy()).Forget();
                break;
            case (ushort)LoginEvent.Forget:
                //UIManager.Instance.OpenModuleUI<ForgetModule>(this, ModulePoint);
                OpenModule<ForgetModule>(this.GetCancellationTokenOnDestroy()).Forget();
                break;
            case (ushort)ShortcutEvent.PressAnyKey:
                if (ShortcutManager.Instance.CheckShortcutKey(msg, ShortcutManager.ApplicationQuit))
                {
                    QuitEvent();
                }
                break;
        }
    }

    private async UniTaskVoid OpenModule<T>(System.Threading.CancellationToken ct) where T : UIModuleBase
    {
        await UniTask.WaitUntil(themeReadyPredicate, cancellationToken: ct);
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