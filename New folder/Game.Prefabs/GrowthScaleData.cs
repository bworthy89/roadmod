using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct GrowthScaleData : IComponentData, IQueryTypeParameter
{
	public float3 m_ChildSize;

	public float3 m_TeenSize;

	public float3 m_AdultSize;

	public float3 m_ElderlySize;

	public float3 m_DeadSize;
}
