using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct NetVertexMatchData : IComponentData, IQueryTypeParameter
{
	public float3 m_Offsets;
}
