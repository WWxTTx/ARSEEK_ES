using System.Collections.Generic;
using static UnityFramework.Runtime.ServiceRequestData;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 协同与服务器交互的接口
    /// </summary>
    public class ServiceApiData
    {
        /// <summary>
        /// 工业实时通信接口
        /// </summary>
        public static string rtc_api_url;

        public static string exam_rtc_api_url;

        /// <summary>
        /// 房间通道地址
        /// </summary>
        public static string rtm_url;
        /// <summary>
        /// 互动通道地址
        /// </summary>
        public static string rti_url;
        /// <summary>
        /// 帧同步通道地址
        /// </summary>
        public static string rtc_url;
        /// <summary>
        /// 视频通道地址
        /// </summary>
        public static string rtv_url;
        /// <summary>
        /// 音频通道地址
        /// </summary>
        public static string rta_url;

        public static void SetUrl(List<APIurl> apiList)
        {
            for (int i = 0; i < apiList.Count; i++)
            {
                switch (apiList[i].channel)
                {
                    case "rtm":
                        rtm_url = apiList[i].uri;
                        break;
                    case "rti":
                        rti_url = apiList[i].uri;
                        break;
                    case "rtc":
                        rtc_url = apiList[i].uri;
                        break;
                    case "rtv":
                        rtv_url = apiList[i].uri;
                        break;
                    case "rta":
                        rta_url = apiList[i].uri;
                        break;
                }
            }
        }

        private static string url = "139.155.5.87:21345";//10.0.1.125:12345
        public static void SetWSUrls()
        {
            rtm_url = $"ws://{url}/ws/rtm";
            rti_url = $"ws://{url}/ws/rti";
            rtc_url = $"ws://{url}/ws/rtc";
            rtv_url = $"ws://{url}/ws/rtv";
            rta_url = $"ws://{url}/ws/rta";
        }

        #region 旧协同服务器接口
        /// <summary>
        /// 获取房间列表
        /// </summary>
        public static string GetRooms { get { return rtc_api_url + "v2/rooms"; } }
        /// <summary>
        /// 创建 Post、删除 Delete、修改房间信息 Put
        /// </summary>
        public static string Room { get { return rtc_api_url + "v2/room"; } }
        /// <summary>
        /// 获取通道地址
        /// </summary>
        public static string URIs { get { return rtc_api_url + "v2/wsuris"; } }
        #endregion

        #region 预约考核房间
        /// <summary>
        /// 房间列表
        /// </summary>
        public static string NewRoomList { get { return exam_rtc_api_url + "room/list"; } }

        /// <summary>
        /// 创建房间
        /// </summary>
        public static string NewRoom { get { return exam_rtc_api_url + "room"; } }

        /// <summary>
        /// 判断能否进入房间
        /// </summary>
        public static string AssertRoom { get { return exam_rtc_api_url + "room/in"; } }

        /// <summary>
        /// 开始考核
        /// </summary>
        public static string RoomWorking { get { return exam_rtc_api_url + "room/working"; } }

        /// <summary>
        /// 重置房间状态
        /// </summary>
        public static string RoomReset { get { return exam_rtc_api_url + "room/reset"; } }

        /// <summary>
        /// 修改房间名称
        /// </summary>
        public static string SetRoomName { get { return exam_rtc_api_url + "room/name"; } }

        /// <summary>
        /// 修改房间密码
        /// </summary>
        public static string SetRoomPassword { get { return exam_rtc_api_url + "room/password"; } }

        /// <summary>
        /// 获取房间成员列表
        /// </summary>
        public static string GetRoomMembers { get { return exam_rtc_api_url + "room/members"; } }
        #endregion
    }
}
