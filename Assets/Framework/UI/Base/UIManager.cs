using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using DG.Tweening;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// UI窗体（位置）类型
    /// </summary>
    public enum UILevel
    {
        /// <summary>
        /// 普通窗体
        /// </summary>
        Normal,
        /// <summary>
        /// 固定窗体 
        /// </summary>                             
        Fixed,
        /// <summary>
        /// 弹出窗体 
        /// </summary> 
        PopUp,
        /// <summary>
        /// loading窗体 
        /// </summary> 
        Loading,
        /// <summary>
        /// 最顶层 用于强行置顶的页面 
        /// </summary>
        Top
    }


    /// <summary>
    /// UI控制器，包含根UI窗体的创建，UI的切换
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        /// <summary>
        /// 缓存单例UI窗体，key：类名称，value：类对象（组件）
        /// </summary>
        private Dictionary<string, UIPanelBase> allSingletonUI = new Dictionary<string, UIPanelBase>();
        /// <summary>
        /// 缓存重复UI
        /// </summary>
        private Dictionary<string, List<UIPanelBase>> allRepeatableUI = new Dictionary<string, List<UIPanelBase>>();
        /// <summary>
        /// 缓存模块UI
        /// </summary>
        private Dictionary<UIPanelBase, List<UIModuleBase>> allModuleBaseUI = new Dictionary<UIPanelBase, List<UIModuleBase>>();

        /// <summary>
        /// UI根节点
        /// </summary>
        private Transform traRoot = null;
        /// <summary>
        /// 全屏幕显示的节点
        /// </summary>
        private Transform traNormal = null;
        /// <summary>
        /// 固定显示的节点
        /// </summary>
        private Transform traFixed = null;
        /// <summary>
        /// 加载节点
        /// </summary>
        private Transform traLoading = null;
        /// <summary>
        /// 弹出节点
        /// </summary>
        private Transform traPopUp = null;
        /// <summary>
        /// 最顶层节点
        /// </summary>
        private Transform traTop = null;
        /// <summary>
        /// 渲染UI画布
        /// </summary>
        public Canvas canvas;
        protected override void InstanceAwake()
        {
            // 加载（根UI窗体）Canvas预设
            GameObject canvasGO = ResLoad.Instance.LoadAndInst(FormData.SystemCanvasPath, null, false);//实例化
            if (canvasGO == null)
            {
                Log.Fatal("UI框架未实例化");
                return;
            }
            //初始化
            traRoot = canvasGO.transform;
            traNormal = traRoot.Find(UILevel.Normal.ToString());
            traFixed = traRoot.Find(UILevel.Fixed.ToString());
            traLoading = traRoot.Find(UILevel.Loading.ToString());
            traPopUp = traRoot.Find(UILevel.PopUp.ToString());
            traTop = traRoot.Find(UILevel.Top.ToString());


            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main.transform.parent.GetComponent<Camera>();
            canvas.planeDistance = 10f;

            Vector2 canvasSize = canvas.GetComponent<RectTransform>().sizeDelta;
            GlobalInfo.CanvasWidth = (int)canvasSize.x;
            GlobalInfo.CanvasHeight = (int)canvasSize.y;

            DontDestroyOnLoad(traRoot);
        }

        #region 面板
        public UIPanelBase OpenUI<T>(UILevel canvasLevel = UILevel.Normal, UIData uiData = null, string prefabPath = null) where T : UIPanelBase
        {
            return OpenUI(typeof(T).Name, canvasLevel, uiData, prefabPath);
        }
        /// <summary>
        /// 打开UI，若UI未实例化则先实例化在打开，若已实例化则直接打开
        /// </summary>
        /// <typeparam name="T">继承UIPanelBase的UI面板类</typeparam>
        /// <param name="canvasLevel">显示位置（父对象）</param>
        /// <param name="uiData">传递参数</param>
        /// <param name="prefabPath">预制体在Resources下文件夹路径，为空值时加载默认路径下的预制体</param>
        /// <param name="position">固定面板UILevel.Normal 空间位置</param>
        /// <returns></returns>
        public UIPanelBase OpenUI(string panelName, UILevel canvasLevel = UILevel.Normal, UIData uiData = null, string prefabPath = null)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Log.Warning("panelName is null");
                return null;
            }

            UIPanelBase ui = null;
            bool isCreate = true;
            if (uiData != null && allRepeatableUI.ContainsKey(panelName))
            {
                List<UIPanelBase> list = allRepeatableUI[panelName];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].CheckIsDuplicated(uiData))
                    {
                        isCreate = false;
                        ui = list[i];
                        break;
                    }
                }
            }

            Transform parent = null;
            switch (canvasLevel)
            {
                case UILevel.Normal:
                    parent = traNormal;
                    break;
                case UILevel.Fixed:
                    parent = traFixed;
                    break;
                case UILevel.PopUp:
                    parent = traPopUp;
                    break;
                case UILevel.Loading:
                    parent = traLoading;
                    break;
                case UILevel.Top:
                    parent = traTop;
                    break;
                default:
                    parent = traNormal;
                    break;
            }

            if (isCreate && !allSingletonUI.TryGetValue(panelName, out ui))
                ui = TryCreate(panelName, parent, canvasLevel, uiData, prefabPath);

            if (ui == null)
                Log.Error(panelName + "未正常打开");
            else
            {

                //设置“UI克隆体”的父节点（根据克隆体中带的脚本中不同的“位置信息”）
                if (ui.transform.parent != parent)
                    ui.transform.SetParent(parent);

                ui.Show(uiData);
            }

            return ui;
        }
        /// <summary>
        /// 尝试创建UI，若已实例化则返回已实例化对象，若未实例化则实例化新对象
        /// </summary>
        /// <param name="panelName">继承UIPanelBase的UI面板类</param>
        /// <param name="canvasLevel">显示位置（父对象）</param>
        /// <param name="uiData">传递参数</param>
        /// <param name="prefabPath">预制体在Resources下文件夹路径，为空值时加载默认路径下的预制体</param>
        /// <returns></returns>
        private UIPanelBase TryCreate(string panelName, Transform parent, UILevel canvasLevel, UIData uiData, string prefabPath)
        {
            string strUIFormPaths = null;
            if (!string.IsNullOrEmpty(prefabPath))
                strUIFormPaths = prefabPath + panelName;
            else
                strUIFormPaths = FormData.SystemUIPrefabsPath + panelName;

            GameObject go = null;
            if (go == null)
                go = ResLoad.Instance.LoadAndInst(strUIFormPaths, parent, false);//根据“UI窗体名称”，加载“预设克隆体”

            if (go == null)
            {
                Log.Error("未正常实例化UI对象：" + panelName);
                return null;
            }

            UIPanelBase ui = go.GetComponent<UIPanelBase>();
            if (ui == null)
            {
                Log.Error(panelName + "-对象上未挂载UIPanelBase的子类脚本！ 预制体路径：" + strUIFormPaths);
                return null;
            }

            if (!ui.Repeatable)
                allSingletonUI.Add(panelName, ui);
            else
            {
                if (allRepeatableUI.ContainsKey(panelName))
                    allRepeatableUI[panelName].Add(ui);
                else
                {
                    List<UIPanelBase> list = new List<UIPanelBase>();
                    list.Add(ui);
                    allRepeatableUI.Add(panelName, list);
                }
            }

            ui.Open(uiData);
            return ui;
        }

 

        /// <summary>
        /// 提供一种全局的获取已创建UIPanel的方式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uIData"></param>
        /// <returns></returns>
        public UIPanelBase GetUI<T>(UIData uiData = null) where T : UIPanelBase 
        {
            string panelName = typeof(T).Name;
            UIPanelBase ui = null;
            if (uiData != null && allRepeatableUI.ContainsKey(panelName))
            {
                List<UIPanelBase> list = allRepeatableUI[panelName];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].CheckIsDuplicated(uiData))
                    {
                        ui = list[i];
                        return ui;
                    }
                }

                Log.Warning(panelName + "未实例化:uiData = " + uiData);
                return ui;
            }

            if (allSingletonUI.TryGetValue(panelName, out ui))
            {
            }
            else
                Log.Warning(panelName + "未实例化");

            return ui;
        }

        //新增一个返回默认第一个不需要data的方法
        public UIPanelBase GetUI<T>() where T : UIPanelBase
        {
            string panelName = typeof(T).Name;
            UIPanelBase ui = null;
            if (allRepeatableUI.ContainsKey(panelName))
            {
                List<UIPanelBase> list = allRepeatableUI[panelName];
                if(list.Count > 0)
                    return list[0];
            }
            return ui;
        }

        /// <summary>
        /// 显示已实例化ui
        /// </summary>
        public UIPanelBase ShowUI<T>(UIData uiData = null) where T : UIPanelBase
        {
            string panelName = typeof(T).Name;
            UIPanelBase ui = null;
            if (uiData != null && allRepeatableUI.ContainsKey(panelName))
            {
                List<UIPanelBase> list = allRepeatableUI[panelName];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].CheckIsDuplicated(uiData))
                    {
                        ui = list[i];
                        ui.Show(uiData);
                        return ui;
                    }
                }

                Log.Error(panelName + "未实例化:uiData = " + uiData);
                return ui;
            }

            if (allSingletonUI.TryGetValue(panelName, out ui))
            {
                if (ui)
                    ui.Show(uiData);
                else
                    Log.Error("字典中 " + panelName + "- Value = null");
            }
            else
                Log.Error(panelName + "未实例化");

            return ui;
        }
        /// <summary>
        /// 隐藏已实例化UI，不卸载与销毁
        /// </summary>
        public UIPanelBase HideUI<T>(UIData uiData = null, UnityAction callback = null) where T : UIPanelBase
        {
            string panelName = typeof(T).Name;
            UIPanelBase ui = null;
            if (allRepeatableUI.ContainsKey(panelName))
            {
                List<UIPanelBase> list = allRepeatableUI[panelName];
                if (uiData != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].CheckIsDuplicated(uiData))
                        {
                            ui = list[i];
                            HideAllModuleUI(ui);
                            ui.Hide(uiData, callback);
                            return ui;
                        }
                    }

                    Log.Warning(panelName + "未实例化:uiData = " + uiData);
                    return ui;
                }
                else
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        ui = list[i];
                        HideAllModuleUI(ui);
                        ui.Hide(uiData, callback);
                    }
                    return null;
                }
            }

            if (allSingletonUI.TryGetValue(panelName, out ui))
            {
                if (ui)
                {
                    HideAllModuleUI(ui);
                    ui.Hide(uiData, callback);
                }
                else
                    Log.Warning("字典中 " + panelName + "- Value = null");
            }
            else
                Log.Warning(panelName + "未实例化");

            return ui;
        }
        /// <summary>
        /// 隐藏所有UI，不卸载与销毁
        /// </summary>
        public void HideAllUI()
        {
            HideAllModuleUI();

            foreach (var item in allRepeatableUI)
            {
                List<UIPanelBase> list = item.Value;
                if (list == null)
                {
                    Log.Warning("字典中 " + item.Key + "- Value = null");
                    continue;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i])
                        list[i].Hide();
                    else
                        Log.Warning("字典中 " + item.Key + "-list[" + i + "] = null");
                }
            }

            foreach (var item in allSingletonUI)
            {
                if (item.Value)
                    item.Value.Hide();
                else
                    Log.Warning("字典中 " + item.Key + "- Value = null");
            }
        }
        /// <summary>
        /// 关闭、销毁、卸载UI
        /// </summary>
        public void CloseUI<T>(UIData uiData = null, UnityAction callback = null) where T : UIPanelBase
        {
            string panelName = typeof(T).Name;
            UIPanelBase ui = null;
            if (uiData != null && allRepeatableUI.ContainsKey(panelName))
            {
                List<UIPanelBase> list = allRepeatableUI[panelName];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].CheckIsDuplicated(uiData))
                    {
                        ui = list[i];
                        CloseAllModuleUI(ui);
                        ui.Close(uiData, callback);
                        list.Remove(ui);
                        return;
                    }
                }

                Log.Warning(panelName + "未实例化:uiData = " + uiData);
                return;
            }

            if (uiData == null && allRepeatableUI.ContainsKey(panelName))
            {
                List<UIPanelBase> list = allRepeatableUI[panelName];
                for (int i = 0; i < list.Count; i++)
                {
                    ui = list[i];
                    CloseAllModuleUI(ui);
                    ui.Close(uiData, callback);
                }
                list.Clear();
                allRepeatableUI.Remove(panelName);
                return;
            }

            if (allSingletonUI.TryGetValue(panelName, out ui))
            {
                if (ui)
                {
                    CloseAllModuleUI(ui);
                    ui.Close(uiData, callback);
                }
                else
                    Log.Warning("字典中 " + panelName + "- Value = null");

                allSingletonUI.Remove(panelName);
            }
            else
                Log.Warning(panelName + "未实例化");
        }
        /// <summary>
        ///  关闭、销毁、卸载字典中所有UI
        /// </summary>
        public void CloseAllUI()
        {
            CloseAllModuleUI();

            foreach (var item in allRepeatableUI)
            {
                List<UIPanelBase> list = item.Value;
                if (list == null)
                {
                    Log.Warning("字典中 " + item.Key + "- Value = null");
                    continue;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i])
                        list[i].Close();
                    else
                        Log.Warning("字典中 " + item.Key + "-list[" + i + "] = null");
                }
            }
            allRepeatableUI.Clear();

            foreach (var item in allSingletonUI)
            {
                if (item.Value)
                    item.Value.Close();
                else
                    Log.Warning("字典中 " + item.Key + "- Value = null");
            }

            allSingletonUI.Clear();
        }

        #endregion

        #region 模块控制
        UIPanelBase panelBase;
        public UIModuleBase OpenModuleUI<T>(UIPanelBase parentPanel = null, UILevel canvasLevel = UILevel.Normal, UIData uiData = null, string prefabPath = null) where T : UIModuleBase
        {
            if (parentPanel == null)
            {
                if (panelBase == null)
                    panelBase = new GameObject("UIPanelBase").AddComponent<UIPanelBase>();

                parentPanel = panelBase;
            }

            Transform parent = null;
            switch (canvasLevel)
            {
                case UILevel.Normal:
                    parent = traNormal;
                    break;
                case UILevel.Fixed:
                    parent = traFixed;
                    break;
                case UILevel.PopUp:
                    parent = traPopUp;
                    break;
                case UILevel.Loading:
                    parent = traLoading;
                    break;
                case UILevel.Top:
                    parent = traTop;
                    break;
                default:
                    parent = traNormal;
                    break;
            }
            return OpenModuleUI<T>(parentPanel, parent, uiData, prefabPath);
        }
        public UIModuleBase OpenModuleUI<T>(UIPanelBase parentPanel, Transform parent, UIData uiData = null, string prefabPath = null) where T : UIModuleBase
        {
            if (parentPanel == null || parent == null)
            {
                Log.Error($"{typeof(T).Name} 打开失败：parentPanel or parent is null");
                return null;
            }

            UIModuleBase ui = null;
            bool isCreate = true;
            if (allModuleBaseUI.ContainsKey(parentPanel))
            {
                List<UIModuleBase> list = allModuleBaseUI[parentPanel];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] as T)
                    {
                        if (list[i].Repeatable)
                        {
                            if (list[i].CheckIsDuplicated(uiData))
                            {
                                isCreate = false;
                                ui = list[i];
                                break;
                            }
                        }
                        else
                        {
                            isCreate = false;
                            ui = list[i];
                            break;
                        }
                    }
                }
            }

            if (isCreate)
                ui = TryCreateModule<T>(parentPanel, parent, uiData, prefabPath);

            if (ui == null)
                Log.Error(typeof(T).Name + "未正常打开");
            else
            {
                //设置“UI克隆体”的父节点（根据克隆体中带的脚本中不同的“位置信息”）
                if (ui.transform.parent != parent)
                    ui.transform.SetParent(parent);
                //RectTransform rectTransform = ui.GetComponent<RectTransform>();
                //rectTransform.anchoredPosition3D = Vector3.zero;
                ////ui.transform.localPosition = Vector3.zero;
                //ui.transform.localRotation = Quaternion.identity;
                //ui.transform.localScale = Vector3.one;

                //2020 06 06 子模块无法自己关闭自己 需要传入父panel
                //ui.ParentPanel = parentPanel;
                ui.Show(uiData);
            }

            return ui;
        }
        /// <summary>
        /// 尝试创建UI，若已实例化则返回已实例化对象，若未实例化则实例化新对象
        /// </summary>
        /// <param name="moduleName">继承UIPanelBase的UI面板类</param>
        /// <param name="canvasLevel">显示位置（父对象）</param>
        /// <param name="uiData">传递参数</param>
        /// <param name="prefabPath">预制体在Resources下文件夹路径，为空值时加载默认路径下的预制体</param>
        /// <returns></returns>
        private UIModuleBase TryCreateModule<T>(UIPanelBase parentPanel, Transform parent, UIData uiData, string prefabPath) where T : UIModuleBase
        {
            string moduleName = typeof(T).Name;
            string strUIFormPaths = null;
            if (!string.IsNullOrEmpty(prefabPath))
                strUIFormPaths = prefabPath + moduleName;
            else
                strUIFormPaths = FormData.ModulePrefabsPath + moduleName;

            GameObject go = null;
            if (go == null)
                go = ResLoad.Instance.LoadAndInst(strUIFormPaths, parent, false);//根据“UI窗体名称”，加载“预设克隆体”

            if (go == null)
            {
                Log.Error("未正常实例化UI对象：" + moduleName);
                return null;
            }

            UIModuleBase ui = go.GetComponent<UIModuleBase>();
            if (ui == null)
            {
                Log.Error(moduleName + "-对象上未挂载UIPanelBase的子类脚本！ 预制体路径：" + strUIFormPaths);
                return null;
            }
            if (allModuleBaseUI.ContainsKey(parentPanel))
                allModuleBaseUI[parentPanel].Add(ui);
            else
            {
                List<UIModuleBase> list = new List<UIModuleBase>();
                list.Add(ui);
                allModuleBaseUI.Add(parentPanel, list);
            }

            ui.ParentPanel = parentPanel;
            ui.Open(uiData);
            return ui;
        }
        /// <summary>
        /// 显示已隐藏UI模块
        /// </summary>
        public UIModuleBase ShowModuleUI<T>(UIPanelBase parentPanel = null, UIData uiData = null) where T : UIModuleBase
        {
            if (parentPanel == null)
                parentPanel = panelBase;

            UIModuleBase ui = null;
            if (allModuleBaseUI.ContainsKey(parentPanel))
            {
                List<UIModuleBase> list = allModuleBaseUI[parentPanel];
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        ui = list[i];
                        if (ui is T)
                        {
                            if (ui.Repeatable)
                            {
                                if (ui.CheckIsDuplicated(uiData))
                                {
                                    ui.Show(uiData);
                                    return ui;
                                }
                            }
                            else
                            {
                                ui.Show(uiData);
                                return ui;
                            }
                        }
                    }
                    Log.Error(typeof(T).Name + "未实例化");
                }
                else
                    Log.Error(parentPanel.ToString() + "未实例化模块");
            }
            else
                Log.Error(parentPanel.ToString() + "未实例化模块");

            return ui;
        }
        /// <summary>
        /// 隐藏已实例化UI模块，不卸载与销毁
        /// </summary>
        public UIModuleBase HideModuleUI<T>(UIPanelBase parentPanel = null, UIData uiData = null, UnityAction callback = null) where T : UIModuleBase
        {
            if (parentPanel == null)
                parentPanel = panelBase;

            UIModuleBase ui = null;
            if (allModuleBaseUI.ContainsKey(parentPanel))
            {
                List<UIModuleBase> list = allModuleBaseUI[parentPanel];
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        ui = list[i];
                        if (ui is T)
                        {
                            if (ui.Repeatable)
                            {
                                if (ui.CheckIsDuplicated(uiData))
                                {
                                    ui.Hide(uiData, callback);
                                    return ui;
                                }
                            }
                            else
                            {
                                ui.Hide(uiData, callback);
                                return ui;
                            }
                        }
                    }
                    Log.Warning(typeof(T).Name + "未实例化");
                }
                else
                    Log.Warning(parentPanel.ToString() + "未实例化模块");
            }
            else
                Log.Warning(parentPanel.ToString() + "未实例化模块");

            return ui;
        }
        /// <summary>
        /// 隐藏面板下所有已实例化UI模块，不卸载与销毁
        /// </summary>
        public void HideAllModuleUI(UIPanelBase parentPanel)
        {
            if (parentPanel == null)
                parentPanel = panelBase;

            if (allModuleBaseUI.ContainsKey(parentPanel))
            {
                List<UIModuleBase> list = allModuleBaseUI[parentPanel];
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i])
                            list[i].Hide();
                    }
                }
            }
            else
                Log.Warning(parentPanel.ToString() + "未实例化模块");
        }
        /// <summary>
        ///  隐藏所有已实例化UI模块，不卸载与销毁
        /// </summary>
        public void HideAllModuleUI()
        {
            foreach (var item in allModuleBaseUI)
            {
                if (item.Value != null)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        if (item.Value[i])
                            item.Value[i].Hide();
                    }
                }
            }
        }
        /// <summary>
        /// 卸载与销毁已实例化UI模块
        /// </summary>
        public void CloseModuleUI<T>(UIPanelBase parentPanel = null, UIData uiData = null, UnityAction callback = null) where T : UIModuleBase
        {
            if (parentPanel == null)
                parentPanel = panelBase;

            if (allModuleBaseUI.ContainsKey(parentPanel))
            {
                List<UIModuleBase> list = allModuleBaseUI[parentPanel];
                if (list != null)
                {
                    UIModuleBase ui = null;
                    for (int i = 0; i < list.Count; i++)
                    {
                        ui = list[i];
                        if (ui is T)
                        {
                            if (ui.Repeatable)
                            {
                                if (ui.CheckIsDuplicated(uiData))
                                {
                                    ui.Close(uiData, callback);
                                    list.Remove(ui);
                                    return;
                                }
                            }
                            else
                            {
                                ui.Close(uiData, callback);
                                list.Remove(ui);
                                return;
                            }
                        }
                    }
                    Log.Warning(typeof(T).Name + "未实例化");
                }
                else
                    Log.Warning(parentPanel.ToString() + "未实例化模块");
            }
            else
                Log.Warning(parentPanel.ToString() + "未实例化模块");
        }

        /// <summary>
        ///  卸载与销毁面板下实例化UI模块
        /// </summary>
        public void CloseAllModuleUI<T>(UIPanelBase parentPanel)
        {
            if (parentPanel == null)
                parentPanel = panelBase;

            if (allModuleBaseUI.ContainsKey(parentPanel))
            {
                List<UIModuleBase> list = allModuleBaseUI[parentPanel];
                if (list != null)
                {
                    UIModuleBase ui = null;
                    for (int i = 0; i < list.Count; i++)
                    {
                        ui = list[i];
                        if (ui is T)
                        {
                            ui.Close();
                            list.Remove(ui);
                        }
                    }
                }
                else
                    Log.Warning(parentPanel.ToString() + "未实例化模块");
            }
            else
                Log.Warning(parentPanel.ToString() + "未实例化模块");
        }

        /// <summary>
        ///  卸载与销毁面板下所有已实例化UI模块
        /// </summary>
        public void CloseAllModuleUI(UIPanelBase parentPanel)
        {
            if (parentPanel == null)
                parentPanel = panelBase;

            if (allModuleBaseUI.ContainsKey(parentPanel))
            {
                List<UIModuleBase> list = allModuleBaseUI[parentPanel];
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i])
                            list[i].Close();
                    }
                }
                allModuleBaseUI.Remove(parentPanel);
            }
            else
                Log.Warning(parentPanel.ToString() + "未实例化模块");
        }

        /// <summary>
        ///  卸载与销毁所有已实例化UI模块
        /// </summary>
        public void CloseAllModuleUI()
        {
            foreach (var item in allModuleBaseUI)
            {
                if (item.Value != null)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        if (item.Value[i])
                            item.Value[i].Close();
                    }
                }
            }

            allModuleBaseUI.Clear();
        }
        #endregion

        #region 常用操作
        /// <summary>
        /// 单例UI Panel或Module是否打开
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsOpen<T>() where T : UIBase
        {
            var name = typeof(T).ToString();

            if (allSingletonUI.ContainsKey(name))
            {
                return true;
            }

            if (allModuleBaseUI.SelectMany(moduleBaseUI => moduleBaseUI.Value).Any(value => value.name == name))
            {
                return true;
            }

            //if (allRepeatableUI.ContainsKey(name))
            //{
            //    return true;
            //}

            return false;
        }

  
        #endregion      
    }
}