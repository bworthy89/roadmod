using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct NetCrosswalkData : IComponentData, IQueryTypeParameter
{
	public Entity m_Lane;

	public float3 m_Start;

	public float3 m_End;
}
