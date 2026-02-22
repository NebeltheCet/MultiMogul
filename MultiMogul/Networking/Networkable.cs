using MultiMogul.Networking.Packets;
using MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MultiMogul.Networking;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class Networkable(PacketType packetId, uint maxSize, uint perSecond, bool serverMessage) : Attribute { /* empty class just for handling Network-able methods */
	public static Dictionary<PacketType, List<MethodInfo>> packetHandlers = [];

	public readonly PacketType packetType = packetId;
	public readonly int packetHash = packetId.ToString().GetHashCode();
	public readonly uint maxPacketSize = maxSize;
	public readonly uint maxPerSecond = perSecond;
	public readonly bool isServerMessage = serverMessage;

	public static void Register() {
		Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in assemblyTypes) {
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo method in methods) {
				if (method.GetCustomAttributes(typeof(Networkable), false).FirstOrDefault() is not Networkable attribute)
					continue;

				if (!method.IsStatic)
					continue;

				if (!packetHandlers.TryGetValue(attribute.packetType, out var handlerList)) {
					handlerList = [];
					packetHandlers[attribute.packetType] = handlerList;
				}

				handlerList.Add(method);
				MMLog.Log($"found packet handler[{type.Namespace}.{type.Name}.{method.Name}({attribute.packetType})]", LogTypes.Debug);
			}
		}
	}
}