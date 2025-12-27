using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Steamworks.Data;
using UnityEngine;

namespace MultiMogul.MultiMogul
{
    public enum PacketType : uint
    {
        None = 0,
        OnConnectionApproved,
        OnAuthTicket,
        OnAuthTicketResponse,
        OnRPCMessage,
    }

    public class Packet : IDisposable
    {
        private MemoryStream stream;
        private BinaryWriter writer;
        private BinaryReader reader;
        private PacketType packetType;

        // construct empty packet for writing
        public Packet(PacketType type)
        {
            this.stream = new MemoryStream();
            this.writer = new BinaryWriter(this.stream);

            this.packetType = type;
            this.Write((uint)type); // write packet type as first data
        }

        // construct from received data for reading
        public Packet(byte[] data, int size)
        {
            this.stream = new MemoryStream(data, 0, size);
            this.reader = new BinaryReader(this.stream);
        }

        public Packet(IntPtr data, int size)
        {
            byte[] buffer = new byte[size];
            Marshal.Copy(data, buffer, 0, size);

            this.stream = new MemoryStream(buffer, 0, size);
            this.reader = new BinaryReader(this.stream);

            this.packetType = (PacketType)this.ReadUInt32();
        }

        public void Send(Steamworks.Data.Connection connection, Steamworks.Data.SendType sendType)
        {
            connection.SendMessage(this.ToArray(), sendType);
        }

        public PacketType GetPacketType() => this.packetType;

        public void Write(long value) => this.writer.Write(value);
        public void Write(ulong value) => this.writer.Write(value);
        public void Write(int value) => this.writer.Write(value);
        public void Write(uint value) => this.writer.Write(value);
        public void Write(float value) => this.writer.Write(value);
        public void Write(string value) => this.writer.Write(value ?? string.Empty);
        public void Write(Vector3 value)
        {
            this.writer.Write(value.x);
            this.writer.Write(value.y);
            this.writer.Write(value.z);
        }

        public void Write(Vector3Int value)
        {
            this.writer.Write(value.x);
            this.writer.Write(value.y);
            this.writer.Write(value.z);
        }

        public void Write(Quaternion value)
        {
            this.writer.Write(value.x);
            this.writer.Write(value.y);
            this.writer.Write(value.z);
            this.writer.Write(value.w);
        }
        public void WriteBytes(byte[] value)
        {
            if (value == null)
            {
                this.writer.Write(0); // length = 0
                return;
            }

            this.writer.Write(value.Length); // prefix with length
            this.writer.Write(value);
        }
        public void Write(bool value)
        {
            this.writer.Write(value);
        }

        public byte[] ToArray() => this.stream.ToArray();
        public int Length => (int)this.stream.Length;

        public long ReadInt64() => this.reader.ReadInt64();
        public ulong ReadUInt64() => this.reader.ReadUInt64();
        public int ReadInt32() => this.reader.ReadInt32();
        public uint ReadUInt32() => this.reader.ReadUInt32();
        public float ReadSingle() => this.reader.ReadSingle();
        public string ReadString() => this.reader.ReadString();
        public Vector3 ReadVector3()
        {
            float x = this.reader.ReadSingle();
            float y = this.reader.ReadSingle();
            float z = this.reader.ReadSingle();

            return new Vector3(x, y, z);
        }

        public Vector3Int ReadVector3Int()
        {
            int x = this.reader.ReadInt32();
            int y = this.reader.ReadInt32();
            int z = this.reader.ReadInt32();

            return new Vector3Int(x, y, z);
        }

        public Quaternion ReadQuaternion()
        {
            float x = this.reader.ReadSingle();
            float y = this.reader.ReadSingle();
            float z = this.reader.ReadSingle();
            float w = this.reader.ReadSingle();

            return new Quaternion(x, y, z, w);
        }
        public byte[] ReadBytes()
        {
            int length = this.reader.ReadInt32(); // read length first
            return this.reader.ReadBytes(length);
        }

        public bool ReadBool()
        {
            return this.reader.ReadBoolean();
        }

        public void ResetReader() => this.stream.Position = 0;

        public void Dispose()
        {
            this.writer?.Dispose();
            this.reader?.Dispose();
            this.stream?.Dispose();
        }
    }
}