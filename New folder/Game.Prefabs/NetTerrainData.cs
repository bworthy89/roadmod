using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct NetTerrainData : IComponentData, IQueryTypeParameter
{
	public float2 m_WidthOffset;

	public float2 m_ClipHeightOffset;

	public float3 m_MinHeightOffset;

	public float3 m_MaxHeightOffset;
}
