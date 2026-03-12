
using System.Collections.Generic;

namespace UnityFramework.Editor
{
    /// <summary>
    /// 主题资源类型
    /// </summary>
    public enum AssetType : byte
    {
        Sprite = 0,
        Font,
        Color,
        Shader
    }
    public class ThemeData
    {
        public string id;
        public string resName;
        public AssetType assetType;
        public ThemeData() { }
        public ThemeData(string id, string resName, AssetType assetType)
        {
            this.id = id;
            this.resName = resName;
            this.assetType = assetType;
        }
    }
    public interface IThemesHelper
    {
        List<ThemeData> SaveThemeData();

        void InitThemeData();
    }
}