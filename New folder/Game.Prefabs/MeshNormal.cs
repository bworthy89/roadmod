using System;
using Colossal.AssetPipeline.Native;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct MeshNormal : IBufferElementData
{
	public float3 m_Normal;

	public MeshNormal(float3 normal)
	{
		m_Normal = normal;
	}

	public static void Unpack(NativeSlice<byte> src, DynamicBuffer<MeshNormal> dst, int count, VertexAttributeFormat format, int dimension)
	{
		dst.ResizeUninitialized(count);
		Unpack(src, dst.AsNativeArray(), count, format, dimension);
	}

	public unsafe static void Unpack(NativeSlice<byte> src, NativeArray<MeshNormal> dst, int count, VertexAttributeFormat format, int dimension)
	{
		if (format == VertexAttributeFormat.Float32 && dimension == 3)
		{
			src.SliceConvert<MeshNormal>().CopyTo(dst);
			return;
		}
		switch (format)
		{
		case VertexAttributeFormat.Float16:
			NativeMath.ArrayHalfToFloat((IntPtr)src.GetUnsafeReadOnlyPtr(), count, dimension, (IntPtr)dst.GetUnsafePtr(), 3);
			return;
		case VertexAttributeFormat.SNorm16:
			if (dimension == 2)
			{
				NativeMath.ArrayOctahedralToNormals((IntPtr)src.GetUnsafeReadOnlyPtr(), count, (IntPtr)dst.GetUnsafePtr());
				return;
			}
			break;
		}
		throw new Exception($"Unsupported source normals format/dimension in Unpack {format} {dimension}");
	}
}
