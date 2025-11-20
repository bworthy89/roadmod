using System;
using Colossal.AssetPipeline.Native;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct MeshUV0 : IBufferElementData
{
	public float2 m_Uv;

	public MeshUV0(float2 uv)
	{
		m_Uv = uv;
	}

	public static void Unpack(NativeSlice<byte> src, DynamicBuffer<MeshUV0> dst, int count, VertexAttributeFormat format, int dimension)
	{
		dst.ResizeUninitialized(count);
		Unpack(src, dst.AsNativeArray(), count, format, dimension);
	}

	public unsafe static void Unpack(NativeSlice<byte> src, NativeArray<MeshUV0> dst, int count, VertexAttributeFormat format, int dimension)
	{
		if (format == VertexAttributeFormat.Float32 && dimension == 2)
		{
			src.SliceConvert<MeshUV0>().CopyTo(dst);
			return;
		}
		if (format == VertexAttributeFormat.Float16)
		{
			NativeMath.ArrayHalfToFloat((IntPtr)src.GetUnsafeReadOnlyPtr(), count, dimension, (IntPtr)dst.GetUnsafePtr(), 2);
			return;
		}
		throw new Exception($"Unsupported source UV0 format/dimension in Unpack {format} {dimension}");
	}
}
