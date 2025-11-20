using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct SwayingData : IComponentData, IQueryTypeParameter
{
	public float3 m_VelocityFactors;

	public float3 m_SpringFactors;

	public float3 m_DampingFactors;

	public float3 m_MaxPosition;
}
