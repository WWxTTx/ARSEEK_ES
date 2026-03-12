using Newtonsoft.Json;
using System.IO;

public class JsonTool
{
	/// <summary>
	/// 中文编码
	/// </summary>
	private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii };
	
	/// <summary>
	/// 对象转json
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static string Serializable(object obj)
	{
		return JsonConvert.SerializeObject(obj, serializerSettings);
	}
	/// <summary>
	/// json转对象
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="jsonData"></param>
	/// <returns></returns>
	public static T DeSerializable<T>(string jsonData)
	{
		return JsonConvert.DeserializeObject<T>(jsonData);
	}

    /// <summary>
    /// 格式化json
    /// </summary>
    /// <param name="str">输入json字符串</param>
    /// <returns>返回格式化后的字符串</returns>
    public static string ConvertJsonString(string str)
    {
        JsonSerializer serializer = new JsonSerializer();
        TextReader tr = new StringReader(str);
        JsonTextReader jtr = new JsonTextReader(tr);
        object obj = serializer.Deserialize(jtr);
        if (obj != null)
        {
            StringWriter textWriter = new StringWriter();
            JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            };
            serializer.Serialize(jsonWriter, obj);
            return textWriter.ToString();
        }
        else
        {
            return str;
        }
    }
}