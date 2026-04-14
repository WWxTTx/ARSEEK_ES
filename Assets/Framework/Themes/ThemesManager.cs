using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityFramework.Editor;

namespace UnityFramework.Runtime
{
    public class ThemesManager : Singleton<ThemesManager>
    {
        //软件初始获取api，检查软件版本，检查资源版本，加载本地配置，加载本地资源
        //皮肤列表（名称、配置表、ab包）
        //皮肤资源（图片、字体）-ab包
        //皮肤参数（字体颜色）？

        private string path;
        private string themeID = "-1";
        private XmlDocument xml;

        /// <summary>
        /// 检查资源
        /// </summary>
        public bool CheckThemesUpdate()
        {
            //本地版本号与后台版本号对比判断是否为最新资源
            //若不是则下载最新资源
            //获取本地记录主题
            //加载主题
            return true;
        }
        /// <summary>
        /// 下载所有主题资源
        /// </summary>
        private bool DownloadThemes()
        {
            return true;
        }
        /// <summary>
        /// 获取所有主题,key-名称，value-id
        /// </summary>
        public Dictionary<string, string> GetThemes()
        {
            Dictionary<string, string> themes = new Dictionary<string, string>();
            //获取后台主题数据
            return themes;
        }

        /// <summary>
        /// 加载当前主题资源
        /// </summary>
        public bool LoadTheme(string themeID)
        {
            //加载ab包和配置表
            if (this.themeID != themeID)
            {
                LoadLocalAsset.Instance.UnloadAB(path, true);
                path = Application.persistentDataPath + "/" + themeID + ".abw";
                TextAsset[] assets = LoadLocalAsset.Instance.LoadABAll<TextAsset>(path);
                if (assets == null || assets.Length <= 0)
                {
                    Log.Error("主题配置表加载失败！");
                    return false;
                }
                TextAsset ob = assets[0];
                Log.Debug("ob.text=" + ob.text);
                xml = new XmlDocument();
                xml.LoadXml(ob.text);

                this.themeID = themeID;
            }
            return true;
        }
        /// <summary>
        /// 加载当前主题配置表
        /// </summary>
        public Dictionary<string, ThemeData> LoadThemeConfig(string planeName)
        {
            XmlNode root = xml.SelectSingleNode("root");
            XmlNode child = root.SelectSingleNode(planeName);
            XmlNodeList xmlNodeList = child.ChildNodes;
            Dictionary<string, ThemeData> data = new Dictionary<string, ThemeData>();
            foreach (XmlElement xl1 in xmlNodeList)
            {
                ThemeData themeData = new ThemeData();
                themeData.id = xl1.GetAttribute("id");
                themeData.resName = xl1.GetAttribute("resName");
                //themeData.assetType = xl1.GetAttribute("assetType");
                data.Add(themeData.id, themeData);
            }

            return data;
        }
        /// <summary>
        /// 获取当前界面资源
        /// </summary>
        public T Load<T>(string resName) where T : Object
        {
            T asset = LoadLocalAsset.Instance.LoadAB<T>(path, resName);
            return asset;
        }   
    }
}