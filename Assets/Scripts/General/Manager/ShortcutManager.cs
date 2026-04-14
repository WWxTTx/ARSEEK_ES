using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 快捷键控制类
/// </summary>
public class ShortcutManager : Singleton<ShortcutManager>
{
    #region 注册快捷键
    /// <summary>
    /// 退出软件
    /// </summary>
    public const string ApplicationQuit = "ApplicationQuit";
    /// <summary>
    /// 打开设置
    /// </summary>
    public const string OpenOption = "OpenOption";

    #region 登录部分
    /// <summary>
    /// 切换输入框
    /// </summary>
    public const string SwapInput = "SwapInput";
    /// <summary>
    /// 登录
    /// </summary>
    public const string Login = "Login";
    #endregion

    #region 小场景部分
    /// <summary>
    /// 切换光标
    /// </summary>
    public const string SmallScene_SwitchCursor = "SmallScene_SwitchCursor";
    /// <summary>
    /// 小场景 打开地图
    /// </summary>
    public const string SmallScene_OpenMap = "SmallScene_OpenMap";
    /// <summary>
    /// 小场景 1号位道具
    /// </summary>
    public const string SmallScene_OpenItem01 = "SmallScene_OpenItem01";
    /// <summary>
    /// 小场景 2号位道具
    /// </summary>
    public const string SmallScene_OpenItem02 = "SmallScene_OpenItem02";
    /// <summary>
    /// 小场景 3号位道具
    /// </summary>
    public const string SmallScene_OpenItem03 = "SmallScene_OpenItem03";
    /// <summary>
    /// 小场景 4号位道具
    /// </summary>
    public const string SmallScene_OpenItem04 = "SmallScene_OpenItem04";
    /// <summary>
    /// 小场景 5号位道具
    /// </summary>
    public const string SmallScene_OpenItem05 = "SmallScene_OpenItem05";
    /// <summary>
    /// 小场景 6号位道具
    /// </summary>
    public const string SmallScene_OpenItem06 = "SmallScene_OpenItem06";
    /// <summary>
    /// 小场景 7号位道具
    /// </summary>
    public const string SmallScene_OpenItem07 = "SmallScene_OpenItem07";
    /// <summary>
    /// 小场景 8号位道具
    /// </summary>
    public const string SmallScene_OpenItem08 = "SmallScene_OpenItem08";
    /// <summary>
    /// 小场景 9号位道具
    /// </summary>
    public const string SmallScene_OpenItem09 = "SmallScene_OpenItem09";
    /// <summary>
    /// 小场景 10号位道具
    /// </summary>
    public const string SmallScene_OpenItem10 = "SmallScene_OpenItem10";
    #endregion
    /// <summary>
    /// 新快捷键要在此注册
    /// </summary>
    public void RegistrationShortcutKey()
    {
        jsonDatas = new Dictionary<string, KeyCode>();
        {
            jsonDatas.Add(ApplicationQuit, KeyCode.Escape);
            jsonDatas.Add(OpenOption, KeyCode.Escape);

            #region 登录部分
            jsonDatas.Add(SwapInput, KeyCode.Tab);
            jsonDatas.Add(Login, KeyCode.Return);
            #endregion

            #region 小场景部分
            jsonDatas.Add(SmallScene_SwitchCursor, KeyCode.LeftAlt);
            jsonDatas.Add(SmallScene_OpenMap, KeyCode.Tab);
            jsonDatas.Add(SmallScene_OpenItem01, KeyCode.F1);//Alpha1
            jsonDatas.Add(SmallScene_OpenItem02, KeyCode.F2);
            jsonDatas.Add(SmallScene_OpenItem03, KeyCode.F3);
            jsonDatas.Add(SmallScene_OpenItem04, KeyCode.F4);
            jsonDatas.Add(SmallScene_OpenItem05, KeyCode.F5);
            jsonDatas.Add(SmallScene_OpenItem06, KeyCode.F6);
            jsonDatas.Add(SmallScene_OpenItem07, KeyCode.Alpha7);
            jsonDatas.Add(SmallScene_OpenItem08, KeyCode.Alpha8);
            jsonDatas.Add(SmallScene_OpenItem09, KeyCode.Alpha9);
            jsonDatas.Add(SmallScene_OpenItem10, KeyCode.Alpha0);
            #endregion
        }
    }
    #endregion

