using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.Utilities.Reflection;

public class ReflectionMethod<TResult> {
	private readonly object _instance;
	private readonly MethodInfo _methodInfo;

	public ReflectionMethod(object instance, Type instanceType, string name, BindingFlags flags) {
		this._instance = instance;

		this._methodInfo = instanceType.GetMethod(name, flags)
			?? throw new Exception($"failed to find \"{instanceType.Name}.{name}\"");

		MMLog.Log($"set up reflection method[{instanceType.Name}.{name}]", LogTypes.Debug);
	}

	public TResult Invoke(object[] args) => (TResult)this._methodInfo.Invoke(this._instance, args);
}
