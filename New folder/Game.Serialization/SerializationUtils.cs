using System;
using Colossal.AssetPipeline.Native;
using Colossal.Serialization.Entities;

namespace Game.Serialization;

public static class SerializationUtils
{
	public static bool IsCompressed(this BufferFormat format)
	{
		if (format != BufferFormat.CompressedLZ4)
		{
			return format == BufferFormat.CompressedZStd;
		}
		return true;
	}

	public static CompressionFormat BufferToCompressionFormat(BufferFormat format)
	{
		return format switch
		{
			BufferFormat.CompressedLZ4 => CompressionFormat.LZ4, 
			BufferFormat.CompressedZStd => CompressionFormat.ZSTD, 
			_ => throw new FormatException($"Invalid format {format}"), 
		};
	}
}
