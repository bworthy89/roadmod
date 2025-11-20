using Game.Net;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct UtilityObjectData : IComponentData, IQueryTypeParameter
{
	public UtilityTypes m_UtilityTypes;

	public float3 m_UtilityPosition;
}
