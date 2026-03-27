using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static UnityFramework.Runtime.ServiceRequestData;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 通用工具类
    /// </summary>
    public static class ToolManager
    {      
        /// <summary>
        /// 不支持离线模式
        /// </summary>
        public static void UnsupportOffline()
        {
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "离线模式不支持此功能",
                 new Dictionary<string, PopupButtonData>() { { "确定", new PopupButtonData(null, true) } }));
        }

        /// <summary>
        /// 请先在线下载资源
        /// </summary>
        public static void PleaseOnline()
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() => GoToLogin(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "请先正常登录后下载资源", popupDic));
        }

        /// <summary>
        /// 返回登录界面提示弹窗
        /// </summary>
        public static void MultipointLogin(string msg)
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确认", new PopupButtonData(() => GoToLogin(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", msg, popupDic));
        }

        /// <summary>
        /// 返回登录界面
        /// </summary>
        public static void GoToLogin()
        {
            UIManager.Instance.CloseAllUI();
            GlobalInfo.SetCourseMode(CourseMode.Training);
            GlobalInfo.isOffLine = false;
            GlobalInfo.courseDicExists.Clear();
            UIManager.Instance.OpenUI<LoginPanel>();
        }
        /// <summary>
        /// 无网络连接弹窗
        /// </summary>
        public static void InternetError()
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("确定", new PopupButtonData(null, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("网络错误", "无网络连接", popupDic));
            }
        }

        /// <summary>
        /// 无网络连接弹窗
        /// </summary>
        public static void ServiceException()
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("确定", new PopupButtonData(null, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("服务器错误", "服务器异常，请稍后再试", popupDic));
            }
        }
        /// <summary>
        /// 发送直播和协同消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="synergia">仅用于协同房间 是否允许非主画面权限用户广播</param>
        public static void SendBroadcastMsg(MsgBase msg, bool synergia = false)
        {
            MsgBrodcastOperate msgBrodcastOperate = new MsgBrodcastOperate(msg.msgId, JsonTool.Serializable(msg));
            //单人考核
            if (GlobalInfo.courseMode == CourseMode.Exam)
            {
                if (GlobalInfo.IsHomeowner())
                {
                    NetworkManager.Instance.SendIMMsg(msgBrodcastOperate);
                }
                else
                {
                    SendLocalMsg(msgBrodcastOperate);
                }
            }
            //多人考核
            else if (GlobalInfo.courseMode == CourseMode.OnlineExam)
            {
                NetworkManager.Instance.SendIMMsg(msgBrodcastOperate);
            }
            //协同
            else if (GlobalInfo.courseMode == CourseMode.Collaboration)
            {
                if (GlobalInfo.IsMainScreen() || GlobalInfo.IsHomeowner())
                {
                    NetworkManager.Instance.SendIMMsg(msgBrodcastOperate);
                }
                else
                {
                    if (synergia)
                        NetworkManager.Instance.SendIMMsg(msgBrodcastOperate);
                    else
                        SendLocalMsg(msgBrodcastOperate);
                }
            }
            //直播
            else if (GlobalInfo.courseMode == CourseMode.Livebroadcast)
            {
                SendLocalMsg(msgBrodcastOperate);
            }
            //本地
            else
            {
                SendLocalMsg(msgBrodcastOperate);
            }
        }

        private static void SendLocalMsg(MsgBrodcastOperate msgBrodcastOperate)
        {
            if (FormMsgManager.Instance != null)
                FormMsgManager.Instance.SendMsg(msgBrodcastOperate);
            else
                Log.Error("FormMsgManager Instance is null!");
        }
    }
}