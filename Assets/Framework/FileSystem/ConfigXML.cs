using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 可扩展，支持多个路径的配置文件
    /// </summary>
    public enum ConfigType
    {
        Cache
    }

    public enum DtataType
    {
        /// <summary>
        /// 设置节点
        /// </summary>
        setting,
        /// <summary>
        /// ab包数据父节点
        /// </summary>
        abs,
        /// <summary>
        /// 识别图数据父节点
        /// </summary>
        arImages,
        /// <summary>
        /// 封面图数据父节点
        /// </summary>
        images,
        /// <summary>
        /// 知识点数据父节点
        /// </summary>
        knowledgePoints,
        /// <summary>
        /// 本地存储的服务器数据
        /// </summary>
        LocalSever,
        /// <summary>
        /// UI主题节点
        /// </summary>
        theme,
    }

    public class ConfigXML
    {
        //private static string baseConfigPath { get { return UnityEngine.Application.streamingAssetsPath + "/BaseConfig.xml"; } }

        private static string cacheConfigPath { get { return ResManager.Instance.resourcesCacheRootPath + "/Config.xml"; } }

        /// <summary>
        /// 根节点
        /// </summary>
        private static string rootNodeName = "root";
        /// <summary>
        /// 数据节点
        /// </summary>
        private static string dataNodeName = "data";
        /// <summary>
        /// 数据key属性
        /// </summary>
        private static string keyAttributeName = "key";
        /// <summary>
        /// 数据value属性
        /// </summary>
        private static string valueAttributeName = "value";

        private static string ConfigPath(ConfigType configType)
        {
            switch (configType)
            {
                //case ConfigType.Base:
                //    return baseConfigPath;
                case ConfigType.Cache:
                default:
                    return cacheConfigPath;
            }
        }


        private static XmlDocument LoadXML(ConfigType configType)
        {
            try
            {
                var filePath = ConfigPath(configType);
                if (File.Exists(filePath))
                {
                    //创建xml文档
                    XmlDocument xml = new XmlDocument();
                    xml.Load(filePath);
                    return xml;
                }
                else
                {
                    //创建保存地址文件夹
                    string saveDir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);

                    //创建
                    XmlDocument xml = new XmlDocument();
                    //创建最上一层的节点。
                    XmlElement root = xml.CreateElement(rootNodeName);
                    //创建子节点
                    foreach (DtataType perName in Enum.GetValues(typeof(DtataType)))
                    {
                        XmlElement element = xml.CreateElement(perName.ToString());
                        root.AppendChild(element);
                    }
                    xml.AppendChild(root);
                    //最后保存文件
                    xml.Save(ConfigPath(configType));

                    return xml;
                }
            }
            catch (Exception e)
            {
                Log.Error($"配置文件加载失败：{e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取记录的数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">数据名称</param>
        /// <returns>数据值</returns>
        public static bool HasData(ConfigType configType, DtataType dataType, string key)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null)
                    return false;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNodeList xmlNodeList = root.SelectSingleNode(dataType.ToString())?.ChildNodes;
                if (xmlNodeList == null)
                    return false;

                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(keyAttributeName).Equals(key))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                Log.Error($"获取配置文件{configType}数据失败：{e.Message}");
                return false;
            }
        }


        /// <summary>
        /// 获取记录的数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">数据名称</param>
        /// <returns>数据值</returns>
        public static string GetData(ConfigType configType, DtataType dataType, string key)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null)
                    return null;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNodeList xmlNodeList = root.SelectSingleNode(dataType.ToString())?.ChildNodes;
                if (xmlNodeList == null)
                    return null;

                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(keyAttributeName).Equals(key))
                    {
                        string temp = xl1.GetAttribute(valueAttributeName);
                        return temp;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error($"获取配置文件{configType}数据失败：{e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取记录
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="value">数据值</param>
        /// <returns>记录Key</returns>
        public static string GetKey(ConfigType configType, DtataType dataType, string value)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null)
                    return null;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNodeList xmlNodeList = root.SelectSingleNode(dataType.ToString())?.ChildNodes;
                if (xmlNodeList == null)
                    return null;

                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(valueAttributeName).Equals(value))
                    {
                        string temp = xl1.GetAttribute(keyAttributeName);
                        return temp;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error($"获取配置文件{configType}记录失败：{e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">数据名称</param>
        /// <param name="value">数据值</param>
        public static void UpdateData(ConfigType configType, DtataType dataType, string key, string value)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null)
                    return;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNodeList xmlNodeList = root.SelectSingleNode(dataType.ToString()).ChildNodes;
                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(keyAttributeName).Equals(key))
                        xl1.SetAttribute(valueAttributeName, value);
                }
                xml.Save(ConfigPath(configType));
            }
            catch (Exception e)
            {
                Log.Error($"配置更新失败：{e.Message}");
            }
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">数据名称</param>
        /// <param name="value">数据值</param>
        public static void AddData(ConfigType configType, DtataType dataType, string key, string value)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null)
                    return;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNode child = root.SelectSingleNode(dataType.ToString());
                if (child == null)
                {
                    child = xml.CreateElement(dataType.ToString());
                }
                XmlElement element = xml.CreateElement(dataNodeName);
                //设置节点的属性
                element.SetAttribute(keyAttributeName, key);
                element.SetAttribute(valueAttributeName, value);
                child.AppendChild(element);
                root.AppendChild(child);
                xml.AppendChild(root);
                //最后保存文件
                xml.Save(ConfigPath(configType));
            }
            catch(Exception e)
            {
                Log.Error($"添加配置数据失败：{e.Message}");
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">数据名称</param>
        public static void DeleteData(ConfigType configType, DtataType dataType, string key)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null)
                    return;
                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNode child = root.SelectSingleNode(dataType.ToString());
                XmlNodeList xmlNodeList = child.ChildNodes;
                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(keyAttributeName).Equals(key))
                        child.RemoveChild(xl1);
                }
                xml.Save(ConfigPath(configType));
            }
            catch (Exception e)
            {
                Log.Error($"删除配置数据失败：{e.Message}");
            }
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="data">数据字典</param>
        public static void AddData(ConfigType configType, DtataType dataType, Dictionary<string, string> data)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null) return;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNode child = root.SelectSingleNode(dataType.ToString());
                if (child == null)
                    child = xml.CreateElement(dataType.ToString());

                XmlElement element = xml.CreateElement(dataNodeName);
                foreach (var item in data)
                {
                    //设置节点的属性
                    element.SetAttribute(item.Key, item.Value);
                }

                child.AppendChild(element);
                root.AppendChild(child);
                xml.AppendChild(root);
                //最后保存文件
                xml.Save(ConfigPath(configType));
            }
            catch (Exception e)
            {
                Log.Error($"添加配置数据失败：{e.Message}");
            }
        }

        /// <summary>
        /// 获取记录的数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">匹配数据名称</param>
        /// <param name="value">匹配数据值</param>
        /// <returns>数据值字典</returns>
        public static Dictionary<string, string> GetData(ConfigType configType, DtataType dataType, string key, string value)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null) return null;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNodeList xmlNodeList = root.SelectSingleNode(dataType.ToString())?.ChildNodes;
                if (xmlNodeList == null)
                    return null;

                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(key).Equals(value))
                    {
                        Dictionary<string, string> temp = new Dictionary<string, string>();
                        foreach (XmlAttribute att in xl1.Attributes)
                        {
                            temp.Add(att.Name, att.Value);
                        }
                        return temp;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error($"获取配置数据失败：{e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">匹配数据名称</param>
        /// <param name="value">匹配数据值</param>
        /// <param name="data">更新数据字典</param>
        public static void UpdateData(ConfigType configType, DtataType dataType, string key, string value, Dictionary<string, string> data)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null) return;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNodeList xmlNodeList = root.SelectSingleNode(dataType.ToString()).ChildNodes;
                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(key).Equals(value))
                    {
                        foreach (var item in data)
                        {
                            xl1.SetAttribute(item.Key, item.Value);
                        }
                    }
                }
                xml.Save(ConfigPath(configType));
            }
            catch (Exception e)
            {
                Log.Error($"更新配置数据失败：{e.Message}");
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="key">匹配数据名称</param>
        /// <param name="value">匹配数据值</param>
        public static void DeleteData(ConfigType configType, DtataType dataType, string key, string value)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if(xml == null) return;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNode child = root.SelectSingleNode(dataType.ToString());
                XmlNodeList xmlNodeList = child.ChildNodes;
                foreach (XmlElement xl1 in xmlNodeList)
                {
                    if (xl1.GetAttribute(key).Equals(value))
                        child.RemoveChild(xl1);
                }
                xml.Save(ConfigPath(configType));
            }
            catch (Exception e)
            {
                Log.Error($"删除配置数据失败：{e.Message}");
            }
        }

        /// <summary>
        /// 删除数据dataType下所有数据
        /// </summary>
        /// <param name="dataType">数据类型</param>
        public static void DeleteData(ConfigType configType, DtataType dataType)
        {
            try
            {
                XmlDocument xml = LoadXML(configType);
                if (xml == null) return;

                XmlNode root = xml.SelectSingleNode(rootNodeName);
                XmlNode child = root.SelectSingleNode(dataType.ToString());
                child.RemoveAll();
                //XmlNodeList xmlNodeList = child.ChildNodes;
                //foreach (XmlElement xl1 in xmlNodeList)
                //{
                //    child.RemoveChild(xl1); 
                //}
                xml.Save(ConfigPath(configType));
            }
            catch (Exception e)
            {
                Log.Error($"删除配置类型数据失败：{e.Message}");
            }
        }
    }
}
