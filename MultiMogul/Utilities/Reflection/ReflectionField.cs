using System;
using System.Reflection;

namespace MultiMogul.Utilities.Reflection;

public class ReflectionField<T> {
	private readonly object _instance;
	private readonly Func<T> _getter;
	private readonly Action<T> _setter;

	public ReflectionField(object instance, Type instanceType, string name, BindingFlags flags) { // instance overload
		this._instance = instance;

		var fieldInfo = instanceType.GetField(name, flags)
			?? throw new Exception($"failed to find \"{instanceType.Name}.{name}\" through reflection");

		this._getter = () => (T)fieldInfo.GetValue(this._instance);
		this._setter = value => fieldInfo.SetValue(this._instance, value);

		MMLog.Log($"set up reflection field[{instanceType.Name}.{name}]", LogTypes.Debug);
	}

	public T Value {
		get => this._getter();
		set => this._setter(value);
	}

	// implicitly assign the value to any local
	public static implicit operator T(ReflectionField<T> field) => field.Value;
}