    #region 代码部分
    private const string flag = "ShortcutJson";
    private Dictionary<string, KeyCode> jsonDatas;
    private Dictionary<KeyCode, MsgShortcut> msgs;

    /// <summary>
    /// 初始化
    /// </summary>
    protected override void InstanceAwake()
    {
        base.InstanceAwake();
        LoadShortcutKey();
    }
    /// <summary>
    /// 读取按键
    /// </summary>
    public void LoadShortcutKey()
    {
        var data = ConfigXML.GetData(ConfigType.Cache, DtataType.setting, flag);
        {
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    jsonDatas = JsonTool.DeSerializable<Dictionary<string, KeyCode>>(data);
                    UpdateShortcutKey();
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"解析失败! \n原因为：{e}");
                }
            }

            Log.Debug($"创建快捷键Json");
            RegistrationShortcutKey();
            SaveShortcutKey();
        }
    }
    /// <summary>
    /// 保存按键
    /// </summary>
    public void SaveShortcutKey()
    {
        ConfigXML.UpdateData(ConfigType.Cache, DtataType.setting, flag, JsonTool.Serializable(jsonDatas));
        UpdateShortcutKey();
    }
    /// <summary>
    /// 刷新快捷键遍历组
    /// </summary>
    public void UpdateShortcutKey()
    {
        msgs = jsonDatas.GroupBy(value => value.Value).ToDictionary(group => group.Key, group => new MsgShortcut()
        {
            msgId = (ushort)ShortcutEvent.PressAnyKey,
            keys = group.Select(value => value.Key).ToList()
        });
    }
    /// <summary>
    /// 更改快捷键
    /// </summary>
    /// <param name="flag">唯一ID</param>
    /// <param name="key">键值</param>
    public void ChangeShortcutKey(string flag, KeyCode key)
    {
        if (jsonDatas.ContainsKey(flag))
        {
            jsonDatas[flag] = key;
        }
        SaveShortcutKey();
    }
    /// <summary>
    /// 快捷检测按键 默认按下
    /// </summary>
    /// <param name="msg">消息</param>
    /// <param name="flag">唯一ID</param>
    /// <param name="state">状态 1按下 2抬起</param>
    /// <returns></returns>
    public bool CheckShortcutKey(MsgBase msg, string flag, int state = 1)
    {
        return msg is MsgShortcut key && key.state == state && key.keys.Contains(flag);
    }
    /// <summary>
    /// 快捷检测按键 默认按下
    /// </summary>
    /// <param name="msg">消息</param>
    /// <param name="events">事件组</param>
    /// <param name="state">状态 1按下 2抬起</param>
    public void CheckShortcutKey(MsgBase msg, Dictionary<string, System.Action> events, int state = 1)
    {
        if (msg is MsgShortcut key && key.state == state)
        {
            foreach (var Event in events)
            {
                if (key.keys.Contains(Event.Key))
                {
                    Event.Value?.Invoke();
                }
            }
        }
    }
    /// <summary>
    /// 遍历按键
    /// </summary>
    private void LateUpdate()
    {
        if(msgs != null)
        {
            foreach (var item in msgs)
            {
                if (Input.GetKeyDown(item.Key))
                {
                    item.Value.state = 1;
                    FormMsgManager.Instance.SendMsg(item.Value);
                }
                else if (Input.GetKeyUp(item.Key))
                {
                    item.Value.state = 2;
                    FormMsgManager.Instance.SendMsg(item.Value);
                }
            }
        }
    }
    #endregion
}

/// <summary>
/// 按键专用消息
/// </summary>
public class MsgShortcut : MsgBase
{
    /// <summary>
    /// 按下按键所需要通知的唯一ID组
    /// </summary>
    public List<string> keys;
    /// <summary>
    /// 0 未定义
    /// 1 按下
    /// 2 抬起
    /// </summary>
    public int state;
}