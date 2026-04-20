using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityFramework.Runtime;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using static UnityFramework.Runtime.RequestData;
using UnityEngine.EventSystems;

/// <summary>
/// 登录窗口
/// </summary>
public class LoginModule : UIModuleBase
{
    private InputField Username;
    private InputField Password;
    //登录
    private Button Login;
    private Image Dot_01;
    private Image Dot_02;
    private Image Dot_03;

    private bool InRequest = false;
    private bool IsFinishLoading = false;

    protected override float joinAnimePlayTime => 0.3f;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        Init();
        InitDropdown();
        //SwitchbackAnim();

        GlobalInfo.account = null;
        ApiData.AccessToken = null;
    }

    /// <summary>
    /// 注册事件
    /// </summary>
    private void Init()
    {
        this.GetComponentByChildName<Text>("Version").text = $"V{Application.version}";

        Dot_01 = this.GetComponentByChildName<Image>("Dot_01");
        Dot_02 = this.GetComponentByChildName<Image>("Dot_02");
        Dot_03 = this.GetComponentByChildName<Image>("Dot_03");
        Dot_01.transform.localScale = Vector3.zero;
        Dot_02.transform.localScale = Vector3.zero;
        Dot_03.transform.localScale = Vector3.zero;

        var BackGround = this.GetComponentByChildName<CanvasGroup>("BackGround");

        Username = this.GetComponentByChildName<InputField>("Username");
        Password = this.GetComponentByChildName<InputField>("Password");

        Login = this.GetComponentByChildName<Button>("Login");
        {
            var UsernameError = this.FindChildByName("UsernameError").gameObject;
            var PasswordError = this.FindChildByName("PasswordError").gameObject;
            var UsernameErrorTip = this.GetComponentByChildName<Text>("UsernameErrorTip");
            var PasswordErrorTip = this.GetComponentByChildName<Text>("PasswordErrorTip");

            bool pass = false;

            Username.onValueChanged.AddListener(content =>
            {
                UsernameError.SetActive(false);
                UsernameErrorTip.text = string.Empty;
            });

            Username.onEndEdit.AddListener(content =>
            {
                if (string.IsNullOrEmpty(Username.text))
                {
                    UsernameError.SetActive(true);
                    UsernameErrorTip.text = "请输入手机号或工号";
                }
                //else if (Username.text.Length != 11)
                //{
                //    UsernameError.SetActive(true);
                //    UsernameErrorTip.text = "账号格式不正确";
                //}
                pass = !UsernameError.activeSelf && !PasswordError.activeSelf;
            });

#if UNITY_STANDALONE
            Password.GetComponentByChildName<Toggle>("PasswordToggle").onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    Password.textComponent.fontSize = 14;
                }
                else
                {
                    Password.textComponent.fontSize = 16;
                }
            });
