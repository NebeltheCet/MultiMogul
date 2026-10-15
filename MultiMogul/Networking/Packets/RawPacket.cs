using MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiMogul.Networking.Packets;

public enum PacketType {
	Invalid = 0,
	OnUserInformationRequest,
	OnConnectionResponse
}

public class RawPacket : IDisposable {
	public static readonly int HeaderSize = sizeof(PacketType) + sizeof(int); // packet type + packet name hash
	public enum PacketUsage {
		None = 0,
		Writing,
		Reading
	}

	// type handlers
	private static readonly Dictionary<Type, Action<RawPacket, object>> _writeHandlers = [];
	private static readonly Dictionary<Type, Func<RawPacket, object>> _readHandlers = [];
	private static readonly Dictionary<Type, Action<RawPacket, object>> _enumHandlers = new()
		{
			{ typeof(byte),  (p, v) => p.Write((byte)v) },
			{ typeof(sbyte), (p, v) => p.Write((sbyte)v) },
			{ typeof(short), (p, v) => p.Write((short)v) },
			{ typeof(ushort),(p, v) => p.Write((ushort)v) },
			{ typeof(int),   (p, v) => p.Write((int)v) },
			{ typeof(uint),  (p, v) => p.Write((uint)v) },
			{ typeof(long),  (p, v) => p.Write((long)v) },
			{ typeof(ulong), (p, v) => p.Write((ulong)v) },
		};

	// packet data
	private readonly MemoryStream _stream;
	private readonly BinaryWriter _writer;
	private readonly BinaryReader _reader;

	public PacketType PacketType { get; }
	public PacketUsage Usage { get; }
	public long PayloadSize => this._stream.Length;

	#region Constructors
	static RawPacket() {
		// register primitive types
		RegisterHandler<byte>((p, v) => p._writer.Write((byte)v), p => p._reader.ReadByte());
		RegisterHandler<sbyte>((p, v) => p._writer.Write((sbyte)v), p => p._reader.ReadSByte());
		RegisterHandler<short>((p, v) => p._writer.Write((short)v), p => p._reader.ReadInt16());
		RegisterHandler<ushort>((p, v) => p._writer.Write((ushort)v), p => p._reader.ReadUInt16());
		RegisterHandler<int>((p, v) => p._writer.Write((int)v), p => p._reader.ReadInt32());
		RegisterHandler<uint>((p, v) => p._writer.Write((uint)v), p => p._reader.ReadUInt32());
		RegisterHandler<long>((p, v) => p._writer.Write((long)v), p => p._reader.ReadInt64());
		RegisterHandler<ulong>((p, v) => p._writer.Write((ulong)v), p => p._reader.ReadUInt64());
		RegisterHandler<float>((p, v) => p._writer.Write((float)v), p => p._reader.ReadSingle());
		RegisterHandler<double>((p, v) => p._writer.Write((double)v), p => p._reader.ReadDouble());
		RegisterHandler<bool>((p, v) => p._writer.Write((bool)v), p => p._reader.ReadBoolean());
		RegisterHandler<string>((p, v) => {
			var bytes = Encoding.UTF8.GetBytes((string)v ?? string.Empty);

			p._writer.Write(bytes.Length);
			p._writer.Write(bytes);
		}, p => {

			int len = p._reader.ReadInt32();
			return Encoding.UTF8.GetString(p._reader.ReadBytes(len));
		});

		RegisterHandler<UnityEngine.Vector3>((p, v) => {
			var vec = (UnityEngine.Vector3)v;

			p.Write(vec.x);
			p.Write(vec.y);
			p.Write(vec.z);

		}, p => new UnityEngine.Vector3 {
			x = p.Read<float>(),
			y = p.Read<float>(),
			z = p.Read<float>()
		});

		RegisterHandler<UnityEngine.Quaternion>((p, v) => {
			var q = (UnityEngine.Quaternion)v;

			p.Write(q.x);
			p.Write(q.y);
			p.Write(q.z);
			p.Write(q.w);

		}, p => new UnityEngine.Quaternion {
			x = p.Read<float>(),
			y = p.Read<float>(),
			z = p.Read<float>(),
			w = p.Read<float>()
		});
	}

	// construct empty packet for writing
	public RawPacket(PacketType type, string packetName) {
		this.AbstractHandler(); // let parent classes implement this for their own packets

		this._stream = new MemoryStream();
		this._writer = new BinaryWriter(this._stream);

		this.PacketType = type;
		this.Usage = PacketUsage.Writing;

		this.Write<PacketType>(type); // write packet type as first data
		this.Write<int>(packetName.GetHashCode());
		MMLog.Log($"constructed write packet of type[{this.GetType()}]", LogTypes.Packets);
	}

	// construct from raw data for reading
	public RawPacket(byte[] data, int size) {
		this.AbstractHandler(); // let parent classes implement this for their own packets

		this._stream = new MemoryStream(data, 0, size);
		this._reader = new BinaryReader(this._stream);

		this.PacketType = this.Read<PacketType>();
		this.Usage = PacketUsage.Reading;

		MMLog.Log($"constructed read packet of type[{this.PacketType}] and size[{size}]", LogTypes.Packets);
	}

