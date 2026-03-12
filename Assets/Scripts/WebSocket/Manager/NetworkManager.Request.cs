using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 协同服务请求
/// </summary>
public partial class NetworkManager : Singleton<NetworkManager>, INetworkManager, IRoomAgentHelper, IIMAgentHelper, IFrameAgentHelper, IAudioAgentHelper, IVideoAgentHelper
{
    private RequestBase requestBase;

    #region 旧接口
    ///// <summary>
    ///// 登录房间，请求通道地址
    ///// </summary>
    ///// <param name="roomNumber">房间号</param>
    ///// <param name="roomPassword">房间密码</param>
    ///// <param name="userId">账号id</param>
    ///// <param name="userName">账号昵称</param>
    ///// <param name="successCallBack">请求成功回调</param>
    ///// <param name="failureCallBack">请求失败回调</param>
    //public void GetWSUrls(string roomNumber, string roomPassword, int userId, string userName, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
    //{
    //    Log.Warning($"向服务器发送加入房间请求 client={ApiData.ClientID}&roomNumber={roomNumber}&roomPassword{roomPassword}&uid={userId}&nickName={userName}");

    //    string signSecretKey = "UwfgVQo6uKjVLG5Ex7PiGOODVq";
    //    string dataToSign = $"{ApiData.ClientID}:{roomNumber}:{userId}:{signSecretKey}";
    //    string sign = Encryption.MD5Encrypt(dataToSign);

    //    requestBase.TryRequest_List("向服务器发送加入房间请求", RequestType.GET, $"{ServiceApiData.URIs}?client={ApiData.ClientID}&roomNumber={roomNumber}&roomPassword={roomPassword}&uid={userId}&nickName={userName}&sign={sign}", string.Empty, (result, message) =>
    //    {
    //        if (result)
    //        {
    //            WSURIResult apiUrlListRequestResult = JsonTool.DeSerializable<WSURIResult>(message);
    //            if (apiUrlListRequestResult.success)
    //            {
    //                ServiceApiData.SetUrl(apiUrlListRequestResult.data);
    //                successCallBack?.Invoke();
    //            }
    //            else
    //                failureCallBack?.Invoke(200, apiUrlListRequestResult.msg);
    //        }
    //        else
    //        {
    //            failureCallBack?.Invoke(0, message);
    //        }
    //    }, false);
    //}

    ///// <summary>
    ///// 创建协同房间
    ///// </summary>
    ///// <param name="roomName"></param>
    ///// <param name="roomPassword"></param>
    ///// <param name="successCallBack">请求成功回调</param>
    ///// <param name="failureCallBack">请求失败回调</param>
    //public void CreateRoom(string roomName, string roomPassword, RoomType roomType, UnityAction<RoomInfoModel> successCallBack, UnityAction<int, string> failureCallBack)
    //{
    //    CreateRoomParam roomParam = new CreateRoomParam()
    //    {
    //        roomName = roomName,
    //        password = roomPassword,
    //        roomType = (int)roomType,
    //        schoolId = GlobalInfo.account.schoolId,
    //        hostId = GlobalInfo.account.id,
    //        hostNickname = GlobalInfo.account.nickname
    //    };
    //    string json = JsonTool.Serializable(roomParam);
    //    Log.Warning($"向服务器发送创建协同房间请求 {json}");
    //    requestBase.TryRequest_List("创建协同房间", RequestType.POST, ServiceApiData.Room, json, (result, message) =>
    //    {
    //        GetRequest(result, message, successCallBack, failureCallBack);
    //    }, false);
    //}

    ///// <summary>
    ///// 创建考核房间
    ///// </summary>
    ///// <param name="roomName"></param>
    ///// <param name="roomPassword"></param>
    ///// <param name="successCallBack">请求成功回调</param>
    ///// <param name="failureCallBack">请求失败回调</param>
    //public void CreateExamRoom(string roomName, string roomPassword, ExamRoomType examRoomType, UnityAction<RoomInfoModel> successCallBack, UnityAction<int, string> failureCallBack)
    //{
    //    CreateExamRoomParam roomParam = new CreateExamRoomParam()
    //    {
    //        roomName = roomName,
    //        password = roomPassword,
    //        examType = (int)examRoomType,
    //        schoolId = GlobalInfo.account.schoolId,
    //        hostId = GlobalInfo.account.id,
    //        hostNickname = GlobalInfo.account.nickname
    //    };
    //    string json = JsonTool.Serializable(roomParam);
    //    Log.Warning($"向服务器发送创建考核房间请求 {json}");
    //    requestBase.TryRequest_List("创建考核房间", RequestType.POST, ServiceApiData.Room, json, (result, message) =>
    //    {
    //        GetRequest(result, message, successCallBack, failureCallBack);
    //    }, false);
    //}
    ///// <summary>
    ///// 获取房间列表
    ///// </summary>
    ///// <param name="successCallBack">请求成功回调</param>
    ///// <param name="failureCallBack">请求失败回调</param>
    //public void GetRoomList(UnityAction<List<RoomInfoModel>> successCallBack, UnityAction<int, string> failureCallBack, bool isLoadingOn = true)
    //{
    //    string json = "schoolId=" + GlobalInfo.account.schoolId;
    //    string url = ServiceApiData.GetRooms + "?" + json;

