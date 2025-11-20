using System;
using Colossal.AssetPipeline.Native;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct MeshTangent : IBufferElementData
{
	public float4 m_Tangent;

	public MeshTangent(float4 tangent)
	{
		m_Tangent = tangent;
	}

	public static void Unpack(NativeSlice<byte> src, DynamicBuffer<MeshTangent> dst, int count, VertexAttributeFormat format, int dimension)
	{
		dst.ResizeUninitialized(count);
		Unpack(src, dst.AsNativeArray(), count, format, dimension);
	}

	public unsafe static void Unpack(NativeSlice<byte> src, NativeArray<MeshTangent> dst, int count, VertexAttributeFormat format, int dimension)
	{
		if (format == VertexAttributeFormat.Float32 && dimension == 4)
		{
			src.SliceConvert<MeshTangent>().CopyTo(dst);
			return;
		}
		switch (format)
		{
		case VertexAttributeFormat.Float16:
			NativeMath.ArrayHalfToFloat((IntPtr)src.GetUnsafeReadOnlyPtr(), count, dimension, (IntPtr)dst.GetUnsafePtr(), 4);
			return;
		case VertexAttributeFormat.Float32:
			if (dimension == 1)
			{
				NativeMath.ArrayOctahedralToTangents((IntPtr)src.GetUnsafeReadOnlyPtr(), count, (IntPtr)dst.GetUnsafePtr());
				return;
			}
			break;
		}
		throw new Exception($"Unsupported source tangents format/dimension in Unpack {format} {dimension}");
	}
}
