using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.Utilities;

// utility class that returns all methods that are an implementation of an interface method
public static class InterfaceTargets {
	public static IEnumerable<MethodBase> Find<TInterface>(string methodName, Type[] parameterTypes = null, Assembly assembly = null) {
		Type iface = typeof(TInterface);
		MethodInfo ifaceMethod = iface.GetMethod(methodName, parameterTypes ?? Type.EmptyTypes)
			?? throw new MissingMethodException(iface.Name, methodName);

		var targets = new HashSet<MethodBase>();
		foreach (Type type in GetTypes(assembly ?? iface.Assembly)) {
			if (type.IsInterface || type.IsAbstract || type.ContainsGenericParameters || !iface.IsAssignableFrom(type))
				continue;

			InterfaceMapping map = type.GetInterfaceMap(iface);
			for (int i = 0; i < map.InterfaceMethods.Length; i++) {
				if (map.InterfaceMethods[i] != ifaceMethod)
					continue;

				MethodInfo target = map.TargetMethods[i];
				if (target.IsAbstract)
					continue;

				if (target.ContainsGenericParameters)
					continue;

				targets.Add(target);
			}
		}

		return targets;
	}

	static IEnumerable<Type> GetTypes(Assembly assembly) {
		try {
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e) {
			return e.Types.Where(t => t != null);
		}
	}
}