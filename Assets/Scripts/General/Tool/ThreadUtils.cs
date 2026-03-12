using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 在非主线程内调用Unity的方法（有些属性、方法只能在主线程中调用 "... can only be called from the main thread"）
/// </summary>
public class ThreadUtils : Singleton<ThreadUtils>
{
	Queue<Action> _methodsToInvoke = new Queue<Action>();

	/// <summary>
	/// Some functions in the UnityEngine namespace can only be invoked on the Main thread.
	/// Some CameraStream processes happen on a different thread, so when we need to do some Unity stuff,
	/// we can invoke them using this utility method.
	/// </summary>
	public void InvokeOnMainThread(Action method)
	{
		lock(_methodsToInvoke)
		{
			_methodsToInvoke.Enqueue(method);
		}
	}


	void Update()
	{
		lock(_methodsToInvoke)
		{
			//foreach (Action method in _methodsToInvoke)
			//{
			//	method();
			//}
			while(_methodsToInvoke.Count > 0)
			{
                _methodsToInvoke.Dequeue().Invoke();
            }
		}
	}
}
