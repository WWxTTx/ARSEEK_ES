using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public static class ListExtend
{
    /// <summary>
    /// Fisher-Yates shuffle algorithm
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static List<T> Shuffle<T>(this List<T> list)
    {
        RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();

        int n = list.Count;
        while (n > 1)
        {
            byte[] randomInt = new byte[4];
            rnd.GetBytes(randomInt);
            int k = Convert.ToInt32(randomInt[0]) % n;
            n--;
            T value = list[n];
            list[n] = list[k];
            list[k] = value;
        }

        return list;
    }
}
