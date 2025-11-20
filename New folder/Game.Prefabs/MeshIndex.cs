using Unity.Collections;
using Unity.Entities;
using UnityEngine.Rendering;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct MeshIndex : IBufferElementData
{
	public int m_Index;

	public MeshIndex(int index)
	{
		m_Index = index;
	}

	public static void Unpack(NativeArray<byte> src, DynamicBuffer<MeshIndex> dst, int count, IndexFormat format)
	{
		dst.ResizeUninitialized(count);
		Unpack(src, dst.AsNativeArray(), count, format, 0);
	}

	public static void Unpack(NativeArray<byte> src, NativeArray<MeshIndex> dst, int count, IndexFormat format, int vertexOffset)
	{
		if (format == IndexFormat.UInt32)
		{
			NativeArray<int> nativeArray = src.Reinterpret<int>(1);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				dst[i] = new MeshIndex(nativeArray[i] + vertexOffset);
			}
		}
		else
		{
			NativeArray<ushort> nativeArray2 = src.Reinterpret<ushort>(1);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				dst[j] = new MeshIndex(nativeArray2[j] + vertexOffset);
			}
		}
	}
}
