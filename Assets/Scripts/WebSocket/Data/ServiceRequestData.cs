using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 协同服务器的交互数据结构
    /// </summary>
    public class ServiceRequestData
    {
        /// <summary>
        /// 请求返回结果基类
        /// </summary>
        public class ServiceRequestResultBase
        {
            /// <summary>
            ///  是否成功
            /// </summary>
            public bool success { get; set; }
            /// <summary>
            /// 结果
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int code { get; set; }
            /// <summary>
            ///  消息
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string msg { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string error { get; set; }
        }

        #region 旧接口
        /// <summary>
        /// 登录获取服务器接口配置列表数据结构 
        /// </summary>
        public class WSURIResult : ServiceRequestResultBase
        {
            public List<APIurl> data;
        }

        /// <summary>
        /// 获取服务器接口配置对象数据结构 
        /// </summary>
        public class APIurl
        {
            public string channel;
            public string uri;
        }

        /// <summary>
        /// 创建协同房间请求参数
        /// </summary>
        public class CreateRoomParam
        {
            /// <summary>
            /// 房间名称
            /// </summary>
            public string roomName;
            /// <summary>
            /// 房间密码
            /// </summary>
            public string password;
            /// <summary>
            /// 房间类型
            /// </summary>
            public int roomType;
            /// <summary>
            /// 学校ID
            /// </summary>
            public int schoolId;
            /// <summary>
            /// 创建人ID
            /// </summary>
            public int hostId;
            /// <summary>
            /// 创建人昵称
            /// </summary>
            public string hostNickname;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? duration;
        }

        /// <summary>
        /// 创建考核房间请求参数
        /// </summary>
        public class CreateExamRoomParam
        {
            /// <summary>
            /// 房间名称
            /// </summary>
            public string roomName;
            /// <summary>
            /// 房间密码
            /// </summary>
            public string password;
            /// <summary>
            /// 考核类型
            /// </summary>
            public int examType;
            /// <summary>
            /// 学校ID
            /// </summary>
            public int schoolId;
            /// <summary>
            /// 创建人ID
            /// </summary>
            public int hostId;
            /// <summary>
            /// 创建人昵称
            /// </summary>
            public string hostNickname;
        }

        /// <summary>
        /// 更新房间信息请求的数据结构 
        /// </summary>
        public class RoomStateRequest
        {
            /// <summary>
            /// 房间号
            /// </summary>
            public int roomId;
            /// <summary>
            /// 直播状态
            /// </summary>
            public int state;
            /// <summary>
            /// 课程Id
            /// </summary>
            public int courseId;
            /// <summary>
            /// 课程名称
            /// </summary>
            public string courseTitle;
            /// <summary>
            /// 课程icon url
            /// </summary>
            public string courseIcon;
        }

        /// <summary>
        /// 修改房间信息请求的数据结构 
        /// </summary>
        public class SetRoomInfoRequest
        {
            /// <summary>
            /// 房间号
            /// </summary>
            public int roomId;
            /// <summary>
            /// 房间名称
            /// </summary>
            public string roomName;
            /// <summary>
            /// 房间密码
            /// </summary>
            public string roomPassword;
        }

        #endregion

        /// <summary>
        /// 协同房间类型
        /// </summary>
        public enum RoomType
        {
            /// <summary>
            /// 直播
            /// </summary>
            Live = 1,
            /// <summary>
            /// 协同
            /// </summary>
            Synergia
        }

        /// <summary>
        /// 考核房间类型
        /// </summary>
        public enum ExamRoomType
        {
            /// <summary>
            /// 个人
            /// </summary>
            Person = 1,
            /// <summary>
            /// 小组
            /// </summary>
            Group
        }

        /// <summary>
        /// 创建预约考核房间请求参数
        /// </summary>
        public class CreateNewRoomParam
        {
            /// <summary>
            /// 房间名称 (房间名称长度不能超过100)
            /// </summary>
            public string roomName;
            /// <summary>
            /// 房间密码
            /// </summary>
            public string password;
            /// <summary>
            /// 考核房间类型
            /// </summary>
            public int examType;
            /// <summary>
            /// 协同房间类型
            /// </summary>
            public int roomType;
            /// <summary>
            /// 开启时间 yyyy-MM-dd HH:mm
            /// </summary>
            public string startTime;
            /// <summary>
            /// 时长 分钟
            /// </summary>
            public int duration;
            /// <summary>
            /// 课程信息
            /// </summary>
            public int courseId;
            public string courseTitle;
            public string courseIcon;
        }


        /// <summary>
        /// 考核房间UUID请求参数
        /// </summary>
        public class RoomUuidParam
        {
            /// <summary>
            /// 房间名称
            /// </summary>
            public string roomUuid;
        }

        /// <summary>
        /// 修改房间名称请求参数
        /// </summary>
        public class RoomNameParam
        {
            public string roomUuid;
            public string roomName;
        }


        /// <summary>
        /// 修改房间密码请求参数
        /// </summary>
        public class RoomPasswordParam
        {
            public string roomUuid;
            public string password;
        }

        /// <summary>
        /// 协同服务房间信息类
        /// </summary>
        public class RoomInfoModel
        {
            /// <summary>
            /// UUID
            /// </summary>
            public string Uuid { get; set; }
            /// <summary>
            /// 房间名字
            /// </summary>
            public string RoomName { get; set; }
            /// <summary>
            /// 房间密码
            /// </summary>
            public string Password;
            /// <summary>
            /// 是否有密码
            /// </summary>
            public bool NeedPwd { get; set; }
            /// <summary>
            /// 创建时间
            /// </summary>
            public string CreateTime { get; set; }
            /// <summary>
            /// 房主ID
            /// </summary>
            public int creatorId { get; set; }//hostId
            /// <summary>
            /// 房主昵称
            /// </summary>
            public string CreatorName { get; set; } //hostNickName
            /// <summary>
            /// 允许连接
            /// </summary>
            public bool AllowIn { get; set; }

            ///// <summary>
            ///// 协同 0未开播 1准备中 2正在直播
            ///// 考核 0未开播 1创建房间完成但未开始考核 2开始考核  
            ///// 0\1区别是房主是否在房间内？
            ///// </summary>
            //public int state { get; set; } = 0;

            /// <summary>
            /// 0：创建房间，1：提前十分钟开放连接，2：开始考核，3：结束
            /// </summary>
            public int Status { get; set; } = 0;
            /// <summary>
            /// 开放时间
            /// </summary>
            public string StartTime { get; set; }
            /// <summary>
            /// 课程Id
            /// </summary>
            public int CourseId { get; set; } = 0;
            /// <summary>
            /// 课程名称
            /// </summary>
            public string CourseTitle { get; set; }
            /// <summary>
            /// 课程封面
            /// </summary>
            public string CourseIcon { get; set; }
            /// <summary>
            /// 房间在线人数
            /// </summary>
            public int MemberCount { get; set; }
            /// <summary>
            /// 协同房间类型 1 直播  2 协同
            /// </summary>
            public int RoomType { get; set; }
            /// <summary>
            /// 考核房间类型 1 单人  2 小组
            /// </summary>
            public int ExamType { get; set; }
            /// <summary>
            /// 时长 分钟
            /// </summary>
            public int Duration { get; set; }

            public RoomInfoModel() { }
        }

      
        /// <summary>
        /// 协同房间成员类
        /// </summary>
        public class Member
        {
            /// <summary>
            /// 用户ID
            /// </summary>
            public int Id { get; set; }
            /// <summary>
            /// 昵称
            /// </summary>
            public string Nickname { get; set; }
            /// <summary>
            /// 工号
            /// </summary>
            public string UserNo { get; set; }
            /// <summary>
            /// 客户端类型
            /// </summary>
            public string ClientType { get; set; }
            /// <summary>
            /// 颜色
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ColorNumber { get; set; }
            /// <summary>
            /// 允许说话 true  成员禁言状态  false   
            /// </summary>
            public bool IsTalk { get; set; }
            /// <summary>
            /// 是否是主画面
            /// </summary>
            public bool IsMainScreen { get; set; }
            /// <summary>
            /// 有操控权限 true  无操控权限  false
            /// </summary>
            public bool IsControl { get; set; }
            /// <summary>
            /// 开启语音 true  静音 false
            /// </summary>
            public bool IsChat { get; set; }
        }
    }
}