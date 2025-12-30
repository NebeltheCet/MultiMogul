using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.Utilities.Reflection;

public class ReflectionProperty<T> {
	private readonly object _instance;
	private readonly Func<T> _getter;
	private readonly Action<T> _setter;

	public ReflectionProperty(object instance, Type instanceType, string name, BindingFlags flags) {
		this._instance = instance;

		var propertyInfo = instanceType.GetProperty(name, flags)
			?? throw new Exception($"failed to find \"{instanceType.Name}.{name}\" through reflection");

		this._getter = () => (T)propertyInfo.GetValue(this._instance);
		this._setter = value => propertyInfo.SetValue(this._instance, value);

		MMLog.Log($"set up reflection property[{instanceType.Name}.{name}]", LogTypes.Debug);
	}

	public T Value {
		get => this._getter();
		set => this._setter(value);
	}

	public static implicit operator T(ReflectionProperty<T> property) => property.Value;
}
