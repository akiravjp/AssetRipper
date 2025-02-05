﻿namespace AssetRipper.IO.Endian;

public partial struct EndianSpanWriter
{
	public EndianSpanWriter(Span<byte> data, EndianType type)
	{
		offset = 0;
		this.data = data;
		bigEndian = type == EndianType.BigEndian;
	}

	public EndianType Type
	{
		readonly get => bigEndian ? EndianType.BigEndian : EndianType.LittleEndian;
		set => bigEndian = value == EndianType.BigEndian;
	}

	public void Write(bool value)
	{
		Write(value ? (byte)1 : (byte)0);
	}

	public void Write(byte value)
	{
		data[offset] = value;
		offset++;
	}

	public void Write(sbyte value)
	{
		Write(unchecked((byte)value));
	}

	public void Write(char value)
	{
		Write((ushort)value);
	}

	public void Write(ReadOnlySpan<byte> value)
	{
		value.CopyTo(data.Slice(Position));
		offset += value.Length;
	}
}