    //    requestBase.TryRequest_List("获取房间列表", RequestType.GET, url, json, (result, message) =>
    //    {
    //        GetRequest(result, message, successCallBack, failureCallBack);
    //    }, isLoadingOn);
    //}

    ///// <summary>
    ///// 更新房间信息
    ///// </summary>
    ///// <param name="roomInfo"></param>
    ///// <param name="successCallBack">请求成功回调</param>
    ///// <param name="failureCallBack">请求失败回调</param>
    //public void UpdateRoomState(RoomInfoModel roomInfo, UnityAction successCallBack, UnityAction<string> failureCallBack)
    //{
    //    RoomStateRequest roomStateRequest = new RoomStateRequest()
    //    {
    //        roomId = roomInfo.roomId,
    //        state = roomInfo.state,
    //        courseId = roomInfo.courseId,
    //        courseTitle = roomInfo.courseTitle,
    //        courseIcon = roomInfo.courseIcon
    //    };
    //    string json = JsonTool.Serializable(roomStateRequest);

    //    requestBase.TryRequest_List("更新房间信息", RequestType.PUT, ServiceApiData.Room, json, (result, message) =>
    //    {
    //        GetRequest(result, message, successCallBack, failureCallBack);
    //    }, false);
    //}

    ///// <summary>
    ///// 修改房间信息
    ///// </summary>
    ///// <param name="roomId"></param>
    ///// <param name="roomName"></param>
    ///// <param name="roomPassword"></param>
    ///// <param name="successCallBack">请求成功回调</param>
    ///// <param name="failureCallBack">请求失败回调</param>
    //public void SetRoomInfo(int roomId, string roomName, string roomPassword, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
    //{
    //    SetRoomInfoRequest roomInfoRequest = new SetRoomInfoRequest()
    //    {
    //        roomId = roomId,
    //        roomName = roomName,
    //        roomPassword = roomPassword
    //    };
    //    string json = JsonTool.Serializable(roomInfoRequest);

    //    requestBase.TryRequest_List("修改房间信息", RequestType.PUT, ServiceApiData.Room, json, (result, message) =>
    //    {
    //        GetRequest(result, message, successCallBack, failureCallBack);
    //    });
    //}

    ///// <summary>
    ///// 删除房间
    ///// </summary>
    ///// <param name="roomId"></param>
    ///// <param name="successCallBack">请求成功回调</param>
    ///// <param name="failureCallBack">请求失败回调</param>
    //public void DeleteRoom(int roomId, UnityAction successCallBack, UnityAction<string> failureCallBack)
    //{
    //    requestBase.TryRequest_List("删除房间", RequestType.DELETE, ServiceApiData.Room + "/" + roomId, string.Empty, (result, message) =>
    //    {
    //        GetRequest(result, message, successCallBack, failureCallBack);
    //    });
    //}
    #endregion

    #region 新接口 预约考核房间
    /// <summary>
    /// 获取房间列表
    /// </summary>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    /// <param name="isLoadingOn"></param>
    public void GetRoomList(UnityAction<List<RoomInfoModel>> successCallBack, UnityAction<int, string> failureCallBack, bool isLoadingOn = true)
    {
        requestBase.TryRequest_List("获取房间列表", RequestType.GET, ServiceApiData.NewRoomList, string.Empty, (result, message) =>
        {
            GetRequest(result, message, successCallBack, failureCallBack);
        }, isLoadingOn);
    }

