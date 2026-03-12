using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using static UnityFramework.Runtime.RequestData;

namespace UnityFramework.Runtime
{
    public enum RequestType
    {
        GET,
        POST,
        PUT,
        DELETE,
        PATCH,
    }

    public class RequestBase : MonoBehaviour
    {
        /// <summary>
        /// 当前请求数量
        /// </summary>
        private int currentRequest = 0;
        /// <summary>
        /// 同时请求的数量
        /// </summary>
        private const int maxRequest = 10;
        /// <summary>
        /// 请求列表
        /// </summary>
        private List<RequestData> requestList = new List<RequestData>();
        /// <summary>
        /// 临时缓存数据
        /// </summary>
        private RequestData tempData;

        /// <summary>
        /// 尝试发送请求
        /// </summary>
        /// <param name="requestName">请求名称</param>
        /// <param name="requestType">请求类型</param>
        /// <param name="requestUrl">请求地址</param>
        /// <param name="paraJson">请求数据</param>
        /// <param name="resultCallBack">响应回调</param>
        /// <param name="isLoadingOn">是否显示加载界面，默认为true-显示</param>
        public void TryRequest_List(string requestName, RequestType requestType, string requestUrl, string paraJson, UnityAction<bool, string> resultCallBack, bool isLoadingOn = true)
        {
            var requestData = new RequestData()
            {
                requestName = requestName,
                requestType = requestType,
                paraJson = paraJson,
                requestUrl = requestUrl,
                resultCallBack = resultCallBack,
                isLoadingOn = isLoadingOn
            };

            lock (requestList)
            {
                requestList.Add(requestData);
            }
        }

        // 新增headers字段
        public void TryRequest_List(string requestName, RequestType requestType, string requestUrl, string paraJson, UnityAction<bool, string> resultCallBack, Dictionary<string, string> headers, bool isLoadingOn = true)
        {
            var requestData = new RequestData()
            {
                requestName = requestName,
                requestType = requestType,
                paraJson = paraJson,
                requestUrl = requestUrl,
                resultCallBack = resultCallBack,
                isLoadingOn = isLoadingOn,
                headers = headers   
            };

            lock (requestList)
            {
                requestList.Add(requestData);
            }
        }


        public void TryRequest(string requestName, RequestType requestType, string requestUrl, string paraJson, UnityAction<bool, string> resultCallBack, bool isLoadingOn = true)
        {
            if (string.IsNullOrEmpty(requestName))
                requestName = "WebApi请求";

            //离线模式，已停用
            //if (GlobalInfo.isOffLine)
            //{
            //    string log = string.Format(requestName + "离线请求:\r\n{0}\r\n{1}\r\n{2}",
            //      "请求类型:" + requestType,
            //      "请求地址:" + requestUrl,
            //      "请求数据:" + paraJson);
            //    Log.Info(log);

            //    switch (requestType)
            //    {
            //        case RequestType.GET://Get请求记录到本地
            //            string result = ConfigXML.GetData(DtataType.LocalSever, GetNewUrl(requestUrl));
            //            this.WaitTime(0.01f, () =>
            //            {
            //                Log.Info(requestName + "离线请求返回：<color=green>" + result + "</color>");
            //                if (string.IsNullOrEmpty(result))
            //                {
            //                    ToolManager.PleaseOnline();
            //                    resultCallBack?.Invoke(false, default);
            //                }
            //                else
            //                    resultCallBack?.Invoke(true, result);
            //            });
            //            break;
            //        case RequestType.POST:
            //        case RequestType.PUT:
            //        case RequestType.DELETE:
            //        case RequestType.PATCH:
            //            ToolManager.UnsupportOffline();
            //            break;
            //        default:
            //            break;
            //    }
            //    return;
            //}

            StartCoroutine(Request(requestName, requestUrl, requestType, paraJson, resultCallBack, isLoadingOn));
        }

