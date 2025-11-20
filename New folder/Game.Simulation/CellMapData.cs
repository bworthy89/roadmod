using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Simulation;

public struct CellMapData<T> where T : struct, ISerializable
{
	public NativeArray<T> m_Buffer;

	public float2 m_CellSize;

	public int2 m_TextureSize;
}
