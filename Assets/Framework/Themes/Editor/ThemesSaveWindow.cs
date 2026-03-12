using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace UnityFramework.Editor
{
    /// <summary>
    /// 主题配置保存窗口
    /// </summary>
    public class ThemesSaveWindow
    {
        private static XmlDocument xml = null;

        [MenuItem("Framework/ThemeConfig", false, 30)]
        public static void ThemeConfig()
        {
            FormTool.OpenFileDialog("保存主题配置文件(.xml)", Application.dataPath, (selectPath) =>
            {
                xml = new XmlDocument();
                //创建保存地址文件夹
                string saveDir = Path.GetDirectoryName(selectPath);
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                else if (File.Exists(selectPath))
                {
                    //加载已创建xml
                    xml.Load(selectPath);
                }

                //加载根节点
                XmlNode root = xml.SelectSingleNode("root");
                if (root == null)
                    root = xml.CreateElement("root");//创建最上一层的节点

                Object[] goList = Selection.objects;
                for (int i = 0; i < goList.Length; i++)
                {
                    if (goList[i] is GameObject)
                    {
                        IThemesHelper themesHelper = (goList[i] as GameObject).GetComponent<IThemesHelper>();
                        if (themesHelper != null)
                        {
                            Save(root, goList[i].name, themesHelper.SaveThemeData());
                        }
                    }
                }

                xml.AppendChild(root);
                //最后保存文件
                xml.Save(selectPath);
            });
        }

        /// <summary>
        /// 保存界面主题切换相关数据
        /// </summary>
        public static void Save(XmlNode root, string planeName, List<ThemeData> data)
        {
            XmlNode child = root.SelectSingleNode(planeName);
            if (child == null)
            {
                //添加界面节点
                child = xml.CreateElement(planeName);
            }
            else
            {
                //删除已保存数据
                child.RemoveAll();
            }

            //添加数据
            for (int i = 0; i < data.Count; i++)
            {
                XmlElement element = xml.CreateElement("data");
                //设置节点的属性
                element.SetAttribute("id", data[i].id);
                element.SetAttribute("resName", data[i].resName);
                element.SetAttribute("assetType", data[i].assetType.ToString());
                child.AppendChild(element);
            }

            root.AppendChild(child);
        }
    }
}