using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 加密工具类
/// </summary>
public class Encryption
{
    /// <summary>
    /// MD5加密
    /// </summary>
    /// <param name="toEncrypt"></param>
    /// <returns></returns>
    public static string MD5Encrypt(string toEncrypt)
    {
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] t = md5.ComputeHash(Encoding.UTF8.GetBytes(toEncrypt));
        StringBuilder sb = new StringBuilder(32);
        for (int i = 0; i < t.Length; i++)
        {
            sb.Append(t[i].ToString("x").PadLeft(2, '0'));
        }
        return sb.ToString();
    }
    /// <summary>
    /// AES加密
    /// </summary>
    /// <param name="toEncrypt"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public static string AESEncrypt(string toEncrypt, string key, string iv)
    {
        byte[] keyArray = Encoding.UTF8.GetBytes(key);
        byte[] ivArray = Encoding.UTF8.GetBytes(iv);
        byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

        RijndaelManaged rDel = new RijndaelManaged();
        rDel.BlockSize = 128;
        rDel.KeySize = 256;
        rDel.FeedbackSize = 128;
        rDel.Padding = PaddingMode.PKCS7;
        rDel.Key = keyArray;
        rDel.IV = ivArray;
        rDel.Mode = CipherMode.CBC;

        ICryptoTransform cTransform = rDel.CreateEncryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        return Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }
}
