using MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiMogul.Networking.Packets;

public enum PacketType {
	Invalid = 0,
}

public class RawPacket : IDisposable {
	public enum PacketUsage {
		None = 0,
		Writing,
		Reading
	}

	// type handlers
	private static readonly Dictionary<Type, Action<RawPacket, object>> _writeHandlers = new();
	private static readonly Dictionary<Type, Func<RawPacket, object>> _readHandlers = new();

	// packet data
	private MemoryStream _stream;
	private BinaryWriter _writer;
	private BinaryReader _reader;

	private readonly PacketUsage _packetUsage;
	private readonly PacketType _packetType;

	public PacketType PacketType => this._packetType;
	public PacketUsage Usage => this._packetUsage;
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
			var bytes = Encoding.UTF8.GetBytes((string)v ?? "");

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
	public RawPacket(PacketType type) {
		this.AbstractHandler(); // let parent classes implement this for their own packets

		this._stream = new MemoryStream();
		this._writer = new BinaryWriter(this._stream);

		this._packetType = type;
		this._packetUsage = PacketUsage.Writing;

		this.Write<PacketType>(type); // write packet type as first data
	}

	// construct from raw data for reading
	public RawPacket(byte[] data, int size) {
		this.AbstractHandler(); // let parent classes implement this for their own packets

		this._stream = new MemoryStream(data, 0, size);
		this._reader = new BinaryReader(this._stream);

		this._packetType = this.Read<PacketType>();
		this._packetUsage = PacketUsage.Reading;
	}

	// construct from raw data for reading
	public RawPacket(IntPtr data, int size) {
		this.AbstractHandler(); // let parent classes implement this for their own packets

		byte[] buffer = new byte[size];
		Marshal.Copy(data, buffer, 0, size);

		this._stream = new MemoryStream(buffer, 0, size);
		this._reader = new BinaryReader(this._stream);

		this._packetType = this.Read<PacketType>();
		this._packetUsage = PacketUsage.Reading;
	}
	#endregion

	#region Write Wrappers
	// generic write
	public void Write<T>(T value) {
		if (this._packetUsage != PacketUsage.Writing) {
			MMLog.LogException("tried writing in a non writing packet!", LogTypes.Packets);
		}

		Type type = typeof(T);
		if (type.IsEnum) {
			this.Write(Convert.ChangeType(value, Enum.GetUnderlyingType(type)));
			return;
		}

		if (!_writeHandlers.TryGetValue(type, out var handler)) {
			MMLog.LogException($"no write handler for type {type}", LogTypes.Packets);
		}

		handler(this, value);
	}
	#endregion

	#region Read Wrappers
	// generic read
	public T Read<T>() {
		Type type = typeof(T);
		if (type.IsEnum) {
			Type baseType = Enum.GetUnderlyingType(type);
			var enumValue = typeof(RawPacket).GetMethod(nameof(Read))
						.MakeGenericMethod(baseType)
						.Invoke(this, null);

			return (T)enumValue;
		}

		if (!_readHandlers.TryGetValue(type, out var handler)) {
			MMLog.LogException($"no read handler for type {type}", LogTypes.Packets);
		}

		return (T)handler(this);
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
	#endregion
}