        /// <summary>
        /// 去掉api前缀
        /// </summary>
        /// <param name="oldUrl">包含前缀的链接</param>
        /// <returns></returns>
        public static string GetNewUrl(string oldUrl)
        {
            string str = "/api/";//标志字符串
            int index = oldUrl.IndexOf(str);
            if (index > 0)
            {
                index = oldUrl.IndexOf("/", index + str.Length) + 1;
                oldUrl = oldUrl.Substring(index);
            }

            return oldUrl;
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="requestName">请求名称</param>
        /// <param name="url">请求地址</param>
        /// <param name="requestType">请求类型</param>
        /// <param name="resultCallBack">响应回调</param>
        /// <param name="isLoadingOn">是否显示加载界面</param>
        /// <returns></returns>
        IEnumerator Request(string requestName, string url, RequestType requestType, string paraJson, UnityAction<bool, string> resultCallBack, bool isLoadingOn)
        {
            if (isLoadingOn)
                UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);

            // 解析发送请求需要的数据
            string contentType = string.Empty;//请求内容类型
            byte[] bytes = null;//请求参数数组
            switch (requestType)
            {
                case RequestType.GET:
                    contentType = "application/x-www-form-urlencoded";
                    break;
                case RequestType.POST:
                case RequestType.PUT:
                case RequestType.DELETE:
                    contentType = "application/json";
                    bytes = System.Text.Encoding.UTF8.GetBytes(paraJson);
                    break;
                case RequestType.PATCH:
                    break;
                default:
                    break;
            }
            string method = requestType.ToString();//请求类型
            if (string.IsNullOrEmpty(paraJson))
                paraJson = "无";

            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                request.SetRequestHeader("Content-Type", contentType);
                request.SetRequestHeader("x-client", ApiData.ClientID);
                request.SetRequestHeader("x-client-version", Application.version);
                request.SetRequestHeader("x-device", ApiData.DeviceID);
                if (!string.IsNullOrEmpty(ApiData.AccessToken))
                    request.SetRequestHeader("Authorization", "bearer " + ApiData.AccessToken);

                //todo Curl error 60: Cert verify failed: UNITYTLS_X509VERIFY_FLAG_EXPIRED
                request.certificateHandler = new CertHandler();

                request.timeout = 60;//30
                if (bytes != null && bytes.Length > 0)
                    request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                //string log = string.Format("{0}:\r\n{1}\r\n{2}\r\n{3}\r\n{4}\r\n{5}\r\n{6}\r\n{7}\r\n{8}\r\n",
                //    requestName,
                //    "请求地址:" + url,
                //    "请求方法:" + method,
                //    "请求参数:" + paraJson,
                //    "x-client:" + ApiData.ClientID,
                //    "x-client-version:" + Application.version,
                //    "x-device:" + ApiData.DeviceID,
                //    "Authorization:bearer " + ApiData.AccessToken,
                //    "Content-Type:" + contentType);
                string log = string.Format("{0}:\r\n{1}\r\n{2}\r\n{3}\r\n{4}\r\n{5}\r\n{6}\r\n{7}\r\n",
                   requestName,
                   "请求地址:" + url,
                   "请求方法:" + method,
                   "请求参数:" + paraJson,
                   "x-client:" + ApiData.ClientID,
                   "x-client-version:" + Application.version,
                   "x-device:" + ApiData.DeviceID,
                   "Content-Type:" + contentType);
                Debug.Log(log);

                yield return request.SendWebRequest();

                if (isLoadingOn)
                    UIManager.Instance.CloseUI<LoadingPanel>();
                Response(requestName, request, resultCallBack);
            }
        }
        /// <summary>
        /// 请求响应
        /// </summary>
        /// <param name="requestName">请求名称</param>
        /// <param name="request">请求实例对象</param>
        /// <param name="resultCallBack">响应回调</param>
        private void Response(string requestName, UnityWebRequest request, UnityAction<bool, string> resultCallBack)
        {
            //Log.Warning("request.responseCode = " + request.responseCode);
            if (request.result != UnityWebRequest.Result.Success)
            {
                Log.Error(requestName + "出错:\r\n请求url:" + request.url + "\r\n错误提示:" + request.error + "\r\n服务器返回:" + request.downloadHandler.text);
                RequestErrorHaneler(request, resultCallBack);//request.responseCode包含UnityWebRequest和后台返回错误
            }
            else
            {
                string result = request.downloadHandler.text;
                Log.Info(requestName + "返回结果:\r\n<color=green>" + result + "</color>");

                // 取消前缀
                string requestUrl = GetNewUrl(request.url);

                //记录服务器返回数据到本地，离线模式使用，已停用
                //string value = ConfigXML.GetData(DtataType.LocalSever, requestUrl);
                //if (string.IsNullOrEmpty(value))
                //    ConfigXML.AddData(DtataType.LocalSever, requestUrl, result);
                //else
                //    ConfigXML.UpdateData(DtataType.LocalSever, requestUrl, result);

                resultCallBack?.Invoke(true, result);
            }
        }
        /// <summary>
        /// 通用错误提示
        /// </summary>
        /// <param name="ErrorCode">错误编码</param>
        private void RequestErrorHaneler(UnityWebRequest request, UnityAction<bool, string> resultCallBack)
        {
            ResultBase msg = JsonTool.DeSerializable<ResultBase>(request.downloadHandler.text);
            string info = string.Empty;//给客户查看提示
            switch (request.responseCode)
            {
                case 0:
                    info = "连接不到服务器!";
                    break;
                case 400:
                    info = "请求有误!";
                    break;
                case 401:
                    //if (msg != null)
                    //    ToolManager.MultipointLogin(msg.code);//有token后台返回错误
                    //else
                    ToolManager.MultipointLogin("登录失效！");//无token后台返回错误
                    return;
                case 403:
                    info = "操作权限不足，请升级权限!";
                    break;
                case 404:
                    info = "请检查网络!";
                    break;
                case 500:
                    info = "服务器出小差了!";
                    break;
                default:
                    info = "未知异常，请稍后重试!";
                    break;
            }

            //UIManager.Instance.OpenModuleUI<ToastPanel1>(this, UILevel.PopUp, new ToastPanelInfo1(info));//给客户查看弱提示

            //if (msg != null)
            //    resultCallBack?.Invoke(false, msg.code); //后台返回错误
            //else
                resultCallBack?.Invoke(false, info); //UnityWebRequest错误      
        }

        /// <summary>
        /// 开这个是为了限制总协程数量
        /// </summary>
        private void LateUpdate()
        {
            if (currentRequest < maxRequest)
            {
                if (requestList.Count > 0)
                {
                    currentRequest++;

                    lock (requestList)
                    {
                        tempData = requestList[0];
                        requestList.RemoveAt(0);
                    }

                    tempData.resultCallBack += (result, data) =>
                    {
                        currentRequest--;
                    };

                    TryRequest(tempData.requestName, tempData.requestType, tempData.requestUrl, tempData.paraJson, tempData.resultCallBack, tempData.isLoadingOn);
                }
            }
        }

        /// <summary>
        /// 为了缓存请求数据的结构
        /// </summary>
        private struct RequestData
        {
            public string requestName;
            public RequestType requestType;
            public string requestUrl;
            public string paraJson;
            public UnityAction<bool, string> resultCallBack;
            public bool isLoadingOn;
            public bool isContentTest;
            public Dictionary<string, string> headers;
        }
    }
}