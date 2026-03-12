using System.Collections.Generic;
using System;

public class BindableProperty<T> where T : IEquatable<T>
{
    //保存真正的值
    private T value;

    //get时返回真正的值，set时顺便调用值改变事件
    public T Value
    {
        get => value;
        set
        {
            if (!Equals(value, this.value))
            {
                this.value = value;
                OnValueChanged?.Invoke(value);
            }
        }
    }

    //用event存储值改变的事件
    public event Action<T> OnValueChanged;

    //初始化
    public BindableProperty(T value)
    {
        this.value = value;
    }
    public BindableProperty()
    {
        this.value = default(T);
    }

    public void InvokeValueChanged()
    {
        OnValueChanged?.Invoke(Value);
    }

    public void SetValueWithoutNotify(T value)
    {
        this.value = value;
    }
}