    /// <summary>
    /// 创建协同房间
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="roomPassword"></param>
    /// <param name="startTime"></param>
    /// <param name="duration"></param>
    /// <param name="roomType"></param>
    /// <param name="courseId"></param>
    /// <param name="courseTitle"></param>
    /// <param name="courseIcon"></param>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    public void CreateRoom(string roomName, string roomPassword, RoomType roomType,
        int courseId, string courseTitle, string courseIcon, UnityAction<string> successCallBack, UnityAction<int, string> failureCallBack)
    {
        CreateNewRoomParam roomParam = new CreateNewRoomParam()
        {
            roomName = roomName,
            password = roomPassword,
            roomType = (int)roomType,
            courseId = courseId,
            courseTitle = courseTitle,
            courseIcon = courseIcon
        };
        string json = JsonTool.Serializable(roomParam);
        Log.Warning($"向服务器发送创建协同请求 {json}");
        requestBase.TryRequest_List("创建协同房间", RequestType.POST, ServiceApiData.NewRoom, json, (result, message) =>
        {
            //GetRequest(result, message, successCallBack, failureCallBack);
            if (result)
            {
                var jObject = JObject.Parse(message);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(jObject["data"].Value<string>());
                else
                    failureCallBack?.Invoke(jObject["code"].Value<int>(), GetMessage(jObject));
            }
            else
                failureCallBack?.Invoke(0, "无网络连接");
        });
    }

    /// <summary>
    /// 创建预约房间
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="roomPassword"></param>
    /// <param name="startTime"></param>
    /// <param name="duration"></param>
    /// <param name="roomType"></param>
    /// <param name="courseId"></param>
    /// <param name="courseTitle"></param>
    /// <param name="courseIcon"></param>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    public void CreateExamReserveRoom(string roomName, string roomPassword, string startTime, int duration, ExamRoomType roomType,
        int courseId, string courseTitle, string courseIcon, UnityAction<string> successCallBack, UnityAction<int, string> failureCallBack)
    {
        CreateNewRoomParam roomParam = new CreateNewRoomParam()
        {
            roomName = roomName,
            password = roomPassword,
            examType = (int)roomType,
            startTime = startTime,
            duration = duration,
            courseId = courseId,
            courseTitle = courseTitle,
            courseIcon = courseIcon
        };
        string json = JsonTool.Serializable(roomParam);
        Log.Warning($"向服务器发送创建预约考核请求 {json}");
        requestBase.TryRequest_List("创建预约考核房间", RequestType.POST, ServiceApiData.NewRoom, json, (result, message) =>
        {
            //GetRequest(result, message, successCallBack, failureCallBack);
            if (result)
            {
                var jObject = JObject.Parse(message);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(jObject["data"].Value<string>());
                else
                    failureCallBack?.Invoke(jObject["code"].Value<int>(), GetMessage(jObject));
            }
            else
                failureCallBack?.Invoke(0, "无网络连接");
        });
    }

    /// <summary>
    /// 获取房间信息
    /// </summary>
    /// <param name="roomUuid"></param>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    /// <param name="isLoadingOn"></param>
    public void GetRoomInfo(string roomUuid, UnityAction<RoomInfoModel> successCallBack, UnityAction<int, string> failureCallBack, bool isLoadingOn = true)
    {
        string url = ServiceApiData.NewRoom + $"?roomUuid={roomUuid}";

        requestBase.TryRequest_List("获取预约考核房间信息", RequestType.GET, url, string.Empty, (result, message) =>
        {
            //GetRequest(result, message, successCallBack, failureCallBack);
            if (result)
            {
                var jObject = JObject.Parse(message);

                if (IsSuccess(jObject))
                {
                    if (jObject["data"] == null)
                    {
                        successCallBack?.Invoke(null);
                    }
                    else
                    {
                        successCallBack?.Invoke(GetData<RoomInfoModel>(jObject));
                    }
                }
                else
                    failureCallBack?.Invoke(jObject["code"].Value<int>(), GetMessage(jObject));
            }
            else
                failureCallBack?.Invoke(0, "无网络连接");
        }, isLoadingOn);
    }

    /// <summary>
    /// 修改房间名称
    /// </summary>
    /// <param name="roomUuid"></param>
    /// <param name="roomName"></param>
    /// <param name="successCallBack">请求成功回调</param>
    /// <param name="failureCallBack">请求失败回调</param>
    public void SetRoomName(string roomUuid, string roomName, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
    {
        RoomNameParam param = new RoomNameParam()
        {
            roomUuid = roomUuid,
            roomName = roomName
        };
        string json = JsonTool.Serializable(param);
        requestBase.TryRequest_List("修改房间名称", RequestType.PUT, ServiceApiData.SetRoomName, json, (result, message) =>
        {
            if (string.IsNullOrEmpty(message))
            {
                successCallBack.Invoke();
                return;
            }
            GetRequest(result, message, successCallBack, failureCallBack);
        }, false);
    }

    /// <summary>
    /// 修改房间密码
    /// </summary>
    /// <param name="roomUuid"></param>
    /// <param name="roomPassword"></param>
    /// <param name="successCallBack">请求成功回调</param>
    /// <param name="failureCallBack">请求失败回调</param>
    public void SetRoomPassword(string roomUuid, string roomPassword, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
    {
        RoomPasswordParam param = new RoomPasswordParam()
        {
            roomUuid = roomUuid,
            password = roomPassword
        };
        string json = JsonTool.Serializable(param);
        requestBase.TryRequest_List("修改房间密码", RequestType.PUT, ServiceApiData.SetRoomPassword, json, (result, message) =>
        {
            if (string.IsNullOrEmpty(message))
            {
                successCallBack.Invoke();
                return;
            }
            GetRequest(result, message, successCallBack, failureCallBack);
        }, false);
    }

    /// <summary>
    /// 删除房间
    /// </summary>
    /// <param name="roomUuid"></param>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    /// <param name="isLoadingOn"></param>
    public void DeleteRoom(string roomUuid, UnityAction successCallBack, UnityAction<int, string> failureCallBack, bool isLoadingOn = true)
    {
        string url = ServiceApiData.NewRoom + $"/{roomUuid}";

        requestBase.TryRequest_List("删除预约考核房间", RequestType.DELETE, url, string.Empty, (result, message) =>
        {
            GetRequest(result, message, successCallBack, failureCallBack);
        }, isLoadingOn);
    }


    /// <summary>
    /// 判断房间能否进入
    /// </summary>
    /// <param name="roomUid"></param>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    /// <param name="isLoadingOn"></param>
    public void AssertExamRoom(string roomUid, UnityAction<RoomInfoModel> successCallBack, UnityAction<string> failureCallBack, bool isLoadingOn = true)
    {
        RoomUuidParam param = new RoomUuidParam()
        {
            roomUuid = roomUid
        };
        string json = JsonTool.Serializable(param);
        requestBase.TryRequest_List("判断房间能否进入", RequestType.POST, ServiceApiData.AssertRoom, json, (result, message) =>
        {
            if (string.IsNullOrEmpty(message))
            {
                successCallBack.Invoke(null);
                return;
            }
            GetRequest(result, message, successCallBack, failureCallBack);
        }, isLoadingOn);
    }


    /// <summary>
    /// 开始考核 修改房间状态
    /// </summary>
    /// <param name="roomUid"></param>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    /// <param name="isLoadingOn"></param>
    public void RoomWorking(string roomUuid, UnityAction successCallBack, UnityAction<string> failureCallBack, bool isLoadingOn = true)
    {
        RoomUuidParam param = new RoomUuidParam()
        {
            roomUuid = roomUuid
        };
        string json = JsonTool.Serializable(param);
        requestBase.TryRequest_List("修改房间状态", RequestType.POST, ServiceApiData.RoomWorking, json, (result, message) =>
        {
            GetRequest(result, message, successCallBack, failureCallBack);
        }, isLoadingOn);
    }


    /// <summary>
    /// 重置房间状态
    /// </summary>
    /// <param name="roomUid"></param>
    /// <param name="successCallBack"></param>
    /// <param name="failureCallBack"></param>
    /// <param name="isLoadingOn"></param>
    public void RoomReset(string roomUid, UnityAction<RoomInfoModel> successCallBack, UnityAction<string> failureCallBack, bool isLoadingOn = true)
    {
        RoomUuidParam param = new RoomUuidParam()
        {
            roomUuid = roomUid
        };
        string json = JsonTool.Serializable(param);
        requestBase.TryRequest_List("重置房间状态", RequestType.POST, ServiceApiData.RoomReset, json, (result, message) =>
        {
            if (string.IsNullOrEmpty(message))
            {
                successCallBack.Invoke(null);
                return;
            }
            GetRequest(result, message, successCallBack, failureCallBack);
        }, isLoadingOn);
    }
    #endregion

    #region 工具
    /// <summary>
    /// 处理请求回调 成功回调不带参数
    /// </summary>
    /// <param name="requestIsSuccess">请求是否成功</param>
    /// <param name="requestData">请求的数据</param>
    /// <param name="successCallBack">成功回调</param>
    /// <param name="failureCallBack">失败回调</param>
    private void GetRequest(bool requestIsSuccess, string requestData, UnityAction successCallBack, UnityAction<string> failureCallBack)
    {
        if (requestIsSuccess)
        {
            var jObject = JObject.Parse(requestData);

            if (IsSuccess(jObject))
                successCallBack?.Invoke();
            else
                failureCallBack?.Invoke(GetMessage(jObject));
        }
        else
            failureCallBack?.Invoke(requestData);
    }
    private void GetRequest(bool requestIsSuccess, string requestData, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
    {
        if (requestIsSuccess)
        {
            var jObject = JObject.Parse(requestData);

            if (IsSuccess(jObject))
                successCallBack?.Invoke();
            else
                failureCallBack?.Invoke(jObject["code"].Value<int>(), GetMessage(jObject));
        }
        else
            failureCallBack?.Invoke(0, "无网络连接");
    }

    /// <summary>
    /// 处理请求回调 成功回调带参数
    /// </summary>
    /// <typeparam name="T">成功回调携带的参数</typeparam>
    /// <param name="requestIsSuccess">请求是否成功</param>
    /// <param name="requestData">请求的数据</param>
    /// <param name="successCallBack">成功回调</param>
    /// <param name="failureCallBack">失败回调</param>
    private void GetRequest<T>(bool requestIsSuccess, string requestData, UnityAction<T> successCallBack, UnityAction<string> failureCallBack) where T : class
    {
        if (requestIsSuccess)
        {
            var jObject = JObject.Parse(requestData);

            if (IsSuccess(jObject))
                successCallBack?.Invoke(GetData<T>(jObject));
            else
                failureCallBack?.Invoke(GetMessage(jObject));
        }
        else
            failureCallBack?.Invoke(requestData);
    }
    private void GetRequest<T>(bool requestIsSuccess, string requestData, UnityAction<T> successCallBack, UnityAction<int, string> failureCallBack) where T : class
    {
        if (requestIsSuccess)
        {
            var jObject = JObject.Parse(requestData);

            if (IsSuccess(jObject))
                successCallBack?.Invoke(GetData<T>(jObject));
            else
                failureCallBack?.Invoke(jObject["code"].Value<int>(), GetMessage(jObject));
        }
        else
            failureCallBack?.Invoke(0, "无网络连接");
    }

    /// <summary>
    /// 处理请求回调,成功回调带泛型参数
    /// </summary>
    /// <param name="requestIsSuccess">请求是否成功</param>
    /// <param name="requestData">请求的数据</param>
    /// <param name="successCallBack">成功回调</param>
    /// <param name="failureCallBack">失败回调</param>
    /// <param name="temp">用于强行区分两种GetRequest</param>
    private void GetRequest<T>(bool requestIsSuccess, string requestData, UnityAction<T> successCallBack, UnityAction<string> failureCallBack, bool temp = false) where T : struct
    {
        if (requestIsSuccess)
        {
            var jObject = JObject.Parse(requestData);

            if (IsSuccess(jObject))
                successCallBack.Invoke(GetData<T>(jObject));
            else
                failureCallBack.Invoke(GetMessage(jObject));
        }
        else
            failureCallBack.Invoke(requestData);
    }

    /// <summary>
    /// 检查是否成功 开出来用于统一管理 应对后台变动
    /// </summary>
    /// <param name="jObject"></param>
    /// <returns></returns>
    private bool IsSuccess(JObject jObject)
    {
        return jObject["success"].Value<bool>();
    }
    /// <summary>
    /// 获取请求回执信息 开出来用于统一管理 应对后台变动
    /// </summary>
    /// <param name="jObject"></param>
    /// <returns></returns>
    private string GetMessage(JObject jObject)
    {
        if (jObject["msg"] != null)
            return jObject["msg"].ToString();
        else if (jObject["message"] != null)
            return jObject["message"].ToString();
        return string.Empty;
    }
    /// <summary>
    /// 仅获取data的json 不获取多余的东西 开出来用于统一管理 应对后台变动
    /// </summary>
    /// <param name="jObject"></param>
    /// <returns></returns>
    private T GetData<T>(JObject jObject)
    {
        return JsonTool.DeSerializable<T>(jObject["data"].ToString());
    }
    #endregion
}