	// construct from raw data for reading
	public RawPacket(IntPtr data, int size) {
		this.AbstractHandler(); // let parent classes implement this for their own packets

		byte[] buffer = new byte[size];
		Marshal.Copy(data, buffer, 0, size);

		this._stream = new MemoryStream(buffer, 0, size);
		this._reader = new BinaryReader(this._stream);

		this.PacketType = this.Read<PacketType>();
		this.Usage = PacketUsage.Reading;

		MMLog.Log($"constructed read packet of type[{this.PacketType}] and size[{size}]", LogTypes.Packets);
	}
	#endregion

	public static void Send<T>(T packet, Connection connection, SendType sendType) where T : RawPacket {
		RawPacket.Send(packet.ToRawPacket(), connection, sendType);
	}

	public static void Send(RawPacket packet, Connection connection, SendType sendType) {
		byte[] packetData = packet.ToArray();

		MMLog.Log($"sent packet of type[{packet.GetType()}] and size[{packetData.Length}] and send type[{sendType}]", LogTypes.Packets);
		connection.SendMessage(packetData, sendType);
		packet.Dispose();
	}

	#region Write Wrappers
	// generic write
	public void Write<T>(T value) {
		if (this.Usage != PacketUsage.Writing) {
			MMLog.LogException("tried writing in a non writing packet!", LogTypes.Packets);
		}

		Type type = typeof(T);
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
			Type elementType = type.GetGenericArguments()[0];
			var method = typeof(RawPacket)
				.GetMethod(nameof(WriteList), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.MakeGenericMethod(elementType);

			method.Invoke(this, [value]);
			return;
		}

		if (type.IsEnum) {
			Type underlyingType = Enum.GetUnderlyingType(type);
			if (!_enumHandlers.TryGetValue(underlyingType, out var writer)) {
				MMLog.LogException($"unsupported enum underlying type: {underlyingType}", LogTypes.Packets);
			}

			writer(this, Convert.ChangeType(value, underlyingType));
			return;
		}

		if (_writeHandlers.TryGetValue(type, out var handler)) {
			handler(this, value);
			return;
		}

		var customWrite = type.GetMethod("Write", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, binder: null, types: [typeof(RawPacket)], modifiers: null);
		if (customWrite != null &&
			customWrite.ReturnType == typeof(void)) {
			customWrite.Invoke(value, [this]);
			return;
		}

		MMLog.LogException($"no write handler or custom Write method for type {type}", LogTypes.Packets);
	}

	private void WriteList<T>(List<T> values) {
		this.Write<int>(values?.Count ?? 0);
		if (values == null) {
			return;
		}

		foreach (T element in values) {
			this.Write<T>(element);
		}
	}
	#endregion

	#region Read Wrappers
	// generic read
	public T Read<T>() {
		Type type = typeof(T);
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
			Type elementType = type.GetGenericArguments()[0];
			var method = typeof(RawPacket)
				.GetMethod(nameof(ReadList))
				.MakeGenericMethod(elementType);

			return (T)method.Invoke(this, null);
		}


		if (type.IsEnum) {
			Type underlyingType = Enum.GetUnderlyingType(type);
			var method = typeof(RawPacket)
						.GetMethod(nameof(Read))
						.MakeGenericMethod(underlyingType);

			object value = method.Invoke(this, null);
			return (T)Enum.ToObject(type, value);
		}

		if (_readHandlers.TryGetValue(type, out var handler)) {
			return (T)handler(this);
		}

		var customRead = type.GetMethod("Read", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, binder: null, types: [typeof(RawPacket)], modifiers: null);
		if (customRead != null && type.IsAssignableFrom(customRead.ReturnType)) {
			return (T)customRead.Invoke(null, [this]);
		}

		MMLog.LogException($"no read handler or custom read method for type {type}", LogTypes.Packets);
		return default;
	}

	public List<T> ReadList<T>() {
		int count = this.Read<int>();
		if (count < 0 || count > 100_000) {
			MMLog.LogException($"invalid list size: {count}", LogTypes.Packets);
			return [];
		}

		var values = new List<T>(count);
		for (int i = 0; i < count; i++) {
			values.Add(this.Read<T>());
		}

		return values;
	}
	#endregion

	#region Helpers 
	public byte[] ToArray() => this._stream.ToArray();
	public void ResetReader() => this._stream.Position = 0;

	internal static void RegisterHandler<T>(Action<RawPacket, object> write, Func<RawPacket, object> read) {
		_writeHandlers[typeof(T)] = write;
		_readHandlers[typeof(T)] = read;
	}

	public void Dispose() {
		this._writer?.Dispose();
		this._reader?.Dispose();
		this._stream?.Dispose();
	}
	#endregion

	#region Inheritance Helpers
	protected virtual void AbstractHandler() { } // empty for this class
	public virtual RawPacket ToRawPacket() => throw new NotImplementedException();
	#endregion
}
