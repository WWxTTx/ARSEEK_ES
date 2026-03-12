using System;
using UnityEngine;
using UnityEngine.Playables;

public static class ArrayExtend
{
	public static void ForEach<T>(this T[] source, Action<T> action)
	{
		if (action == null) return;
		int len = source.Length;
		for (int i = 0; i < len; i++)
		{
			action(source[i]);
		}
	}

	public static T Find<T>(this T[] source, Predicate<T> action)
	{
		if (action == null) return default;
		int len = source.Length;
		for (int i = 0; i < len; i++)
		{
			if (action(source[i]))
				return source[i];
		}
		return default;
	}

	public static bool Contains<T>(this T[] source, T contains)
	{
		bool result = false;
		int len = source.Length;

		for (int i = 0; i < len; i++)
		{
			if (source[i].Equals(contains))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public static TResult[] Select<TSource,TResult>(this TSource[] source,Func<TSource,TResult> selector)
	{
		if (selector == null) return null;
		int len = source.Length;
		TResult[] result = new TResult[len];
		for (int i = 0; i < len; i++)
		{
			result[i] = selector(source[i]);
		}

		return result;
	}

	public static T Last<T>(this T[] t,Predicate<T> action)
	{
		int len = t.Length;
		if (action == null) return t[len - 1];
		for (int i = len-1; i >=0 ; i--)
		{
			T tmp = t[i];
			if (action != null && action.Invoke(tmp))
				return tmp;
		}

		return default;
	}
}
