using System;
using Colossal.AssetPipeline.Native;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct MeshVertex : IBufferElementData
{
	public float3 m_Vertex;

	public MeshVertex(float3 vertex)
	{
		m_Vertex = vertex;
	}

	public static void Unpack(NativeSlice<byte> src, DynamicBuffer<MeshVertex> dst, int count, VertexAttributeFormat format, int dimension)
	{
		dst.ResizeUninitialized(count);
		Unpack(src, dst.AsNativeArray(), count, format, dimension);
	}

	public unsafe static void Unpack(NativeSlice<byte> src, NativeArray<MeshVertex> dst, int count, VertexAttributeFormat format, int dimension)
	{
		if (format == VertexAttributeFormat.Float32 && dimension == 3)
		{
			src.SliceConvert<MeshVertex>().CopyTo(dst);
			return;
		}
		if (format == VertexAttributeFormat.Float16)
		{
			NativeMath.ArrayHalfToFloat((IntPtr)src.GetUnsafeReadOnlyPtr(), count, dimension, (IntPtr)dst.GetUnsafePtr(), 3);
			return;
		}
		throw new Exception($"Unsupported source position format/dimension in Unpack {format} {dimension}");
	}
}
