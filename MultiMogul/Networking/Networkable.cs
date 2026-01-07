using MultiMogul.Networking.Packets;
using MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MultiMogul.Networking;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class Networkable : Attribute { /* empty class just for handling Network-able methods */
	public static Dictionary<PacketType, List<MethodInfo>> packetHandlers = new Dictionary<PacketType, List<MethodInfo>>();

	public readonly PacketType packetType;
	public readonly int packetHash;
	public readonly bool isServerMessage;

	public Networkable(PacketType packetId, string packetName, bool serverMessage) {
		this.packetType = packetId;
		this.packetHash = packetName.GetHashCode();
		this.isServerMessage = serverMessage;
	}

	public static void Register() {
		Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in assemblyTypes) {
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo method in methods) {
				if (method.GetCustomAttributes(typeof(Networkable), false).FirstOrDefault() is not Networkable attribute)
					continue;

				if (!method.IsStatic)
					continue;

				packetHandlers[attribute.packetType].Add(method);
				MMLog.Log($"found packet handler[{type.Namespace}.{type.Name}.{method.Name}({attribute.packetType.ToString()})]", LogTypes.Debug);
			}
		}
	}
}