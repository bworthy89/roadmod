using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering;

public struct Swaying : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public float3 m_LastVelocity;

	public float3 m_SwayPosition;

	public float3 m_SwayVelocity;
}
