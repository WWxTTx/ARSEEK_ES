using System.Collections.Generic;
using UnityEngine;

namespace UnityFramework.Runtime
{
    /// <summary>
    ///   UI面板类（常驻UI）
    /// </summary>
    public class UIPanelBase : UIBase
    {
        /// <summary>
        /// 当前界面是否需要使用快捷键（esc）打开设置界面
        /// </summary>
        public virtual bool canOpenOption => false;
        public override void Open(UIData uiData = null)
        {
            base.Open(uiData);

            if (CanLogout)
                AddMsg((ushort)OptionPanelEvent.Logout);
        }

        public override void ProcessEvent(MsgBase msg)
        {
            base.ProcessEvent(msg);

            switch (msg.msgId)
            {
                case (ushort)OptionPanelEvent.Logout:
                    GotoLogout();
                    break;
                case (ushort)ShortcutEvent.PressAnyKey:
                    if (canOpenOption && ShortcutManager.Instance.CheckShortcutKey(msg, ShortcutManager.OpenOption))
                    {
                        if (UIManager.Instance.IsOpen<OptionPanel>())
                        {
                            UIManager.Instance.CloseUI<OptionPanel>();
                        }
                        else
                        {
                            UIManager.Instance.OpenUI<OptionPanel>(UILevel.Fixed);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 返回上一页
        /// </summary>
        public virtual void Previous()
        {

        }

        /// <summary>
        /// 退出登录
        /// </summary>
        public virtual void GotoLogout()
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null));
            popupDic.Add("退出", new PopupButtonData(() => ToolManager.GoToLogin(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("登出提示", "确定要退出登录吗?", popupDic));
        }
    }
}