#endif

            Password.onValueChanged.AddListener(content =>
            {
                PasswordError.SetActive(false);
                PasswordErrorTip.text = string.Empty;
            });

            Password.onEndEdit.AddListener(content =>
            {
                if (string.IsNullOrEmpty(Password.text))
                {
                    PasswordError.SetActive(true);
                    PasswordErrorTip.text = "请输入密码";
                }
                else if (Password.text.Length < 6)
                {
                    PasswordError.SetActive(true);
                    PasswordErrorTip.text = "请输入6-16位密码";
                }

                pass = !UsernameError.activeSelf && !PasswordError.activeSelf;
            });

            Login.onClick.AddListener(() =>
            {
                Username.onEndEdit.Invoke(Username.text);
                Password.onEndEdit.Invoke(Password.text);

                if (!pass)
                    return;

                if (InRequest)
                    return;

                InRequest = true;

                SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));
                //SendMsg(new MsgBase((ushort)LoginEvent.LoginAnim));

                LoginAnim(Login.GetComponentByChildName<Text>("Text"), () =>
                {
                    if (!InRequest) return;
                    ValidateLogin();
                }, this.GetCancellationTokenOnDestroy()).Forget();

                GlobalInfo.isOffLine = false;

                RequestManager.Instance.Login(Username.text, Password.text, LoginSuccess, (code, message) =>
                {
                    IsFinishLoading = false;
                    InRequest = false;
                    LoginFailure(code, message);
                });
            });

            this.GetComponentByChildName<Button>("Offline").onClick.AddListener(() =>
            {
                GlobalInfo.isOffLine = true;
                RequestManager.Instance.Login(Username.text, Password.text, LoginSuccess, LoginFailure);
            });

            var ExtendContent = this.FindChildByName("ExtendContent").gameObject;
            {
                var CommonAccount = JsonTool.DeSerializable<Dictionary<string, string>>(PlayerPrefs.GetString(GlobalInfo.commonAccount));

                Username.text = PlayerPrefs.GetString(GlobalInfo.accountCacheKey);
                if (CommonAccount != null && CommonAccount.ContainsKey(Username.text))
                {
                    Password.text = CommonAccount[Username.text];
                }
                else
                {
                    Username.text = "";
                }

                this.GetComponentByChildName<Button>("UsernameExtendButton").onClick.AddListener(() =>
                {
                    List<KeyValuePair<string, string>> accounts = CommonAccount?.ToList();
                    accounts.Reverse();
                    ExtendContent.transform.FindChildByName("Content").RefreshItemsView(accounts, (item, info) =>
                    {
                        item.GetComponentInChildren<Text>().text = info.Key;

                        Button temp = item.GetComponentByChildName<Button>("Input");
                        {
                            temp.onClick.RemoveAllListeners();
                            temp.onClick.AddListener(() =>
                            {
                                Username.text = info.Key;
                                Password.text = info.Value;
                                UsernameError.SetActive(false);
                                UsernameErrorTip.text = string.Empty;
                                PasswordError.SetActive(false);
                                PasswordErrorTip.text = string.Empty;
                                CloseDropdown(() => ExtendContent.SetActive(false));
                            });
                        }

                        temp = item.GetComponentByChildName<Button>("Del");
                        {
                            temp.onClick.RemoveAllListeners();
                            temp.onClick.AddListener(() =>
                            {
                                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                                popupDic.Add("删除", new PopupButtonData(() =>
                                {
                                    if (info.Key.Equals(Username.text))
                                    {
                                        Username.SetTextWithoutNotify("");
                                        Password.SetTextWithoutNotify("");
                                        //Login.interactable = false;
                                    }

                                    CommonAccount.Remove(info.Key);
                                    PlayerPrefs.SetString(GlobalInfo.commonAccount, JsonTool.Serializable(CommonAccount));
                                    Destroy(item.gameObject);
                                    //UIManager.Instance.Tip(ParentPanel, "删除成功!");
                                }, true));
                                popupDic.Add("取消", new PopupButtonData(null));
                                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", $"确认删除账号 {info.Key}  吗？删除后将无法快捷登录", popupDic));
                            });
                        }
                    });

                    ExtendContent.SetActive(true);
                    OpenDropdown();
                });

                this.GetComponentByChildName<Button>("CloseExtendButton").onClick.AddListener(() => CloseDropdown(() => ExtendContent.SetActive(false)));
            }
        }

        this.GetComponentByChildName<Button>("Forget").onClick.AddListener(() =>
        {
            UIManager.Instance.CloseModuleUI<LoginModule>(ParentPanel, null, () =>
            {
                SendMsg(new MsgBase((ushort)LoginEvent.Forget));
            });
        });

        this.GetComponentByChildName<Button>("Register").onClick.AddListener(() =>
        {
            UIManager.Instance.CloseModuleUI<LoginModule>(ParentPanel, null, () =>
            {
                SendMsg(new MsgBase((ushort)LoginEvent.Register));
            });
        });

        var Save = this.GetComponentByChildName<Toggle>("Save");
        {
            Save.onValueChanged.AddListener(isOn =>
            {
                PlayerPrefs.SetInt(GlobalInfo.savePasswordKey, isOn ? 1 : 0);
            });
            Save.SetIsOnWithoutNotify(PlayerPrefs.GetInt(GlobalInfo.savePasswordKey) == 1);
        }
    }

    /// <summary>
    /// 登录成功回调
    /// </summary>
    /// <param name="account"></param>
    /// <param name="message"></param>
    private void LoginSuccess(Account account, string message)
    {
        IsFinishLoading = false;
        SetAccountCache();

        ApiData.AccessToken = account.token.accessToken;

        //记录服务器返回数据到本地
        var tempSave = RequestBase.GetNewUrl(ApiData.Login);
        string value = ConfigXML.GetData(ConfigType.Cache, DtataType.LocalSever, tempSave);
        if (string.IsNullOrEmpty(value))
            ConfigXML.AddData(ConfigType.Cache, DtataType.LocalSever, tempSave, message);
        else
            ConfigXML.UpdateData(ConfigType.Cache, DtataType.LocalSever, tempSave, message);

        GlobalInfo.account = account;

        if (GlobalInfo.isOffLine)
            ValidateLogin();
    }
    /// <summary>
    /// 检查是否加入单位
    /// </summary>
    private void ValidateLogin()
    {
        if (!GlobalInfo.isOffLine)
            StartRefreshToken();

        if (GlobalInfo.account.schoolId == 0 || GlobalInfo.account.schoolName == null)
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("确定", new PopupButtonData(null, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "该账号没有加入单位，请联系管理员加入单位", popupDic));
            }

            //if (GlobalInfo.isOffLine)
            //    ToolManager.PleaseOnline();
            //else
            //{
            //    UIManager.Instance.CloseUI<LoginPanel>();
            //    UIManager.Instance.OpenUI<ActivationPanel>();
            //}
        }
        else
        {
            UIManager.Instance.CloseUI<LoginPanel>();
            UIManager.Instance.OpenUI<HomePagePanel>();
        }
        InRequest = false;
    }
    /// <summary>
    /// 登录失败
    /// </summary>
    /// <param name="code"></param>
    /// <param name="failureMessage"></param>
    private void LoginFailure(int code, string failureMessage)
    {
        Log.Warning($"登录失败！原因为：{failureMessage}");

        if (GlobalInfo.isOffLine)
        {
            ToolManager.PleaseOnline();
            return;
        }

        switch (code)
        {
            case 408:
                this.GetComponentByChildName<Text>("PasswordErrorTip").text = failureMessage;
                break;
            case 400:
                this.GetComponentByChildName<Text>("PasswordErrorTip").text = "账号或密码错误";
                break;
            case 0:
                ToolManager.InternetError();
                break;
            default:
                this.GetComponentByChildName<Text>("PasswordErrorTip").text = failureMessage;
                break;
        }
    }

    /// <summary>
    /// 设置账户缓存
    /// </summary>
    /// <param name="phoneNum"></param>
    /// <param name="password"></param>
    private void SetAccountCache()
    {
        var Username = this.GetComponentByChildName<InputField>("Username").text;
        var Password = this.GetComponentByChildName<InputField>("Password").text;

        var CommonAccount = JsonTool.DeSerializable<Dictionary<string, string>>(PlayerPrefs.GetString(GlobalInfo.commonAccount));
        {
            if (CommonAccount == null)
            {
                CommonAccount = new Dictionary<string, string>();
            }

            string cachePwd = string.Empty;
            if (this.GetComponentByChildName<Toggle>("Save").isOn)
            {
                cachePwd = Password;
            }

            if (!CommonAccount.ContainsKey(Username))
            {
                CommonAccount.Add(Username, cachePwd);
            }
            else
            {
                Dictionary<string, string> tempCache = new Dictionary<string, string>(CommonAccount.Count);
                foreach (KeyValuePair<string, string> userPwd in CommonAccount)
                {
                    if (userPwd.Key.Equals(Username))
                        continue;
                    tempCache.Add(userPwd.Key, userPwd.Value);
                }
                tempCache.Add(Username, cachePwd);
                CommonAccount = tempCache.ToDictionary(entry => entry.Key, entry => entry.Value);
            }

            PlayerPrefs.SetString(GlobalInfo.commonAccount, JsonTool.Serializable(CommonAccount));
        }

        PlayerPrefs.SetString(GlobalInfo.accountCacheKey, Username);
        PlayerPrefs.SetString(GlobalInfo.passwordCacheKey, Password);
    }

    /// <summary>
    /// 开始刷新token
    /// </summary>
    private void StartRefreshToken()
    {
        Timer.AddTimer(GlobalInfo.account.token.expiresIn / 2).OnCompleted(() =>
        {
            RequestManager.Instance.RefreshToken(GlobalInfo.account.token.refreshToken, (token) =>
            {
                GlobalInfo.account.token = token;
                ApiData.AccessToken = token.accessToken;
                Log.Debug("Token刷新成功");
                StartRefreshToken();
            }, (msg) =>
            {
                GlobalInfo.account.token = null;
                ApiData.AccessToken = null;

                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("确定", new PopupButtonData(() => ToolManager.GoToLogin(), true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("登录错误", "身份验证失败，请重新登录", popupDic));

                Log.Error($"刷新Token失败！原因为：{msg}");
            });
        });
    }

    /// <summary>
    /// 登录动效
    /// </summary>
    /// <param name="loginText"></param>
    /// <param name="callback"></param>
    async UniTaskVoid LoginAnim(Text loginText, UnityAction callback, System.Threading.CancellationToken ct)
    {
        IsFinishLoading = true;
        loginText.text = "登录中" + "<color=#FFFFFF00>-----</color>";
        Dot_01.gameObject.SetActive(true);
        Dot_02.gameObject.SetActive(true);
        Dot_03.gameObject.SetActive(true);

        int index = 0;
        float waitTime = 0.3f;
        float scal = 1.3f;
        float time = 0;

        //强制登录2s以上
        while (IsFinishLoading || time <= 2f)
        {
            index++;
            if (index == 0)
            {
                Dot_01.transform.DOScale(Vector3.one * scal, waitTime);
                Dot_02.transform.DOScale(Vector3.one, waitTime);
                Dot_03.transform.DOScale(Vector3.one, waitTime);
            }
            else if (index == 1)
            {
                Dot_01.transform.DOScale(Vector3.one, waitTime);
                Dot_02.transform.DOScale(Vector3.one * scal, waitTime);
                Dot_03.transform.DOScale(Vector3.one, waitTime);
            }
            else
            {
                Dot_01.transform.DOScale(Vector3.one, waitTime);
                Dot_02.transform.DOScale(Vector3.one, waitTime);
                Dot_03.transform.DOScale(Vector3.one * scal, waitTime);
                index = -1;
            }
            time += Time.deltaTime;
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: ct);
            time += waitTime;
        }

        ResetLogin(loginText);
        callback?.Invoke();
    }

    private void ResetLogin(Text loginText)
    {
        loginText.text = "登录";
        Dot_01.gameObject.SetActive(false);
        Dot_02.gameObject.SetActive(false);
        Dot_03.gameObject.SetActive(false);
        SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)ShortcutEvent.PressAnyKey:
                if (ShortcutManager.Instance.CheckShortcutKey(msg, ShortcutManager.SwapInput))
                {
                    if (Username.gameObject != EventSystem.current.currentSelectedGameObject)
                    {
                        Username.Select();
                    }
                    else
                    {
                        Password.Select();
                    }
                }
                else if (ShortcutManager.Instance.CheckShortcutKey(msg, ShortcutManager.Login))
                {
                    Login.onClick.Invoke();
                }
                break;
        }
    }

    public override void JoinAnim(UnityAction callback)
    {
        transform.localRotation = Quaternion.Euler(0, 90f, 0);
        JoinSequence.Append(transform.DOLocalRotate(Vector3.up * 0, JoinAnimePlayTime).SetEase(Ease.Linear));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Append(transform.DOLocalRotate(Vector3.up * 90f, ExitAnimePlayTime).SetEase(Ease.Linear));
        base.ExitAnim(callback);
    }

    #region 动效
    private float animeTime = 0.3f;
    private RectTransform arrow;
    private Image extendContentImage;
    private Mask mask;
    private void InitDropdown()
    {
        arrow = this.GetComponentByChildName<RectTransform>("UsernameExtendButton");
        extendContentImage = this.GetComponentByChildName<Image>("ExtendContent");
        mask = extendContentImage.AutoComponent<Mask>();

        UIMoveVertical moveVertical = this.FindChildByName("Username").AutoComponent<UIMoveVertical>();
        moveVertical.Init(this.FindChildByName("UsernameExtendButton") as RectTransform, -3f, 0f);
    }
    private void OpenDropdown()
    {
        mask.enabled = true;
        extendContentImage.fillAmount = 0;
        DOTween.To(() => extendContentImage.fillAmount, value => extendContentImage.fillAmount = value, 1, animeTime).OnComplete(() =>
        {
            mask.enabled = false;
        });
        arrow.DORotate(Vector3.forward * 180, animeTime);
    }
    private void CloseDropdown(UnityAction callBack)
    {
        mask.enabled = true;
        extendContentImage.fillAmount = 1;
        DOTween.To(() => extendContentImage.fillAmount, value => extendContentImage.fillAmount = value, 0, animeTime).OnComplete(() =>
        {
            mask.enabled = false;
            callBack.Invoke();
        });
        arrow.DORotate(Vector3.zero, animeTime);
    }
    #endregion
}