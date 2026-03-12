using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class SMSVerificationPanel : UIPanelBase
{
    private Dictionary<string, System.DateTime> datas = new Dictionary<string, System.DateTime>();
    private System.TimeSpan timeSpan = new System.TimeSpan(0, 1, 0);

    private bool isGetCode;
    /// <summary>
    /// 获取验证码提示文字
    /// </summary>
    private Text GetCode;
    /// <summary>
    /// 获取验证码提示文字内容
    /// </summary>
    private string codetext = "获取验证码";
    /// <summary>
    /// 获取验证码间隔时间
    /// </summary>
    private int intervaTime = 60;
    /// <summary>
    /// 获取验证码计时器
    /// </summary>
    private int timer;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        var data = uiData as PanelData;
        {
            this.GetComponentByChildName<Text>("Title").text = data.title;
            this.GetComponentByChildName<Text>("Hint").text = "将发送验证码到" + data.hint;

            this.GetComponentByChildName<Button>("Cancel").onClick.AddListener(() =>
            {
                data.onCancel?.Invoke();
                UIManager.Instance.CloseUI<SMSVerificationPanel>();
            });

            Button GetCodeBtn = this.GetComponentByChildName<Button>("GetCode");
            GetCode = GetCodeBtn.GetComponentInChildren<Text>();
            GetCode.text = codetext;
            GetCodeBtn.onClick.AddListener(() =>
            {
                if (isGetCode)
                    return;

                isGetCode = true;

                StartCoroutine(UpdateTime());
                RequestManager.Instance.GetCaptcha(data.phoneNumber, data.path, () =>
                {
                    UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("验证码发送成功"));
                }, (code, error) =>
                {
                    Log.Error($"请求验证码失败，原因为{error}", transform);

                    switch (code)
                    {
                        case 0:
                            UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("网络连接断开，请检查网络设置"));
                            break;
                        case 113:
                            UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo(error));
                            break;
                        default:
                            StopCoroutine(UpdateTime());
                            isGetCode = false;
                            GetCode.text = codetext;
                            UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo(error));
                            break;
                    }
                });
            });

            var inputField = this.GetComponentByChildName<InputField>("Code");
            {
                var button = this.GetComponentByChildName<Button>("Enter");
                {
                    button.onClick.AddListener(() =>
                    {
                        data.callBack.Invoke(inputField.text);
                        UIManager.Instance.CloseUI<SMSVerificationPanel>();
                    });
                }

                inputField.onValueChanged.AddListener(content =>
                {
                    if (inputField.text.Length >= 4)
                        button.interactable = true;
                    else
                        button.interactable = false;
                });
            }
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        StopCoroutine(UpdateTime());
    }

    /// <summary>
    /// 更新计时UI
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator UpdateTime()
    {
        timer = intervaTime;
        while (timer >= 0)
        {
            GetCode.text = $"({timer.ToString("D2")}s)";
            yield return new WaitForSeconds(1);
            timer -= 1;
        }
        isGetCode = false;
        GetCode.text = codetext;
    }

    public class PanelData : UIData
    {
        public string title;
        public string hint;
        public string phoneNumber;
        /// <summary>
        /// ApiData.Captcha
        /// </summary>
        public string path;
        public System.Action onCancel;
        public System.Action<string> callBack;
    }
}